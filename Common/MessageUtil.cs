using System.Windows.Forms;

namespace Common
{
    public static class MessageUtil
    {
        private static string _appName = "UNLOCALIZED";

        public static void SetAppName(string appName)
        {
            _appName = appName;
        }

        public static DialogResult ShowInfo(string message)
        {
            return MessageBox.Show(message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult ShowError(string message)
        {
            return MessageBox.Show(message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
