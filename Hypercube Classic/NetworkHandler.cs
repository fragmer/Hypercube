﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Hypercube_Classic.Libraries;
using Hypercube_Classic.Network;
using Hypercube_Classic.Client;
using Hypercube_Classic.Core;
using Hypercube_Classic.Map;

namespace Hypercube_Classic {
    public struct NetworkSettings : ISettings {
        public string Filename { get; set; }
        public string CurrentGroup { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<string, Dictionary<string, string>> Settings { get; set; }
        public object LoadSettings { get; set; }
        public bool Save { get; set; }
    }

    /// <summary>
    /// Handles clients trying to connect, and is a container for some network settings.
    /// </summary>
    public class NetworkHandler {
        #region Variables
        public ClassicWrapped.ClassicWrapped wSock;
        public List<Client.NetworkClient> Clients;
        public TcpListener CoreListener;
        public NetworkSettings NS;
        public object WriteLock = new object();

        // -- Network Settings
        public int Port, MaxPlayers;
        public bool VerifyNames, Public, DualHeartbeat;

        Hypercube ServerCore;
        Thread ListenThread;
        #endregion

        public NetworkHandler(Hypercube Core) {
            ServerCore = Core;
            NS = new NetworkSettings();
            NS.Filename = "Network.txt";
            NS.CurrentGroup = "";
            NS.Settings = new Dictionary<string, Dictionary<string, string>>();
            NS.LoadSettings = new PBSettingsLoader.LoadSettings(LoadNetworkSettings);
            NS.Save = true;
            ServerCore.Settings.SettingsFiles.Add(NS);
            ServerCore.Settings.ReadSettings(NS);
        }

        /// <summary>
        /// Is called when network settings have been reloaded. This allows the server to reload pertinent information.
        /// </summary>
        public void LoadNetworkSettings() {
            Port = int.Parse(ServerCore.Settings.ReadSetting(NS, "Port", "25565"));
            MaxPlayers = int.Parse(ServerCore.Settings.ReadSetting(NS, "MaxPlayers", "128"));
            VerifyNames = bool.Parse(ServerCore.Settings.ReadSetting(NS, "VerifyNames", "true"));
            Public = bool.Parse(ServerCore.Settings.ReadSetting(NS, "Public", "true"));

            ServerCore.Logger._Log("Network", "Network settings loaded.", LogType.Info);
        }

        /// <summary>
        /// Starts the Server Listener
        /// </summary>
        public void Start() {
            if (ServerCore.Running) // -- If the server is currently running, stop before continuing.
                Stop();

            CoreListener = new TcpListener(IPAddress.Any, Port); // -- Creates a new listener on the server's port..
            ServerCore.Running = true;
            CoreListener.Start(); // -- Starts the listening

            ListenThread = new Thread(HandleIncoming); // -- Creates a thread to handle incoming connections.
            ListenThread.Start();

            ServerCore.Logger._Log("NetworkHandler", "Server started on port " + Port.ToString(), LogType.Info);
        }

        /// <summary>
        /// Stops the Server Listener and disconnects clients.
        /// </summary>
        public void Stop() {
            CoreListener.Stop(); // -- Stop Listening
            ServerCore.Running = false;

            if (ListenThread != null) // -- Abort the connection thread, if it is still active.
                ListenThread.Abort();

            var DisconnectPacket = new Disconnect(); // -- Send a disconnect packet to all clients that are still connected.
            DisconnectPacket.Reason = "Server closing";

            foreach (NetworkClient c in Clients) {
                DisconnectPacket.Write(c);
                c.DataRunner.Abort();
            }

            Clients.Clear(); // -- Annnd kill them. :)
        }

        /// <summary>
        /// Triggered when a client disconnects.
        /// </summary>
        public void HandleDisconnect(NetworkClient Disconnecting) {
            Clients.Remove(Disconnecting); // -- Remove them from the network's list of clients

            if (Disconnecting.CS.LoggedIn) {
                Disconnecting.CS.CurrentMap.Clients.Remove(Disconnecting);
                Disconnecting.CS.CurrentMap.DeleteEntity(ref Disconnecting.CS.MyEntity);
                ServerCore.OnlinePlayers -= 1;

                ServerCore.FreeID = Disconnecting.CS.NameID;

                var RemoveItem = new ExtRemovePlayerName();
                RemoveItem.NameID = Disconnecting.CS.NameID;

                foreach (NetworkClient c in Clients) {
                    if (c.CS.CPEExtensions.ContainsKey("ExtPlayerList"))
                        RemoveItem.Write(c);
                }
                
                ServerCore.Logger._Log("Network", "Player " + Disconnecting.CS.LoginName + " has disconnected.", LogType.Info); // -- Notify of their disconnection.
                Chat.SendGlobalChat(ServerCore, "&ePlayer " + Disconnecting.CS.FormattedName + "&e left.");
            }

            try {
                Disconnecting.BaseSocket.Close();
                Disconnecting.DataRunner.Abort();
            } catch {

            }
            
        }

        /// <summary>
        /// Handles incoming connections.
        /// </summary>
        public void HandleIncoming() {
            while (ServerCore.Running) {
                TcpClient TempClient;

                try {
                    TempClient = CoreListener.AcceptTcpClient(); // -- This will block until someone tries to connect.
                } catch {
                    continue; // -- Catches in the event of a server shutdown.
                }

                string IP = TempClient.Client.RemoteEndPoint.ToString().Substring(0, TempClient.Client.RemoteEndPoint.ToString().IndexOf(":")); // -- Strips the port the user is connecting from.

                if (IP == "0.0.0.0") { //TODO: PlayerDB IP Ban Lookup.

                }

                var NewClient = new NetworkClient(TempClient, ServerCore); // -- Creates a new network client, which will begin waiting for and parsing packets.
                NewClient.CS.IP = IP;
                Clients.Add(NewClient);

                ServerCore.Logger._Log("Network", "Client created (IP = " + NewClient.CS.IP + ")", LogType.Info);
            }
        }
    }
}
