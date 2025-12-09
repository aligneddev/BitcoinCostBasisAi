namespace BitcoinCostBasis

// based on https://youtu.be/VInKZ45YKAM from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
open Microsoft.Agents.AI.Workflows
open Entry

module EntryProgramDemo_Sample =
    
    let configuration = { AzureOpenAiEndpoint = "https://kl-demo-hub-resource.openai.azure.com/"; AzureOpenAiKey = "REPLACE_ME"; ModelDeploymentName = "gpt-4.1-mini" }
    let agentFactory = AgentFactory(configuration)

    // DEMO code from https://github.com/rwjdk/MicrosoftAgentFrameworkSamples/blob/main/src/Workflow.Handoff/Program.cs
    let intentAgent = agentFactory.CreateAzureOpenAiAgent "IntentAgent" "IntentAgent" "Determine what type of question was asked and handoff to the other agents. Never answer yourself, always pass to the other agents. If neither of them match the criteria, say so, don't answer the question"

    let movieNerd = agentFactory.CreateAzureOpenAiAgent "MovieNerd" "Movie Nerd Agent" "You are a Movie Nerd"

    let musicNerd = agentFactory.CreateAzureOpenAiAgent "MusicNerd" "Music Nerd Agent" "You are a Music Nerd"
    let doNotKnowAgent = agentFactory.CreateAzureOpenAiAgent "DoNotKnowAgent" "Do Not Know Agent" "I don't know the answer to that question."
    let workflow =
        AgentWorkflowBuilder.CreateHandoffBuilderWith(intentAgent)
            .WithHandoffs(intentAgent, [| movieNerd; musicNerd; doNotKnowAgent |])
            .WithHandoffs([| movieNerd; musicNerd; doNotKnowAgent |], intentAgent)
            .Build()

    //[<EntryPoint>]
    //let main argv =
    //    // Call the function returned by runLoop to get a Task, then await it synchronously.
    //    runLoop workflow () |> Async.AwaitTask |> Async.RunSynchronously
       
    //    //.GetAwaiter().GetResult()
    //    0