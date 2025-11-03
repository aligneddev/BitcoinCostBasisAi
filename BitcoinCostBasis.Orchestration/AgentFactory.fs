namespace BitcoinCostBasis

open Azure.AI.OpenAI
open OllamaSharp
open OpenAI
open System
open OpenAI.Chat
open Microsoft.Agents.AI
open Azure.Identity
open Microsoft.Extensions.AI
open ModelContextProtocol.Client
open ModelContextProtocol.Protocol

module Entry =

    type AzureConfiguration =
        { AzureOpenAiEndpoint: string
          AzureOpenAiKey: string
          ModelDeploymentName: string}

    type AgentFactory(configuration) =
        // System-level guardrails prepended to every agent's instructions.
        let systemGuardrail =
            "SYSTEM GUARDRAILS:\n- Do not invent facts. If you are unsure, ask for sources or clarifying information.\n- State uncertainty explicitly and avoid fabricating legal or tax advice.\n- Prefer asking clarifying questions over guessing an answer.\n- When returning factual claims, if possible request or cite the source."

        member private this.MergeInstructions (instructions: string) =
            sprintf "%s\n\n%s" systemGuardrail instructions

        member this.CreateAzureOpenAiAgentWithTools (agentName: string) (description: string) (instructions: string) (tools: AITool[]) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName, description, tools)
            agent

        member this.CreateAzureOpenAiAgent (agentName: string) (description: string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agentInstr = this.MergeInstructions instructions
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName, description)
            agent

        ///// Create an agent using a specific deployment (useful to choose cheaper/stronger models per task).
        //member this.CreateAzureOpenAiAgentWithDeployment (agentName:string) (instructions: string) (deploymentName: string) =
        //    let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
        //    let chatClient: ChatClient = client.GetChatClient(deploymentName)
        //    let agentInstr = this.MergeInstructions(instructions)
        //    let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName)
        //    agent

        ///// Create an agent and include modeled settings guidance (temperature, stop sequences) in the instructions.
        ///// Note: the Azure/Microsoft agent SDK may provide native parameters for temperature/stop tokens; if available prefer using SDK overloads.
        //member this.CreateAzureOpenAiAgentWithDeploymentAndSettings (agentName:string) (instructions: string) (deploymentName: string) (temperature: float) (stopSequences: string[]) =
        //    let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
        //    let chatClient: ChatClient = client.GetChatClient(deploymentName)
        //    let settingsText = sprintf "Model settings: temperature=%.2f. Stop sequences: %s" temperature (String.Join(", ", stopSequences))
        //    let agentInstr = this.MergeInstructions(settingsText + "\n\n" + instructions)
        //    let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName)
        //    agent

        member this.CreateAzureOpenAiOrchestrationAgent (orchestrationAgentName:string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, orchestrationAgentName)
            agent

        member this.CreateLocalOrchestrationAgent ollamaBaseUri ollamaModelName orchestrationAgentName instructions =
            let agentInstr = this.MergeInstructions(instructions)
            this.CreateLocalOrchestrationAgentollamaBaseUri ollamaBaseUri ollamaModelName orchestrationAgentName agentInstr

        member private this.CreateLocalOrchestrationAgentollamaBaseUri ollamaBaseUri ollamaModelName orchestrationAgentName instructions =
            let baseUri = Uri(ollamaBaseUri)
            let client = new OllamaApiClient(baseUri, ollamaBaseUri)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = new ChatClientAgent(client, ollamaModelName, agentInstr, orchestrationAgentName)
            agent

        member private this.CreateAzureOpenAiClient() =
              new AzureOpenAIClient(new Uri(configuration.AzureOpenAiEndpoint), new AzureCliCredential())
              //production will need a key new ApiKeyCredential(configuration.AzureOpenAiKey))

        member this.createPodmanMcpClient name imageName =
        
            //await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
            //{
            //    Name = "BitcoinHistoricalPriceMcp",
            //    Command = "podman",
            //    Arguments = [ "run", "--rm", "-i", "bitcoin-historical-mcp" ]
            //}));
            let mutable mcpClientObj : obj = null
            try
                // Replace the inner options type name if different in your project (e.g. StdioClientTransportOptions).
                // This attempts to mirror the original intent in F#:
                let transport =
                    StdioClientTransport(
                        StdioClientTransportOptions(Name = name, Command = "podman", Arguments = [| "run"; "--rm"; "-i"; imageName |])
                    )

            // Create and return the McpClient synchronously.
                McpClient.CreateAsync(transport).GetAwaiter().GetResult()
            finally
                // If McpClient implements IAsyncDisposable, call DisposeAsync synchronously.
                match mcpClientObj with
                | null -> ()
                | client ->
                    match client with
                    | :? System.IAsyncDisposable as iad -> iad.DisposeAsync().AsTask().GetAwaiter().GetResult()
                    | :? IDisposable as d -> d.Dispose()
                    | _ -> ()
