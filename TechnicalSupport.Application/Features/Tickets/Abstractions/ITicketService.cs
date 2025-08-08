using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Abstractions
{
    /// <summary>
    /// Định nghĩa giao diện cho các dịch vụ liên quan đến quản lý ticket.
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        /// Lấy danh sách ticket phân trang dựa trên các tiêu chí lọc.
        /// </summary>
        /// <param name="filterParams">Các tham số lọc và phân trang.</param>
        /// <returns>Kết quả phân trang chứa danh sách ticket.</returns>
        Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams);

        /// <summary>
        /// Lấy thông tin chi tiết một ticket theo ID.
        /// </summary>
        /// <param name="id">ID của ticket.</param>
        /// <returns>DTO của ticket hoặc null nếu không tìm thấy.</returns>
        Task<TicketDto?> GetTicketByIdAsync(int id);

        /// <summary>
        /// Tạo một ticket mới.
        /// </summary>
        /// <param name="model">Dữ liệu để tạo ticket.</param>
        /// <returns>DTO của ticket vừa được tạo.</returns>
        Task<TicketDto> CreateTicketAsync(CreateTicketModel model);

        /// <summary>
        /// Cập nhật trạng thái của ticket.
        /// </summary>
        /// <param name="id">ID của ticket.</param>
        /// <param name="model">Dữ liệu cập nhật trạng thái.</param>
        /// <returns>DTO của ticket sau khi cập nhật hoặc null nếu không tìm thấy.</returns>
        Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model);

        /// <summary>
        /// Thêm một bình luận vào ticket.
        /// </summary>
        /// <param name="ticketId">ID của ticket.</param>
        /// <param name="model">Dữ liệu bình luận.</param>
        /// <returns>DTO của bình luận vừa được thêm hoặc null nếu ticket không tồn tại.</returns>
        Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model);

        /// <summary>
        /// Gán ticket cho một agent cụ thể.
        /// </summary>
        /// <param name="ticketId">ID của ticket.</param>
        /// <param name="model">Dữ liệu gán ticket.</param>
        /// <returns>DTO của ticket sau khi được gán.</returns>
        Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model);

        /// <summary>
        /// Gán ticket cho một nhóm hỗ trợ.
        /// </summary>
        /// <param name="ticketId">ID của ticket.</param>
        /// <param name="model">Dữ liệu gán nhóm.</param>
        /// <returns>DTO của ticket sau khi được gán.</returns>
        Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model);

        /// <summary>
        /// Xóa một ticket.
        /// </summary>
        /// <param name="ticketId">ID của ticket cần xóa.</param>
        /// <returns>Một tuple chứa trạng thái thành công và thông báo.</returns>
        Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId);
        
        /// <summary>
        /// Cho phép một agent tự nhận (claim) một ticket chưa được gán.
        /// </summary>
        /// <param name="ticketId">ID của ticket cần nhận.</param>
        /// <returns>DTO của ticket sau khi được nhận.</returns>
        Task<TicketDto> ClaimTicketAsync(int ticketId);

        /// <summary>
        /// Đẩy một ticket ra khỏi nhóm hiện tại và trả về hàng đợi chung.
        /// </summary>
        /// <param name="ticketId">ID của ticket cần xử lý.</param>
        /// <returns>DTO của ticket sau khi được cập nhật.</returns>
        Task<TicketDto> RejectFromGroupAsync(int ticketId);
    }
} 