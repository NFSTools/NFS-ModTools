using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapEd
{
    public partial class MapEdSearch : Form
    {
        public string Query { get; private set; }

        public MapEdSearch()
        {
            InitializeComponent();

            textBox1.TextChanged += TextBox1_OnTextChanged;
        }

        private void TextBox1_OnTextChanged(object sender, EventArgs e)
        {
            searchButton.Enabled = !string.IsNullOrWhiteSpace(textBox1.Text);
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            Query = textBox1.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
