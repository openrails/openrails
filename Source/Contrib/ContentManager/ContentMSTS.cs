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
using System.IO;
using System.Linq;

namespace ORTS.ContentManager
{
    [Serializable]
    public class ContentMSTSCollection : Content
    {
        public override ContentType Type { get { return ContentType.Collection; } }

        public ContentMSTSCollection(Content parent, string name, string path)
            : base(parent)
        {
            Name = name;
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            if (type == ContentType.Package)
            {
                if (Directory.Exists(PathName))
                {
                    foreach (var item in Directory.GetDirectories(PathName))
                    {
                        if (ContentMSTSPackage.IsValid(item))
                            yield return new ContentMSTSPackage(this, Path.GetFileName(item), item);
                    }
                }
            }
        }
    }

    [Serializable]
    public class ContentMSTSPackage : Content
    {
        public static bool IsValid(string pathName)
        {
            return Directory.Exists(Path.Combine(pathName, "ROUTES")) || Directory.Exists(Path.Combine(pathName, "TRAINS"));
        }

        public override ContentType Type { get { return ContentType.Package; } }

        public ContentMSTSPackage(Content parent, string name, string path)
            : base(parent)
        {
            Name = name;
            PathName = path;
        }

        public override IEnumerable<Content> Get(ContentType type)
        {
            if (type == ContentType.Route)
            {
                var path = Path.Combine(PathName, "Routes");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetDirectories(path))
                        yield return new ContentMSTSRoute(this, Path.Combine(path, item));
            }
            else if (type == ContentType.Consist)
            {
                var path = Path.Combine(Path.Combine(PathName, "Trains"), "Consists");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.con"))
                        yield return new ContentMSTSConsist(this, Path.Combine(path, item));
            }
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Car)
            {
                var pathEng = Path.Combine(Path.Combine(Path.Combine(PathName, "Trains"), "Trainset"), name + ".eng");
                if (File.Exists(pathEng))
                    return new ContentMSTSCar(this, pathEng);

                var pathWag = Path.Combine(Path.Combine(Path.Combine(PathName, "Trains"), "Trainset"), name + ".wag");
                if (File.Exists(pathWag))
                    return new ContentMSTSCar(this, pathWag);
            }
            return base.Get(name, type);
        }
    }

    [Serializable]
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
            if (type == ContentType.Activity)
            {
                var path = Path.Combine(PathName, "Activities");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.act"))
                        yield return new ContentMSTSActivity(this, Path.Combine(path, item));

                var pathOR = Path.Combine(PathName, @"Activities\OpenRails");
                if (Directory.Exists(pathOR))
                    foreach (var item in Enumerable.Concat(Directory.GetFiles(pathOR, "*.timetable_or"), Directory.GetFiles(pathOR, "*.timetable-or")))
                        yield return new ContentORTimetableActivity(this, Path.Combine(pathOR, item));
            }
            else if (type == ContentType.Path)
            {
                var path = Path.Combine(PathName, "Paths");
                if (Directory.Exists(path))
                    foreach (var item in Directory.GetFiles(path, "*.pat"))
                        yield return new ContentMSTSPath(this, Path.Combine(path, item));
            }
        }

        public override Content Get(string name, ContentType type)
        {
            if (type == ContentType.Path)
            {
                var path = Path.Combine(Path.Combine(PathName, "Paths"), name + ".pat");
                if (File.Exists(path))
                    return new ContentMSTSPath(this, path);
            }
            return base.Get(name, type);
        }
    }

    [Serializable]
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
                if (names.Length >= 4 && names[0] == "AI")
                    return new ContentMSTSService(this, GetRelatedPath("Services", names[1], ".srv"), GetRelatedPath("Traffic", names[2], ".trf"), int.Parse(names[3]));
            }
            return base.Get(name, type);
        }

        string GetRelatedPath(string type, string name, string extension)
        {
            return Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(PathName)), type), name + extension);
        }
    }

    [Serializable]
    public class ContentMSTSService : Content
    {
        public override ContentType Type { get { return ContentType.Service; } }
        public bool IsPlayer { get; private set; }
        public string TrafficPathName { get; private set; }
        public int TrafficIndex { get; private set; }

        public ContentMSTSService(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PathName = path;
            IsPlayer = true;
        }

        public ContentMSTSService(Content parent, string path, string traffic, int traffixIndex)
            : this(parent, path)
        {
            IsPlayer = false;
            TrafficPathName = traffic;
            TrafficIndex = traffixIndex;
        }
    }

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    public class ContentMSTSCar : Content
    {
        public override ContentType Type { get { return ContentType.Car; } }

        public ContentMSTSCar(Content parent, string path)
            : base(parent)
        {
            Name = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileNameWithoutExtension(path);
            PathName = path;
        }
    }

    [Serializable]
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
            if (type == ContentType.Texture)
            {
                foreach (var item in Directory.GetFiles(Path.GetDirectoryName(PathName), "*.ace"))
                    yield return new ContentMSTSTexture(this, Path.Combine(PathName, item));
            }
        }
    }

    [Serializable]
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

    [Serializable]
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
