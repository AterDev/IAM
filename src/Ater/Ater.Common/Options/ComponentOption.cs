namespace Ater.Common.Options;

/// <summary>
/// 组件配置
/// </summary>
public class ComponentOption
{
    public const string ConfigPath = "Components";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CacheType Cache { get; set; } = CacheType.Memory;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DatabaseType Database { get; set; } = DatabaseType.PostgreSql;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthType AuthType { get; set; } = AuthType.Jwt;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MQType MQType { get; set; } = MQType.None;
    public bool UseCors { get; set; } = true;
    public bool UseSMS { get; set; }
    public bool UseSmtp { get; set; }
    public bool UseAWSS3 { get; set; }
    public bool UseOpenAPI { get; set; } = true;
}

/// <summary>
/// 认证类型
/// </summary>
public enum AuthType
{
    /// <summary>
    /// JWT认证
    /// </summary>
    [Description("JWT")]
    Jwt,
    /// <summary>
    /// Cookie认证
    /// </summary>
    [Description("Cookie")]
    Cookie,
    /// <summary>
    /// OAuth认证
    /// </summary>
    [Description("OAuth")]
    OAuth,
}

/// <summary>
/// 数据库类型
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQL Server数据库
    /// </summary>
    [Description("SQL Server")]
    SqlServer,
    /// <summary>
    /// PostgreSQL数据库
    /// </summary>
    [Description("PostgreSQL")]
    PostgreSql,
}

/// <summary>
/// 缓存类型
/// </summary>
public enum CacheType
{
    /// <summary>
    /// 内存缓存
    /// </summary>
    [Description("内存")]
    Memory,
    /// <summary>
    /// Redis缓存
    /// </summary>
    [Description("Redis")]
    Redis,
    /// <summary>
    /// 混合缓存
    /// </summary>
    [Description("混合")]
    Hybrid,
}

/// <summary>
/// 消息队列类型
/// </summary>
public enum MQType
{
    /// <summary>
    /// 不使用消息队列
    /// </summary>
    [Description("无")]
    None,
    /// <summary>
    /// RabbitMQ消息队列
    /// </summary>
    [Description("RabbitMQ")]
    RabbitMQ,
    /// <summary>
    /// Kafka消息队列
    /// </summary>
    [Description("Kafka")]
    Kafka,
}
