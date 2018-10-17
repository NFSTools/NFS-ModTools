namespace Hasher
{
    partial class HasherMain
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
            this.inputText = new System.Windows.Forms.TextBox();
            this.inputTextLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.binFileHash = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.binMemHash = new System.Windows.Forms.TextBox();
            this.vltMemHash = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.vltFileHash = new System.Windows.Forms.TextBox();
            this.vlt64MemHash = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.vlt64FileHash = new System.Windows.Forms.TextBox();
            this.copyInputButton = new System.Windows.Forms.Button();
            this.copyBinFileHashButton = new System.Windows.Forms.Button();
            this.copyBinMemHashButton = new System.Windows.Forms.Button();
            this.copyVltFileHashButton = new System.Windows.Forms.Button();
            this.copyVltMemHashButton = new System.Windows.Forms.Button();
            this.copyVlt64FileHashButton = new System.Windows.Forms.Button();
            this.copyVlt64MemHashButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // inputText
            // 
            this.inputText.Location = new System.Drawing.Point(145, 25);
            this.inputText.Multiline = true;
            this.inputText.Name = "inputText";
            this.inputText.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.inputText.Size = new System.Drawing.Size(281, 30);
            this.inputText.TabIndex = 0;
            // 
            // inputTextLabel
            // 
            this.inputTextLabel.AutoSize = true;
            this.inputTextLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputTextLabel.Location = new System.Drawing.Point(13, 31);
            this.inputTextLabel.Name = "inputTextLabel";
            this.inputTextLabel.Size = new System.Drawing.Size(70, 17);
            this.inputTextLabel.TabIndex = 1;
            this.inputTextLabel.Text = "Input Text";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(14, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "BIN File";
            // 
            // binFileHash
            // 
            this.binFileHash.Enabled = false;
            this.binFileHash.Location = new System.Drawing.Point(146, 72);
            this.binFileHash.Multiline = true;
            this.binFileHash.Name = "binFileHash";
            this.binFileHash.ReadOnly = true;
            this.binFileHash.Size = new System.Drawing.Size(281, 30);
            this.binFileHash.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(14, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "BIN Memory";
            // 
            // binMemHash
            // 
            this.binMemHash.Enabled = false;
            this.binMemHash.Location = new System.Drawing.Point(146, 119);
            this.binMemHash.Multiline = true;
            this.binMemHash.Name = "binMemHash";
            this.binMemHash.ReadOnly = true;
            this.binMemHash.Size = new System.Drawing.Size(281, 30);
            this.binMemHash.TabIndex = 5;
            // 
            // vltMemHash
            // 
            this.vltMemHash.Enabled = false;
            this.vltMemHash.Location = new System.Drawing.Point(146, 215);
            this.vltMemHash.Multiline = true;
            this.vltMemHash.Name = "vltMemHash";
            this.vltMemHash.ReadOnly = true;
            this.vltMemHash.Size = new System.Drawing.Size(281, 30);
            this.vltMemHash.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(14, 221);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "VLT Memory";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(14, 174);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 17);
            this.label4.TabIndex = 7;
            this.label4.Text = "VLT File";
            // 
            // vltFileHash
            // 
            this.vltFileHash.Enabled = false;
            this.vltFileHash.Location = new System.Drawing.Point(146, 168);
            this.vltFileHash.Multiline = true;
            this.vltFileHash.Name = "vltFileHash";
            this.vltFileHash.ReadOnly = true;
            this.vltFileHash.Size = new System.Drawing.Size(281, 30);
            this.vltFileHash.TabIndex = 6;
            // 
            // vlt64MemHash
            // 
            this.vlt64MemHash.Enabled = false;
            this.vlt64MemHash.Location = new System.Drawing.Point(146, 313);
            this.vlt64MemHash.Multiline = true;
            this.vlt64MemHash.Name = "vlt64MemHash";
            this.vlt64MemHash.ReadOnly = true;
            this.vlt64MemHash.Size = new System.Drawing.Size(281, 30);
            this.vlt64MemHash.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(14, 319);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "VLT64 Memory";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(14, 272);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 17);
            this.label6.TabIndex = 11;
            this.label6.Text = "VLT64 File";
            // 
            // vlt64FileHash
            // 
            this.vlt64FileHash.Enabled = false;
            this.vlt64FileHash.Location = new System.Drawing.Point(146, 266);
            this.vlt64FileHash.Multiline = true;
            this.vlt64FileHash.Name = "vlt64FileHash";
            this.vlt64FileHash.ReadOnly = true;
            this.vlt64FileHash.Size = new System.Drawing.Size(281, 30);
            this.vlt64FileHash.TabIndex = 10;
            // 
            // copyInputButton
            // 
            this.copyInputButton.Enabled = false;
            this.copyInputButton.Location = new System.Drawing.Point(444, 29);
            this.copyInputButton.Name = "copyInputButton";
            this.copyInputButton.Size = new System.Drawing.Size(75, 23);
            this.copyInputButton.TabIndex = 14;
            this.copyInputButton.Text = "Copy";
            this.copyInputButton.UseVisualStyleBackColor = true;
            // 
            // copyBinFileHashButton
            // 
            this.copyBinFileHashButton.Location = new System.Drawing.Point(444, 76);
            this.copyBinFileHashButton.Name = "copyBinFileHashButton";
            this.copyBinFileHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyBinFileHashButton.TabIndex = 15;
            this.copyBinFileHashButton.Text = "Copy";
            this.copyBinFileHashButton.UseVisualStyleBackColor = true;
            // 
            // copyBinMemHashButton
            // 
            this.copyBinMemHashButton.Location = new System.Drawing.Point(444, 123);
            this.copyBinMemHashButton.Name = "copyBinMemHashButton";
            this.copyBinMemHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyBinMemHashButton.TabIndex = 16;
            this.copyBinMemHashButton.Text = "Copy";
            this.copyBinMemHashButton.UseVisualStyleBackColor = true;
            // 
            // copyVltFileHashButton
            // 
            this.copyVltFileHashButton.Location = new System.Drawing.Point(447, 172);
            this.copyVltFileHashButton.Name = "copyVltFileHashButton";
            this.copyVltFileHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyVltFileHashButton.TabIndex = 17;
            this.copyVltFileHashButton.Text = "Copy";
            this.copyVltFileHashButton.UseVisualStyleBackColor = true;
            // 
            // copyVltMemHashButton
            // 
            this.copyVltMemHashButton.Location = new System.Drawing.Point(447, 219);
            this.copyVltMemHashButton.Name = "copyVltMemHashButton";
            this.copyVltMemHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyVltMemHashButton.TabIndex = 18;
            this.copyVltMemHashButton.Text = "Copy";
            this.copyVltMemHashButton.UseVisualStyleBackColor = true;
            // 
            // copyVlt64FileHashButton
            // 
            this.copyVlt64FileHashButton.Location = new System.Drawing.Point(447, 270);
            this.copyVlt64FileHashButton.Name = "copyVlt64FileHashButton";
            this.copyVlt64FileHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyVlt64FileHashButton.TabIndex = 19;
            this.copyVlt64FileHashButton.Text = "Copy";
            this.copyVlt64FileHashButton.UseVisualStyleBackColor = true;
            // 
            // copyVlt64MemHashButton
            // 
            this.copyVlt64MemHashButton.Location = new System.Drawing.Point(447, 318);
            this.copyVlt64MemHashButton.Name = "copyVlt64MemHashButton";
            this.copyVlt64MemHashButton.Size = new System.Drawing.Size(75, 23);
            this.copyVlt64MemHashButton.TabIndex = 20;
            this.copyVlt64MemHashButton.Text = "Copy";
            this.copyVlt64MemHashButton.UseVisualStyleBackColor = true;
            // 
            // HasherMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 361);
            this.Controls.Add(this.copyVlt64MemHashButton);
            this.Controls.Add(this.copyVlt64FileHashButton);
            this.Controls.Add(this.copyVltMemHashButton);
            this.Controls.Add(this.copyVltFileHashButton);
            this.Controls.Add(this.copyBinMemHashButton);
            this.Controls.Add(this.copyBinFileHashButton);
            this.Controls.Add(this.copyInputButton);
            this.Controls.Add(this.vlt64MemHash);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.vlt64FileHash);
            this.Controls.Add(this.vltMemHash);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.vltFileHash);
            this.Controls.Add(this.binMemHash);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.binFileHash);
            this.Controls.Add(this.inputTextLabel);
            this.Controls.Add(this.inputText);
            this.MaximizeBox = false;
            this.Name = "HasherMain";
            this.Text = "Hasher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputText;
        private System.Windows.Forms.Label inputTextLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox binFileHash;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox binMemHash;
        private System.Windows.Forms.TextBox vltMemHash;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox vltFileHash;
        private System.Windows.Forms.TextBox vlt64MemHash;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox vlt64FileHash;
        private System.Windows.Forms.Button copyInputButton;
        private System.Windows.Forms.Button copyBinFileHashButton;
        private System.Windows.Forms.Button copyBinMemHashButton;
        private System.Windows.Forms.Button copyVltFileHashButton;
        private System.Windows.Forms.Button copyVltMemHashButton;
        private System.Windows.Forms.Button copyVlt64FileHashButton;
        private System.Windows.Forms.Button copyVlt64MemHashButton;
    }
}

