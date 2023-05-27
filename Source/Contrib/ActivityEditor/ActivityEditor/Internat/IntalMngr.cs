// COPYRIGHT 2013 by the Open Rails project.
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

//
using System.Globalization;
using System.Reflection;
using System.Resources;
using ORTS.Settings;

namespace ActivityEditor.Internat
{
    public class IntalMngr
    {
        private ResourceManager resMan;
        private CultureInfo EnglishCulture;
        private CultureInfo FrenchCulture;

        public IntalMngr(UserSettings settings)
        {
            EnglishCulture = new CultureInfo("en");
            FrenchCulture = new CultureInfo("fr-FR");
            CultureInfo.DefaultThreadCurrentUICulture = EnglishCulture;
            resMan = new ResourceManager("ActivityEditor.ActEditor", Assembly.GetExecutingAssembly());
        }

        public string GetString(string name)
        {
            string info = resMan.GetString(name);
            return info;
        }
    }
}
