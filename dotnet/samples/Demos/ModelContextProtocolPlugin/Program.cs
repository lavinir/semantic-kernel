// Copyright (c) Microsoft. All rights reserved.

using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AzureOpenAI:Endpoint"] = "https://gaea-ai.openai.azure.com/",
        ["AzureOpenAI:DeploymentName"] = "gpt-4o"
    })
    .Build();

// Check for required Azure OpenAI settings
if (string.IsNullOrEmpty(config["AzureOpenAI:Endpoint"]))
{
    Console.Error.WriteLine("Please provide a valid AzureOpenAI:Endpoint to run this sample. See the associated README.md for more details.");
    return;
}

if (string.IsNullOrEmpty(config["AzureOpenAI:DeploymentName"]))
{
    Console.Error.WriteLine("Please provide a valid AzureOpenAI:DeploymentName to run this sample. See the associated README.md for more details.");
    return;
}

// Create an MCPClient for the GitHub server
// await using var mcpClient = await McpClientFactory.CreateAsync(
//     new()
//     {
//         Id = "github",
//         Name = "GitHub",
//         TransportType = "stdio",
//         TransportOptions = new Dictionary<string, string>
//         {
//             ["command"] = "npx",
//             ["arguments"] = "-y @modelcontextprotocol/server-github",
//         }
//     },
//     new() { ClientInfo = new() { Name = "GitHub", Version = "1.0.0" } }).ConfigureAwait(false);

await using var mcpClient = await McpClientFactory.CreateAsync(
    new()
    {
        Id = "spotify",
        Name = "Spotify",
        TransportType = "stdio",
        TransportOptions = new Dictionary<string, string>
        {
            ["command"] = "uv",
            ["arguments"] = "--directory C:\\repos\\spotify-mcp run spotify-mcp",
            ["env:SPOTIFY_CLIENT_ID"] = "b248377ebd604abe81763675dc8ebdf7",
            ["env:SPOTIFY_CLIENT_SECRET"] = "8bb7f23175cf44099df88ea9233fd3b7",
            ["env:SPOTIFY_REDIRECT_URI"] = "http://localhost:8888"
        }
    },
    new() { ClientInfo = new() { Name = "Spotify", Version = "1.0.0" } }
).ConfigureAwait(false);

// Retrieve the list of tools available on the GitHub server
var tools = await mcpClient.GetAIFunctionsAsync().ConfigureAwait(false);
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

// Prepare and build kernel with the MCP tools as Kernel functions
var builder = Kernel.CreateBuilder();
builder.Services
    .AddLogging(c => c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace))
    .AddAzureOpenAIChatCompletion(
        serviceId: "azureopenai",
        deploymentName: config["AzureOpenAI:DeploymentName"],
        endpoint: config["AzureOpenAI:Endpoint"],
        credentials: new DefaultAzureCredential());
Kernel kernel = builder.Build();
kernel.Plugins.AddFromFunctions("GitHub", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

// Enable automatic function calling
OpenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
};

// Test using GitHub tools
var prompt = "Pause the music";
var result = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);
Console.WriteLine($"\n\n{prompt}\n{result}");
