using Modules.Fms.Core.Entities;

namespace Modules.FMS.Controllers.Topology.Dto;

public class TopologyOutput : FmsTopology
{
}

/// <summary>
/// Output 1 file config: tên file + nội dung topology đã parse
/// </summary>
public class TopologyConfigOutput
{
    public string FileName { get; set; }
    public NagiosTopologyOutput Content { get; set; }
}

/// <summary>
/// Cấu trúc tổng của file config Nagios
/// </summary>
public class NagiosTopologyOutput
{
    public string LastTimeSync { get; set; }
    public List<TopologyNodeOutput> Roots { get; set; } = new();
}

/// <summary>
/// 1 node thiết bị trong cây topology (đệ quy cha - con)
/// </summary>
public class TopologyNodeOutput
{
    public string Name { get; set; }
    public string Ip { get; set; }
    /// <summary>Tên thiết bị ITS khớp theo IP (null = thiết bị ngoài ITS). Name vẫn là tên trên CyberMonitor.</summary>
    public string? EquipmentName { get; set; }
    public int? EquipmentState { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public List<TopologyNodeOutput> Children { get; set; } = new();
}

/// <summary>
/// Cấu trúc file topology.json do dịch vụ DeviceMonitor ghi ra (danh sách node PHẲNG).
/// Dùng để deserialize trước khi dựng cây cha - con cho FE.
/// </summary>
public class TopologyFileParse
{
    public string LastTimeSync { get; set; }
    public string MapName { get; set; }
    public List<TopologyFileNode> Nodes { get; set; } = new();
}

/// <summary>
/// 1 node phẳng trong file topology.json (khớp cấu trúc dịch vụ ghi).
/// </summary>
public class TopologyFileNode
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DeviceType { get; set; }
    /// <summary>Id node cha (dịch vụ dựng từ links của API topology). null = node gốc. File cũ không có trường này.</summary>
    public string? ParentId { get; set; }
    public string Ip { get; set; }
    public string EquipmentId { get; set; }
    /// <summary>Tên thiết bị ITS dịch vụ ghi kèm (file cũ không có trường này).</summary>
    public string? EquipmentName { get; set; }
    /// <summary>Trạng thái thiết bị (map từ Zabbix) - dùng làm EquipmentState khi hiển thị.</summary>
    public int? Status { get; set; }
}
