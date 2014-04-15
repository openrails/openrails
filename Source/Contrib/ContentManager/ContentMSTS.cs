// COPYRIGHT 2014 by the Open Rails project.
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
    public class ContentMSTSPackage : Content
    {
        public override ContentType Type { get { return ContentType.Package; } }

        public ContentMSTSPackage(string path)
        {
            Name = Path.GetFileName(path);
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            var content = new List<Content>();

            if (type == ContentType.Route)
            {
                var path = Path.Combine(PathName, "Routes");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetDirectories(path))
                        content.Add(new ContentMSTSRoute(Path.Combine(path, item)));
            }
            else if (type == ContentType.Consist)
            {
                var path = Path.Combine(Path.Combine(PathName, "Trains"), "Consists");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.con"))
                        content.Add(new ContentMSTSConsist(Path.Combine(path, item)));
            }
            else if (type == ContentType.Trainset)
            {
                var path = Path.Combine(Path.Combine(PathName, "Trains"), "Trainset");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetDirectories(path))
                        content.Add(new ContentMSTSTrainset(Path.Combine(path, item)));
            }

            return content;
        }
    }

    public class ContentMSTSRoute : Content
    {
        public override ContentType Type { get { return ContentType.Route; } }

        public ContentMSTSRoute(string path)
        {
            Name = Path.GetFileName(path);
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            var content = new List<Content>();

            if (type == ContentType.Activity)
            {
                var path = Path.Combine(PathName, "Activities");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.act"))
                        content.Add(new ContentMSTSActivity(Path.Combine(path, item)));
            }
            else if (type == ContentType.Service)
            {
                var path = Path.Combine(PathName, "Services");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.srv"))
                        content.Add(new ContentMSTSService(Path.Combine(path, item)));
            }
            else if (type == ContentType.Traffic)
            {
                var path = Path.Combine(PathName, "Traffic");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.trf"))
                        content.Add(new ContentMSTSTraffic(Path.Combine(path, item)));
            }
            else if (type == ContentType.Path)
            {
                var path = Path.Combine(PathName, "Paths");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.pat"))
                        content.Add(new ContentMSTSPath(Path.Combine(path, item)));
            }
            else if (type == ContentType.Scenery)
            {
            }
            else if (type == ContentType.Model)
            {
                var path = Path.Combine(PathName, "Shapes");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.s", SearchOption.AllDirectories))
                        content.Add(new ContentMSTSModel(Path.Combine(path, item)));
            }
            else if (type == ContentType.Texture)
            {
                var path = Path.Combine(PathName, "Textures");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.ace", SearchOption.AllDirectories))
                        content.Add(new ContentMSTSTexture(Path.Combine(path, item)));
            }

            return content;
        }
    }

    public class ContentMSTSActivity : Content
    {
        public override ContentType Type { get { return ContentType.Activity; } }

        public ContentMSTSActivity(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Route)
            {
                var path = Path.GetDirectoryName(Path.GetDirectoryName(PathName));
                if (Directory.Exists(path))
                    return new ContentMSTSRoute(path);
            }
            else if (type == ContentType.Service)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Services"), name + ".srv");
                if (File.Exists(path))
                    return new ContentMSTSService(path);
            }
            else if (type == ContentType.Traffic)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Traffic"), name + ".trf");
                if (File.Exists(path))
                    return new ContentMSTSTraffic(path);
            }
            else if (type == ContentType.Path)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Paths"), name + ".pat");
                if (File.Exists(path))
                    return new ContentMSTSPath(path);
            }
            return base.Get(name, type);
        }
    }

    public class ContentMSTSService : Content
    {
        public override ContentType Type { get { return ContentType.Service; } }

        public ContentMSTSService(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Path)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Paths"), name + ".pat");
                if (File.Exists(path))
                    return new ContentMSTSPath(path);
            }
            else if (type == ContentType.Consist)
            {
                var path = Path.Combine(Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(PathName)))), "Trains"), "Consists"), name + ".con");
                if (File.Exists(path))
                    return new ContentMSTSConsist(path);
            }
            return base.Get(name, type);
        }
    }

    public class ContentMSTSTraffic : Content
    {
        public override ContentType Type { get { return ContentType.Traffic; } }

        public ContentMSTSTraffic(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Service)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Services"), name + ".srv");
                if (File.Exists(path))
                    return new ContentMSTSService(path);
            }
            return base.Get(name, type);
        }
    }

    public class ContentMSTSPath : Content
    {
        public override ContentType Type { get { return ContentType.Path; } }

        public ContentMSTSPath(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSConsist : Content
    {
        public override ContentType Type { get { return ContentType.Consist; } }

        public ContentMSTSConsist(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSTrainset : Content
    {
        public override ContentType Type { get { return ContentType.Trainset; } }

        public ContentMSTSTrainset(string path)
        {
            Name = Path.GetFileName(path);
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            var content = new List<Content>();

            if (type == ContentType.Car)
            {
                foreach (var item in Directory.GetFiles(PathName, "*.eng"))
                    content.Add(new ContentMSTSCar(Path.Combine(PathName, item)));
                foreach (var item in Directory.GetFiles(PathName, "*.wag"))
                    content.Add(new ContentMSTSCar(Path.Combine(PathName, item)));
            }
            else if (type == ContentType.Cab)
            {
                var path = Path.Combine(PathName, "Cabview");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.cvf"))
                        content.Add(new ContentMSTSCab(Path.Combine(path, item)));
            }
            else if (type == ContentType.Model)
            {
                foreach (var item in Directory.GetFiles(PathName, "*.s", SearchOption.AllDirectories))
                    content.Add(new ContentMSTSModel(Path.Combine(PathName, item)));
            }
            else if (type == ContentType.Texture)
            {
                foreach (var item in Directory.GetFiles(PathName, "*.ace", SearchOption.AllDirectories))
                    content.Add(new ContentMSTSTexture(Path.Combine(PathName, item)));
            }

            return content;
        }
    }

    public class ContentMSTSCar : Content
    {
        public override ContentType Type { get { return ContentType.Car; } }

        public ContentMSTSCar(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSCab : Content
    {
        public override ContentType Type { get { return ContentType.Cab; } }

        public ContentMSTSCab(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSModel : Content
    {
        public override ContentType Type { get { return ContentType.Model; } }

        public ContentMSTSModel(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSTexture : Content
    {
        public override ContentType Type { get { return ContentType.Texture; } }

        public ContentMSTSTexture(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }
}
