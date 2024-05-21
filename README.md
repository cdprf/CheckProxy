# CheckProxy

CheckProxy is a command-line tool designed to quickly test the connectivity and validity of proxy servers. It simultaneously checks both SOCKS and HTTP proxy connections and displays the results clearly and concisely.


## Features

- **Proxy Testing**: Quickly test the availability and responsiveness of SOCKS and HTTP proxies.
- **Batch Testing**: Provide a list of proxy servers (e.g., from a file) and CheckProxy will test them all.
- **Proxy Extraction**: Automatically extract and combine proxy server lists from a directory of files.
- **Detailed Output**: Clearly display the proxy type, IP address, port, and connection status for each tested proxy.

## Installation

You can install CheckProxy using pip:

```
download it and put it anywhere wich in your environment path checkproxy
```

## Usage

To test a single proxy:

```
checkproxy 192.168.1.100:8080
```

To test a list of proxies from a file:

```
checkproxy proxies.txt
```

To extract and combine proxy lists from a directory:

```
checkproxy --extract-proxies  http://example.com/freeproxies -output extracted.txt
```

For more information on usage and available options, please run:

```
checkproxy --help
```

## Contributing

Contributions to CheckProxy are welcome! If you find a bug or have a feature request, please open an issue on the [GitHub repository](https://github.com/your-username/checkproxy). Pull requests are also encouraged.

## License

CheckProxy is licensed under the [MIT License](LICENSE).
