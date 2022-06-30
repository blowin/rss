using RssDetect.Domain;

namespace RssDetect.WinForms
{
    public partial class MainForm : Form
    {
        private Rss _rss = new Rss();

        public MainForm()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void btnDetect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLink.Text))
            {
                ShowError("Specify url");
                return;
            }

            if (!Uri.TryCreate(txtLink.Text, UriKind.Absolute, out var uri))
            {
                ShowError("Invalid Uri");
                return;
            }

            var result = new HashSet<RssLink>();
            
            using var cursor = new WaitCursor(btnDetect, txtLink);
            lstDetectResult.Items.Clear();

            await foreach (var rssLink in _rss.DetectAsync(uri))
                lstDetectResult.Items.Add(rssLink.Link);
        }

        private void lstDetectResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.C) return;
            
            var link = lstDetectResult.SelectedItem?.ToString();
            if(string.IsNullOrEmpty(link))
                return;

            Clipboard.SetData(DataFormats.StringFormat, link);
        }
    }
}