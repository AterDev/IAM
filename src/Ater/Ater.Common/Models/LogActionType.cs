namespace Ater.Common.Models;

/// <summary>
/// 日志操作类型
/// </summary>
public enum LogActionType
{
    /// <summary>
    /// 无
    /// </summary>
    [Description("无")]
    None,
    /// <summary>
    /// 新增
    /// </summary>
    [Description("新增")]
    Add,
    /// <summary>
    /// 修改
    /// </summary>
    [Description("修改")]
    Update,
    /// <summary>
    /// 新增或修改
    /// </summary>
    [Description("新增或修改")]
    AddOrUpdate,
    /// <summary>
    /// 删除
    /// </summary>
    [Description("删除")]
    Delete,
    /// <summary>
    /// 全部
    /// </summary>
    [Description("全部")]
    All
}
