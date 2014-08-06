﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using ClassicWrapped;

using Hypercube.Core;
using Hypercube.Network;
using Hypercube.Map;

namespace Hypercube.Client {
    public class NetworkClient {
        #region Variables
        public ClientSettings CS;

        public ClassicWrapped.ClassicWrapped wSock;
        public TcpClient BaseSocket;
        public NetworkStream BaseStream;
        public Thread DataRunner, TimeoutThread;
        public ConcurrentQueue<IPacket> SendQueue;

        Dictionary<byte, Func<IPacket>> Packets;
        public Hypercube ServerCore;
        #endregion

        public NetworkClient(TcpClient baseSock, Hypercube Core, string IP) {
            ServerCore = Core;

            CS = new ClientSettings();
            CS.LastActive = DateTime.UtcNow;
            CS.Entities = new Dictionary<int, EntityStub>();
            CS.CPEExtensions = new Dictionary<string, int>();
            CS.SelectionCuboids = new List<byte>();
            CS.LoggedIn = false;
            CS.CurrentIndex = 0;
            CS.UndoObjects = new List<Undo>();
            CS.IP = IP;

            BaseSocket = baseSock;
            BaseStream = BaseSocket.GetStream();

            wSock = new ClassicWrapped.ClassicWrapped();
            wSock._Stream = BaseStream;

            SendQueue = new ConcurrentQueue<IPacket>();
            Populate();

            DataRunner = new Thread(DataHandler);
            DataRunner.Start();
        }

