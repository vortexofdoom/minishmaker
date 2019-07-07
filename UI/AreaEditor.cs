using MinishMaker.Core;
using MinishMaker.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinishMaker.UI
{
	public partial class AreaEditor : Form
	{
		private int currentArea=-1;
		private int selectedRoomRect = -1;
		private int biggestX = 0;
		private int biggestY = 0;
		private byte unknown1 = 0;
		private byte unknown2 = 0;
		private byte flagOffset = 0;
		private bool loading = false;

		private Dictionary<int,Rectangle> roomRects = new Dictionary<int,Rectangle>();

		public AreaEditor()
		{
			InitializeComponent();
			mapX.KeyDown+=EnterUnfocus;
			mapY.KeyDown+=EnterUnfocus;
			areaSongId.KeyDown+=EnterUnfocus;
		}

		public void LoadArea(int area)
		{
			loading = true;
			areaLabel.Text= "Area: "+area.Hex();
			mapX.Text = "FFF";
			mapY.Text = "FFF";
			roomLabel.Text = "Selected Room: -";
			mapX.Enabled=false;
			mapY.Enabled=false;

			canFlute.Enabled = true;
			dungeonMap.Enabled = true;
			moleCave.Enabled = true;
			redName.Enabled = true;
			keysShown.Enabled = true;

			areaSongId.Enabled=true;

			roomRects.Clear();
			MainWindow main = (MainWindow)Application.OpenForms[0];
			currentArea = area;

			var manager = MapManager.Instance;
			var roomlist = manager.MapAreas.Where(a=>a.Index==area).Single();

			var reader = ROM.Instance.reader;
			var dataloc = ROM.Instance.headers.areaInformationTableLoc + area * 4;
			reader.SetPosition(dataloc);
			var data = reader.ReadBytes(4);
			
			Console.WriteLine( dataloc.Hex());
			Console.WriteLine( data[0] );

			canFlute.Checked=(data[0]%2==1);//bit 1
		
			data[0]= (byte)(data[0]>>1);
			keysShown.Checked=(data[0]%2==1);//bit 2

			data[0]= (byte)(data[0]>>1);
			redName.Checked=(data[0]%2==1);//bit 4

			data[0]= (byte)(data[0]>>1);
			dungeonMap.Checked=(data[0]%2==1);//bit 8

			data[0]= (byte)(data[0]>>1);
			unknown1 = (byte)(data[0]%2);//bit 10 //currently unknown use

			data[0]= (byte)(data[0]>>1);
			moleCave.Checked=(data[0]%2==1);//bit 20

			data[0]= (byte)(data[0]>>1);
			unknown2 = (byte)(data[0]%2);//bit 40 //unknown

			data[0]= (byte)(data[0]>>1);
			canFlute.Checked=(data[0]%2==1 || canFlute.Checked);//bit 80 //unused in eur, seems to be same as bit 1?

			areaNameId.Text = data[1].Hex();

			flagOffset = data[2];

			areaSongId.Text = data[3].Hex();

			biggestX = 0;
			biggestY = 0;

			foreach(var room in roomlist.Rooms)
			{
				var rect = room.GetMapRect(area);
				roomRects.Add(room.Index,rect);

				if(rect.Bottom>biggestY)
				{
					biggestY = rect.Bottom;
				}

				if(rect.Right>biggestX)
				{
					biggestX= rect.Right;
				}
			}
			DrawRects();
			//modify text


			loading = false;
		}

		public void DrawRects()
		{
			var bitmap = new Bitmap(biggestX,biggestY);

			using( var gr = Graphics.FromImage(bitmap) )
			{
				var i = 0;
				foreach(var rect in roomRects.Values)
				{
					var g = i%8 *32 +31;
					var r = i%32 /8 *32;
					var b = i/32 *32;

					var color = Color.FromArgb(r,g,b);
					var brush = new SolidBrush(color);

					gr.FillRectangle(brush, rect);
					i+=1;
				}
			}

			areaLayout.Image = bitmap;
		}

		private void pictureBox1_Click( object sender, MouseEventArgs e )
		{
			if(currentArea==-1) //nothing loaded
				return;

			mapX.Enabled=true;
			mapY.Enabled=true;

			Rectangle clickedRect = Rectangle.Empty;
			int index = 0;
			foreach(var rect in roomRects.Values)
			{
				if(rect.Contains(e.X,e.Y))
				{
					clickedRect = rect;
					break;
				}
				index+=1;
			}

			if(clickedRect == Rectangle.Empty)
				return;

			selectedRoomRect = roomRects.ElementAt(index).Key;

			roomLabel.Text = "Selected Room: " +selectedRoomRect.Hex();

			mapX.Text = clickedRect.Left.Hex();
			mapY.Text = clickedRect.Top.Hex();
		}

		private void EnterUnfocus(object sender, KeyEventArgs e)
		{
			if(e.KeyCode==Keys.Enter)
			{
				HiddenLabel.Focus();
			}
		}

		private void mapBox_LostFocus( object sender, EventArgs e )
		{
			var x = Convert.ToInt16(mapX.Text,16);
			var y = Convert.ToInt16(mapY.Text,16);
			var roomRect = roomRects[selectedRoomRect];

			if(x!=roomRect.Left || y!= roomRect.Top)
			{
				roomRect.X = x;
				roomRect.Y = y;
				if(biggestX<roomRect.Right)
				{
					biggestX = roomRect.Right;
				}

				if(biggestY<roomRect.Bottom)
				{
					biggestY = roomRect.Bottom;
				}

				roomRects[selectedRoomRect]=roomRect;
				DrawRects();

				MainWindow main = (MainWindow)Application.OpenForms[0];
				main.AddPendingChange(DataType.roomLocation);
			}
		}

		private void AreaChanged(object sender, EventArgs e)
		{
			if(loading)
				return;

			var byte1Data = 
				(canFlute.Checked	?0:1) +
				(keysShown.Checked	?0:2) +
				(redName.Checked	?0:4) +
				(dungeonMap.Checked ?0:8) +
				(unknown1			  *10) +
				(moleCave.Checked	?0:20) +
				(unknown2			  *40) +
				(canFlute.Checked	?0:80);

			var byte2Data = Convert.ToByte(areaNameId.Text,16);

			//reserved for spoopy scary byte3 flag offset

			var byte4Data = Convert.ToByte(areaSongId.Text,16);
			var rom = ROM.Instance;
			var dataloc = ROM.Instance.headers.areaInformationTableLoc + currentArea * 4;
			var data = new byte[4]{ (byte)byte1Data,byte2Data,flagOffset,byte4Data};

			rom.WriteData(0,dataloc,data,DataType.areaData);

			MainWindow main = (MainWindow)Application.OpenForms[0];
			main.AddPendingChange(DataType.areaData);
		}
	}
}
