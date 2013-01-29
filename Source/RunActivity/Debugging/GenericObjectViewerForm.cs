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
