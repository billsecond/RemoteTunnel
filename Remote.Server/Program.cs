using CommandLine;
using Remote.Server.Core;
using Utils;

class Options
{
    [Option('p', "port", Required = true, HelpText = "Port number of Local Server")]
    public ushort LocalPort { get; set; }
      
    [Option("pointBPort", Required = true, HelpText = "Port number to accept Point A Client")]
    public ushort PointBPort { get; set; }

    [Option("encrypted", Default =false, Required = false, HelpText = "True if Point B is encrypted")]
    public bool IsEncrypted { get; set; }

}
public class Program
{
    public static bool IsStarting;

    public static void Main(string[] args)
    {
        Logger.LogWatcher = Console.Out;
        EncryptService.Init();
        LocalListenServer localListenServer;
        PointAListenServer pointAListenServer;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                localListenServer = new LocalListenServer(o.LocalPort);
                pointAListenServer = new PointAListenServer(o.PointBPort, o.IsEncrypted, "test", "testpassword");
                localListenServer.Start(pointAListenServer);
                pointAListenServer.Start(localListenServer);
                Program.IsStarting = true;
                Console.ReadLine();
                Program.IsStarting = false;
                localListenServer.Stop();
                pointAListenServer.Stop();
            });    
    
    }
}