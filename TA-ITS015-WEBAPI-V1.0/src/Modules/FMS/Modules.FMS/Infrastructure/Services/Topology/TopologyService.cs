using Microsoft.AspNetCore.Hosting;
using Modules.FMS.Controllers.Topology.Dto;
using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Entities;
using System.Text.Json;
using Wolverine;

namespace Modules.FMS.Infrastructure.Services.Topology
{
    public class TopologyService : IScoped
    {
        private const string DeviceTypeServer = "Server";
        private const string FileSearchPattern = "*.json";

        private readonly IWebHostEnvironment _env;
        private readonly IMessageBus _messageBus;

        // Cho phép khớp key không phân biệt hoa/thường: "lasttimesync" -> LastTimeSync
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public TopologyService(IWebHostEnvironment env, IMessageBus messageBus)
        {
            _env = env;
            _messageBus = messageBus;
        }

        /// <summary>
        /// Đọc tất cả file JSON trong folder, parse ra cây topology, map thông tin thiết bị từ TMS và trả về
        /// Author: ChuongLP
        /// Date: 2026-07-13
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task<List<TopologyConfigOutput>> ReadConfigFilesAsync(string folderPath)
        {
            var result = new List<TopologyConfigOutput>();

            if (string.IsNullOrWhiteSpace(folderPath))
                return result;

            var directory = Path.IsPathRooted(folderPath)
                ? folderPath
                : Path.Combine(_env.WebRootPath, folderPath);

            if (!Directory.Exists(directory))
                return result;

            var files = Directory.GetFiles(directory, FileSearchPattern);
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);

