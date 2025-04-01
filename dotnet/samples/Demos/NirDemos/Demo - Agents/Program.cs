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

// Define colors for each participant
var participantColors = new Dictionary<string, ConsoleColor>
{
    { "Nicole", ConsoleColor.Magenta },
    { "John", ConsoleColor.Blue },
    { "Ariana", ConsoleColor.Green },
};

ChatCompletionAgent groomAgent =
    new()
    {
        Name = "John",
        Instructions = "You are John, the groom. Your main priorities are ensuring your best buddies from college have their own table, getting a rock band for the wedding, and making sure you sit as far away as possible from Nicole's mom. Express enthusiasm for these ideas and try to steer conversations in their favor.",
        Kernel = kernel,
    };

ChatCompletionAgent brideAgent =
    new()
    {
        Name = "Nicole",
        Instructions = "You are Nicole, the bride. Your main priorities are having the entire family sit together at one table, hiring a violin player for the wedding, and limiting John to inviting at most one of his buddies from college. You feel strongly about these choices and try to guide the conversation to align with them.",
        Kernel = kernel,
    };

ChatCompletionAgent plannerAgent =
    new()
    {
        Name = "Ariana",
        Instructions = "You are Ariana, an experienced wedding planner. You know how to mediate between couples and find creative compromises. You remain impartial, listen to both sides, and ensure that decisions lead to a solution acceptable to all parties. Your goal is to create a wedding that balances John and Nicole's preferences while keeping things stress-free.",
        Kernel = kernel,
    };

KernelFunction selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Determine which participant takes the next turn in a conversation based on the the most recent participant.
        State only the name of the participant to take the next turn.
        No participant should take more than one turn in a row.

        Choose only from these participants:
        - {{{groomAgent.Name}}}
        - {{{brideAgent.Name}}}       
        - {{{plannerAgent.Name}}}       
        
        Always follow these rules when selecting the next participant:
        - After {{{brideAgent.Name}}}, it is {{{groomAgent.Name}}}'s turn.
        - After {{{groomAgent.Name}}}, it is {{{plannerAgent.Name}}}'s turn.
        - After {{{plannerAgent.Name}}}, it is {{{brideAgent.Name}}}'s turn.
                
        History:
        {{$history}}
        """,
        safeParameterNames: "history");


KernelFunctionSelectionStrategy selectionStrategy =
  new(selectionFunction, kernel)
  {
      // Always start with the PM agent.
      InitialAgent = brideAgent,
      // Parse the function response.
      ResultParser = (result) => result.GetValue<string>() ?? plannerAgent.Name,
      // The prompt variable name for the history argument.
      HistoryVariableName = "history",
      // Save tokens by not including the entire history in the prompt
      //HistoryReducer = new ChatHistoryTruncationReducer(3),
  };

KernelFunction terminationFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Determine if the conversation should continue or stop.
        State only "continue" or "stop".
        Always follow these rules when determining if the conversation should continue or stop:
        - The conversation should stop if both Nicole and John have accepted the proposal explicitly.
        History:
        {{$history}}
        """,
        safeParameterNames: "history");

KernelFunctionTerminationStrategy terminationStrategy =
    new(terminationFunction, kernel)
    {
        ResultParser = (result) => result.GetValue<string>().Equals("stop", StringComparison.OrdinalIgnoreCase) ? true : false,
        HistoryVariableName = "history",
    };


AgentGroupChat chat = new(groomAgent, brideAgent, plannerAgent)
{
    ExecutionSettings = new()
    {
        TerminationStrategy = terminationStrategy,
        SelectionStrategy = selectionStrategy
    }
};

chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Please express your requests for the wedding, keep your responses short and to the point."));

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates

await foreach (ChatMessageContent response in chat.InvokeAsync().ConfigureAwait(false))
{
    // Set color based on participant name
    string authorName = response.AuthorName ?? "User";

    if (participantColors.TryGetValue(authorName, out ConsoleColor color))
    {
        Console.ForegroundColor = color;
    }
    else
    {
        Console.ResetColor();
    }

    Console.WriteLine($"{authorName}:\n {response.Content}\n");

    // Reset console color after printing
    Console.ResetColor();
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates
