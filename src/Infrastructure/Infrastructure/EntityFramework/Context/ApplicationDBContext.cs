namespace GamaEdtech.Infrastructure.EntityFramework.Context
{
    using GamaEdtech.Domain.Entity.Identity;

    using GamaEdtech.Common.DataAnnotation;

    using global::EntityFramework.Exceptions.SqlServer;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    [ServiceLifetime(ServiceLifetime.Transient, "System.IServiceProvider,System.ComponentModel")]
    public class ApplicationDBContext(IServiceProvider serviceProvider) : Common.DataAccess.Context.IdentityEntityContext<ApplicationDBContext, ApplicationUser, ApplicationRole,
        int, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationRoleClaim, ApplicationUserToken>(serviceProvider)
    {
        protected override Assembly EntityAssembly => typeof(ApplicationUser).Assembly;

        protected override void OnConfiguring([NotNull] DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            _ = optionsBuilder.UseSqlServer(ConnectionName, t =>
            {
                _ = t.CommandTimeout(60 * 5);
                _ = t.UseNetTopologySuite();
            });
            _ = optionsBuilder.UseExceptionProcessor();
        }
    }
}
