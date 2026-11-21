using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using Newtonsoft.Json.Linq;

namespace Shared.Utility.Helpers
{
    public class ExcelHelper
    {
        /* Các hàm hỗ trợ xuất excel sử dụng thư viện Magicodes.IE
         * Vui lòng tham khảo thư viện về cách sử dụng
         */


        /// <summary>
        /// Xuất file excel với dữ liệu danh sách
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<string> ExportList<T>(List<T> data, string filePath) where T : class, new()
        {
            IExporter exporter = new ExcelExporter();
            var result = await exporter.Export(filePath, data);
            return result.FileName;
        }

        public static async Task<string> ExportByTemplate<T>(T data, string templateFile, string filePath) where T: class
        {

            //Creating Excel Export Objects
            IExportFileByTemplate exporter = new ExcelExporter();

            //Export from template
            var rs = await exporter.ExportByTemplate(filePath, data, templateFile);
            return rs.FileName;

        }
    }
}
