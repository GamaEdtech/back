namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using EntityFramework.Exceptions.Common;

    using GamaEdtech.Common.Core.Extensions.Collections.Generic;
    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Data.Dto.Location;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;
    using GamaEdtech.Application.Interface;

    public class LocationService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<LocationService>> localizer, Lazy<ILogger<LocationService>> logger)
        : LocalizableServiceBase<LocationService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), ILocationService
    {
        public async Task<ResultData<ListDataSource<LocationsDto>>> GetLocationsAsync(ListRequestDto<Location>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Location, int>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new LocationsDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Code = t.Code,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<LocationDto>> GetLocationAsync([NotNull] ISpecification<Location> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var location = await uow.GetRepository<Location, int>().GetManyQueryable(specification).Select(t => new LocationDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Code = t.Code,
                    ParentId = t.ParentId,
                    ParentTitle = t.Parent != null ? t.Parent.Title : null,
                    Coordinates = t.Coordinates,
                    LocalTitle = t.LocalTitle,
                }).FirstOrDefaultAsync();

                return location is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["LocationNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = location };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> ManageLocationAsync([NotNull] ManageLocationRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Location, int>();
                Location? location = null;

                if (requestDto.Id.HasValue)
                {
                    location = await repository.GetAsync(requestDto.Id.Value);
                    if (location is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["LocationNotFound"] },],
                        };
                    }

                    location.ParentId = requestDto.ParentId ?? location.ParentId;
                    location.Title = requestDto.Title ?? location.Title;
                    location.Code = requestDto.Code ?? location.Code;
                    location.LocationType = requestDto.LocationType ?? location.LocationType;
                    location.Coordinates = requestDto.Coordinates ?? location.Coordinates;
                    location.LocalTitle = requestDto.LocalTitle ?? location.LocalTitle;

                    _ = repository.Update(location);
                }
                else
                {
                    location = new Location
                    {
                        Title = requestDto.Title,
                        Code = requestDto.Code,
                        LocationType = requestDto.LocationType,
                        ParentId = requestDto.ParentId,
                        Coordinates = requestDto.Coordinates,
                        LocalTitle = requestDto.LocalTitle,
                    };
                    repository.Add(location);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = location.Id };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidParentId"], }] };
            }
            catch (UniqueConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["DuplicateError"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveLocationAsync([NotNull] ISpecification<Location> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var location = await uow.GetRepository<Location, int>().GetAsync(specification);
                if (location is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["LocationNotFound"] },],
                    };
                }

                uow.GetRepository<Location, int>().Remove(location);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["LocationCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }
    }
}
