using System;
using System.Windows;
using Akavache;

namespace Pack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            BlobCache.ApplicationName = "Pack";
            BlobCache.EnsureInitialized();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            BlobCache.Shutdown();
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var root = ((Exception) e.ExceptionObject).GetBaseException();
            MessageBox.Show("ERROR:\n\n" + root.Message + "\n\n Full details are now on your Clipboard, please paste them into a new issue", "ERROR!",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
