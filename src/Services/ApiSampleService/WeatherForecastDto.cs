namespace ApiSampleService;

internal sealed record WeatherForecastDto(string Message, string? RequestedBy, WeatherForecast[] Forecast, DateTime Timestamp);
