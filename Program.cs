using Spectre.Console;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        if (IPEndPoint.TryParse(proxyAddress, out var proxyEndPoint))
        {
            var webProxy = new WebProxy(proxyAddress);
            AnsiConsole.MarkupLine("Target: [bold]" + proxyAddress + "[/]");

            var columns = new List<Markup>
            {
                new Markup("[bold]Ping Check[/]"),
                new Markup("[bold]TCP Connection[/]"),
                new Markup("[bold]HTTP Proxy Check[/]"),
            };
            AnsiConsole.Write(new Columns(columns));

            var results = await RunChecksInParallel(webProxy, proxyEndPoint, timeout);
            AnsiConsole.Write(new Columns(results.Select(r => new Markup(r.ToString()))));
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

        var columns = new List<Markup>
        {
            new Markup("[bold]Proxy[/]"),
            new Markup("[bold]Ping[/]"),
            new Markup("[bold]TCP[/]"),
            new Markup("[bold]HTTP[/]"),
        };
        AnsiConsole.Write(new Columns(columns));

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent checks

        foreach (var proxyAddress in proxies)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (IPEndPoint.TryParse(proxyAddress, out var proxyEndPoint))
                    {
                        var webProxy = new WebProxy(proxyAddress);
                        var results = await RunChecksInParallel(webProxy, proxyEndPoint, timeout);
                        AnsiConsole.Write(new Columns(
                            new Markup(proxyAddress),
                            new Markup(results[0].ToString()),
                            new Markup(results[1].ToString()),
                            new Markup(results[2].ToString())
                        ));
                    }
                    else
                    {
                        AnsiConsole.Write(new Columns(
                            new Markup(proxyAddress),
                            new Markup("[red]Invalid[/]"),
                            new Markup("[red]Invalid[/]"),
                            new Markup("[red]Invalid[/]")
                        ));
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        AnsiConsole.MarkupLine("[green]All proxy checks complete.[/]");
    }

    /// <summary>
    /// Runs a series of checks for a given proxy in parallel.
    /// </summary>
    /// <param name="wp">The WebProxy object for the proxy.</param>
    /// <param name="proxyEp">The IPEndPoint for the proxy.</param>
    /// <param name="timeout">The timeout in milliseconds for each check.</param>
    /// <returns>A boolean array indicating the result of each check.</returns>
    static async Task<bool[]> RunChecksInParallel(WebProxy wp, IPEndPoint proxyEp, int timeout)
    {
        var pingCheckTask = PingCheckAsync(proxyEp.Address.ToString(), timeout);
        var tcpConnectionTask = TcpConnectionCheckAsync(wp.Address.Host, wp.Address.Port, timeout);
        var httpProxyCheckTask = HttpProxyCheckAsync(wp, timeout);

        await Task.WhenAll(
            pingCheckTask,
            tcpConnectionTask,
            httpProxyCheckTask);

        return new[]
        {
            pingCheckTask.Result,
            tcpConnectionTask.Result,
            httpProxyCheckTask.Result,
        };
    }

    /// <summary>
    /// Performs an HTTP GET request through the proxy to check its functionality.
    /// </summary>
    /// <param name="wp">The WebProxy to use for the request.</param>
    /// <param name="timeout">The timeout in milliseconds for the request.</param>
    /// <returns>True if the request is successful; otherwise, false.</returns>
    static async Task<bool> HttpProxyCheckAsync(WebProxy wp, int timeout)
    {
        var handler = new HttpClientHandler
        {
            Proxy = wp,
            UseProxy = true,
        };

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromMilliseconds(timeout);

        try
        {
            using var response = await client.GetAsync("http://www.google.com/generate_204");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pings the specified address to check for reachability.
    /// </summary>
    /// <param name="address">The IP address or hostname to ping.</param>
    /// <param name="timeout">The timeout in milliseconds for the ping.</param>
    /// <returns>True if the ping is successful; otherwise, false.</returns>
    static async Task<bool> PingCheckAsync(string address, int timeout)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(address, timeout);
            return reply?.Status == IPStatus.Success;
        }
        catch (PingException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to open a TCP socket to the specified host and port.
    /// </summary>
    /// <param name="host">The target host.</param>
    /// <param name="port">The target port.</param>
    /// <param name="timeout">The timeout in milliseconds for the connection attempt.</param>
    /// <returns>True if the connection is successful; otherwise, false.</returns>
    static async Task<bool> TcpConnectionCheckAsync(string host, int port, int timeout)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            var connectTask = socket.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(timeout));

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return false;
            }

            await connectTask;
            return socket.Connected;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (socket.Connected)
            {
                socket.Disconnect(false);
            }
        }
    }
}