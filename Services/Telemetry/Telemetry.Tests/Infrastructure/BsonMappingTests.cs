using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.ValueObjects;
using Telemetry.Infrastructure.Persistence;
using Xunit;

namespace Telemetry.Tests.Infrastructure;

public sealed class BsonMappingTests
{
    private static readonly string[] s_commentFields = ["commentId", "author", "body", "postedAt", "replies"];
    private static readonly string[] s_metricFields = ["name", "observed", "threshold", "unit"];

    [Fact]
    public void DiscussionIsStoredWithTheLabOneFieldNames()
    {
        MongoMappings.Register();
        Discussion discussion = new(
            2,
            "platform-team/devhub-auth",
            "devhub-auth",
            "platform-team",
            "Token refresh fails behind a proxy",
            DiscussionCategory.FeatureRequest,
            "m.shevchenko",
            "tests"
        );
        discussion.AddComment("o.kovalenko", "Reproduced on 2.1.0");

        BsonDocument document = discussion.ToBsonDocument();

        string[] expected =
        [
            "_id", "createdAt", "createdBy", "updatedAt", "version", "packageId", "packageKey", "packageName",
            "publisherSlug", "title", "category", "state", "labels", "affectedVersions", "linkedBuildRunIds",
            "openedBy", "openedAt", "commentCount", "comments",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), document.Names.Order(StringComparer.Ordinal));
        Assert.Equal("featureRequest", document["category"].AsString);
        Assert.Equal("open", document["state"].AsString);
        Assert.Equal(s_commentFields, document["comments"][0].AsBsonDocument.Names);
    }

    [Fact]
    public void PrivateCollectionFieldsSurviveARoundTrip()
    {
        MongoMappings.Register();
        BsonDocument stored = BsonDocument.Parse("""
            {
              "_id": { "$oid": "6aa522a03c9a51e04b7d0001" },
              "packageId": { "$numberLong": "2" },
              "packageKey": "platform-team/devhub-auth",
              "packageName": "devhub-auth",
              "publisherSlug": "platform-team",
              "title": "Expired tokens throw instead of returning a validation result",
              "category": "bug",
              "state": "answered",
              "openedBy": "m.shevchenko",
              "openedAt": { "$date": "2026-09-12T10:00:00Z" },
              "commentCount": 1,
              "comments": [
                {
                  "commentId": "c1",
                  "author": "m.shevchenko",
                  "body": "Expired tokens throw.",
                  "postedAt": { "$date": "2026-09-12T10:00:00Z" },
                  "reactions": { "+1": 4 },
                  "replies": [
                    {
                      "commentId": "c1r1",
                      "author": "o.kovalenko",
                      "body": "Reproduced.",
                      "postedAt": { "$date": "2026-09-13T08:15:00Z" }
                    }
                  ]
                }
              ],
              "createdAt": { "$date": "2026-09-12T10:00:00Z" },
              "createdBy": "discussion-api",
              "version": 0
            }
            """);

        Discussion discussion = BsonSerializer.Deserialize<Discussion>(stored);
        discussion.AddComment("o.kovalenko", "Fixed in 2.1.0.");
        BsonDocument saved = discussion.ToBsonDocument();

        Comment first = discussion.Comments.First();
        Assert.Equal(4, first.Reactions!["+1"]);
        Assert.Equal("c1r1", Assert.Single(first.Replies).CommentId);
        Assert.Equal(2, saved["comments"].AsBsonArray.Count);
        Assert.Equal("c1r1", saved["comments"][0]["replies"][0]["commentId"].AsString);
        Assert.Equal("c2", saved["comments"][1]["commentId"].AsString);
    }

    [Fact]
    public void ValueObjectsAreStoredAsCamelCaseSubdocumentsAndCreatedThroughTheirConstructor()
    {
        MongoMappings.Register();
        CrashReport report = CrashReport.ForPerformanceDegradation(
            3,
            "platform-team/devhub-storage@1.4.2",
            3,
            "platform-team/devhub-storage",
            "f0e1d2c3b4a59687",
            DateTime.UtcNow.AddMinutes(-1),
            DeploymentEnvironment.Production,
            null,
            null,
            "tests",
            new PerformanceMetric("blobUploadLatencyP95", 2840.5, 1200, "ms")
        );

        BsonDocument document = report.ToBsonDocument();
        CrashReport restored = BsonSerializer.Deserialize<CrashReport>(document);

        Assert.Equal("performanceDegradation", document["kind"].AsString);
        Assert.Equal(s_metricFields, document["metric"].AsBsonDocument.Names);
        Assert.False(document.Contains("severity"));
        Assert.False(document.Contains("exception"));
        Assert.Equal(report.Metric, restored.Metric);
        Assert.Equal(report.Id, restored.Id);
    }
}
