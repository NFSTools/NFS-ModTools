namespace StreamEd
{
    sealed partial class StreamEdMain
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
            this.gameFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.streamToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportSectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bundleSelectionBox = new System.Windows.Forms.ComboBox();
            this.sectionsDataGrid = new System.Windows.Forms.DataGridView();
            this.messageLabel = new System.Windows.Forms.Label();
            this.SectionName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionChunkNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionPosition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionStreamOffset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionOtherSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sectionsDataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.streamToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1264, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
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
            this.openToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.saveToolStripMenuItem.Text = "Save Stream";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(135, 6);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.closeToolStripMenuItem.Text = "Close";
            // 
            // streamToolStripMenuItem
            // 
            this.streamToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportSectionsToolStripMenuItem});
            this.streamToolStripMenuItem.Name = "streamToolStripMenuItem";
            this.streamToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.streamToolStripMenuItem.Text = "Stream";
            // 
            // exportSectionsToolStripMenuItem
            // 
            this.exportSectionsToolStripMenuItem.Enabled = false;
            this.exportSectionsToolStripMenuItem.Name = "exportSectionsToolStripMenuItem";
            this.exportSectionsToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.exportSectionsToolStripMenuItem.Text = "Export Sections";
            this.exportSectionsToolStripMenuItem.Click += new System.EventHandler(this.exportSectionsToolStripMenuItem_Click);
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
            // 
            // bundleSelectionBox
            // 
            this.bundleSelectionBox.DisplayMember = "Name";
            this.bundleSelectionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bundleSelectionBox.Enabled = false;
            this.bundleSelectionBox.FormattingEnabled = true;
            this.bundleSelectionBox.Location = new System.Drawing.Point(3, 27);
            this.bundleSelectionBox.Name = "bundleSelectionBox";
            this.bundleSelectionBox.Size = new System.Drawing.Size(164, 21);
            this.bundleSelectionBox.TabIndex = 2;
            // 
            // sectionsDataGrid
            // 
            this.sectionsDataGrid.AllowUserToAddRows = false;
            this.sectionsDataGrid.AllowUserToDeleteRows = false;
            this.sectionsDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.sectionsDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.sectionsDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SectionName,
            this.SectionChunkNumber,
            this.SectionPosition,
            this.SectionStreamOffset,
            this.SectionSize,
            this.SectionOtherSize});
            this.sectionsDataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.sectionsDataGrid.Location = new System.Drawing.Point(0, 55);
            this.sectionsDataGrid.MultiSelect = false;
            this.sectionsDataGrid.Name = "sectionsDataGrid";
            this.sectionsDataGrid.ReadOnly = true;
            this.sectionsDataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.sectionsDataGrid.Size = new System.Drawing.Size(1264, 596);
            this.sectionsDataGrid.TabIndex = 3;
            // 
            // messageLabel
            // 
            this.messageLabel.AutoSize = true;
            this.messageLabel.Location = new System.Drawing.Point(12, 660);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(83, 13);
            this.messageLabel.TabIndex = 4;
            this.messageLabel.Text = "INFO: Waiting...";
            // 
            // SectionName
            // 
            this.SectionName.DataPropertyName = "Name";
            this.SectionName.HeaderText = "Name";
            this.SectionName.Name = "SectionName";
            this.SectionName.ReadOnly = true;
            // 
            // SectionChunkNumber
            // 
            this.SectionChunkNumber.DataPropertyName = "Number";
            this.SectionChunkNumber.HeaderText = "Chunk Number";
            this.SectionChunkNumber.Name = "SectionChunkNumber";
            this.SectionChunkNumber.ReadOnly = true;
            // 
            // SectionPosition
            // 
            this.SectionPosition.DataPropertyName = "Position";
            this.SectionPosition.HeaderText = "Position";
            this.SectionPosition.Name = "SectionPosition";
            this.SectionPosition.ReadOnly = true;
            // 
            // SectionStreamOffset
            // 
            this.SectionStreamOffset.DataPropertyName = "Offset";
            this.SectionStreamOffset.HeaderText = "Offset";
            this.SectionStreamOffset.Name = "SectionStreamOffset";
            this.SectionStreamOffset.ReadOnly = true;
            // 
            // SectionSize
            // 
            this.SectionSize.DataPropertyName = "Size";
            this.SectionSize.HeaderText = "Size";
            this.SectionSize.Name = "SectionSize";
            this.SectionSize.ReadOnly = true;
            // 
            // SectionOtherSize
            // 
            this.SectionOtherSize.DataPropertyName = "OtherSize";
            this.SectionOtherSize.HeaderText = "Other Size";
            this.SectionOtherSize.Name = "SectionOtherSize";
            this.SectionOtherSize.ReadOnly = true;
            // 
            // StreamEdMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.sectionsDataGrid);
            this.Controls.Add(this.bundleSelectionBox);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "StreamEdMain";
            this.Text = "StreamEd v0.0.1 by heyitsleo";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sectionsDataGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog gameFolderBrowser;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem streamToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportSectionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ComboBox bundleSelectionBox;
        private System.Windows.Forms.DataGridView sectionsDataGrid;
        private System.Windows.Forms.Label messageLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionChunkNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionPosition;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionStreamOffset;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionOtherSize;
    }
}

