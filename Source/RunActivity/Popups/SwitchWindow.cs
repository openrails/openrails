// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS.Popups
{
	public class SwitchWindow : Window
	{
		const int SwitchImageSize = 32;

		Image SwitchForwards;
		Image SwitchBackwards;

        static Texture2D SwitchStates;

		public SwitchWindow(WindowManager owner)
			: base(owner, Window.DecorationSize.X + 2 * SwitchImageSize, Window.DecorationSize.Y + 2 * SwitchImageSize, "Switch")
		{
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (SwitchStates == null)
                SwitchStates = Owner.Viewer.RenderProcess.Content.Load<Texture2D>("SwitchStates");
        }

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			{
				vbox.Add(SwitchForwards = new Image(vbox.RemainingWidth / 4, 0, vbox.RemainingWidth / 2, vbox.RemainingWidth / 2));
				vbox.Add(SwitchBackwards = new Image(vbox.RemainingWidth / 4, 0, vbox.RemainingWidth / 2, vbox.RemainingWidth / 2));
                SwitchForwards.Texture = SwitchBackwards.Texture = SwitchStates;
                SwitchForwards.Click += new Action<Control, Point>(SwitchForwards_Click);
				SwitchBackwards.Click += new Action<Control, Point>(SwitchBackwards_Click);
			}
			return vbox;
		}

		void SwitchForwards_Click(Control arg1, Point arg2)
		{
			Owner.Viewer.Simulator.SwitchTrackAhead(Owner.Viewer.PlayerTrain);
		}

		void SwitchBackwards_Click(Control arg1, Point arg2)
		{
			Owner.Viewer.Simulator.SwitchTrackBehind(Owner.Viewer.PlayerTrain);
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var train = Owner.Viewer.PlayerTrain;
				try
				{
					UpdateSwitch(SwitchForwards, train, true);
					UpdateSwitch(SwitchBackwards, train, false);
				}
				catch (Exception) { }
            }
        }

		void UpdateSwitch(Image image, Train train, bool front)
		{
			image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize);

            var traveller = front ^ train.LeadLocomotive.Flipped ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
			TrackNode SwitchPreviousNode = traveller.TN;
			TrackNode SwitchNode = null;
			while (traveller.NextSection())
			{
				if (traveller.IsJunction)
				{
					SwitchNode = traveller.TN;
					break;
				}
				SwitchPreviousNode = traveller.TN;
			}
			if (SwitchNode == null)
				return;

			Debug.Assert(SwitchPreviousNode != null);
			Debug.Assert(SwitchNode.Inpins == 1);
			Debug.Assert(SwitchNode.Outpins == 2);
			Debug.Assert(SwitchNode.TrPins.Count() == 3);
			Debug.Assert(SwitchNode.TrJunctionNode != null);
			Debug.Assert(SwitchNode.TrJunctionNode.SelectedRoute == 0 || SwitchNode.TrJunctionNode.SelectedRoute == 1);

			var switchPreviousNodeID = Owner.Viewer.Simulator.TDB.TrackDB.TrackNodesIndexOf(SwitchPreviousNode);
			var switchBranchesAwayFromUs = SwitchNode.TrPins[0].Link == switchPreviousNodeID;
			var switchTrackSection = Owner.Viewer.Simulator.TSectionDat.TrackShapes.Get(SwitchNode.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route
			var switchMainRouteIsLeft = (int)switchTrackSection.MainRoute == 0;  // align the switch

			image.Source.X = ((switchBranchesAwayFromUs == front ? 1 : 3) + (switchMainRouteIsLeft ? 1 : 0)) * SwitchImageSize;
			image.Source.Y = SwitchNode.TrJunctionNode.SelectedRoute * SwitchImageSize;
		}
	}
}
