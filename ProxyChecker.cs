using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

/// <summary>
/// A class to check the validity of a proxy server.
/// </summary>
public class ProxyChecker
{
    private readonly HttpClient _httpClient;
    private readonly WebProxy _proxy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyChecker"/> class.
    /// </summary>
    /// <param name="proxy">The proxy to use for the checks.</param>
    public ProxyChecker(WebProxy proxy)
    {
        _proxy = proxy;
        var handler = new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true,
        };

        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(5000);
    }

    /// <summary>
    /// Checks the proxy and returns a <see cref="ProxyInfo"/> object with the results.
    /// </summary>
    /// <returns>A <see cref="ProxyInfo"/> object with the results of the check.</returns>
    public async Task<ProxyInfo> CheckProxyAsync()
    {
        var proxyInfo = new ProxyInfo
        {
            Address = _proxy.Address.ToString(),
            IsAlive = false,
        };

        try
        {
            var response = await _httpClient.GetStringAsync("http://httpbin.org/headers");
            var headers = JsonConvert.DeserializeObject<HttpBinHeaders>(response);

            var publicIpAddress = headers.Origin;
            var headerDictionary = headers.Headers;

            proxyInfo.IsAlive = true;
            proxyInfo.OutgoingIp = publicIpAddress;

            var ipInfo = await GetIpInfoAsync(publicIpAddress);
            if (ipInfo != null)
            {
                proxyInfo.Country = ipInfo.Country;
                proxyInfo.Asn = ipInfo.Asn;
            }

            proxyInfo.Type = await GetProxyTypeAsync();
            proxyInfo.Anonymity = GetAnonymity(publicIpAddress, headerDictionary);
            proxyInfo.AdditionalHeaders = string.Join(", ", headerDictionary.Keys);
        }
        catch
        {
            // Ignore exceptions
        }

        return proxyInfo;
    }

    /// <summary>
    /// Gets the public IP address of the system.
    /// </summary>
    /// <param name="useProxy">Whether to use the proxy to get the public IP address.</param>
    /// <returns>The public IP address of the system.</returns>
    private async Task<string> GetPublicIpAddressAsync(bool useProxy)
    {
        try
        {
            var httpClient = useProxy ? _httpClient : new HttpClient();
            var response = await httpClient.GetStringAsync("https://api.ipify.org");
            return response.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the geolocation and ASN information for the specified IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to get the information for.</param>
    /// <returns>A <see cref="IpInfo"/> object with the geolocation and ASN information.</returns>
    private async Task<IpInfo> GetIpInfoAsync(string ipAddress)
    {
        try
        {
            var response = await new HttpClient().GetStringAsync($"http://ip-api.com/json/{ipAddress}");
            return JsonConvert.DeserializeObject<IpInfo>(response);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the proxy type.
    /// </summary>
    /// <returns>The proxy type.</returns>
    private async Task<string> GetProxyTypeAsync()
    {
        if (await IsSocks5ProxyAsync())
        {
            return "SOCKS5";
        }

        if (await IsHttpsProxyAsync())
        {
            return "HTTPS";
        }

        return "HTTP";
    }

    /// <summary>
    /// Checks if the proxy is a SOCKS5 proxy.
    /// </summary>
    /// <returns>True if the proxy is a SOCKS5 proxy; otherwise, false.</returns>
    private async Task<bool> IsSocks5ProxyAsync()
    {
        try
        {
            var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            await socket.ConnectAsync(_proxy.Address.Host, _proxy.Address.Port);
            var buffer = new byte[] { 5, 1, 0 };
            await socket.SendAsync(buffer, System.Net.Sockets.SocketFlags.None);
            var response = new byte[2];
            await socket.ReceiveAsync(response, System.Net.Sockets.SocketFlags.None);
            return response[0] == 5 && response[1] == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the proxy is an HTTPS proxy.
    /// </summary>
    /// <returns>True if the proxy is an HTTPS proxy; otherwise, false.</returns>
    private async Task<bool> IsHttpsProxyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the anonymity level of the proxy.
    /// </summary>
    /// <param name="publicIpAddress">The public IP address of the system.</param>
    /// <param name="headers">The headers from the proxy.</param>
    /// <returns>The anonymity level of the proxy.</returns>
    private string GetAnonymity(string publicIpAddress, Dictionary<string, string> headers)
    {
        if (headers.ContainsKey("X-Forwarded-For") || headers.ContainsKey("Via"))
        {
            var forwardedFor = headers.ContainsKey("X-Forwarded-For") ? headers["X-Forwarded-For"] : "";
            if (forwardedFor.Contains(publicIpAddress))
            {
                return "Transparent";
            }
            return "Anonymous";
        }

        return "Elite";
    }
}

/// <summary>
/// Represents the headers returned by httpbin.org/headers.
/// </summary>
public class HttpBinHeaders
{
    /// <summary>
    /// Gets or sets the origin IP address.
    /// </summary>
    [JsonProperty("origin")]
    public string Origin { get; set; }

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    [JsonProperty("headers")]
    public Dictionary<string, string> Headers { get; set; }
}

/// <summary>
/// Represents the geolocation and ASN information for an IP address.
/// </summary>
public class IpInfo
{
    /// <summary>
    /// Gets or sets the country of the IP address.
    /// </summary>
    [JsonProperty("country")]
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets the Autonomous System Number (ASN) of the IP address.
    /// </summary>
    [JsonProperty("as")]
    public string Asn { get; set; }
}
