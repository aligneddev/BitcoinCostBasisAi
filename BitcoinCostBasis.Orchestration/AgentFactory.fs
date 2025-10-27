namespace BitcoinCostBasis

open Azure.AI.OpenAI
open OllamaSharp
open OpenAI
open System
open OpenAI.Chat
open Microsoft.Agents.AI
open Azure.Identity

module Entry =

    type AzureConfiguration =
        { AzureOpenAiEndpoint: string
          AzureOpenAiKey: string
          ModelDeploymentName: string}

    type AgentFactory(configuration) =
        member this.CreateAzureOpenAiAgent (agentName:string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agent: AIAgent = chatClient.CreateAIAgent(instructions, agentName)
            agent

        member this.CreateAzureOpenAiOrchestrationAgent (orchestrationAgentName:string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agent: AIAgent = chatClient.CreateAIAgent(instructions, orchestrationAgentName)
            agent

        member this.CreateLocalOrchestrationAgent ollamaBaseUri ollamaModelName orchestrationAgentName =
            let instructions = "You are an orchestration agent that users interact with and coordinates multiple specialized agents to accomplish proper Bitcoin Cost Analysis and reporting. You will determine the intent of the question or direction and hand it off to the other agents."
            this.CreateLocalOrchestrationAgentollamaBaseUri ollamaBaseUri ollamaModelName orchestrationAgentName instructions

        member private this.CreateLocalOrchestrationAgentollamaBaseUri ollamaBaseUri ollamaModelName orchestrationAgentName instructions =
            let baseUri = Uri(ollamaBaseUri)
            let client = new OllamaApiClient(baseUri, ollamaBaseUri)
            let agent: AIAgent = new ChatClientAgent(client, ollamaModelName, instructions, orchestrationAgentName)
            agent

        member private this.CreateAzureOpenAiClient() =
              new AzureOpenAIClient(new Uri(configuration.AzureOpenAiEndpoint), new AzureCliCredential())
              //production will need a key new ApiKeyCredential(configuration.AzureOpenAiKey))
