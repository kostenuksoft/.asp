using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Domain.Models;

namespace Cicd.Bll.Mapping;

public sealed class AgentProfile : Profile
{
    public AgentProfile()
    {
        CreateMap<Agent, AgentDto>();
    }
}
