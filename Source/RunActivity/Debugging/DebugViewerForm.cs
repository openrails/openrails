/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
///
/// Principal Author:
///     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
/// Contributor:
///    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
/// 


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using System.Windows.Forms;
using MSTS;
using ORTS.Interlocking;
namespace ORTS.Debugging
{


   /// <summary>
   /// Defines an external window for use as a debugging viewer 
   /// when using Open Rails 
   /// </summary>
   public partial class DispatchViewer : Form
   {


      #region Data Viewers
	  //public MessageViewer MessageViewer;
      #endregion

      /// <summary>
      /// Reference to the main simulator object.
      /// </summary>
      private readonly Simulator simulator;


      private int IM_Width = 720;
      private int IM_Height = 720;

	  private int X;
	  private int Y; //X, Y of mouse
	  /// <summary>
	  /// True when the user is dragging the route view
	  /// </summary>
	  private bool Dragging = false;
	  private WorldPosition worldPos;
	  string name = "";
	  List<SwitchWidget> switchItemsDrawn;
	  List<SignalWidget> signalItemsDrawn;

	  public SwitchWidget switchPickedItem = null;
	  public SignalWidget signalPickedItem = null;
	  bool switchPickedItemChanged = false;
	  PointF switchPickedLocation = new PointF();
	  public bool switchPickedItemHandled = false;
	  public double switchPickedTime = 0.0f;
	  bool signalPickedItemChanged = false;
	  PointF signalPickedLocation = new PointF();
	  public bool signalPickedItemHandled = false;
	  public double signalPickedTime = 0.0f;

	  ImageList imageList1 = null;
	  /// <summary>
	  /// contains the last position of the mouse
	  /// </summary>
	  private System.Drawing.Point LastCursorPosition = new System.Drawing.Point();


      /// <summary>
      /// True when the user has the "Move left" pressed.
      /// </summary>
      private bool LeftButtonDown = false;

      /// <summary>
      /// True when the user has the "Move right" pressed.
      /// </summary>
      private bool RightButtonDown = false;

      /// <summary>
      /// True when the user has the "Move up" pressed.
      /// </summary>
      private bool UpButtonDown = false;

      /// <summary>
      /// True when the user has the "Move down" pressed.
      /// </summary>
      private bool DownButtonDown = false;


       /// <summary>
      /// Defines the area to view, in meters.
      /// </summary>
      private RectangleF ViewWindow;

      /// <summary>
      /// Used to periodically check if we should shift the view when the
      /// user is holding down a "shift view" button.
      /// </summary>
      private Timer UITimer;

      bool loaded = false;
	  TrackNode[] nodes;
	  float minX = float.MaxValue;
	  float minY = float.MaxValue;

	  float maxX = float.MinValue;
	  float maxY = float.MinValue;

	  Viewer3D Viewer;
      /// <summary>
      /// Creates a new DebugViewerForm.
      /// </summary>
      /// <param name="simulator"></param>
      /// /// <param name="viewer"></param>
      public DispatchViewer(Simulator simulator, Viewer3D viewer)
      {
         InitializeComponent();

         if (simulator == null)
         {
            throw new ArgumentNullException("simulator", "Simulator object cannot be null.");
         }

         this.simulator = simulator;
		 this.Viewer = viewer;

		 nodes = simulator.TDB.TrackDB.TrackNodes;

		 trainFont = new Font("Arial", 14, FontStyle.Bold);
		 sidingFont = new Font("Arial", 12, FontStyle.Bold);

		 trainBrush = new SolidBrush(Color.Red);
		 sidingBrush = new SolidBrush(Color.Blue);


         // initialise the timer used to handle user input
         UITimer = new Timer();
         UITimer.Interval = 100;
         UITimer.Tick += new EventHandler(UITimer_Tick);
         UITimer.Start();

         ViewWindow = new RectangleF(0, 0, 5000f, 5000f);
         windowSizeUpDown.Accelerations.Add(new NumericUpDownAcceleration(1, 100));

      

		  InitData();
        InitImage();
		chkShowAvatars.Checked = Program.Simulator.Settings.ShowAvatar;
		if (!MultiPlayer.MPManager.IsMultiPlayer())
		{
			msgAll.Enabled = false; msgSelected.Enabled = false; composeMSG.Enabled = false;
		}
		  /*
		if (MultiPlayer.MPManager.IsMultiPlayer())
		{
			MessageViewer = new MessageViewer();
			MessageViewer.Show();
			MessageViewer.Visible = false;
		}*/
      }


      public int RedrawCount = 0;
	  private Font trainFont;
	  private Font sidingFont;
	  private SolidBrush trainBrush;
	  private SolidBrush sidingBrush;
	  private double lastUpdateTime = 0;

      /// <summary>
      /// When the user holds down the  "L", "R", "U", "D" buttons,
      /// shift the view. Avoids the case when the user has to click
      /// buttons like crazy.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void UITimer_Tick(object sender, EventArgs e)
      {
		  if (Viewer.DebugViewerEnabled == false) { this.Visible = false; firstShow = true; return; }
		  else this.Visible = true;
         if (DownButtonDown)
         {
            ShiftViewDown();
         }

         if (UpButtonDown)
         {
            ShiftViewUp();
         }

         if (LeftButtonDown)
         {
            ShiftViewLeft();
         }

         if (RightButtonDown)
         {
            ShiftViewRight();
         }

		 if (Program.Simulator.GameTime - lastUpdateTime < 1) return;
		 lastUpdateTime = Program.Simulator.GameTime;

            GenerateView();
      }

