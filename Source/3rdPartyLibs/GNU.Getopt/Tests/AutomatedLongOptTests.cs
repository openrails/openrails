using System;
using System.Text;

using NUnit.Framework;

namespace GNU.Getopt.Tests
{
	[TestFixture]
	public class AutomatedLongOptTests
	{
		[Test] [ExpectedException(typeof(ArgumentException))]
		public void InvalidHasArg() 
		{
			LongOpt test = new LongOpt("test", (Argument) 20, null, 32);
		}
		[Test]
		public void LongOptTest() 
		{
			LongOpt test = new LongOpt("test", Argument.Required, null, 32);
			Assert.AreEqual(test.Flag, null, "Flag");
			Assert.AreEqual(test.HasArg, Argument.Required, "HasArg");
			Assert.AreEqual(test.Name, "test", "Name");
			Assert.AreEqual(test.Val, 32, "Val");
			StringBuilder sb = new StringBuilder(10); 
			test = new LongOpt("test2", Argument.No, sb, 12);
			Assert.AreSame(test.Flag, sb, "StringBuilder");
		}
	}
}
