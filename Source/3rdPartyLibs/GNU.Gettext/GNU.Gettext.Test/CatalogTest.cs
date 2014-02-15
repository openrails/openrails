using System;

using NUnit.Framework;

namespace GNU.Gettext.Test
{
	[TestFixture()]
	public class CatalogTest
	{
		[Test()]
		public void ParsingTest()
		{
			Catalog cat = new Catalog();
			cat.Load("./Data/Test01.po");

			Assert.AreEqual(6, cat.Count, "Entries count");
			Assert.AreEqual(3, cat.PluralFormsCount, "Plurals entries count");

			int nonTranslatedCount = 0;
			int ctx = 0;
			foreach(CatalogEntry entry in cat)
			{
				if (!entry.IsTranslated)
					nonTranslatedCount++;
				if (entry.HasPlural)
				{
					Assert.AreEqual("{0} ошибка найдена", entry.GetTranslation(0));
					Assert.AreEqual("{0} ошибки найдены", entry.GetTranslation(1));
					Assert.AreEqual("{0} ошибок найдено", entry.GetTranslation(2));
				}
				if (entry.HasContext)
					ctx++;
			}

			Assert.AreEqual(1, nonTranslatedCount, "Non translated strings count");
			Assert.AreEqual(2, ctx, "Contextes count");
		}
		
		
		[Test]
		public void ToGettextFormatTest()
		{
			Assert.AreEqual("123456", StringEscaping.UnEscape(StringEscaping.EscapeMode.CSharp, "123456"), "Case 1");
			Assert.AreEqual(@"12""3""456", StringEscaping.UnEscape(StringEscaping.EscapeMode.CSharpVerbatim, "12\"\"3\"\"456"), "Case 2");
			Assert.AreEqual("12\r\n\"3\"456", StringEscaping.UnEscape(StringEscaping.EscapeMode.CSharp, "12\\r\\n\\\"3\\\"456"), "Case 3");
			Assert.AreEqual("12\r\n\"3\"\r\n456", StringEscaping.UnEscape(StringEscaping.EscapeMode.CSharpVerbatim, 
				@"12
""""3""""
456"), "Case 4");
		}
	}
}

