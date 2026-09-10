using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SnapLocalGameLab.Core
{
    public static class Bootstrap
    {
        private static Thread _window;
        static Bootstrap()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var path = Path.Combine(Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location),
                    new AssemblyName(args.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
        }

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
