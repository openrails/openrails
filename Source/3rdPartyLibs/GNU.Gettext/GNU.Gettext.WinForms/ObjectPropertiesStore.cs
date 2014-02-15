using System;
using System.Reflection;
using System.Collections.Generic;

namespace GNU.Gettext.WinForms
{
	class PropertiesValuesStore : Dictionary<string, object>
	{
	}
	
	public class ObjectPropertiesStore
	{
		private Dictionary<int, PropertiesValuesStore> store = new Dictionary<int, PropertiesValuesStore>();
		
		public ObjectPropertiesStore()
		{
		}
		
		public void SetState(object obj, string propertyName)
        {
            SetState(obj, propertyName, null);
        }

		public void SetState(object obj, string propertyName, object value)
		{
			if (value == null)
			{
				PropertyInfo pi = obj.GetType().GetProperty(propertyName);
				if (pi != null && pi.CanRead)
					value = pi.GetValue(obj, null);
				else
					throw new Exception(String.Format("Property '{0}' not exists or write-only. Object: {1}", propertyName, obj.ToString()));
			}
			PropertiesValuesStore propStore;
			store.TryGetValue(obj.GetHashCode(), out propStore);
			if (propStore == null)
			{
				propStore = new PropertiesValuesStore();
				store.Add(obj.GetHashCode(), propStore);
			}
			
			if (propStore.ContainsKey(propertyName))
			{
				propStore[propertyName] = value;
			}
			else
			{
				propStore.Add(propertyName, value);
			}
		}
		
		public object GetState(object obj, string propertyName)
		{
			PropertiesValuesStore propStore;
			store.TryGetValue(obj.GetHashCode(), out propStore);
			if (propStore == null)
				return null;
			
			if (propStore.ContainsKey(propertyName))
				return propStore[propertyName];
			return null;
		}
		
		public string GetStateString(object obj, string propertyName)
		{
			object value = GetState(obj, propertyName);
			return value == null ? null : value.ToString();
		}
	}
}

