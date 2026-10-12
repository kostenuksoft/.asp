using Microsoft.EntityFrameworkCore;
using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.Repositories;

public sealed class TagRepository(RegistryDbContext context) : GenericRepository<Tag>(context), ITagRepository
{
    public async Task<Tag?> GetWithPackagesAsync(
        long tagId,
        CancellationToken cancellationToken = default
    )
    {
        Tag? tag = await Set.FirstOrDefaultAsync(candidate => candidate.TagId == tagId, cancellationToken);
        if (tag is null)
        {
            return null;
        }

        await Context.Entry(tag)
            .Collection(loaded => loaded.PackageTags)
            .Query()
            .Include(packageTag => packageTag.Package)
            .LoadAsync(cancellationToken);

        return tag;
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    ) => Set.IgnoreQueryFilters().AnyAsync(tag => tag.Name == name, cancellationToken);
}
