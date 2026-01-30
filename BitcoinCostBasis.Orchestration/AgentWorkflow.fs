namespace BitcoinCostBasis.Orchestration

open System
open System.Threading
open System.Threading.Tasks
open System.Text.Json
open Microsoft.Agents.AI
open Microsoft.Agents.AI.Workflows
open Microsoft.Extensions.AI

// General-purpose agent workflow loop
// based on https://youtu.be/VInKZ45YKAM from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
module AgentWorkflow =
    let tryParseRoutingJson (text: string) : (string * float * string) option =
        if String.IsNullOrWhiteSpace(text) then None
        else
            try
                use doc = JsonDocument.Parse(text)
                let root = doc.RootElement
                if root.ValueKind <> JsonValueKind.Object then None
                else
                    let tryGetString (name: string) =
                        let mutable prop = Unchecked.defaultof<JsonElement>
                        if root.TryGetProperty(name, &prop) then
                            match prop.ValueKind with
                            | JsonValueKind.String -> Some(prop.GetString())
                            | _ -> None
                        else None

                    let tryGetNumber (name: string) =
                        let mutable prop = Unchecked.defaultof<JsonElement>
                        if root.TryGetProperty(name, &prop) then
                            match prop.ValueKind with
                            | JsonValueKind.Number ->
                                match prop.TryGetDouble() with
                                | true, v -> Some v
                                | _ -> None
                            | _ -> None
                        else None

                    match tryGetString("agent"), tryGetNumber("confidence") with
                    | Some agent, Some confidence ->
                        let reason =
                            match tryGetString("reason") with
                            | Some r -> r
                            | None -> String.Empty
                        Some(agent, confidence, reason)
                    | _ -> None
            with
            | _ -> None

    // Modified: runLoop now takes a workflow factory to create a fresh workflow per turn
    let rec runLoop (workflowFactory: unit -> Workflow) (ct: CancellationToken) =
        task {
            let mutable continueLoop = true
            while continueLoop && not ct.IsCancellationRequested do
                let messages = ResizeArray<ChatMessage>()
                Console.Write("> ")

                // Read line asynchronously and support cancellation by racing the read against a cancel token.
                let readTask = Console.In.ReadLineAsync()
                let cancelTask = Task.Delay(Timeout.Infinite, ct)
                let! finished = Task.WhenAny(readTask, cancelTask)
                if finished = cancelTask then
                    // Cancellation requested - break the loop
                    continueLoop <- false
                else
                    let userInput = readTask.Result
                    if not (isNull userInput) then
                        // Create a new workflow instance per user turn to avoid ownership conflicts.
                        let workflow = workflowFactory()
                        messages.Add(ChatMessage(ChatRole.User, userInput))
                        let! results = RunWorkflowAsync workflow messages ct
                        messages.AddRange(results)
                    else
                        continueLoop <- false
        }
    and RunWorkflowAsync (workflow: Workflow) (messages: ResizeArray<ChatMessage>) (ct: CancellationToken) : Task<ResizeArray<ChatMessage>> =
        task {
            let mutable lastExecutorId : string = null
            use! run = InProcessExecution.StreamAsync(workflow, messages)
            let! _ = run.TrySendMessageAsync(TurnToken(emitEvents = true))
            let mutable finalOutput : ResizeArray<ChatMessage> = ResizeArray()
            
            use stream = run.WatchStreamAsync().GetAsyncEnumerator()
            let mutable running = true
            while running && not ct.IsCancellationRequested do
                let! hasNext = stream.MoveNextAsync()
                if not hasNext then
                    running <- false
                else
                    let current = stream.Current
                    Console.WriteLine(sprintf "Debug: Current Step is %s" (current.GetType().ToString()))
                    match current with
                    | :? ExecutorInvokedEvent as e ->
                        if e.ExecutorId <> lastExecutorId then
                            lastExecutorId <- e.ExecutorId
                            Console.WriteLine()
                            Console.WriteLine(sprintf "[ExecutorStarting: %s]" e.ExecutorId)
                    | :? ExecutorCompletedEvent as e ->
                        if e.ExecutorId <> lastExecutorId then
                            lastExecutorId <- e.ExecutorId
                            Console.WriteLine()
                            Console.WriteLine(sprintf "[ExecutorCompleted: %s]" e.ExecutorId)
                    | :? ExecutorFailedEvent as e ->
                        Console.WriteLine()
                        Console.WriteLine(sprintf "[ExecutorFailed: %s]" e.ExecutorId)
                        if not (isNull e.Data) then
                            Console.WriteLine(sprintf "  Error: %s" e.Data.Message)
                    | :? AgentResponseUpdateEvent as e ->
                        if e.ExecutorId <> lastExecutorId then
                            lastExecutorId <- e.ExecutorId
                            Console.WriteLine()
                            Console.WriteLine(e.ExecutorId)
                        // Print the streaming data directly
                        Console.Write(e.Data.ToString())
                    //| :? SuperStepStartedEvent as e ->
                    //    Console.WriteLine(sprintf "[SuperStepStarted: Step=%d]" e.StepNumber)
                    //| :? SuperStepCompletedEvent as e ->
                    //    Console.WriteLine(sprintf "[SuperStepCompleted: Step=%d]" e.StepNumber)
                    | :? WorkflowOutputEvent as output ->
                        Console.WriteLine("\n--- Workflow Output ---")
                        // Access Data directly and cast to the expected type
                        match output.Data with
                        | :? ResizeArray<ChatMessage> as outputMessages ->
                            for m in outputMessages do
                                Console.WriteLine(sprintf "Role: %s, Text: %s" (m.Role.ToString()) (m.Text))
                            finalOutput <- outputMessages
                        | _ ->
                            Console.WriteLine(sprintf "Unexpected output type: %s" (output.Data.GetType().ToString()))
                    | _ ->
                        Console.WriteLine(sprintf "Unhandled event: %s" (current.GetType().ToString()))
            return finalOutput
        }
