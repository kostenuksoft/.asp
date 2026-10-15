using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetry.Application.Common.Mappings;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.ValueObjects;
using Xunit;

namespace Telemetry.Tests.Application;

public sealed class MappingProfileTests
{
    private readonly MapperConfiguration _configuration = new(
        expression => expression.AddProfile<MappingProfile>(),
        NullLoggerFactory.Instance
    );

    [Fact]
    public void ProfileMapsEveryDestinationMember()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void DiscussionMapsObjectIdToStringAndNestedComments()
    {
        Discussion discussion = new(
            2,
            "platform-team/devhub-auth",
            "devhub-auth",
            "platform-team",
            "Token refresh fails behind a proxy",
            DiscussionCategory.Bug,
            "m.shevchenko",
            "tests"
        );
        discussion.AddComment("o.kovalenko", "Reproduced on 2.1.0");

        DiscussionDetailsDto dto = _configuration.CreateMapper().Map<DiscussionDetailsDto>(discussion);

        Assert.Equal(discussion.Id.ToString(), dto.Id);
        Assert.Equal("o.kovalenko", Assert.Single(dto.Comments).Author);
    }

    [Fact]
    public void CrashReportMapsValueObjectsAndStackFrames()
    {
        CrashReport report = CrashReport.ForUnhandledException(
            2,
            "platform-team/devhub-auth@2.0.0",
            2,
            "platform-team/devhub-auth",
            "a1b2c3d4e5f60718",
            DateTime.UtcNow.AddMinutes(-1),
            DeploymentEnvironment.Production,
            CrashSeverity.High,
            null,
            "tests",
            new ExceptionInfo("System.TimeoutException", "The operation timed out.", null),
            [new StackFrame("ClaimValidator.Validate", "ClaimValidator.cs", 88)]
        );

        CrashReportDto dto = _configuration.CreateMapper().Map<CrashReportDto>(report);

        Assert.Equal(report.Id.ToString(), dto.Id);
        Assert.Equal("System.TimeoutException", dto.Exception?.Type);
        Assert.Equal(88, Assert.Single(dto.StackFrames!).Line);
        Assert.Null(dto.DiscussionId);
    }
}
