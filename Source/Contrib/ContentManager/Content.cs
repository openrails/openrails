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

// Uncomment this define to enable debug logging for the Content.Get(string, ContentType) method.
//#define DEBUG_CONTENT_GET_SCAN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ORTS.ContentManager
{
    // Root
    //   Collection
    //     Package
    //       Route
    //         Activity
    //         Service - the timetabled stops and other actions along a path
    //         Path - the physical route taken
    //         Scenery
    //         Model
    //         Texture
    //       Consist
    //       Car
    //         Model
    //         Texture
    //       Cab
    //         Texture
    public enum ContentType
    {
        Root,
        Collection,
        Package,
        Route,
        Activity,
        Service,
        Path,
        Consist,
        Car,
        Cab,
        Scenery,
        Model,
        Texture,
    }

    [Serializable]
    public abstract class Content
    {
        public Content Parent { get; set; }
        public abstract ContentType Type { get; }
        public string Name { get; protected set; }
        public string PathName { get; protected set; }

        public static bool operator ==(Content left, Content right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(left, right) : left.Equals(right);
        }

        public static bool operator !=(Content left, Content right)
        {
            return !(left == right);
        }

        protected Content(Content parent)
        {
            Parent = parent;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var content = obj as Content;
            return Type == content.Type && Name == content.Name && PathName == content.PathName;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ PathName.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0}({1})", Type, PathName);
        }

        // TODO: Be abstract.
        public virtual IEnumerable<Content> Get(ContentType type)
        {
            return new Content[0];
        }

        public virtual Content Get(string name, ContentType type)
        {
            // This is a very naive implementation which is meant only for prototyping and maybe as a final backstop.
            var children = Get(type);
#if DEBUG_CONTENT_GET_SCAN
            if (children.Any())
            {
                Debug.WriteLine(String.Format("{0} naively scanning for {2} '{1}'", this, name, type));
#endif
                foreach (var child in children)
                {
                    if (child.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
#if DEBUG_CONTENT_GET_SCAN
                        Debug.WriteLine(String.Format("{0} naive scan found match for {2} '{1}'", this, name, type));
#endif
                        return child;
                    }
                }
#if DEBUG_CONTENT_GET_SCAN
            }
#endif
            // This is how Get(name, type) is meant to work: stepping up the hierarchy as needed.
            if (Parent != null)
                return Parent.Get(name, type);
            // We're at the top, sorry.
            return null;
        }
    }
}
