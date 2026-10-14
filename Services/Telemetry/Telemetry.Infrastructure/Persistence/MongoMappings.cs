using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Telemetry.Domain.Common;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.ValueObjects;
using Telemetry.Infrastructure.Persistence.Serializers;

namespace Telemetry.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly Lock s_gate = new();
    private static bool s_registered;

    public static void Register()
    {
        lock (s_gate)
        {
            if (s_registered)
            {
                return;
            }

            ConventionRegistry.Register(
                "camelCase",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true),
                },
                _ => true
            );

            BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            BsonSerializer.RegisterSerializer(new CamelCaseEnumSerializer<DiscussionCategory>());
            BsonSerializer.RegisterSerializer(new CamelCaseEnumSerializer<DiscussionState>());
            BsonSerializer.RegisterSerializer(new CamelCaseEnumSerializer<CrashReportKind>());
            BsonSerializer.RegisterSerializer(new CamelCaseEnumSerializer<CrashSeverity>());
            BsonSerializer.RegisterSerializer(new CamelCaseEnumSerializer<DeploymentEnvironment>());

            BsonClassMap.RegisterClassMap<BaseEntity>(map => map.AutoMap());

            BsonClassMap.RegisterClassMap<Discussion>(map =>
            {
                map.AutoMap();
                map.MapField("_comments").SetElementName("comments");
            });

            BsonClassMap.RegisterClassMap<Comment>(map =>
            {
                map.AutoMap();
                map.MapField("_replies").SetElementName("replies");
            });

            BsonClassMap.RegisterClassMap<Reply>(map => map.AutoMap());

            BsonClassMap.RegisterClassMap<CrashReport>(map => map.AutoMap());

            BsonClassMap.RegisterClassMap<ExceptionInfo>(map =>
            {
                map.AutoMap();
                map.MapProperty(info => info.Type);
                map.MapProperty(info => info.Message);
                map.MapProperty(info => info.InnerType);
                map.MapCreator(info => new ExceptionInfo(info.Type, info.Message, info.InnerType));
            });

            BsonClassMap.RegisterClassMap<StackFrame>(map =>
            {
                map.AutoMap();
                map.MapProperty(frame => frame.Method);
                map.MapProperty(frame => frame.File);
                map.MapProperty(frame => frame.Line);
                map.MapCreator(frame => new StackFrame(frame.Method, frame.File, frame.Line));
            });

            BsonClassMap.RegisterClassMap<PerformanceMetric>(map =>
            {
                map.AutoMap();
                map.MapProperty(metric => metric.Name);
                map.MapProperty(metric => metric.Observed);
                map.MapProperty(metric => metric.Threshold);
                map.MapProperty(metric => metric.Unit);
                map.MapCreator(item => new PerformanceMetric(item.Name, item.Observed, item.Threshold, item.Unit));
            });

            s_registered = true;
        }
    }
}
