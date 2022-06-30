using System.Reflection;
using RssDetect.Domain;

namespace RssDetect.WinForms
{
    public partial class MainForm : Form
    {
        private readonly Rss _rss;
        private readonly AppMessageBox _appMessageBox;

        public MainForm(AppMessageBox appMessageBox)
        {
            _appMessageBox = appMessageBox;
            InitializeComponent();

            var progress = new Progress<DetectProgress>(OnProgress);
            _rss = new Rss(progress);

            var version = typeof(MainForm).Assembly.GetName()?.Version?.ToString();
            if(!string.IsNullOrEmpty(version))
                Text += " - " + version;
        }

        private void OnProgress(DetectProgress obj)
        {
            obj.Match(progress =>
                {
                    progressBar.Value = 0;
                    progressBar.Maximum = progress.MaxOperation;
                    progressBar.Step = 1;
                },
                progress => progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum),
                progress => progressBar.Value = 0);
        }

        private async void btnDetect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLink.Text))
            {
                _appMessageBox.ShowError("Specify url");
                return;
            }

            if (!Uri.TryCreate(txtLink.Text, UriKind.Absolute, out var uri))
            {
                _appMessageBox.ShowError("Invalid Uri");
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