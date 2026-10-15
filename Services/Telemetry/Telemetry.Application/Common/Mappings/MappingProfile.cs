using AutoMapper;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.ValueObjects;

namespace Telemetry.Application.Common.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Discussion, DiscussionDetailsDto>()
            .ForMember(dto => dto.Id, options => options.MapFrom(source => source.Id.ToString()));
        CreateMap<Comment, CommentDto>();
        CreateMap<Reply, ReplyDto>();

        CreateMap<CrashReport, CrashReportDto>()
            .ForMember(dto => dto.Id, options => options.MapFrom(source => source.Id.ToString()))
            .ForMember(
                dto => dto.DiscussionId,
                options => options.MapFrom(
                    source => source.DiscussionId.HasValue ? source.DiscussionId.Value.ToString() : null
                )
            );
        CreateMap<ExceptionInfo, ExceptionInfoDto>();
        CreateMap<StackFrame, StackFrameDto>();
        CreateMap<PerformanceMetric, PerformanceMetricDto>();
    }
}
