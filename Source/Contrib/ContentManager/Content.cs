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
using System.Linq;
using System.Text;

namespace ORTS.ContentManager
{
    // Root
    //   Package
    //     Route
    //       Activity
    //       Path
    //       Scenery
    //       Model
    //       Texture
    //     Consist
    //     Trainset
    //       Car
    //       Cab
    //       Model
    //       Texture
    public enum ContentType
    {
        Root,
        Package,
        Route,
        Activity,
        Path,
        Consist,
        Trainset,
        Car,
        Cab,
        Scenery,
        Model,
        Texture,
    }

    public abstract class Content
    {
        public abstract ContentType Type { get; }
        public string Name { get; protected set; }
        public string PathName { get; protected set; }

        public virtual IEnumerable<Content> Get(ContentType type)
        {
            return new Content[0];
        }

        public virtual Content Get(string name, ContentType type)
        {
            return null;
        }
    }
}
