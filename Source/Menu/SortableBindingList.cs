// COPYRIGHT 2012 by the Open Rails project.
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Menu
{
    public class SortableBindingList<T> : BindingList<T>
    {
        public SortableBindingList()
        {
        }

        public SortableBindingList(IList<T> list)
            : base(list)
        {
        }

        protected override bool SupportsSortingCore { get { return true; } }

        protected ListSortDirection sortDirection = ListSortDirection.Ascending;
        protected override ListSortDirection SortDirectionCore { get { return sortDirection; } }

        protected PropertyDescriptor sortProperty = null;
        protected override PropertyDescriptor SortPropertyCore { get { return sortProperty; } }

        protected bool isSorted = false;
        protected override bool IsSortedCore { get { return isSorted; } }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            if (SortableBindingList<T>.PropertyComparer.IsAllowable(prop))
            {
                ((List<T>)Items).Sort(new SortableBindingList<T>.PropertyComparer(prop, direction));
                sortDirection = direction;
                sortProperty = prop;
                isSorted = true;
            }
        }

        protected override void RemoveSortCore()
        {
            sortProperty = null;
            isSorted = false;
        }

        class PropertyComparer : Comparer<T>
        {
            PropertyDescriptor Prop;
            ListSortDirection Direction;
            IComparer Comparer;

            public PropertyComparer(PropertyDescriptor prop, ListSortDirection direction)
            {
                Prop = prop;
                Direction = direction;
                Comparer = (IComparer)typeof(Comparer<>).MakeGenericType(prop.PropertyType).GetProperty("Default").GetValue(null, null);
            }

            public override int Compare(T x, T y)
            {
                if (Direction == ListSortDirection.Ascending)
                    return Comparer.Compare(Prop.GetValue(x), Prop.GetValue(y));
                return Comparer.Compare(Prop.GetValue(y), Prop.GetValue(x));
            }

            public static bool IsAllowable(PropertyDescriptor prop)
            {
                return prop.PropertyType.GetInterface("IComparable") != null;
            }
        }
    }
}
