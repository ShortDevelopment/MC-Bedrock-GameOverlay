using MCWebSocket.MCJSON;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Media;

namespace Overlay
{
    public class MapData
    {
        /// <summary>
        /// Maps coordinates to color
        /// </summary>
        public Dictionary<Vector2, Color> ColorCoordinates { get; set; } = new Dictionary<Vector2, Color>();

        public Dictionary<Vector2, MapPlayerData> PlayerCoordinates { get; set; } = new Dictionary<Vector2, MapPlayerData>();

        public MapData() { }
        public MapData(Dictionary<Vector2, Color> colorCoordinates)
        {
            this.ColorCoordinates = colorCoordinates;
        }

        /// <summary>
        /// Generates random data for testing
        /// </summary>
        /// <returns></returns>
        public static MapData GetRandomData()
        {
            var data = new MapData();
            var random = new System.Random();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    data.ColorCoordinates.Add(new Vector2(x, y), Color.FromArgb(255, (byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255)));
                }
            }
            return data;
        }
    }

    public struct MapPlayerData
    {
        public string name;
        public float yRot;
        public Color color;
        public bool isCurrentPlayer;

        public MapPlayerData(MCPlayerInfo playerInfo, MCEntityInfo positionInfo, bool isCurrentPlayer = false)
        {
            name = playerInfo.name;
            yRot = (float)positionInfo.yRot;
            color = playerInfo.GetColor();
            this.isCurrentPlayer = isCurrentPlayer;
        }
    }
}
