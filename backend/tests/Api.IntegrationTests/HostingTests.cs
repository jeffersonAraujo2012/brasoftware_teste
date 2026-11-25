using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.IntegrationTests;

public sealed class HostingTests
{
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task OpenApiIsAvailableOnlyInDevelopment(string environment, HttpStatusCode expected)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDescribesStatusAsContractStrings()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var status = document.GetProperty("components").GetProperty("schemas").GetProperty("UserStatus");

        Assert.Equal("string", status.GetProperty("type").GetString());
        Assert.Equal(new[] { "active", "inactive" },
            status.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://localhost:8080", false)]
    public async Task CorsAllowsOnlyConfiguredOrigin(string origin, bool allowed)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/openapi/v1.json");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
