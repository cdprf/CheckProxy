/// <summary>
/// Represents the information about a proxy server.
/// </summary>
public class ProxyInfo
{
    /// <summary>
    /// Gets or sets the proxy address.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the proxy type (e.g., HTTP, HTTPS, SOCKS5).
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the anonymity level (e.g., Elite, Anonymous, Transparent).
    /// </summary>
    public string Anonymity { get; set; }

    /// <summary>
    /// Gets or sets the country of the proxy.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets the Autonomous System Number (ASN) of the proxy.
    /// </summary>
    public string Asn { get; set; }

    /// <summary>
    /// Gets or sets the outgoing IP address of the proxy.
    /// </summary>
    public string OutgoingIp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the proxy is alive.
    /// </summary>
    public bool IsAlive { get; set; }

    /// <summary>
    /// Gets or sets any additional headers added by the proxy.
    /// </summary>
    public string AdditionalHeaders { get; set; }

    /// <summary>
    /// Gets or sets the latency of the proxy in milliseconds.
    /// </summary>
    public long Latency { get; set; }

    /// <summary>
    /// Gets or sets the download speed of the proxy in KB/s.
    /// </summary>
    public double DownloadSpeed { get; set; }

    /// <summary>
    /// Gets or sets the score of the proxy.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the proxy is blacklisted.
    /// </summary>
    public bool IsBlacklisted { get; set; }
}
