/*
 * Project này CỐ TÌNH không dùng global using của Furion / Wolverine / AspNetCore.
 *
 * Lý do: tầng Integration phải chạy được ở CẢ 2 nơi:
 *   - trong WebAPI (có runtime Furion)
 *   - trong dịch vụ ITS.VMS.Worker / ITS.VMS.Wpf (KHÔNG có Furion)
 * Nên toàn bộ code ở đây chỉ được dùng .NET cơ bản + tầng Modules.Integration.VMS.Base.
 *
 * Trước 22/09/2026 project có global using Furion.*, Autofac, Wolverine.Attributes và
 * [assembly: WolverineModule] (kế thừa từ khuôn mẫu module) — nhưng không có handler nào,
 * và chính các phụ thuộc này khiến dịch vụ không dùng lại được IntgVmsManager.
 *
 * Author: ChuongLP
 * Created date: 22/09/2026
 */

global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Data;
