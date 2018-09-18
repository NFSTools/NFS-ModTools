using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChunkView
{
    public partial class ChunkViewCompare : Form
    {
        public ChunkViewCompare()
        {
            hexBox1.Font = new Font("Consolas", 10.25F);
            hexBox1.ReadOnly = true;
            hexBox1.LineInfoVisible = true;
            hexBox1.ShadowSelectionColor = Color.FromArgb(100, 60, 188, 255);
            hexBox1.StringViewVisible = true;
            hexBox1.UseFixedBytesPerLine = true;
            hexBox1.VScrollBarVisible = true;
            hexBox1.ColumnInfoVisible = true;

            hexBox2.Font = new Font("Consolas", 10.25F);
            hexBox2.ReadOnly = true;
            hexBox2.LineInfoVisible = true;
            hexBox2.ShadowSelectionColor = Color.FromArgb(100, 60, 188, 255);
            hexBox2.StringViewVisible = true;
            hexBox2.UseFixedBytesPerLine = true;
            hexBox2.VScrollBarVisible = true;
            hexBox2.ColumnInfoVisible = true;
        }
    }
}
