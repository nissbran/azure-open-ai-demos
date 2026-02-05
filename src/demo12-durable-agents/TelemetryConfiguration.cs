using System;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Demo12;

public static class TelemetryConfiguration
{

    public static FunctionsApplicationBuilder ConfigureTelemetry(this FunctionsApplicationBuilder builder)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults()
            .WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .AddSource("DurableTask.Core")
                    .AddSource("*Microsoft.Agents.AI") // Agent Framework telemetry
                    .AddSource("ChatClient", "AlarmAnalyticsAgent", "SupplierAgent", "SummaryAgent", "CasePublishAgent") // Our agents
                    .AddOtlpExporter();
                
                if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    tracingBuilder.AddOtlpExporter();
                }
                
                if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
                {
                    tracingBuilder.AddAzureMonitorTraceExporter();
                }
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddMeter("Microsoft.Agents.AI*") // Agent Framework metrics
                    .AddMeter("DurableTask.*")
                    .AddHttpClientInstrumentation();
            });
        
        return builder;
    }
}