	  private void InitData()
	  {
		  if (!loaded)
		  {
			  // do this only once
			  loaded = true;
			  //trackSections.DataSource = new List<InterlockingTrack>(simulator.InterlockingSystem.Tracks.Values).ToArray();
		  }

		  switchItemsDrawn = new List<SwitchWidget>();
		  signalItemsDrawn = new List<SignalWidget>();
		  switches = new List<SwitchWidget>();
		  for (int i = 0; i < nodes.Length; i++)
		  {
			  TrackNode currNode = nodes[i];

			  if (currNode != null)
			  {

				  if (currNode.TrEndNode)
				  {
					  //buffers.Add(new PointF(currNode.UiD.TileX * 2048 + currNode.UiD.X, currNode.UiD.TileZ * 2048 + currNode.UiD.Z));
				  }
				  else if (currNode.TrVectorNode != null)
				  {

					  if (currNode.TrVectorNode.TrVectorSections.Length > 1)
					  {
						  AddSegments(segments, currNode, currNode.TrVectorNode.TrVectorSections, ref minX, ref minY, ref maxX, ref maxY, simulator);
					  }
					  else
					  {
						  TrVectorSection s = currNode.TrVectorNode.TrVectorSections[0];

						  foreach (TrPin pin in currNode.TrPins)
						  {

							  TrackNode connectedNode = nodes[pin.Link];


							  //bool occupied = false;

							  //if (simulator.InterlockingSystem.Tracks.ContainsKey(connectedNode))
							  //{
							  //occupied = connectedNode   
							  //}

							  //if (currNode.UiD == null)
							  //{
								  PointF A = new PointF(s.TileX * 2048 + s.X, s.TileZ * 2048 + s.Z);
								  PointF B = new PointF(connectedNode.UiD.TileX * 2048 + connectedNode.UiD.X, connectedNode.UiD.TileZ * 2048 + connectedNode.UiD.Z);
								  segments.Add(new LineSegment(A, B, /*s.InterlockingTrack.IsOccupied*/ false, null));
							  //}
						  }


					  }
				  }
				  else if (currNode.TrJunctionNode != null)
				  {
					  foreach (TrPin pin in currNode.TrPins)
					  {
						  if (pin.Direction == 1) continue;
						  TrVectorSection item = null;
						  try
						  {
							  if (nodes[pin.Link].TrVectorNode == null || nodes[pin.Link].TrVectorNode.TrVectorSections.Length < 1) continue;
							  item = nodes[pin.Link].TrVectorNode.TrVectorSections.Last();
						  }
						  catch { continue; }
						  PointF A = new PointF(currNode.UiD.TileX * 2048 + currNode.UiD.X, currNode.UiD.TileZ * 2048 + currNode.UiD.Z);
						  PointF B = new PointF(item.TileX * 2048 + item.X, item.TileZ * 2048 + item.Z);
						  var x = Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2);
						  if (x < 0.1) continue;
						  segments.Add(new LineSegment(B, A, /*s.InterlockingTrack.IsOccupied*/ false, item));
					  }
					  switches.Add(new SwitchWidget(currNode));
				  }
			  }
		  }

		  foreach (TrItem item in simulator.TDB.TrackDB.TrItemTable)
		  {
			  if (item.ItemType == TrItem.trItemType.trSIGNAL)
			  {
				  if (item is SignalItem)
				  {

					  SignalItem si = item as SignalItem;

					  if (si.sigObj >=0  && si.sigObj < simulator.Signals.SignalObjects.Length)
					  {
						  SignalObject s = simulator.Signals.SignalObjects[si.sigObj];

						  signals.Add(new SignalWidget(item, s));
					  }
				  }

			  }
			  if (item.ItemType == TrItem.trItemType.trSIDING || item.ItemType == TrItem.trItemType.trPLATFORM)
			  {
				  SidingItem s = item as SidingItem;

				  sidings.Add(new SidingWidget(item));

			  }
		  }

		  Inited = true;
	  }
	  bool Inited = false;
	  List<LineSegment> segments = new List<LineSegment>();
	  List<SwitchWidget> switches;
	  //List<PointF> buffers = new List<PointF>();
	  List<SignalWidget> signals = new List<SignalWidget>();
	  List<SidingWidget> sidings = new List<SidingWidget>();

	  PointF PlayerLocation = new PointF();

	   /// <summary>
      /// Initialises the picturebox and the image it contains. 
      /// </summary>
      public void InitImage()
      {
         pictureBox1.Width = IM_Width;
         pictureBox1.Height = IM_Height;

         if (pictureBox1.Image != null)
         {
            pictureBox1.Image.Dispose();
         }

         pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
		 imageList1 = new ImageList();
		 this.AvatarView.View = View.LargeIcon;
		 imageList1.ImageSize = new Size(64, 64);
		 this.AvatarView.LargeImageList = this.imageList1;

      }

	  Dictionary<string, Image> avatarList = null;
	  public void AddAvatar(string name, string url)
	  {
		  if (avatarList == null) avatarList = new Dictionary<string, Image>();

		  try
		  {
			  var request = WebRequest.Create(url);

			  using (var response = request.GetResponse())
			  using (var stream = response.GetResponseStream())
			  {
				  Image newImage = Image.FromStream(stream);//Image.FromFile("C:\\test1.png");//
				  avatarList[name] = newImage;

				  /*using (MemoryStream ms = new MemoryStream())
				  {
					  // Convert Image to byte[]
					  newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					  byte[] imageBytes = ms.ToArray();

					  // Convert byte[] to Base64 String
					  string base64String = Convert.ToBase64String(imageBytes);
				  }*/
			  }
		  }
		  catch
		  {
			  byte[] imageBytes = Convert.FromBase64String(imagestring);
			  MemoryStream ms = new MemoryStream(imageBytes, 0,
				imageBytes.Length);

			  // Convert byte[] to Image
			  ms.Write(imageBytes, 0, imageBytes.Length);
			  Image newImage = Image.FromStream(ms, true);
			  avatarList[name] = newImage;
		  }

		  /*
		  imageList1.Images.Clear();
		  AvatarView.Items.Clear();
		  var i = 0;
		  foreach (var pair in avatarList)
		  {
			  if (pair.Value == null) AvatarView.Items.Add(pair.Key);
			  else
			  {
				  AvatarView.Items.Add(pair.Key).ImageIndex = i;
				  imageList1.Images.Add(pair.Value);
				  i++;
			  }
		  }*/
	  }

