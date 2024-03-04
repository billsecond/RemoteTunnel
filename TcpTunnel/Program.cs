using TcpTunnel.Core;
using TcpTunnel.Utils;
using CommandLine;
namespace TcpTunnel
{
    class Options
    {        
        [Option('p', "port", Required = true, HelpText = "Port number of this Listener")]
        public int Port { get; set; }

        [Option('u', "username", Required = false, Default ="", HelpText = "Username for Listener.")]
        public string Username { get; set; }
        
        [Option("password", Required = false, Default = "", HelpText = "Password for Listener.")]
        public string Password{ get; set; }

        [Option("destHost", Required = true, HelpText = "Host to connect to.")]
        public string DestHost { get; set; }

        [Option("destPort", Required = true, HelpText = "Port number to connect to")]
        public int DestPort { get; set; }

        [Option("destUsername", Required = false, Default ="", HelpText = "Username for authentication.")]
        public string DestUsername { get; set; }

        [Option("destPassword", Required = false, Default = "", HelpText = "Password for authentication.")]
        public string DestPassword { get; set; }

        [Option("requireEncryption", Required = false, Default = false, HelpText = "True if communiation of Listener has to be encrypted ")]
        public bool RequireEncryption { get; set; }

        [Option("destEncrypted", Required = false, Default = false, HelpText = "True if communiation of Destination Server is encrypted ")]
        public bool IsDestEncrypted { get; set; }
    }
    internal class Program
    {        
        static void Main(string[] args)
        {
            Logger.LogWatcher = Console.Out;
            EncryptService.Init();
            TunnelService listenerService;
            Parser.Default.ParseArguments<Options>(args)
              .WithParsed<Options>(o =>
              {
                  listenerService = new TunnelService((ushort)o.Port,o.DestHost, (ushort)o.DestPort,o.Username, o.Password, o.DestUsername, o.DestPassword, o.RequireEncryption, o.IsDestEncrypted);
                  Console.WriteLine($"Processing file: {o.Username}");
                  listenerService.Start();
                  Console.ReadLine();
                  listenerService.Stop();
              });
        }
    }
}