        public void LoadDB() {
            CS.ID = (short)ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "Number");
            CS.Stopped = (ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "Stopped") > 0);
            CS.Global = (ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "Global") > 0);
            CS.MuteTime = ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "Time_Muted");

            CS.PlayerRanks = RankContainer.SplitRanks(ServerCore, ServerCore.DB.GetDatabaseString(CS.LoginName, "PlayerDB", "Rank"));
            CS.RankSteps = RankContainer.SplitSteps(ServerCore.DB.GetDatabaseString(CS.LoginName, "PlayerDB", "RankStep"));
            CS.FormattedName = CS.PlayerRanks[CS.PlayerRanks.Count - 1].Prefix + CS.LoginName + CS.PlayerRanks[CS.PlayerRanks.Count - 1].Suffix;

            foreach (Rank r in CS.PlayerRanks) {
                if (r.Op) {
                    CS.Op = true;
                    break;
                }
            }

            ServerCore.DB.SetDatabase(CS.LoginName, "PlayerDB", "LoginCounter", (ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "LoginCounter") + 1));
            ServerCore.DB.SetDatabase(CS.LoginName, "PlayerDB", "IP", CS.IP);
        }

        public void Login() {
            if (!ServerCore.DB.ContainsPlayer(CS.LoginName))
                ServerCore.DB.CreatePlayer(CS.LoginName, CS.IP, ServerCore);

            CS.LoginName = ServerCore.DB.GetPlayerName(CS.LoginName);

            if ((ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "Banned") > 0)) { // -- Check if the player is banned
                KickPlayer("Banned: " + ServerCore.DB.GetDatabaseString(CS.LoginName, "PlayerDB", "BanMessage"));
                ServerCore.Logger.Log("Client", "Disconnecting player " + CS.LoginName + ": Player is banned.", LogType.Info);
                return;
            }

            LoadDB(); // -- Load the user's profile

            if (ServerCore.nh.LoggedClients.ContainsKey(CS.LoginName)) {
                //TODO: This
            }

            // -- Get the user logged in to the main map.
            CS.CurrentMap = ServerCore.Maps[ServerCore.MapIndex];
            CS.CurrentMap.Send(this);

            lock (CS.CurrentMap.ClientLock) {
                CS.CurrentMap.Clients.Add(CS.ID, this);
                CS.CurrentMap.CreateList();
            }

            ServerCore.Logger.Log("Client", "Player logged in. (Name = " + CS.LoginName + ")", LogType.Info);
            ServerCore.Luahandler.RunFunction("E_PlayerLogin", this);

            Chat.SendGlobalChat(ServerCore, ServerCore.TextFormats.SystemMessage + "Player " + CS.FormattedName + ServerCore.TextFormats.SystemMessage + " has joined.");
            Chat.SendClientChat(this, ServerCore.WelcomeMessage);

            // -- Create the user's entity.
            CS.MyEntity = new Entity(ServerCore, CS.CurrentMap, CS.LoginName, (short)(CS.CurrentMap.CWMap.SpawnX * 32), 
                (short)(CS.CurrentMap.CWMap.SpawnZ * 32), (short)((CS.CurrentMap.CWMap.SpawnY * 32) + 51), CS.CurrentMap.CWMap.SpawnRotation, CS.CurrentMap.CWMap.SpawnLook);

            CS.MyEntity.MyClient = this;
            CS.MyEntity.Boundblock = ServerCore.Blockholder.GetBlock(ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "BoundBlock"));
            
            ESpawn(CS.MyEntity.Name, CS.MyEntity.CreateStub());

            lock (CS.CurrentMap.EntityLock) {
                CS.CurrentMap.Entities.Add(CS.MyEntity.ID, CS.MyEntity); // -- Add the entity to the map so that their location will be updated.
            }

            // -- CPE stuff
            CPE.SetupExtPlayerList(this);

            // -- Set the user as logged in.
            CS.LoggedIn = true;
            ServerCore.OnlinePlayers += 1;
            ServerCore.nh.LoggedClients.Add(CS.LoginName, this);
            ServerCore.nh.CreateShit();
        }

        public void SendHandshake(string MOTD = "") {
            var HS = new Handshake();
            HS.Name = ServerCore.ServerName;

            if (MOTD == "")
                HS.MOTD = ServerCore.MOTD;
            else
                HS.MOTD = MOTD;

            HS.ProtocolVersion = 7;

            if (CS.Op)
                HS.Usertype = 100;
            else
                HS.Usertype = 0;

            SendQueue.Enqueue(HS);
        }

        public void KickPlayer(string Reason, bool Log = false) {
            var DC = new Disconnect();
            DC.Reason = Reason;
            SendQueue.Enqueue(DC);

            Thread.Sleep(100);

            if (BaseSocket.Connected)
                BaseSocket.Close();

            if (CS.LoggedIn && Log) {
                ServerCore.Logger.Log("Client", CS.LoginName + " has been kicked. (" + Reason + ")", LogType.Info);
                ServerCore.Luahandler.RunFunction("E_PlayerKicked", CS.LoginName, Reason);
                Chat.SendGlobalChat(ServerCore, CS.FormattedName + ServerCore.TextFormats.SystemMessage + " has been kicked. (" + Reason + ")");

                var Values = new Dictionary<string, string>();
                Values.Add("KickCounter", (ServerCore.DB.GetDatabaseInt(CS.LoginName, "PlayerDB", "KickCounter") + 1).ToString());
                Values.Add("KickMessage", Reason);

                ServerCore.DB.Update("PlayerDB", Values, "Name='" + CS.LoginName + "'");
            }
        }

        public void HandleBlockChange(short x, short y, short z, byte mode, byte block) {
            if (CS.Stopped) {
                Chat.SendClientChat(this, "§EYou are stopped, you cannot build.");
                CS.CurrentMap.SendBlock(this, x, y, z, CS.CurrentMap.GetBlock(x, y, z));
                return;
            }

            if ((0 > x || CS.CurrentMap.CWMap.SizeX <= x) || (0 > z || CS.CurrentMap.CWMap.SizeY <= z) || (0 > y || CS.CurrentMap.CWMap.SizeZ <= y)) // -- Out of bounds check.
                return;

            var myBlock = ServerCore.Blockholder.GetBlock((int)block);

            if (myBlock == CS.MyEntity.Boundblock && CS.MyEntity.BuildMaterial.Name != "Unknown") // -- If there is a bound block, change the material.
                myBlock = CS.MyEntity.BuildMaterial;

            CS.MyEntity.Lastmaterial = myBlock;

            if (CS.MyEntity.BuildMode.Name != "") {
                CS.MyEntity.ClientState.AddBlock(x, y, z);

                if (!ServerCore.BMContainer.Modes.ContainsValue(CS.MyEntity.BuildMode)) {
                    Chat.SendClientChat(this, "§EBuild mode '" + CS.MyEntity.BuildMode + "' not found.");
                    CS.MyEntity.BuildMode = new BMStruct();
                    CS.MyEntity.BuildMode.Name = "";
                    CS.MyEntity.ClientState.ResendBlocks(this);
                    return;
                }

                ServerCore.Luahandler.RunFunction(CS.MyEntity.BuildMode.Plugin, this, CS.CurrentMap, x, y, z, mode, myBlock.ID);
            } else 
                CS.CurrentMap.ClientChangeBlock(this, x, y, z, mode, myBlock);
            
        }

        public void Redo(int steps) {
            if (steps > (CS.UndoObjects.Count - CS.CurrentIndex))
                steps = (CS.UndoObjects.Count - CS.CurrentIndex);

            if (CS.CurrentIndex == CS.UndoObjects.Count - 1)
                return;

            if (CS.CurrentIndex == -1)
                CS.CurrentIndex = 0;

            for (int i = CS.CurrentIndex; i < (CS.CurrentIndex + steps); i++)
                CS.CurrentMap.BlockChange(CS.ID, CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z, CS.UndoObjects[i].NewBlock, CS.CurrentMap.GetBlock(CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z), false, false, true, 100);

            CS.CurrentIndex += (steps - 1);
        }

        public void Undo(int steps) {
            if (steps - 1 > (CS.CurrentIndex))
                steps = (CS.CurrentIndex + 1);

            if (CS.CurrentIndex == -1)
                return;

            for (int i = CS.CurrentIndex; i > (CS.CurrentIndex - steps); i--)
                CS.CurrentMap.BlockChange(CS.ID, CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z, CS.UndoObjects[i].OldBlock, CS.CurrentMap.GetBlock(CS.UndoObjects[i].x, CS.UndoObjects[i].y, CS.UndoObjects[i].z), false, false, true, 100);

            CS.CurrentIndex -= (steps - 1);
        }

        public void ChangeMap(HypercubeMap newMap) {
            Chat.SendMapChat(newMap, ServerCore, "§SPlayer " + CS.FormattedName + " §Schanged to map &f" + newMap.CWMap.MapName + ".", 0, true);
            Chat.SendMapChat(CS.CurrentMap, ServerCore, "§SPlayer " + CS.FormattedName + " §Schanged to map &f" + newMap.CWMap.MapName + ".");
            ServerCore.Luahandler.RunFunction("E_MapChange", this, CS.CurrentMap, newMap);

            lock (CS.CurrentMap.ClientLock) {
                CS.CurrentMap.Clients.Remove(CS.ID);
                CS.CurrentMap.CreateList();
            }

            CS.CurrentMap.DeleteEntity(ref CS.MyEntity);
            CS.CurrentMap = newMap;

            newMap.Send(this);

            lock (newMap.ClientLock) {
                newMap.Clients.Add(CS.ID, this);
                newMap.CreateList();
            }

            CS.MyEntity.X = (short)(newMap.CWMap.SpawnX * 32);
            CS.MyEntity.Y = (short)(newMap.CWMap.SpawnZ * 32);
            CS.MyEntity.Z = (short)((newMap.CWMap.SpawnY * 32) + 51);
            CS.MyEntity.Rot = newMap.CWMap.SpawnRotation;
            CS.MyEntity.Look = newMap.CWMap.SpawnLook;
            CS.MyEntity.Map = newMap;

            if (newMap.FreeID != 128) {
                CS.MyEntity.ClientID = (byte)newMap.FreeID;

                if (newMap.FreeID != newMap.NextID)
                    newMap.FreeID = newMap.NextID;
                else {
                    newMap.FreeID += 1;
                    newMap.NextID = newMap.FreeID;
                }
            }

            ESpawn(CS.MyEntity.Name, CS.MyEntity.CreateStub());

            lock (newMap.EntityLock) {
                newMap.Entities.Add(CS.ID, CS.MyEntity);
            }

            CPE.UpdateExtPlayerList(this);
        }
        #region Entity Management
        void EntityPositions() {
            var Delete = new List<int>();

            foreach (EntityStub e in CS.Entities.Values) {
                if (e.Map != CS.CurrentMap) {
                    // -- Delete the entity.
                    EDelete((sbyte)e.ClientID);
                    Delete.Add(e.ID);
                    continue;
                }

                if (e.ID == CS.MyEntity.ID) {
                    // -- Delete yourself
                    EDelete((sbyte)e.ClientID);
                    Delete.Add(e.ID);
                    continue;
                }

                if (!CS.CurrentMap.Entities.ContainsKey(e.ID)) { // -- Delete old entities.
                    EDelete((sbyte)e.ClientID);
                    Delete.Add(e.ID);
                    continue;
                }

                if (!e.Spawned) { // -- Spawn an entity if it's not there yet.
                    ESpawn(CS.CurrentMap.Entities[e.ID].Name, e);
                    e.Spawned = true;
                }

                if (e.Looked) { // -- If the player looked, send them just an orient update.
                    ELook((sbyte)e.ClientID, CS.CurrentMap.Entities[e.ID].Rot, CS.CurrentMap.Entities[e.ID].Look);
                    e.Looked = false;
                }

                if (e.Changed) { // -- If they moved, send them both.
                    EFullMove(CS.CurrentMap.Entities[e.ID]);
                    e.Changed = false;
                }
            }

            foreach (int i in Delete) 
                CS.Entities.Remove(i); // -- If anyone needs to be removed, remove them. (Avoids collection modification)

            Delete = null;

            var IDs = CS.CurrentMap.Entities.Keys.ToList(); // -- Prevents need to use a lock.

            foreach (int id in IDs) {
                if (!CS.Entities.ContainsKey(id)) 
                    CS.Entities.Add(id, CS.CurrentMap.Entities[id].CreateStub()); // -- If we do not have them yet, add them!
                else {
                    if (CS.CurrentMap.Entities[id].X != CS.Entities[id].X || CS.CurrentMap.Entities[id].Y != CS.Entities[id].Y || CS.CurrentMap.Entities[id].Z != CS.Entities[id].Z) {
                        CS.Entities[id].X = CS.CurrentMap.Entities[id].X;
                        CS.Entities[id].Y = CS.CurrentMap.Entities[id].Y;
                        CS.Entities[id].Z = CS.CurrentMap.Entities[id].Z;
                        CS.Entities[id].Changed = true;
                    }

                    if (CS.CurrentMap.Entities[id].Rot != CS.Entities[id].Rot || CS.CurrentMap.Entities[id].Look != CS.Entities[id].Look) {
                        CS.Entities[id].Rot = CS.CurrentMap.Entities[id].Rot;
                        CS.Entities[id].Look = CS.CurrentMap.Entities[id].Look;

                        if (!CS.Entities[id].Changed)
                            CS.Entities[id].Looked = true;
                    }
                }
            }

            IDs = null;

            if (CS.MyEntity.SendOwn)
                EFullMove(CS.MyEntity, true);
        }

        void ESpawn(string Name, EntityStub Entity) {
            var Spawn = new SpawnPlayer();
            Spawn.PlayerName = Name;

            if (Entity.ID == CS.MyEntity.ID)
                Spawn.PlayerID = -1;
            else
                Spawn.PlayerID = (sbyte)Entity.ClientID;

            Spawn.X = Entity.X;
            Spawn.Y = Entity.Y;
            Spawn.Z = Entity.Z;
            Spawn.Yaw = Entity.Rot;
            Spawn.Pitch = Entity.Look;
            SendQueue.Enqueue(Spawn);
            //Spawn.Write(this);
        }

        void EDelete(sbyte ID) {
            var Despawn = new DespawnPlayer();
            Despawn.PlayerID = ID;
            SendQueue.Enqueue(Despawn);
        }

        void ELook(sbyte ID, byte Rot, byte Look) {
            var OUp = new OrientationUpdate();
            OUp.PlayerID = ID;
            OUp.Yaw = Rot;
            OUp.Pitch = Look;
            SendQueue.Enqueue(OUp);
        }

        void EFullMove(Entity fullEntity, bool own = false) {
            var Move = new PlayerTeleport();

            if (own)
                Move.PlayerID = (sbyte)-1;
            else
                Move.PlayerID = (sbyte)fullEntity.ClientID;

            Move.X = fullEntity.X;
            Move.Y = fullEntity.Y;
            Move.Z = fullEntity.Z;
            Move.yaw = fullEntity.Rot;
            Move.pitch = fullEntity.Look;
            SendQueue.Enqueue(Move);
        }
        #endregion
        #region Network functions
        /// <summary>
        /// Populates the list of accetpable packets from the client. Anything other than these will be rejected.
        /// </summary>
        void Populate() {
            Packets = new Dictionary<byte, Func<IPacket>> {
                {0, () => new Handshake()},
                {1, () => new Ping()},
                {5, () => new SetBlock()},
                {8, () => new PlayerTeleport()},
                {13, () => new Message()},
                {16, () => new ExtInfo()},
                {17, () => new ExtEntry()},
                {19, () => new CustomBlockSupportLevel()}
            };
        }
        void DataHandler() {
            while (BaseSocket.Connected) {
                if (BaseStream.DataAvailable) {
                    var opCode = wSock.ReadByte();

                    if (!Packets.ContainsKey(opCode)) {
                        KickPlayer("Invalid packet received.");
                        ServerCore.Logger.Log("Client", "Invalid packet received: " + opCode.ToString(), LogType.Warning);
                    }

                    CS.LastActive = DateTime.UtcNow;

                    var Incoming = Packets[opCode]();
                    Incoming.Read(this);

                    try {
                        Incoming.Handle(this, ServerCore);
                    } catch (Exception e) {
                        ServerCore.Logger.Log("Client", e.Message, LogType.Error);
                        ServerCore.Logger.Log("Client", e.StackTrace, LogType.Debug);
                    }
                }

                IPacket myPacket;

                while (SendQueue.TryDequeue(out myPacket)) {
                    myPacket.Write(this);
                }

                if ((DateTime.UtcNow - CS.LastActive).Seconds > 5 && (DateTime.UtcNow - CS.LastActive).Seconds < 10) {
                    var MyPing = new Ping();
                    MyPing.Write(this);
                } else if ((DateTime.UtcNow - CS.LastActive).Seconds > 10) {
                    ServerCore.Logger.Log("Timeout", "Player " + CS.IP + " timed out.", LogType.Info);
                    KickPlayer("Timed out");
                    return;
                }

                if (CS.LoggedIn)
                    EntityPositions();

                Thread.Sleep(0);
            }

            ServerCore.nh.HandleDisconnect(this);
        }
        #endregion
    }
}
