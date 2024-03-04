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
RemoteServer.exe -p <LocalPort> --pointBPort <PointBPort> [--encrypted]
```
### Options

- `-p`, `--port`: (Required) Specifies the port number of the local server.
- `--pointBPort`: (Required) Specifies the port number of the server for Remote Agent
- `--encrypted`: (Optional) Specifies whether the communication with Point B should be encrypted. Defaults to `false`.

### Example

```sh
RemoteServer.exe -p 2282 --pointBPort 2281 [--encrypted]
```
If All connections are established, when user send request to 2282 port, request will be transmitted to the local server of Remote Agent
## Stopping the Remote Server

To stop the Remote Server, simply press `Enter` in the console window where the application is running.