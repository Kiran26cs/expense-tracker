using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ExpensesBackend.API.Infrastructure.Data;

/// <summary>
/// Serializer that reads both BSON ObjectId (legacy data) and BSON String (new data)
/// as a C# string, and always writes as BSON String.
/// </summary>
public class FlexibleStringSerializer : SerializerBase<string>
{
    public static readonly FlexibleStringSerializer Instance = new();

    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        switch (context.Reader.CurrentBsonType)
        {
            case BsonType.ObjectId:
                return context.Reader.ReadObjectId().ToString();
            case BsonType.String:
                return context.Reader.ReadString();
            case BsonType.Null:
                context.Reader.ReadNull();
                return string.Empty;
            default:
                throw new BsonSerializationException($"Cannot deserialize a String from BsonType '{context.Reader.CurrentBsonType}'.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        context.Writer.WriteString(value ?? string.Empty);
    }
}
