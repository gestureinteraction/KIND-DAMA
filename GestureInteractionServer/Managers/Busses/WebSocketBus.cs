using System;
using System.Collections.Generic;
using Fleck;
using Managers.DevicesDriver;
using Utilities.Logger;
using Managers.Busses;
using GestureInteractionServer.Properties;
using Newtonsoft.Json;
using Models;

namespace Managers.Busses
{
    class WebSocketBus : Bus
    {
      
        private WebSocketServer server;
        private List<IWebSocketConnection> _clients = new List<IWebSocketConnection>();

        public WebSocketBus(String ip, String port)
        {

            uri = "ws://" + ip + ":" + port;
            server = new WebSocketServer(uri);
            //manages incoming connections
            IWebSocketConnection g_socket = null;

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    
                    g_socket = socket;

                    Logging.Instance.Information(this.GetType(), "Connected to {0}", socket.ConnectionInfo.ClientIpAddress);
                    _clients.Add(socket);

                };

                socket.OnClose = () =>
                {
                    Logging.Instance.Information(this.GetType(), "Disconnected from {0}", socket.ConnectionInfo.ClientIpAddress);
                    _clients.Remove(socket);
                    g_socket = null;
                };
            });
        }

        public override int CountClients()
        {
            return _clients.Count;
        }
        public override void Dispose()
        {
            server.Dispose();
           Logging.Instance.Information(this.GetType(), "Bus"+ uri + " disposed");
        }

        public override void receive(DeviceDriver sender_dev, string data_type, object content)
        {
            try
            {
                
                foreach (var socket in _clients)
                {
                    
                    //                    Console.WriteLine("Sending to client:" + socket.ConnectionInfo.ClientIpAddress);
                    if (data_type == Settings.Default.DEV_OUTTYPE_SKELETON)
                    {
                            socket.Send((String)content);
                            //Console.WriteLine(JsonConvert.DeserializeObject<JointsFrameElement>((String)(content)).timestamp-DateTime.UtcNow.Ticks);
                    }

                    if (data_type == Settings.Default.DEV_OUTTYPE_CURSOR)
                    {
                        socket.Send((String)content);
                    }

                    //convertire nel tipo piu idoneo
                    if (data_type == Settings.Default.DEV_OUTTYPE_RGB)
                    {
                        socket.Send((String)content);
                    }

                    //convertire nel tipo piu idoneo
                    if (data_type == Settings.Default.DEV_OUTTYPE_DEPTH)
                    {
                        socket.Send((String)content);
                    }
                }
                //manages other types of input
            }
            catch (Exception e)
            {
                //Gestire l'eccezione di invio dati in fase di chiusura della socket
                Logging.Instance.Error(this.GetType(), "Eccezione invio dati: {0}", e);
            }
        }
    }
}
