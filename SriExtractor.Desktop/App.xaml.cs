using System.Windows;
using SriExtractor.Desktop.Infrastructure;

namespace SriExtractor.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppHost.Initialize();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppHost.Dispose();
            base.OnExit(e);
        }
    }

}
