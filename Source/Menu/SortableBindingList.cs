// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ORTS
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
