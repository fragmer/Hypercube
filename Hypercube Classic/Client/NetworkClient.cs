﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Hypercube_Classic.Network;
using Hypercube_Classic.Map;
using Hypercube_Classic.Core;

namespace Hypercube_Classic.Client {
    /// <summary>
    /// A Container for a remotely connected client. Includes user's socket and so on.
    /// </summary>
    public class NetworkClient {
        #region Variables
        public ClassicWrapped.ClassicWrapped wSock;
        public TcpClient BaseSocket;
        public NetworkStream BaseStream;
        public Thread DataRunner;
        public Thread ClientTimeout;
        public ClientSettings CS;
        public Hypercube ServerCore;
        public object WriteLock = new object();

        Dictionary<byte, Func<IPacket>> Packets;
        #endregion

        public NetworkClient(TcpClient baseSock, Hypercube Core) {
            BaseSocket = baseSock;
            BaseStream = BaseSocket.GetStream();

            ServerCore = Core;

            wSock = new ClassicWrapped.ClassicWrapped();
            wSock._Stream = BaseStream;

            Populate();

            CS = new ClientSettings();
            CS.CPEExtensions = new Dictionary<string, int>();
            CS.SelectionCuboids = new List<byte>();
            CS.LoggedIn = false;
            CS.LastActive = DateTime.UtcNow;
            CS.UndoObjects = new List<Undo>();
            CS.CurrentIndex = 0;

            DataRunner = new Thread(DataHandler);
            DataRunner.Start();

            ClientTimeout = new Thread(Timeout);
            ClientTimeout.Start();
        }

        /// <summary>
        /// As the name implies, sends a Minecraft handshake to the user. Mostly used for map sends.
        /// </summary>
        public void SendHandshake() {
            var Handshake = new Handshake();
            Handshake.Name = ServerCore.ServerName;
            Handshake.MOTD = ServerCore.MOTD;
            Handshake.ProtocolVersion = 7;

            if (CS.Op)
                Handshake.Usertype = 100;
            else
                Handshake.Usertype = 0;

            Handshake.Write(this);
        }

        public void KickPlayer(string Reason, bool IncreaseCounter = false) {
            var Disconnect = new Disconnect();
            Disconnect.Reason = Reason;
            Disconnect.Write(this);

            if (BaseSocket.Connected == true)
                BaseSocket.Close();

            BaseStream.Close();
            BaseStream.Dispose();

            if (CS.LoggedIn && IncreaseCounter) {
                var Values = new Dictionary<string, string>(); // -- Update the PlayerDB.
                Values.Add("KickCounter", (ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "KickCounter") + 1).ToString());
                Values.Add("KickMessage", Reason);
                ServerCore.Database.Update("PlayerDB", Values, "Name='" + CS.LoginName + "'");
            }

            //ServerCore.nh.HandleDisconnect(this);
        }

        public void Undo(int Steps) {
            if (Steps - 1 > (CS.UndoObjects.Count - CS.CurrentIndex))
                Steps = (CS.UndoObjects.Count - CS.CurrentIndex);

            if (CS.CurrentIndex == -1)
                return;

            for (int i = CS.CurrentIndex; i > (CS.CurrentIndex - Steps); i--)
                CS.CurrentMap.BlockChange(CS.ID, CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z, CS.UndoObjects[i].OldBlock, CS.CurrentMap.GetBlock(CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z), false, false, true, 100);

            CS.CurrentIndex -= Steps;
        }

        public void Redo(int Steps) {
            if (Steps - 1 > (CS.UndoObjects.Count - CS.CurrentIndex))
                Steps = (CS.UndoObjects.Count - CS.CurrentIndex) - 1;

            if (CS.CurrentIndex == CS.UndoObjects.Count - 1)
                return;

            if (CS.CurrentIndex == -1)
                CS.CurrentIndex = 0;

            for (int i = CS.CurrentIndex; i < (CS.CurrentIndex + Steps); i++)
                CS.CurrentMap.BlockChange(CS.ID, CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z, CS.UndoObjects[i].OldBlock, CS.CurrentMap.GetBlock(CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z), false, false, true, 100);
            
            CS.CurrentIndex += Steps;
        }
        