	  public void CheckAvatar()
	  {
		  if (!MultiPlayer.MPManager.IsMultiPlayer() || MultiPlayer.MPManager.OnlineTrains == null || MultiPlayer.MPManager.OnlineTrains.Players == null) return;
		  var player = MultiPlayer.MPManager.OnlineTrains.Players;
		  if (avatarList == null) avatarList = new Dictionary<string, Image>();
		  if (avatarList.Count == player.Count) return;

		  foreach (var p in player) {
			  if (avatarList.ContainsKey(p.Key)) continue;
			  AddAvatar(p.Key, p.Value.url);
		  }

		  Dictionary<string, Image> tmplist = null;
		  foreach (var a in avatarList)
		  {
			  if (player.ContainsKey(a.Key)) continue;
			  if (tmplist == null) tmplist = new Dictionary<string, Image>();
			  tmplist.Add(a.Key, a.Value);
		  }

		  if (tmplist != null)
		  {
			  foreach (var t in tmplist) avatarList.Remove(t.Key);
		  }
		  imageList1.Images.Clear();
		  AvatarView.Items.Clear();
		  var i = 0;
		  foreach (var pair in avatarList)
		  {
			  if (pair.Value == null) AvatarView.Items.Add(pair.Key).ImageIndex = -1;
			  else
			  {
				  AvatarView.Items.Add(pair.Key).ImageIndex = i;
				  imageList1.Images.Add(pair.Value);
				  i++;
			  }
		  }
	  }
	  public bool firstShow = true;

