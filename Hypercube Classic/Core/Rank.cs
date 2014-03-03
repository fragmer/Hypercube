﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using Hypercube_Classic.Libraries;

namespace Hypercube_Classic.Core {
    /// <summary>
    /// Contains multiple Rank objects, and contains methods for accessing their information.
    /// </summary>
    public class RankContainer {
        #region Variables
        public List<Rank> Ranks;
        #endregion

        public RankContainer(Hypercube Core) {
            Ranks = new List<Rank>();
            LoadRanks(Core);
        }

        /// <summary>
        /// Returns a rank object for modification or reference.
        /// </summary>
        /// <returns></returns>
        public Rank GetRank(int ID) {
            Rank thisRank = null;

            foreach (Rank k in Ranks) {
                if (k.ID == ID) {
                    thisRank = k;
                    break;
                }
            }

            return thisRank;
        }

        /// <summary>
        /// Returns a rank object for modification or reference.
        /// </summary>
        /// <returns></returns>
        public Rank GetRank(string Name) {
            Rank thisRank = null;

            foreach (Rank k in Ranks) {
                if (k.Name == Name) {
                    thisRank = k;
                    break;
                }
            }

            return thisRank;
        }

        /// <summary>
        /// Splits a comma delimited string of rank IDs into a list of ranks.
        /// </summary>
        /// <param name="RankString"></param>
        /// <returns></returns>
        public static List<Rank> SplitRanks(Hypercube ServerCore, string RankString) {
            var result = new List<Rank>();
            var splitRanks = RankString.Split(',');

            foreach (string s in splitRanks) 
                result.Add(ServerCore.Rankholder.GetRank(int.Parse(s)));

            return result;
        }

        /// <summary>
        /// Splits a comma delimited string of rank steps into a list of int (steps).
        /// </summary>
        /// <param name="StepString">The comma delimited string containing the steps to split.</param>
        /// <returns></returns>
        public static List<int> SplitSteps(string StepString) {
            var result = new List<int>();
            var splitSteps = StepString.Split(',');

            foreach (string s in splitSteps)
                result.Add(int.Parse(s));

            return result;
        }

        /// <summary>
        /// Searches a list of ranks to see if it contains one of the ranks in another list.
        /// </summary>
        /// <param name="Search">The list to search. if a rank from Compare is found here, the function returns true.</param>
        /// <param name="Compare">The list to iterate through and compare against Search.</param>
        /// <returns></returns>
        public static bool RankListContains(List<Rank> Search, List<Rank> Compare) {
            foreach (Rank r in Compare) {
                if (Search.Contains(r))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Triggers when the ranks file is reloaded. This will reload all ranks into the database.
        /// </summary>
        public void LoadRanks(Hypercube Core) {
            Ranks.Clear();
            var dt = Core.Database.GetDataTable("SELECT * FROM RankDB");
            
            foreach (DataRow c in dt.Rows) {
                var NewRank = new Rank();
                NewRank.ID = Convert.ToInt32(c["Number"]);
                NewRank.Name = (string)c["Name"];
                NewRank.Group = (string)c["RGroup"];
                NewRank.Prefix = (string)c["Prefix"];
                if (c["Op"].GetType() == typeof(string))
                    NewRank.Suffix = (string)c["Suffix"];
                NewRank.Op = ((long)c["Op"] > 0);
                NewRank.PointsInRank = Convert.ToInt32(c["Points"]);
                if (c["Next"].GetType() == typeof(string))
                    NewRank.NextRank = (string)c["Next"];
                
                Ranks.Add(NewRank);
            }
        }
    }

    public class Rank {
        public string Name, Prefix, Suffix, NextRank, Group;
        public int ID, PointsInRank;
        public bool Op;
        
        /// <summary>
        /// Creates a blank rank object that you can assign values to.
        /// </summary>
        public Rank() {

        }

        /// <summary>
        /// Creates a filled rank object with all fields filled.
        /// </summary>
        /// <param name="RankName">Name of the rank.</param>
        /// <param name="RankPrefix">The Prefix any user with this rank will have on their name.</param>
        /// <param name="RankSuffix">The Suffix any user with this rank will have on their name.</param>
        /// <param name="IsOp">Indicates if this user is considered an Operator or higher.</param>
        /// <param name="RankPoints">Indicates the number of points contained in this rank. When this number has been exceeded, a user will be moved to NextRank.</param>
        /// <param name="NextRankUp">The Next rank the user will acheive if they have received all points possible for this rank. Blank indicates no higher rank.</param>
        public Rank(string RankName, string RankGroup, string RankPrefix, string RankSuffix, bool IsOp, int RankPoints, string NextRankUp) {
            Name = RankName;
            Prefix = RankPrefix;
            Suffix = RankSuffix;
            Op = IsOp;
            PointsInRank = RankPoints;
            NextRank = NextRankUp;
            Group = RankGroup;
        }
    }
}