        /// <summary>
        /// Performs basic login functions for this client. 
        /// </summary>
        public void Login() {
            if (!ServerCore.Database.ContainsPlayer(CS.LoginName)) // -- Create the user in the PlayerDB.
                ServerCore.Database.CreatePlayer(CS.LoginName, CS.IP, ServerCore);

            CS.LoginName = ServerCore.Database.GetPlayerName(CS.LoginName);

            if ((ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "Banned") > 0)) {
                var Disconnecter = new Disconnect();

                Disconnecter.Reason = "Banned: " + ServerCore.Database.GetDatabaseString(CS.LoginName, "PlayerDB", "BanMessage");
                Disconnecter.Write(this);

                if (BaseSocket.Connected == true)
                    BaseSocket.Close();

                BaseStream.Close();
                BaseStream.Dispose();

                ServerCore.Logger._Log("Client", "Disconnecting player " + CS.LoginName + ": Player is banned.", Libraries.LogType.Info);
                return;
            }

            //TODO: Load From PlayerDB.
            CS.ID = ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "Number");
            CS.Stopped = (ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "Stopped") > 0);
            CS.Global = (ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "Global") > 0);
            CS.MuteTime = ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "Time_Muted");
            
            CS.LoggedIn = true;
            
            CS.PlayerRanks = RankContainer.SplitRanks(ServerCore, ServerCore.Database.GetDatabaseString(CS.LoginName, "PlayerDB", "Rank"));
            CS.RankSteps = RankContainer.SplitSteps(ServerCore.Database.GetDatabaseString(CS.LoginName, "PlayerDB", "RankStep"));
            CS.FormattedName = CS.PlayerRanks[CS.PlayerRanks.Count - 1].Prefix + CS.LoginName + CS.PlayerRanks[CS.PlayerRanks.Count - 1].Suffix;

            foreach (Rank r in CS.PlayerRanks) {
                if (r.Op) {
                    CS.Op = true;
                    break;
                }
            }

            ServerCore.Database.SetDatabase(CS.LoginName, "PlayerDB", "LoginCounter", (ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "LoginCounter") + 1));
            ServerCore.Database.SetDatabase(CS.LoginName, "PlayerDB", "IP", CS.IP);

            CS.CurrentMap = ServerCore.MainMap;

            // -- Finds our main map, and sends it to the client.

            CS.CurrentMap.SendMap(this);
            CS.CurrentMap.Clients.Add(this); // -- Add the client to the map

            CS.MyEntity = new Entity(ServerCore, CS.CurrentMap, CS.LoginName, (short)(CS.CurrentMap.Map.SpawnX * 32), (short)(CS.CurrentMap.Map.SpawnZ * 32), (short)((CS.CurrentMap.Map.SpawnY * 32) + 51), CS.CurrentMap.Map.SpawnRotation, CS.CurrentMap.Map.SpawnLook); // -- Create the entity..
            CS.MyEntity.MyClient = this;
            CS.MyEntity.Boundblock = ServerCore.Blockholder.GetBlock(ServerCore.Database.GetDatabaseInt(CS.LoginName, "PlayerDB", "BoundBlock") - 1);
            CS.CurrentMap.SpawnEntity(CS.MyEntity); // -- Send the client spawn to everyone.
            CS.CurrentMap.Entities.Add(CS.MyEntity); // -- Add the entity to the map so that their location will be updated.

            CS.CurrentMap.SendAllEntities(this);

            ServerCore.Logger._Log("Client", "Player logged in. (Name = " + CS.LoginName + ")", Libraries.LogType.Info);

            Chat.SendGlobalChat(ServerCore, "&ePlayer " + CS.FormattedName + "&e has joined.");
            Chat.SendClientChat(this, ServerCore.WelcomeMessage);

            CS.NameID = ServerCore.FreeID;

            if (ServerCore.FreeID != ServerCore.NextID)
                ServerCore.FreeID = ServerCore.NextID;
            else {
                ServerCore.FreeID += 1;
                ServerCore.NextID = ServerCore.FreeID;
            }

            var ExtPlayerListPacket = new ExtAddPlayerName();
            ExtPlayerListPacket.GroupRank = 0;

            foreach (NetworkClient c in ServerCore.nh.Clients) {
                if (c.CS.CPEExtensions.ContainsKey("ExtPlayerList")) {
                    if (c != this) {
                        ExtPlayerListPacket.NameID = CS.NameID;
                        ExtPlayerListPacket.ListName = CS.FormattedName;
                        ExtPlayerListPacket.PlayerName = CS.LoginName;
                        ExtPlayerListPacket.GroupName = ServerCore.TextFormats.ExtPlayerList + CS.CurrentMap.Map.MapName;
                        
                        ExtPlayerListPacket.Write(c);

                        if (CS.CPEExtensions.ContainsKey("ExtPlayerList")) {
                            ExtPlayerListPacket.NameID = c.CS.NameID;
                            ExtPlayerListPacket.ListName = c.CS.FormattedName;
                            ExtPlayerListPacket.PlayerName = c.CS.LoginName;
                            ExtPlayerListPacket.GroupName = ServerCore.TextFormats.ExtPlayerList + c.CS.CurrentMap.Map.MapName;
                            ExtPlayerListPacket.Write(this);
                        }
                    } else {
                        ExtPlayerListPacket.NameID = CS.NameID;
                        ExtPlayerListPacket.ListName = CS.FormattedName;
                        ExtPlayerListPacket.PlayerName = CS.LoginName;
                        ExtPlayerListPacket.GroupName = ServerCore.TextFormats.ExtPlayerList + CS.CurrentMap.Map.MapName;
                        ExtPlayerListPacket.Write(c);
                    }
                }
            }

            ServerCore.OnlinePlayers += 1;
        }

        /// <summary>
        /// Populates the list of accetpable packets from the client. Anything other than these will be rejected.
        /// </summary>
        void Populate() {
            Packets = new Dictionary<byte, Func<IPacket>> {
                {0, () => new Handshake()},
                {5, () => new SetBlock()},
                {8, () => new PlayerTeleport()},
                {13, () => new Message()},
                {16, () => new ExtInfo()},
                {17, () => new ExtEntry()},
                {19, () => new CustomBlockSupportLevel()}
            };
        }

        /// <summary>
        /// Blocks until data is received, then handles that data.
        /// </summary>
        void DataHandler() {
            try {
                byte PacketType = 255;

                while ((PacketType = wSock.ReadByte()) != 255) {
                    if (BaseSocket.Connected == true) {
                        if (Packets.ContainsKey(PacketType) == false) // -- Kick player, unknown packet received.
                            KickPlayer("Invalid packet received: " + PacketType.ToString());

                        CS.LastActive = DateTime.UtcNow;
                        var IncomingPacket = Packets[PacketType]();
                        IncomingPacket.Read(this);
                        IncomingPacket.Handle(this, ServerCore);
                    }
                }

            } catch (Exception e) {
                if (e.GetType() != typeof(System.IO.IOException)) {
                    ServerCore.Logger._Log("Client", e.Message, Libraries.LogType.Error);
                    ServerCore.Logger._Log("Client", e.StackTrace, Libraries.LogType.Debug);
                }

                // -- User probably disconnected.
                if (BaseSocket.Connected == true)
                    BaseSocket.Close();

                BaseStream.Close();
                BaseStream.Dispose();

                ServerCore.nh.HandleDisconnect(this);
            }
        }

        void Timeout() {
            while (BaseSocket.Connected) {

                if ((DateTime.UtcNow - CS.LastActive).Seconds > 5 && (DateTime.UtcNow - CS.LastActive).Seconds < 10) {
                    var MyPing = new Ping();
                    MyPing.Write(this);
                } else if ((DateTime.UtcNow - CS.LastActive).Seconds > 10) {
                    ServerCore.Logger._Log("Timeout", "Player " + CS.IP + " timed out.", Libraries.LogType.Info);
                    KickPlayer("Timed out");
                    return;
                }

                Thread.Sleep(500);
            }

            ServerCore.nh.HandleDisconnect(this);
        }
    }
}
