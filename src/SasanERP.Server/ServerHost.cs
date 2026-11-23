using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SasanERP.Data.Context;
using SasanERP.Data.Seeding;
using SasanERP.Services.Accounting;
using SasanERP.Services.Analytics;
using SasanERP.Services.Backup;
using SasanERP.Services.Barcode;
using SasanERP.Services.Billing;
using SasanERP.Services.Invoicing;
using SasanERP.Services.Printing;
using SasanERP.Services.Reporting;
using SasanERP.Services.Security;
using SasanERP.Services.Wireless;

namespace SasanERP.Server;

public static class ServerHost
{
    private static WebApplication? _app;
    private static readonly object _lock = new();

    public static async Task<WebApplication> StartAsync(string[]? args = null, string url = "http://127.0.0.1:5000")
    {
        lock (_lock)
        {
            if (_app != null) return _app;
        }

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        builder.WebHost.UseUrls(url);

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalDesktop", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Configure JSON formatting
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        // SQLite Database Path
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SasanTheGadgetLounge",
            "SasanERP.db");

        string? dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Core Services
        builder.Services.AddSingleton<BarcodeGeneratorService>();
        builder.Services.AddSingleton<InvoicePrintEngine>();
        builder.Services.AddSingleton<BarcodeLabelEngine>();
        builder.Services.AddScoped<BillingService>();
        builder.Services.AddScoped<GstReportService>();
        builder.Services.AddScoped<ExecutiveDashboardService>();
        builder.Services.AddScoped<AuditLogService>();
        builder.Services.AddScoped<DatabaseBackupService>();
        builder.Services.AddScoped<InvoiceHistoryService>();
        builder.Services.AddScoped<PeriodAnalyticsService>();
        builder.Services.AddScoped<StoreExpenseService>();
        builder.Services.AddSingleton<ExecutiveReportPdfEngine>();
        builder.Services.AddSingleton<NfcTagService>();
        builder.Services.AddSingleton<UhfRfidService>();
        builder.Services.AddScoped<StockAuditService>();
        builder.Services.AddSingleton<UnifiedWirelessManager>();

        var app = builder.Build();

        app.UseCors("AllowLocalDesktop");
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Seed DB
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            await DbInitializer.InitializeAsync(context);
        }

        app.MapSasanEndpoints();
        app.MapFallbackToFile("index.html");

        await app.StartAsync();

        lock (_lock)
        {
            _app = app;
        }

        return app;
    }

    public static async Task StopAsync()
    {
        WebApplication? appToStop = null;
        lock (_lock)
        {
            appToStop = _app;
            _app = null;
        }

        if (appToStop != null)
        {
            await appToStop.StopAsync();
            await appToStop.DisposeAsync();
        }
    }
}
