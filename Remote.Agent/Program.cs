using System.Threading;
using CommandLine;
using Remote.Agent.Core;
using Utils;


class Options
{
    [Option("pointBHost", Required = true, HelpText = "Host to connect to.")]
    public string PointBHost { get; set; }

    [Option("pointBPort", Required = true, HelpText = "Port number to connect to")]
    public int PointBPort { get; set; }

    [Option("encrypted", Default = false, Required = false, HelpText = "True if Point B is encrypted")]
    public bool IsEncrypted { get; set; }

}
internal class Program
{
    static void Main(string[] args)
    {
        Logger.LogWatcher = Console.Out;
        EncryptService.Init();
        PointAClient pointAClient;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                pointAClient = new PointAClient(o.PointBHost, o.PointBPort, o.IsEncrypted, "test","testpassword");
                ThreadPool.QueueUserWorkItem(new WaitCallback(pointAClient.Start));
                
                Console.ReadLine();
                pointAClient.Stop();
            });
    }
}
