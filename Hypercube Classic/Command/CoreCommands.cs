﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Hypercube_Classic.Network;
using Hypercube_Classic.Client;
using Hypercube_Classic.Core;
using Hypercube_Classic.Map;

namespace Hypercube_Classic.Command {

    public struct AddRankCommand : Command {
        public string Command { get { return "/addrank"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SAdds a rank to a player.<br>§SUsage: /addrank [Name] [RankName]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length < 2) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp addrank.");
                return;
            }

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            var newRank = Core.Rankholder.GetRank(args[1]);

            if (newRank == null) {
                Chat.SendClientChat(Client, "&4Error: &fCould not find the rank you specified.");
                return;
            }

            //TODO: Add permissions

            var Ranks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));
            var Steps = RankContainer.SplitSteps(Core.Database.GetDatabaseString(args[0], "PlayerDB", "RankStep"));
            Ranks.Add(newRank);
            Steps.Add(0);

            string RankString = "";

            foreach (Rank r in Ranks) 
                RankString += r.ID.ToString() + ",";

            RankString = RankString.Substring(0, RankString.Length - 1);

            Core.Database.SetDatabase(args[0], "PlayerDB", "Rank", RankString);
            Core.Database.SetDatabase(args[0], "PlayerDB", "RankStep", string.Join(",", Steps.ToArray()));

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0]) {
                    c.CS.PlayerRanks = Ranks;
                    c.CS.RankSteps = Steps;
                    Chat.SendClientChat(c, "§SYou now have a rank of " + newRank.Prefix + newRank.Name + newRank.Suffix + "!");
                    c.CS.FormattedName = newRank.Prefix + c.CS.LoginName + newRank.Suffix;
                }
            }

            Chat.SendClientChat(Client, "§S" + args[0] + "'s Rank was updated.");
        }
    }
    public struct BanCommand : Command {
        public string Command { get { return "/ban"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "§SBans a player.<br>§SUsage: /Ban [Name] <Reason>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            string BanReason;

            if (args.Length == 1)
                BanReason = "Banned by staff.";
            else
                BanReason = Text2;

            Core.Logger._Log("Command", "Player " + args[0] + " was banned by " + Client.CS.LoginName + ". (" + BanReason + ")", Libraries.LogType.Info);
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was banned by " + Client.CS.FormattedName + "§S. (&f" + BanReason + "§S)");

            Core.Database.BanPlayer(args[0], BanReason, Client.CS.LoginName);

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower() && c.CS.LoggedIn) {
                    c.KickPlayer("§SBanned: " + BanReason);
                    break;
                }
            }


        }
    }
    public struct BindCommand : Command {
        public string Command { get { return "/bind"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Build"; } }
        public string Help { get { return "§SChanges the block you have bound for using /material.<br>§SUsage: /bind [material] [build material]<br>§SEx. /bind Stone, or /bind Stone Fire"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§SYour currently bound block is &f" + Client.CS.MyEntity.Boundblock + "§S.");
                Chat.SendClientChat(Client, "§SLooking for fCraft style bind? See /cmdhelp bind and /cmdhelp material.");
                return;
            } else if (args.Length == 1) {
                // -- Change the Bound block only.
                var newBlock = Core.Blockholder.GetBlock(args[0]);

                if (newBlock == null) {
                    Chat.SendClientChat(Client, "&4Error: &fCouldn't find a block called '" + args[0] + "'.");
                    return;
                }

                Client.CS.MyEntity.Boundblock = newBlock;
                Core.Database.SetDatabase(Client.CS.LoginName, "PlayerDB", "BoundBlock", newBlock.ID);
                Chat.SendClientChat(Client, "§SYour bound block is now " + newBlock.Name);

            } else if (args.Length == 2) {
                // -- Change the bound block and the current build material.
                var newBlock = Core.Blockholder.GetBlock(args[0]);

                if (newBlock == null) {
                    Chat.SendClientChat(Client, "&4Error: &fCouldn't find a block called '" + args[0] + "'.");
                    return;
                }

                var materialBlock = Core.Blockholder.GetBlock(args[1]);

                if (materialBlock == null) {
                    Chat.SendClientChat(Client, "&4Error: &fCouldn't find a block called '" + args[1] + "'.");
                    return;
                }

                Client.CS.MyEntity.Boundblock = newBlock;
                Core.Database.SetDatabase(Client.CS.LoginName, "PlayerDB", "BoundBlock", newBlock.ID);
                Chat.SendClientChat(Client, "§SYour bound block is now " + newBlock.Name);

                Client.CS.MyEntity.BuildMaterial = materialBlock;
                Chat.SendClientChat(Client, "§SYour build material is now " + materialBlock.Name);
            }
        }
    }
    public struct DelRankCommand : Command {
        public string Command { get { return "/delrank"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SRemoves a rank to a player.<br>§SUsage: /delrank [Name] [RankName]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length < 2) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp delrank.");
                return;
            }

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            var newRank = Core.Rankholder.GetRank(args[1]);

            if (newRank == null) {
                Chat.SendClientChat(Client, "&4Error: &fCould not find the rank you specified.");
                return;
            }

            //TODO: Add permissions

            var Ranks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));
            var Steps = RankContainer.SplitSteps(Core.Database.GetDatabaseString(args[0], "PlayerDB", "RankStep"));
            Steps.RemoveAt(Ranks.IndexOf(newRank));
            Ranks.Remove(newRank);

            string RankString = "";

            foreach (Rank r in Ranks)
                RankString += r.ID.ToString() + ",";

            RankString = RankString.Substring(0, RankString.Length - 1);

            Core.Database.SetDatabase(args[0], "PlayerDB", "Rank", RankString);
            Core.Database.SetDatabase(args[0], "PlayerDB", "RankStep", string.Join(",", Steps.ToArray()));

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0]) {
                    c.CS.PlayerRanks = Ranks;
                    c.CS.RankSteps = Steps;
                    Chat.SendClientChat(c, "§SYour rank of " + newRank.Prefix + newRank.Name + newRank.Suffix + " has been removed.");
                    c.CS.FormattedName = newRank.Prefix + c.CS.LoginName + newRank.Suffix;
                }
            }

            Chat.SendClientChat(Client, "§S" + args[0] + "'s Ranks were updated.");
        }
    }
    public struct GetRankCommand : Command {
        public string Command { get { return "/getrank"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SGives the rank(s) of a player.<br>§SUsage: /getrank [Name]"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            
            var PlayerRanks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));
            var PlayerSteps = RankContainer.SplitSteps(Core.Database.GetDatabaseString(args[0], "PlayerDB", "RankStep"));
            string PlayerInfo = "§SRank(s) for " + args[0] + ": ";

            foreach (Rank r in PlayerRanks)
                PlayerInfo += r.Prefix + r.Name + r.Suffix + "(" + PlayerSteps[PlayerRanks.IndexOf(r)] + "), ";

            PlayerInfo = PlayerInfo.Substring(0, PlayerInfo.Length - 1); // -- Remove the final comma.
            PlayerInfo += "<br>";

            Chat.SendClientChat(Client, PlayerInfo);
        }
    }
    public struct GlobalCommand : Command {
        public string Command { get { return "/global"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "§SAllows you to switch between chat modes.<br>§SUsage: /global (optional)[on/off]"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {

            } else if (args.Length == 1) {
                if (args[0].ToLower() == "on" || args[0].ToLower() == "true") {
                    Client.CS.Global = true;
                    Chat.SendClientChat(Client, "§SGlobal chat is now on by default.");
                    Core.Database.SetDatabase(Client.CS.LoginName, "PlayerDB", "Global", "1");
                } else if (args[0].ToLower() == "off" || args[0].ToLower() == "false") {
                    Client.CS.Global = false;
                    Chat.SendClientChat(Client, "§SGlobal chat is now off by default.");
                    Core.Database.SetDatabase(Client.CS.LoginName, "PlayerDB", "Global", "0");
                } else 
                    Chat.SendClientChat(Client, "&4Error: &fUnreconized argument '" + args[0] + "'.");
            } else 
                Chat.SendClientChat(Client, "&4Error: &fIncorrect number of arguments, see /cmdhelp global.");
            
        }
    }
    public struct KickCommand : Command {
        public string Command { get { return "/kick"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SKicks a player.<br>§SUsage: /kick [Name] <Reason>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;
            
            bool kicked = false;
            string KickReason;

            if (args.Length == 1)
                KickReason = "Kicked by staff.";
            else
                KickReason = Text2;

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower() && c.CS.LoggedIn) {
                    Core.Logger._Log("Command", "Player " + c.CS.LoginName + " was kicked by " + Client.CS.LoginName + ". (" + KickReason + ")", Libraries.LogType.Info);
                    Chat.SendGlobalChat(Core, "§SPlayer " + c.CS.FormattedName + "§S was kicked by " + Client.CS.FormattedName + "§S. (&f" + KickReason + "§S)");

                    c.KickPlayer("§S" + KickReason, true);

                    kicked = true;
                    break;
                }
            }

            if (!kicked)
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
        }
    }
    public struct MapCommand : Command {
        public string Command { get { return "/map"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "§STeleports you in the selected map.<br>§SUsage: /map [Name]"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp map.");
                return;
            }

            foreach (HypercubeMap m in Core.Maps) {
                if (m.Map.MapName.ToLower() == args[0].ToLower()) {
                    if (RankContainer.RankListContains(m.JoinRanks, Client.CS.PlayerRanks)) {
                        //TODO: Add vanish
                        Chat.SendMapChat(m, Core, "§SPlayer " + Client.CS.FormattedName + " §Schanged to map &f" + m.Map.MapName + ".");
                        Chat.SendMapChat(Client.CS.CurrentMap, Core, "§SPlayer " + Client.CS.FormattedName + " §Schanged to map &f" + m.Map.MapName + ".");

                        Client.CS.CurrentMap.Clients.Remove(Client);
                        Client.CS.CurrentMap.DeleteEntity(ref Client.CS.MyEntity);

                        Client.CS.CurrentMap = m;
                        m.SendMap(Client);
                        m.Clients.Add(Client);

                        Client.CS.MyEntity.X = (short)(m.Map.SpawnX * 32);
                        Client.CS.MyEntity.Y = (short)(m.Map.SpawnZ * 32);
                        Client.CS.MyEntity.Z = (short)((m.Map.SpawnY * 32) + 51);
                        Client.CS.MyEntity.Rot = m.Map.SpawnRotation;
                        Client.CS.MyEntity.Look = m.Map.SpawnLook;
                        Client.CS.MyEntity.Map = m;

                        m.SpawnEntity(Client.CS.MyEntity);
                        m.Entities.Add(Client.CS.MyEntity);
                        m.SendAllEntities(Client);

                        // -- ExtPlayerList
                        var ToRemove = new ExtRemovePlayerName(); // -- This is needed due to a client bug that doesn't update entries properly. I submitted a PR that fixes this issue, but it hasn't been pushed yet.
                        ToRemove.NameID = Client.CS.NameID;

                        var ToUpdate = new ExtAddPlayerName();
                        ToUpdate.NameID = Client.CS.NameID;
                        ToUpdate.ListName = Client.CS.FormattedName;
                        ToUpdate.PlayerName = Client.CS.LoginName;
                        ToUpdate.GroupName = Client.ServerCore.TextFormats.ExtPlayerList + Client.CS.CurrentMap.Map.MapName;
                        ToUpdate.GroupRank = 0;

                        foreach (NetworkClient c in Core.nh.Clients) {
                            if (c.CS.CPEExtensions.ContainsKey("ExtPlayerList")) {
                                ToRemove.Write(c);
                                ToUpdate.Write(c);
                            }
                        }

                        break;
                    } else {
                        Chat.SendClientChat(Client, "&4Error: &fYou are not allowed to join this map.");
                        return;
                    }
                }
            }
        }
    }
    public struct MapAddCommand : Command {
        public string Command { get { return "/mapadd"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SAdds a new map.<br>§SUsage: /mapadd [Name]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§EThis command requires 1 argument. See /cmdhelp mapadd for usage.");
                return;
            }

            var NewMap = new HypercubeMap(Core, "Maps/" + args[0] + ".cw", args[0], 64, 64, 64);
            Core.Maps.Add(NewMap);

            Chat.SendClientChat(Client, "§SMap added successfully.");
        }
    }
    public struct MapsCommand : Command {
        public string Command { get { return "/maps"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "§SGives a list of available maps."; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            string MapString = "§SMaps:<br>";

            foreach (HypercubeMap m in Core.Maps) {
                if (RankContainer.RankListContains(m.ShowRanks, Client.CS.PlayerRanks)) {
                    MapString += "§S" + m.Map.MapName + " §D ";
                }
            }

            Chat.SendClientChat(Client, MapString);
        }
    }
    public struct MapFillCommand : Command {
        public string Command { get { return "/mapfill"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SFills the map you are in.<br>§SUsage: /mapfill [Script] <Arguments>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§EThis command requires 1 or more arguments.<br>See /cmdhelp mapfill.");
                return;
            }

            if (!Core.MapFills.MapFills.ContainsKey(args[0])) {
                Chat.SendClientChat(Client, "§EMapfill '" + args[0] + "' not found. See /mapfills.");
                return;
            }

            Core.MapFills.FillMap(Client.CS.CurrentMap, args[0], Text1.Split(' '));
        }
    }
    public struct MapFillsCommand : Command {
        public string Command { get { return "/mapfills"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SShow available mapfills. Use them with /Mapfill."; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            string MapFillString = "§D";

            foreach (KeyValuePair<string, IMapFill> value in Core.MapFills.MapFills)
                MapFillString += " §S" + value.Key + " §D";

            Chat.SendClientChat(Client, "§SMapFills:");
            Chat.SendClientChat(Client, MapFillString);
        }
    }
    public struct MapInfoCommand : Command {
        public string Command { get { return "/mapinfo"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SGives some information about a map."; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            Chat.SendClientChat(Client, "§SMap Name: &f" + Client.CS.CurrentMap.Map.MapName);
            Chat.SendClientChat(Client, "§SSize: &f:" + Client.CS.CurrentMap.Map.SizeX.ToString() + " x " + Client.CS.CurrentMap.Map.SizeZ.ToString() + " x " + Client.CS.CurrentMap.Map.SizeY.ToString());
            Chat.SendClientChat(Client, "§SMemory Usage (Rough): &f" + ((Client.CS.CurrentMap.Map.SizeX * Client.CS.CurrentMap.Map.SizeY * Client.CS.CurrentMap.Map.SizeZ) / 2048).ToString() + " MB");
            Chat.SendClientChat(Client, "§SBuild Ranks:");
            Chat.SendClientChat(Client, "§SJoin Ranks:");
            Chat.SendClientChat(Client, "§SShow Ranks:");
            Chat.SendClientChat(Client, "§SPhysics Enabled: &f" + Client.CS.CurrentMap.HCSettings.Physics.ToString());
            Chat.SendClientChat(Client, "§SBuilding Enabled: &f" + Client.CS.CurrentMap.HCSettings.Building.ToString());
            Chat.SendClientChat(Client, "§SMapHistory Enabled: &f" + Client.CS.CurrentMap.HCSettings.History.ToString());
            Chat.SendClientChat(Client, "§SBlocksend-Queue: &f" + Client.CS.CurrentMap.BlockchangeQueue.Count.ToString());
            Chat.SendClientChat(Client, "§SPhysics-Queue: &f" + Client.CS.CurrentMap.PhysicsQueue.Count.ToString());

        }
    }
    public struct MapLoadCommand : Command {
        public string Command { get { return "/mapload"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SLoads the map you are in.<br>§SUsage: /mapload <Name>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§EThis command requires 1 argument.<br>See /cmdhelp mapload.");
                return;
            }

            Client.CS.CurrentMap.LoadNewFile(args[0]);
            Client.CS.CurrentMap.ResendMap();
        }
    }
    public struct MapResendCommand : Command {
        public string Command { get { return "/mapresend"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SResends the map you are in."; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            Client.CS.CurrentMap.BlockchangeQueue.Clear();
            Client.CS.CurrentMap.ResendMap();
        }
    }
    public struct MapResizeCommand : Command {
        public string Command { get { return "/mapresize"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SResizes the map you are in.<br>§SUsage: /mapresize [X] [Y] [Z]<br>&cDont make smaller maps than 16x16x16, the client can crash!"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length < 3) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp mapresize.");
                return;
            }

            Client.CS.CurrentMap.ResizeMap(short.Parse(args[0]), short.Parse(args[1]), short.Parse(args[2]));
            Chat.SendClientChat(Client, "§SMap Resized.");
        }
    }
    public struct MapSaveCommand : Command {
        public string Command { get { return "/mapsave"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SSaves the map you are in.<br>§SUsage: /mapsave <Name><br>§SName is not needed."; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) 
                Client.CS.CurrentMap.SaveMap();
             else 
                Client.CS.CurrentMap.SaveMap("/Maps/" + args[0] + ".cw");
            
            Chat.SendClientChat(Client, "§SMap saved.");
        }
    }
    public struct MaterialCommand : Command {
        public string Command { get { return "/material"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Build"; } }
        public string Help { get { return "§SChanges your building material. Build it with your bound block.<br>§SYou get a list of materials with /materials<br>§SUsage: /material [material]"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§SYour build material has been reset.");
                Client.CS.MyEntity.BuildMaterial = Core.Blockholder.GetBlock("");
                return;
            }

            var newBlock = Core.Blockholder.GetBlock(args[0]);

            if (newBlock == null) {
                Chat.SendClientChat(Client, "&4Error: &fCouldn't find a block called '" + args[0] + "'.");
                return;
            }

            Client.CS.MyEntity.BuildMaterial = newBlock;
            Chat.SendClientChat(Client, "§SYour build material is now " + newBlock.Name);
        }
    }
    public struct MuteCommand : Command {
        public string Command { get { return "/mute"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SMutes a player, he can't speak now.<br>§SUsage: /mute [Name] <minutes>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            int MuteDuration;
            string muteReason = "You have been muted";

            if (args.Length == 1)
                MuteDuration = 999999;
            else if (args.Length == 2)
                MuteDuration = int.Parse(Text2);
            else {
                MuteDuration = int.Parse(args[1]);
                muteReason = Text2.Substring(Text2.IndexOf(" ") + 1, Text2.Length - (Text2.IndexOf(" ") + 1));
            }

            Core.Logger._Log("Command", "Player " + args[0] + " was muted for " + MuteDuration.ToString() + " Minutes. (" + muteReason + ")");
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was muted for " + MuteDuration.ToString() + " minutes. (&f" + muteReason + "§S)");

            var MutedUntil = DateTime.UtcNow.AddMinutes((double)MuteDuration) - Hypercube.UnixEpoch;
            
            Core.Database.MutePlayer(args[0], (int)MutedUntil.TotalSeconds, muteReason);

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower() && c.CS.LoggedIn) {
                    c.CS.MuteTime = (int)MutedUntil.TotalSeconds;
                    break;
                }
            }
        }
    }
    public struct PinfoCommand : Command {
        public string Command { get { return "/pinfo"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SGives some information about a player.<br>§SUsage: /pinfo [Name]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Chat.SendClientChat(Client, "§EUsage: /pinfo [name]");
                return;
            }

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            string PlayerInfo = "§SPlayerinfo:<br>";

            var dt = Core.Database.GetDataTable("SELECT * FROM PlayerDB WHERE Name='" + args[0] + "' LIMIT 1");
            PlayerInfo += "§SNumber: " + Core.Database.GetDatabaseInt(args[0],"PlayerDB", "Number").ToString() + "<br>";
            PlayerInfo += "§SName: " + args[0] + "<br>";

            var PlayerRanks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));
            PlayerInfo += "§SRank(s): ";

            foreach(Rank r in PlayerRanks) 
                PlayerInfo += r.Prefix + r.Name + r.Suffix + ",";

            PlayerInfo = PlayerInfo.Substring(0, PlayerInfo.Length - 1); // -- Remove the final comma.
            PlayerInfo += "<br>";
            PlayerInfo += "§SIP: " + Core.Database.GetDatabaseString(args[0], "PlayerDB", "IP") + "<br>";
            PlayerInfo += "§SLogins: " + Core.Database.GetDatabaseInt(args[0], "PlayerDB", "LoginCounter").ToString() + "<br>";
            PlayerInfo += "§SKicks: " + Core.Database.GetDatabaseInt(args[0], "PlayerDB", "KickCounter").ToString() + "( " + Core.Database.GetDatabaseString(args[0], "PlayerDB", "KickMessage") + ")<br>";

            if (Core.Database.GetDatabaseInt(args[0], "PlayerDB","Banned") > 0) 
                PlayerInfo += "§SBanned: " + Core.Database.GetDatabaseString(args[0], "PlayerDB","BanMessage") + " (" + Core.Database.GetDatabaseString(args[0], "PlayerDB","BannedBy") + ")<br>";
            
            if (Core.Database.GetDatabaseInt(args[0], "PlayerDB","Stopped") > 0)
                PlayerInfo += "§SStopped: " + Core.Database.GetDatabaseString(args[0], "PlayerDB", "StopMessage") + " (" + Core.Database.GetDatabaseString(args[0], "PlayerDB","StoppedBy") + ")<br>";

            if (Core.Database.GetDatabaseInt(args[0], "PlayerDB", "Time_Muted") > Hypercube.GetCurrentUnixTime())
                PlayerInfo += "§SMuted: "+ Core.Database.GetDatabaseString(args[0], "PlayerDB", "MuteMessage") + "<br>";

            Chat.SendClientChat(Client, PlayerInfo);
        }
    }
    public struct PlaceCommand : Command {
        public string Command { get { return "/place"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Build"; } }
        public string Help { get { return "§SPlaces a block under you. The material is your last built<br>§SUsage: /place <material>"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0) {
                Client.CS.CurrentMap.ClientChangeBlock(Client, (short)(Client.CS.MyEntity.X / 32), (short)(Client.CS.MyEntity.Y / 32), (short)((Client.CS.MyEntity.Z / 32) - 2), 1, Client.CS.MyEntity.Lastmaterial);
                Chat.SendClientChat(Client, "§SBlock placed.");
            } else if (args.Length == 1) {
                var newBlock = Core.Blockholder.GetBlock(args[0]);

                if (newBlock == null) {
                    Chat.SendClientChat(Client, "&4Error: &fCouldn't find a block called '" + args[0] + "'.");
                    return;
                }

                Client.CS.MyEntity.Lastmaterial = newBlock;
                Client.CS.CurrentMap.ClientChangeBlock(Client, (short)(Client.CS.MyEntity.X / 32), (short)(Client.CS.MyEntity.Y / 32), (short)((Client.CS.MyEntity.Z / 32) - 2), 1, Client.CS.MyEntity.Lastmaterial);
                Chat.SendClientChat(Client, "§SBlock placed.");
            }
        }
    }
    public struct PlayersCommand : Command {
        public string Command { get { return "/players"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SBans a player.<br>§SUsage: /Ban [Name] <Reason>"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (Client.CS.CPEExtensions.ContainsKey("ExtPlayerList")) {
                Chat.SendClientChat(Client, "§SIt appears your client supports CPE ExtPlayerList.<br>§STo see all online players and what map they are on, hold tab!");
                return;
            }

            string OnlineString = "§SOnline Players: " + Core.nh.Clients.Count.ToString() + "<br>";

            foreach (HypercubeMap hm in Core.Maps) {
                OnlineString += "§S" + hm.Map.MapName + "&f: ";

                foreach (NetworkClient c in hm.Clients) {
                    OnlineString += c.CS.FormattedName + " ";
                }
                OnlineString += "<br>";
            }

            Chat.SendClientChat(Client, OnlineString);
        }
    }
    public struct PushRankCommand : Command {
        public string Command { get { return "/pushrank"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SSets a rank as the player's active rank. (Sets their name color)<br>§SUsage: /pushrank [Name] [RankName]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length < 2) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp pushrank.");
                return;
            }

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            var newRank = Core.Rankholder.GetRank(args[1]);

            if (newRank == null) {
                Chat.SendClientChat(Client, "&4Error: &fCould not find the rank you specified.");
                return;
            }

            //TODO: Add permissions

            var Ranks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));

            if (!Ranks.Contains(newRank)) {
                Chat.SendClientChat(Client, "&4Error: &fPlayer '" + args[0] + "' does not have rank '" + args[1] + "'.");
                return;
            }

            var Steps = RankContainer.SplitSteps(Core.Database.GetDatabaseString(args[0], "PlayerDB", "RankStep"));
            int TempInt = Steps[Ranks.IndexOf(newRank)];
            Steps.RemoveAt(Ranks.IndexOf(newRank));
            Ranks.Remove(newRank);
            Ranks.Add(newRank);
            Steps.Add(TempInt);

            string RankString = "";

            foreach (Rank r in Ranks)
                RankString += r.ID.ToString() + ",";

            RankString = RankString.Substring(0, RankString.Length - 1);

            Core.Database.SetDatabase(args[0], "PlayerDB", "Rank", RankString);
            Core.Database.SetDatabase(args[0], "PlayerDB", "RankStep", string.Join(",", Steps.ToArray()));

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0]) {
                    c.CS.PlayerRanks = Ranks;
                    c.CS.RankSteps = Steps;
                    c.CS.FormattedName = newRank.Prefix + c.CS.LoginName + newRank.Suffix;
                }
            }

            Chat.SendClientChat(Client, "§S" + args[0] + "'s Rank was updated.");
        }
    }
    public struct RanksCommand : Command {
        public string Command { get { return "/ranks"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "&3Shows a list of all possible ranks in the server."; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            Chat.SendClientChat(Client, "§SGroups&f:");
            var GroupDict = new Dictionary<string, string>();

            foreach (Rank r in Core.Rankholder.Ranks) {
                if (GroupDict.Keys.Contains(r.Group))
                    GroupDict[r.Group] += "§S| " + r.Prefix + r.Name + r.Suffix + " ";
                else
                    GroupDict.Add(r.Group, "§S" + r.Group + "&f: " + r.Prefix + r.Name + r.Suffix + " ");
            }

            foreach (string b in GroupDict.Keys)
                Chat.SendClientChat(Client, GroupDict[b]);

        }
    }
    public struct RedoCommand : Command {
        public string Command { get { return "/redo"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Build"; } }
        public string Help { get { return "Redoes shit"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            int myInt = -999;
            if (int.TryParse(args[0], out myInt)) {
                Client.Redo(myInt);
            }
        }
    }
    public struct RulesCommand : Command {
        public string Command { get { return "/rules"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "General"; } }
        public string Help { get { return "§SShows the server rules.<br>§SUsage: /rules"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            Chat.SendClientChat(Client, "&6Server Rules:");

            for (int i = 0; i < Core.Rules.Count; i++) 
                Chat.SendClientChat(Client, "&6" + (i + 1).ToString() + ": " + Core.Rules[i]);
            
        }
    }
    public struct SetrankCommand : Command {
        public string Command { get { return "/setrank"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SChanges the step of a player's rank.<br>§SUsage: /setrank [Name] [RankName] [Step]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length < 3) {
                Chat.SendClientChat(Client, "&4Error: &fYou are missing some arguments. Look at /cmdhelp setrank.");
                return;
            }

            args[0] = Core.Database.GetPlayerName(args[0]);

            if (args[0] == "") {
                Chat.SendClientChat(Client, "§ECould not find player.");
                return;
            }

            var newRank = Core.Rankholder.GetRank(args[1]);

            if (newRank == null) {
                Chat.SendClientChat(Client, "&4Error: &fCould not find the rank you specified.");
                return;
            }
            //TODO: Add permissions
            var Ranks = RankContainer.SplitRanks(Core, Core.Database.GetDatabaseString(args[0], "PlayerDB", "Rank"));

            if (!Ranks.Contains(newRank)) {
                Chat.SendClientChat(Client, "&4Error: &fPlayer '" + args[0] + "' does not have rank '" + args[1] + "'.");
                return;
            }

            var Steps = RankContainer.SplitSteps(Core.Database.GetDatabaseString(args[0], "PlayerDB", "RankStep"));
            Steps[Ranks.IndexOf(newRank)] = int.Parse(args[2]);

            Core.Database.SetDatabase(args[0], "PlayerDB", "RankStep", string.Join(",", Steps.ToArray()));

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0]) {
                    c.CS.RankSteps = Steps;
                    Chat.SendClientChat(c, "§SYour rank of " + newRank.Prefix + newRank.Name + newRank.Suffix + " has been updated.");
                    c.CS.FormattedName = newRank.Prefix + c.CS.LoginName + newRank.Suffix;
                }
            }

            Chat.SendClientChat(Client, "§S" + args[0] + "'s Rank was updated.");
        }
    }
    public struct SetSpawnCommand : Command {
        public string Command { get { return "/setspawn"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Map"; } }
        public string Help { get { return "§SChanges the spawnpoint of the map."; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            Client.CS.CurrentMap.Map.SpawnX = (short)(Client.CS.MyEntity.X / 32);
            Client.CS.CurrentMap.Map.SpawnY = (short)(Client.CS.MyEntity.Z / 32);
            Client.CS.CurrentMap.Map.SpawnZ = (short)(Client.CS.MyEntity.Y / 32);
            Client.CS.CurrentMap.Map.SpawnLook = Client.CS.MyEntity.Look;
            Client.CS.CurrentMap.Map.SpawnRotation = Client.CS.MyEntity.Rot;
            Client.CS.CurrentMap.SaveMap();

            Chat.SendClientChat(Client, "§SSpawnpoint set.");
        }
    }
    public struct StopCommand : Command {
        public string Command { get { return "/stop"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SStops a player, he can't built now.<br>§SUsage: /Stop [Name] <Reason>"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            string StopReason = "You have been stopped.";

            if (args.Length > 1) {
                StopReason = Text2;
            }

            Core.Logger._Log("Command", "Player " + args[0] + " was stopped by " + Client.CS.LoginName + ". (" + StopReason + ")", Libraries.LogType.Info);
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was stopped by " + Client.CS.FormattedName + "§S. (&f" + StopReason + "§S)");

            Core.Database.StopPlayer(args[0], StopReason, Client.CS.LoginName);

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower()) {
                    c.CS.Stopped = true;
                    break;
                }
            }

        }
    }
    public struct UnbanCommand : Command {
        public string Command { get { return "/unban"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SUnbans a player.<br>§SUsage: /Unban [Name]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            Core.Logger._Log("Command", "Player " + args[0] + " was unbanned by " + Client.CS.LoginName + ".", Libraries.LogType.Info);
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was unbanned by " + Client.CS.FormattedName + "§S.");

            Core.Database.UnbanPlayer(args[0]);
        }
    }
    public struct UndoCommand : Command {
        public string Command { get { return "/undo"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Build"; } }
        public string Help { get { return "Undoes shit"; } }

        public string ShowRanks { get { return "1,2"; } }
        public string UseRanks { get { return "1,2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            int myInt = -999;
            if (int.TryParse(args[0], out myInt)) {
                Client.Undo(myInt);
            }
        }
    }
    public struct UnmuteCommand : Command {
        public string Command { get { return "/unmute"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SUnmutes a player, he can speak now.<br>§SUsage: /mute [Name]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            Core.Logger._Log("Command", "Player " + args[0] + " was unmuted.", Libraries.LogType.Info);
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was unmuted.");

            Core.Database.UnmutePlayer(args[0]);

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower() && c.CS.LoggedIn) {
                    c.CS.MuteTime = 0;
                    break;
                }
            }
        }
    }
    public struct UnstopCommand : Command {
        public string Command { get { return "/unstop"; } }
        public string Plugin { get { return ""; } }
        public string Group { get { return "Op"; } }
        public string Help { get { return "§SUnstops a player, he can build now.<br>§SUsage: /Unstop [Name]"; } }

        public string ShowRanks { get { return "2"; } }
        public string UseRanks { get { return "2"; } }

        public void Run(string Command, string[] args, string Text1, string Text2, Hypercube Core, NetworkClient Client) {
            if (args.Length == 0)
                return;

            if (!Core.Database.ContainsPlayer(args[0])) {
                Chat.SendClientChat(Client, "§ECould not find a user with the name '" + args[0] + "'.");
                return;
            }

            Core.Logger._Log("Command", "Player " + args[0] + " was unstopped by " + Client.CS.LoginName + ".", Libraries.LogType.Info);
            Chat.SendGlobalChat(Core, "§SPlayer " + args[0] + "§S was unstopped by " + Client.CS.FormattedName + "§S.");

            Core.Database.UnstopPlayer(args[0]);

            foreach (NetworkClient c in Core.nh.Clients) {
                if (c.CS.LoginName.ToLower() == args[0].ToLower()) {
                    c.CS.Stopped = false;
                    break;
                }
            }

        }
    }
}
