# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-bookworm

# Install podman (rootless compatible) and other tools
RUN apt-get update \
 && apt-get install -y podman \
 && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Enable F# workload explicitly (usually included but ensure templates)
RUN dotnet new list > /dev/null

# Set up a non-root user (already provided by base image: vscode)
USER vscode

WORKDIR /workspaces/BitcoinCostBasisAi
