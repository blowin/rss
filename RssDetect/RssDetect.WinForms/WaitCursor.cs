namespace RssDetect.WinForms;

public sealed class WaitCursor : IDisposable
{
    private readonly (bool InitEnableState, Control Control)[] _updateControls;

    public WaitCursor() : this(null)
    {
    }

    public WaitCursor(params Control[]? controls)
    {
        if (controls == null)
        {
            _updateControls = Array.Empty<(bool, Control)>();
        }
        else
        {
            _updateControls = new (bool InitEnableState, Control Control)[controls.Length];
            for (var i = 0; i < controls.Length; i++)
                _updateControls[i] = (controls[i].Enabled, controls[i]);
        }

        foreach (var updateControl in _updateControls)
            updateControl.Control.Enabled = false;

        Application.UseWaitCursor = true;
        Application.DoEvents();
    }

    public void Dispose()
    {
        for (var i = 0; i < _updateControls.Length; i++)
            _updateControls[i].Control.Enabled = _updateControls[i].InitEnableState;
        
        Cursor.Current = Cursors.Default;
        Application.UseWaitCursor = false;
    }
}