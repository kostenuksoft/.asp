using System.Text.Json;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Telemetry.Infrastructure.Persistence.Serializers;

public sealed class CamelCaseEnumSerializer<TEnum> : StructSerializerBase<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Deserialize(
        BsonDeserializationContext context,
        BsonDeserializationArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        return Enum.Parse<TEnum>(context.Reader.ReadString(), ignoreCase: true);
    }

    public override void Serialize(
        BsonSerializationContext context,
        BsonSerializationArgs args,
        TEnum value
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.WriteString(JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));
    }
}
