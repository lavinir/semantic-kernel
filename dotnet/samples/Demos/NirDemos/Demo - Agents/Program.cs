using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var config = new ConfigurationBuilder()
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

ChatCompletionAgent devAgent =
    new()
    {
        Name = "Alex_the_Software_Engineer",
        Instructions = "You are a Software Engineer named Alex. Alex Loves clean, maintainable code and hates rushed deadlines.\n\nSpeaks in technical terms and prioritizes feasibility.\n\nSkeptical of vague requirements and marketing buzzwords.\n\nPrefers logical, incremental improvements over \"big ideas.\"",
        Kernel = kernel,
    };

ChatCompletionAgent pmAgent =
    new()
    {
        Name = "Jordan_the_Product_Manager",
        Instructions = "You are a Product Manager named Jordan. Jordan is excited about innovation and delivering features quickly.\n\nSpeaks in business impact, user experience, and \"MVP\" language.\n\nFocuses on deadlines, customer needs, and market trends.\n\nSometimes overlooks technical complexity in favor of speed.",
        Kernel = kernel,
    };

KernelFunction selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Determine which participant takes the next turn in a conversation based on the the most recent participant.
        State only the name of the participant to take the next turn.
        No participant should take more than one turn in a row.

        Choose only from these participants:
        - {{{devAgent.Name}}}
        - {{{pmAgent.Name}}}

        Always follow these rules when selecting the next participant:
        - After {{{devAgent.Name}}}, it is {{{pmAgent.Name}}}'s turn.
        - After {{{pmAgent.Name}}}, it is {{{devAgent.Name}}}'s turn.

        History:
        {{$history}}
        """,
        safeParameterNames: "history");

KernelFunctionSelectionStrategy selectionStrategy =
  new(selectionFunction, kernel)
  {
      // Always start with the PM agent.
      InitialAgent = pmAgent,
      // Parse the function response.
      ResultParser = (result) => result.GetValue<string>() ?? pmAgent.Name,
      // The prompt variable name for the history argument.
      HistoryVariableName = "history",
      // Save tokens by not including the entire history in the prompt
      //HistoryReducer = new ChatHistoryTruncationReducer(3),
  };
AgentGroupChat chat = new(devAgent, pmAgent)
{
    ExecutionSettings = new()
    {
        TerminationStrategy = { MaximumIterations = 10 },
        SelectionStrategy = selectionStrategy
    }
};

chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Create a simple program that teaches kids basic math problems"));

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates

await foreach (ChatMessageContent response in chat.InvokeAsync().ConfigureAwait(false))
{
    Console.WriteLine($"{response.AuthorName ?? string.Empty}: {response.Content}");
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates
