namespace BitcoinHistoricalPriceMcp
open ModelContextProtocol.Server
open System.ComponentModel
open System

module Mcp =
    [<Sealed>]
    [<McpServerToolType>]
    type HistoricalBitcoinDataMcp() =
        [<McpServerTool>]
        [<Description("Reads historical Bitcoin price data from a CSV file.")>]
        member _.ReadHistoricalData() =
            // path in the container/image
            let defaultCsv = "./data/BitcoinHistoricalData.csv"
            HistoricalPriceReader.readHistoricalData defaultCsv
            |> Array.toList
        [<McpServerTool>]
        [<Description("Reads historical Bitcoin price data from a CSV file for the given date range.")>]
        member _.ReadHistoricalDataForDateRange(startDate: DateTime, endDate: DateTime) =
            // path in the container/image
            let defaultCsv = "./data/BitcoinHistoricalData.csv"
            HistoricalPriceReader.readHistoricalDataForDateRange(defaultCsv, startDate, endDate)
            |> Array.toList
