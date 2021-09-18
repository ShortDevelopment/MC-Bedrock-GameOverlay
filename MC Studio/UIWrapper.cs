using MCWebSocket;
using MCWebSocket.MCJSON;
using MCWebSocket.PackagePayloads;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace MC_Studio
{
    public class UIWrapper
    {
        public WebView2 WebView { get; private set; }
        public Server Server { get; private set; }
        public UIBridge Bridge { get; private set; }

        public Dispatcher Dispatcher { get; private set; }

        public UIWrapper(WebView2 webview)
        {
            this.WebView = webview;
            this.Dispatcher = Dispatcher.CurrentDispatcher;
            SetServer();
        }

        public async void LoadContent(string name)
        {
            await WebView.EnsureCoreWebView2Async();

            Assembly assembly = this.GetType().Assembly;
            string id = assembly.GetManifestResourceNames().Where((x) => x.ToLower().Contains(name.ToLower())).FirstOrDefault();

            if (id == null)
                throw new FileNotFoundException();

            using (Stream stream = assembly.GetManifestResourceStream(id))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    WebView.NavigateToString(html);
                }
            }

            Bridge = new UIBridge(this);
            WebView.CoreWebView2.AddHostObjectToScript("ui_bridge", Bridge);
            WebView.CoreWebView2.AddHostObjectToScript("winrt", new WinRTProxy(WebView.CoreWebView2));
        }

        public Server SetServer(int port = 3000)
        {
            if (Server != null)
                return Server;

            Server = new Server(port);
            Server.Start();
            Server.NewClient += Server_NewClient;
            return Server;
        }

        private void Server_NewClient(object sender, ClientInstance e)
        {
            Dispatcher.Invoke(() =>
            {
                JObject json = new JObject();
                json["type"] = "event";
                json["event_type"] = "new_client";
                json["event_data"] = null;
                WebView.CoreWebView2.PostWebMessageAsJson(json.ToString());
            });
        }

        //[ClassInterface(ClassInterfaceType.AutoDual)]
        //[ComVisible(true)]
        public class UIBridge
        {
            public UIWrapper UIWrapper { get; private set; }

            public UIBridge(UIWrapper wrapper)
            {
                this.UIWrapper = wrapper;
            }

            public string StartServer(int port)
            {
                UIWrapper.SetServer(port);
                return "ok";
            }

            public async void ExecuteCommand(string cmd)
            {
                Package data = await UIWrapper.Server.Connections[0].ExecuteCommand(cmd);
                UIWrapper.Dispatcher.Invoke(() =>
                {
                    JObject json = new JObject();
                    json["type"] = "event";
                    json["event_type"] = "command_result";
                    json["event_data"] = JObject.FromObject(data);
                    UIWrapper.WebView.CoreWebView2.PostWebMessageAsJson(json.ToString());
                });
            }

            public async void GetCurrentMCPosition()
            {
                MCEntityInfo data = (await UIWrapper.Server.Connections[0].InternalCommands.QueryTarget("@s")).GetPayload<CommandResponsePayload>().GetDetails<MCEntityInfo[]>()[0];
                UIWrapper.Dispatcher.Invoke(() =>
                {
                    JObject json = new JObject();
                    json["type"] = "event";
                    json["event_type"] = "get_current_mc_position";
                    json["event_data"] = JObject.FromObject(data);
                    UIWrapper.WebView.CoreWebView2.PostWebMessageAsJson(json.ToString());
                });
            }

            public async void MoveTo(float origin_x, float origin_y, float origin_z,
                float origin_rot_x, float origin_rot_y,
                float destination_x, float destination_y, float destination_z,
                float destination_rot_x, float destination_rot_y)
            {
                Vector3 origin = new Vector3(origin_x, origin_y, origin_z);
                Vector2 origin_rot = new Vector2(origin_rot_x, origin_rot_y);

                Vector3 destination = new Vector3(destination_x, destination_y, destination_z);
                Vector2 destination_rot = new Vector2(destination_rot_x, destination_rot_y);

                Vector3 connection = destination - origin;
                float length = connection.Length();

                Vector2 connection_rot = destination_rot - origin_rot;
                float length_rot = connection_rot.Length();

                float speed = 5;
                float fps = 40;

                Stopwatch watch = new Stopwatch();

                float i = 0;

                Timer timer = new Timer();
                timer.Interval = (double)(1000F / fps);
                timer.Elapsed += delegate
                {
                    UIWrapper.Dispatcher.Invoke(() =>
                    {
                        float percentage = Math.Min(i / length, 1);
                        Vector3 pos = origin + connection * percentage;
                        Vector2 rot = origin_rot + connection_rot * percentage;

                        i += (speed * ((float)watch.ElapsedMilliseconds / 1000F));

                        if (i >= length)
                        {
                            timer.Stop();
                            return;
                        }

                        watch.Restart();

                        UIWrapper.Server.Connections[0].ExecuteCommand($"/tp {pos.X.ToString(CultureInfo.InvariantCulture)} {pos.Y.ToString(CultureInfo.InvariantCulture)} {pos.Z.ToString(CultureInfo.InvariantCulture)} {rot.Y.ToString(CultureInfo.InvariantCulture)} {rot.X.ToString(CultureInfo.InvariantCulture)} false");
                    });
                };
                timer.Start();
            }

        }

    }
}
