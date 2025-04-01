using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using static Demo.Process.GenerateDocumentationStep;

namespace Demo.Process
{
    /// <summary>
    /// A process step to gather information about a product
    /// </summary>
    #pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class GatherProductInfoStep : KernelProcessStep
    {
        /// <summary>
        /// Gathers detailed information about a specified product
        /// </summary>
        /// <param name="productName">The name of the product to gather information for</param>
        /// <returns>Detailed product information as a formatted string</returns>
        [KernelFunction]
        public string GatherProductInformation(string productName)
        {
            Console.WriteLine($"{nameof(GatherProductInfoStep)}:\n\tGathering product information for product named {productName}");

            return
                """
                Product Description:
                Central Perk is New York City's coziest and most iconic coffee house, located in Manhattan's Greenwich Village. Famous for its oversized coffee cups, comfortable orange couch, and warm, inviting atmosphere that makes everyone feel like a regular.

                Product Features:
                1. **Signature Coffee Experience**: Premium coffee served in distinctive oversized mugs, featuring our house blend and daily specialty roasts.
                2. **Live Entertainment Space**: Regular performance area featuring local musicians and our legendary resident performer, Phoebe Buffay.
                3. **Community Gathering Spot**: Comfortable seating arrangements centered around our famous orange couch, perfect for friends meeting up.

                Services & Amenities:
                - Fresh-baked pastries and light snacks
                - Free Wi-Fi for customers
                - Regular open mic nights
                - Outdoor seating (seasonal)

                Opening Hours:
                Monday-Sunday: 7:00 AM - 11:00 PM
                """;
        }
    }

    /// <summary>
    /// A process step to generate documentation for a product
    /// </summary>
    public class GenerateDocumentationStep : KernelProcessStep<GeneratedDocumentationState>
    {
        private GeneratedDocumentationState _state = new();
        private readonly string _systemPrompt =
                """
                Your job is to write high quality and engaging customer facing documentation for a new product from Contoso. You will be provide with information
                about the product in the form of internal documentation, specs, and troubleshooting guides and you must use this information and
                nothing else to generate the documentation. If suggestions are provided on the documentation you create, take the suggestions into account and
                rewrite the documentation. Make sure the product sounds amazing.
                """;

        /// <summary>
        /// Called by the process runtime when the step instance is activated
        /// </summary>
        /// <param name="state">The kernel process step state</param>
        public override ValueTask ActivateAsync(KernelProcessStepState<GeneratedDocumentationState> state)
        {
            this._state = state.State!;
            this._state.ChatHistory ??= new ChatHistory(this._systemPrompt);

            return base.ActivateAsync(state);
        }

        /// <summary>
        /// Generates documentation based on provided product information
        /// </summary>
        [KernelFunction]
        public async Task GenerateDocumentationAsync(Kernel kernel, KernelProcessStepContext context, string productInfo)
        {
            Console.WriteLine($"{nameof(GenerateDocumentationStep)}:\n\tGenerating documentation for provided productInfo...");

            this._state.ChatHistory!.AddUserMessage($"Product Info:\n\n{productInfo}");

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var generatedDocumentationResponse = await chatCompletionService.GetChatMessageContentAsync(this._state.ChatHistory!).ConfigureAwait(false);

            await context.EmitEventAsync("DocumentationGenerated", generatedDocumentationResponse.Content!).ConfigureAwait(false);
        }

        /// <summary>
        /// State class for storing generated documentation information
        /// </summary>
        public class GeneratedDocumentationState
        {
            /// <summary>
            /// Chat history containing the documentation generation conversation
            /// </summary>
            public ChatHistory? ChatHistory { get; set; }
        }
    }

    /// <summary>
    /// A process step to publish documentation
    /// </summary>
    public class PublishDocumentationStep : KernelProcessStep
    {
        /// <summary>
        /// Publishes the generated documentation
        /// </summary>
        /// <param name="docs">The documentation to publish</param>
        [KernelFunction]
        public void PublishDocumentation(string docs)
        {
            Console.WriteLine($"{nameof(PublishDocumentationStep)}:\n\tPublishing product documentation:\n\n{docs}");
        }
    }

    #pragma warning restore SKEXP0080
}
