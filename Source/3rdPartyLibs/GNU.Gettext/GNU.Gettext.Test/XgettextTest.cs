using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

using GNU.Gettext.Xgettext;

namespace GNU.Gettext.Test
{
	[TestFixture()]
	public class XgettextTest
	{
		[Test()]
		public void ExtractorCSharpTest()
		{
			string ressourceId = String.Format("{0}.{1}", this.GetType().Assembly.GetName().Name, "Data.XgettextTest.txt");
			string text = "";
			using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(ressourceId))
			{
		        using (StreamReader reader = new StreamReader(stream))
				{
					text = reader.ReadToEnd();
				}
			}
			
			Options options = new Options();
			options.InputFiles.Add(@"./Test/File/Name.cs"); // File wont be used, feed the plain text
			options.OutFile = @"./Test.pot";
			options.Overwrite = true;
			ExtractorCsharp extractor = new ExtractorCsharp(options);
			extractor.GetMessages(text, options.InputFiles[0]);
			extractor.Save();
			
			int ctx = 0;
			int multiline = 0;
			foreach(CatalogEntry entry in extractor.Catalog)
			{
				if (entry.HasContext)
					ctx++;
				if (entry.String == "multiline-string-1-string-2" ||
				    entry.String == "Multiline Hint for label1")
					multiline++;
			}
			
			Assert.AreEqual(2, ctx, "Context count");
			
			Assert.AreEqual(2, extractor.Catalog.PluralFormsCount, "PluralFormsCount");
			Assert.AreEqual(17, extractor.Catalog.Count, "Duplicates may not detected");
			Assert.AreEqual(2, multiline, "Multiline string");
		}
		
		[Test()]
		public void RemoveCommentsTest()
		{
			string input = @"
/*
 *
 * This
 * is
 * // Comment
 */
string s = ""/*This is not comment*/"";
string s2 = ""This is //not comment too"";
button1.Text = ""Save""; // Save data.Text = ""10""
//button1.Text = ""Save""; // Save data.Text = ""10""
// button1.Text = ""Save""; // Save data.Text = ""10""
/*button1.Text = ""Save""; // Save data.Text = ""10""*/
";
			string output = ExtractorCsharp.RemoveComments(input);
			Assert.IsTrue(output.IndexOf("/*This is not comment*/") >= 0, "Multiline comment chars in string");
			Assert.IsTrue(output.IndexOf("This is //not comment too") >= 0, "Single line comment chars in string");
			Assert.AreEqual(-1, output.IndexOf("// Save"), "Single line comment");
			Assert.AreEqual(-1, output.IndexOf("//button1"), "Single line comment");
			Assert.AreEqual(-1, output.IndexOf("/*\n"), "Multi line comment");
			Assert.AreEqual(-1, output.IndexOf("/*button1"), "Multi line comment in single line");
		}
	}
	
}

