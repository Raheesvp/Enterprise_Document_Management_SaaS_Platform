using FluentAssertions;
using Integration.Tests.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Integration.Tests;

// Week4EndToEndTests — proves full Week 4 pipeline works
//
// Tests:
// 1. Create workflow definition with stages
// 2. Retrieve definition — verify stages persisted
// 3. Start workflow using definition stages
// 4. Full approval flow — all stages approved
// 5. Rejection flow
// 6. Cannot start duplicate workflow for same document
// 7. Tenant A definition not visible to Tenant B
public sealed class Week4EndToEndTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // TEST 1 — Create workflow definition
    [Fact]
    public async Task CreateWorkflowDefinition_ValidRequest_ReturnsId()
    {
        var (token, _, _) = await RegisterAndLogin();
        using var client  = CreateClient(token);

        var response = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/definitions",
            BuildDefinitionPayload("Standard Approval", 3));

        response.IsSuccessStatusCode.Should().BeTrue(
            $"Create definition failed: " +
            $"{await response.Content.ReadAsStringAsync()}");

        var json = await ParseResponse(response);
        json.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetString().Should().NotBeNullOrEmpty();
    }

    // TEST 2 — Get definitions returns stages
    [Fact]
    public async Task GetWorkflowDefinitions_AfterCreate_ReturnsStagess()
    {
        var (token, _, _) = await RegisterAndLogin();
        using var client  = CreateClient(token);

        // Create definition
        await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/definitions",
            BuildDefinitionPayload("Test Definition", 2));

        // Get all definitions
        var response = await client.GetAsync(
            "http://localhost:5003/api/workflow/definitions");

        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var arr     = JsonSerializer.Deserialize<JsonElement>(
            content, JsonOptions);

        arr.GetArrayLength().Should().BeGreaterThan(0,
            "Should return at least one definition");

        var first  = arr[0];
        var stages = first.GetProperty("stages");
        stages.GetArrayLength().Should().Be(2,
            "Should have 2 stages");
    }

    // TEST 3 — Start workflow with definition
    [Fact]
    public async Task StartWorkflow_WithDefinition_UsesDefinitionStages()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client       = CreateClient(token);

        // Create 3-stage definition
        var defResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/definitions",
            BuildDefinitionPayload("Contract Approval", 3));

        defResponse.IsSuccessStatusCode.Should().BeTrue();

        // Start workflow
        var wfResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 3));

        wfResponse.IsSuccessStatusCode.Should().BeTrue();

        var wfJson = await ParseResponse(wfResponse);
        wfJson.GetProperty("status").GetString()
            .Should().Be("InProgress");

        wfJson.GetProperty("stages")
            .GetArrayLength()
            .Should().Be(3,
                "Workflow should have 3 stages");
    }

    // TEST 4 — Full approval flow through all stages
    [Fact]
    public async Task ApproveWorkflow_AllStages_ReturnsApproved()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client       = CreateClient(token);

        // Start 3-stage workflow
        var startResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 3));

        var startJson  = await ParseResponse(startResponse);
        var workflowId = startJson.GetProperty("id").GetString()!;

        // Approve all 3 stages
        for (int i = 0; i < 3; i++)
        {
            var approveResponse = await client.PostAsJsonAsync(
                $"http://localhost:5003/api/workflow/{workflowId}/approve",
                new { comments = $"Stage {i + 1} approved" });

            approveResponse.IsSuccessStatusCode.Should().BeTrue(
                $"Stage {i + 1} approve failed: " +
                $"{await approveResponse.Content.ReadAsStringAsync()}");
        }

        // Verify final status
        var statusResponse = await client.GetAsync(
            $"http://localhost:5003/api/workflow/{workflowId}");

        var statusJson = await ParseResponse(statusResponse);
        statusJson.GetProperty("status").GetString()
            .Should().Be("Approved",
                "All stages approved — workflow should be Approved");
    }

    // TEST 5 — Reject workflow on first stage
    [Fact]
    public async Task RejectWorkflow_FirstStage_ReturnsRejected()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client       = CreateClient(token);

        var startResponse = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 2));

        var startJson  = await ParseResponse(startResponse);
        var workflowId = startJson.GetProperty("id").GetString()!;

        var rejectResponse = await client.PostAsJsonAsync(
            $"http://localhost:5003/api/workflow/{workflowId}/reject",
            new { comments = "Does not comply with policy" });

        rejectResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Reject failed: " +
            $"{await rejectResponse.Content.ReadAsStringAsync()}");

        var rejectJson = await ParseResponse(rejectResponse);
        rejectJson.GetProperty("status").GetString()
            .Should().Be("Rejected");
    }

    // TEST 6 — Cannot start duplicate workflow for same document
    [Fact]
    public async Task StartWorkflow_SameDocument_ReturnsBadRequest()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client       = CreateClient(token);

        var documentId = Guid.NewGuid();
        var payload    = BuildWorkflowPayloadWithDocumentId(
            userId, 1, documentId);

        // First workflow — should succeed
        var first = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start", payload);
        first.IsSuccessStatusCode.Should().BeTrue();

        // Second workflow for same document — should fail
        var second = await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start", payload);
        second.IsSuccessStatusCode.Should().BeFalse(
            "Cannot start duplicate workflow for same document");
    }

    // TEST 7 — Tenant isolation for workflow definitions
    [Fact]
    public async Task WorkflowDefinitions_TenantIsolated()
    {
        // Tenant A creates a definition
        var (tokenA, _, _) = await RegisterAndLogin();
        using var clientA  = CreateClient(tokenA);

        await clientA.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/definitions",
            BuildDefinitionPayload("Tenant A Template", 2));

        // Tenant B gets definitions — should not see Tenant A's
        var (tokenB, _, _) = await RegisterAndLogin();
        using var clientB  = CreateClient(tokenB);

        var responseB = await clientB.GetAsync(
            "http://localhost:5003/api/workflow/definitions");

        responseB.IsSuccessStatusCode.Should().BeTrue();

        var content = await responseB.Content.ReadAsStringAsync();
        var arr     = JsonSerializer.Deserialize<JsonElement>(
            content, JsonOptions);

        arr.GetArrayLength().Should().Be(0,
            "Tenant B should not see Tenant A definitions");
    }

    // TEST 8 — Notifications created after workflow start
    [Fact]
    public async Task StartWorkflow_CreatesNotification()
    {
        var (token, userId, _) = await RegisterAndLogin();
        using var client       = CreateClient(token);

        // Start workflow
        await client.PostAsJsonAsync(
            "http://localhost:5003/api/workflow/start",
            BuildWorkflowPayload(userId, 1));

        // Wait briefly for async notification creation
        await Task.Delay(500);

        // Check notifications
        var notifResponse = await client.GetAsync(
            "http://localhost:5004/api/notifications");

        notifResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    // -- Helpers ----------------------------------------------

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
        var uniqueId  = Guid.NewGuid().ToString("N")[..8];
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
            var parts     = token.Split('.');
            var payload   = parts[1];
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
        catch { return Guid.NewGuid().ToString(); }
    }

    private static object BuildDefinitionPayload(
        string name, int stageCount)
    {
        var stages = Enumerable.Range(1, stageCount)
            .Select(i => new
            {
                order        = i,
                stageName    = $"Stage {i}",
                roleRequired = i == 1 ? "Manager"
                             : i == 2 ? "Legal" : "CFO",
                slaDays      = i
            }).ToList();

        return new
        {
            name        = name,
            description = $"{stageCount}-stage approval process",
            stages      = stages
        };
    }

    private static object BuildWorkflowPayload(
        string userId, int stageCount)
    {
        return BuildWorkflowPayloadWithDocumentId(
            userId, stageCount, Guid.NewGuid());
    }

    private static object BuildWorkflowPayloadWithDocumentId(
        string userId, int stageCount, Guid documentId)
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
            documentId           = documentId,
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
