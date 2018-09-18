using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CommonHasher = Common.Hasher;

namespace Hasher
{
    public partial class HasherMain : Form
    {
        public HasherMain()
        {
            InitializeComponent();

            inputText.TextChanged += InputText_OnTextChanged;
        }

        private void InputText_OnTextChanged(object sender, EventArgs e)
        {
            UpdateHashes(inputText.Text);
        }

        private void UpdateHashes(string text)
        {
            var binHashBytes = BitConverter.GetBytes(CommonHasher.BinHash(text));
            var vltBytes = BitConverter.GetBytes(CommonHasher.VltHash(text));
            var vlt64Bytes = BitConverter.GetBytes(CommonHasher.VltHash64(text));

            binFileHash.Text = $"0x{string.Join("", binHashBytes.Reverse().Select(c => c.ToString("X2")))}";
            binMemHash.Text = $"0x{string.Join("", binHashBytes.Select(c => c.ToString("X2")))}";
            vltFileHash.Text = $"0x{string.Join("", vltBytes.Reverse().Select(c => c.ToString("X2")))}";
            vltMemHash.Text = $"0x{string.Join("", vltBytes.Select(c => c.ToString("X2")))}";
            vlt64FileHash.Text = $"0x{string.Join("", vlt64Bytes.Reverse().Select(c => c.ToString("X2")))}";
            vlt64MemHash.Text = $"0x{string.Join("", vlt64Bytes.Select(c => c.ToString("X2")))}";

            binHashBytes = null;
            vltBytes = null;
            vlt64Bytes = null;
        }
    }
}
