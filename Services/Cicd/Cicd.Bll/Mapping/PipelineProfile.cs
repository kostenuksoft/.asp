using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Domain.Models;

namespace Cicd.Bll.Mapping;

public sealed class PipelineProfile : Profile
{
    public PipelineProfile()
    {
        CreateMap<Pipeline, PipelineDto>();
    }
}
