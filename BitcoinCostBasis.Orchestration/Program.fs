namespace BitcoinCostBasis

//// based on https://youtu.be/VInKZ45YKAM from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
//open System
//open System.Threading.Tasks
//open Microsoft.Agents.AI.Workflows
//open Microsoft.Extensions.AI
//open Entry

//module EntryProgram =
//    let configuration = { AzureOpenAiEndpoint = "https://kl-demo-hub-resource.openai.azure.com/"; AzureOpenAiKey = "REPLACE_ME"; ModelDeploymentName = "gpt-4.1-mini" }
    
//    let agentFactory = AgentFactory(configuration)
//    let orchestratorAgent = agentFactory.CreateLocalOrchestrationAgent "http://localhost:11434/" "OrchestrationAgent" "llama3.2:3b"

//    // TODO enable requests and responses for human intervention

//    let rec runLoop () =
//        task {
//            let mutable continueLoop = true
//            while continueLoop do
//                let messages = ResizeArray<ChatMessage>()
//                let workflow =
//                    AgentWorkflowBuilder.CreateHandoffBuilderWith(orchestratorAgent)
//                        .WithHandoffs(orchestratorAgent, [| movieNerd; musicNerd |])
//                        .WithHandoffs([| movieNerd; musicNerd |], orchestratorAgent)
//                        .Build()
//                Console.Write("> ")
//                let userInput = Console.ReadLine()
//                if not (isNull userInput) then
//                    messages.Add(ChatMessage(ChatRole.User, userInput))
//                    let! results = RunWorkflowAsync workflow messages
//                    messages.AddRange(results)
//                else
//                    continueLoop <- false
//        }
//    and RunWorkflowAsync (workflow: Workflow) (messages: ResizeArray<ChatMessage>) : Task<ResizeArray<ChatMessage>> =
//        task {
//            let mutable lastExecutorId : string = null
//            let! run = InProcessExecution.StreamAsync(workflow, messages)
//            // Discard bool result properly
//            let! _ = run.TrySendMessageAsync(TurnToken(emitEvents = true))
//            let results = ResizeArray<ChatMessage>()
//            let mutable outputReceived = false
//            let stream = run.WatchStreamAsync().GetAsyncEnumerator()
//            while not outputReceived do
//                let! hasNext = stream.MoveNextAsync()
//                if hasNext then
//                    let current = stream.Current
//                    match current with
//                    | :? AgentRunUpdateEvent as e ->
//                        if e.ExecutorId <> lastExecutorId then
//                            lastExecutorId <- e.ExecutorId
//                            Console.WriteLine()
//                            Console.WriteLine(if String.IsNullOrEmpty(e.Update.AuthorName) then e.ExecutorId else e.Update.AuthorName)
//                        Console.Write(e.Update.Text)
//                        match e.Update.Contents |> Seq.tryPick (fun c -> match c with :? FunctionCallContent as call -> Some call | _ -> None) with
//                        | Some call ->
//                            Console.WriteLine()
//                            Console.WriteLine(sprintf "Call '%s' with arguments: %s]" call.Name (System.Text.Json.JsonSerializer.Serialize(call.Arguments)))
//                        | None -> ()
//                    | :? WorkflowOutputEvent as output ->
//                        Console.WriteLine("\n--- Workflow Output ---")
//                        let outputMessages = output.As<ResizeArray<ChatMessage>>()
//                        if not (isNull outputMessages) then
//                            results.AddRange(outputMessages)
//                        outputReceived <- true
//                    | _ -> ()
//                else
//                    outputReceived <- true
//            return results
//        }

//    [<EntryPoint>]
//    let main argv =
//        runLoop().GetAwaiter().GetResult()
//        0