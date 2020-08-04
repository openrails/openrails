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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests.Orts.Formats.OR
{
    public class VehicleListTests
    {
        private static readonly IDictionary<string, string> Folders = new Dictionary<string, string>();

        #region List train type
        [Fact]
        private static void GetListForwardWagonReferences()
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

        [Fact]
        private static void GetListRepeatedForwardWagonReferences()
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
                        Count = 3,
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
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), false, 3),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), true, 4),
                };
                Assert.Equal(expected, train.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetListRepeatedReverseWagonReferences()
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
                        Count = 3,
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
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), true, 3),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), true, 4),
                };
                Assert.Equal(expected, train.GetReverseWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetListLeadLocomotiveChoices()
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
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                };
                Assert.Equal(expected, train.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetListReverseLocomotiveChoices()
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
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                };
                Assert.Equal(expected, train.GetReverseLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetListLeadLocomotiveChoicesWithoutEngine()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                },
            };
            using (var content = new TestContent())
                Assert.Equal(PreferredLocomotive.NoLocomotiveSet, train.GetLeadLocomotiveChoices(content.Path, Folders));
        }

        [Fact]
        private static void GetListReverseLocomotiveChoicesWithoutEngine()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                },
            };
            using (var content = new TestContent())
                Assert.Equal(PreferredLocomotive.NoLocomotiveSet, train.GetReverseLocomotiveChoices(content.Path, Folders));
        }

        [Fact]
        private static void GetEmptyListLeadLocomotiveChoices()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = false,
                List = new ListTrainItem[] { },
            };
            using (var content = new TestContent())
                Assert.Empty(train.GetLeadLocomotiveChoices(content.Path, Folders));
        }

        [Fact]
        private static void GetEmptyListReverseLocomotiveChoices()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = false,
                List = new ListTrainItem[] { },
            };
            using (var content = new TestContent())
                Assert.Empty(train.GetReverseLocomotiveChoices(content.Path, Folders));
        }

        [Fact]
        public static void GetListForwardWagonReferencesGivenUnsatisifablePreference()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                },
            };
            using (var content = new TestContent())
            {
                var unsatisfiable = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "acela", "acela.eng"));
                Assert.Empty(train.GetForwardWagonList(content.Path, Folders, preference: unsatisfiable));
            }
        }

        [Fact]
        public static void DisallowRecursiveListTrains()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                    },
                    new ListTrainReference()
                    {
                        Train = "test",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "test.train-or"), JsonConvert.SerializeObject(train));
                var iterator = train.GetForwardWagonList(content.Path, Folders);
                Assert.Throws<RecursiveTrainException>(() => iterator.Count());
            }
        }

        [Fact]
        public static void GetListReverseWagonReferencesGivenUnsatisifablePreference()
        {
            var train = new ListTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Flip = true,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var unsatisfiable = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "acela", "acela.eng"));
                Assert.Empty(train.GetForwardWagonList(content.Path, Folders, preference: unsatisfiable));
            }
        }
        #endregion

        #region List train type -> List train type
        [Fact]
        public static void GetListInListForwardWagonReferences()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon3",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon4",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon1.wag"), false, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon2.wag"), false, 2),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), false, 3),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon3.wag"), false, 4),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon4.wag"), false, 5),
                };
                Assert.Equal(expected, parentTrain.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetReverseListInListForwardWagonReferences()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                    new ListTrainReference()
                    {
                        Train = "child",
                        Flip = true,
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon3",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon4",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon1.wag"), false, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon2.wag"), false, 2),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon4.wag"), true, 3),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon3.wag"), true, 4),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), true, 5),
                };
                Assert.Equal(expected, parentTrain.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetReverseListInListReverseWagonReferences()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                    new ListTrainReference()
                    {
                        Train = "child",
                        Flip = true,
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon3",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon4",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon3.wag"), false, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon4.wag"), false, 2),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon2.wag"), true, 3),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon1.wag"), true, 4),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), true, 5),
                };
                Assert.Equal(expected, parentTrain.GetReverseWagonList(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetListInListLeadLocomotiveChoices()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                };
                Assert.Equal(expected, parentTrain.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetListInListReverseLocomotiveChoicesWithLeadingWagon()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                };
                Assert.Equal(expected, parentTrain.GetReverseLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetListInListLeadLocomotiveChoicesWithoutEngine()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon2",
                    },
                },
            };
            var childTrain = new ListTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon3",
                    },
                    new ListTrainWagon()
                    {
                        Wagon = "SomeWagon4",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                Assert.Equal(PreferredLocomotive.NoLocomotiveSet, parentTrain.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        public static void DisallowIndirectlyRecursiveListTrains()
        {
            var trainA = new ListTrainFile()
            {
                DisplayName = "Train A",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                    },
                    new ListTrainReference()
                    {
                        Train = "trainB",
                    },
                },
            };
            var trainB = new ListTrainFile()
            {
                DisplayName = "Train B",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "trainC",
                    },
                },
            };
            var trainC = new ListTrainFile()
            {
                DisplayName = "Train C",
                PlayerDrivable = false,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "trainA",
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "trainA.train-or"), JsonConvert.SerializeObject(trainA));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "trainB.train-or"), JsonConvert.SerializeObject(trainB));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "trainC.train-or"), JsonConvert.SerializeObject(trainC));
                var iterator = trainA.GetForwardWagonList(content.Path, Folders);
                Assert.Throws<RecursiveTrainException>(() => iterator.Count());
            }
        }
        #endregion

        #region Random train type
        [Fact]
        private static void GetRandomOneForwardWagonReference()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Probability = 0.5f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), false, 0),
                };
                Assert.Equal(expected, train.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomOneReverseWagonReferenceWithZeroProbabilityItem()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 1f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), true, 0),
                };
                Assert.Equal(expected, train.GetReverseWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomRepeatedForwardWagonReferences()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Count = 3,
                        Probability = 0.5f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Count = 3,
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), false, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), false, 2),
                };
                Assert.Equal(expected, train.GetForwardWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomRepeatedReverseWagonReferences()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Count = 3,
                        Probability = 0.5f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotive",
                        Count = 3,
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), true, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), true, 1),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotive.eng"), true, 2),
                };
                Assert.Equal(expected, train.GetReverseWagonList(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomLeadLocomotiveChoices()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.33f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.33f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveC",
                        Probability = 0.34f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveC.eng")),
                };
                Assert.Equal(expected, train.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomReverseLocomotiveChoices()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.33f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.33f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveC",
                        Probability = 0.34f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveC.eng")),
                };
                Assert.Equal(expected, train.GetReverseLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomLeadLocomotiveChoicesWithWagon()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.33f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.33f,
                    },
                    new RandomTrainWagon()
                    {
                        Wagon = "SomeWagon1",
                        Probability = 0.34f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                    PreferredLocomotive.NoLocomotive,
                };
                Assert.Equal(expected, train.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomReverseLocomotiveChoicesWithZeroProbabilityItem()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.5f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveC",
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveC.eng")),
                };
                Assert.Equal(expected, train.GetReverseLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        private static void GetRandomForwardWagonReferenceGivenPreference()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.9f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.1f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), false, 0),
                };
                var preference = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"));
                Assert.Equal(expected, train.GetForwardWagonList(content.Path, Folders, preference));
            }
        }

        private static void GetRandomReverseWagonReferenceGivenPreference()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.1f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.9f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"), true, 0),
                };
                var preference = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng"));
                Assert.Equal(expected, train.GetReverseWagonList(content.Path, Folders, preference));
            }
        }

        [Fact]
        private static void GetRandomForwardWagonReferenceGivenUnsatisfiablePreference()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = true,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.5f,
                    },
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                var unsatisfiable = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "acela", "acela.eng"));
                Assert.Empty(train.GetForwardWagonList(content.Path, Folders, preference: unsatisfiable));
            }
        }

        [Fact]
        private static void GetEmptyRandomLeadLocomotiveChoices()
        {
            var train = new RandomTrainFile()
            {
                DisplayName = "Test train",
                PlayerDrivable = false,
                Random = new RandomTrainItem[] { },
            };
            using (var content = new TestContent())
                Assert.Empty(train.GetLeadLocomotiveChoices(content.Path, Folders));
        }
        #endregion

        #region List train type -> Random train type
        [Fact]
        public static void GetRandomInListLeadLocomotiveChoicesWithLeadingWagon()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                },
            };
            var childTrain = new RandomTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = false,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.5f,
                    },
                    new RandomTrainWagon()
                    {
                        Wagon = "SomeWagon",
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                };
                Assert.Equal(expected, parentTrain.GetLeadLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetRandomInListReverseLocomotiveChoicesWithLeadingWagon()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                },
            };
            var childTrain = new RandomTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = false,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.5f,
                    },
                    new RandomTrainWagon()
                    {
                        Wagon = "SomeWagon",
                        Probability = 0.5f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new HashSet<PreferredLocomotive>()
                {
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveA.eng")),
                    new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng")),
                };
                Assert.Equal(expected, parentTrain.GetReverseLocomotiveChoices(content.Path, Folders));
            }
        }

        [Fact]
        public static void GetRandomInListForwardWagonReferencesWithLeadingWagon()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                },
            };
            var childTrain = new RandomTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = false,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.9f,
                    },
                    new RandomTrainWagon()
                    {
                        Wagon = "SomeWagon",
                        Probability = 0.1f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), false, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), false, 1),
                };
                var preference = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"));
                Assert.Equal(expected, parentTrain.GetForwardWagonList(content.Path, Folders, preference));
            }
        }

        [Fact]
        public static void GetRandomInListReverseWagonReferencesWithLeadingWagon()
        {
            var parentTrain = new ListTrainFile()
            {
                DisplayName = "Parent train",
                PlayerDrivable = true,
                List = new ListTrainItem[]
                {
                    new ListTrainEngine()
                    {
                        Engine = "SomeLocomotiveB",
                    },
                    new ListTrainReference()
                    {
                        Train = "child",
                    },
                },
            };
            var childTrain = new RandomTrainFile()
            {
                DisplayName = "Child train",
                PlayerDrivable = false,
                Random = new RandomTrainItem[]
                {
                    new RandomTrainEngine()
                    {
                        Engine = "SomeLocomotiveA",
                        Probability = 0.9f,
                    },
                    new RandomTrainWagon()
                    {
                        Wagon = "SomeWagon",
                        Probability = 0.1f,
                    },
                },
            };
            using (var content = new TestContent())
            {
                File.WriteAllText(Path.Combine(content.ConsistsPath, "parent.train-or"), JsonConvert.SerializeObject(parentTrain));
                File.WriteAllText(Path.Combine(content.ConsistsPath, "child.train-or"), JsonConvert.SerializeObject(childTrain));
                var expected = new[]
                {
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeWagon.wag"), true, 0),
                    new WagonReference(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"), true, 1),
                };
                var preference = new PreferredLocomotive(Path.Combine(content.TrainsetPath, "SomeLocomotiveB.eng"));
                Assert.Equal(expected, parentTrain.GetReverseWagonList(content.Path, Folders, preference));
            }
        }
        #endregion
    }
}
