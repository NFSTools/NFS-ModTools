namespace TexEd
{
    partial class TexEdMain
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
            this.textureDataGrid = new System.Windows.Forms.DataGridView();
            this.TexName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TexDimensions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TexMipmaps = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TexFormat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.messageLabel = new System.Windows.Forms.Label();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addTextureMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.streamToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateSectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportSectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tpkDataGrid = new System.Windows.Forms.DataGridView();
            this.PackName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PackPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PackSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PackOffset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PackTexCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.textureDataGrid)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tpkDataGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textureDataGrid
            // 
            this.textureDataGrid.AllowUserToAddRows = false;
            this.textureDataGrid.AllowUserToDeleteRows = false;
            this.textureDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.textureDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.textureDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TexName,
            this.TexDimensions,
            this.TexMipmaps,
            this.TexFormat});
            this.textureDataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.textureDataGrid.Location = new System.Drawing.Point(0, 352);
            this.textureDataGrid.MultiSelect = false;
            this.textureDataGrid.Name = "textureDataGrid";
            this.textureDataGrid.ReadOnly = true;
            this.textureDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.textureDataGrid.Size = new System.Drawing.Size(542, 296);
            this.textureDataGrid.TabIndex = 3;
            // 
            // TexName
            // 
            this.TexName.DataPropertyName = "Name";
            this.TexName.HeaderText = "Name";
            this.TexName.Name = "TexName";
            this.TexName.ReadOnly = true;
            // 
            // TexDimensions
            // 
            this.TexDimensions.DataPropertyName = "Dimensions";
            this.TexDimensions.HeaderText = "Dimensions";
            this.TexDimensions.Name = "TexDimensions";
            this.TexDimensions.ReadOnly = true;
            // 
            // TexMipmaps
            // 
            this.TexMipmaps.DataPropertyName = "MipMapCount";
            this.TexMipmaps.HeaderText = "Mipmaps";
            this.TexMipmaps.Name = "TexMipmaps";
            this.TexMipmaps.ReadOnly = true;
            // 
            // TexFormat
            // 
            this.TexFormat.DataPropertyName = "Format";
            this.TexFormat.HeaderText = "Format";
            this.TexFormat.Name = "TexFormat";
            this.TexFormat.ReadOnly = true;
            // 
            // messageLabel
            // 
            this.messageLabel.AutoSize = true;
            this.messageLabel.Location = new System.Drawing.Point(12, 659);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(83, 13);
            this.messageLabel.TabIndex = 5;
            this.messageLabel.Text = "INFO: Waiting...";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.toolStripSeparator1,
            this.closeToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.saveToolStripMenuItem.Text = "Save";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(100, 6);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.closeToolStripMenuItem.Text = "Close";
            // 
            // packToolStripMenuItem
            // 
            this.packToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addTextureMenuItem});
            this.packToolStripMenuItem.Name = "packToolStripMenuItem";
            this.packToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.packToolStripMenuItem.Text = "Pack";
            // 
            // addTextureMenuItem
            // 
            this.addTextureMenuItem.Enabled = false;
            this.addTextureMenuItem.Name = "addTextureMenuItem";
            this.addTextureMenuItem.Size = new System.Drawing.Size(137, 22);
            this.addTextureMenuItem.Text = "Add Texture";
            // 
            // streamToolStripMenuItem
            // 
            this.streamToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importToolStripMenuItem,
            this.updateSectionsToolStripMenuItem,
            this.exportSectionsToolStripMenuItem});
            this.streamToolStripMenuItem.Name = "streamToolStripMenuItem";
            this.streamToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.streamToolStripMenuItem.Text = "Texture";
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Enabled = false;
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.importToolStripMenuItem.Text = "Import";
            // 
            // updateSectionsToolStripMenuItem
            // 
            this.updateSectionsToolStripMenuItem.Enabled = false;
            this.updateSectionsToolStripMenuItem.Name = "updateSectionsToolStripMenuItem";
            this.updateSectionsToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.updateSectionsToolStripMenuItem.Text = "Export Selected";
            // 
            // exportSectionsToolStripMenuItem
            // 
            this.exportSectionsToolStripMenuItem.Enabled = false;
            this.exportSectionsToolStripMenuItem.Name = "exportSectionsToolStripMenuItem";
            this.exportSectionsToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.exportSectionsToolStripMenuItem.Text = "Export All";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.packToolStripMenuItem,
            this.streamToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1264, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tpkDataGrid
            // 
            this.tpkDataGrid.AllowUserToAddRows = false;
            this.tpkDataGrid.AllowUserToDeleteRows = false;
            this.tpkDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.tpkDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tpkDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PackName,
            this.PackPath,
            this.PackSize,
            this.PackOffset,
            this.PackTexCount});
            this.tpkDataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.tpkDataGrid.Location = new System.Drawing.Point(0, 27);
            this.tpkDataGrid.MultiSelect = false;
            this.tpkDataGrid.Name = "tpkDataGrid";
            this.tpkDataGrid.ReadOnly = true;
            this.tpkDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.tpkDataGrid.Size = new System.Drawing.Size(542, 296);
            this.tpkDataGrid.TabIndex = 2;
            // 
            // PackName
            // 
            this.PackName.DataPropertyName = "Name";
            this.PackName.HeaderText = "Name";
            this.PackName.Name = "PackName";
            this.PackName.ReadOnly = true;
            // 
            // PackPath
            // 
            this.PackPath.DataPropertyName = "PipelinePath";
            this.PackPath.HeaderText = "Path";
            this.PackPath.Name = "PackPath";
            this.PackPath.ReadOnly = true;
            // 
            // PackSize
            // 
            this.PackSize.DataPropertyName = "Size";
            this.PackSize.HeaderText = "Size";
            this.PackSize.Name = "PackSize";
            this.PackSize.ReadOnly = true;
            // 
            // PackOffset
            // 
            this.PackOffset.DataPropertyName = "Offset";
            this.PackOffset.HeaderText = "Offset";
            this.PackOffset.Name = "PackOffset";
            this.PackOffset.ReadOnly = true;
            // 
            // PackTexCount
            // 
            this.PackTexCount.DataPropertyName = "NumTextures";
            this.PackTexCount.HeaderText = "Textures";
            this.PackTexCount.Name = "PackTexCount";
            this.PackTexCount.ReadOnly = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(548, 27);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(704, 621);
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // TexEdMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.textureDataGrid);
            this.Controls.Add(this.tpkDataGrid);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1280, 720);
            this.MinimumSize = new System.Drawing.Size(1280, 720);
            this.Name = "TexEdMain";
            this.Text = "TexEd v0.0.1 by heyitsleo";
            ((System.ComponentModel.ISupportInitialize)(this.textureDataGrid)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tpkDataGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView textureDataGrid;
        private System.Windows.Forms.Label messageLabel;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addTextureMenuItem;
        private System.Windows.Forms.ToolStripMenuItem streamToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateSectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportSectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.DataGridView tpkDataGrid;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.DataGridViewTextBoxColumn PackName;
        private System.Windows.Forms.DataGridViewTextBoxColumn PackPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn PackSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn PackOffset;
        private System.Windows.Forms.DataGridViewTextBoxColumn PackTexCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn TexName;
        private System.Windows.Forms.DataGridViewTextBoxColumn TexDimensions;
        private System.Windows.Forms.DataGridViewTextBoxColumn TexMipmaps;
        private System.Windows.Forms.DataGridViewTextBoxColumn TexFormat;
    }
}

