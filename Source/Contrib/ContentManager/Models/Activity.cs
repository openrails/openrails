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

using Orts.Formats.Msts;
using Orts.Parsers.OR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ORTS.ContentManager.Models
{
    public class Activity
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string Briefing;

        public readonly IEnumerable<string> PlayerServices;
        public readonly IEnumerable<string> Services;

        public Activity(Content content)
        {
            Debug.Assert(content.Type == ContentType.Activity);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".act", StringComparison.OrdinalIgnoreCase))
            {
                var file = new ActivityFile(content.PathName);
                Name = file.Tr_Activity.Tr_Activity_Header.Name;
                Description = file.Tr_Activity.Tr_Activity_Header.Description;
                Briefing = file.Tr_Activity.Tr_Activity_Header.Briefing;
                PlayerServices = new[] { String.Format("Player|{0}", file.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name) };
                if (file.Tr_Activity.Tr_Activity_File.Traffic_Definition != null)
                    Services = file.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList.Select((service, index) =>
                        String.Format("AI|{0}|{1}|{2}", service.Name, file.Tr_Activity.Tr_Activity_File.Traffic_Definition.Name, index)
                    );
                else
                    Services = new string[0];
            }
            else if (System.IO.Path.GetExtension(content.PathName).Equals(".timetable_or", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Make common timetable parser.
                var file = new TimetableReader(content.PathName);
                Name = content.Name;

                var services = new List<string>();
                for (var column = 0; column < file.Strings[0].Length; column++)
                {
                    if (String.IsNullOrEmpty(file.Strings[0][column]) || file.Strings[0][column].StartsWith("#"))
                        continue;

                    services.Add(file.Strings[0][column]);
                }
                PlayerServices = services;
                Services = new string[0];
            }
        }
    }
}
