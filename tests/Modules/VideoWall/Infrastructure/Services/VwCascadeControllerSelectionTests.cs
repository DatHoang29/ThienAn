using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Controllers.Scene.Validators;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System.Reflection;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Kiểm thử logic lựa chọn bộ điều khiển trung tâm (Cascade Controller Selection).
    /// Created date: 08/09/2026
    /// </summary>
    [Collection("api")]
    public class VwCascadeControllerSelectionTests(Host host)
    {
        private readonly IVwISAPIDeviceService _service = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseRepository<VwController> _controllerRep = host.Services.GetRequiredService<BaseRepository<VwController>>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        /// <summary>
        /// Description: B1 - Khi có cấu hình Cascade (1 center, n sub), ResolveTargetControllers luôn trả về duy nhất bộ điều khiển trung tâm cho kịch bản toàn tường.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B1_ResolveTargetControllers_WithCascadeConfiguration_ReturnsOnlyCenterController()
        {
            // Arrange
            await ClearDatabase();
            
            var center = new VwController
            {
                ID = $"center-{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = "192.168.1.10"
            };
            await _db.Insertable(center).ExecuteCommandAsync();
            
            for (int i = 0; i < 3; i++)
            {
                var sub = new VwController
                {
                    ID = $"sub-{Guid.NewGuid():N}",
                    Role = "sub",
                    IntegrationMode = "inventory",
                    IP = $"192.168.1.1{i + 1}"
                };
                await _db.Insertable(sub).ExecuteCommandAsync();
            }

            var scene = new VwScene 
            { 
                ID = $"scene-{Guid.NewGuid():N}" 
            };

            // Act
            var result = await InvokeResolveTargetControllers(scene);

            // Assert
            Assert.Single(result);
            Assert.Equal(center.ID, result[0].ID);
        }

        /// <summary>
        /// Description: B2 - Khi thiết lập kịch bản cho vùng (ControllerId được gán cho một bộ điều khiển con), hệ thống vẫn chỉ trả về bộ điều khiển trung tâm.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B2_ResolveTargetControllers_ZoneScene_ReturnsOnlyCenterController()
        {
            // Arrange
            await ClearDatabase();
            
            var center = new VwController
            {
                ID = $"center-{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = "192.168.1.10"
            };
            await _db.Insertable(center).ExecuteCommandAsync();
            
            var sub = new VwController
            {
                ID = $"sub-{Guid.NewGuid():N}",
                Role = "sub",
                IntegrationMode = "inventory",
                IP = "192.168.1.11"
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            var zoneScene = new VwScene 
            { 
                ID = $"scene-{Guid.NewGuid():N}",
                ControllerId = sub.ID
            };

            // Act
            var result = await InvokeResolveTargetControllers(zoneScene);

            // Assert
            Assert.Single(result);
            Assert.Equal(center.ID, result[0].ID);
        }

        /// <summary>
        /// Description: B3 - VwSceneValidator từ chối VwAddSceneInput nếu ControllerId trỏ đến một bộ điều khiển có Role="sub".
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B3_VwSceneValidator_WhenControllerIdPointsToSubController_Rejects()
        {
            // Arrange
            await ClearDatabase();
            
            var sub = new VwController
            {
                ID = $"sub-{Guid.NewGuid():N}",
                Role = "sub",
                IntegrationMode = "inventory",
                IP = "192.168.1.11"
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            var validator = new VwSceneValidator(_localizer, true, _controllerRep);
            
            var input = new VwAddSceneInput 
            { 
                Code = "SCENE_01",
                Name = "Scene 01",
                Status = BaseEnums.StatusEnum.Enable,
                ControllerId = sub.ID 
            };

            // Act
            var result = await validator.ValidateAsync(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("không được trỏ bộ điều khiển con"));
        }

        /// <summary>
        /// Description: B4 - GetCenterController ném ngoại lệ chứa thông báo Oops nếu không tìm thấy bộ điều khiển trung tâm nào.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B4_GetCenterController_WhenNoActiveControllerConfigured_ThrowsOops()
        {
            // Arrange
            await ClearDatabase();
            
            var sub = new VwController
            {
                ID = $"sub-{Guid.NewGuid():N}",
                Role = "sub",
                IntegrationMode = "inventory",
                IP = "192.168.1.11"
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => _service.GetCenterController());
            
            Assert.Contains("Chưa cấu hình bộ điều khiển trung tâm", exception.Message);
        }

        /// <summary>
        /// Description: B5 - GetCenterController ném ngoại lệ chứa thông báo Oops nếu có nhiều hơn 1 bộ điều khiển trung tâm.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B5_GetCenterController_WhenMultipleActiveControllersConfigured_ThrowsOops()
        {
            // Arrange
            await ClearDatabase();
            
            for (int i = 0; i < 2; i++)
            {
                var center = new VwController
                {
                    ID = $"center-{Guid.NewGuid():N}",
                    Role = "center",
                    IntegrationMode = "active",
                    IP = $"192.168.1.1{i}"
                };
                await _db.Insertable(center).ExecuteCommandAsync();
            }

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => _service.GetCenterController());
            
            Assert.Contains("Có >1 bộ điều khiển trung tâm", exception.Message);
        }

        /// <summary>
        /// Description: B6 - Trong môi trường CSDL trống không có bản ghi, GetCenterController vẫn ném ngoại lệ Oops rõ ràng mà không phải NullReferenceException.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B6_GetCenterController_WhenDatabaseEmpty_ThrowsOopsWithoutNullReference()
        {
            // Arrange
            await ClearDatabase();

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => _service.GetCenterController());
            
            Assert.Contains("Chưa cấu hình bộ điều khiển trung tâm", exception.Message);
            Assert.IsNotType<NullReferenceException>(exception);
        }

        /// <summary>
        /// Description: Xóa toàn bộ VwController trong CSDL để đảm bảo môi trường test trống.
        /// Created date: 08/09/2026
        /// </summary>
        private async Task ClearDatabase()
        {
            await _db.Deleteable<VwController>().ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Gọi method private ResolveTargetControllers bằng Reflection.
        /// Created date: 08/09/2026
        /// </summary>
        private async Task<List<VwController>> InvokeResolveTargetControllers(VwScene scene)
        {
            var method = typeof(VwISAPIDeviceService).GetMethod("ResolveTargetControllers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                throw new Exception("Method ResolveTargetControllers not found");
                
            var task = (Task<List<VwController>>)method.Invoke(_service, new object[] { scene })!;
            return await task;
        }
    }
}
