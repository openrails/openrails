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

using Orts.Formats.OR;
using ORTS.Content;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Tests.Orts.Formats.OR
{
    public class TrainFileTests
    {
        private static readonly IDictionary<string, string> Folders = new Dictionary<string, string>();

        [Fact]
        private static void GetTrainForwardWagonReferences()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Flip = true,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), false, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), false, 2),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), true, 3),
                };
                Assert.Equal(expected, train.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetListReverseWagonReferences()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Flip = true,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), true, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), true, 2),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), true, 3),
                };
                Assert.Equal(expected, train.GetReverseWagonList(content.Path, Folders));
            }
        }
    }
}
