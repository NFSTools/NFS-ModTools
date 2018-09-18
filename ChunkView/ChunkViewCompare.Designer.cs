namespace ChunkView
{
    partial class ChunkViewCompare
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
            this.hexBox1 = new Be.Windows.Forms.HexBox();
            this.hexBox2 = new Be.Windows.Forms.HexBox();
            this.SuspendLayout();
            // 
            // hexBox1
            // 
            this.hexBox1.BackColor = System.Drawing.Color.Gainsboro;
            this.hexBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.hexBox1.Location = new System.Drawing.Point(1, 12);
            this.hexBox1.Name = "hexBox1";
            this.hexBox1.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexBox1.Size = new System.Drawing.Size(487, 684);
            this.hexBox1.TabIndex = 0;
            // 
            // hexBox2
            // 
            this.hexBox2.BackColor = System.Drawing.Color.Gainsboro;
            this.hexBox2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.hexBox2.Location = new System.Drawing.Point(494, 12);
            this.hexBox2.Name = "hexBox2";
            this.hexBox2.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexBox2.Size = new System.Drawing.Size(513, 684);
            this.hexBox2.TabIndex = 1;
            // 
            // ChunkViewCompare
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.hexBox2);
            this.Controls.Add(this.hexBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ChunkViewCompare";
            this.Text = "ChunkViewCompare";
            this.ResumeLayout(false);

        }

        #endregion

        private Be.Windows.Forms.HexBox hexBox1;
        private Be.Windows.Forms.HexBox hexBox2;
    }
}