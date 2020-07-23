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

using Orts.Formats.Msts;
using ORTS.Common;
using System.IO;
using Xunit;

namespace Tests.Orts.Formats.Msts
{
    public class ConsistFileTests
    {
        [Fact]
        public static void TestTrainProperties()
        {
            ITrainFile train;
            using (TestContent content = new TestContent())
                train = new ConsistFile(MakeTestFile(content));

            Assert.Equal("Test consist", train.DisplayName);
            Assert.Equal(36.65728f, train.MaxVelocityMpS);
            Assert.Equal(0.5f, train.Durability);
            Assert.True(train.PlayerDrivable);
        }

        private static string MakeTestFile(TestContent content)
        {
            const string text = @"SIMISA@@@@@@@@@@JINX0D0t______

Train (
	TrainCfg ( ""test""
		Name(""Test consist"")
		Serial(1)
		MaxVelocity(36.65728 1.00000)
		NextWagonUID(7)
		Durability(0.50000)
		Wagon(
			WagonData(us2bnsfcar US2BNSFCAR)
			UiD(0)
		)
		Engine(
			UiD(1)
			EngineData(dash9 DASH9)
		)
		Wagon(
			WagonData(us2graincar US2GRAINCAR)
			UiD(2)
		)
		Wagon(
			WagonData(us2graincar US2GRAINCAR)
			UiD(3)
		)
		Wagon(
			WagonData(us2graincar US2GRAINCAR)
			UiD(4)
		)
		Wagon(
			WagonData(us2graincar US2GRAINCAR)
			UiD(5)
		)
		Engine(
			Flip()
			UiD(6)
			EngineData(gp38 GP38)
		)
	)
)";
            string path = Path.Combine(content.ConsistsPath, "test.con");
            File.WriteAllText(path, text);
            return path;
        }
    }
}
