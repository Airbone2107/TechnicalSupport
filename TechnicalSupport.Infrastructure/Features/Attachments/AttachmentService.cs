using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechnicalSupport.Application.Authorization;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Features.Attachments
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttachmentService(
            ApplicationDbContext context, 
            IFileStorageService fileStorageService, 
            IMapper mapper, 
            UserManager<ApplicationUser> userManager,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext.User;
        private string GetCurrentUserId() => _userManager.GetUserId(GetCurrentUser());

        public async Task<List<AttachmentDto>> UploadAttachmentsForTicketAsync(int ticketId, IEnumerable<FileContentDto> files)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
            }
            
            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), ticket, TicketOperations.UploadFile);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("User is not authorized to upload attachments for this ticket.");
            }

            var uploadedAttachments = new List<Attachment>();
            var userId = GetCurrentUserId();

            foreach (var file in files)
            {
                var storedPath = await _fileStorageService.SaveFileAsync(file, ticketId.ToString());

                var attachment = new Attachment
                {
                    TicketId = ticketId,
                    UploadedById = userId,
                    OriginalFileName = file.FileName,
                    StoredPath = storedPath,
                    FileType = file.ContentType,
                    UploadedAt = DateTime.UtcNow
                };

                _context.Attachments.Add(attachment);
                uploadedAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync();

            foreach (var att in uploadedAttachments)
            {
                await _context.Entry(att).Reference(a => a.UploadedBy).LoadAsync();
            }

            return _mapper.Map<List<AttachmentDto>>(uploadedAttachments);
        }

        public async Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
            }
            
            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), ticket, TicketOperations.Read);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("You are not authorized to view attachments for this ticket.");
            }

            return await _context.Attachments
                .Where(a => a.TicketId == ticketId)
                .ProjectTo<AttachmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<FileDownloadDto?> GetAttachmentFileAsync(int attachmentId)
        {
            var attachment = await _context.Attachments
                .Include(a => a.Ticket)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

            if (attachment == null) return null;
            
            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), attachment.Ticket, TicketOperations.Read);
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("You are not authorized to download this attachment.");
            }

            var fileData = await _fileStorageService.GetFileAsync(attachment.StoredPath);
            if (fileData == null) return null;

            return new FileDownloadDto
            {
                Content = fileData.Value.Content,
                ContentType = attachment.FileType ?? "application/octet-stream",
                FileName = attachment.OriginalFileName
            };
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.Attachments
                .Include(a => a.Ticket)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
                
            if (attachment == null) return false;

            var authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), attachment.Ticket, TicketOperations.Update);
            // Hoặc tạo một operation riêng cho xóa file, tạm dùng Update
            if (!authResult.Succeeded)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this attachment.");
            }
            
            _fileStorageService.DeleteFile(attachment.StoredPath);

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 