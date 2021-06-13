using MCWebSocket.Helpers;
using MCWebSocket.PackagePayloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCWebSocket
{
    public class ClientInstance
    {
        /// <summary>
        /// Gets the <see cref="Server"/>
        /// </summary>
        public Server Server { get; private set; }

        /// <summary>
        /// Gets the underlaying <see cref="HttpListenerWebSocketContext"/>
        /// </summary>
        public HttpListenerWebSocketContext Context { get; private set; }

        internal ClientInstance(Server server, HttpListenerWebSocketContext context)
        {
            this.Server = server;
            this.Context = context;

            Task.Run(MessageLoop);

            InitializeHelpers();
        }

        #region MessageLoop

        private async void MessageLoop()
        {
            var ws = Context.WebSocket;

            while (ws.State == WebSocketState.Open)
            {
                Package package = await ReceivePackage(ws);
                if(package != null)
                {
                    if(package.Header.Purpose == "event")
                    {
                        if (package.Payload["eventName"].ToString() != "PlayerTravelled")
                        {
                            Debug.Print(JsonConvert.SerializeObject(package));
                        }
                        ReceivedEventMessage?.Invoke(this, package);
                    }
                    else
                    {
                        if (promisQueue.ContainsKey(package.Header.ID))
                        {
                            promisQueue[package.Header.ID].SetResult(package);
                            promisQueue.Remove(package.Header.ID);
                        }
                    }                    
                }
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/23784968
        /// </summary>
        /// <param name="ws"></param>
        /// <returns></returns>
        private async Task<Package> ReceivePackage(WebSocket ws)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
            WebSocketReceiveResult result = null;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        return JsonConvert.DeserializeObject<Package>(reader.ReadToEnd());
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        /// <summary>
        /// Sends <see cref="Package"/>
        /// </summary>
        /// <param name="pack"></param>
        public async void SendPackage(Package pack)
        {
            var ws = Context.WebSocket;
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pack))), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        #region Events

        public event EventHandler<Package> ReceivedEventMessage;

        public void SubscribeEvent(string eventName)
        {
            SendPackage(new SubscribePackage(eventName));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Send a <see cref="Package"/> and returns result <see cref="Package"/> from minecraft
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public Task<Package> SendPackageWait(Package pack)
        {
            SendPackage(pack);
            var promise = new TaskCompletionSource<Package>();
            Debug.Print(pack.Header.ID);
            promisQueue.Add(pack.Header.ID, promise);
            return promise.Task;
        }

        private Dictionary<string, TaskCompletionSource<Package>> promisQueue = new Dictionary<string, TaskCompletionSource<Package>>();

        public Task<Package> ExecuteCommand(string commandLine)
        {
            return SendPackageWait(new CommandRequestPackage(commandLine));
        }

        #endregion

        #region Helpers

        private void InitializeHelpers()
        {
            InternalCommands = new InternalCommandsHelper(this);
            Players = new PlayerHelper(this);
        }

        public InternalCommandsHelper InternalCommands { get; private set; }
        public PlayerHelper Players { get; private set; }

        #endregion

    }
}
