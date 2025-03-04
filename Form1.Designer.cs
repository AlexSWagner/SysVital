namespace SysVital
{
    partial class Form1
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
            this.lblCPU = new System.Windows.Forms.Label();
            this.lblRAM = new System.Windows.Forms.Label();
            this.lblDiskRead = new System.Windows.Forms.Label();
            this.lblDiskWrite = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblCPU
            // 
            this.lblCPU.Location = new System.Drawing.Point(0, 0);
            this.lblCPU.Name = "lblCPU";
            this.lblCPU.Size = new System.Drawing.Size(112, 29);
            this.lblCPU.TabIndex = 3;
            // 
            // lblRAM
            // 
            this.lblRAM.Location = new System.Drawing.Point(0, 0);
            this.lblRAM.Name = "lblRAM";
            this.lblRAM.Size = new System.Drawing.Size(112, 29);
            this.lblRAM.TabIndex = 2;
            // 
            // lblDiskRead
            // 
            this.lblDiskRead.Location = new System.Drawing.Point(0, 0);
            this.lblDiskRead.Name = "lblDiskRead";
            this.lblDiskRead.Size = new System.Drawing.Size(112, 29);
            this.lblDiskRead.TabIndex = 1;
            // 
            // lblDiskWrite
            // 
            this.lblDiskWrite.Location = new System.Drawing.Point(0, 0);
            this.lblDiskWrite.Name = "lblDiskWrite";
            this.lblDiskWrite.Size = new System.Drawing.Size(112, 29);
            this.lblDiskWrite.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 375);
            this.Controls.Add(this.lblDiskWrite);
            this.Controls.Add(this.lblDiskRead);
            this.Controls.Add(this.lblRAM);
            this.Controls.Add(this.lblCPU);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form1";
            this.Text = "PC Performance Monitor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblCPU;
        private System.Windows.Forms.Label lblRAM;
        private System.Windows.Forms.Label lblDiskRead;
        private System.Windows.Forms.Label lblDiskWrite;
    }
}
