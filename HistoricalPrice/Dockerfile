# Use official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore as distinct layer
COPY ["BitcoinHistoricalPriceMcp.fsproj", "./"]
COPY *.fs ./
RUN dotnet restore BitcoinHistoricalPriceMcp.fsproj

# Copy all sources and publish
COPY . .
RUN dotnet publish BitcoinHistoricalPriceMcp.fsproj -c Release -o /app/publish

# Use a minimal runtime image for running the app
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Ensure the data directory exists and copy CSV into it (optional; publish may already include the file)
COPY BitcoinHistoricalData.csv ./data/BitcoinHistoricalData.csv

# Start the self-contained single-file executable directly and pass the CSV path as an argument
ENTRYPOINT ["./BitcoinHistoricalPriceMcp", "./data/BitcoinHistoricalData.csv"]
