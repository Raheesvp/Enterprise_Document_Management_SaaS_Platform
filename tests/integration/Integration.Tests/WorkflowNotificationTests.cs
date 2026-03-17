using FluentAssertions;
using Integration.Tests.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Integration.Tests;

public sealed class WorkflowNotificationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task WorkflowService_HealthCheck_ReturnsHealthy()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(
            "http://localhost:5003/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task NotificationService_HealthCheck_ReturnsHealthy()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(
            "http://localhost:5004/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task StartWorkflow_ValidRequest_ReturnsInProgress()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client = CreateClient(token);

        var response = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 1));

        response.IsSuccessStatusCode.Should().BeTrue();
        var json = await ParseResponse(response);
        json.GetProperty("status").GetString()
            .Should().Be("InProgress");
    }

    [Fact]
    public async Task ApproveWorkflow_TwoStages_ReturnsApproved()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client = CreateClient(token);

        var startResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 2));

        var startJson  = await ParseResponse(startResponse);
        var workflowId = startJson.GetProperty("id").GetString()!;

        await client.PostAsJsonAsync(
            $"http://localhost:5003/api/workflow/{workflowId}/approve",
            new { comments = "Stage 1 approved" });

        var finalResponse = await client.PostAsJsonAsync(
            $"http://localhost:5003/api/workflow/{workflowId}/approve",
            new { comments = "Stage 2 approved" });

        var finalJson = await ParseResponse(finalResponse);
        finalJson.GetProperty("status").GetString()
            .Should().Be("Approved");
    }

    [Fact]
    public async Task RejectWorkflow_ReturnsRejected()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client = CreateClient(token);

        var startResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 1));

        startResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Start failed: " +
            $"{await startResponse.Content.ReadAsStringAsync()}");

        var startJson  = await ParseResponse(startResponse);
        var workflowId = startJson.GetProperty("id").GetString()!;

        var rejectResponse = await client.PostAsJsonAsync(
            $"http://localhost:5003/api/workflow/{workflowId}/reject",
            new { comments = "Does not meet requirements" });

        var rejectContent = await rejectResponse.Content
            .ReadAsStringAsync();

        rejectResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Reject failed with: {rejectContent}");

        var rejectJson = JsonSerializer.Deserialize<JsonElement>(
            rejectContent, JsonOptions);

        rejectJson.GetProperty("status").GetString()
            .Should().Be("Rejected");
    }

    [Fact]
    public async Task GetNotifications_AuthenticatedUser_ReturnsOk()
    {
        var (token, _, _) = await RegisterAndLogin();
        using var client  = CreateClient(token);

        var response = await client.GetAsync(
            "http://localhost:5004/api/notifications");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetUnreadCount_AuthenticatedUser_ReturnsCount()
    {
        var (token, _, _) = await RegisterAndLogin();
        using var client  = CreateClient(token);

        var response = await client.GetAsync(
            "http://localhost:5004/api/notifications/unread-count");

        response.IsSuccessStatusCode.Should().BeTrue();
        var json = await ParseResponse(response);
        json.TryGetProperty("unreadCount", out _)
            .Should().BeTrue();
    }

    private static HttpClient CreateClient(string token)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<(string token, string userId, string email)>
        RegisterAndLogin()
    {
        var uniqueId  = Guid.NewGuid().ToString("N").Substring(0, 8);
        var subdomain = $"test-{uniqueId}";
        var email     = $"admin@{subdomain}.com";

        using var authClient = new HttpClient();

        var token = await TestAuthHelper.RegisterAndLoginAsync(
            authClient,
            tenantName: $"Tenant{uniqueId}",
            email:      email);

        var userId = ExtractUserIdFromToken(token);

        return (token, userId, email);
    }

    private static string ExtractUserIdFromToken(string token)
    {
        try
        {
            var parts   = token.Split('.');
            var payload = parts[1];

            // Fix base64 padding manually
            var remainder = payload.Length % 4;
            if (remainder == 2) payload += "==";
            else if (remainder == 3) payload += "=";

            var bytes   = Convert.FromBase64String(payload);
            var json    = Encoding.UTF8.GetString(bytes);
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            if (element.TryGetProperty("sub", out var sub))
                return sub.GetString() ?? Guid.NewGuid().ToString();

            return Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private static object BuildWorkflowPayload(
        string userId,
        int stageCount)
    {
        var stages = Enumerable.Range(1, stageCount)
            .Select(i => new
            {
                stageOrder       = i,
                stageName        = $"Stage {i}",
                assignedToUserId = userId,
                assignedToEmail  = "approver@company.com",
                slaDays          = 2
            }).ToList();

        return new
        {
            documentId           = Guid.NewGuid(),
            documentTitle        = "Test Document",
            initiatedByUserId    = userId,
            workflowDefinitionId = Guid.NewGuid(),
            tenantId             = Guid.NewGuid(),
            stageAssignments     = stages
        };
    }

    private static async Task<JsonElement> ParseResponse(
        HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return JsonSerializer.Deserialize<JsonElement>(
                "{}", JsonOptions);

        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            return JsonSerializer.Deserialize<JsonElement>(
                "{}", JsonOptions);

        return JsonSerializer.Deserialize<JsonElement>(
            content, JsonOptions);
    }
}
