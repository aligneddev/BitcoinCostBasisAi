# Bitcoin Cost Basis AI Agent System

## TODOs
- [ ] Define F# types for transaction data - include fees, wallet/exchange and details (for any notes)
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


## Copilot getting started suggestions
make suggestions for a "microsoft agent framework" implementation from this document. not semantic kernel