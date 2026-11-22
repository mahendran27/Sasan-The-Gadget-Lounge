using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SasanERP.Data.Context;
using SasanERP.Data.Seeding;
using SasanERP.Services.Barcode;
using SasanERP.Services.Billing;
using SasanERP.Services.Reporting;
using SasanERP.UI.ViewModels;

namespace SasanERP.UI;

/// <summary>
/// Interaction logic for App.xaml
/// Configures Dependency Injection container, EF Core SQLite DB, and services.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // SQLite Database Configuration (Standalone Offline First for Shop POS)
                string dbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SasanTheGadgetLounge",
                    "SasanERP.db");

                string? dbDir = System.IO.Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
                {
                    System.IO.Directory.CreateDirectory(dbDir);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={dbPath}");
                });

                // Core Services
                services.AddSingleton<BarcodeGeneratorService>();
                services.AddScoped<BillingService>();
                services.AddScoped<GstReportService>();

                // ViewModels
                services.AddTransient<PosViewModel>();
                services.AddTransient<InventoryIntakeViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<Views.InventoryIntakeWindow>();
            })
            .Build();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        // Ensure database is created and seeded with default Company & Chart of Accounts
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.InitializeAsync(db);
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
