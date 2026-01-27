using DailyReflect.Components.Data;
using DailyReflect.Components.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace DailyReflect
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add MauiBlazorWebView
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Add MudBlazor services
            builder.Services.AddMudServices();

            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(roaming, "DailyReflect");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "DailyReflect.db");

         
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Register application services
            builder.Services.AddScoped<IJournalService, JournalService>();
            builder.Services.AddScoped<ITagCategoryService, TagCategoryService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPdfExportService, PdfExportService>();

    
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate(); // 👈 creates DB + tables + seeds
            }





            return app;
        }
    }
}