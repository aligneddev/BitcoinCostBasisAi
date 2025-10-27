namespace BitcoinCostBasis

// based on https://youtu.be/VInKZ45YKAM from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
open Microsoft.Agents.AI.Workflows
open System
open System.Threading
open System.Threading.Tasks
open BitcoinCostBasis.Orchestration.AgentWorkflow
open Entry

module EntryProgramDemo =
    let configuration = { AzureOpenAiEndpoint = "https://kl-demo-hub-resource.openai.azure.com/"; AzureOpenAiKey = "REPLACE_ME"; ModelDeploymentName = "gpt-4.1-mini" }
    let agentFactory = AgentFactory(configuration)

    let orchestatorPrompt =
        "You are a helpful assistant. Determine what type of question was asked and handoff to the other agents. Keep track of the current part of the workflow and prompt for next actions. Never answer yourself, **always** pass to the other agents. If none of the agents match the criteria, say so, don't answer the question. "
        + "When you start up or if the users asks for what you can do, explain the different options and workflows you can help with. "
        + "There are several possible workflows: "
        + "The `record transactions` workflow is as follows that you will hand off to the bitcoinTransactionAccountantAgent:1) The user will ask to record transactions,2)"
        + "The `determine cost basis` workflow is as follows: "
        + "The `fill in the tax forms workflow` is as follows:1) The user will ask questions related to Bitcoin tax reporting, cost basis calculations, and relevant tax regulations to the BitcoinTaxSpecialistAgent or BitcoinTransactionAccountantAgent.2) Once the user is ready to fill out tax forms, they will instruct you to hand off to the FillTaxFormAgent.3) After the FillTaxFormAgent completes filling out the forms, the user may ask you to verify the filled forms, at which point you will hand off to the VerifyFilledTaxFormAgent.4) After verification, return control to the user for any further questions or actions. "
        + "The user may also ask questions which you should hand off to the appropriate agent."

    let orchestratorAgent = agentFactory.CreateAzureOpenAiAgent "IntentAgent" orchestatorPrompt

    let bitcoinTaxSpecialistAgent = agentFactory.CreateAzureOpenAiAgent "BitcoinTaxSpecialistAgent" "You are a Bitcoin Tax Specialist. Answer questions related to Bitcoin tax reporting, cost basis calculations, and relevant tax regulations."
    let bitcoinTransactionAccountantAgent = agentFactory.CreateAzureOpenAiAgent "BitcoinTransactionAccountantAgent" "You are a Bitcoin Transaction Accountant. You can record transactions, store them in the correct bucket per type and wallet. You can retrieve and report on the transactions. You can use the BitcoinCostBasisToolsMcp to determine cost basis."
    let fillTaxFormAgent = agentFactory.CreateAzureOpenAiAgent "FillTaxFormAgent" "You are an expert in filling out tax forms on a web tax platform the user will specify related to Bitcoin transactions. Provide guidance on how to accurately complete these forms and then attempt to fill the forms using the PlayWright MCP. Wait for user verification after each complete cost basis fill action."
    let verifyFilledTaxFormAgent = agentFactory.CreateAzureOpenAiAgent "VerifyFilledTaxFormAgent" "You are an expert in verifying filled out tax forms on a web tax platform the user will specify related to Bitcoin transactions. Verify the form after the fillTaxFormAgent is finished and the user asks for verification"

    let workflow =
        AgentWorkflowBuilder.CreateHandoffBuilderWith(orchestratorAgent)
            .WithHandoffs(orchestratorAgent, [| bitcoinTaxSpecialistAgent; bitcoinTransactionAccountantAgent; fillTaxFormAgent; verifyFilledTaxFormAgent |])
            .WithHandoffs([| bitcoinTaxSpecialistAgent; bitcoinTransactionAccountantAgent; fillTaxFormAgent; verifyFilledTaxFormAgent |], orchestratorAgent)
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

        // Call the function returned by runLoop to get a Task, then await it synchronously.
        runLoop workflow cts.Token |> Async.AwaitTask |> Async.RunSynchronously

        //.GetAwaiter().GetResult()
        0