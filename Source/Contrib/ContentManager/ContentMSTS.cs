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

        public ContentMSTSPackage(Content parent, string name, string path)
            : base(parent)
        {
            Name = name;
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
                        content.Add(new ContentMSTSRoute(this, Path.Combine(path, item)));
            }
            else if (type == ContentType.Consist)
            {
                var path = Path.Combine(Path.Combine(PathName, "Trains"), "Consists");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.con"))
                        content.Add(new ContentMSTSConsist(this, Path.Combine(path, item)));
            }
            else if (type == ContentType.Car)
            {
                var path = Path.Combine(Path.Combine(PathName, "Trains"), "Trainset");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetDirectories(path))
                        foreach (var car in Directory.GetFiles(Path.Combine(path, item), "*.wag").Union(Directory.GetFiles(Path.Combine(path, item), "*.eng")))
                            content.Add(new ContentMSTSCar(this, Path.Combine(Path.Combine(path, item), car)));
            }

            return content;
        }
    }

    public class ContentMSTSRoute : Content
    {
        public override ContentType Type { get { return ContentType.Route; } }

        public ContentMSTSRoute(Content parent, string path)
            : base(parent)
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
                        content.Add(new ContentMSTSActivity(this, Path.Combine(path, item)));
            }
            //else if (type == ContentType.Scenery)
            //{
            //}
            //else if (type == ContentType.Model)
            //{
            //    var path = Path.Combine(PathName, "Shapes");
            //    if (Directory.Exists(path))
            //        foreach (var item in Directory.GetFiles(path, "*.s", SearchOption.AllDirectories))
            //            content.Add(new ContentMSTSModel(this, Path.Combine(path, item)));
            //}
            //else if (type == ContentType.Texture)
            //{
            //    var path = Path.Combine(PathName, "Textures");
            //    if (Directory.Exists(path))
            //        foreach (var item in Directory.GetFiles(path, "*.ace", SearchOption.AllDirectories))
            //            content.Add(new ContentMSTSTexture(this, Path.Combine(path, item)));
            //}

            return content;
        }
    }

    public class ContentMSTSActivity : Content
    {
        public override ContentType Type { get { return ContentType.Activity; } }

        public ContentMSTSActivity(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Service)
            {
                var names = name.Split('|');
                if (names.Length >= 2 && names[0] == "Player")
                    return new ContentMSTSService(this, GetRelatedPath("Services", names[1], ".srv"));
                if (names.Length >= 3 && names[0] == "AI")
                    return new ContentMSTSService(this, GetRelatedPath("Services", names[1], ".srv"), GetRelatedPath("Traffic", names[2], ".trf"));
            }
            return base.Get(name, type);
        }

        string GetRelatedPath(string type, string name, string extension)
        {
            return Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), type), name + extension);
        }
    }

    public class ContentMSTSService : Content
    {
        public override ContentType Type { get { return ContentType.Service; } }
        public bool IsPlayer { get; private set; }
        public string TrafficPathName { get; private set; }

        public ContentMSTSService(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
            IsPlayer = true;
        }

        public ContentMSTSService(Content parent, string path, string traffic)
            : this(parent, path)
        {
            IsPlayer = false;
            TrafficPathName = traffic;
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Path)
            {
                var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), "Paths"), name + ".pat");
                if (File.Exists(path))
                    return new ContentMSTSPath(this, path);
            }
            return base.Get(name, type);
        }
    }

    public class ContentMSTSPath : Content
    {
        public override ContentType Type { get { return ContentType.Path; } }

        public ContentMSTSPath(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSConsist : Content
    {
        public override ContentType Type { get { return ContentType.Consist; } }

        public ContentMSTSConsist(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSCar : Content
    {
        public override ContentType Type { get { return ContentType.Car; } }

        public ContentMSTSCar(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        //public override IEnumerable<Content> Get(ContentType type)
        //{
        //    var content = new List<Content>();

        //    if (type == ContentType.Cab)
        //    {
        //        var path = Path.Combine(Path.GetDirectoryName(PathName), "Cabview");
        //        if (Directory.Exists(path))
        //            foreach (var item in Directory.GetFiles(path, "*.cvf"))
        //                content.Add(new ContentMSTSCab(this, Path.Combine(path, item)));
        //    }
        //    else if (type == ContentType.Model)
        //    {
        //        foreach (var item in Directory.GetFiles(Path.GetDirectoryName(PathName), "*.s"))
        //            content.Add(new ContentMSTSModel(this, Path.Combine(PathName, item)));
        //    }
        //    else if (type == ContentType.Texture)
        //    {
        //        foreach (var item in Directory.GetFiles(Path.GetDirectoryName(PathName), "*.ace"))
        //            content.Add(new ContentMSTSTexture(this, Path.Combine(PathName, item)));
        //    }

        //    return content;
        //}
    }

    public class ContentMSTSCab : Content
    {
        public override ContentType Type { get { return ContentType.Cab; } }

        public ContentMSTSCab(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            var content = new List<Content>();

            if (type == ContentType.Texture)
            {
                foreach (var item in Directory.GetFiles(Path.GetDirectoryName(PathName), "*.ace"))
                    content.Add(new ContentMSTSTexture(this, Path.Combine(PathName, item)));
            }

            return content;
        }
    }

    public class ContentMSTSModel : Content
    {
        public override ContentType Type { get { return ContentType.Model; } }

        public ContentMSTSModel(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    public class ContentMSTSTexture : Content
    {
        public override ContentType Type { get { return ContentType.Texture; } }

        public ContentMSTSTexture(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }
}
