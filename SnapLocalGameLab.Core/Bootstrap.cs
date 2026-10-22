using System;
using System.Threading;
using System.Windows.Forms;

namespace SnapLocalGameLab.Core
{
    public static class Bootstrap
    {
        private static Thread _window;

        public static void Init(string settings)
        {
            lock (typeof(Bootstrap))
            {
                if (_window != null && _window.IsAlive) return;
                _window = new Thread(() =>
                {
                    try
                    {
                        GameThread.Initialize();
                        Application.Run(new LabForm());
                    }
                    catch (Exception e) { MessageBox.Show(e.ToString(), "Snap Local Game Lab"); }
                }) { IsBackground = true };
                _window.SetApartmentState(ApartmentState.STA);
                _window.Start();
            }
        }
    }
}
