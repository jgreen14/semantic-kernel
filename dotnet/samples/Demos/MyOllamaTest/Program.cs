// Copyright (c) Microsoft. All rights reserved.
#pragma warning disable VSTHRD111 // Use ConfigureAwait(bool)
#pragma warning disable CA1050 // Declare types in namespaces
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable SKEXP0070

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build()
    ?? throw new InvalidOperationException("Configuration is not provided.");

ArgumentNullException.ThrowIfNull(config["Ollama:ChatModelId"], "Ollama:ChatModelId");
ArgumentNullException.ThrowIfNull(config["Ollama:BaseUri"], "Ollama:BaseUri");

var kernelBuilder = Kernel.CreateBuilder().AddOllamaChatCompletion(
    modelId: config["Ollama:ChatModelId"]!,
    baseUri: new Uri(config["Ollama:BaseUri"]!));

var kernel = kernelBuilder.Build();

// Get chat completion service
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

//Console.WriteLine("Ask questions to use the Time Plugin such as:\n" +
//                  "- What time is it?");

ChatHistory chatHistory = [];
string? input = null;
while (true)
{
    Console.Write("\nUser > ");
    input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        // Leaves if the user hit enter without typing any word
        break;
    }
    chatHistory.AddUserMessage(input);

    // Get the response from the AI
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        chatHistory,
        null,
        kernel: kernel);

    // Stream the results
    string fullMessage = "";
    var first = true;
    await foreach (var content in result.ConfigureAwait(false))
    {
        if (content.Role.HasValue && first)
        {
            Console.Write("Assistant > ");
            first = false;
        }
        Console.Write(content.Content);
        fullMessage += content.Content;
    }
    Console.WriteLine();
}
