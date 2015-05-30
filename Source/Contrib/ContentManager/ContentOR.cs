// COPYRIGHT 2015 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS.ContentManager
{
    [Serializable]
    public class ContentORTimetableActivity : Content
    {
        public override ContentType Type { get { return ContentType.Activity; } }

        public ContentORTimetableActivity(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Service)
            {
                return new ContentORTimetableService(this, name);
            }
            return base.Get(name, type);
        }
    }

    [Serializable]
    public class ContentORTimetableService : Content
    {
        public override ContentType Type { get { return ContentType.Service; } }

        public ContentORTimetableService(Content parent, string serviceName)
            : base(parent)
        {
            Name = serviceName;
            PathName = parent.PathName;
        }
    }
}
