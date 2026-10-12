using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Domain.Models;

namespace Cicd.Bll.Mapping;

public sealed class BuildRunProfile : Profile
{
    public BuildRunProfile()
    {
        CreateMap<byte[], string>().ConvertUsing(source => Convert.ToBase64String(source));
        CreateMap<BuildRun, BuildRunDto>();
        CreateMap<BuildRunDetail, BuildRunDetailDto>();
        CreateMap<BuildStep, BuildStepDto>();
        CreateMap<RunTimeline, RunTimelineDto>();
        CreateMap<RunStatusChange, RunStatusChangeDto>();
        CreateMap<RunArtifact, RunArtifactDto>();
    }
}
