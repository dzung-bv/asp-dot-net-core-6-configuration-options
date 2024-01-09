using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TennisBookings.Pages;

public class IndexModel : PageModel
{
	private const string DefaultForecastSectionTitle = "How's the weather";

	private readonly ILogger<IndexModel> _logger;
	private readonly IGreetingService _greetingService;
	private readonly IConfiguration _configuration;
	private readonly IWeatherForecaster _weatherForecaster;

	public IndexModel(
		ILogger<IndexModel> logger,		
		IGreetingService greetingService,
		IConfiguration configuration,
		IWeatherForecaster weatherForecaster)
	{
		_logger = logger;
		_greetingService = greetingService;
		_configuration = configuration;
		_weatherForecaster = weatherForecaster;
	}

	public string WeatherDescription { get; private set; } =
			"We don't have the latest weather information right now, " +
			"please check again later.";

	public bool ShowWeatherForecast { get; private set; } = false;
	public string ForecastSectionTitle { get; private set; } =
		DefaultForecastSectionTitle;
	public bool ShowGreeting => !string.IsNullOrEmpty(Greeting);
	public string Greeting { get; private set; } = string.Empty;
	public string GreetingColour => _greetingService.GreetingColour;

	public async Task OnGet()
	{
		var features = new Features();
		_configuration.Bind("Features:HomePage", features);

		if (features.EnableGreeting)
		{
			Greeting = _greetingService.GetRandomGreeting();
		}

		ShowWeatherForecast = features.EnableWeatherForecast
			&& _weatherForecaster.ForecastEnabled;

		if (ShowWeatherForecast)
		{
			var title = features.ForecastSectionTitle;
			ForecastSectionTitle = string.IsNullOrEmpty(title)
				? DefaultForecastSectionTitle : title;

			var currentWeather = await _weatherForecaster
				.GetCurrentWeatherAsync("Eastbourne");

			if (currentWeather is not null)
			{
				switch (currentWeather.Weather.Summary)
				{
					case "Sun":
						WeatherDescription = "It's sunny right now. " +
							"A great day for tennis!";
						break;
					case "Cloud":
						WeatherDescription = "It's cloudy at the moment and " +
							"the outdoor courts are availale.";
						break;
					case "Rain":
						WeatherDescription = "We're sorry but it's raining here. " +
							"No outdoor courts are available.";
						break;
					case "Snow":
						WeatherDescription = "It's snowing!! Outdoor courts " +
							"will remain closed until the snow clears.";
						break;
				}
			}
		}
	}

	private class Features
	{
		public bool EnableGreeting { get; set; }
		public bool EnableWeatherForecast { get; set; }
		public string ForecastSectionTitle { get; set; } = string.Empty;
	}
}
