#region Global Usings
global using Microsoft.AspNetCore.Identity;

global using TennisBookings;
global using TennisBookings.Data;
global using TennisBookings.Domain;
global using TennisBookings.Extensions;
global using TennisBookings.Configuration;
global using TennisBookings.Caching;
global using TennisBookings.Shared.Weather;
global using TennisBookings.DependencyInjection;
global using TennisBookings.Services.Bookings;
global using TennisBookings.Services.Greetings;
global using TennisBookings.Services.Unavailability;
global using TennisBookings.Services.Bookings.Rules;
global using TennisBookings.Services.Notifications;
global using TennisBookings.Services.Time;
global using TennisBookings.Services.Staff;
global using TennisBookings.Services.Courts;
global using TennisBookings.Services.Security;
global using Microsoft.EntityFrameworkCore;
#endregion

using Microsoft.Data.Sqlite;
using TennisBookings.BackgroundService;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Azure.Identity;
using System.Reflection;
using TennisBookings.Configuration.Custom;

var builder = WebApplication.CreateBuilder(args);

using var connection = new SqliteConnection(builder.Configuration
	.GetConnectionString("SqliteConnection"));

await connection.OpenAsync();

builder.Host.ConfigureAppConfiguration((ctx, configBuilder) =>
{
	configBuilder.Sources.Clear();

	var env = ctx.HostingEnvironment;

	configBuilder.AddEnvironmentVariables("ASPNETCORE_");

	configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

	if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
	{
		var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
		if (appAssembly is not null)
		{
			configBuilder.AddUserSecrets(appAssembly, optional: true);
		}
	}

	//if (builder.Environment.IsProduction())
	//{
	configBuilder.AddAzureKeyVault(
			new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
			new DefaultAzureCredential());

	configBuilder.AddSystemsManager("/tennisBookings");
	//}

	configBuilder.AddEnvironmentVariables();

	configBuilder.AddEfConfiguration(o => o.UseSqlite(connection));
});

//builder.Services.Configure<HomePageConfiguration>(builder.Configuration.
//	GetSection("Features:HomePage"));

builder.Services.AddOptions<HomePageConfiguration>()
	.Bind(builder.Configuration.GetSection("Features:HomePage"))
	//.Validate(c =>
	//{
	//	return !c.EnableWeatherForecast || !string.IsNullOrEmpty(c.ForecastSectionTitle);
	//}, "A section title must be provided when the homepage weather forecast is enabled.")
	.ValidateOnStart();

builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<HomePageConfiguration>,
		HomePageConfigurationValidation>());
builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<ExternalServicesConfiguration>,
		ExternalServicesConfigurationValidation>());

builder.Services.Configure<GreetingConfiguration>(builder.Configuration.
	GetSection("Features:Greeting"));

//builder.Services.Configure<ExternalServicesConfiguration>(
//	ExternalServicesConfiguration.WeatherApi,
//	builder.Configuration.GetSection("ExternalServices:WeatherApi"));
//builder.Services.Configure<ExternalServicesConfiguration>(
//	ExternalServicesConfiguration.ProductsApi,
//	builder.Configuration.GetSection("ExternalServices:ProductsApi"));

builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.WeatherApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:WeatherApi"))
	.ValidateOnStart();
builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.ProductsApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:ProductsApi"))
	.ValidateOnStart();

builder.Services
	.AddAppConfiguration(builder.Configuration)
	.AddBookingServices()
	.AddBookingRules()
	.AddCourtUnavailability()
	.AddMembershipServices()
	.AddStaffServices()
	.AddCourtServices()
	.AddWeatherForecasting(builder.Configuration)
	.AddProducts()
	.AddNotifications()
	.AddGreetings()
	.AddCaching()
	.AddTimeServices()
	.AddProfanityValidationService()
	.AddAuditing();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
	options.Conventions.AuthorizePage("/Bookings");
	options.Conventions.AuthorizePage("/BookCourt");
	options.Conventions.AuthorizePage("/FindAvailableCourts");
	options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
});

// Add services to the container.
builder.Services.AddDbContext<TennisBookingsDbContext>(options =>
	options.UseSqlite(connection));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<TennisBookingsUser, TennisBookingsRole>(options => options.SignIn.RequireConfirmedAccount = false)
	.AddEntityFrameworkStores<TennisBookingsDbContext>()
	.AddDefaultUI()
	.AddDefaultTokenProviders();

builder.Services.AddHostedService<InitialiseDatabaseService>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.AccessDeniedPath = "/identity/account/access-denied";
});

if (builder.Environment.IsDevelopment())
{
	var debugView = builder.Configuration.GetDebugView();
	Console.WriteLine(debugView);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
