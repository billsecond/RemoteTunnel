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

- `--pointBPort`: (Required) Specifies the port number of the server for Remote Agent
- `--encrypted`: (Optional) Specifies whether the communication with Point B should be encrypted. Defaults to `false`.
- `--config-file`: (Required) Specifies the config file path of host mapping list

### Example
## sample config file content
``` text
"127.0.0.1:5000": "3200"
"127.0.0.1:5000": "3201"
"127.0.0.1:5100": "3202"
```
When user send request to **3200** port, then its request will be sent to **5000** port of point A Local resource.

## running server
```sh
RemoteServer.exe --pointBPort 2281 --config-file D:\conf.txt [--encrypted]
```
## Stopping the Remote Server

To stop the Remote Server, simply press `Enter` in the console window where the application is running.