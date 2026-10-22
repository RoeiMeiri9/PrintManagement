using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PrintManagement.Services;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public static IServiceProvider Services { get; private set; } = null!;

        public static Window? MainWindow => (Current as App)?._window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += (_, e) => System.Diagnostics.Debug.WriteLine(e.Exception);
            Services = ConfigureServices;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        private static IServiceProvider ConfigureServices
        {
            get
            {
                var services = new ServiceCollection();

                // רישום השירותים באפליקציה (Singleton או Transient)
                services.AddSingleton<IExportService, ExportService>();

                // אם יש ViewModels שרוצים להזריק:
                // services.AddTransient<MainViewModel>();

                return services.BuildServiceProvider();
            }
        }

        public static T GetService<T>() where T : class
        {
            var service = Services.GetService(typeof(T)) as T;
            return service ?? throw new ArgumentException($"{typeof(T)} is not registered in Dependency Injection.");
        }
    }
}
