using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Demo9.Agents.WareHouseAgent;

public class CurrentTimePlugin
{
    [KernelFunction("current_time")]
    [Description("Get the current date and time in ISO 8601 format.")]
    public string GetCurrentTime()
    {
        return DateTime.UtcNow.ToString("O");
    }
}