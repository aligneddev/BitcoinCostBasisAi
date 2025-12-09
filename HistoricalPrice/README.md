# MCP Server

This README was created using the C# MCP server project template.
It demonstrates how you can easily create an MCP server using C# and publish it as a NuGet package.

The MCP server is built as a self-contained application and does not require the .NET runtime to be installed on the target machine.
However, since it is self-contained, it must be built for each target platform separately.
By default, the template is configured to build for:
* `win-x64`
* `win-arm64`
* `osx-arm64`
* `linux-x64`
* `linux-arm64`
* `linux-musl-x64`

If your users require more platforms to be supported, update the list of runtime identifiers in the project's `<RuntimeIdentifiers />` element.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.


## Bitcoin Historical Price MCP

```
"BitcoinHistoricalPriceMcp": {
        "type": "stdio",
        "command": "podman",
        "args": [ "run", "--rm", "-i", "bitcoin-historical-mcp" ]
      }
```
I did not have to do podman run, VS fired up it's own container.

### running in Podman:

In the HistoricalPrice directory,
build and run the MCP with:
`podman build -t bitcoin-historical-mcp .`
if it hangs on dotnet restore, try: `podman build -t bitcoin-historical-mcp --network host .`

run with: 
`podman run bitcoin-historical-mcp`

run for MCP tool:
`podman run --rm -it --name bitcoin-historical-mcp bitcoin-historical-mcp`

## More information

.NET MCP servers use the [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) C# SDK. For more information about MCP:

- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
- https://github.com/modelcontextprotocol/csharp-sdk

