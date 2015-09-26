using System.Net;
using System.Windows;
using Akavache;
using Pack_v2.ViewModels;
using Pack_v2.Views;
using ReactiveUI;

namespace Pack_v2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterParts();
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
            BlobCache.ApplicationName = "Pack";
            BlobCache.EnsureInitialized();

            var shellVm = new ShellViewModel();
            var shellWindow = new ShellWindow
            {
                ViewModel = shellVm,
            };
            Current.MainWindow = shellWindow;
            Current.MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            BlobCache.Shutdown().Wait();
        }

        private static void RegisterParts()
        {
            var locator = Splat.Locator.CurrentMutable;

            locator.Register(() => new ProgressView(), typeof (IViewFor<ProgressViewModel>));
            locator.Register(() => new GitHubLoginView(), typeof(IViewFor<GitHubLoginViewModel>));
            locator.Register(() => new AuthPasswordEntryView(), typeof (IViewFor<AuthPasswordEntryViewModel>));
            locator.Register(() => new PackScreenView(), typeof(IViewFor<PackScreenViewModel>));
        }
    }
}
