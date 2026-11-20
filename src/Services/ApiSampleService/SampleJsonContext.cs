using System.Text.Json.Serialization;

namespace ApiSampleService;

[JsonSerializable(typeof(SampleMessageDto))]
[JsonSerializable(typeof(ProtectedMessageDto))]
[JsonSerializable(typeof(WeatherForecastDto))]
[JsonSerializable(typeof(FutureWeatherForecastDto))]
[JsonSerializable(typeof(WeatherForecastErrorDto))]
internal partial class SampleJsonContext : JsonSerializerContext;
