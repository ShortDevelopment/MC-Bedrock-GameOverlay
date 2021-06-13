using MCWebSocket;
using MCWebSocket.MCJSON;
using MCWebSocket.PackagePayloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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

            //MapRenderer.Render(new Vector2(0, 0), MapData.GetRandomData());
        }

        public ClientInstance CurrentClient { get; private set; }

        private void Server_NewClient(object sender, ClientInstance client)
        {
            CurrentClient = client;
            client.InternalCommands.SendChatMessage("[§bBot§r]: Connected!", "Groeningen00");

            Task.Run(RenderMap);
        }

        private async void RenderMap()
        {
            while (true)
            {
                try
                {
                    const int radius = 15;
                    var mapData = new MapData();

                    //var tasks = new List<Task>();
                    //for (int x = -(radius / 2); x < (radius / 2); x++)
                    //{
                    //    for (int z = -(radius / 2); z < (radius / 2); z++)
                    //    {
                    //        tasks.Add(CurrentClient.InternalCommands.GetTopSolidBlock($"~{x}", "~", $"~{z}").ContinueWith((task) =>
                    //        {
                    //            var data = task.Result;
                    //            if ((int)data.Payload["statusCode"] == 0)
                    //            {
                    //                var blockPos = new Vector2((float)data.Payload["position"]["x"], (float)data.Payload["position"]["z"]);
                    //                mapData.ColorCoordinates[blockPos] = MCColorHelper.GetColorFromBlock(data.Payload["blockName"].ToString());
                    //            }
                    //        }));
                    //    }
                    //}
                    //await Task.WhenAll(tasks.ToArray());

                    var currentPlayer = (await CurrentClient.Players.GetPlayersInfo())[0];
                    Vector2 currentPlayerPosition = new Vector2();

                    var players = (await CurrentClient.Players.GetPlayers()).ToDictionary((x) => x.GetID());

                    foreach (var playerPositionInfo in await CurrentClient.Players.GetPlayersInfo("@a"))
                    {
                        var pos = playerPositionInfo.GetPosition2D();

                        bool isCurrentPlayer = false;

                        if (playerPositionInfo.GetID() == currentPlayer.GetID())
                        {
                            isCurrentPlayer = true;
                            currentPlayerPosition = playerPositionInfo.GetPosition2D();
                        }

                        mapData.PlayerCoordinates[pos] = new MapPlayerData(players[playerPositionInfo.GetID()], playerPositionInfo, isCurrentPlayer);
                    }

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
