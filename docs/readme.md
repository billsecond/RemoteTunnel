# Remote Agent

Remote Agent is a command-line application designed to facilitate secure and encrypted communication between two points (Point A and Point B) over a network. 
It first establishs **Master Connection ** to the Point B. With this MasterClient, Slave Client has been created when Point B send a request when there is a new request from user on Point B
In that way, Remote Agent process the concurrent request of users.
## Features

- **Secure Communication**: Supports encrypted communication with the remote host, ensuring that data transmitted over the network is protected against eavesdropping and tampering.
- **Command-Line Interface**: Easy-to-use command-line interface for setting up and managing the connection.
- **Flexible Configuration**: Allows configuration of local and remote host details, including host addresses, port numbers, and encryption settings.
- **Remote Local Resource Configuration**: Receive the CREATE_NEW_PROXY_BRIDGE request from PointB server and its request contains the host and port infomration of Local Resources of this Point A.
If it doesn't contains the host and port information, use the default LocalWebServerHost, LocalWebServerPort specified in arguments
## Installation

To use Remote Agent, you need to have .NET installed on your machine. After ensuring that .NET is installed, follow these steps:

1. Clone the repository or download the source code.
2. Navigate to the project directory.
3. Build the project using the .NET CLI: `dotnet build`.
4. Run the application from the build output directory.

## Usage

To start the Remote Agent, use the following command:

```sh
RemoteAgent.exe -p <LocalPort> -h <LocalHost> --pointBHost <PointBHost> --pointBPort <PointBPort> [--encrypted] [--config-file <File path>] 
```
### Options

- `-p`, `--port`: (Required) Specifies the port number of the local server.
- `-h`, `--host`: (Required) Specifies the host string of the local server.
- `--pointBHost`: (Required) Specifies the host to connect to (Point B).
- `--pointBPort`: (Required) Specifies the port number to connect to at Point B.
- `--encrypted`: (Optional) Specifies whether the communication with Point B should be encrypted. Defaults to `false`.
- `--config-file`: (Optional) Specifies the config file path of host mapping list

### Example

```sh
RemoteAgent.exe -p 8080 -h localhost --pointBHost example.com --pointBPort 2281 --encrypted
```

## Stopping the Remote Agent

To stop the Remote Agent, simply press `Enter` in the console window where the application is running.

# Remote Server

Remote Sever is a command-line application designed to tramit the request of user to the secured resource of Remote Agent.
Remote Server listens the connection from two servers, one for request of user and other one for connection from Remote Agent.
## Features

- **Secure Communication**: Supports encrypted communication with the remote host, ensuring that data transmitted over the network is protected against eavesdropping and tampering.
- **Command-Line Interface**: Easy-to-use command-line interface for setting up and managing the connection.
- **Flexible Configuration**: Allows configuration of local and remote host details, including host addresses, port numbers, and encryption settings.


## Usage

To start the Remote Server, use the following command:

```sh
RemoteServer.exe --pointBPort <PointBPort> [--encrypted] --config-file <file_path_to_config_>
```
### Options

- `-p`, `--port`: (Required) Specifies the port number of the local server.
- `--pointBPort`: (Required) Specifies the port number of the server for Remote Agent
- `--encrypted`: (Optional) Specifies whether the communication with Point B should be encrypted. Defaults to `false`.
- `--config-file`: (Optional) Specifies the config file path of host mapping list

### Example
## sample config file content
``` text
"127.0.0.1:5000": "3200"
"127.0.0.1:5000": "3201"
"127.0.0.1:5100": "3202"
```
When user send request to **3200** port, then its request will be sent to **5000** port of point A Local resource.
However, if **5000** port is not allowed in Point A, the request will be failed. You can allow it by providing the same config file on **Remote.Agent**

## running server
```sh
RemoteServer.exe -p 2282  --pointBPort 2281 --config-file D:\conf.txt
```
## Stopping the Remote Server

To stop the Remote Server, simply press `Enter` in the console window where the application is running.
