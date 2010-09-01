/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: James Ross
/// 
/// Train Operations; used to uncouple and put brakes on train cars.
/// 

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
			AlignCenter();
			if (CouplerTexture == null)
				CouplerTexture = owner.Viewer.RenderProcess.Content.Load<Texture2D>("TrainOperationsCoupler");
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var hbox = base.Layout(layout).AddLayoutHorizontal();
			var scrollbox = hbox.AddLayoutScrollboxHorizontal(hbox.RemainingHeight);
			if (PlayerTrain != null)
			{
				foreach (var car in PlayerTrain.Cars)
				{
					var carLabel = new Label(CarWidth, CarListHeight, "0-" + car.UiD, LabelAlignment.Center);
					scrollbox.Add(carLabel);
					if (car != PlayerTrain.Cars.Last())
						scrollbox.Add(new TrainOperationsCoupler(0, (CarListHeight - CouplerSize) / 2, Owner.Viewer.Simulator, car));
				}
			}
			return hbox;
		}

		public void UpdateText(ElapsedTime elapsedTime, Train playerTrain)
		{
			if ((PlayerTrain != playerTrain) || (playerTrain.Cars.Count != LastPlayerTrainCars))
			{
				PlayerTrain = playerTrain;
				LastPlayerTrainCars = playerTrain.Cars.Count;
				Layout();
			}
			// FIXME
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
