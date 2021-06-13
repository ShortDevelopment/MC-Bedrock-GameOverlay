using Newtonsoft.Json;
using System;
using static MCWebSocket.PackagePayloads.SubscribePackage;

namespace MCWebSocket.PackagePayloads
{

    public class SubscribePackage : Package<SubscribePayload>
    {

        public SubscribePackage(string eventName)
        {
            Header.ID = Guid.NewGuid().ToString();
            Header.Type = "commandRequest";
            Header.Purpose = "subscribe";

            Payload = new SubscribePayload();
            Payload.EventName = eventName;
        }

        public class SubscribePayload
        {
            [JsonProperty("eventName")]
            public string EventName { get; set; }
        }
    }

}
