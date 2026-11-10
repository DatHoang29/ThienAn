using FluentValidation;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Kiểm thử cho ShareDataPartnerController và PartnerValidator qua CallApiAsync.
    ///              Xác nhận các lỗi cấu trúc và giá trị ConnectionConfig bị chặn với thông báo i18n tương ứng,
    ///              cấu hình hợp lệ lưu thành công, và kiểm tra tính hợp lệ của ConnectionType.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataPartnerControllerTests(Host host)
    {
        #region Kết nối và trạng thái đối tác

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo một đối tác kênh API chưa kết nối, trạng thái theo tham số.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<ShareDataPartner> SeedPartnerAsync(ISqlSugarClient db, BaseEnums.StatusEnum status = BaseEnums.StatusEnum.Enable)
        {
            var unique = Guid.NewGuid().ToString("N")[..20];
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"P_CN_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionType = ConnectionType.Api,
                Status = status,
                SessionState = BaseEnums.SessionState.Disconnected
            };
            await db.Insertable(partner).ExecuteCommandAsync();
            return partner;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo một đăng ký chiều gửi đang chạy của đối tác.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<ShareDataSubscription> SeedActiveSubscriptionAsync(ISqlSugarClient db, ShareDataPartner partner)
        {
            var subscription = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = $"DT_{Guid.NewGuid():N}",
                Direction = BaseEnums.Direction.Outbound,
                State = BaseEnums.SubSubscriptionState.Active
            };
            await db.Insertable(subscription).ExecuteCommandAsync();
            return subscription;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dữ liệu sửa đối tác giữ nguyên các giá trị đang có, ca test chỉ đổi trường cần kiểm.
        /// Created date: 05/11/2026
        /// </summary>
        private static ShareDataUpdatePartnerInput UpdateInput(ShareDataPartner partner) => new()
        {
            ID = partner.ID,
            Code = partner.Code,
            Name = partner.Name,
            Address = partner.Address,
            Port = partner.Port,
            ConnectionType = partner.ConnectionType,
            Status = partner.Status
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Kết nối mở một phiên và đánh dấu đối tác đã kết nối, kết nối lại không mở thêm phiên; ngắt kết nối đóng phiên kèm lý do, đánh dấu đối tác đã ngắt và tạm dừng đăng ký đang chạy.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ConnectPartner_WhenConnectedTwiceThenDisconnected_KeepsOneSessionAndPausesSubscriptions_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db);
            var subscription = await SeedActiveSubscriptionAsync(db, partner);

            // Act
            var first = await sp.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID });
            var second = await sp.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID });
            var whileConnected = await db.Queryable<ShareDataPartner>().InSingleAsync(partner.ID);
            await sp.CallApiAsync(new ShareDataDisconnectPartnerInput { ID = partner.ID });

            // Assert
            var sessions = await db.Queryable<ShareDataSession>().Where(s => s.PartnerId == partner.ID).ToListAsync();
            var session = Assert.Single(sessions);
            var afterDisconnect = await db.Queryable<ShareDataPartner>().InSingleAsync(partner.ID);
            var savedSubscription = await db.Queryable<ShareDataSubscription>().InSingleAsync(subscription.ID);
            Assert.Equal(first.ID, second.ID);
            Assert.Equal(BaseEnums.SessionState.Connected, whileConnected.SessionState);
            Assert.Equal(BaseEnums.SessionFsmState.Closed, session.State);
            Assert.Equal(ShareDataConst.Reason.ClientRequested, session.EndReason);
            Assert.Equal(BaseEnums.SessionState.Disconnected, afterDisconnect.SessionState);
            Assert.Equal(BaseEnums.SubSubscriptionState.Paused, savedSubscription.State);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác đã tắt thì không kết nối được và không mở phiên.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ConnectPartner_WhenPartnerDisabled_RejectsAndOpensNoSession_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db, BaseEnums.StatusEnum.Disable);

            // Act
            var error = await Record.ExceptionAsync(() => sp.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID }));

            // Assert
            var hasSession = await db.Queryable<ShareDataSession>().Where(s => s.PartnerId == partner.ID).AnyAsync();
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.PartnerDisabled);
            Assert.False(hasSession);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Sửa đối tác khi còn phiên kết nối: đổi tham số kết nối thì từ chối, chỉ đổi tên thì lưu được.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("doiDiaChi", true)]
        [InlineData("doiCong", true)]
        [InlineData("doiTen", false)]
        public async Task UpdatePartner_WhenSessionActive_LocksOnlyConnectionParams_Test(string change, bool rejected)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db);
            await sp.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID });
            var input = UpdateInput(partner);
            switch (change)
            {
                case "doiDiaChi": input.Address = "10.0.0.9"; break;
                case "doiCong": input.Port = 9090; break;
                default: input.Name = $"{partner.Name} moi"; break;
            }

            // Act
            var error = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().InSingleAsync(partner.ID);
            if (rejected)
            {
                RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.PartnerSessionActive);
                Assert.Equal(partner.Address, saved.Address);
                Assert.Equal(partner.Port, saved.Port);
            }
            else
            {
                Assert.Null(error);
                Assert.Equal(input.Name, saved.Name);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mốc gửi và gói tin nhận gắn theo mã đối tác nên API không cho đổi mã, kể cả khi gọi thẳng không qua màn hình; giữ nguyên mã (có khoảng trắng thừa) thì lưu được.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("_M", true)]
        [InlineData("", false)]
        public async Task UpdatePartner_WhenCodeChanged_RejectsAndKeepsCode_Test(string suffix, bool rejected)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db);
            var input = UpdateInput(partner);
            input.Code = $"  {partner.Code}{suffix}  ";
            input.Name = $"{partner.Name} moi";

            // Act
            var error = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().InSingleAsync(partner.ID);
            Assert.Equal(partner.Code, saved.Code);
            if (rejected)
            {
                RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.PartnerCodeLocked);
                Assert.Equal(partner.Name, saved.Name);
            }
            else
            {
                Assert.Null(error);
                Assert.Equal(input.Name, saved.Name);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tắt đối tác thì đăng ký đang chạy của đối tác đó tự tạm dừng, Worker không còn gửi theo đăng ký bị bỏ quên.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task UpdatePartner_WhenDisabled_PausesActiveSubscriptions_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db);
            var subscription = await SeedActiveSubscriptionAsync(db, partner);
            var input = UpdateInput(partner);
            input.Status = BaseEnums.StatusEnum.Disable;

            // Act
            await sp.CallApiAsync(input);

            // Assert
            var savedPartner = await db.Queryable<ShareDataPartner>().InSingleAsync(partner.ID);
            var savedSubscription = await db.Queryable<ShareDataSubscription>().InSingleAsync(subscription.ID);
            Assert.Equal(BaseEnums.StatusEnum.Disable, savedPartner.Status);
            Assert.Equal(BaseEnums.SubSubscriptionState.Paused, savedSubscription.State);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa đối tác còn đăng ký đang chạy thì từ chối và giữ nguyên (một bản ghi và cả lô); không còn đăng ký thì xóa mềm và đóng phiên đang mở.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task DeletePartner_WhenSubscriptionAliveOrNot_OnlyFreePartnerIsDeleted_Test(bool hasActiveSubscription, bool batch)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var partner = await SeedPartnerAsync(db);
            await sp.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID });
            if (hasActiveSubscription)
            {
                await SeedActiveSubscriptionAsync(db, partner);
            }

            // Act
            var error = await Record.ExceptionAsync(() => batch
                ? sp.CallApiAsync(new List<ShareDataDeletePartnerInput> { new() { ID = partner.ID } })
                : sp.CallApiAsync(new ShareDataDeletePartnerInput { ID = partner.ID }));

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().ClearFilter().Where(p => p.ID == partner.ID).FirstAsync();
            var session = await db.Queryable<ShareDataSession>().Where(s => s.PartnerId == partner.ID).FirstAsync();
            if (hasActiveSubscription)
            {
                RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.PartnerHasActiveSubscription);
                Assert.Null(saved.IsDelete);
                Assert.Equal(BaseEnums.SessionFsmState.Active, session.State);
            }
            else
            {
                Assert.Null(error);
                Assert.NotNull(saved.IsDelete);
                Assert.Equal(BaseEnums.SessionFsmState.Closed, session.State);
            }
        }

        #endregion

        #region 1. Validation qua AddPartnerValidator & Lưu qua IMessageBus

        /// <summary>
        /// Description: Kiểm chứng các trường hợp ConnectionConfig không hợp lệ bị validator chặn
        ///              với mã thông báo i18n tương ứng và không lưu bản ghi vào CSDL.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData("[1, 2, 3]", BaseMsg.ShareData.Validation.ConnectConfigNotJsonObject)]
        [InlineData("{ invalid_json: 123 }", BaseMsg.ShareData.Validation.ConnectConfigSyntaxError)]
        [InlineData("{\"\": \"some_value\"}", BaseMsg.ShareData.Validation.ConnectConfigEmptyKey)]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}", BaseMsg.ShareData.Validation.ConnectConfigReservedKey)]
        [InlineData("{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}", BaseMsg.ShareData.Validation.ConnectConfigDuplicateKey)]
        [InlineData("{\"Options\": {\"Key\": \"Value\"}}", BaseMsg.ShareData.Validation.ConnectConfigNestedValue)]
        [InlineData("{\"OutputPath\": \"/var/log/sharedata\"}", BaseMsg.ShareData.Validation.ConnectConfigObsoleteKey)]
        [InlineData("{\"TimeoutSeconds\": -1}", BaseMsg.ShareData.Validation.ConnectConfigInvalidTimeout)]
        [InlineData("{\"BypassSsl\": \"not_a_bool\"}", BaseMsg.ShareData.Validation.ConnectConfigInvalidBoolean)]
        [InlineData("{\"AuthType\": \"UnsupportedAuthMethod\"}", BaseMsg.ShareData.Validation.ConnectConfigUnsupportedAuthType)]
        [InlineData("{\"Encryptor\": \"Bzip2Unsupported\"}", BaseMsg.ShareData.Validation.ConnectConfigUnsupportedEncryptor)]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}", BaseMsg.ShareData.Validation.ConnectConfigFileNamePatternNoToken)]
        public async Task AddPartner_WhenConnectionConfigInvalid_ValidatorRejectsWithI18n_Test(string config, string expectedErrorKey)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = config
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[expectedErrorKey]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigValid_SavesSuccessfully_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"PARTNER_OK_{unique}",
                Name = $"Partner Valid Config {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionConfig = "{\"TimeoutSeconds\": 60, \"BypassSsl\": true, \"AuthType\": \"Bearer\", \"Encryptor\": \"None\", \"FileNamePattern\": \"data_{timestamp}_{serial}.json\"}"
            };

            // Act: Gọi CallApiAsync mô phỏng luồng API thật
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>()
                .Where(p => p.Code == input.Code)
                .FirstAsync();
            Assert.NotNull(saved);
            Assert.Equal(input.ConnectionConfig, saved.ConnectionConfig);
        }

        #endregion

        #region 2. PartnerValidator Unit Tests

        /// <summary>
        /// Description: Kiểm chứng ConnectionType hợp lệ (Api, Ftp) hoặc null (mặc định Api) lưu thành công vào CSDL.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData(ConnectionType.Api, ConnectionType.Api)]
        [InlineData(ConnectionType.Ftp, ConnectionType.Ftp)]
        [InlineData(null, ConnectionType.Api)]
        public async Task AddPartner_WhenConnectionTypeValidOrNull_SavesExpectedType_Test(ConnectionType? inputType, ConnectionType expectedType)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_TYPE_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = inputType
            };

            // Act
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().FirstAsync(p => p.Code == input.Code);
            Assert.NotNull(saved);
            Assert.Equal(expectedType, saved.ConnectionType);
        }

        /// <summary>
        /// Description: Kiểm chứng ConnectionType không hợp lệ (ngoài enum) bị validator từ chối.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Fact]
        public async Task AddPartner_WhenConnectionTypeInvalid_ValidatorRejects_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var code = $"P_INVALID_TYPE_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = (ConnectionType)999
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionType));

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        /// <summary>
        /// Description: Kênh tệp không cần địa chỉ, cổng nên thêm đối tác không nhập vẫn lưu; các kênh còn lại vẫn bắt buộc địa chỉ.
        /// Created date: 10/11/2026
        /// </summary>
        [Theory]
        [InlineData(ConnectionType.File, true)]
        [InlineData(ConnectionType.Api, false)]
        [InlineData(ConnectionType.Ftp, false)]
        [InlineData(ConnectionType.Kafka, false)]
        public async Task AddPartner_WhenAddressAndPortEmpty_OnlyFileChannelSaves_Test(ConnectionType connectionType, bool expectSaved)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var code = $"P_NOADDR_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                ConnectionType = connectionType
            };

            // Act
            var ex = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Equal(expectSaved, saved != null);
            if (expectSaved)
            {
                Assert.Null(ex);
                Assert.Null(saved!.Address);
                Assert.Null(saved.Port);
            }
            else
            {
                var validation = Assert.IsType<ValidationException>(ex);
                Assert.Contains(validation.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.Address));
            }
        }

        #endregion
    }
}

