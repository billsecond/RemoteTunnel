using CommandLine;
using Remote.Server.Core;
using Utils;
using System.Text.Json;


class Options
{    
    [Option("pointBPort", Required = true, HelpText = "Port number to accept Point A Client")]
    public ushort PointBPort { get; set; }

    [Option("encrypted", Default =false, Required = false, HelpText = "True if Point B is encrypted")]
    public bool IsEncrypted { get; set; }
    
    [Option("config-file", Required = true, HelpText = "specify the json file which represents the server mapping information")]
    public string ConfigFilePath{ get; set; }

}
public struct HostPort
{
    public string Host { get; }
    public int Port { get; }

    public HostPort(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public override string ToString()
    {
        return $"{Host}:{Port}";
    }
}
public class Program
{
    public static bool IsStarting;
    static bool TryParseHostPort(string host, string portStr, out HostPort hostPort)
    {
        if (int.TryParse(portStr, out int port))
        {
            hostPort = new HostPort(host, port);
            return true;
        }

        hostPort = default;
        return false;
    }

    static List<(HostPort, int)> LoadMappingsFromFile(string filePath)
    {
        var mappingsList = new List<(HostPort, int)>();

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ":", " ","\"" }, StringSplitOptions.RemoveEmptyEntries);                
                if (parts.Length == 3 && TryParseHostPort(parts[0], parts[1], out HostPort key) && int.TryParse(parts[2], out int port))
                {
                    mappingsList.Add((key, port));
                }
                else
                {
                    Console.WriteLine($"Invalid or incomplete mapping found and skipped: {line}");
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
        }

        return mappingsList;
    }

    public static void Main(string[] args)
    {

        Logger.LogWatcher = Console.Out;
        EncryptService.Init();
        LocalListenServer[] localListenServerList = [];
        PointAListenServer pointAListenServer;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                List<(HostPort, int)> mappingsList;
                try
                {
                    mappingsList = LoadMappingsFromFile(o.ConfigFilePath);

                    Console.WriteLine("Valid Mappings Loaded:");
                    foreach (var mapping in mappingsList)
                    {
                        Console.WriteLine($"From: {mapping.Item1} To: {mapping.Item2}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing the file: {ex.Message}");
                    return ;
                }
                Program.IsStarting = true;
                pointAListenServer = new PointAListenServer(o.PointBPort, o.IsEncrypted, "test", "testpassword");
                pointAListenServer.Start();
                foreach (var mapping in mappingsList)
                {
                    LocalListenServer s = new LocalListenServer(mapping.Item1, mapping.Item2);
                    s.Start(pointAListenServer);
                    localListenServerList.Append(s);                    
                }                                
                Console.ReadLine();
                Program.IsStarting = false;                
                pointAListenServer.Stop();
                foreach(var s in localListenServerList)
                {
                    s.Stop();
                }
            });    
    
    }
}