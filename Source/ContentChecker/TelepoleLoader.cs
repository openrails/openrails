// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.IO;
using Orts.Formats.Msts;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for route telepole.dat files.
    /// </summary>
    class TelepoleLoader : Loader
    {
        public override void TryLoading(string file)
        {
            string routePath = Path.GetDirectoryName(file);
            var telepoleData = new TelepoleDataFile(file,
                Path.Combine(routePath, "shapes"));
        }
    }
}
