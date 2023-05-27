// COPYRIGHT 2014, 2015 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using ORTS.Settings;

namespace ORTS.ContentManager
{
    [Serializable]
    public class ContentManager : Content
    {
        [NonSerialized]
        readonly FolderSettings Settings;

        public override ContentType Type { get { return ContentType.Root; } }

        public ContentManager(FolderSettings settings)
            : base(null)
        {
            Settings = settings;
            Name = "Content Manager";
            PathName = "";
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            if (type == ContentType.Package)
            {
                // TODO: Support OR content folders.
                foreach (var folder in Settings.Folders)
                {
                    if (ContentMSTSPackage.IsValid(folder.Value))
                        yield return new ContentMSTSPackage(this, folder.Key, folder.Value);
                    else
                        yield return new ContentMSTSCollection(this, folder.Key, folder.Value);
                }
            }
        }
    }
}
