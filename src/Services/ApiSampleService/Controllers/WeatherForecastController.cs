using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiSampleService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    /// <summary>
    /// 获取天气预报 - 需要认证
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
        }).ToArray();

        var userId = User.FindFirst("sub")?.Value;

        logger.LogInformation("Weather forecast requested by user: {UserId}", userId);

        return Ok(new
        {
            Message = "成功获取天气预报",
            RequestedBy = User.Identity?.Name ?? userId,
            Forecast = forecast,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// 获取特定日期的天气预报 - 需要认证
    /// </summary>
    [HttpGet("forecast/{days:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetForecast(int days)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { Error = "天数必须在1到30之间" });
        }

        var forecast = Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
        }).ToArray();

        return Ok(new
        {
            Message = $"未来{days}天的天气预报",
            RequestedBy = User.Identity?.Name,
            Forecast = forecast,
        });
    }
}

public record WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
