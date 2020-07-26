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
        private static readonly IDictionary<string, string> Folders = new Dictionary<string, string>();

        [Fact]
        public static void CompareCaseInsensitiveWagonReferences() => AssertAllEqual(
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeWagon.wag", false, 0),
            new WagonReference(@"C:\msts\trains\trainset\somedirectory\somewagon.WAG", false, 0),
            new WagonReference(@"c:/Msts/Trains/Trainset/Somedirectory/Somewagon.wAg", false, 0));

        [Fact]
        public static void CompareCaseInsensitivePreferredLocomotives() => AssertAllEqual(
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\MSTS\TRAINS\TRAINSET\..\..\TRAINS\TRAINSET\SomeDirectory\SomeEngine.eng"),
            new PreferredLocomotive(@"C:\msts\trains\trainset\somedirectory\someengine.ENG"),
            new PreferredLocomotive(@"c:/Msts/Trains/Trainset/Somedirectory/Someengine.EnG"));

        [Fact]
        public static void ResolveStandaloneMstsConsistFile()
        {
            using (var content = new TestContent())
                Assert.Equal(MakeMstsConsistFile(content), TrainFileUtilities.ResolveTrainFile(content.Path, "test"), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public static void ResolveStandaloneOrtsTrainFile()
        {
            using (var content = new TestContent())
                Assert.Equal(MakeOrtsTrainFile(content), TrainFileUtilities.ResolveTrainFile(content.Path, "test"), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public static void ResolveShadowingOrtsTrainFile()
        {
            using (var content = new TestContent())
            {
                MakeMstsConsistFile(content);
                Assert.Equal(MakeOrtsTrainFile(content), TrainFileUtilities.ResolveTrainFile(content.Path, "test"), StringComparer.InvariantCultureIgnoreCase);
            }
        }

        private static string MakeOrtsTrainFile(TestContent content)
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
            var path = Path.Combine(content.ConsistsPath, "test.train-or");
            File.WriteAllText(path, JsonConvert.SerializeObject(train));
            return path;
        }

        private static string MakeMstsConsistFile(TestContent content)
        {
            const string text = @"SIMISA@@@@@@@@@@JINX0D0t______

Train (
	TrainCfg ( ""test""
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
            var path = Path.Combine(content.ConsistsPath, "test.con");
            File.WriteAllText(path, text);
            return path;
        }

        private static void AssertAllEqual<T>(params T[] sequence)
        {
            var pairs = from a in sequence
                        from b in sequence
                        select Tuple.Create(a, b);
            foreach ((T a, T b) in pairs)
                Assert.Equal(a, b);
        }
    }
}
