# Bitcoin Cost Basis AI Agent System


Goals:
- Learning Microsoft Agent Framework  
- Use F#
- Share what I learned with others
- Have the Agent as the interface for the user
- Create a system to help with calculating Bitcoin cost basis for tax reporting.
- Make tax reporting easier

## TODOs
- [ ] Define F# types for transaction data - include fees, wallet/exchange (distinct wallet IDs in the DB for each venue/account), Tx hash (on‑chain), Type (purchased, transfer, traded, mining, staking, airdrop, fork, wage/comp, gift) and details (for any notes)
- [ ] Add average to the csv, add average to the F# type
- [ ] data storage for transactions recording (container?, local db? how to persist data?)
    - how would this scale with more users in the future?
- [ ] F# compute cost basis (given new type `type costBasisDetails = dateBought: dateTime, amountBought:float, dateSold: dateTime, amountSold: float`), 
  - [ ] add method to MCP
- [ ] Buy and sell buckets for FIFO (storage, matching, reporting)
- [ ] Can the MCP be an API and an MCP tool?
    - [ ] reorganize notes below
- [ ] Rename BitcoinHistoricalPriceMcp to BitcoinCostBasisToolsMcp
- [ ] Add Aspire to Entry
- [ ] Make an Api version of Entry with a 402 payment requirement
- [ ] add unit tests for deterministic code
- [ ] Persist history during session
- [ ] Persist history after session

## Agents

- Bitcoin Cost Basis Orchestrator Agent
  - The main orchestrator agent that coordinates the workflow and the user will interact with.
  - takes in rows of data as csv in a described F# type (date bought, date sold, amount bought, amount sold)
  - verifies type is matched
  - use BitcoinHistoricalPriceMcp to get historical prices and line them up with the transactions
    - calculate cost basis using FIFO method via the MCP tool
  - calls the Bitcoin Tax Specialist Agent to get tax advice
    - reports suggestions on changes 
  - asks the user if they want to run the Fill form with Playwright Agent to fill in the tax form
  - calls the Verify filled form Agent to check the filled in form is correct
  - outputs the results to a file or console
  - asks the Bitcoin Tax Specialist Agent for things to do differently for the next year

- Bitcoin Statistical And History Analyst Agent

- Bitcoin Tax Specialist Agent
  - knows the Bitcoin tax rules and gives advice

- Bitcoin Transaction Accountant Agent
  - Could just be a console app, but the reporting would be nice as an agent
  - records transactions (user can add transactions over time)
  - stores the transactions in a database or file
  - supplies reports on the transactions over time as requested by the user
  - Recording the buckets: Normally I buy Bitcoin with Daily Cost Average (DCA), but other times in the past I buy lumps and other times I've traded from other cryptos.
    - With FIFO cost basis calculation, I need to take the First In purchase and match it to the First Out sale. 
    - Usually the sells are only a small portion of the buys, but others times the sell is larger than the first buy, so I need to take the next buy(s) until the sell is fully matched.
  - How can we import data from my custom CSV?
    - Someday we could import from an exchange API.
    
- Fill form with Playwright Agent
  - structured input - JSON
  - optional agent
  - take the data and fill in the Capital Gains form (is this even possible? we shall see)
  - asks for confirmation before filling in the row
  - ask for verification after filling in the row
  - can it be generic or have to be specific to my tax software?
  - Answer the digital asset question on your 1040 (or other return). [irs.gov]

- Verify filled form Agent
  - check the filled in form is correct with the data given

### Other Miscellaneous Agents

Maybe I could make this a larger web of agents...

- Bitcoin Analyzer
  - how much have I increased in Sats/USD?
  - What if I sold now?
  - reporting over time with graphs based on transactions
- Bitcoin Maximalist Expert
  - not really needed ask questions, inspiration, longer term thinking
  - security suggestions
  - it doesn't add much value to Cost basis and taxes, but maybe it can add some commentary
  - complain that there shouldn't be taxes on Bitcoin transactions
- Reporting Agent
  - Reporting workflow (year‑end)
  - Generate Form 8949 detail (short‑ vs long‑term sections) and Schedule D totals. [irs.gov]
  - Store 1099‑DA imports and reconcile variances (e.g., fees, basis, proceeds timing). Brokers must report gross proceeds for 2025+ transactions and basis starting 2026 for certain assets—expect mismatches i- your method differs; track adjustments. [irs.gov]
  - What should be deterministic code vs agent LLM?

- User Advisory / Explanation Agent
  - A layer for translating deterministic outputs into user‑friendly narrative.
 
## Deterministic Code & MCP Tool

- Bitcoin Historical Price MCP Tool
  - Given a date, return the historical price of Bitcoin in USD on that date.
  - Use a public API or historical data source.
  - This will be used by the Orchestrator Agent to get prices for cost basis calculation.
  - Could be split into more MCP tools as needed
  - Transaction data model + immutable storage (parse/validate CSV, wallet/exchange IDs, fees, Tx hash, type enums)
  - FIFO cost basis engine (matching buys to sells, partial bucket consumption, long/ vs short‑term holding period, gift carryover basis logic)
  - Bitcoin Analyzer metrics (portfolio value over time, unrealized gain/loss, DCA averages, what‑if sell)
  - CSV import & normalization (schema versioning, idempotent ingestion)
  - Validation/verification of filled tax form rows (rule checks: dates, proceeds vs basis math, holding period classification)
  - Form fill executor (Playwright automation steps – deterministic sequence with confirmations)
  - Gift and transfer handlers (rules: no gain on gift, carryover basis, non-taxable internal transfers)
  - Data export (JSON, CSV, audit trail snapshots)

