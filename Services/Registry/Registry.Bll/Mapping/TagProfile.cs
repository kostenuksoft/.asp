using AutoMapper;
using Registry.Bll.Dtos;
using Registry.Domain.Entities;

namespace Registry.Bll.Mapping;

public sealed class TagProfile : Profile
{
    public TagProfile()
    {
        CreateMap<Tag, TagDto>();

        CreateMap<Tag, TagWithPackagesDto>()
            .ForCtorParam(nameof(TagWithPackagesDto.Packages), options => options.MapFrom(tag => tag.PackageTags));

        CreateMap<PackageTag, TaggedPackageDto>();
    }
}
