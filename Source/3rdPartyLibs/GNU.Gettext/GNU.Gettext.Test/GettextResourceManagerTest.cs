using System;

using NUnit.Framework;

using GNU.Gettext;


namespace GNU.Gettext.Test
{
	[TestFixture()]
	public class GettextResourceManagerTest
	{
		[Test()]
		public void NamesExtractionTest()
		{
			string n1 = "One.Two.Three";
			Assert.AreEqual("Three", GettextResourceManager.ExtractClassName(n1));
			Assert.AreEqual("One.Two", GettextResourceManager.ExtractNamespace(n1));

			Assert.AreEqual("Class", GettextResourceManager.ExtractClassName("Class"));
			Assert.AreEqual(String.Empty, GettextResourceManager.ExtractNamespace(".Test"));
		}
		
		[Test][ExpectedException]
		public void Ex1Test()
		{
			GettextResourceManager.ExtractClassName(null);
		}
		
		[Test][ExpectedException]
		public void Ex2Test()
		{
			GettextResourceManager.ExtractClassName(String.Empty);
		}

		[Test][ExpectedException]
		public void Ex3Test()
		{
			GettextResourceManager.ExtractClassName("Class.");
		}
	}
}

