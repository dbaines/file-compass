using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using HDDIndexer.Services;
using HDDIndexer.ViewModels;
using HDDIndexer.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Windows.Storage;

namespace HDDIndexer
{
    public partial class App : Application
    {
        private Window? m_window;
        public IHost Host { get; }
        public static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            this.InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    ConfigureServices(services);
                })
                .Build();

            Services = Host.Services;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database context
            var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "hddindexer.db");
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Services
            services.AddSingleton<IDriveService, DriveService>();
            services.AddSingleton<IScannerService, ScannerService>();
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IValidationService, ValidationService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DriveListViewModel>();
            services.AddTransient<FileBrowserViewModel>();
            services.AddTransient<SearchViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<BackupRestoreViewModel>();

            // Views
            services.AddTransient<MainWindow>();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Start the host
            await Host.StartAsync();

            // Initialize database
            using (var scope = Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            m_window = Services.GetRequiredService<MainWindow>();
            m_window.Activate();
        }

        protected override async void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
        }
    }
}