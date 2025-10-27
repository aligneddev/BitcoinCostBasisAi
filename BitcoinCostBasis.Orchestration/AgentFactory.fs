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
        // System-level guardrails prepended to every agent's instructions.
        let systemGuardrail =
            "SYSTEM GUARDRAILS:\n- Do not invent facts. If you are unsure, ask for sources or clarifying information.\n- State uncertainty explicitly and avoid fabricating legal or tax advice.\n- Prefer asking clarifying questions over guessing an answer.\n- When returning factual claims, if possible request or cite the source."

        member private this.MergeInstructions (instructions: string) =
            sprintf "%s\n\n%s" systemGuardrail instructions

        member this.CreateAzureOpenAiAgent (agentName:string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName)
            agent

        /// Create an agent using a specific deployment (useful to choose cheaper/stronger models per task).
        member this.CreateAzureOpenAiAgentWithDeployment (agentName:string) (instructions: string) (deploymentName: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(deploymentName)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName)
            agent

        /// Create an agent and include modeled settings guidance (temperature, stop sequences) in the instructions.
        /// Note: the Azure/Microsoft agent SDK may provide native parameters for temperature/stop tokens; if available prefer using SDK overloads.
        member this.CreateAzureOpenAiAgentWithDeploymentAndSettings (agentName:string) (instructions: string) (deploymentName: string) (temperature: float) (stopSequences: string[]) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(deploymentName)
            let settingsText = sprintf "Model settings: temperature=%.2f. Stop sequences: %s" temperature (String.Join(", ", stopSequences))
            let agentInstr = this.MergeInstructions(settingsText + "\n\n" + instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, agentName)
            agent

        member this.CreateAzureOpenAiOrchestrationAgent (orchestrationAgentName:string) (instructions: string) =
            let client: AzureOpenAIClient = this.CreateAzureOpenAiClient()
            let chatClient: ChatClient = client.GetChatClient(configuration.ModelDeploymentName)
            let agentInstr = this.MergeInstructions(instructions)
            let agent: AIAgent = chatClient.CreateAIAgent(agentInstr, orchestrationAgentName)
            agent

        member this.CreateLocalOrchestrationAgent ollamaBaseUri ollamaModelName orchestrationAgentName =
            let instructions = "You are an orchestration agent that users interact with and coordinates multiple specialized agents to accomplish proper Bitcoin Cost Analysis and reporting. You will determine the intent of the question or direction and hand it off to the other agents."
            
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