      /// <summary>
      /// Regenerates the 2D view. At the moment, examines the track network
      /// each time the view is drawn. Later, the traversal and drawing can be separated.
      /// </summary>
      public void GenerateView()
      {


		  if (!Inited) return;

		  if (pictureBox1.Image == null) InitImage();

		  if (firstShow)
		  {
			  WorldPosition pos;
			  if (Program.Simulator.PlayerLocomotive != null)
			  {
				  pos = Program.Simulator.PlayerLocomotive.WorldPosition;
			  }
			  else
			  {
				  pos = Program.Simulator.Trains[0].Cars[0].WorldPosition;
			  }
			  var ploc = new PointF(pos.TileX * 2048 + pos.Location.X, pos.TileZ * 2048 + pos.Location.Z);
			  ViewWindow.X = ploc.X - minX - ViewWindow.Width / 2; ViewWindow.Y = ploc.Y - minY - ViewWindow.Width / 2;
			  firstShow = false;
		  }

		  if (MultiPlayer.MPManager.IsServer()) rmvButton.Visible = true;
		  else rmvButton.Visible = false;

		  //if (Program.Random.Next(100) == 0) AddAvatar("Test:"+Program.Random.Next(5), "http://trainsimchina.com/discuz/uc_server/avatar.php?uid=72965&size=middle");
		  try
		  {
			  CheckAvatar();
		  }
		  catch {  } //errors for avatar, just ignore
         using(Graphics g = Graphics.FromImage(pictureBox1.Image))
         using(Pen redPen = new Pen(Color.Red))
         using (Pen grayPen = new Pen(Color.Gray))
         {

            g.Clear(Color.White);

            // this is the total size of the entire viewable route (xRange == width, yRange == height in metres)
            float xRange = maxX - minX;
            float yRange = maxY - minY;

            float xScale = pictureBox1.Width / ViewWindow.Width;
            float yScale = pictureBox1.Height/ ViewWindow.Height;

			PointF[] points = new PointF[3];
			Pen p = grayPen;

			if (xScale > 3) p.Width = 3f;
			else if (xScale > 2) p.Width = 2f;
			else p.Width = 1f;
			PointF scaledA = new PointF(0, 0);
			PointF scaledB = new PointF(0, 0);
			PointF scaledC = new PointF(0, 0);

			foreach (var line in segments)
            {

				scaledA.X = (line.A.X - minX - ViewWindow.X) * xScale; scaledA.Y = pictureBox1.Height - (line.A.Y - minY - ViewWindow.Y) * yScale;
				scaledB.X = (line.B.X - minX - ViewWindow.X) * xScale; scaledB.Y = pictureBox1.Height - (line.B.Y - minY - ViewWindow.Y) * yScale;


				if ((scaledA.X < 0 && scaledB.X < 0) || (scaledA.X > IM_Width && scaledB.X > IM_Width) || (scaledA.Y > IM_Height && scaledB.Y > IM_Height) || (scaledA.Y < 0 && scaledB.Y < 0)) continue;


               //if (highlightTrackSections.Checked)
               //{
               //   if (line.Section != null && line.Section.InterlockingTrack == trackSections.SelectedValue)
               //   {
               //      p.Width = 5f;
               //   }
               //}

			   if (line.isCurved == true)
			   {
				   scaledC.X = (line.C.X - minX - ViewWindow.X) * xScale; scaledC.Y = pictureBox1.Height - (line.C.Y - minY - ViewWindow.Y) * yScale;
				   points[0] = scaledA; points[1] = scaledC; points[2] = scaledB;
				   g.DrawCurve(p, points);
			   }
               else g.DrawLine(p, scaledA, scaledB);
            }
			
			switchItemsDrawn.Clear();
			signalItemsDrawn.Clear();
			 float x, y;
			 PointF scaledItem = new PointF(0f, 0f);
			 for (var i = 0; i < switches.Count; i++)
			 {
				 SwitchWidget sw = switches[i];

				 x = (sw.Location.X - minX - ViewWindow.X) * xScale; y = pictureBox1.Height - (sw.Location.Y - minY - ViewWindow.Y) * yScale;

				 if (x < 0 || x > IM_Width || y > IM_Height || y < 0) continue;

				 scaledItem.X = x; scaledItem.Y = y;


				 g.FillEllipse(Brushes.Black, GetRect(scaledItem, 5f * p.Width));
				 sw.Location2D.X = scaledItem.X; sw.Location2D.Y = scaledItem.Y;
				 switchItemsDrawn.Add(sw);
			 }

			 foreach (var s in signals)
			 {
				 x = (s.Location.X - minX - ViewWindow.X) * xScale; y = pictureBox1.Height - (s.Location.Y - minY - ViewWindow.Y) * yScale;
				 if (x < 0 || x > IM_Width || y > IM_Height || y < 0) continue;
				 scaledItem.X = x; scaledItem.Y = y;
				 s.Location2D.X = scaledItem.X; s.Location2D.Y = scaledItem.Y;

				 if (s.IsProceed == 0)
				 {
					 g.FillEllipse(Brushes.Green, GetRect(scaledItem, 5f * p.Width));
				 }
				 else if (s.IsProceed == 1)
				 {
					 g.FillEllipse(Brushes.Orange, GetRect(scaledItem, 5f * p.Width));
				 }
				 else
				 {
					 g.FillEllipse(Brushes.Red, GetRect(scaledItem, 5f * p.Width));
				 }
				 signalItemsDrawn.Add(s);
			 }

            if (true/*showPlayerTrain.Checked*/)
            {

				CleanVerticalCells();//clean the drawing area for text of sidings
				foreach (var s in sidings)
				{
					scaledItem.X = (s.Location.X - minX - ViewWindow.X) * xScale;
					scaledItem.Y = DetermineSidingLocation(scaledItem.X, pictureBox1.Height - (s.Location.Y - minY - ViewWindow.Y) * yScale, s.Name);
					if (scaledItem.Y >= 0f) //if we need to draw the siding names
					{

						g.DrawString(s.Name, sidingFont, sidingBrush, scaledItem);
					}
				}
				foreach (Train t in simulator.Trains)
				{
					name = "";
					if (t.LeadLocomotive != null)
					{
						worldPos = t.LeadLocomotive.WorldPosition;
						name = t.LeadLocomotive.CarID;
					}
					else if (t.Cars != null && t.Cars.Count > 0)
					{
						worldPos = t.Cars[0].WorldPosition;
						name = t.Cars[0].CarID;

					}
					else continue;

					PlayerLocation = new PointF(
					   worldPos.TileX * 2048 + worldPos.Location.X,
					   worldPos.TileZ * 2048 + worldPos.Location.Z);

					x = (PlayerLocation.X - minX - ViewWindow.X) * xScale; y = pictureBox1.Height - (PlayerLocation.Y - minY - ViewWindow.Y) * yScale;
					if (x < 0 || x > IM_Width || y > IM_Height || y < 0) continue;

					scaledItem.X = x; scaledItem.Y = y;

					g.FillRectangle(Brushes.DarkGreen, GetRect(scaledItem, 15f));
					scaledItem.Y -= 25;
					g.DrawString(GetTrainName(name), trainFont, trainBrush, scaledItem);
				}
				if (switchPickedItemHandled) switchPickedItem = null;
				if (signalPickedItemHandled) signalPickedItem = null;

				if (switchPickedItem != null /*&& switchPickedItemChanged == true*/ && !switchPickedItemHandled && simulator.GameTime - switchPickedTime < 5)
				{
					switchPickedLocation.X = switchPickedItem.Location2D.X + 150; switchPickedLocation.Y = switchPickedItem.Location2D.Y;
					g.FillRectangle(Brushes.LightGray, GetRect(switchPickedLocation, 400f, 64f));

					switchPickedLocation.X -= 180; switchPickedLocation.Y += 10;
					var node = switchPickedItem.Item.TrJunctionNode;
					if (node.SelectedRoute == 0) g.DrawString("Current Route: Main Route", trainFont, trainBrush, switchPickedLocation);
					else g.DrawString("Current Route: Side Route", trainFont, trainBrush, switchPickedLocation);
					if (!MultiPlayer.MPManager.IsMultiPlayer() || MultiPlayer.MPManager.IsServer())
					{
						switchPickedLocation.Y -= 22;
						g.DrawString(InputSettings.Commands[(int)UserCommands.GameSwitchPicked] + " to throw the switch", trainFont, trainBrush, switchPickedLocation);
						switchPickedLocation.Y += 8;
					}
					switchPickedLocation.Y -= 30;
					g.DrawString(InputSettings.Commands[(int)UserCommands.CameraJumpSeeSwitch] + " to see the switch", trainFont, trainBrush, switchPickedLocation);
				}
				if (signalPickedItem != null /*&& signalPickedItemChanged == true*/ && !signalPickedItemHandled && simulator.GameTime - signalPickedTime < 5)
				{
					signalPickedLocation.X = signalPickedItem.Location2D.X + 150; signalPickedLocation.Y = signalPickedItem.Location2D.Y;
					g.FillRectangle(Brushes.LightGray, GetRect(signalPickedLocation, 400f, 64f));
					signalPickedLocation.X -= 180; signalPickedLocation.Y -= 2;
					if (signalPickedItem.IsProceed == 0) g.DrawString("Current Signal: Proceed", trainFont, trainBrush, signalPickedLocation);
					else if (signalPickedItem.IsProceed == 1) g.DrawString("Current Signal: Approach", trainFont, trainBrush, signalPickedLocation);
					else g.DrawString("Current Signal: Stop", trainFont, trainBrush, signalPickedLocation);
					if (!MultiPlayer.MPManager.IsMultiPlayer() || MultiPlayer.MPManager.IsServer())
					{
						signalPickedLocation.Y -= 24;
						g.DrawString(InputSettings.Commands[(int)UserCommands.GameSignalPicked] + " to change signal", trainFont, trainBrush, signalPickedLocation);
					}
				}
			}

         }

         pictureBox1.Invalidate();
      }

