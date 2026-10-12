using AutoMapper;
using Registry.Bll.Dtos;
using Registry.Domain.Entities;

namespace Registry.Bll.Mapping;

public sealed class PackageProfile : Profile
{
    public PackageProfile()
    {
        CreateMap<Package, PackageDto>();

        CreateMap<Package, PackageDetailsDto>()
            .ForCtorParam(nameof(PackageDetailsDto.Tags), options => options.MapFrom(package => package.PackageTags));

        CreateMap<PackageVersion, PackageVersionDto>()
            .ForCtorParam(
                nameof(PackageVersionDto.LicenseExpression),
                options => options.MapFrom(version => version.Manifest == null ? null : version.Manifest.LicenseExpression)
            )
            .ForCtorParam(
                nameof(PackageVersionDto.TargetFramework),
                options => options.MapFrom(version => version.Manifest == null ? null : version.Manifest.TargetFramework)
            );

        CreateMap<PackageTag, PackageTagDto>();
    }
}
