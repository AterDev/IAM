using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SampleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取天气预报 - 需要认证
    /// </summary>
    /// <remarks>
    /// 此端点需要有效的JWT令牌。
    /// 令牌应通过Authorization头发送: Bearer {token}
    /// </remarks>
    /// <response code="200">成功返回天气预报和用户信息</response>
    /// <response code="401">未认证 - 缺少或无效的令牌</response>
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
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        // 从JWT令牌中提取用户信息
        var userInfo = new
        {
            Username = User.Identity?.Name,
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            UserId = User.FindFirst("sub")?.Value,
            Email = User.FindFirst("email")?.Value,
            // 列出所有声明用于调试
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        };

        _logger.LogInformation("Weather forecast requested by user: {UserId}", userInfo.UserId);

        return Ok(new 
        { 
            Message = "成功获取天气预报",
            UserInfo = userInfo, 
            Forecast = forecast,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取特定日期的天气预报 - 需要认证
    /// </summary>
    /// <param name="days">未来天数 (1-30)</param>
    /// <response code="200">成功返回指定天数的天气预报</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="401">未认证</response>
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
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        return Ok(new
        {
            Message = $"未来{days}天的天气预报",
            RequestedBy = User.Identity?.Name,
            Forecast = forecast
        });
    }
}

/// <summary>
/// 天气预报数据模型
/// </summary>
public record WeatherForecast
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 摄氏温度
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// 华氏温度
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// 天气描述
    /// </summary>
    public string? Summary { get; set; }
}
