using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;

using Steam4NET;
using Newtonsoft.Json;

namespace CsgoLobbyJoiner
{
    class Program
    {
        static int Main(string[] args)
        {
            if (Steamworks.Load(true))
            {
                Console.WriteLine("Found Steam");
            }
            else
            {
                Console.WriteLine("Failed");
                return -1;
            }

            ISteam006 steam006 = Steamworks.CreateSteamInterface<ISteam006>();
            if (steam006 == null)
            {
                Console.WriteLine("steam006 is null !");
                return -1;
            }

            ISteamClient012 steamclient = Steamworks.CreateInterface<ISteamClient012>();
            if (steamclient == null)
            {
                Console.WriteLine("steamclient is null !");
                return -1;
            }

            IClientEngine clientengine = Steamworks.CreateInterface<IClientEngine>();
            if (clientengine == null)
            {
                Console.WriteLine("clientengine is null !");
                return -1;
            }

            int pipe = steamclient.CreateSteamPipe();
            if (pipe == 0)
            {
                Console.WriteLine("Failed to create a pipe");
                return -1;
            }

            int user = steamclient.ConnectToGlobalUser(pipe);
            if (user == 0 || user == -1)
            {
                Console.WriteLine("Failed to connect to global user");
                return -1;
            }

            var cuser = clientengine.GetIClientUser<IClientUser>(user, pipe);

            Console.WriteLine($"Your id: {cuser.GetSteamID()}");

            if (!File.Exists("apikey.txt"))
            {
                Console.WriteLine("Please get an API key from https://steamcommunity.com/dev/apikey and put it in apikey.txt next to the .exe");
                Console.ReadKey();
                return -1;
            }

            var apikey = File.ReadAllText("apikey.txt");

            long baselobby = 0;
            using (var http = new HttpClient())
            {
                while (baselobby == 0)
                {
                    var res =
                        http.GetAsync(
                            $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v1/?key={apikey}&format=json&steamids={cuser.GetSteamID().ConvertToUint64()}")
                            .Result;
                    if (res.IsSuccessStatusCode)
                    {
                        var jsonresp = JsonConvert.DeserializeObject<dynamic>(res.Content.ReadAsStringAsync().Result);
                        if (jsonresp.response?.players?.player?[0]?.lobbysteamid != null)
                        {
                            baselobby = jsonresp.response?.players?.player?[0]?.lobbysteamid;
                        }
                        else
                        {
                            Console.WriteLine("Please create a lobby in-game to start the process, retrying in 3s...");
                            Thread.Sleep(3000);
                        }

                    }
                }

            }

            Console.WriteLine($"Found your lobby, it's: {baselobby}. Starting search! ");
            for (;;)
            {
                Console.WriteLine($"Joining {baselobby}, hit a key to join the next");

                System.Diagnostics.Process.Start($"steam://joinlobby/730/{baselobby++}/{cuser.GetSteamID().ConvertToUint64()}");

                Console.ReadKey();
            }
        }
    }
}
