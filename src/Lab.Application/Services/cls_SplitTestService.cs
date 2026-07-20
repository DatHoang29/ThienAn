/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Mapster;
using Lab.Application.DTOs;
using Lab.Domain.Entities;
using Lab.Domain.Repositories;

namespace Lab.Application.Services
{
    /// <summary>
    /// Description: Lớp dịch vụ xử lý nghiệp vụ chia bảng SplitTest (Application Service)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestService : Icls_SplitTestService
    {
        /// <summary>
        /// Description: Instance Repository truy cập cơ sở dữ liệu chia bảng
        /// </summary>
        private readonly Icls_SplitTestRepository vSplitRepository;

        /// <summary>
        /// Description: Hàm khởi tạo SplitTestService tiếp nhận Repository qua DI
        /// </summary>
        /// <param name="objSplitRepository">Instance Icls_SplitTestRepository được inject</param>
        public cls_SplitTestService(Icls_SplitTestRepository objSplitRepository)
        {
            vSplitRepository = objSplitRepository;
        }

        /// <summary>
        /// Description: Thêm dữ liệu vào bảng phân chia và tự động map sang DTO
        /// </summary>
        /// <param name="strName">Tên dữ liệu nhập vào</param>
        /// <returns>Tuple chứa DTO và tên bảng đã tạo</returns>
        public (cls_SplitTestDto objDto, string strTableName) fn_InsertSplitData(string strName)
        {
            var objNewRecord = new cls_SplitTestTable
            {
                vName = strName,
                vCreateTime = DateTime.Now
            };

            var longGeneratedId = vSplitRepository.fn_InsertSplitData(objNewRecord);
            objNewRecord.vId = longGeneratedId;

            var objDto = objNewRecord.Adapt<cls_SplitTestDto>();
            var strTableName = $"tbl_SplitTestTable_{objNewRecord.vCreateTime:yyyyMMdd}";

            return (objDto, strTableName);
        }

        /// <summary>
        /// Description: Lấy toàn bộ dữ liệu từ các bảng phân chia và map sang DTO
        /// </summary>
        /// <returns>Danh sách DTO</returns>
        public List<cls_SplitTestDto> fn_GetAllSplitData()
        {
            var lstEntities = vSplitRepository.fn_GetAllSplitData();
            return lstEntities.Adapt<List<cls_SplitTestDto>>();
        }

        /// <summary>
        /// Description: Lấy dữ liệu phân chia theo khoảng thời gian và map sang DTO
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách DTO</returns>
        public List<cls_SplitTestDto> fn_GetByTimeRange(DateTime dtBegin, DateTime dtEnd)
        {
            var lstEntities = vSplitRepository.fn_GetByTimeRange(dtBegin, dtEnd);
            return lstEntities.Adapt<List<cls_SplitTestDto>>();
        }

        /// <summary>
        /// Description: Lấy danh sách các bảng phân chia thực tế đã sinh ra trong DB
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        public List<string> fn_GetCreatedTables()
        {
            return vSplitRepository.fn_GetCreatedTables();
        }
    }
}
