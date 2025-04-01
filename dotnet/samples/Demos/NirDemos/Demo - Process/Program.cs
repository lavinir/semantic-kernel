// Create the process builder
using Azure.Identity;
using Demo.Process;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AzureOpenAI:Endpoint"] = "https://gaea-ai.openai.azure.com/",
        ["AzureOpenAI:DeploymentName"] = "gpt-4o"
    })
    .Build();

var builder = Kernel.CreateBuilder();
builder.Services
    .AddAzureOpenAIChatCompletion(
        serviceId: "azureopenai",
        deploymentName: config["AzureOpenAI:DeploymentName"],
        endpoint: config["AzureOpenAI:Endpoint"],
        credentials: new DefaultAzureCredential());
Kernel kernel = builder.Build();


#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
ProcessBuilder processBuilder = new("DocumentationGeneration");

// Add the steps
var infoGatheringStep = processBuilder.AddStepFromType<GatherProductInfoStep>();
var docsGenerationStep = processBuilder.AddStepFromType<GenerateDocumentationStep>();
var docsPublishStep = processBuilder.AddStepFromType<PublishDocumentationStep>();

// Orchestrate the events
processBuilder
    .OnInputEvent("Start")
    .SendEventTo(new(infoGatheringStep));

infoGatheringStep
    .OnFunctionResult()
    .SendEventTo(new(docsGenerationStep));

docsGenerationStep
    .OnEvent("DocumentationGenerated")
    .SendEventTo(new(docsPublishStep));

// Build and run the process
var process = processBuilder.Build();
await process.StartAsync(kernel, new KernelProcessEvent { Id = "Start", Data = "Central Perk" }).ConfigureAwait(false);
