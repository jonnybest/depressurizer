﻿#region LICENSE

//     This file (AutoCatCurator.cs) is part of Depressurizer.
//     Copyright (C) 2011 Steve Labbe
//     Copyright (C) 2017 Theodoros Dimos
//     Copyright (C) 2017 Martijn Vegter
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using Rallion;

namespace Depressurizer
{
    public class AutoCatCurator : AutoCat
    {
        #region Constants

        // Serialization constants
        public const string TypeIdString = "AutoCatCurator";

        #endregion

        #region Fields

        private Dictionary<int, CuratorRecommendation> curatorRecommendations;

        #endregion

        #region Constructors and Destructors

        public AutoCatCurator(string name, string filter = null, string categoryName = null, string curatorUrl = null, List<CuratorRecommendation> includedRecommendations = null, bool selected = false) : base(name)
        {
            Filter = filter;
            CategoryName = categoryName;
            CuratorUrl = curatorUrl;
            IncludedRecommendations = includedRecommendations == null ? new List<CuratorRecommendation>() : includedRecommendations;
            Selected = selected;
        }

        protected AutoCatCurator(AutoCatCurator other) : base(other)
        {
            Filter = other.Filter;
            CategoryName = other.CategoryName;
            CuratorUrl = other.CuratorUrl;
            IncludedRecommendations = other.IncludedRecommendations == null ? new List<CuratorRecommendation>() : other.IncludedRecommendations;
            Selected = other.Selected;
        }

        //XmlSerializer requires a parameterless constructor
        private AutoCatCurator() { }

        #endregion

        #region Public Properties

        public override AutoCatType AutoCatType => AutoCatType.Curator;

        // AutoCat configuration
        public string CategoryName { get; set; }

        public string CuratorUrl { get; set; }

        [XmlArray("Recommendations")]
        [XmlArrayItem("Recommendation")]
        public List<CuratorRecommendation> IncludedRecommendations { get; set; }

        #endregion

        #region Public Methods and Operators

        public override AutoCatResult CategorizeGame(GameInfo game, Filter filter)
        {
            if (games == null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.Log_AutoCat_GamelistNull);
                throw new ApplicationException(GlobalStrings.AutoCatGenre_Exception_NoGameList);
            }

            if (game == null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.Log_AutoCat_GameNull);
                return AutoCatResult.Failure;
            }

            if (curatorRecommendations == null || curatorRecommendations.Count == 0)
            {
                return AutoCatResult.Failure;
            }

            if (!game.IncludeGame(filter))
            {
                return AutoCatResult.Filtered;
            }

            if (curatorRecommendations.ContainsKey(game.Id) && IncludedRecommendations.Contains(curatorRecommendations[game.Id]))
            {
                string typeName = Utility.GetEnumDescription(curatorRecommendations[game.Id]);
                Category c = games.GetCategory(GetProcessedString(typeName));
                game.AddCategory(c);
            }

            return AutoCatResult.Success;
        }

        public override AutoCat Clone()
        {
            return new AutoCatCurator(this);
        }

        public override void PreProcess(GameList games, Database db)
        {
            this.games = games;
            this.db = db;

            GetRecommendations();
        }

        #endregion

        #region Methods

        private string GetProcessedString(string type)
        {
            if (!string.IsNullOrEmpty(CategoryName))
            {
                return CategoryName.Replace("{type}", type);
            }

            return type;
        }

        private void GetRecommendations()
        {
            Regex curatorIdRegex = new Regex(@"(?:https?://)?store.steampowered.com/curator/(\d+)([^\/]*)/?", RegexOptions.Singleline | RegexOptions.Compiled);
            Match m = curatorIdRegex.Match(CuratorUrl);
            if (!m.Success || !long.TryParse(m.Groups[1].Value, out long curatorId))
            {
                Program.Logger.Write(LoggerLevel.Error, $"Failed to parse curator id from url {CuratorUrl}.");
                MessageBox.Show(string.Format(GlobalStrings.AutocatCurator_CuratorIdParsing_Error, CuratorUrl), GlobalStrings.Gen_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GetCuratorRecommendationsDlg dlg = new GetCuratorRecommendationsDlg(curatorId);
            DialogResult res = dlg.ShowDialog();

            if (dlg.Error != null)
            {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.AutocatCurator_GetRecommendations_Error, dlg.Error.Message);
                MessageBox.Show(string.Format(GlobalStrings.AutocatCurator_GetRecommendations_Error, dlg.Error.Message), GlobalStrings.Gen_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (res != DialogResult.Cancel && res != DialogResult.Abort)
            {
                curatorRecommendations = dlg.CuratorRecommendations;
            }
        }

        #endregion
    }
}
