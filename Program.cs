// See https://aka.ms/new-console-template for more information
using RestSharp;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

//var arg = "188.165.0.203:8080";
string parameter;
IPEndPoint p = new IPEndPoint(127001, 1080);
string[] arguments = Environment.GetCommandLineArgs();

var columns = new List<Markup>(){
    new Markup("[bold]Pingable[/]"),
    new Markup("[bold]SoketConnect[/]"),
    new Markup("[bold]PingTcpSock[/]"),
    new Markup("[bold]TestRestCli[/]"),
    new Markup("[bold]WebClientCheck[/]"),
    new Markup("[bold]HttpClientCheck[/]")
};

if (arguments != null && arguments.Count() > 0)
{
    parameter = arguments[1].Trim();
    if (IPEndPoint.TryParse(parameter, out p))
    {
        var wp = new WebProxy(parameter);

        AnsiConsole.MarkupLine("Target: [bold]" + parameter + "[/]");
        AnsiConsole.Write(new Columns(columns));
        AnsiConsole.Write(new Columns(
            new Markup(Pingable(parameter).ToString()),
            new Markup(SoketConnect(wp.Address.Host, wp.Address.Port).ToString()),
            new Markup(PingTcpSock(wp.Address.Host.ToString(), wp.Address.Port).ToString()),
            new Markup(TestRestCli(wp).ToString()),
            new Markup(WebClientCheck(wp).ToString()),
            new Markup(HttpCliCheckAsync(wp).Result.ToString())
            ));

        //AnsiConsole.MarkupLine("[bold]Pingable : [/]" + Pingable(parameter).ToString());
        //TestProxy(wp);
        //AnsiConsole.MarkupLine("[bold]SoketConnect :[/] " + SoketConnect(wp.Address.Host, wp.Address.Port));
        //AnsiConsole.MarkupLine("[bold]PingHost: [/]" + PingHost(wp.Address.Host.ToString(), wp.Address.Port));
    }
}
else
{
    PrintHelp("no argument given.");
}

Console.ReadLine();

static void PrintHelp(string message)
{ Console.WriteLine(message); }

static void TestProxies(string proxyfile)
{
    var lowp = new List<WebProxy> { new WebProxy("1.2.3.4", 8080), new WebProxy("5.6.7.8", 80) };

    Parallel.ForEach(lowp, wp =>
    {
        TestRestCli(wp);
    });
}

static async Task<bool> HttpCliCheckAsync(WebProxy wp)
{
    // Create an HttpClientHandler object and set to use default credentials
    //HttpClientHandler handler = new HttpClientHandler();
    //handler.UseDefaultCredentials = true;

    var handler = new HttpClientHandler()
    {
        Proxy = new WebProxy(new Uri($"socks5://{wp.Address.Host}:{wp.Address.Port}")),
        UseProxy = true,
    };

    //var socketsHandler = new SocketsHttpHandler
    //{
    //    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    //    Proxy = wp,
    //    ConnectTimeout = TimeSpan.FromSeconds(30),
    //    UseProxy = true,
    //};
    //socketsHandler.Proxy= new 
    // HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
    //HttpClient client = new HttpClient(socketsHandler);
    HttpClient client = new HttpClient(handler);

    try
    {
        using HttpResponseMessage response = await client.GetAsync("http://www.contoso.com/");
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        // Above three lines can be replaced with new helper method below
        // string responseBody = await client.GetStringAsync(uri);
        //Console.WriteLine(responseBody);
        return true;
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine(e.Message);
        return false;
    }
    finally
    {
        handler.Dispose();
        client.Dispose();
    }
    
}
static bool WebClientCheck(WebProxy wp)
{
    WebClient webClient = new WebClient();
    webClient.Proxy = wp;
    webClient.BaseAddress = "http://ipmoz.com";
    try
    {
        var res = webClient.DownloadString("HTTP://ipmoz.com/");
        if (res.Contains(wp.Address.Host.ToString()))
            return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine(wp.Address.OriginalString + " : " + ex.Message);
    }
    return false;
}
static bool Pingable(string address)
{
    Ping ping = new Ping();
    try
    {
        PingReply reply = ping.Send(address, 10000);
        if (reply == null) return false;

        return (reply.Status == IPStatus.Success);
    }
    catch (PingException e)
    {
        return false;
    }
}

static bool TestRestCli(WebProxy wp)
{
    bool success = false;
    string errorMsg = "";
    var sw = new Stopwatch();
    try
    {
        sw.Start();
        var Cli = new RestClient("http://ipmoz.com");
        Cli.Options.Proxy = wp;
        Cli.Options.MaxTimeout = 20;
        Cli.Options.Timeout = 10;

        var response = Cli.Execute(new RestRequest
        {
            Resource = "/",
            Method = Method.Get,
            Timeout = 10000,
            //            RequestFormat = DataFormat.Json
        });

        if (response.ErrorException != null)
        {
            throw response.ErrorException;
        }
        success = (response.Content == wp.Address.Host);
        return true;
    }
    catch (Exception ex)
    {
        errorMsg = ex.Message;
        return false;
    }
    finally
    {
        sw.Stop();
        //return ""
        //AnsiConsole.MarkupLine("[bold]Success:[/]" + success.ToString() + " | [bold]Connection Time:[/]" + sw.Elapsed.TotalSeconds + "| [bold]ErrorMsg:[/] " + errorMsg);
    }
}

static bool SoketConnect(string host, int port)
{
    var is_success = false;
    try
    {
        var connsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        connsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 200);
        System.Threading.Thread.Sleep(500);
        var hip = IPAddress.Parse(host);
        var ipep = new IPEndPoint(hip, port);
        connsock.Connect(ipep);
        if (connsock.Connected)
        {
            is_success = true;
        }
        connsock.Close();
    }
    catch (Exception)
    {
        is_success = false;
    }
    return is_success;
}

static bool PingTcpSock(string strIP, int intPort)
{
    bool blProxy = false;
    try
    {
        TcpClient client = new TcpClient(strIP, intPort);

        blProxy = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error pinging host:'" + strIP + ":" + intPort.ToString() + "'");
        return false;
    }
    return blProxy;
}

static void ReadFile(FileInfo file)
{
    File.ReadLines(file.FullName).ToList()
        .ForEach(line => Console.WriteLine(line));
}