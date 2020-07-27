namespace Orts.Viewer3D.Debugging
{
    partial class MessageViewer
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
		  this.clearAll = new System.Windows.Forms.Button();
		  this.replySelected = new System.Windows.Forms.Button();
		  this.messages = new System.Windows.Forms.ListBox();
		  this.MSG = new System.Windows.Forms.TextBox();
		  this.compose = new System.Windows.Forms.Button();
		  this.compose.Click += new System.EventHandler(this.ComposeClick);
		  this.SuspendLayout();
		  // 
		  // clearAll
		  // 
		  this.clearAll.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.clearAll.Location = new System.Drawing.Point(12, 12);
		  this.clearAll.Name = "clearAll";
		  this.clearAll.Size = new System.Drawing.Size(110, 26);
		  this.clearAll.TabIndex = 2;
		  this.clearAll.Text = "Clear All";
		  this.clearAll.UseVisualStyleBackColor = true;
		  this.clearAll.Click += new System.EventHandler(this.ClearAllClick);
		  // 
		  // replySelected
		  // 
		  this.replySelected.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.replySelected.Location = new System.Drawing.Point(338, 12);
		  this.replySelected.Name = "replySelected";
		  this.replySelected.Size = new System.Drawing.Size(118, 26);
		  this.replySelected.TabIndex = 4;
		  this.replySelected.Text = "Reply Selected";
		  this.replySelected.UseVisualStyleBackColor = true;
		  this.replySelected.Click += new System.EventHandler(this.ReplySelectedClick);
		  // 
		  // messages
		  // 
		  this.messages.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.messages.FormattingEnabled = true;
		  this.messages.ItemHeight = 20;
		  this.messages.Location = new System.Drawing.Point(14, 78);
		  this.messages.Name = "messages";
		  this.messages.Size = new System.Drawing.Size(519, 184);
		  this.messages.TabIndex = 3;
		  // 
		  // MSG
		  // 
		  this.MSG.Enabled = false;
		  this.MSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.MSG.Location = new System.Drawing.Point(14, 44);
		  this.MSG.Name = "MSG";
		  this.MSG.Size = new System.Drawing.Size(519, 29);
		  this.MSG.TabIndex = 19;
		  this.MSG.WordWrap = false;
		  // 
		  // compose
		  // 
		  this.compose.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.compose.Location = new System.Drawing.Point(174, 12);
		  this.compose.Name = "compose";
		  this.compose.Size = new System.Drawing.Size(118, 26);
		  this.compose.TabIndex = 20;
		  this.compose.Text = "Compose MSG";
		  this.compose.UseVisualStyleBackColor = true;
		  // 
		  // MessageViewer
		  // 
		  this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		  this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		  this.ClientSize = new System.Drawing.Size(537, 266);
		  this.Controls.Add(this.compose);
		  this.Controls.Add(this.MSG);
		  this.Controls.Add(this.replySelected);
		  this.Controls.Add(this.messages);
		  this.Controls.Add(this.clearAll);
		  this.Name = "MessageViewer";
		  this.Text = "MessageViewer";
		  this.ResumeLayout(false);
		  this.PerformLayout();

      }

      #endregion

	  private System.Windows.Forms.Button clearAll;
	  private System.Windows.Forms.Button replySelected;
	  private System.Windows.Forms.ListBox messages;
	  private System.Windows.Forms.TextBox MSG;
	  private System.Windows.Forms.Button compose;
   }
}
