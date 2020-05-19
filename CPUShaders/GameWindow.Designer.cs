namespace CPUShaders
{
    partial class GameWindow
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.frameLabel = new System.Windows.Forms.Label();
            this.controlLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(800, 600);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // frameLabel
            // 
            this.frameLabel.AccessibleName = "FrameLabel";
            this.frameLabel.AutoSize = true;
            this.frameLabel.BackColor = System.Drawing.Color.Green;
            this.frameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 25F);
            this.frameLabel.ForeColor = System.Drawing.Color.White;
            this.frameLabel.Location = new System.Drawing.Point(10, 10);
            this.frameLabel.Name = "frameLabel";
            this.frameLabel.Size = new System.Drawing.Size(93, 39);
            this.frameLabel.TabIndex = 1;
            this.frameLabel.Text = "1000";
            // 
            // controlLabel
            // 
            this.controlLabel.AccessibleName = "ControlLabel";
            this.controlLabel.AutoSize = true;
            this.controlLabel.BackColor = System.Drawing.Color.Green;
            this.controlLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.controlLabel.ForeColor = System.Drawing.Color.White;
            this.controlLabel.Location = new System.Drawing.Point(47, 543);
            this.controlLabel.Name = "controlLabel";
            this.controlLabel.Size = new System.Drawing.Size(709, 31);
            this.controlLabel.TabIndex = 2;
            this.controlLabel.Text = "Press x For Next, z for Previous, d to Hide/Show Interface";
            // 
            // GameWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.controlLabel);
            this.Controls.Add(this.frameLabel);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "GameWindow";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label frameLabel;
        private System.Windows.Forms.Label controlLabel;
    }
}

