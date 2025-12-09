namespace BitcoinCostBasis

// based on https://youtu.be/VInKZ45YKAM from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
open Microsoft.Agents.AI.Workflows
open System
open System.Threading
open BitcoinCostBasis.Orchestration.AgentWorkflow
open Entry
open Microsoft.Extensions.AI
open Microsoft.Agents.AI

module EntryProgramDemo =
    let configuration =
        { AzureOpenAiEndpoint = "https://kl-demo-hub-resource.openai.azure.com/"
          AzureOpenAiKey = "REPLACE_ME"
          ModelDeploymentName = "gpt-4.1-mini" }

    let build (cts: CancellationTokenSource) =
        let agentFactory = AgentFactory(configuration)
        let bitcoinHistoricalPriceMcpClient =
            agentFactory.createPodmanMcpClient "BitcoinHistoricalPriceMcp" "bitcoin-historical-mcp"

        let aiTools =
            task {
                let! tools = bitcoinHistoricalPriceMcpClient.ListToolsAsync(cancellationToken = cts.Token)
                return
                    tools
                    |> Seq.map (fun t -> t :> AITool)
                    |> Array.ofSeq
            }

        let orchestatorPrompt =
            "You are a helpful assistant. Determine what type of question was asked and handoff to the other agents. Keep track of the current part of the workflow and prompt for next actions. Never answer yourself, **always** pass to the other agents. If none of the agents match the criteria, say so, don't answer the question. "
            + "When you start up or if the users asks for what you can do, explain the different options and workflows you can help with. "
            + "There are several possible workflows: "
            + "The `record transactions` workflow is as follows that you will hand off to the bitcoinTransactionAccountantAgent:1) The user will ask to record transactions,2,"
            + "The `determine cost basis` workflow is as follows: "
            + "The `fill in the tax forms workflow` is as follows:1) The user will ask questions related to Bitcoin tax reporting, cost basis calculations, and relevant tax regulations to the BitcoinTaxSpecialistAgent or BitcoinTransactionAccountantAgent.2) Once the user is ready to fill out tax forms, they will instruct you to hand off to the FillTaxFormAgent.3) After the FillTaxFormAgent completes filling out the forms, the user may ask you to verify the filled forms, at which point you will hand off to the VerifyFilledTaxFormAgent.4) After verification, return control to the user for any further questions or actions. "
            + "The user may also ask questions which you should hand off to the appropriate agent."
            + "You can call the MCP tool HistoricalBitcoinDataMcp to fetch historical BTC prices given a date range"
            + "When handing off to another agent, provide a JSON object with the following format: { \"agent\": \"<AgentName>\", \"confidence\": <ConfidenceScore>, \"reason\": \"<ReasonForHandoff>\" } where <AgentName> is the name of the agent you are handing off to, <ConfidenceScore> is a float between 0 and 1 indicating your confidence in the handoff, and <ReasonForHandoff> is a brief explanation of why you are making this handoff."

        let tools = aiTools.Result
        let orchestratorAgent =
            agentFactory.CreateAzureOpenAiAgentWithTools "IntentAgent" "The Intent/Orchestrator Agent" orchestatorPrompt tools

        let bitcoinStatisticalAgent =
            agentFactory.CreateAzureOpenAiAgentWithTools
                "BitcoinStatisticalAndHistoryAnalystAgent"
                "A Bitcoin Statistical and History Analyst Agent"
                "You are a Bitcoin specialist and statistics export. You analyze the historical Bitcoin prices from the HistoricalBitcoinDataMcp tool and answer questions relating to historical Bitcoin prices, trends and stats."
                tools
        let bitcoinTaxSpecialistAgent =
            agentFactory.CreateAzureOpenAiAgent
                "BitcoinTaxSpecialistAgent"
                "A Bitcoin Tax Specialist Agent"
                "You are a Bitcoin Tax Specialist. Answer questions related to Bitcoin tax reporting, cost basis calculations, and relevant tax regulations."

        let bitcoinTransactionAccountantAgent =
            agentFactory.CreateAzureOpenAiAgent
                "BitcoinTransactionAccountantAgent"
                "A Bitcoin Transaction Accountant Agent"
                "You are a Bitcoin Transaction Accountant. You can record transactions, store them in the correct bucket per type and wallet. You can retrieve and report on the transactions. You can call the BitcoinCostBasisToolsMcp MCP tool to determine cost basis given a date range."

        let fillTaxFormAgent =
            agentFactory.CreateAzureOpenAiAgent
                "FillTaxFormAgent"
                "A Fill Tax Form Agent"
                "You are an expert in filling out tax forms on a web tax platform the user will specify related to Bitcoin transactions. Provide guidance on how to accurately complete these forms and then attempt to fill the forms using the PlayWright MCP. Wait for user verification after each complete cost basis fill action."

        let verifyFilledTaxFormAgent =
            agentFactory.CreateAzureOpenAiAgent
                "VerifyFilledTaxFormAgent"
                "A Verify Filled Tax Form Agent"
                "You are an expert in verifying filled out tax forms on a web tax platform the user will specify related to Bitcoin transactions. Verify the form after the fillTaxFormAgent is finished and the user asks for verification"

        let x: (AIAgent seq) = [ bitcoinTaxSpecialistAgent ]
        AgentWorkflowBuilder.CreateHandoffBuilderWith(orchestratorAgent)
            .WithHandoffs(orchestratorAgent, [ bitcoinStatisticalAgent
                                               bitcoinTaxSpecialistAgent 
                                               bitcoinTransactionAccountantAgent
                                               fillTaxFormAgent
                                               verifyFilledTaxFormAgent])
            .WithHandoffs([ bitcoinStatisticalAgent
                            bitcoinTaxSpecialistAgent
                            bitcoinTransactionAccountantAgent
                            fillTaxFormAgent
                            verifyFilledTaxFormAgent ],
                            orchestratorAgent)
            .Build()

    [<EntryPoint>]
    let main argv =
        use cts = new CancellationTokenSource()
        // Wire Ctrl+C to cancel the run loop and prevent the process from exiting immediately.
        Console.CancelKeyPress.Add(fun args ->
            args.Cancel <- true
            Console.WriteLine("Cancellation requested, shutting down...")
            cts.Cancel()
        )
        // Pass a factory so each turn uses a new workflow instance.
        let workflowFactory () = build cts
        runLoop workflowFactory cts.Token |> Async.AwaitTask |> Async.RunSynchronously

        0