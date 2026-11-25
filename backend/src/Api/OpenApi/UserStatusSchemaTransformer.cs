using System.Text.Json.Nodes;
using Domain.Enums;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.OpenApi;

// UserStatusJsonConverter serializes the enum as text, which the default schema generator cannot see.
internal sealed class UserStatusSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(UserStatus))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            schema.Pattern = null;
            schema.Enum = [JsonValue.Create("active"), JsonValue.Create("inactive")];
        }

        return Task.CompletedTask;
    }
}
