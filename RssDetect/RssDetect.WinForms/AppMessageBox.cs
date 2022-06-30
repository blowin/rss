namespace RssDetect.WinForms;

public class AppMessageBox
{
    public void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}