using System.Threading;
using CommandLine;
using Remote.Agent.Core;
using Utils;
using Newtonsoft.Json;



class Options
{
    [Option('p', "port", Required = true, HelpText = "Port number of Local Server")]
    public int LocalPort { get; set; }

    [Option('h', "host", Required = true, HelpText = "Host String of Local Server")]
    public required string LocalHost { get; set; }

    [Option("pointBHost", Required = true, HelpText = "Host to connect to.")]
    public required string PointBHost { get; set; }

    [Option("pointBPort", Required = true, HelpText = "Port number to connect to")]
    public int PointBPort { get; set; }

    [Option("encrypted", Default = false, Required = false, HelpText = "True if Point B is encrypted")]
    public bool IsEncrypted { get; set; }

    [Option("config-file", Required = false, HelpText = "specify the json file which represents the server mapping information")]
    public string? ConfigFilePath { get; set; }

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
public class HostMapConfig
{
    public string PointALocalServer { get; set; }
    public int PointALocalHost { get; set; }
    public int PointBHost { get; set; } // Assuming you might need this later
}
internal class Program
{
    public static List<(HostPort HostPort, int Port, string KeyName)> LoadMappingsFromFile(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<Dictionary<string, HostMapConfig>>(jsonContent);
            if (config == null) throw new Exception("Failed to parse JSON.");

            var mappings = new List<(HostPort HostPort, int Port, string KeyName)>();
            foreach (var entry in config)
            {
                string keyName = entry.Key;
                var details = entry.Value;

                mappings.Add((new HostPort(details.PointALocalServer, details.PointALocalHost), details.PointBHost, keyName));
            }
            return mappings;
        }
        catch (Newtonsoft.Json.JsonException je)
        {
            throw new Exception("JSON format is incorrect.", je);
        }
        catch (FileNotFoundException ex)
        {
            throw new Exception("File Not Found", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while processing the file.", ex);
        }
    }
    static void Main(string[] args)
    {
        Logger.LogWatcher = Console.Out;
        EncryptService.Init();
        PointAClient pointAClient;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                List<(HostPort HostPort, int Port, string KeyName)> mappingsList = new List<(HostPort HostPort, int Port, string KeyName)>();
                HashSet<string> mappingSet = new HashSet<string>();
                mappingSet.Add($"{o.LocalHost}:{o.LocalPort}");
                try
                {
                    if (o.ConfigFilePath != null)
                    {
                        mappingsList = LoadMappingsFromFile(o.ConfigFilePath);

                        Console.WriteLine("========= Valid Mappings Loaded From Config File===========");
                        foreach (var mapping in mappingsList)
                        {
                            Console.WriteLine($"Name:{mapping.KeyName},  From: {mapping.HostPort} To: {mapping.Port}");
                            mappingSet.Add($"{mapping.HostPort.Host}:{mapping.HostPort.Port}");
                        }
                        Console.WriteLine("===========================================================");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing the config file: {ex.Message}");
                }

                pointAClient = new PointAClient(o.PointBHost, o.PointBPort, o.LocalHost, o.LocalPort, o.IsEncrypted, "test", "testpassword",mappingSet);
                ThreadPool.QueueUserWorkItem(new WaitCallback(pointAClient.Start));
                
                Console.ReadLine();
                pointAClient.Stop();
            });
    }
}
