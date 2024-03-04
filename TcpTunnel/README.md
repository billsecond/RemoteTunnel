# TcpTunnel

TcpTunnel is a lightweight, command-line utility designed for creating secure and encrypted tunnels between a local listener and a remote destination. It supports optional authentication and encryption to ensure secure data transmission.

## Features

- **Port Forwarding:** Forwards traffic from a local port to a remote host and port.
- **Authentication:** Supports optional username and password authentication for both the listener and the destination server.
- **Encryption:** Optional encryption for data in transit, ensuring secure communication.

## Requirements

- .NET Core 8.0 or higher
- CommandLineParser package for parsing command-line options

## Installation

Clone the repository and build the solution using .NET Core:

```bash
git clone https://github.com/your-repo/TcpTunnel.git
cd TcpTunnel
dotnet build
```

## Usage
Run the application from the command line, specifying the required and optional parameters:

```bash
dotnet run -- -p <ListenerPort> --destHost <DestinationHost> --destPort <DestinationPort> [options]
```


### Options

- `-p`, `--port`: **(Required)** Port number for the local listener.
- `-u`, `--username`: (Optional) Username for listener authentication.
- `--password`: (Optional) Password for listener authentication.
- `--destHost`: **(Required)** Hostname or IP address of the destination.
- `--destPort`: **(Required)** Port number of the destination.
- `--destUsername`: (Optional) Username for destination authentication.
- `--destPassword`: (Optional) Password for destination authentication.
- `--requireEncryption`: (Optional) Set to `true` to enable encryption for listener communication.
- `--isDestEncrypted`: (Optional) Set to `true` if the destination server's communication is encrypted.

### Example

```bash
dotnet run -- -p 8080 --destHost 192.168.1.10 --destPort 80 --requireEncryption true
```
