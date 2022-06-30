using System.Text.Json;
using RssDetect.Domain;

namespace RssDetect.WinForms
{
    public sealed partial class MainForm : Form
    {
        private const string SettingsFileName = "appsettings.json";

        private readonly Rss _rss;
        private readonly AppMessageBox _appMessageBox;

        public MainForm(AppMessageBox appMessageBox)
        {
            InitializeComponent();

            _appMessageBox = appMessageBox;
            progressBar.Step = 1;

            _rss = CreateRss();
            Text = BuildTitle(Text);
        }

        private Rss CreateRss()
        {
            var configuration = ReadRssConfiguration();
            var progress = new Progress<DetectProgress>(OnProgress);
            return new Rss(progress, configuration);
        }

        private static string BuildTitle(string currentTitle)
        {
            var version = typeof(MainForm).Assembly.GetName()?.Version?.ToString();
            return !string.IsNullOrEmpty(version) ? 
                currentTitle + " - " + version : 
                currentTitle;
        }

        private static RssConfiguration ReadRssConfiguration()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
            using var file = File.OpenRead(path);
            return JsonSerializer.Deserialize<RssConfiguration>(file) ?? new RssConfiguration();
        }

        private void OnProgress(DetectProgress progress)
        {
            progress.Match(
                start => progressBar.Maximum = start.MaxOperation,
                increase => progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum),
                finish => progressBar.Value = 0
            );
        }

        private (Uri? Uri, string ErrorMessage) GetUriFromLink()
        {
            if (string.IsNullOrEmpty(txtLink.Text))
                return (null, "Specify url");

            if (!Uri.TryCreate(txtLink.Text, UriKind.Absolute, out var uri))
                return (null, "Invalid Uri");

            return (uri, string.Empty);
        }

        private async void btnDetect_Click(object sender, EventArgs e)
        {
            var (uri, errorMessage) = GetUriFromLink();
            if (uri == null || !string.IsNullOrEmpty(errorMessage))
            {
                _appMessageBox.ShowError(errorMessage);
                return;
            }
            
            using var cursor = new WaitCursor(btnDetect, txtLink);
            lstDetectResult.Items.Clear();

            await foreach (var rssLink in _rss.DetectAsync(uri))
                lstDetectResult.Items.Add(rssLink.Link);
        }

        private void lstDetectResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.C) 
                return;
            
            var link = lstDetectResult.SelectedItem?.ToString();
            if(string.IsNullOrEmpty(link))
                return;

            Clipboard.SetData(DataFormats.StringFormat, link);
        }
    }
}