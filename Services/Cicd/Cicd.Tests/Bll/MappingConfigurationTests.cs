using AutoMapper;
using Cicd.Bll.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cicd.Tests.Bll;

public sealed class MappingConfigurationTests
{
    [Fact]
    public void EveryProfileMapsEveryDestinationMember()
    {
        MapperConfiguration configuration = new(
            expression => expression.AddMaps(typeof(BuildRunProfile).Assembly),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }
}
