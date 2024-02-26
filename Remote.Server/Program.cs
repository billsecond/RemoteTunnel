using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class PointBServer
{
    private const int ListenPortForA = 8080;
    private const int ListenPortForLocal = 3920;
    private TcpListener listenerForA;
    private TcpListener listenerForLocal;
    private TcpClient connectionToA;
    private readonly object lockObj = new object();

    public void Start()
    {
        listenerForA = new TcpListener(IPAddress.Any, ListenPortForA);
        listenerForA.Start();
        Console.WriteLine($"Listening for Point A on port {ListenPortForA}");

        listenerForLocal = new TcpListener(IPAddress.Any, ListenPortForLocal);
        listenerForLocal.Start();
        Console.WriteLine($"Listening for local connections on port {ListenPortForLocal}");

        AcceptConnectionsFromPointA();
        AcceptLocalConnections();
    }

    private void AcceptConnectionsFromPointA()
    {
        Task.Run(async () =>
        {
            while (true) // Continuously accept connections from Point A
            {
                try
                {
                    var clientToA = await listenerForA.AcceptTcpClientAsync();
                    Console.WriteLine("Point A connected.");

                    lock (lockObj)
                    {
                        connectionToA?.Close(); // Close existing connection if any
                        connectionToA = clientToA; // Update the connection to the new one
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting connection from Point A: {ex.Message}");
                    await Task.Delay(1000); // Wait a bit before trying again
                }
            }
        });
    }

    private void AcceptLocalConnections()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var localClient = await listenerForLocal.AcceptTcpClientAsync();
                    Console.WriteLine($"Local client connected: {localClient.Client.RemoteEndPoint}");

                    // Handle each local client connection in a separate task
                    HandleLocalClient(localClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting local client: {ex.Message}");
                }
            }
        });
    }

    private void HandleLocalClient(TcpClient localClient)
    {
        Task.Run(async () =>
        {
            try
            {
                using (localClient)
                {
                    NetworkStream localStream = localClient.GetStream();
                    NetworkStream streamToA = null;

                    // Lock to ensure exclusive access to connectionToA when checking/using it
                    lock (lockObj)
                    {
                        if (connectionToA == null || !connectionToA.Connected)
                        {
                            Console.WriteLine("No connection to Point A or it was closed.");
                            // Optionally, try to reconnect or handle the error as needed
                            return; // For now, just return. Reconnection could be attempted here if desired.
                        }
                        streamToA = connectionToA.GetStream();
                    }

                    // Use separate tasks to handle the bidirectional forwarding
                    var localToATask = ForwardStream(localStream, streamToA, "LocalToA.log");
                    var aToLocalTask = ForwardStream(streamToA, localStream, "AToLocal.log");

                    await Task.WhenAll(localToATask, aToLocalTask);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in handling local client: {ex}");
            }
        });
    }


    private static readonly object _lock = new object(); // Lock for synchronization

    private async Task ForwardStream(NetworkStream input, NetworkStream output, string logFilePath)
    {
        byte[] buffer = new byte[512];
        int bytesRead;
        while (true)
        {
            try
            {
                bytesRead = await input.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client closed the connection.");
                    break; // Exit the loop if no more data to read.
                }

                if (output.CanWrite)
                {
                    await output.WriteAsync(buffer, 0, bytesRead);
                    await output.FlushAsync();
                    LogData(buffer, bytesRead, logFilePath);
                }
                else
                {
                    Console.WriteLine("Output stream is no longer writable.");
                    break; // If we cannot write to the stream, exit the loop.
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IOException in ForwardStream: {ex.Message}. Connection might have been reset.");
                break; // Break the loop, indicating a need to possibly re-establish the connection.
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Stream disposed unexpectedly: {ex.Message}");
                break; // If the stream was disposed, exit the loop.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General exception in ForwardStream: {ex.Message}");
                break; // For any other exceptions, log and break the loop.
            }
        }
    }


    private static ManualResetEvent exitSignal = new ManualResetEvent(false);

    // Logging method
    private void LogData(byte[] data, int bytesRead, string logFileName)
    {
        string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
        try
        {
            File.AppendAllText(logFilePath, $"[{DateTime.UtcNow}] {bytesRead} bytes: {BitConverter.ToString(data, 0, bytesRead)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log data: {ex.Message}");
        }
    }

    public static void Main()
    {
        var server = new PointBServer();
        server.Start();

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Shutting down...");
            exitSignal.Set();
            e.Cancel = true; // Prevents the application from terminating immediately
        };

        // This will block the main thread until the exit signal is set
        exitSignal.WaitOne();
    }
}