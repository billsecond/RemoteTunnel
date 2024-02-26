using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class PointAClient
{
    private const string PointBHost = "10.0.11.1";
    private const int PointBPort = 8080;
    private const string LocalWebServerHost = "10.0.0.48";
    private const int LocalWebServerPort = 9000;
    private static ManualResetEvent exitSignal = new ManualResetEvent(false);

    public async Task Start()
    {
        while (!exitSignal.WaitOne(0))
        {
            try
            {
                using (var clientToB = new TcpClient(PointBHost, PointBPort))
                {
                    Console.WriteLine("Connected to Point B.");
                    await HandleConnection(clientToB);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Point B: {ex.Message}");
                await Task.Delay(5000); // wait before reconnecting
            }
        }
    }

    private async Task HandleConnection(TcpClient clientToB)
    {
        {
            using (var clientToLocalWebServer = new TcpClient(LocalWebServerHost, LocalWebServerPort))
            {
                Console.WriteLine("Connected to local web server.");
                var streamToB = clientToB.GetStream();
                var streamToLocalWebServer = clientToLocalWebServer.GetStream();

                // Forwarding logic here, should be modified to handle exceptions and possibly reconnect
                await Task.WhenAll(
                    ForwardStream(streamToB, streamToLocalWebServer, "BToLocalWebServer_log.txt"),
                    ForwardStream(streamToLocalWebServer, streamToB, "LocalWebServerToB_log.txt")
                );
            }
        }
    }


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

    private static readonly object _lock = new object(); // Lock for synchronization

    // Assuming the rest of the PointBServer class remains unchanged, focus on updates:

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

    public static void Main()
    {
        Console.CancelKeyPress += (sender, args) =>
        {
            Console.WriteLine("Exit signal received.");
            exitSignal.Set();
            args.Cancel = true;
        };

        var pointAClient = new PointAClient();
        pointAClient.Start().Wait(); // Start and wait for the exit signal
    }
}