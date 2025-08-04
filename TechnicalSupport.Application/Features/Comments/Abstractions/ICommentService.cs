using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Comments.Abstractions
{
    public interface ICommentService
    {
        Task<CommentDto?> GetCommentByIdAsync(int commentId);
        Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentModel model);
        Task<bool> DeleteCommentAsync(int commentId);
    }
} 