using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Dto.Packet;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData;

namespace Module.ShareData.Controllers.Packet.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update gói tin (ShareDataPacket)
    /// Created date: 2026-09-17
    /// </summary>
    public class PacketValidator : AbstractValidator<ShareDataAddPacketInput>
    {
        public PacketValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PacketVersion)
                .MaximumLength(EntityConst.Length16)
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Description)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Description])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới gói tin
    /// Created date: 2026-09-17
    /// </summary>
    public class AddPacketValidator : AbstractValidator<ShareDataAddPacketInput>
    {
        public AddPacketValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.OrderNo)
                .NotNull()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .InclusiveBetween(101, 111)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            Include(new PacketValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật gói tin
    /// Created date: 2026-09-17
    /// </summary>
    public class UpdatePacketValidator : AbstractValidator<ShareDataUpdatePacketInput>
    {
        public UpdatePacketValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new PacketValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa gói tin
    /// Created date: 2026-09-17
    /// </summary>
    public class DeletePacketValidator : AbstractValidator<ShareDataDeletePacketInput>
    {
        public DeletePacketValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    #region Câu SQL trích xuất (mẫu chung / SQL riêng đối tác), dò cột, trường

    /// <summary>
    /// Author: DatHQ
    /// Description: Luật dùng chung cho các input câu SQL trích xuất: mã gói tin, mã đối tác, câu SQL bắt buộc.
    ///              Thay cho [Required] trên DTO (rule: kiểm tra 100% qua FluentValidation).
    /// Created date: 24/10/2026
    /// </summary>
    internal static class PacketExtractSqlRules
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Luật dùng chung: mã gói tin bắt buộc nhập.
        /// Created date: 26/10/2026
        /// </summary>
        public static void PacketCodeRequired<T>(AbstractValidator<T> validator, IStringLocalizer localizer, System.Linq.Expressions.Expression<Func<T, string>> selector) =>
            validator.RuleFor(selector)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

        /// <summary>
        /// Author: DatHQ
        /// Description: Luật dùng chung: ID đối tác bắt buộc nhập.
        /// Created date: 26/10/2026
        /// </summary>
        public static void PartnerIdRequired<T>(AbstractValidator<T> validator, IStringLocalizer localizer, System.Linq.Expressions.Expression<Func<T, string>> selector) =>
            validator.RuleFor(selector)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

        /// <summary>
        /// Author: DatHQ
        /// Description: Luật dùng chung: câu SQL không được trống.
        /// Created date: 26/10/2026
        /// </summary>
        public static void ExtractSqlRequired<T>(AbstractValidator<T> validator, IStringLocalizer localizer, System.Linq.Expressions.Expression<Func<T, string>> selector) =>
            validator.RuleFor(selector)
                .NotEmpty()
                    .WithMessage(localizer[Msg.Validation.SqlScriptEmpty]);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy câu SQL trích xuất của gói tin
    /// Created date: 24/10/2026
    /// </summary>
    public class GetPacketExtractSqlValidator : AbstractValidator<GetPacketExtractSqlInput>
    {
        public GetPacketExtractSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator cập nhật câu SQL trích xuất của gói tin
    /// Created date: 24/10/2026
    /// </summary>
    public class UpdatePacketExtractSqlValidator : AbstractValidator<UpdatePacketExtractSqlInput>
    {
        public UpdatePacketExtractSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator dò cột trả về của câu SQL
    /// Created date: 24/10/2026
    /// </summary>
    public class ParsePacketColumnsValidator : AbstractValidator<ParsePacketColumnsInput>
    {
        public ParsePacketColumnsValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.ExtractSqlRequired(this, localizer, x => x.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator chạy thử câu SQL trích xuất
    /// Created date: 24/10/2026
    /// </summary>
    public class ExecutePacketSqlSampleValidator : AbstractValidator<ExecutePacketSqlSampleInput>
    {
        public ExecutePacketSqlSampleValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
            PacketExtractSqlRules.ExtractSqlRequired(this, localizer, x => x.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy danh sách đối tác có SQL riêng của gói tin
    /// Created date: 24/10/2026
    /// </summary>
    public class GetPartnerSqlListValidator : AbstractValidator<GetPartnerSqlListInput>
    {
        public GetPartnerSqlListValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy câu SQL riêng của đối tác
    /// Created date: 24/10/2026
    /// </summary>
    public class GetPartnerExtractSqlValidator : AbstractValidator<GetPartnerExtractSqlInput>
    {
        public GetPartnerExtractSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator cập nhật câu SQL riêng của đối tác (câu SQL rỗng do handler báo lỗi riêng PartnerExtractSqlRequired)
    /// Created date: 24/10/2026
    /// </summary>
    public class UpdatePartnerExtractSqlValidator : AbstractValidator<UpdatePartnerExtractSqlInput>
    {
        public UpdatePartnerExtractSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator đặt lại câu SQL riêng của đối tác về mẫu chung
    /// Created date: 24/10/2026
    /// </summary>
    public class ResetPartnerExtractSqlValidator : AbstractValidator<ResetPartnerExtractSqlInput>
    {
        public ResetPartnerExtractSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator dò cột trả về của câu SQL đang soạn (thay cho [Required] trên DescribeColumnsInput)
    /// Created date: 27/10/2026
    /// </summary>
    public class DescribeColumnsValidator : AbstractValidator<DescribeColumnsInput>
    {
        public DescribeColumnsValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
            PacketExtractSqlRules.ExtractSqlRequired(this, localizer, x => x.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy danh sách trường của gói tin cho màn ánh xạ (thay cho [Required] trên GetPacketFieldsInput)
    /// Created date: 27/10/2026
    /// </summary>
    public class GetPacketFieldsValidator : AbstractValidator<GetPacketFieldsInput>
    {
        public GetPacketFieldsValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.DatatypeId);
        }
    }

    #endregion

    #region Tham số câu truy vấn và mốc đã gửi

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy bảng tham số câu truy vấn của gói tin
    /// Created date: 24/10/2026
    /// </summary>
    public class GetSqlVariablesValidator : AbstractValidator<GetPacketSqlVariablesInput>
    {
        public GetSqlVariablesValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lưu bảng tham số câu truy vấn của gói tin. Kiểm tra từng dòng (tên, kiểu, nguồn, giá trị) nằm ở handler
    ///              vì cần đối chiếu với biến của mẫu chung trong CSDL.
    /// Created date: 24/10/2026
    /// </summary>
    public class UpdateSqlVariablesValidator : AbstractValidator<UpdatePacketSqlVariablesInput>
    {
        public UpdateSqlVariablesValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Items)
                .NotNull()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator đặt lại mốc đã gửi (ShareDataLastSend) của gói tin cho đối tác
    /// Created date: 27/10/2026
    /// </summary>
    public class ResetLastSendValidator : AbstractValidator<ResetLastSendInput>
    {
        public ResetLastSendValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PartnerId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator đặt mốc nối đuôi (ShareDataLastSend) của gói tin cho một đối tác; định dạng mốc thời gian kiểm ở handler.
    /// Created date: 29/10/2026
    /// </summary>
    public class SetLastSendValidator : AbstractValidator<SetLastSendInput>
    {
        public SetLastSendValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PartnerId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.LastKey)
                .MaximumLength(EntityConst.Length256)
                    .WithName(nameof(SetLastSendInput.LastKey))
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.CursorKey)
                .MaximumLength(EntityConst.Length64)
                    .WithName(nameof(SetLastSendInput.CursorKey))
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    #endregion

    #region Khối câu ghi chiều nhận

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lấy danh sách câu ghi chiều nhận của gói tin
    /// Created date: 26/10/2026
    /// </summary>
    public class GetWriteSqlsValidator : AbstractValidator<GetPacketWriteSqlsInput>
    {
        public GetWriteSqlsValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator lưu khối câu ghi chiều nhận. Kiểm an toàn khối câu nằm ở handler (cần CSDL nhận).
    /// Created date: 26/10/2026
    /// </summary>
    public class UpdateWriteSqlsValidator : AbstractValidator<UpdatePacketWriteSqlsInput>
    {
        public UpdateWriteSqlsValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleForEach(x => x.AffectedTables).ChildRules(table =>
            {
                table.RuleFor(t => t)
                    .MaximumLength(EntityConst.Length128)
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
            });
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator lấy danh sách đối tác có thể cấu hình câu ghi chiều nhận riêng
    /// Created date: 30/10/2026
    /// </summary>
    public class GetPartnerWriteSqlListValidator : AbstractValidator<GetPartnerWriteSqlListInput>
    {
        public GetPartnerWriteSqlListValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator lấy câu ghi chiều nhận riêng của đối tác
    /// Created date: 30/10/2026
    /// </summary>
    public class GetPartnerWriteSqlValidator : AbstractValidator<GetPartnerWriteSqlInput>
    {
        public GetPartnerWriteSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cập nhật câu ghi chiều nhận riêng của đối tác (câu ghi rỗng do handler báo lỗi riêng PartnerWriteSqlRequired)
    /// Created date: 30/10/2026
    /// </summary>
    public class UpdatePartnerWriteSqlValidator : AbstractValidator<UpdatePartnerWriteSqlInput>
    {
        public UpdatePartnerWriteSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleForEach(x => x.AffectedTables).ChildRules(table =>
            {
                table.RuleFor(t => t)
                    .MaximumLength(EntityConst.Length128)
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
            });
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator đặt lại câu ghi chiều nhận của đối tác về mẫu chung
    /// Created date: 30/10/2026
    /// </summary>
    public class ResetPartnerWriteSqlValidator : AbstractValidator<ResetPartnerWriteSqlInput>
    {
        public ResetPartnerWriteSqlValidator(IStringLocalizer localizer)
        {
            PacketExtractSqlRules.PartnerIdRequired(this, localizer, x => x.PartnerId);
            PacketExtractSqlRules.PacketCodeRequired(this, localizer, x => x.PacketCode);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator kiểm an toàn một câu ghi chiều nhận
    /// Created date: 26/10/2026
    /// </summary>
    public class ValidateWriteSqlValidator : AbstractValidator<ValidateWriteSqlInput>
    {
        public ValidateWriteSqlValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Validator chạy thử câu ghi chiều nhận
    /// Created date: 26/10/2026
    /// </summary>
    public class TryWriteSqlValidator : AbstractValidator<TryWriteSqlInput>
    {
        public TryWriteSqlValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    #endregion
}
