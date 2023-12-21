using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PluginBuilder.Authentication;
using PluginBuilder.HostedServices;
using PluginBuilder.Services;

namespace PluginBuilder;

public class Program
{
    public static Task Main(string[] args)
    {
        var host = new Program();
        return new Program().Start(args);
    }

    public Task Start(string[]? args = null)
    {
        WebApplication app = CreateWebApplication(args);
        return app.RunAsync();
    }

    public WebApplication CreateWebApplication(string[]? args = null)
    {
        WebApplicationBuilder builder = CreateWebApplicationBuilder(args);
        var app = builder.Build();
        Configure(app);
        return app;
    }

    public WebApplicationBuilder CreateWebApplicationBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        builder.Configuration.AddEnvironmentVariables("PB_");
#if DEBUG
        builder.Logging.AddFilter(typeof(ProcessRunner).FullName, LogLevel.Trace);
#endif
        builder.Logging.AddFilter("Microsoft", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Information);
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Critical);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Antiforgery.Internal", LogLevel.Critical);
        AddServices(builder.Configuration, builder.Services);
        return builder;
    }

    public void Configure(WebApplication app)
    {
        var forwardingOptions = new ForwardedHeadersOptions()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardingOptions.KnownNetworks.Clear();
        forwardingOptions.KnownProxies.Clear();
        forwardingOptions.ForwardedHeaders = ForwardedHeaders.All;
        app.UseForwardedHeaders(forwardingOptions);
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHub<Hubs.PluginHub>("hub");
        app.MapHub<Hubs.PluginHub>("/plugins/{pluginSlug}/hub");
        app.MapHub<Hubs.PluginHub>("/plugins/{pluginSlug}/builds/{buildId}/hub");
        //        app.MapControllerRoute(
        //name: "default",
        //pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapControllers();
    }

    public void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        services.AddControllersWithViews()
                .AddRazorRuntimeCompilation()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationFormats.Add("/{0}.cshtml");
                })
               .AddNewtonsoftJson(o => o.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented)
               .AddApplicationPart(typeof(Program).Assembly);
        if (configuration["DATADIR"] is string datadir)
        {
            services.AddDataProtection()
                     .SetApplicationName("Plugin Builder")
                    .PersistKeysToFileSystem(new DirectoryInfo(datadir));
        }
        services.AddHostedService<DatabaseStartupHostedService>();
        services.AddHostedService<DockerStartupHostedService>();
        services.AddHostedService<AzureStartupHostedService>();
        services.AddHostedService<PluginHubHostedService>();

        services.AddSingleton<DBConnectionFactory>();
        services.AddSingleton<BuildService>();
        services.AddSingleton<ProcessRunner>();
        services.AddSingleton<AzureStorageClient>();
        services.AddSingleton<ServerEnvironment>();
        services.AddSingleton<EventAggregator>();

        services.AddDbContext<IdentityDbContext<IdentityUser>>(b =>
        {
            b.UseNpgsql(configuration.GetRequired("POSTGRES"));
        });
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<IdentityDbContext<IdentityUser>>();

        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, opt =>
        {
            opt.LoginPath = "/login";
            opt.AccessDeniedPath = null;
            opt.LogoutPath = "/logout";
        });
        services.AddAuthentication()
            .AddScheme<PluginBuilderAuthenticationOptions, BasicAuthenticationHandler>(PluginBuilderAuthenticationSchemes.BasicAuth, o => {});
        services.AddAuthorization(o =>
        {
            o.AddPolicy(Policies.OwnPlugin, o => o.AddRequirements(new OwnPluginRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, PluginBuilderAuthorizationHandler>();
        services.AddSignalR();
    }
}
