open System.ComponentModel
open System.Runtime.InteropServices
open ModelContextProtocol.Server

/// Sample MCP tools for demonstration purposes.
/// These tools can be invoked by MCP clients to perform various operations.
[<Sealed>]
type RandomNumberTools() =
    [<McpServerTool>]
    [<Description("Generates a random number between the specified minimum and maximum values.")>]
    member _.GetRandomNumber
        ([<Description("Minimum value (inclusive)"); Optional; DefaultParameterValue(0)>] min:int,
         [<Description("Maximum value (exclusive)"); Optional; DefaultParameterValue(100)>] max:int) : int =
        System.Random.Shared.Next(min, max)
