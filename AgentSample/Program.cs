using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

//await AzureAIChat();

static async Task AzureAIChat()
{
    var endpoint = "https://kl-demo-hub-resource.openai.azure.com/";
    var deploymentName = "gpt-4.1-mini";

    const string JokerName = "Joker Dad";
    const string JokerInstructions = "You are good at telling jokes.";

    var agent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
        .GetChatClient(deploymentName)
        .CreateAIAgent(JokerInstructions, JokerName);
    await CallTheAgent(agent);
}

await LocalLlmChat();

static async Task LocalLlmChat()
{
    var baseUri = new Uri("http://localhost:11434/");
    Console.WriteLine($"Probing {baseUri} ...");

    // Quick HTTP probe to detect reachability and port issues.
    using var probeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    try
    {
        var probeResp = await probeClient.GetAsync(baseUri);
        Console.WriteLine($"Probe response: {(int)probeResp.StatusCode} {probeResp.ReasonPhrase}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HTTP probe failed: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine(
            "Check podman port mapping and container logs (podman ps, podman port, podman logs)."
        );
        return;
    }
    // https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/chat-client-agent?pivots=programming-language-csharp
    // podman run -d --name ollama --replace --restart=always -p 11434:11434 -v ollama:/root/.ollama docker.io/ollama/ollama
    // podman exec -it ollama ollama pull llama3.2:3b
    using OllamaApiClient chatClient = new(baseUri, "llama3.2:3b");
    //var response = await chatClient.GetResponseAsync("Tell me a joke.");
    var response = await chatClient
        .GetResponseAsync("Tell me a joke.")
        .WaitAsync(TimeSpan.FromSeconds(300));

    Console.WriteLine(response);
    //var agent = new ChatClientAgent(
    //            chatClient,
    //            instructions: "You are good at telling Dad jokes for kids.",
    //            name: "Joker");
    //await CallTheAgent(agent);
}

static async Task CallTheAgent(AIAgent agent)
{
    // Invoke the agent and output the text result.
    //Console.WriteLine(await agent.RunAsync("Tell me a joke."));

    // Invoke the agent with streaming support.
    await foreach (var update in agent.RunStreamingAsync("Tell me a joke."))
    {
        Console.WriteLine(update);
    }
}

Console.ReadLine();
