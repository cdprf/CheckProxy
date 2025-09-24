# CheckProxy

CheckProxy is a modern, fast, and efficient command-line tool for checking the validity of HTTP proxy servers. It's built with .NET 6 and leverages asynchronous operations to check proxies concurrently, providing clear and concise results.

## Features

- **High Performance**: Built with async operations to test a large number of proxies very quickly.
- **Concurrent Checks**: Uses a semaphore to control the level of parallelism, preventing system overload.
- **Flexible Input**: Test a single proxy or a list of proxies from a file.
- **Configurable Timeout**: Set a custom timeout for all checks.
- **Clear Output**: Uses Spectre.Console to display results in a clean, color-coded table.

## Installation

To use CheckProxy, you can either download a pre-built executable from the Releases page (TBD) or build it from source.

### Building from Source

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/checkproxy.git
    cd checkproxy
    ```

2.  **Build the project:**
    You will need the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) installed.
    ```bash
    dotnet build -c Release
    ```

3.  **Run the application:**
    After building, you can find the executable in the `bin/Release/net6.0` directory.
    ```bash
    ./bin/Release/net6.0/CheckProxy --help
    ```

## Usage

### Test a Single Proxy
```bash
CheckProxy 192.168.1.100:8080
```

### Test a List of Proxies from a File
Provide a text file with one proxy per line.
```bash
CheckProxy --file proxies.txt
```

### Set a Custom Timeout
You can specify a timeout in milliseconds for the checks. The default is 5000ms.
```bash
CheckProxy --file proxies.txt --timeout 10000
```

### Get Help
For a full list of commands and options, run:
```bash
CheckProxy --help
```

## Contributing

Contributions to CheckProxy are welcome! If you find a bug or have a feature request, please open an issue on the GitHub repository. Pull requests are also encouraged.

## License

CheckProxy is licensed under the [MIT License](LICENSE).