## Cost Basis Manual Workflow

The Bitcoin Transaction Accountant Agent will help with this workflow.

### Purchasing Bitcoin

Bitcoin is purchased manually or automatically on an ongoing basis (DCA - Daily Cost Average) or automatically based on spot prices.

### Recording

After the Bitcoin is purchased, then recorded with date purchased, amount purchased, USD price per Bitcoin, fees, wallet/exchange and any notes.

I've been using a .csv file, but we want to move this into a database that is immutable for an audit trail.

### Trading To Bitcoin

After the trade is completed, then record the the type of crypto, date sold, amount sold, price per unit in USD, fees, from wallet/exchange and any notes.
Then record the Bitcoin purchase as above date purchased, amount purchased, USD price per Bitcoin, fees, to wallet/exchange and any notes.

### Selling Bitcoin for Dollars or Goods

After the sell is completed, then record the date sold, amount sold, price per Bitcoin in USD, fees, the wallet/exchange and notes (what was purchased or the dollars will be used for).

### Giving Bitcoin

Gifts: giving is a disposition without gain (unless debt relief etc.), but donee takes carryover basis; receiving a gift is not income. (Track gifts separately in your schema; still needed for future gain calculations.) [General Pub. 551 principles for basis other than cost]

### Transferring Bitcoin Between Wallets/Exchanges


#### Capital Gains Calculation

Using FIFO (First In First Out) method, match the sold Bitcoin to the earliest purchased Bitcoin that has not yet been sold. 
Find the cost basis for the sold Bitcoin by using the first bucket of purchased Bitcoin. If there is more sold than in that bucket, continue to the next, averaging the price until the sold amount is covered.

The difference between the sale price and the cost basis is the capital gain or loss.
Long term is if the Bitcoin was held for more than one year before selling, otherwise it is short term.

### Trading From Bitcoin

I won't be doing this, but in the past I have done this.

After the trade is completed, then record the Bitcoin sale as above date sold, amount sold, price per Bitcoin in USD, fees, from wallet/exchange and any notes.
Then record the purchase of the other crypto as above date purchased, amount purchased, USD price per unit, fees, to wallet/exchange and any notes.


### Basis method & Rev. Proc. 2024‑28 transition plan (DO THIS ONCE)

IRS Rev. Proc. 2024‑28 allows per wallet cost basis tracking.
Freeze your 12/31/2024 end‑of‑year inventory and balances by wallet.
Choose your safe harbor:

Specific Unit Allocation (map legacy unused basis to the actual units/wallets you hold on 1/1/2025), or
Global Allocation (apply a consistent rule across all holdings, e.g., earliest buys → Wallet A, later → Wallet B).

Record the allocation in your database table above with supporting reports. Once done, it’s irrevocable.


## Copilot getting started suggestions
make suggestions for a "microsoft agent framework" implementation from this document. not semantic kernel

### analyze my readme and tell me which of these agents should be deterministic code and MCP tools

Rationale:
- Anything requiring strict reproducibility, audit trails, tax math or regulatory compliance → deterministic F# + MCP.
- Narrative, advisory, strategic suggestions → LLM agents.
- Automation needing side effects (web form filling) but with predictable steps → deterministic tool callable by an agent.

## SpecKit
### Constitution
- The Bitcoin Cost Basis AI Agent System exists to help users accurately calculate their Bitcoin cost basis for tax reporting purposes, leveraging historical price data and adhering to relevant tax regulations.
-  Create principles focused on code quality, testing standards, user experience consistency, and performance requirements following Clean Architecture, Functional Programming and TDD principles.
- Using F#, .Net 10, Microsoft Agent Framework and Podman containerization will be used to create agents and F# deterministic code.


## Testing

- Based on the historical prices from Jan 2023 to today, On average, what is the best day of the week to buy bitcoin?
    - didn't complete
    - IntentAgent

        Call 'handoff_to_1' with arguments: {"reasonForHandoff":"User wants analysis on the best day of the week to buy bitcoin based on historical prices from Jan 2023 to today."}]

        BitcoinStatisticalAndHistoryAnalystAgent
        To determine the best day of the week to buy Bitcoin based on historical prices from January 2023 to today, I will analyze the average closing prices of Bitcoin for each day of the week over this period. This will help identify which day typically had the lowest average closing price, suggesting the best day to buy.

        Let me fetch and analyze the data for you.
      I need to check the historical Bitcoin closing prices for each day of the week from January 2023 to today. Let me gather the data and calculate the average closing price for each weekday.

    - After this attempt, I gave it the MCP tool
    -   BitcoinStatisticalAndHistoryAnalystAgent

        Call 'read_historical_data_for_date_range' with arguments: {"startDate":"2023-01-01T00:00:00Z","endDate":"2024-06-21T00:00:00Z"}]
        I have the historical Bitcoin price data from January 2023 to today. I will analyze the average prices for each day of the week to determine which day on average is the best day to buy Bitcoin (i.e., the day with the lowest average closing price). Let me perform this analysis.
        After analyzing the historical Bitcoin prices from January 2023 to today, here is the average closing price of Bitcoin for each day of the week:

        - Monday: (calculated average price)
        - Tuesday: (calculated average price)
        - Wednesday: (calculated average price)
        - Thursday: (calculated average price)
        - Friday: (calculated average price)
        - Saturday: (calculated average price)
        - Sunday: (calculated average price)

        Based on these averages, the best day to buy Bitcoin on average (the day with the lowest average closing price) is: [Day of the Week].

        Please hold on while I calculate the exact averages and finalize the best day for buying Bitcoin.
        --- Workflow Output ---