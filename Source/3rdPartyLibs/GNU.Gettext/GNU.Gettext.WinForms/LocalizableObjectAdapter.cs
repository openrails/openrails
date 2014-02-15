using System;
using System.Reflection;
using System.Windows.Forms;

namespace GNU.Gettext.WinForms
{
	class LocalizableObjectAdapter
	{
		public object Source { get; private set; }
		public ObjectPropertiesStore Store { get; set; }
		public ToolTipControls ToolTips { get; set; } 
		
		#region Constructors
		public LocalizableObjectAdapter(object source, ObjectPropertiesStore store, ToolTipControls toolTips)
		{
			this.Source = source;
			this.Store = store;
			this.ToolTips = toolTips;
		}
		#endregion

		#region Public interface
		public void Localize(GettextResourceManager catalog)
		{
			LocalizeProperty(catalog, "Text");
			LocalizeProperty(catalog, "HeaderText");
			LocalizeProperty(catalog, "ToolTipText");
			
			if (Source is Control)
			{
				foreach(ToolTip toolTip in ToolTips)
				{
					string hint = toolTip.GetToolTip(Source as Control);
					if (hint != null)
					{
						StoreIfOriginal("FromToolTipText", hint);
						string translatedHint = catalog.GetString(hint);
						if (translatedHint != toolTip.GetToolTip(Source as Control))
							toolTip.SetToolTip((Source as Control), translatedHint);
					}
				}
			}
		}
		
		public void Revert()
		{
			RevertProperty("Text");
			RevertProperty("HeaderText");
			RevertProperty("ToolTipText");
			
			if (Source is Control)
			{
				foreach(ToolTip toolTip in ToolTips)
				{
					if (Store != null)
					{
						string hint = Store.GetStateString(Source, "FromToolTipText");
						if (hint != null && hint != toolTip.GetToolTip(Source as Control))
							toolTip.SetToolTip((Source as Control), hint);
					}
				}
			}
		}
		
		public override string ToString()
		{
			return string.Format("[LocalizableObjectAdapter: Source={0}]", Source);
		}
		#endregion
		
		private void StoreIfOriginal(string propertyName, string value)
		{
			if (Store != null)
			{
				if (Store.GetState(Source, propertyName) == null)
					Store.SetState(Source, propertyName, value);
			}
		}
		
		private void LocalizeProperty(GettextResourceManager catalog, string propertyName)
		{
			string text = GetPropertyValue(propertyName);
			if (text != null)
				SetPropertyValue(propertyName, catalog.GetString(text));
		}
		
		private void RevertProperty(string propertyName)
		{
			if (Store != null)
			{
				string originalText = Store.GetStateString(Source, propertyName);
				if (originalText != null)
					SetPropertyValue(propertyName, originalText);
			}
		}
		
		private string GetPropertyValue(string name)
		{
			PropertyInfo pi = Source.GetType().GetProperty(name);
			if (pi != null && pi.CanRead)
			{
				object value = pi.GetValue(Source, null);
				return value != null && value is string ? (string)value : null;
			}
			return null;
		}
		
		private void SetPropertyValue(string name, string value)
		{
			PropertyInfo pi = Source.GetType().GetProperty(name);
			if (pi != null && pi.CanWrite)
			{
				StoreIfOriginal(name, GetPropertyValue(name));
                try
                {
                    pi.SetValue(Source, value, null);
                }
                catch (Exception ex)
                {
                    // Workaround:
                    // Property may exist and be accessible as read/write but not supported
                    // Example: WebBrowser.Text
                    if (!(ex is NotSupportedException ||
                        (ex.InnerException != null && ex.InnerException is NotSupportedException)))
                        throw;
                }
			}
		}
	}
}