	  private Vector2[][] alignedTextY;
	  private int[] alignedTextNum;
	  private int spacing = 12;
	  private void CleanVerticalCells()
	  {
		  if (alignedTextY == null || alignedTextY.Length != IM_Height / spacing) //first time to put text, or the text height has changed
		  {
			  alignedTextY = new Vector2[IM_Height/spacing][];
			  alignedTextNum = new int[IM_Height/spacing];
			  for (var i = 0; i < IM_Height / spacing; i++) alignedTextY[i] = new Vector2[4]; //each line has at most 4 sidings
		  }
		  for (var i = 0; i < IM_Height / spacing; i++) { alignedTextNum[i] = 0; }

	  }
	  private float DetermineSidingLocation(float startX, float wantY, string name)
	  {
		  //out of drawing area
		  if (startX < -64 || startX > IM_Width || wantY < -spacing || wantY > IM_Height) return -1f;

		  int position = (int)(wantY / spacing);//the cell of the text it wants in
		  if (position > alignedTextY.Length) return wantY;//position is larger than the number of cells
		  var endX = startX + name.Length*trainFont.Size;
		  int desiredPosition = position;
		  while (position < alignedTextY.Length && position >= 0)
		  {
			  //if the line contains no text yet, put it there
			  if (alignedTextNum[position] == 0)
			  {
				  alignedTextY[position][alignedTextNum[position]].X = startX;
				  alignedTextY[position][alignedTextNum[position]].Y = endX;//add info for the text (i.e. start and end location)
				  alignedTextNum[position]++;
				  return position * spacing;
			  }

			  bool conflict = false;
			  //check if it is intersect any one in the cell
			  foreach (Vector2 v in alignedTextY[position])
			  {
				  //check conflict with a text, v.x is the start of the text, v.y is the end of the text
				  if ((startX > v.X && startX < v.Y) || (endX > v.X && endX < v.Y) || (v.X > startX && v.X < endX) || (v.Y > startX && v.Y < endX))
				  {
					  conflict = true;
					  break;
				  }
			  }
			  if (conflict == false) //no conflict
			  {
				  if (alignedTextNum[position] >= alignedTextY[position].Length) return -1f;
				  alignedTextY[position][alignedTextNum[position]].X = startX;
				  alignedTextY[position][alignedTextNum[position]].Y = endX;//add info for the text (i.e. start and end location)
				  alignedTextNum[position]++;
				  return position * spacing;
			  }
			  position--;
			  //cannot move up, then try to move it down
			  if (position - desiredPosition < -1)
			  {
				  position = desiredPosition + 2;
			  }
			  //could not find any position up or down, just return negative
			  if (position == desiredPosition) return -1f;
		  }
		  return position * spacing;
	  }
	  private string GetTrainName(string ID)
	  {
		  int location = ID.LastIndexOf('-');
		  if (location < 0) return ID;
		  return ID.Substring(0, location - 1);
	  }
      /// <summary>
      /// Generates a rectangle representing a dot being drawn.
      /// </summary>
      /// <param name="p">Center point of the dot, in pixels.</param>
      /// <param name="size">Size of the dot's diameter, in pixels</param>
      /// <returns></returns>
      private RectangleF GetRect(PointF p, float size)
      {
         return new RectangleF(p.X - size / 2f, p.Y - size / 2f, size, size);
      }

	  /// <summary>
	  /// Generates a rectangle representing a rectangle being drawn.
	  /// </summary>
	  /// <param name="p">Center point of the rec, in pixels.</param>
	  /// <param name="sizeX">Size of the rec's X, in pixels</param>
	  /// <param name="sizeY">Size of the rec's Y, in pixels</param>
	  /// <returns></returns>
	  private RectangleF GetRect(PointF p, float sizeX, float sizeY)
	  {
		  return new RectangleF(p.X - sizeX / 2f, p.Y - sizeY / 2f, sizeX, sizeY);
	  }
	  /// <summary>
      /// Generates line segments from an array of TrVectorSection. Also computes 
      /// the bounds of the entire route being drawn.
      /// </summary>
      /// <param name="segments"></param>
      /// <param name="items"></param>
      /// <param name="minX"></param>
      /// <param name="minY"></param>
      /// <param name="maxX"></param>
      /// <param name="maxY"></param>
      /// <param name="simulator"></param>
      private static void AddSegments(List<LineSegment> segments, TrackNode node, TrVectorSection[] items,  ref float minX, ref float minY, ref float maxX, ref float maxY, Simulator simulator)
      {

         bool occupied = false;


         //if (simulator.InterlockingSystem.Tracks.ContainsKey(node))
         //{
         //   occupied = node.InterlockingTrack.IsOccupied;
         //}

         for (int i = 0; i < items.Length - 1; i++)
         {
			 PointF A = new PointF(items[i].TileX * 2048 + items[i].X, items[i].TileZ * 2048 + items[i].Z);
            PointF B = new PointF(items[i + 1].TileX * 2048 + items[i + 1].X, items[i + 1].TileZ * 2048 + items[i+1].Z);

            CalcBounds(ref maxX, A.X, true);
            CalcBounds(ref maxY, A.Y, true);
            CalcBounds(ref maxX, B.X, true);
            CalcBounds(ref maxY, B.Y, true);

            CalcBounds(ref minX, A.X, false);
            CalcBounds(ref minY, A.Y, false);
            CalcBounds(ref minX, B.X, false);
            CalcBounds(ref minY, B.Y, false);

            segments.Add(new LineSegment(A, B, occupied, items[i]));
         }
      }

      /// <summary>
      /// Given a value representing a limit, evaluate if the given value exceeds the current limit.
      /// If so, expand the limit.
      /// </summary>
      /// <param name="limit">The current limit.</param>
      /// <param name="value">The value to compare the limit to.</param>
      /// <param name="gt">True when comparison is greater-than. False if less-than.</param>
      private static void CalcBounds(ref float limit, float value, bool gt)
      {
         if (gt)
         {
            if (value > limit)
            {
               limit = value;
            }
         }
         else
         {
            if (value < limit)
            {
               limit = value;
            }
         }
      }


      private float ScrollSpeedX
      {
         get
         {
            return ViewWindow.Width * 0.10f;
         }
      }

      private float ScrollSpeedY
      {
         get
         {
            return ViewWindow.Width * 0.10f;
         }
      }

      private void refreshButton_Click(object sender, EventArgs e)
      {
		  firstShow = true;
         GenerateView();
      }

      private void leftButton_Click(object sender, EventArgs e)
      {
         ShiftViewLeft();
      }

      private void rightButton_Click(object sender, EventArgs e)
      {
         ShiftViewRight();
      }

      private void upButton_Click(object sender, EventArgs e)
      {
         ShiftViewUp();
      }

      private void downButton_Click(object sender, EventArgs e)
      {
         ShiftViewDown();
      }

