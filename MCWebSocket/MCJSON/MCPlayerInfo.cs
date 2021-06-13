using System;
using ColorConverter = System.Windows.Media.ColorConverter;
using WPFColor = System.Windows.Media.Color;

namespace MCWebSocket.MCJSON
{
    public struct MCPlayerInfo
    {
        public string activeSessionId;
        public string clientId;
        public string color;
        public string deviceSessionId;
        public string globalMultiplayerCorrelationId;
        public string name;
        public string randomId;
        public string uuid;

        public WPFColor GetColor()
        {
            return (WPFColor)ColorConverter.ConvertFromString($"#{color}");
        }

        public Guid GetID()
        {
            return Guid.Parse(uuid);
        }
    }
}
