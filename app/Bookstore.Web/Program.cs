using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bookstore.Data;
using Bookstore.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        DependencyInjectionSetup.ConfigureContainer(containerBuilder, builder.Configuration);
    });

    // builder.Logging.ClearProviders(); // Not needed with NLog
    builder.Host.UseNLog();

    LoggingSetup.ConfigureLogging();
    ConfigurationSetup.ConfigureConfiguration(builder.Configuration);

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    var connectionString = ConfigurationSetup.GetConnectionString(builder.Configuration);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    AuthenticationSetup.ConfigureAuthentication(builder.Services, builder.Configuration);

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseLocalAuthenticationMiddleware();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
