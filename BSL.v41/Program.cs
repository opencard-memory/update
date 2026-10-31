using System.Text;
using BSL.v41.Init;
using BSL.v41.Manage.Laser;
using BSL.v41.Supercell.Titan.GameApp.HelperZone;

namespace BSL.v41;

public abstract class Program : Initializer
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        DrawLogo();

        Custom.Stream.Program.Main(args);
        General.Program.Main(args);
        Logic.Program.Main(args);
        Manage.Program.Main(args);
        Proxy.Program.Main(args);
        Service.Program.Main(args);
        StaticService.Program.Main(args);
        Supercell.Titan.Program.Main(args);
        Titan.Program.Main(args);
        Tools.Program.Main(args);

        MapsJsonToMapStructureJson.ConvertAndWrite("SaveBase/map_structure.json");

        var loader = new Loader();
        {
            Start();

            loader.SetParameters("138.2.50.92", [9449],
                [9339]);
            loader.LoadNet();
            loader.ManageNet(); 
            loader.ManageDb();
            loader.ManageTools();
            loader.ManageAdmin();
        }
    }

    private static void DrawLogo()
    {
        const string logoText = 
            """
                                
            ██████╗░░██████╗██╗░░░░░░░░░░░██╗░░░██╗░░██╗██╗░░███╗░░
            ██╔══██╗██╔════╝██║░░░░░░░░░░░██║░░░██║░██╔╝██║░████║░░
            ██████╦╝╚█████╗░██║░░░░░█████╗╚██╗░██╔╝██╔╝░██║██╔██║░░
            ██╔══██╗░╚═══██╗██║░░░░░╚════╝░╚████╔╝░███████║╚═╝██║░░
            ██████╦╝██████╔╝███████╗░░░░░░░░╚██╔╝░░╚════██║███████╗
            ╚═════╝░╚═════╝░╚══════╝░░░░░░░░░╚═╝░░░░░░░░╚═╝╚══════╝
            """;

        var lines = logoText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var longestLineLength = lines.Select(line => line.Length).Prepend(0).Max();

        foreach (var line in lines) Console.WriteLine(line.PadLeft(longestLineLength - line.Length / 10 + line.Length - 16));
    }
}