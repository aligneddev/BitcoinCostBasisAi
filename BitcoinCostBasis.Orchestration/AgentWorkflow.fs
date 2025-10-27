namespace BitcoinCostBasis.Orchestration

open System
open System.Threading
open System.Threading.Tasks
open System.Text.Json
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
                        if root.TryGetProperty(name, &_) then
                            match root.GetProperty(name).ValueKind with
                            | JsonValueKind.String -> Some(root.GetProperty(name).GetString())
                            | _ -> None
                        else None

                    let tryGetNumber (name: string) =
                        if root.TryGetProperty(name, &_) then
                            match root.GetProperty(name).ValueKind with
                            | JsonValueKind.Number ->
                                match root.GetProperty(name).TryGetDouble() with
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

    let rec runLoop (workflow: Workflow) (ct: CancellationToken) =
        task {
            let mutable continueLoop = true
            while continueLoop && not ct.IsCancellationRequested do
                let messages = ResizeArray<ChatMessage>()
                Console.Write("> ")

                // Read line asynchronously and support cancellation by racing the read against a cancel token.
                let readTask = Console.In.ReadLineAsync() :> Task<string>
                let cancelTask = Task.Delay(Timeout.Infinite, ct)
                let! finished = Task.WhenAny(readTask, cancelTask)
                if finished = cancelTask then
                    // Cancellation requested - break the loop
                    continueLoop <- false
                else
                    let userInput = readTask.Result
                    if not (isNull userInput) then
                        messages.Add(ChatMessage(ChatRole.User, userInput))
                        let! results = RunWorkflowAsync workflow messages ct
                        messages.AddRange(results)
                    else
                        continueLoop <- false
        }
    and RunWorkflowAsync (workflow: Workflow) (messages: ResizeArray<ChatMessage>) (ct: CancellationToken) : Task<ResizeArray<ChatMessage>> =
        task {
            let mutable lastExecutorId : string = null
            // Use the default StreamAsync overload; propagate cancellation through local checks.
            let! run = InProcessExecution.StreamAsync(workflow, messages)

            // Send initial turn token.
            let! _ = run.TrySendMessageAsync(TurnToken(emitEvents = true))

            let results = ResizeArray<ChatMessage>()
            let mutable outputReceived = false
            let stream = run.WatchStreamAsync().GetAsyncEnumerator()
            while not outputReceived && not ct.IsCancellationRequested do
                let! hasNext = stream.MoveNextAsync()
                if hasNext then
                    let current = stream.Current
                    match current with
                    | :? AgentRunUpdateEvent as e ->
                        if e.ExecutorId <> lastExecutorId then
                            lastExecutorId <- e.ExecutorId
                            Console.WriteLine()
                            Console.WriteLine(if String.IsNullOrEmpty(e.Update.AuthorName) then e.ExecutorId else e.Update.AuthorName)
                        Console.Write(e.Update.Text)
                        match e.Update.Contents |> Seq.tryPick (fun c -> match c with :? FunctionCallContent as call -> Some call | _ -> None) with
                        | Some call ->
                            Console.WriteLine()
                            Console.WriteLine(sprintf "Call '%s' with arguments: %s]" call.Name (System.Text.Json.JsonSerializer.Serialize(call.Arguments)))
                        | None -> ()
                    | :? WorkflowOutputEvent as output ->
                        Console.WriteLine("\n--- Workflow Output ---")
                        let outputMessages = output.As<ResizeArray<ChatMessage>>()
                        if not (isNull outputMessages) then
                            // Validate any routing JSON produced by orchestrator and print warnings if invalid.
                            for m in outputMessages do
                                // Try to parse the message text as routing JSON.
                                let text =
                                    try
                                        // ChatMessage exposes a ToString or Text member depending on SDK; attempt both safely.
                                        let t =
                                            try m.GetType().GetProperty("Text").GetValue(m) :?> string
                                            with _ -> m.ToString()
                                        t
                                    with _ -> m.ToString()
                                match tryParseRoutingJson(text) with
                                | Some(agent, confidence, reason) ->
                                    // Validate confidence range
                                    if Double.IsNaN(confidence) || confidence <0.0 || confidence >1.0 then
                                        Console.WriteLine(sprintf "[Routing validation] Invalid confidence value: %f (expected0.0-1.0)" confidence)
                                    else
                                        Console.WriteLine(sprintf "[Routing validation] Valid routing decision -> agent: %s, confidence: %.2f, reason: %s" agent confidence (if String.IsNullOrEmpty(reason) then "(none)" else reason))
                                | None ->
                                    // If the message contains braces, warn the user that routing JSON was malformed.
                                    if text.Contains("{") && text.Contains("}") then
                                        Console.WriteLine("[Routing validation] Warning: detected JSON-like output but it did not match required schema {\"agent\":string,\"confidence\":number,\"reason\":string}")
                            results.AddRange(outputMessages)
                        outputReceived <- true
                    | _ -> ()
                else
                    outputReceived <- true
            return results
        }
