using MCWebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Overlay
{
    public partial class MainWindow : Window
    {

        public MapRenderer MapRenderer { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            MapRenderer = new MapRenderer(Canvas);
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var server = new Server();
            server.Start();
            server.NewClient += Server_NewClient;

            Debug.Print($"/connect localhost:{server.Port}");

            //MapRenderer.Render(new Vector2(0, 0), MapData.GetRandomData());
        }

        public ClientInstance CurrentClient { get; private set; }

        private async void Server_NewClient(object sender, ClientInstance client)
        {
            CurrentClient = client;

            client.InternalCommands.SendChatMessage("[§bBot§r]: Connected!", "Groeningen00");

            Task.Run(RenderMap);
        }

        List<int[]> mapPositions = null;

        public Dictionary<Vector2, Color> ColorCoordinatesCache { get; set; } = new Dictionary<Vector2, Color>();

        private async void RenderMap()
        {
            const int radius = 20;
            mapPositions = new List<int[]>();
            for (int x = -(radius / 2); x < (radius / 2); x++)
            {
                for (int z = -(radius / 2); z < (radius / 2); z++)
                {
                    mapPositions.Add(new[] { x, z });
                }
            }

            while (true)
            {
                try
                {
                    var mapData = new MapData();

                    var currentPlayerID = (await CurrentClient.Players.GetPlayersInfo())[0].GetID();
                    Vector2 currentPlayerPosition = new Vector2();

                    var players = (await CurrentClient.Players.GetPlayers()).ToDictionary((x) => x.GetID());
                    foreach (var playerPositionInfo in await CurrentClient.Players.GetPlayersInfo("@a"))
                    {
                        var pos = playerPositionInfo.GetPosition2D();

                        bool isCurrentPlayer = false;

                        if (playerPositionInfo.GetID() == currentPlayerID)
                        {
                            isCurrentPlayer = true;
                            currentPlayerPosition = playerPositionInfo.GetPosition2D();
                        }

                        mapData.PlayerCoordinates[pos] = new MapPlayerData(players[playerPositionInfo.GetID()], playerPositionInfo, isCurrentPlayer);
                    }

                    _ = Task.WhenAll(mapPositions.Select(async (pos) =>
                    {
                        int x = (int)currentPlayerPosition.X + pos[0];
                        int z = (int)currentPlayerPosition.Y + pos[1];
                        if (!ColorCoordinatesCache.ContainsKey(new Vector2(x, z)))
                        {
                            Package data = await CurrentClient.InternalCommands.GetTopSolidBlock($"{x}", "~", $"{z}");
                            if ((int)data.Payload["statusCode"] == 0)
                            {
                                var blockPos = new Vector2((float)data.Payload["position"]["x"], (float)data.Payload["position"]["z"]);
                                ColorCoordinatesCache[blockPos] = MCColorHelper.GetColorFromBlock(data.Payload["blockName"].ToString());
                            }
                            else
                            {
                                Debug.Print(data.Payload["statusMessage"].ToString());
                            }
                        }
                    }));

                    mapData.ColorCoordinates = ColorCoordinatesCache;

                    Dispatcher.Invoke(delegate
                    {
                        MapRenderer.Render(currentPlayerPosition, mapData);
                    });
                }
                catch (Exception)
                {
                }

                System.Threading.Thread.Sleep(100);
            }
        }
    }
}
