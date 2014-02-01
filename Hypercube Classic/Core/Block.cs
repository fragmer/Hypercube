﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Hypercube_Classic.Core {
    public class BlockContainer {
        public List<Block> Blocks = new List<Block>();
        Hypercube ServerCore;

        public BlockContainer(Hypercube Core) {
            ServerCore = Core;
        }

        /// <summary>
        /// Splits a comma delimited string of rank IDs into a list of ranks.
        /// </summary>
        /// <param name="RankString"></param>
        /// <returns></returns>
        public List<Rank> SplitRanks(string RankString) {
            var result = new List<Rank>();
            var splitRanks = RankString.Split(',');

            foreach (string s in splitRanks) {
                result.Add(ServerCore.Rankholder.GetRank(int.Parse(s)));
            }

            return result;
        }

        public void LoadBlocks() {
            Blocks.Clear();
            var dt = ServerCore.Database.GetDataTable("SELECT * FROM BlockDB");

            foreach (DataRow c in dt.Rows) {
                var newBlock = new Block();
                newBlock.ID = Convert.ToInt32(c["Number"]);
                newBlock.Name = (string)c["Name"];
                newBlock.OnClient = (byte)c["OnClient"];
                newBlock.RanksPlace = SplitRanks((string)c["PlaceRank"]);
                newBlock.RanksDelete = SplitRanks((string)c["DeleteRank"]);
                newBlock.Physics = Convert.ToInt32(c["Physics"]);
                newBlock.PhysicsPlugin = (string)c["PhysicsPlugin"];
                newBlock.Kills = (bool)c["Kills"];
                newBlock.Color = Convert.ToInt32(c["Color"]);
                newBlock.CPELevel = Convert.ToInt32(c["CPELevel"]);
                newBlock.CPEReplace = Convert.ToInt32(c["CPEReplace"]);
                newBlock.Special = (bool)c["Special"];
                newBlock.ReplaceOnLoad = Convert.ToInt32(c["ReplaceOnLoad"]);

                Blocks.Add(newBlock);
            }
        }

        public void UpdateBlock(Block BlockToUpdate) {
            var MyValues = new Dictionary<string, string>();
            
        }

        public void AddBlock(string Name, byte OnClient, string PlaceRanks, string DeleteRanks, int Physics, string PhysicsPlugin, bool Kills, int Color, int CPELevel, int CPEReplace, bool Special, int ReplaceOnLoad) {
            if (ServerCore.Database.ContainsBlock(Name))
                return;

            var newBlock = new Block();
            newBlock.Name = Name;
            newBlock.OnClient = OnClient;
            newBlock.RanksPlace = SplitRanks(PlaceRanks);
            newBlock.RanksDelete = SplitRanks(DeleteRanks);
            newBlock.Physics = Physics;
            newBlock.PhysicsPlugin = PhysicsPlugin;
            newBlock.Kills = Kills;
            newBlock.Color = Color;
            newBlock.CPELevel = CPELevel;
            newBlock.CPEReplace = CPEReplace;
            newBlock.Special = Special;
            newBlock.ReplaceOnLoad = ReplaceOnLoad;

            Blocks.Add(newBlock);

            ServerCore.Database.CreateBlock(Name, OnClient, PlaceRanks, DeleteRanks, Physics, PhysicsPlugin, Kills, Color, CPELevel, CPEReplace, Special, ReplaceOnLoad);

            newBlock.ID = ServerCore.Database.GetDatabaseInt(Name, "BlockDB", "ID");
        }

        public void DeleteBlock(int ID) {
            Block ToDelete = null;

            foreach (Block b in Blocks) {
                if (b.ID == ID) {
                    ToDelete = b;
                    break;
                }
            }

            if (ToDelete != null) {
                Blocks.Remove(ToDelete);
                ServerCore.Database.Delete("BlockDB", "Number=" + ID.ToString());
            }
        }

        public void DeleteBlock(string Name) {
            Block ToDelete = null;

            foreach (Block b in Blocks) {
                if (b.Name.ToLower() == Name.ToLower()) {
                    ToDelete = b;
                    break;
                }
            }

            if (ToDelete != null) {
                Blocks.Remove(ToDelete);
                ServerCore.Database.Delete("BlockDB", "Name='" + Name + "'");
            }
        }
    }

    public class Block {
        public int ID, Physics, Color, CPELevel, CPEReplace, ReplaceOnLoad;
        public byte OnClient;
        public string Name, PhysicsPlugin;
        public bool Kills, Special;
        public List<Rank> RanksPlace = new List<Rank>();
        public List<Rank> RanksDelete = new List<Rank>();
    }
}
