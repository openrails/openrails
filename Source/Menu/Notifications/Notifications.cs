// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using System.Collections.Generic;
using ORTS.Updater;

namespace ORTS
{
    public class Notifications
    {
        public List<Notification> NotificationList = new List<Notification>();
        public List<Check> CheckList = new List<Check>();
    }

    class JsonInput
    {
        public List<Notification> NotificationList { get; set; }
        public List<Check> CheckList { get; set; }
    }

    public class Notification
    {
        public string Date { get; set; }
        public string Title { get; set; }
        public string UpdateMode { get; set; }
        public List<Item> PrefixItemList { get; set; }
        public Met MetLists { get; set; }
        public List<Item> SuffixItemList { get; set; }
        public bool ToDelete { get; set; } = false; // So we can mark items for deletion and then delete in single statement.
    }
    class Record : Item
    {
        public string Value { get; set; }
    }
    class Text : Item
    {
        public string Color { get; set; } = "black";
    }
    class Heading : Item
    {
        public string Color { get; set; } = "red";
    }
    class Link : Item
    {
        public string Value { get; set; }
        public string Url { get; set; }
    }
    class Update : Item
    {
        public string Value { get; set; }
        public string UpdateMode { get; set; }
    }
    public class Item
    {
        public string Label { get; set; }
        public int Indent { get; set; } = 140;
    }
    public class Met
    {
        public List<Item> ItemList { get; set; }
        public List<CheckId> CheckIdList { get; set; }
    }
    public class CheckId
    {
        public string Id { get; set; }
    }

    public class Check
    {
        public string Id { get; set; }
        public List<CheckAllOf> CheckAnyOf { get; set; }
        public List<Item> UnmetItemList { get; set; }
    }

    public class CheckAllOf
    {
        public List<Criteria> AllOf { get; set; }
    }

    public class Excludes : CheckAllOf
    {
    }

    public class Includes : CheckAllOf
    {
    }

    class Contains : Criteria { }
    class NotContains : Criteria { }

    // Not implemented yet
    // String comparison, not numerical
    class NoLessThan : Criteria { }
    class NoMoreThan : Criteria { }
    
    public class Criteria
    {
        // System Information "examples"
        public string Name { get; set; }    // installed_version, direct3d, runtime, system, memory, cpu, gpu
        public string Value { get; set; }   // {{new_version}}, {{10_0}}
    }

    public class OverrideValues
    {
        public List<Criteria> ValueList = new List<Criteria>();
    }
}
