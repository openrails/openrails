// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
	public class TrainOperationsWindow : Window
	{
		const int CarListHeight = 16;
		const int CarListPadding = 2;
		const int CarWidth = 100;
		internal const int CouplerSize = 16;
		internal static Texture2D CouplerTexture;
		Train PlayerTrain;
		int LastPlayerTrainCars;

		public TrainOperationsWindow(WindowManager owner)
			: base(owner, 600, Window.DecorationSize.Y + CarListHeight + CarListPadding + ControlLayoutScrollbox.ScrollbarSize, "Train Operations")
		{
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (CouplerTexture == null)
                CouplerTexture = Owner.Viewer.RenderProcess.Content.Load<Texture2D>("TrainOperationsCoupler");
        }

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var hbox = base.Layout(layout).AddLayoutHorizontal();
			var scrollbox = hbox.AddLayoutScrollboxHorizontal(hbox.RemainingHeight);
			if (PlayerTrain != null)
			{
				foreach (var car in PlayerTrain.Cars)
				{
					var carLabel = new Label(CarWidth, CarListHeight, car.CarID, LabelAlignment.Center);
					scrollbox.Add(carLabel);
					if (car != PlayerTrain.Cars.Last())
						scrollbox.Add(new TrainOperationsCoupler(0, (CarListHeight - CouplerSize) / 2, Owner.Viewer.Simulator, car));
				}
			}
			return hbox;
		}

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                if ((PlayerTrain != Owner.Viewer.PlayerTrain) || (Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars))
                {
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    Layout();
                }
            }
        }
	}

	class TrainOperationsCoupler : Image
	{
		readonly Simulator Simulator;
		readonly TrainCar Car;

		public TrainOperationsCoupler(int x, int y, Simulator simulator, TrainCar car)
			: base(x, y, TrainOperationsWindow.CouplerSize, TrainOperationsWindow.CouplerSize)
		{
			Simulator = simulator;
			Car = car;
			Texture = TrainOperationsWindow.CouplerTexture;
			Source = new Rectangle(0, 0, TrainOperationsWindow.CouplerSize, TrainOperationsWindow.CouplerSize);
			Click += new Action<Control, Point>(TrainOperationsCoupler_Click);
		}

		void TrainOperationsCoupler_Click(Control arg1, Point arg2)
		{
			Simulator.UncoupleBehind(Car);
		}
	}
}
