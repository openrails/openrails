using NUnit.Framework;
using System;
using System.Windows.Forms;

using GNU.Gettext.WinForms;

namespace GNU.Gettext.Test
{
	[TestFixture()]
	public class ObjectPropertiesStoreTest
	{
		[Test()]
		public void TestCase()
		{
			ObjectPropertiesStore store = new ObjectPropertiesStore();
			
			Label label1  = new Label();
			label1.Text = "Original1";
			label1.Tag = 123;
			
			store.SetState(label1, "Text");
			store.SetState(label1, "Tag");
			
			Label label2  = new Label();
			label2.Text = "Original2";
			label2.Tag = "456";
			store.SetState(label2, "Text");
			store.SetState(label2, "Tag");
			
			object o1 = new object();
			store.SetState(o1, "Custom", "Value");
			
			
			Assert.AreEqual("Original1", store.GetStateString(label1, "Text"));
			Assert.AreEqual(123, int.Parse(store.GetState(label1, "Tag").ToString()));
			Assert.AreEqual("Original2", store.GetStateString(label2, "Text"));
			Assert.AreEqual("456", store.GetStateString(label2, "Tag"));
			Assert.AreEqual("Value", store.GetStateString(o1, "Custom"));
		}
	}
}

