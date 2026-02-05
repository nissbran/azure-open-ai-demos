using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Demo12;

public static class FunctionsTriggers
{
    public const string OrchestrationFunctionName = "AlarmAnalytisicsOrchestrator";

    [Function(OrchestrationFunctionName)]
    public static async Task<object> RunOrchestrationAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(OrchestrationFunctionName);
        // Get the prompt from the orchestration input
        string prompt = context.GetInput<string>() ?? throw new InvalidOperationException("Prompt is required");

        // Get both agents
        var analyticsAgent = context.GetAgent("AlarmAnalyticsAgent");
        var analyticsSession = await analyticsAgent.GetNewSessionAsync();

        logger.Log(LogLevel.Information, "Starting agent thread for prompt: {Prompt}", prompt);
        var analyticsResponse = await analyticsAgent.RunAsync(prompt, analyticsSession);

        var supplierAgent = context.GetAgent("SupplierAgent");
        var supplierSession = await supplierAgent.GetNewSessionAsync();

        logger.Log(LogLevel.Information, "Starting supplier agent thread for analytics response: {AnalyticsResponse}", analyticsResponse.Text);
        var supplierResponse = await supplierAgent.RunAsync(analyticsResponse.Messages.LastOrDefault()?.Text, supplierSession);

        // Fix after issues with serialization of the agent response object
        // var casePublishAgent = context.GetAgent("CasePublishAgent");
        // var casePublishSession = await casePublishAgent.GetNewSessionAsync();
        // logger.Log(LogLevel.Information, "Starting case publisher agent thread for supplier response: {SupplierResponse}", supplierResponse.Text);
        // var casePublisherResponse = await casePublishAgent.RunAsync(supplierResponse.Messages.LastOrDefault()?.Text, casePublishSession);

        // Human in the loop - wait for approval with 30 minute timeout
        logger.Log(LogLevel.Information, "Waiting for human approval to publish case...");

        var caseToBePublished = supplierResponse.Messages.LastOrDefault()?.Text;

        var timeout = TimeSpan.FromMinutes(30);

        context.SetCustomStatus($"Requesting human feedback before publishing the case. Timeout: {timeout:g}");

        await context.CallActivityAsync(nameof(NotifyUserForApproval), caseToBePublished);

        HumanApprovalResponse humanResponse;
        try
        {
            humanResponse = await context.WaitForExternalEvent<HumanApprovalResponse>(
                eventName: "HumanApproval",
                timeout: timeout);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred - treat as rejection
            context.SetCustomStatus($"Human approval timed out after {timeout:g}. The case will not be published.");
            throw new TimeoutException($"Human approval timed out after {timeout:g}. The case will not be published.");
        }

        string casePublishResponse;

        if (humanResponse.Approved)
        {
            context.SetCustomStatus("Case approved by human reviewer. Publishing...");
            
            var latestSupplierResponse = await supplierAgent.RunAsync("Give me the latest information as a publishable report.", supplierSession);
            
            casePublishResponse = await context.CallActivityAsync<string>(nameof(CasePublishAgentActivity), latestSupplierResponse.Messages.LastOrDefault().Text);

            context.SetCustomStatus($"Case published successfully at {context.CurrentUtcDateTime:s}: {casePublishResponse}");

            logger.Log(LogLevel.Information, "Human approved case publication");
        }
        else
        {
            context.SetCustomStatus("Case rejected by human reviewer. It will not be published.");
            casePublishResponse = "Case publishing rejected by human reviewer.";
        }

        return new
        {
            AnalyticsResult = analyticsResponse.Text,
            SupplierResult = supplierResponse.Text,
            CasePublishResult = casePublishResponse
        };
    }


    /// <summary>
    /// Represents the human approval response.
    /// </summary>
    public sealed class HumanApprovalResponse
    {
        [JsonPropertyName("approved")] public bool Approved { get; set; }

        [JsonPropertyName("feedback")] public string Feedback { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workaround for MCP Serialization issue on tool calling
    /// https://github.com/microsoft/agent-framework/issues/3281
    /// </summary>
    /// <returns></returns>
    [Function(nameof(CasePublishAgentActivity))]
    public static async Task<string> CasePublishAgentActivity([ActivityTrigger] string prompt,
        FunctionContext executionContext)
    {
        var agent = Agents.CreateCasePublisherAgent(executionContext.InstanceServices);

        var session = await agent.GetNewSessionAsync();
        var response = await agent.RunAsync(prompt, session);
        return response.Text;
    }

    // POST /alarm-analytics/run
    [Function(nameof(StartOrchestrationAsync))]
    public static async Task<IActionResult> StartOrchestrationAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "alarm-analytics/run")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var prompt = await File.ReadAllTextAsync("alarm1.json");

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            orchestratorName: OrchestrationFunctionName,
            input: prompt);

        return new AcceptedResult($"/api/alarm-analytics/{instanceId}/status", new
        {
            message = "Multi-agent concurrent orchestration started.",
            instanceId,
        });
    }

    [Function(nameof(NotifyUserForApproval))]
    public static async Task NotifyUserForApproval([ActivityTrigger] string content, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("NotifyUserForApproval");
        // Simulate sending notification to user
        logger.LogInformation("Notifying user for approval of content: {Content}", content);
        await Task.CompletedTask;
    }

    // POST /alarm-analytics/{instanceId}/approve
    [Function(nameof(ApproveCaseAsync))]
    public static async Task<IActionResult> ApproveCaseAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "alarm-analytics/{instanceId}/approve")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("ApproveCaseAsync");

        logger.LogInformation("Received approval request for instance {InstanceId}", instanceId);
        
        await client.RaiseEventAsync(instanceId, "HumanApproval", new HumanApprovalResponse
        {
            Approved = true,
            Feedback = "Approved by user."
        });

        return new OkObjectResult(new
        {
            message = "Case approved.",
            instanceId,
        });
    }

    // POST /alarm-analytics/{instanceId}/reject
    [Function(nameof(RejectCaseAsync))]
    public static async Task<IActionResult> RejectCaseAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "alarm-analytics/{instanceId}/reject")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RejectCaseAsync");

        logger.LogInformation("Received rejection request for instance {InstanceId}", instanceId);
        
        await client.RaiseEventAsync(instanceId, "HumanApproval", new HumanApprovalResponse
        {
            Approved = false,
            Feedback = "Rejected by user."
        });

        return new OkObjectResult(new
        {
            message = "Case rejected.",
            instanceId,
        });
    }

    // GET /alarm-analytics/{instanceId}/status
    [Function(nameof(GetOrchestrationStatusAsync))]
    public static async Task<IActionResult> GetOrchestrationStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alarm-analytics/{instanceId}/status")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        OrchestrationMetadata? status = await client.GetInstanceAsync(
            instanceId,
            getInputsAndOutputs: true,
            req.FunctionContext.CancellationToken);

        if (status is null)
        {
            return new NotFoundObjectResult(new { error = "Instance not found" });
        }

        return new OkObjectResult(new
        {
            instanceId = status.InstanceId,
            runtimeStatus = status.RuntimeStatus.ToString(),
            input = status.SerializedInput is not null ? (object)status.ReadInputAs<JsonElement>() : null,
            output = status.SerializedOutput is not null ? (object)status.ReadOutputAs<JsonElement>() : null,
            failureDetails = status.FailureDetails
        });
    }
}