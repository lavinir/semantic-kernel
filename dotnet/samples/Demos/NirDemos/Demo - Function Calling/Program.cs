// Copyright (c) Microsoft. All rights reserved.

using Azure.Identity;
using DemoFunctionCalling;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

static KernelFunction GetFunctionFromMethod<T1, T2, TResult>(
    Func<T1, T2, TResult> function,
    string functionName,
    string description)
{
    return KernelFunctionFactory.CreateFromMethod(function, functionName, description);
}

static int Add(int a, int b) => a + b;
static int Subtract(int a, int b) => a - b;
static int Multiply(int a, int b) => a * b;


var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AzureOpenAI:Endpoint"] = "https://gaea-ai.openai.azure.com/",
        ["AzureOpenAI:DeploymentName"] = "gpt-4o"
    })
    .Build();


// Create a kernel builder
var builder = Kernel.CreateBuilder();

// Add Azure OpenAI service
builder.AddAzureOpenAIChatCompletion(
        serviceId: "azureopenai",
        deploymentName: config["AzureOpenAI:DeploymentName"],
        endpoint: config["AzureOpenAI:Endpoint"],
        credentials: new DefaultAzureCredential());

// Build the kernel
var kernel = builder.Build();

// Register a custom plugin with functions that can be called
kernel.Plugins.AddFromFunctions("MathPlugin", new[]
{
    GetFunctionFromMethod<int, int, int>(Add, "Add", "Add two numbers"),
    GetFunctionFromMethod<int, int, int>(Subtract, "Subtract", "Subtract the second number from the first"),
    GetFunctionFromMethod<int, int, int>(Multiply, "Multiply", "Multiply two numbers")
});

// Register the TimePlugin
kernel.Plugins.AddFromType<TimePlugin>("TimePlugin");


// Execute the demos
await RunFunctionCallingDemoAsync(kernel).ConfigureAwait(false);
await RunTimePluginDemoAsync(kernel).ConfigureAwait(false);

// Main demo function
static async Task RunFunctionCallingDemoAsync(Kernel kernel)
{
    try
    {
        Console.WriteLine("\nAsking the AI to perform math operations using our functions...");

        // Examples of prompts that should trigger function calling
        var prompts = new[]
        {
            "What is 25 plus 8?",
            "Calculate 13 times 19",
            "What's the difference between 100 and 37?"
        };

        foreach (var prompt in prompts)
        {
            Console.WriteLine($"\nPrompt: {prompt}");

            // Create the execution settings with function calling enabled
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Invoke the model with the prompt
            var result = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);

            Console.WriteLine($"Result: {result.GetValue<string>()}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Time plugin demo function
static async Task RunTimePluginDemoAsync(Kernel kernel)
{
    try
    {
        Console.WriteLine("\nAsking the AI to retrieve time and date information...");

        // Examples of prompts that should trigger TimePlugin functions
        var prompts = new[]
        {
            "What time is it now?",
            "What's today's date?",
            "Can you tell me the current time and date?",
            "What is the time in London?"
        };

        foreach (var prompt in prompts)
        {
            Console.WriteLine($"\nPrompt: {prompt}");

            // Create the execution settings with function calling enabled
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Invoke the model with the prompt
            var result = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);

            Console.WriteLine($"Result: {result.GetValue<string>()}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