      private void ShiftViewUp()
      {
         ViewWindow.Offset(0, -ScrollSpeedY);

         GenerateView();
      }
      private void ShiftViewDown()
      {
         ViewWindow.Offset(0, ScrollSpeedY);

         GenerateView();
      }

      private void ShiftViewRight()
      {
         ViewWindow.Offset(ScrollSpeedX, 0);

         GenerateView();
      }


      private void ShiftViewLeft()
      {
         ViewWindow.Offset(-ScrollSpeedX, 0);

         GenerateView();
      }

	  private void rmvButton_Click(object sender, EventArgs e)
	  {
		  if (!MultiPlayer.MPManager.IsServer()) return;
		  var chosen = AvatarView.SelectedItems;
		  if (chosen.Count > 0)
		  {
			  for (var i =0; i < chosen.Count; i++)
			  {
				  var tmp = chosen[i];
				  MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGMessage(tmp.Text, "Error", "Sorry the server has removed you")).ToString());
			  }
		  }

	  }



      private void windowSizeUpDown_ValueChanged(object sender, EventArgs e)
      {
         // this is the center, before increasing the size
         PointF center = new PointF(ViewWindow.X + ViewWindow.Width / 2f, ViewWindow.Y + ViewWindow.Height / 2f);


         float newSize = (float)windowSizeUpDown.Value;

         ViewWindow = new RectangleF(center.X - newSize / 2f, center.Y - newSize / 2f, newSize, newSize);


         GenerateView();

      }


	  protected override void OnMouseWheel(MouseEventArgs e)
	  {
		  decimal tempValue = windowSizeUpDown.Value;
		  if (e.Delta < 0) tempValue /= 0.95m;
		  else if (e.Delta > 0) tempValue *= 0.95m;
		  else return;

		  if (tempValue < windowSizeUpDown.Minimum) tempValue = windowSizeUpDown.Minimum;
		  if (tempValue > windowSizeUpDown.Maximum) tempValue = windowSizeUpDown.Maximum;
		  windowSizeUpDown.Value = tempValue;
	  }

	  private bool Zooming = false;
	  private bool LeftClick = false;
	  private bool RightClick = false;

	  private void pictureBoxMouseDown(object sender, MouseEventArgs e)
	  {
		  if (e.Button == MouseButtons.Left) LeftClick = true;
		  if (e.Button == MouseButtons.Right) RightClick = true;

		  if (LeftClick == true && RightClick == false)
		  {
			  if (Dragging == false) Dragging = true;
		  }
		  else if (LeftClick == true && RightClick == true)
		  {
			  if (Zooming == false) Zooming = true;
		  }
		  LastCursorPosition.X = e.X;
		  LastCursorPosition.Y = e.Y;
		  //MSG.Enabled = false;
		  MultiPlayer.MPManager.Instance().ComposingText = false;
	  }

	  private void pictureBoxMouseUp(object sender, MouseEventArgs e)
	  {
		  if (e.Button == MouseButtons.Left) LeftClick = false;
		  if (e.Button == MouseButtons.Right) RightClick = false; 

		  if (LeftClick == false)
		  {
			  Dragging = false;
			  Zooming = false;
		  }

		  if (LeftClick == false)
		  {
			  if (LastCursorPosition.X == e.X && LastCursorPosition.Y == e.Y)
			  {
				  var temp = findItemFromMouse(e.X, e.Y, 5);
				  if (temp != null)
				  {
					  if (temp is SwitchWidget) switchPickedItem = (SwitchWidget)temp; //read by MPManager
					  if (temp is SignalWidget) signalPickedItem = (SignalWidget)temp;
#if false
					  pictureBox1.ContextMenu.Show(pictureBox1, e.Location);
					  pictureBox1.ContextMenu.MenuItems[0].Checked = pictureBox1.ContextMenu.MenuItems[1].Checked = false;
					  if (pickedItem.Item.TrJunctionNode != null)
					  {
						  if (pickedItem.Item.TrJunctionNode.SelectedRoute == 0) pictureBox1.ContextMenu.MenuItems[0].Checked = true;
						  else pictureBox1.ContextMenu.MenuItems[1].Checked = true;
					  }
#endif
				  }
				  else { switchPickedItem = null; signalPickedItem = null; }
			  }

		  }

	  }
#if DEBUG
	  void switchMainClick(object sender, EventArgs e)
	  {
		  if (switchPickedItem != null && switchPickedItem.Item.TrJunctionNode != null)
		  {
			  TrJunctionNode nextSwitchTrack = Program.DebugViewer.switchPickedItem.Item.TrJunctionNode;
			  if (nextSwitchTrack != null && !Program.Simulator.SwitchIsOccupied(nextSwitchTrack))
			  {
				  if (nextSwitchTrack.SelectedRoute == 0)
					  nextSwitchTrack.SelectedRoute = 1;
				  else
					  nextSwitchTrack.SelectedRoute = 0;
			  }
		  }

	  }

	   	  void switchSideClick(object sender, EventArgs e)
	  {
		  if (switchPickedItem != null && switchPickedItem.Item.TrJunctionNode != null)
		  {
			  TrJunctionNode nextSwitchTrack = Program.DebugViewer.switchPickedItem.Item.TrJunctionNode;
			  if (nextSwitchTrack != null && !Program.Simulator.SwitchIsOccupied(nextSwitchTrack))
			  {
				  if (nextSwitchTrack.SelectedRoute == 0)
					  nextSwitchTrack.SelectedRoute = 1;
				  else
					  nextSwitchTrack.SelectedRoute = 0;
			  }
		  }

	  }
