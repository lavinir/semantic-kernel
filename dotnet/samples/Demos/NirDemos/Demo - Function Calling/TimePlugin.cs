// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace DemoFunctionCalling;

/// <summary>
/// Time-related functions
/// </summary>
public class TimePlugin
{
    /// <summary>
    /// Get the current time
    /// </summary>
    /// <returns>Current time in HH:mm:ss format</returns>
    [KernelFunction, Description("Get the current time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }

    /// <summary>
    /// Get the current date
    /// </summary>
    /// <returns>Current date in yyyy-MM-dd format</returns>
    [KernelFunction, Description("Get the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Get the current time in a specified time zone
    /// </summary>
    /// <param name="timeZoneId">The time zone ID (e.g., "UTC", "America/New_York")</param>
    /// <returns>Current time in the specified time zone in HH:mm:ss format</returns>
    [KernelFunction, Description("Get the current time in a specified time zone")]
    public string GetTimeInTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            DateTime utcNow = DateTime.UtcNow;
            DateTime timeInZone = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            return timeInZone.ToString("HH:mm:ss");
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Invalid time zone: {timeZoneId}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
