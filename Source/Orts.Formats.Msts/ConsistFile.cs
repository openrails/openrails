// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orts.Parsers.Msts;
using ORTS.Common;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Work with consist files
    /// </summary>
    public class ConsistFile : IConsist
    {
        public string Name; // from the Name field or label field of the consist file
        public Train_Config Train;

        public ConsistFile(string filePath)
        {
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("train", ()=>{ Train = new Train_Config(stf); }),
                });
            Name = Train.TrainCfg.Name;
        }

        string IConsist.DisplayName => Train.TrainCfg.Name;

        public float? MaxVelocityMpS
        {
            get
            {
                float a = Train.TrainCfg.MaxVelocity?.A ?? 0f;
                if (a <= 0f || a == 40f)
                    return null;
                else
                    return a;
            }
        }

        public float Durability => Train.TrainCfg.Durability;

        public bool PlayerDrivable => true;

        public ISet<PreferredLocomotive> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders)
        {
            Wagon firstEngine = Train.TrainCfg.WagonList
                .Where((Wagon wagon) => wagon.IsEngine)
                .FirstOrDefault();
            if (firstEngine == null)
                return PreferredLocomotive.NoLocomotiveSet;
            else
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(WagonPath(basePath, firstEngine)) };
        }

        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders)
        {
            Wagon lastEngine = Train.TrainCfg.WagonList
                .Where((Wagon wagon) => wagon.IsEngine)
                .LastOrDefault();
            if (lastEngine == null)
                return PreferredLocomotive.NoLocomotiveSet;
            else
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(WagonPath(basePath, lastEngine)) };
        }

        public IEnumerable<WagonReference> GetForwardWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            if (GetLeadLocomotiveChoices(basePath, folders).FirstOrDefault().Equals(preference))
                return new WagonReference[0] { };
            return Train.TrainCfg.WagonList
                .Select((Wagon wagon) => new WagonReference(WagonPath(basePath, wagon), wagon.Flip, wagon.UiD));
        }

        public IEnumerable<WagonReference> GetReverseWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            if (GetReverseLocomotiveChoices(basePath, folders).FirstOrDefault().Equals(preference))
                return new WagonReference[0] { };
            return Train.TrainCfg.WagonList
                .Select((Wagon wagon) => new WagonReference(WagonPath(basePath, wagon), !wagon.Flip, wagon.UiD))
                .Reverse();
        }

        private static string WagonPath(string basePath, Wagon wagon)
        {
            string trainsetPath = Path.Combine(basePath, "trains", "trainset");
            return Path.Combine(trainsetPath, wagon.Folder, Path.ChangeExtension(wagon.Name, wagon.IsEngine ? ".eng" : ".wag"));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
