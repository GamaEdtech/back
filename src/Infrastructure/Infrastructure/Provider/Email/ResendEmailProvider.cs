namespace GamaEdtech.Infrastructure.Provider.Email
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.HttpProvider;
    using GamaEdtech.Data.Dto.Email;
    using GamaEdtech.Data.Dto.Provider.Email;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Infrastructure.Interface;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    using Resend;

    using Svix.Exceptions;

    using static GamaEdtech.Common.Core.Constants;
    using static GamaEdtech.Data.Dto.Email.EmailDto;

    public sealed class ResendEmailProvider(Lazy<ILogger<ResendEmailProvider>> logger, Lazy<IConfiguration> configuration, Lazy<IHttpProvider> httpProvider) : IEmailProvider
    {
        public EmailProviderType ProviderType => EmailProviderType.Resend;

        public async Task<ResultData<Common.Data.Void>> SendEmailAsync([NotNull] EmailRequestDto requestDto)
        {
            try
            {
                var client = CreateClient();
                var chunks = requestDto.Receivers.Chunk(100);
                List<string?> errors = [];
                foreach (var item in chunks)
                {
                    var response = await client.EmailBatchAsync(item.Select(t => new EmailMessage
                    {
                        From = requestDto.From,
                        HtmlBody = requestDto.Body,
                        Subject = requestDto.Subject,
                        To = EmailAddressList.From(t),
                    }));
                    if (!response.Success)
                    {
                        errors.Add(response.Exception?.Message);
                    }
                }

                return new(errors.Count == 0 ? OperationResult.Succeeded : OperationResult.Failed)
                {
                    Errors = errors.Count == 0 ? null : errors.Select(t => new Error { Message = t, }),
                };
            }
            catch (Exception exc)
            {
                logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<EmailDto>> ProccessInboundEmailAsync([NotNull] HttpRequest request)
        {
            try
            {
                if (!request.Body.CanSeek)
                {
                    request.EnableBuffering();
                }
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8);
                var payload = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                // Every early return below was previously silent - no log call at all, since none of these
                // are exceptions (the outer catch never sees them). Combined with TicketService.
                // ProccessInboundEmailAsync discarding a Failed result with no logging of its own, and
                // TicketsController.InboundWebHook always answering Resend/Svix with 200 regardless of
                // outcome (so it never retries), a dropped inbound email left zero trace anywhere. Fixed
                // 2026-09-09 - see docs/business/support-and-social.md.
                if (!request.Headers.TryGetValue("svix-id", out var svixId))
                {
                    logger.Value.LogWarning("ProccessInboundEmailAsync: missing required header 'svix-id'");
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Missing required header 'svix-id'", }] };
                }

                if (!request.Headers.TryGetValue("svix-timestamp", out var svixTimestamp))
                {
                    logger.Value.LogWarning("ProccessInboundEmailAsync: missing required header 'svix-timestamp' (svix-id {SvixId})", svixId.ToString());
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Missing required header 'svix-timestamp'", }] };
                }

                if (!request.Headers.TryGetValue("svix-signature", out var svixSignature))
                {
                    logger.Value.LogWarning("ProccessInboundEmailAsync: missing required header 'svix-signature' (svix-id {SvixId})", svixId.ToString());
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Missing required header 'svix-signature'", }] };
                }

                if (!long.TryParse(svixTimestamp.ToString(), out _))
                {
                    logger.Value.LogWarning("ProccessInboundEmailAsync: invalid 'svix-timestamp' value '{SvixTimestamp}' (svix-id {SvixId})", svixTimestamp.ToString(), svixId.ToString());
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Invalid value 'svix-timestamp', expected long", }] };
                }

                try
                {
                    var webhook = new Svix.Webhook(configuration.Value.GetValue<string>("EmailProvider:Resend:Secret")!);
                    webhook.Verify(payload, new WebHeaderCollection
                    {
                        { "svix-id", svixId.ToString() },
                        { "svix-timestamp", svixTimestamp.ToString() },
                        { "svix-signature", svixSignature.ToString() }
                    });
                }
                catch (WebhookVerificationException exc)
                {
                    // Includes a stale/expired timestamp (Svix's own replay-tolerance window), not just a
                    // wrong signature - worth knowing which, since one indicates tampering/misconfiguration
                    // and the other can indicate real delivery delay (e.g. this app mid-restart/deploy when
                    // Resend's original attempt came in, if Resend's own retry then lands outside the window).
                    logger.Value.LogWarning(exc, "ProccessInboundEmailAsync: webhook signature verification failed (svix-id {SvixId})", svixId.ToString());
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Invalid Signature", }] };
                }

                var data = await request.ReadFromJsonAsync<Data.Dto.Provider.Email.ResendResponse<ResendEmailReceivedWebhookDto>>();
                if (!"email.received".Equals(data?.Type, StringComparison.OrdinalIgnoreCase))
                {
                    // A different Resend event type (e.g. email.sent/delivered/bounced) hitting the same
                    // endpoint - not an error, just not one this handler acts on.
                    return new(OperationResult.Succeeded);
                }

                if (data.Data is null)
                {
                    // Type matched but the payload's own `data` didn't deserialize into anything usable -
                    // e.g. a Resend payload-shape change this DTO no longer matches. Unlike the branch
                    // above, this is a genuine anomaly worth surfacing, not an expected non-match.
                    logger.Value.LogWarning("ProccessInboundEmailAsync: 'email.received' webhook had a null/undeserializable data payload (svix-id {SvixId})", svixId.ToString());
                    return new(OperationResult.Succeeded);
                }

                var client = CreateClient();
                var content = await client.ReceivedEmailRetrieveAsync(data!.Data.EmailId);
                List<AttachmentDto>? lst = [];
                if (data.Data.Attachments?.Any() == true)
                {
                    foreach (var item in data.Data.Attachments)
                    {
                        var attachment = await client.EmailAttachmentRetrieveAsync(data!.Data.EmailId, item.Id);
                        if (string.IsNullOrEmpty(attachment.Content?.DownloadUrl))
                        {
                            continue;
                        }

                        var response = await httpProvider.Value.GetByteArrayAsync<IHttpRequest, IHttpRequest>(new()
                        {
                            Uri = attachment.Content.DownloadUrl,
                            Request = null,
                        });
                        if (response is not null)
                        {
                            lst.Add(new()
                            {
                                File = response,
                                Filename = item.Filename,
                                ContentType = item.ContentType,
                            });
                        }
                    }
                }
                return new(OperationResult.Succeeded)
                {
                    Data = new()
                    {
                        Body = string.IsNullOrEmpty(content.Content.HtmlBody) ? content.Content.TextBody : content.Content.HtmlBody,
                        From = data.Data.From,
                        To = data.Data.To,
                        Subject = data.Data.Subject,
                        Attachments = lst,
                    }
                };
            }
            catch (Exception exc)
            {
                logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        private IResend CreateClient() => ResendClient.Create(configuration.Value.GetValue<string>("EmailProvider:Resend:ApiToken")!);
    }
}
