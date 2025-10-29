using Spectre.Console;
using System.Net;
using System.CommandLine;

/// <summary>
/// Defines the entry point for the application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>An integer representing the exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var proxyArgument = new Argument<string?>("proxy", "A single proxy address (e.g., 1.2.3.4:8080).");
        var fileOption = new Option<FileInfo?>("--file", "A file containing a list of proxies, one per line.");
        var timeoutOption = new Option<int>("--timeout", () => 5000, "Timeout in milliseconds for each check.");

        var rootCommand = new RootCommand("CheckProxy - A tool to check the validity of proxy servers.")
        {
            proxyArgument,
            fileOption,
            timeoutOption
        };

        rootCommand.SetHandler(async (proxy, file, timeout) =>
        {
            if (proxy != null)
            {
                await CheckSingleProxy(proxy, timeout);
            }
            else if (file != null)
            {
                await CheckProxiesFromFile(file, timeout);
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error: You must provide a proxy address or a file.[/]");
            }
        }, proxyArgument, fileOption, timeoutOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Checks a single proxy address and displays the results.
    /// </summary>
    /// <param name="proxyAddress">The proxy address string (e.g., "1.2.3.4:8080").</param>
    /// <param name="timeout">The timeout in milliseconds for each check.</param>
    static async Task CheckSingleProxy(string proxyAddress, int timeout)
    {
        if (IPEndPoint.TryParse(proxyAddress, out _))
        {
            var webProxy = new WebProxy(proxyAddress);
            var checker = new ProxyChecker(webProxy);
            var proxyInfo = await checker.CheckProxyAsync();

            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Address", proxyInfo.Address ?? "N/A");
            table.AddRow("Type", proxyInfo.Type ?? "N/A");
            table.AddRow("Anonymity", proxyInfo.Anonymity ?? "N/A");
            table.AddRow("Country", proxyInfo.Country ?? "N/A");
            table.AddRow("ASN", proxyInfo.Asn ?? "N/A");
            table.AddRow("Outgoing IP", proxyInfo.OutgoingIp ?? "N/A");
            table.AddRow("Alive", proxyInfo.IsAlive ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("Additional Headers", proxyInfo.AdditionalHeaders ?? "N/A");

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid proxy address format: {proxyAddress}");
        }
    }

    /// <summary>
    /// Reads a list of proxy addresses from a file and checks them concurrently.
    /// </summary>
    /// <param name="file">The file containing the list of proxies.</param>
    /// <param name="timeout">The timeout in milliseconds for each check.</param>
    static async Task CheckProxiesFromFile(FileInfo file, int timeout)
    {
        if (!file.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {file.FullName}");
            return;
        }

        var proxies = await File.ReadAllLinesAsync(file.FullName);
        AnsiConsole.MarkupLine($"[yellow]Found {proxies.Length} proxies in {file.Name}. Starting checks...[/]");

        var table = new Table();
        table.AddColumn("Address");
        table.AddColumn("Type");
        table.AddColumn("Anonymity");
        table.AddColumn("Country");
        table.AddColumn("ASN");
        table.AddColumn("Outgoing IP");
        table.AddColumn("Alive");

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent checks

        foreach (var proxyAddress in proxies)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (IPEndPoint.TryParse(proxyAddress, out _))
                    {
                        var webProxy = new WebProxy(proxyAddress);
                        var checker = new ProxyChecker(webProxy);
                        var proxyInfo = await checker.CheckProxyAsync();

                        table.AddRow(
                            proxyInfo.Address ?? "N/A",
                            proxyInfo.Type ?? "N/A",
                            proxyInfo.Anonymity ?? "N/A",
                            proxyInfo.Country ?? "N/A",
                            proxyInfo.Asn ?? "N/A",
                            proxyInfo.OutgoingIp ?? "N/A",
                            proxyInfo.IsAlive ? "[green]Yes[/]" : "[red]No[/]"
                        );
                    }
                    else
                    {
                        table.AddRow(proxyAddress, "[red]Invalid[/]", "[red]Invalid[/]", "[red]Invalid[/]", "[red]Invalid[/]", "[red]Invalid[/]", "[red]No[/]");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[green]All proxy checks complete.[/]");
    }
}
