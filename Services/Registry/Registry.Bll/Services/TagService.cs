using AutoMapper;
using Registry.Bll.Dtos;
using Registry.Bll.Services.Interfaces;
using Registry.Dal.UnitOfWork;
using Registry.Domain.Entities;
using Registry.Domain.Exceptions;

namespace Registry.Bll.Services;

public sealed class TagService(IUnitOfWork unitOfWork, IMapper mapper) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Tag> tags = await unitOfWork.Tags.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<TagDto>>(tags);
    }

    public async Task<TagWithPackagesDto> GetWithPackagesAsync(
        long tagId,
        CancellationToken cancellationToken
    )
    {
        Tag tag = await unitOfWork.Tags.GetWithPackagesAsync(tagId, cancellationToken)
            ?? throw new EntityNotFoundException($"Tag {tagId} was not found.");
        return mapper.Map<TagWithPackagesDto>(tag);
    }

    public async Task<IReadOnlyList<PackageDto>> GetRecentlyTaggedPackagesAsync(
        string tagName,
        DateTime since,
        CancellationToken cancellationToken
    )
    {
        if (since > DateTime.UtcNow)
        {
            throw new ValidationException("Parameter 'since' cannot be in the future.");
        }

        if (!await unitOfWork.Tags.ExistsByNameAsync(tagName, cancellationToken))
        {
            throw new EntityNotFoundException($"Tag '{tagName}' was not found.");
        }

        IReadOnlyList<Package> packages = await unitOfWork.PackageTags.GetRecentlyTaggedAsync(tagName, since, cancellationToken);
        return mapper.Map<IReadOnlyList<PackageDto>>(packages);
    }
}
