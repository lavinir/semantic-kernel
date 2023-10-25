// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

/// <summary>
/// Represents a function parameter that can be pass to the OpenAI API
/// </summary>
public class OpenAIFunctionParameter
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of the parameter.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the parameter is required or not.
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// An optional set of closed values indicating the valid options for this parameter.
    /// </summary>
    public IEnumerable<object>? Enum { get; set; } = null;
}

/// <summary>
/// Represents a function that can be pass to the OpenAI API
/// </summary>
public class OpenAIFunction
{
    /// <summary>
    /// Separator between the plugin name and the function name
    /// </summary>
    public const string NameSeparator = "-";

    /// <summary>
    /// Name of the function
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the function's associated plugin, if applicable
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the function. This is the concatenation of the plugin name and the function name,
    /// separated by the value of <see cref="NameSeparator"/>.
    /// If there is no plugin name, this is the same as the function name.
    /// </summary>
    public string FullyQualifiedName => BuildFullyQualifiedName(this.PluginName, this.FunctionName);

    /// <summary>
    /// Description of the function
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of parameters for the function
    /// </summary>
    public IList<OpenAIFunctionParameter> Parameters { get; set; } = new List<OpenAIFunctionParameter>();

    /// <summary>
    /// Builds the Fully qualified name of a function given a function name and the name of the plugin it belongs to.
    /// </summary>
    /// <param name="pluginName">The plugin name</param>
    /// <param name="functionName">The function name</param>
    /// <returns></returns>
    public static string BuildFullyQualifiedName(string pluginName, string functionName)
    {
        return pluginName.IsNullOrEmpty() ? functionName : string.Join(NameSeparator, pluginName, functionName);
    }

    /// <summary>
    /// Converts the <see cref="OpenAIFunction"/> to OpenAI's <see cref="FunctionDefinition"/>.
    /// </summary>
    /// <returns>A <see cref="FunctionDefinition"/> containing all the function information.</returns>
    public FunctionDefinition ToFunctionDefinition()
    {
        var requiredParams = new List<string>();

        var paramProperties = new Dictionary<string, object>();
        foreach (var param in this.Parameters)
        {
            paramProperties.Add(
                param.Name,
                new
                {
                    type = param.Type,
                    description = param.Description,
                    @enum = param.Enum,
                });

            if (param.IsRequired)
            {
                requiredParams.Add(param.Name);
            }
        }
        return new FunctionDefinition
        {
            Name = this.FullyQualifiedName,
            Description = this.Description,
            Parameters = BinaryData.FromObjectAsJson(
            new
            {
                type = "object",
                properties = paramProperties,
                required = requiredParams,
            }, s_options),
        };
    }

    #region private ================================================================================

    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion
}
