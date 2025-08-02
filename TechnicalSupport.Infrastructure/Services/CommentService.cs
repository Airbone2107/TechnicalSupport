using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CommentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<CommentDto?> GetCommentByIdAsync(int commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            return comment == null ? null : _mapper.Map<CommentDto>(comment);
        }

        public async Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentModel model, string currentUserId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return null; // Không tìm thấy bình luận
            }

            if (comment.UserId != currentUserId)
            {
                // Ném lỗi Unauthorized để Controller bắt
                throw new UnauthorizedAccessException("User is not authorized to update this comment.");
            }

            comment.Content = model.Content;
            await _context.SaveChangesAsync();

            // Tải lại thông tin User để trả về DTO đầy đủ
            await _context.Entry(comment).Reference(c => c.User).LoadAsync();

            return _mapper.Map<CommentDto>(comment);
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string currentUserId, bool isAdmin)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return false; // Không tìm thấy bình luận
            }

            // Chỉ người tạo ra bình luận hoặc Admin mới có quyền xóa
            if (comment.UserId != currentUserId && !isAdmin)
            {
                throw new UnauthorizedAccessException("User is not authorized to delete this comment.");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 