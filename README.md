﻿# Bitcoin Cost Basis AI Agent System

Using SpecKit to design an AI Agent System to help with calculating Bitcoin cost basis for tax reporting.

## TODOs
- [ ] Define F# types for transaction data - include fees, wallet/exchange (distinct wallet IDs in the DB for each venue/account), Tx hash (on‑chain), Type (purchased, transfer, traded, mining, staking, airdrop, fork, wage/comp, gift) and details (for any notes)
- [ ] Add average to the csv, add average to the F# type
- [ ] data storage for transactions
- [ ] F# compute cost basis (given new type `type costBasisDetails = dateBought: dateTime, amountBought:float, dateSold: dateTime, amountSold: float`), 
  - [ ] add method to MCP
- [ ] Buy and sell buckets for FIFO (storage, matching, reporting)

## Agents

- Bitcoin Cost Basis Orchestrator Agent
  - takes in rows of data as csv in a described F# type (date bought, date sold, amount bought, amount sold)
  - verifies type is matched
  - use BitcoinHistoricalPriceMcp to get historical prices and line them up with the transactions
  - calculate cost basis using FIFO method
  - calls the Bitcoin Tax Specialist Agent to get tax advice
  - reports suggestions on changes 
  - asks the user if they want to run the Fill form with Playwright Agent to fill in the tax form
  - calls the Verify filled form Agent to check the filled in form is correct
  - outputs the results to a file or console
  - asks the Bitcoin Tax Specialist Agent for things to do differently for the next year 

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
  -  Reporting workflow (year‑end)
  - Generate Form 8949 detail (short‑ vs long‑term sections) and Schedule D totals. [irs.gov]
  - Store 1099‑DA imports and reconcile variances (e.g., fees, basis, proceeds timing). Brokers must report gross proceeds for 2025+ transactions and basis starting 2026 for certain assets—expect mismatches i- your method differs; track adjustments. [irs.gov]
    

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

## SpecKit
### Constitution
- The Bitcoin Cost Basis AI Agent System exists to help users accurately calculate their Bitcoin cost basis for tax reporting purposes, leveraging historical price data and adhering to relevant tax regulations.
-  Create principles focused on code quality, testing standards, user experience consistency, and performance requirements following Clean Architecture, Functional Programming and TDD principles.
- Using  F#, .Net 10, Microsoft Agent Framework and Podman containerization will be used to create agents and F# deterministic code.