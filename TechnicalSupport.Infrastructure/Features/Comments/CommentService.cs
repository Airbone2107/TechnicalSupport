using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Application.Features.Comments.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Comments
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommentService(ApplicationDbContext context, IMapper mapper, IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }
        
        private System.Security.Claims.ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext.User;

        public async Task<CommentDto?> GetCommentByIdAsync(int commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
            
            // Logic kiểm tra quyền đọc comment có thể được thêm ở đây nếu cần
            
            return comment == null ? null : _mapper.Map<CommentDto>(comment);
        }

        public async Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentModel model)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return null;
            }

            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), comment, CommentOperations.Update);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to update this comment.");
            }

            comment.Content = model.Content;
            await _context.SaveChangesAsync();

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();

            return _mapper.Map<CommentDto>(comment);
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return false;
            }
            
            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), comment, CommentOperations.Delete);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to delete this comment.");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 