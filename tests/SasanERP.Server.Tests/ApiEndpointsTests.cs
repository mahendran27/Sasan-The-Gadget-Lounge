using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SasanERP.Server.Tests;

public class ApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboardTelemetry_Returns200AndJson()
    {
        var response = await _client.GetAsync("/api/dashboard/telemetry?timeframe=Today");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        Assert.NotNull(json);
        Assert.True(json.ContainsKey("todayRevenue") || json.ContainsKey("TodayRevenue"));
    }

    [Fact]
    public async Task GetProductsSearch_Returns200AndList()
    {
        var response = await _client.GetAsync("/api/products/search?query=Samsung");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStockValuation_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/live-stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
