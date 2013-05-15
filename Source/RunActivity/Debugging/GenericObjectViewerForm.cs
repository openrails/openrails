// COPYRIGHT 2010 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ORTS.Debugging
{
   public partial class GenericObjectViewerForm : Form
   {

      object[] objects;

      Timer timer;

      public GenericObjectViewerForm(string title, object[] objects)
      {
         InitializeComponent();

         this.Text = title;

         this.objects = objects;

         comboBox1.Items.AddRange(objects);


         timer = new Timer();
         timer.Interval = 600;
         timer.Tick += timer_Tick;
         timer.Start();
      }

      void timer_Tick(object sender, EventArgs e)
      {
         RefreshView();
      }


      /// <summary>
      /// Get the propertyGrid to refresh, if any of the values
      /// gas changed.
      /// </summary>
      private void RefreshView()
      {
         object temp = propertyGrid.SelectedObject;
         propertyGrid.SelectedObject = null;
         propertyGrid.SelectedObject = temp;
      }

      private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
      {
         propertyGrid.SelectedObject = comboBox1.SelectedItem;
      }

      private void GenericObjectViewerForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (e.CloseReason == CloseReason.UserClosing)
         {
            e.Cancel = true;
            Hide();
         }
      }

      private void pauseResume_Click(object sender, EventArgs e)
      {
         if (timer.Enabled)
         {
            timer.Stop();
            pauseResume.Text = "Resume";
         }
         else
         {
            timer.Start();
            pauseResume.Text = "Pause";
         }
      }
   }
}
