using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Api.Serialization;

// Accepts only the exact contract values, unlike JsonStringEnumConverter, which also takes numbers and any casing.
internal sealed class UserStatusJsonConverter : JsonConverter<UserStatus>
{
    private const string Active = "active";
    private const string Inactive = "inactive";

    public override UserStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.ValueTextEquals(Active))
            {
                return UserStatus.Active;
            }

            if (reader.ValueTextEquals(Inactive))
            {
                return UserStatus.Inactive;
            }
        }

        throw new JsonException("Status must be active or inactive.");
    }

    public override void Write(Utf8JsonWriter writer, UserStatus value, JsonSerializerOptions options)
        => writer.WriteStringValue(value switch
        {
            UserStatus.Active => Active,
            UserStatus.Inactive => Inactive,
            _ => throw new JsonException("Status is invalid.")
        });
}
