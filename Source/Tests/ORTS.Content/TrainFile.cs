// COPYRIGHT 2020 by the Open Rails project.
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

using Newtonsoft.Json;
using Orts.Formats.OR;
using ORTS.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests.ORTS.Content
{
    public class TrainFileTests
    {
        [Fact]
        public static void CompareSimilarWagonReferences() => AssertAllEqual(
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\msts\trains\trainset\somedirectory\somewagon.WAG", false, 0),
            new WagonReference(@"c:/Msts/Trains/Trainset/Somedirectory/Somewagon.wAg", false, 0));

        [Fact]
        public static void CompareSimilarPreferredLocomotives() => AssertAllEqual(
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\msts\trains\trainset\somedirectory\someengine.ENG"),
            new PreferredLocomotive(@"c:/Msts/Trains/Trainset/Somedirectory/Someengine.EnG"));

        [Fact]
        public static void CompareSimilarWagonReferenceHashCodes() => AssertAllEqualHashCodes(
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\msts\trains\trainset\somedirectory\somewagon.WAG", false, 0),
            new WagonReference(@"c:/Msts/Trains/Trainset/Somedirectory/Somewagon.wAg", false, 0));

        [Fact]
        public static void CompareSimilarPreferredLocomotiveHashCodes() => AssertAllEqualHashCodes(
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\msts\trains\trainset\somedirectory\someengine.ENG"),
            new PreferredLocomotive(@"c:/Msts/Trains/Trainset/Somedirectory/Someengine.EnG"));

        [Fact]
        public static void ResolveWagonFileWithPeriods()
        {
            const string filename = "wagon.with.periods";
            var subFolders = new[] { "testwagon" };
            using (var content = new TestContent())
            {
                var expected = subFolders
                    .Prepend(content.TrainsetPath)
                    .Append($"{filename}.wag")
                    .ToArray();
                AssertPathsEqual(Path.Combine(expected), TrainFileUtilities.ResolveWagonFile(content.Path, subFolders, filename));
            }
        }

        [Fact]
        public static void ResolveEngineFileWithPeriods()
        {
            const string filename = "engine.with.periods";
            var subFolders = new[] { "testengine" };
            using (var content = new TestContent())
            {
                var expected = subFolders
                    .Prepend(content.TrainsetPath)
                    .Append($"{filename}.eng")
                    .ToArray();
                AssertPathsEqual(Path.Combine(expected), TrainFileUtilities.ResolveEngineFile(content.Path, subFolders, filename));
            }
        }

        [Fact]
        public static void ResolveStandaloneMstsConsistFile() => ResolveConsist("test");

        [Fact]
        public static void ResolveStandaloneOrtsTrainFile() => ResolveTrain("test");

        [Fact]
        public static void ResolveShadowingOrtsTrainFile() => ResolveTrainThatShadowsConsist("test");

        [Fact]
        public static void ResolveStandaloneMstsConsistFileWithPeriods() => ResolveConsist("test.with.periods");

        [Fact]
        public static void ResolveStandaloneOrtsTrainFileWithPeriods() => ResolveTrain("test.with.periods");

        [Fact]
        public static void ResolveShadowingOrtsTrainFileWithPeriods() => ResolveTrainThatShadowsConsist("test.with.periods");

        private static void ResolveConsist(string filename)
        {
            using (var content = new TestContent())
                AssertPathsEqual(MakeMstsConsistFile(content, filename), TrainFileUtilities.ResolveTrainFile(content.Path, filename));
        }

        private static void ResolveTrain(string filename)
        {
            using (var content = new TestContent())
                AssertPathsEqual(MakeOrtsTrainFile(content, filename), TrainFileUtilities.ResolveTrainFile(content.Path, filename));
        }
        private static void ResolveTrainThatShadowsConsist(string filename)
        {
            using (var content = new TestContent())
            {
                MakeMstsConsistFile(content, filename);
                AssertPathsEqual(MakeOrtsTrainFile(content, filename), TrainFileUtilities.ResolveTrainFile(content.Path, filename));
            }
        }

        [Fact]
        public static void EnumerateOneConsistFile() =>
            TestAllTrainFiles(consists: new[] { "test" });

        [Fact]
        public static void EnumerateMultipleConsistFiles() =>
            TestAllTrainFiles(consists: new[] { "test1", "test2", "test3" });

        [Fact]
        public static void EnumerateMultipleConsistFilesWithPeriods() =>
            TestAllTrainFiles(consists: new[] { "test.period.1", "test.period.2", "test.period.3" });

        [Fact]
        public static void EnumerateOneTrainFile() =>
            TestAllTrainFiles(trains: new[] { "test" });

        [Fact]
        public static void EnumerateMultipleTrainFiles() =>
            TestAllTrainFiles(trains: new[] { "test1", "test2", "test3" });

        [Fact]
        public static void EnumerateMultipleTrainFilesWithPeriods() =>
            TestAllTrainFiles(trains: new[] { "test.period.1", "test.period.2", "test.period.3" });

        [Fact]
        public static void EnumerateConsistAndTrainFiles() =>
            TestAllTrainFiles(consists: new[] { "test1", "test2", "test3" }, trains: new[] { "testA", "testB", "testC" });

        [Fact]
        public static void EnumerateConsistAndTrainFilesWithPeriods() =>
            TestAllTrainFiles(consists: new[] { "test.no.1", "test.no.2", "test.no.3" }, trains: new[] { "te.st.A", "te.st.B", "te.st.C" });

        [Fact]
        public static void EnumerateTrainFileShadowingConsistFile() =>
            TestAllTrainFiles(consists: new[] { "test" }, trains: new[] { "test" });

        [Fact]
        public static void EnumerateMultipleTrainFilesShadowingConsistFiles() =>
            TestAllTrainFiles(consists: new[] { "test1", "test2", "testA" }, trains: new[] { "test1", "test2", "testB" });

        private static void TestAllTrainFiles(string[] consists = null, string[] trains = null)
        {
            var empty = new string[0] { };
            consists = consists ?? empty;
            trains = trains ?? empty;

            using (var content = new TestContent())
            {
                foreach (var filename in consists)
                    MakeMstsConsistFile(content, filename);
                foreach (var filename in trains)
                    MakeOrtsTrainFile(content, filename);

                var trainsSet = new HashSet<string>(trains, StringComparer.InvariantCultureIgnoreCase);
                var expected = consists
                    .Where((string filename) => !trainsSet.Contains(filename))
                    .Select((string filename) => Path.GetFullPath(Path.Combine(content.ConsistsPath, $"{filename}.con")))
                    .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
                expected.UnionWith(trains
                    .Select((string filename) => Path.GetFullPath(Path.Combine(content.ConsistsPath, $"{filename}.train-or"))));

                foreach (var trainFile in TrainFileUtilities.AllTrainFiles(content.Path))
                    Assert.Contains(trainFile, expected);
            }
        }

        private static string MakeOrtsTrainFile(TestContent content, string filename)
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                MaxVelocityMpS = 0f,
                List = new ListTrainItem[]
                {
                    new ListTrainWagon()
                    {
                        Wagon = Path.Combine("us2graincar", "US2GRAINCAR"),
                    },
                },
            };
            var path = Path.Combine(content.ConsistsPath, $"{filename}.train-or");
            File.WriteAllText(path, JsonConvert.SerializeObject(train));
            return path;
        }

        private static string MakeMstsConsistFile(TestContent content, string filename)
        {
            var text = @"SIMISA@@@@@@@@@@JINX0D0t______

Train (
	TrainCfg ( """ + filename + @"""
		Name(""Test consist"")
		Serial(1)
		MaxVelocity(0.00000 0.10000)
		NextWagonUID(1)
		Durability(1.00000)
		Wagon(
			WagonData(us2graincar US2GRAINCAR)
			UiD(0)
		)
	)
)";
            var path = Path.Combine(content.ConsistsPath, $"{filename}.con");
            File.WriteAllText(path, text);
            return path;
        }

        private static void AssertPathsEqual(params string[] paths)
        {
            var pairs = from a in paths
                        from b in paths
                        select Tuple.Create(a, b);
            foreach ((string a, string b) in pairs)
                Assert.Equal(Path.GetFullPath(a), Path.GetFullPath(b), StringComparer.InvariantCultureIgnoreCase);
        }

        private static void AssertAllEqual<T>(params T[] sequence)
        {
            var pairs = from a in sequence
                        from b in sequence
                        select Tuple.Create(a, b);
            foreach ((T a, T b) in pairs)
                Assert.Equal(a, b);
        }

        private static void AssertAllEqualHashCodes<T>(params T[] sequence)
        {
            var pairs = from a in sequence
                        from b in sequence
                        select Tuple.Create(a, b);
            foreach ((T a, T b) in pairs)
                Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }
}
