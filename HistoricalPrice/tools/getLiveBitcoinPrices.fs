namespace BitcoinHistoricalPriceMcp

open System
open System.Globalization
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization


module GetHistoricalPrices =

    type HistoricalDataFromCsv =
        { Date: DateTime
          Price: decimal
          Open: decimal
          High: decimal
          Low: decimal
          Vol: string
          ChangePercent: decimal }

    type PricePoint =
        { Timestamp: DateTime
          PriceUsd: decimal }

    type MarketChartResponse =
        { [<JsonPropertyName("prices")>]
          Prices: double[][] }


    let parseDate (label: string) (arg: string) =
        let mutable parsed = DateTime.MinValue

        let ok =
            DateTime.TryParseExact(
                arg,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal ||| DateTimeStyles.AdjustToUniversal,
                &parsed
            )

        if not ok then
            failwithf "Invalid %s '%s'. Expecting format yyyy-MM-dd." label arg

        DateTime.SpecifyKind(parsed, DateTimeKind.Utc)

    let toUnixSeconds (dt: DateTime) = DateTimeOffset(dt).ToUnixTimeSeconds()

    let fetchPrices (startDate: DateTime) (endDate: DateTime) =
        async {
            use httpClient = new HttpClient()
            // CoinGecko range endpoint returns unix timestamps in milliseconds.
            let fromUnix = toUnixSeconds startDate
            let toUnix = toUnixSeconds (endDate.AddDays(1.0))

            let url =
                $"https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency=usd&from={fromUnix}&to={toUnix}"

            let! response = httpClient.GetAsync(url) |> Async.AwaitTask

            if not response.IsSuccessStatusCode then
                let! errorText = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return failwithf "API request failed (%O): %s" response.StatusCode errorText

            let! payload = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let docRaw = JsonSerializer.Deserialize<MarketChartResponse>(payload)

            if obj.ReferenceEquals(docRaw, null) then
                failwith "Unable to deserialize API response."

            let doc = docRaw

            let prices =
                doc.Prices
                |> Array.choose (fun datapoint ->
                    if datapoint.Length >= 2 then
                        let timestamp =
                            DateTimeOffset.FromUnixTimeMilliseconds(int64 datapoint.[0]).UtcDateTime

                        Some
                            { Timestamp = timestamp
                              PriceUsd = decimal datapoint.[1] }
                    else
                        None)

            return prices
        }

    let run (args: string[]) =
        if args.Length <> 2 then
            failwith "Usage: dotnet fsi getBitcoinPrices.fsx <start-date: yyyy-MM-dd> <end-date: yyyy-MM-dd>"

        let startDate = parseDate "start date" args.[0]
        let endDate = parseDate "end date" args.[1]

        if endDate < startDate then
            failwith "End date must be on or after start date."

        let prices = fetchPrices startDate endDate |> Async.RunSynchronously

        if prices.Length = 0 then
            printfn "No price data returned for the specified range."
        else
            printfn "timestamp_utc,price_usd"

            prices
            |> Array.sortBy (fun p -> p.Timestamp)
            |> Array.iter (fun point -> printfn "%s,%.2f" (point.Timestamp.ToString("u")) (float point.PriceUsd))

// GetHistoricalPrices.run([| "2025-01-01"; "2025-10-10" |])
