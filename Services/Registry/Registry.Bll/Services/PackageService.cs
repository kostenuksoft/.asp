using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Registry.Bll.Dtos;
using Registry.Bll.Services.Interfaces;
using Registry.Bll.Specifications;
using Registry.Dal.UnitOfWork;
using Registry.Domain.Entities;
using Registry.Domain.Exceptions;

namespace Registry.Bll.Services;

public sealed class PackageService(
    IUnitOfWork unitOfWork,
    IPackageEvents packageEvents,
    IMapper mapper,
    ILogger<PackageService> logger
)
    : IPackageService
{
    internal const int MaxTopPackages = 50;

    private static readonly ActivitySource s_activitySource = new("Registry.Api");

    public async Task<PagedResult<PackageDto>> GetListAsync(
        PackageQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        int pageSize = Math.Clamp(query.PageSize, 1, PackageQuery.MaxPageSize);
        PackagesFilterSpec specification = new(query, pageSize);

        int totalCount = await unitOfWork.Packages.CountAsync(specification, cancellationToken);
        IReadOnlyList<Package> packages = await unitOfWork.Packages.ListAsync(specification, cancellationToken);

        return new PagedResult<PackageDto>(
            mapper.Map<IReadOnlyList<PackageDto>>(packages),
            totalCount,
            query.Page,
            pageSize
        );
    }

    public async Task<IReadOnlyList<PackageDto>> GetTopDownloadedAsync(
        int take,
        CancellationToken cancellationToken
    )
    {
        TopDownloadedPackagesSpec specification = new(Math.Clamp(take, 1, MaxTopPackages));
        IReadOnlyList<Package> packages = await unitOfWork.Packages.ListAsync(specification, cancellationToken);
        return mapper.Map<IReadOnlyList<PackageDto>>(packages);
    }

    public async Task<PackageDetailsDto> GetByIdAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        Package package = await unitOfWork.Packages.GetWithDetailsAsync(packageId, cancellationToken)
            ?? throw new EntityNotFoundException($"Package {packageId} was not found.");
        return mapper.Map<PackageDetailsDto>(package);
    }

    public async Task<PackageDto> CreateAsync(
        CreatePackageDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using Activity? activity = s_activitySource.StartActivity("CreatePackage");
        activity?.SetTag("publisher.id", input.PublisherId);

        Publisher publisher = await unitOfWork.Publishers.GetByIdAsync([input.PublisherId], cancellationToken)
            ?? throw new EntityNotFoundException($"Publisher {input.PublisherId} was not found.");

        if (await unitOfWork.Packages.ExistsByNameAsync(input.PublisherId, input.Name, cancellationToken))
        {
            throw new ConflictException($"Package '{publisher.Slug}/{input.Name}' already exists.");
        }

        Package package = new(input.PublisherId, input.Name, input.Description, input.ProjectUrl, input.LicensePrice);

        bool committed = false;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Packages.AddAsync(package, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            packageEvents.PackageCreated(package);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
            }
        }

        await packageEvents.PublishPendingAsync(CancellationToken.None);

        activity?.SetTag("package.id", package.PackageId);
        logger.LogInformation(
            "Package {PackageId} '{PackageKey}' created with licence price {LicensePrice}",
            package.PackageId,
            $"{publisher.Slug}/{package.Name}",
            package.LicensePrice
        );

        return mapper.Map<PackageDto>(package);
    }

    public async Task RenameAsync(
        long packageId,
        RenamePackageDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using Activity? activity = s_activitySource.StartActivity("RenamePackage");
        activity?.SetTag("package.id", packageId);

        Package package = await FindWithPublisherAsync(packageId, cancellationToken);
        if (await unitOfWork.Packages.ExistsByNameAsync(package.PublisherId, input.Name, cancellationToken))
        {
            throw new ConflictException($"Package '{package.Publisher.Slug}/{input.Name}' already exists.");
        }

        string previousName = package.Name;
        package.Rename(input.Name);
        packageEvents.PackageRenamed(package, previousName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await packageEvents.PublishPendingAsync(CancellationToken.None);

        logger.LogInformation(
            "Package {PackageId} renamed from '{PreviousPackageKey}' to '{PackageKey}'",
            packageId,
            $"{package.Publisher.Slug}/{previousName}",
            $"{package.Publisher.Slug}/{package.Name}"
        );
    }

    public async Task ChangeLicensePriceAsync(
        long packageId,
        ChangeLicensePriceDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using Activity? activity = s_activitySource.StartActivity("ChangeLicensePrice");
        activity?.SetTag("package.id", packageId);

        Package package = await FindWithPublisherAsync(packageId, cancellationToken);
        decimal previousPrice = package.LicensePrice;
        package.ChangeLicensePrice(input.LicensePrice);
        packageEvents.LicensePriceChanged(package, previousPrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await packageEvents.PublishPendingAsync(CancellationToken.None);

        logger.LogInformation(
            "Package {PackageId} licence price changed from {PreviousPrice} to {LicensePrice}",
            packageId,
            previousPrice,
            package.LicensePrice
        );
    }

    public async Task RegisterDownloadsAsync(
        long packageId,
        RegisterDownloadsDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        Package package = await FindAsync(packageId, cancellationToken);
        package.RegisterDownloads(input.Count);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlistAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = s_activitySource.StartActivity("UnlistPackage");
        activity?.SetTag("package.id", packageId);

        Package package = await FindAsync(packageId, cancellationToken);
        package.Unlist();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Package {PackageId} unlisted", packageId);
    }

    public async Task RelistAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        Package package = await FindAsync(packageId, cancellationToken);
        package.Relist();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignTagAsync(
        long packageId,
        AssignTagDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        Package package = await FindAsync(packageId, cancellationToken);
        Tag tag = await unitOfWork.Tags.GetByIdAsync([input.TagId], cancellationToken)
            ?? throw new EntityNotFoundException($"Tag {input.TagId} was not found.");

        if (await unitOfWork.PackageTags.GetByIdAsync([packageId, input.TagId], cancellationToken) is not null)
        {
            throw new ConflictException($"Package '{package.Name}' already has tag '{tag.Name}'.");
        }

        await unitOfWork.PackageTags.AddAsync(
            new PackageTag(packageId, input.TagId, DateTime.UtcNow),
            cancellationToken
        );
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        Package package = await FindWithPublisherAsync(packageId, cancellationToken);
        unitOfWork.Packages.Delete(package);
        packageEvents.PackageDeleted(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await packageEvents.PublishPendingAsync(CancellationToken.None);
    }

    private async Task<Package> FindWithPublisherAsync(
        long packageId,
        CancellationToken cancellationToken
    ) => await unitOfWork.Packages.GetWithPublisherAsync(packageId, cancellationToken)
        ?? throw new EntityNotFoundException($"Package {packageId} was not found.");

    private async Task<Package> FindAsync(
        long packageId,
        CancellationToken cancellationToken
    ) => await unitOfWork.Packages.GetByIdAsync([packageId], cancellationToken)
        ?? throw new EntityNotFoundException($"Package {packageId} was not found.");
}
