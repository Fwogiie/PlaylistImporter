using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using FMOD;
using Newtonsoft.Json.Linq;
using RewiredConsts;
using UnityEngine.Networking.PlayerConnection;
using ZeepSDK.Chat;
using ZeepSDK.ChatCommands;
using ZeepSDK.Level;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;
using ZeepSDK.Playlist;
using Action = System.Action;
using Debug = UnityEngine.Debug;
using Thread = System.Threading.Thread;

namespace PlaylistImporter;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ChatCommandApi.RegisterLocalChatCommand<GetPlaylist>();
    }
    
    public class GetPlaylist : ILocalChatCommand
    {
        public static Action OnHandle;
        public string Prefix => "/";
        public string Command => "getpl";
        public string Description => "Get the playlist you just generated!";

        public async void Handle(string arguments)
        {
            OnHandle?.Invoke();
            if (arguments.IsNullOrWhiteSpace())
            {
                ChatApi.AddLocalMessage("Missing playlist code!\nExample: /getpl 1");
            }
            else
            {
                try
                {
                    await Main(arguments);
                }
                catch (FormatException e)
                {
                    MessengerApi.LogError("An Error occurred.");
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }

    public static PlaylistSaveJSON Playlist = PlaylistApi.CreatePlaylist("Temp");
    public static IPlaylistEditor PlaylistEditor = PlaylistApi.CreateEditor(Playlist);

    static async Task Main(string plcode)
    {
        // Define the URL
        string url = $"https://fwogiiedev.com/api/playlists?plcode={plcode}";

        // Create an instance of HttpClient
        using (HttpClient client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true }))
        {
            try
            {
                // Send the GET request
                HttpResponseMessage response = await client.GetAsync(url);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // Read the response content as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // logic
                Console.WriteLine(responseBody);
                var jsonresponse = JObject.Parse(responseBody);
                PlaylistEditor.Name = jsonresponse["name"].ToString();
                PlaylistEditor.RoundLength = jsonresponse["roundLength"].ToObject<double>();
                PlaylistEditor.Shuffle = jsonresponse["shufflePlaylist"].ToObject<bool>();
                Console.WriteLine(jsonresponse["levels"]);
                foreach (var level in jsonresponse["levels"])
                {
                    PlaylistEditor.AddLevel(level["UID"].ToString(), level["Author"].ToString(), level["Name"].ToString(), level["WorkshopID"].ToObject<ulong>());
                }
                PlaylistEditor.Save();
                MessengerApi.LogSuccess($"{jsonresponse["name"]} has been successfully added to your list of playlists!");
            }
            catch (HttpRequestException e)
            {
                // Handle any errors that occur during the request
                Console.WriteLine("Request error: " + e.Message);
                MessengerApi.LogError("An Error occurred, did you make sure to use the right code?");
            }
        }
    }
}


