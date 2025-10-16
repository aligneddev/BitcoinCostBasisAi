namespace BitcoinHistoricalPriceMcp

open System
open System.Globalization
module HistoricalPriceReader =
    type HistoricalBitcoinData = {
        Date: DateTime
        Price: decimal
        Open: decimal
        High: decimal
        Low: decimal
        Vol: string
        ChangePercent: decimal
    }

    let parseCsvLine (line: string) =
            let sb = System.Text.StringBuilder()
            let fields = System.Collections.Generic.List<string>()
            let mutable i = 0
            let len = line.Length
            let mutable inQuotes = false
            while i < len do
                let c = line.[i]
                if c = '"' then
                    if inQuotes && i + 1 < len && line.[i + 1] = '"' then
                        sb.Append('"') |> ignore
                        i <- i + 1
                    else
                        inQuotes <- not inQuotes
                elif c = ',' && not inQuotes then
                    fields.Add(sb.ToString())
                    sb.Clear() |> ignore
                else
                    sb.Append(c) |> ignore
                i <- i + 1
            fields.Add(sb.ToString())
            fields.ToArray()

    let readHistoricalData (filePath: string) =
        if not (System.IO.File.Exists filePath) then
            failwithf "File not found: %s" filePath

        let lines = System.IO.File.ReadAllLines filePath
        lines
        |> Array.skip 1 // skip header
        // |> Array.take 1
        |> Array.choose (fun line ->
            let parts = parseCsvLine line
            if parts.Length >= 7 then
                let parseDecimalField (s: string) =
                    s.Trim().Trim('"').Replace(",", "")
                    |> fun t -> Decimal.Parse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)

                let dateStr = parts.[0].Trim().Trim('"')
                let date =
                    let formats = [| "M/d/yyyy"; "MM/dd/yyyy"; "yyyy-MM-dd" |]
                    let mutable dt = DateTime.MinValue
                    let ok = DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, &dt)
                    if not ok then
                        failwithf "Invalid date in CSV: '%s' (expected formats: M/d/yyyy, MM/dd/yyyy, yyyy-MM-dd)" dateStr
                    DateTime.SpecifyKind(dt, DateTimeKind.Utc)

                let price = parseDecimalField parts.[1]
                let openPrice = parseDecimalField parts.[2]
                let high = parseDecimalField parts.[3]
                let low = parseDecimalField parts.[4]
                let vol = parts.[5].Trim().Trim('"')
                let changePercent =
                    parts.[6].Trim().Trim('"').TrimEnd('%')
                    |> fun s -> Decimal.Parse(s, NumberStyles.Number ||| NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)

                Some { Date = date; Price = price; Open = openPrice; High = high; Low = low; Vol = vol; ChangePercent = changePercent }
            else
                None  )
                
    let readHistoricalDataForDateRange (filePath: string, startDate: DateTime, endDate: DateTime) =
        let allData = readHistoricalData filePath
        allData
        |> Array.filter (fun d -> d.Date >= startDate && d.Date <= endDate)
// Testing
// BitcoinHistoricalData.csv came from https://www.investing.com/crypto/bitcoin/historical-data
//					- logged in, choose date range, download csv
// // for interactive
// HistoricalData (System.IO.Path.Combine(__SOURCE_DIRECTORY__, "HistoricalPrice", "BitcoinHistoricalData.csv"))
// //HistoricalData "BitcoinHistoricalData.csv"
// |> Array.sortByDescending (fun d -> d.Date)
// |> Array.take 50
// |> Array.iter (fun data ->
//     let fmt (d: decimal) = d.ToString("G29", CultureInfo.InvariantCulture)
//     let dateStr = data.Date.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
//     Console.WriteLine($"{dateStr}, {fmt data.Price}, {fmt data.Open}, {fmt data.High}, {fmt data.Low}, {data.Vol}, {fmt data.ChangePercent}" )
// )
