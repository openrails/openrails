using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

using NUnit.Framework;

using GNU.Gettext.Msgfmt;


namespace GNU.Gettext.Test
{
	[TestFixture()]
	public class MsgfmtTest
	{
		[Test()]
		public void AssemblyGenerationTest()
		{
			Options options = new Options();
			options.Mode = Mode.SateliteAssembly;
			options.InputFiles.Add("../../../Examples.Hello/po/fr.po");
			options.BaseName = "Examples.Hello.Messages";
			options.OutDir = "../../../Examples.Hello/bin/Debug";
			if (Path.DirectorySeparatorChar == '\\')
				options.CompilerName = "csc";
			else
				options.CompilerName = "mcs";
			options.LibDir = "./";
			options.Locale = new CultureInfo("fr-FR");
			options.Verbose = true;
			options.DebugMode = true;

			AssemblyGen gen = new AssemblyGen(options);
			try
			{
				gen.Run();
			}
			catch(Exception e)
			{
				Assert.Fail("Assebly generation faild. Check that CSharp compiler is in PATH.\n{0}", e.Message);
			}
		}

		[Test()]
		public void ResourcesGenerationTest()
		{
			Options options = new Options();
			options.Mode = Mode.Resources;
			options.InputFiles.Add("./Data/Test01.po");
			options.OutFile = "./Messages.fr-FR.resources";
			options.Verbose = true;

			ResourcesGen gen = new ResourcesGen(options);
			gen.Run();
		}
	}
}

