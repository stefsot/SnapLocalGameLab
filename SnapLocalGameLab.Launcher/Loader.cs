using ManagedInjector;
using SnapLocalGameLab.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapLocalGameLab.Launcher
{
    internal static class Loader
    {
        public static Process GetActiveSnapProcess()
        {
            return Process.GetProcessesByName("SNAP").FirstOrDefault();
        }

        public static bool Inject(string settings = null)
        {
            var process = GetActiveSnapProcess();

            if (process == null)
                return false;

            var type = typeof(Bootstrap);

            var transportData = new InjectorData
            {
                AssemblyName = type.Assembly.Location,
                ClassName = type.FullName,
                MethodName = "Init",
                SettingsFile = settings
            };

            Injector.Launch(process.MainWindowHandle, transportData);

            return true;
        }
    }
}
