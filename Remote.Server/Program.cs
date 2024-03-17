using CommandLine;
using Remote.Server.Core;
using Utils;
using Newtonsoft.Json;


class Options
{
    [Option('p', "port", Required = true, HelpText = "Port number of Local Server")]
    public ushort LocalPort { get; set; }

    [Option("pointBPort", Required = true, HelpText = "Port number to accept Point A Client")]
    public ushort PointBPort { get; set; }

    [Option("encrypted", Default =false, Required = false, HelpText = "True if Point B is encrypted")]
    public bool IsEncrypted { get; set; }
    
    [Option("config-file", Required = false, HelpText = "specify the json file which represents the server mapping information")]
    public string? ConfigFilePath{ get; set; }

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
{    public string PointALocalServer { get; set; }
    public int PointALocalHost { get; set; }
    public int PointBHost { get; set; } // Assuming you might need this later
}
public class Program
{
    public static bool IsStarting;

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

    public static void Main(string[] args)
    {

        Logger.LogWatcher = Console.Out;
        EncryptService.Init();
        LocalListenServer[] localListenServerList = [];
        PointAListenServer pointAListenServer;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                List < (HostPort HostPort, int Port, string KeyName)> mappingsList=new List<(HostPort HostPort, int Port, string KeyName)>();

                try
                {
                    if (o.ConfigFilePath != null)
                    {
                        mappingsList = LoadMappingsFromFile(o.ConfigFilePath);

                        Console.WriteLine("========= Valid Mappings Loaded From Config File===========");
                        foreach (var mapping in mappingsList)
                        {
                            Console.WriteLine($"Name:{mapping.KeyName},  From: {mapping.HostPort} To: {mapping.Port}");
                        }
                        Console.WriteLine("===========================================================");
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing the config file: {ex.Message}");                    
                }
                Program.IsStarting = true;
                pointAListenServer = new PointAListenServer(o.PointBPort, o.IsEncrypted, "test", "testpassword");
                pointAListenServer.Start();
                LocalListenServer s = new LocalListenServer(null, o.LocalPort);
                s.Start(pointAListenServer);
                localListenServerList.Append(s);
                foreach (var mapping in mappingsList)
                {
                    s = new LocalListenServer(mapping.HostPort, mapping.Port);
                    s.Start(pointAListenServer);
                    localListenServerList.Append(s);                    
                }                                
                Console.ReadLine();
                Program.IsStarting = false;                
                pointAListenServer.Stop();
                foreach(var ls in localListenServerList)
                {
                    ls.Stop();
                }
            });    
    
    }
}