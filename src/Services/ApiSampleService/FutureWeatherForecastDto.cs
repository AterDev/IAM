namespace ApiSampleService;

internal sealed record FutureWeatherForecastDto(string Message, string? RequestedBy, WeatherForecast[] Forecast);
