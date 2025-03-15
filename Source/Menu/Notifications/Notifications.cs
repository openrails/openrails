// COPYRIGHT 2009 - 2024 by the Open Rails project.
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


namespace Menu.Notifications
{
    public class Notifications
    {
        public List<Notification> NotificationList = new List<Notification>();
        public List<Check> CheckList = new List<Check>();
    }

    public class Notification
    {
        public string Date { get; set; }
        public string Title { get; set; }
        public List<string> IncludeIf { get; set; }
        public List<string> IncludeIfNot { get; set; }
        public List<Item> ItemList { get; set; }
    }
    class Record : ValueItem
    {
    }
    class Text : Item
    {
    }
    class Heading : Item
    {
        public new string Color { get; set; } = "blue";
    }
    class Link : ValueItem
    {
        public string Url { get; set; }
        public string StableUrl { get; set; }
        public string TestingUrl { get; set; }
        public string UnstableUrl { get; set; }
    }
    class Dialog : ValueItem
    {
        public string Form { get; set; }
    }
    class Update : ValueItem
    {
    }
    abstract class ValueItem : Item
    {
        public string Value { get; set; }
    }
    public abstract class Item
    {
        public List<string> IncludeIf { get; set; }
        public List<string> IncludeIfNot { get; set; }
        public string Label { get; set; }
        public string Color { get; set; } = "black";
        public int Indent { get; set; } = 140;
    }

    public class Check
    {
        public string Id { get; set; }
        public List<AnyOf> AnyOfList { get; set; }
    }

    public class AnyOf
    {
        public List<Criteria> AllOfList { get; set; }
    }

    // These criteria are all doing an actual comparison
    class Contains : Criteria { public override bool IsMatch() => Property.Contains(Value); }
    class Equals : Criteria { public override bool IsMatch() => Property == Value; }
    class LessThan : NumericCriteria { public override bool IsMatch() => PropertyAsInt < ValueAsInt; }
    class MoreThan : NumericCriteria { public override bool IsMatch() => PropertyAsInt > ValueAsInt; }

    // These criteria are all negated versions of those above
    class NotContains : Contains { public override bool IsMatch() => !base.IsMatch(); }
    class NotEquals : Equals { public override bool IsMatch() => !base.IsMatch(); }
    class MoreThanOrEquals : LessThan { public override bool IsMatch() => !base.IsMatch(); }
    class LessThanOrEquals : MoreThan { public override bool IsMatch() => !base.IsMatch(); }

    abstract class NumericCriteria : Criteria
    {
        public int? PropertyAsInt => int.TryParse(Property, out var value) ? value : (int?)null;
        public int? ValueAsInt => int.TryParse(Value, out var value) ? value : (int?)null;
    }

    public abstract class Criteria
    {
        // System Information "examples"
        public string Property { get; set; }    // installed_version, direct3d, runtime, system, memory, cpu, gpu
        public string Value { get; set; }       // {{new_version}}, {{10_0}}
        public abstract bool IsMatch();
    }

    class ParameterValue
    {
        public string Parameter { get; set; }    // installed_version, direct3d, runtime, system, memory, cpu, gpu
        public string Value { get; set; }       // {{new_version}}, {{10_0}}
    }

    class OverrideParameterList
    {
        public List<ParameterValue> ParameterValueList = new List<ParameterValue>();
    }
}