                    // File dịch vụ ghi ra là danh sách node PHẲNG -> parse rồi dựng cây cha-con cho FE.
                    var parsed = JsonSerializer.Deserialize<TopologyFileParse>(content, _jsonOptions);
                    if (parsed != null)
                    {
                        result.Add(new TopologyConfigOutput
                        {
                            FileName = Path.GetFileName(file),
                            Content = new NagiosTopologyOutput
                            {
                                LastTimeSync = parsed.LastTimeSync,
                                Roots = BuildRoots(parsed.Nodes),
                            },
                        });
                    }
                }
                catch
                {
                    // Bỏ qua file JSON lỗi định dạng
                }
            }
            // Sau khi đã build xong result (các cây topology), enrich 1 lần:
            await MapEquipmentAsync(result);
            return result;
        }

        /// <summary>
        /// Dựng cây cha - con - cháu từ danh sách node phẳng của file topology.json.
        /// - File mới (có ParentId, dịch vụ dựng từ links của API topology): dựng cây nhiều tầng theo ParentId.
        /// - File cũ (không node nào có ParentId): node deviceType = "Server" làm gốc, các node còn lại gắn vào server đầu tiên;
        ///   không có server thì trả về danh sách phẳng (FE tự tạo 1 root ảo để bao ngoài).
        /// EquipmentState lấy trực tiếp từ Status trong file (trạng thái realtime từ Zabbix).
        /// Author: ChuongLP
        /// Date: 2026-09-07 (dựng theo ParentId: 2026-10-20)
        /// </summary>
        private static List<TopologyNodeOutput> BuildRoots(List<TopologyFileNode> nodes)
        {
            var roots = new List<TopologyNodeOutput>();
            if (nodes == null || nodes.Count == 0) return roots;

            static TopologyNodeOutput ToOut(TopologyFileNode n) => new()
            {
                Name = n.Name,
                Ip = n.Ip,
                EquipmentName = n.EquipmentName, // tên lúc dịch vụ ghi file; MapEquipment ghi đè bằng tên TMS hiện tại
                EquipmentState = n.Status,
            };

            if (nodes.Any(n => !string.IsNullOrWhiteSpace(n.ParentId)))
                return BuildRootsByParent(nodes, ToOut);

            var servers = nodes
                .Where(n => string.Equals(n.DeviceType, DeviceTypeServer, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var others = nodes
                .Where(n => !string.Equals(n.DeviceType, DeviceTypeServer, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (servers.Count > 0)
            {
                var serverOuts = servers.Select(ToOut).ToList();
                // Gắn toàn bộ node không phải server vào server đầu tiên.
                serverOuts[0].Children = others.Select(ToOut).ToList();
                roots.AddRange(serverOuts);
            }
            else
            {
                roots.AddRange(nodes.Select(ToOut));
            }

            return roots;
        }

        /// <summary>
        /// Dựng cây theo ParentId. Node có ParentId rỗng / trỏ tới node không tồn tại -> làm gốc; trùng Id -> giữ node đầu tiên.
        /// Chỉ duyệt từ gốc xuống nên dữ liệu lỗi có vòng (A cha B, B cha A) không gây lặp vô hạn;
        /// các node nằm trong vòng như vậy được đưa lên làm gốc để không bị mất khỏi sơ đồ.
        /// </summary>
        private static List<TopologyNodeOutput> BuildRootsByParent(
            List<TopologyFileNode> nodes, Func<TopologyFileNode, TopologyNodeOutput> toOut)
        {
            var outById = new Dictionary<string, TopologyNodeOutput>();
            var childrenOf = new Dictionary<string, List<TopologyFileNode>>();
            var rootNodes = new List<TopologyFileNode>();

            foreach (var n in nodes)
            {
                if (string.IsNullOrWhiteSpace(n.Id))
                {
                    rootNodes.Add(n); // không có Id -> không ai trỏ tới được, làm gốc
                    continue;
                }
                if (!outById.ContainsKey(n.Id)) outById[n.Id] = toOut(n); // trùng Id -> giữ node đầu tiên
            }

            var ids = new HashSet<string>(outById.Keys);
            foreach (var n in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).GroupBy(n => n.Id).Select(g => g.First()))
            {
                if (string.IsNullOrWhiteSpace(n.ParentId) || n.ParentId == n.Id || !ids.Contains(n.ParentId))
                {
                    rootNodes.Add(n);
                    continue;
                }

                if (!childrenOf.TryGetValue(n.ParentId, out var list)) childrenOf[n.ParentId] = list = new List<TopologyFileNode>();
                list.Add(n);
            }

            var visited = new HashSet<string>();
            var roots = new List<TopologyNodeOutput>();

            TopologyNodeOutput Attach(TopologyFileNode n)
            {
                var node = !string.IsNullOrWhiteSpace(n.Id) && outById.TryGetValue(n.Id, out var o) ? o : toOut(n);
                if (string.IsNullOrWhiteSpace(n.Id) || !visited.Add(n.Id)) return node;

                foreach (var child in childrenOf.GetValueOrDefault(n.Id) ?? new List<TopologyFileNode>())
                {
                    if (!visited.Contains(child.Id)) node.Children.Add(Attach(child));
                }
                return node;
            }

            foreach (var r in rootNodes) roots.Add(Attach(r));

            // Node chưa được thăm = nằm trong vòng cha-con lỗi -> đưa lên làm gốc.
            // (kiểm tra visited ngay trong vòng lặp: Attach 1 node trong vòng sẽ thăm luôn các node còn lại của vòng đó)
            foreach (var n in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).GroupBy(n => n.Id).Select(g => g.First()).ToList())
            {
                if (!visited.Contains(n.Id)) roots.Add(Attach(n));
            }

            return roots;
        }


        /// <summary>
        /// Xử lý làm sao map được thông tin thiết bị (KmNumber, MetNumber, EquipmentState) từ TMS về cây topology
        /// Author: ChuongLP
        /// Date: 2026-07-13
        /// </summary>
        private async Task MapEquipmentAsync(List<TopologyConfigOutput> configs)
        {
            if (configs.Count == 0) return;

            // Enrich Km/Met từ TMS chỉ là bước bổ sung. Nếu API TMS lỗi/timeout thì KHÔNG được
            // làm hỏng cả cây topology (đã đọc/dựng xong từ file) -> nuốt lỗi, trả cây như hiện có.
            try
            {
                // Lấy danh sách thiết bị IN-PROCESS qua MessBus (Wolverine) thay cho BaseApiClient (HTTP self-call).
                var equipList = await _messageBus.InvokeAsync<List<TmsEquipment>>(new GetAllEquipmentInput())
                                ?? new List<TmsEquipment>();

                // Map theo IP (bỏ IP rỗng/trùng; trim giống dịch vụ DeviceMonitor)
                var byIp = equipList
                    .Where(e => !string.IsNullOrWhiteSpace(e.Ip))
                    .GroupBy(e => e.Ip.Trim())
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var cfg in configs)
                    foreach (var root in cfg.Content?.Roots ?? new())
                        MapEquipment(root, byIp);
            }
            catch
            {
                // Không enrich được vị trí (Km/Met) -> vẫn hiển thị cấu trúc + trạng thái realtime.
            }
        }


        /// <summary>
        /// Xử lý đệ quy map thông tin thiết bị (KmNumber, MetNumber, EquipmentState) từ TMS về cây topology
        /// Author: ChuongLP
        /// Date: 2026-07-13
        /// </summary>
        private void MapEquipment(TopologyNodeOutput node, Dictionary<string, TmsEquipment> byIp)
        {
            if (!string.IsNullOrWhiteSpace(node.Ip) && byIp.TryGetValue(node.Ip.Trim(), out var eq))
            {
                // Enrich tên thiết bị ITS + vị trí (Km/Met) từ TMS; EquipmentState giữ theo Status realtime trong file.
                node.EquipmentName = eq.Name ?? node.EquipmentName;
                node.KmNumber = eq.KmNumber;
                node.MetNumber = eq.MetNumber;
            }

            foreach (var child in node.Children)
                MapEquipment(child, byIp);
        }
    }
}