#endif

	  private ItemWidget findItemFromMouse(int x, int y, int range)
	  {
		  foreach (var item in switchItemsDrawn)
		  {
			  //if out of range, continue
			  if (item.Location2D.X < x - range || item.Location2D.X > x + range
				 || item.Location2D.Y < y - range || item.Location2D.Y > y + range) continue;

			  if (true/*item != switchPickedItem*/) { switchPickedItemChanged = true; switchPickedItemHandled = false; switchPickedTime = simulator.GameTime; }
			  return item;
		  }
		  foreach (var item in signalItemsDrawn)
		  {
			  //if out of range, continue
			  if (item.Location2D.X < x - range || item.Location2D.X > x + range
				 || item.Location2D.Y < y - range || item.Location2D.Y > y + range) continue;

			  if (true/*item != signalPickedItem*/) { signalPickedItemChanged = true; signalPickedItemHandled = false; signalPickedTime = simulator.GameTime; }
			  return item;
		  }

		  return null;
	  }

	  private void pictureBoxMouseMove(object sender, MouseEventArgs e)
	  {
		  if (Dragging&&!Zooming)
		  {
			  int diffX = LastCursorPosition.X - e.X;
			  int diffY = LastCursorPosition.Y - e.Y;

			  ViewWindow.Offset(diffX * ScrollSpeedX/10, -diffY * ScrollSpeedX/10);
			  GenerateView();
		  }
		  else if (Zooming)
		  {
			  decimal tempValue = windowSizeUpDown.Value;
			  if (LastCursorPosition.Y - e.Y < 0) tempValue /= 0.95m;
			  else if (LastCursorPosition.Y - e.Y > 0) tempValue *= 0.95m;

			  if (tempValue < windowSizeUpDown.Minimum) tempValue = windowSizeUpDown.Minimum;
			  if (tempValue > windowSizeUpDown.Maximum) tempValue = windowSizeUpDown.Maximum;
			  windowSizeUpDown.Value = tempValue;
			  GenerateView();

		  }
		  LastCursorPosition.X = e.X;
		  LastCursorPosition.Y = e.Y;
	  }

	  public bool addNewMessage(double time, string msg)
	  {
		  var count = 0;
		  while (count < 3)
		  {
			  try
			  {
				  if (messages.Items.Count > 10)
				  {
					  messages.Items.RemoveAt(0);
				  }
				  messages.Items.Add(msg);
				  break;
			  }
			  catch { count++; }
		  }
		  return true;
	  }

      private void leftButton_MouseDown(object sender, MouseEventArgs e)
      {
         LeftButtonDown = true;
      }

      private void leftButton_MouseLeave(object sender, EventArgs e)
      {
         LeftButtonDown = false;
      }

      private void leftButton_MouseUp(object sender, MouseEventArgs e)
      {
         LeftButtonDown = false;
      }

      private void rightButton_MouseUp(object sender, MouseEventArgs e)
      {
         RightButtonDown = false;
      }

      private void rightButton_MouseDown(object sender, MouseEventArgs e)
      {
         RightButtonDown = true;
      }

      private void rightButton_MouseLeave(object sender, EventArgs e)
      {
         RightButtonDown = false;
      }

      private void upButton_MouseUp(object sender, MouseEventArgs e)
      {
         UpButtonDown = false;
      }

      private void upButton_MouseDown(object sender, MouseEventArgs e)
      {
         UpButtonDown = true;
      }

      private void upButton_MouseLeave(object sender, EventArgs e)
      {
         UpButtonDown = false;
      }

      private void downButton_MouseUp(object sender, MouseEventArgs e)
      {
         DownButtonDown = false;
      }

      private void downButton_MouseDown(object sender, MouseEventArgs e)
      {
         DownButtonDown = true;
      }

      private void downButton_MouseLeave(object sender, EventArgs e)
      {
         DownButtonDown = false;
      }

      private void showSwitches_CheckedChanged(object sender, EventArgs e)
      {
         GenerateView();
      }

      private void showBuffers_CheckedChanged(object sender, EventArgs e)
      {
         GenerateView();
      }

      private void showSignals_CheckedChanged(object sender, EventArgs e)
      {
         GenerateView();
      }

	  private void chkAllowUserSwitch_CheckedChanged(object sender, EventArgs e)
	  {
		  MultiPlayer.MPManager.Instance().ClientAllowedSwitch = chkAllowUserSwitch.Checked;
	  }

	  private void chkShowAvatars_CheckedChanged(object sender, EventArgs e)
	  {
		  Program.Simulator.Settings.ShowAvatar = chkShowAvatars.Checked;
	  }
	  
	   private void highlightTrackShapes_CheckedChanged(object sender, EventArgs e)
      {
         GenerateView();
      }

      private void trackShapes_SelectedIndexChanged(object sender, EventArgs e)
      {
         GenerateView();
      }


	  private const int CP_NOCLOSE_BUTTON = 0x200;
	  protected override CreateParams CreateParams
	  {
		  get
		  {
			  CreateParams myCp = base.CreateParams;
			  myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
			  return myCp;
		  }
	  }
	  string imagestring = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAACpJREFUOE9jYBjs4D/QgSBMNhg1ABKAFAUi2aFPNY0Ue4FiA6jmlUFsEABfyg/x8/L8/gAAAABJRU5ErkJggg==";

	  private void composeMSG_Click(object sender, EventArgs e)
	  {
		  MSG.Enabled = true;
		  MultiPlayer.MPManager.Instance().ComposingText = true;
	  }

	  private void msgAll_Click(object sender, EventArgs e)
	  {
		  if (!MultiPlayer.MPManager.IsMultiPlayer()) return;
		  var msg = MSG.Text;
		  msg = msg.Replace("\r", "");
		  msg = msg.Replace("\t", "");
		  if (msg != "")
		  {
			  try
			  {
				  var user = "";
				  foreach (var p in MultiPlayer.MPManager.OnlineTrains.Players)
				  {
					  user += p.Key + "\r";
				  }
				  user += "0END";
				  MultiPlayer.MPManager.Notify((new MultiPlayer.MSGText(MultiPlayer.MPManager.GetUserName(), user, msg)).ToString());
				  MSG.Text = "";
				  MSG.Enabled = false;
				  MultiPlayer.MPManager.Instance().ComposingText = false;

			  }
			  catch { }
		  }
	  }

	  private void msgSelected_Click(object sender, EventArgs e)
	  {
		  if (!MultiPlayer.MPManager.IsMultiPlayer()) return;
		  var chosen = AvatarView.SelectedItems;
		  var msg = MSG.Text;
		  msg = msg.Replace("\r", "");
		  msg = msg.Replace("\t", "");
		  if (msg == "") return;
		  if (chosen.Count > 0)
		  {
			  var user = "";

			  for (var i = 0; i < chosen.Count; i++)
			  {
				  user += chosen[i].Text +"\r";
			  }
			  user += "0END";

			  MultiPlayer.MPManager.Notify((new MultiPlayer.MSGText(MultiPlayer.MPManager.GetUserName(), user, msg)).ToString());
			  MSG.Text = "";
			  MSG.Enabled = false;
			  MultiPlayer.MPManager.Instance().ComposingText = false;

		  }

	  }

	  private void MSGLeave(object sender, EventArgs e)
	  {
		  //MultiPlayer.MPManager.Instance().ComposingText = false;
	  }

	  private void MSGEnter(object sender, EventArgs e)
	  {
		  //MultiPlayer.MPManager.Instance().ComposingText = true;
	  }

	  private void DispatcherLeave(object sender, EventArgs e)
	  {
		  //MultiPlayer.MPManager.Instance().ComposingText = false;
	  }

   }

   /// <summary>
   /// Defines a signal being drawn in a 2D view.
   /// </summary>
   public class SignalWidget : ItemWidget
   {
	   public TrItem Item;
	   /// <summary>
	   /// The underlying signal object as referenced by the TrItem.
	   /// </summary>
	   public SignalObject Signal;


	   /// <summary>
	   /// For now, returns true if any of the signal heads shows any "clear" aspect.
	   /// This obviously needs some refinement.
	   /// </summary>
	   public int IsProceed
	   {
		   get
		   {
			   int returnValue = 2;

			   foreach (var head in Signal.SignalHeads)
			   {
				   if (head.state == SignalHead.SIGASP.CLEAR_1 ||
					   head.state == SignalHead.SIGASP.CLEAR_2)
				   {
					   returnValue = 0;
				   }
				   if (head.state == SignalHead.SIGASP.APPROACH_1 ||
					   head.state == SignalHead.SIGASP.APPROACH_2 || head.state == SignalHead.SIGASP.APPROACH_3)
				   {
					   returnValue = 1;
				   }
			   }

			   return returnValue;
		   }
	   }

	   /// <summary>
	   /// 
	   /// </summary>
	   /// <param name="item"></param>
	   /// <param name="signal"></param>
	   public SignalWidget(TrItem item, SignalObject signal)
	   {
		   Item = item;
		   Signal = signal;

		   Location = new PointF(item.TileX * 2048 + item.X, item.TileZ * 2048 + item.Z);
		   Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
	   }
   }

   /// <summary>
   /// Defines a signal being drawn in a 2D view.
   /// </summary>
   public class SwitchWidget : ItemWidget
   {
	   public TrackNode Item;

	   /// <summary>
	   /// 
	   /// </summary>
	   /// <param name="item"></param>
	   /// <param name="signal"></param>
	   public SwitchWidget(TrackNode item)
	   {
		   Item = item;

		   Location = new PointF(Item.UiD.TileX * 2048 + Item.UiD.X, Item.UiD.TileZ * 2048 + Item.UiD.Z);
		   Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
	   }
   }

   public class BufferWidget : ItemWidget
   {
	   public TrackNode Item;

	   /// <summary>
	   /// 
	   /// </summary>
	   /// <param name="item"></param>
	   /// <param name="signal"></param>
	   public BufferWidget(TrackNode item)
	   {
		   Item = item;

		   Location = new PointF(Item.UiD.TileX * 2048 + Item.UiD.X, Item.UiD.TileZ * 2048 + Item.UiD.Z);
		   Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
	   }
   }

   public class ItemWidget
   {
	   public PointF Location;
	   public PointF Location2D;

	   /// <summary>
	   /// 
	   /// </summary>
	   /// <param name="item"></param>
	   public ItemWidget()
	   {

		   Location = new PointF(float.NegativeInfinity, float.NegativeInfinity);
		   Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
	   }

   }

   /// <summary>
   /// Defines a geometric line segment.
   /// </summary>
   public class LineSegment
   {
	   public PointF A;
	   public PointF B;
	   public PointF C;
	   //public float radius = 0.0f;
	   public bool isCurved = false;

	   public float angle1, angle2;
	   //public SectionCurve curve = null;

	   public LineSegment(PointF A, PointF B, bool Occupied, TrVectorSection Section)
	   {
		   this.A = A;
		   this.B = B;

		   isCurved = false; 
		   if (Section == null) return;

		   
		   uint k = Section.SectionIndex;
		   TrackSection ts = Program.Simulator.TSectionDat.TrackSections.Get(k);
		   if (ts != null)
		   {
			   if (ts.SectionCurve != null)
			   {
				   float diff = (float) (ts.SectionCurve.Radius * (1 - Math.Cos(ts.SectionCurve.Angle * 3.14f / 360)));
				   if (diff < 3) return; //not need to worry, curve too small
				   //curve = ts.SectionCurve;
				   Vector3 v = new Vector3(B.X - A.X, 0, B.Y - A.Y);
				   isCurved = true;
				   Vector3 v2 = Vector3.Cross(Vector3.Up, v); v2.Normalize();
				   v = v / 2; v.X += A.X; v.Z += A.Y;
				   if (ts.SectionCurve.Angle > 0)
				   {
					   v = v2*-diff + v;
				   }
				   else v = v2*diff + v;
				   C = new PointF(v.X, v.Z);
			   }
		   }

	   }
   }

   /// <summary>
   /// Defines a siding name being drawn in a 2D view.
   /// </summary>
   public struct SidingWidget
   {
	   public PointF Location;
	   public string Name;
	   /// <summary>
	   /// The underlying track item.
	   /// </summary>
	   private TrItem Item;

	   /// <summary>
	   /// 
	   /// </summary>
	   /// <param name="item"></param>
	   /// <param name="signal"></param>
	   public SidingWidget(TrItem item)
	   {
		   Item = item;

		   Name = item.ItemName;

		   Location = new PointF(item.TileX * 2048 + item.X, item.TileZ * 2048 + item.Z);
	   }
   }
}
