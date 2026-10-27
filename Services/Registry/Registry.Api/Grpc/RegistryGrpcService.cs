using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Registry.Api.Authorization;
using Registry.Bll.Dtos;
using Registry.Bll.Services.Interfaces;
using Registry.Domain.Exceptions;
using Registry.Grpc;

namespace Registry.Api.Grpc;

[Authorize(Policy = Permissions.ReadScope)]
public sealed class RegistryGrpcService(IPackageService packages) : RegistryService.RegistryServiceBase
{
    private const int MaxBatchSize = 100;
    private const decimal NanosPerUnit = 1_000_000_000m;

    public override async Task<PackageReply> GetPackage(
        GetPackageRequest request,
        ServerCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.PackageId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Package id must be positive."));
        }

        try
        {
            PackageDetailsDto package = await packages.GetByIdAsync(request.PackageId, context.CancellationToken);
            return ToReply(package);
        }
        catch (EntityNotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
    }

    public override async Task<PackagesReply> GetPackagesByIds(
        GetPackagesByIdsRequest request,
        ServerCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.PackageIds.Count is 0 or > MaxBatchSize)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Between 1 and {MaxBatchSize} package ids are required."
            ));
        }

        if (request.PackageIds.Any(packageId => packageId <= 0))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Package ids must be positive."));
        }

        PackagesReply reply = new();
        foreach (long packageId in request.PackageIds.Distinct())
        {
            try
            {
                reply.Packages.Add(ToReply(await packages.GetByIdAsync(packageId, context.CancellationToken)));
            }
            catch (EntityNotFoundException)
            {
                continue;
            }
        }

        return reply;
    }

    private static PackageReply ToReply(PackageDetailsDto package)
    {
        PackageReply reply = new()
        {
            PackageId = package.PackageId,
            PublisherSlug = package.PublisherSlug ?? string.Empty,
            Name = package.Name,
            LicensePrice = ToDecimalValue(package.LicensePrice),
            DownloadCount = package.DownloadCount,
            IsListed = package.IsListed
        };
        if (package.Description is not null)
        {
            reply.Description = package.Description;
        }

        return reply;
    }

    private static DecimalValue ToDecimalValue(decimal value)
    {
        long units = decimal.ToInt64(decimal.Truncate(value));
        int nanos = decimal.ToInt32((value - units) * NanosPerUnit);
        return new DecimalValue { Units = units, Nanos = nanos };
    }
}
