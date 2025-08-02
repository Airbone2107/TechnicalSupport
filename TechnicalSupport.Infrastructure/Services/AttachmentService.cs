using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttachmentService(ApplicationDbContext context, IFileStorageService fileStorageService, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<List<AttachmentDto>> UploadAttachmentsForTicketAsync(int ticketId, string userId, IEnumerable<FileContentDto> files)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
            }

            // Kiểm tra quyền: chỉ customer của ticket hoặc technician/admin mới được upload
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = ticket.CustomerId == userId || roles.Contains("Technician") || roles.Contains("Admin");

            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("User is not authorized to upload attachments for this ticket.");
            }

            var uploadedAttachments = new List<Attachment>();

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

        public async Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId, string currentUserId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
            }
            
            var user = await _userManager.FindByIdAsync(currentUserId);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = ticket.CustomerId == currentUserId || ticket.AssigneeId == currentUserId || roles.Contains("Technician") || roles.Contains("Admin");

            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("You are not authorized to view attachments for this ticket.");
            }

            return await _context.Attachments
                .Where(a => a.TicketId == ticketId)
                .ProjectTo<AttachmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<FileDownloadDto?> GetAttachmentFileAsync(int attachmentId, string currentUserId)
        {
            var attachment = await _context.Attachments
                .Include(a => a.Ticket)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

            if (attachment == null) return null;

            var user = await _userManager.FindByIdAsync(currentUserId);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = attachment.Ticket.CustomerId == currentUserId || attachment.Ticket.AssigneeId == currentUserId || roles.Contains("Technician") || roles.Contains("Admin");

            if (!isAuthorized)
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

        public async Task<bool> DeleteAttachmentAsync(int attachmentId, string currentUserId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null) return false;

            var user = await _userManager.FindByIdAsync(currentUserId);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = attachment.UploadedById == currentUserId || roles.Contains("Admin");

            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this attachment.");
            }
            
            // Xóa file vật lý trước
            _fileStorageService.DeleteFile(attachment.StoredPath);

            // Sau đó xóa record trong DB
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 