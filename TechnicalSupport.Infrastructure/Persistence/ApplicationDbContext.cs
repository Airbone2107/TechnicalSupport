using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<TechnicianGroup> TechnicianGroups { get; set; }
        public DbSet<Status> Statuses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Ticket relationships
            builder.Entity<Ticket>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Ticket>()
                .HasOne(t => t.Group)
                .WithMany()
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Ticket>()
                .HasOne(t => t.Status)
                .WithMany()
                .HasForeignKey(t => t.StatusId);

            // Configure Comment relationships
            builder.Entity<Comment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comments) // Sửa đổi ở đây
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Attachment relationships
            builder.Entity<Attachment>()
                .HasOne(a => a.Ticket)
                .WithMany(t => t.Attachments) // Sửa đổi ở đây
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade); // Thay đổi thành Cascade để khi xóa ticket thì xóa attachment

            builder.Entity<Attachment>()
                .HasOne(a => a.Comment)
                .WithMany()
                .HasForeignKey(a => a.CommentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Attachment>()
                .HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TechnicianGroup composite key
            builder.Entity<TechnicianGroup>()
                .HasKey(tg => new { tg.UserId, tg.GroupId });

            builder.Entity<TechnicianGroup>()
                .HasOne(tg => tg.User)
                .WithMany()
                .HasForeignKey(tg => tg.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TechnicianGroup>()
                .HasOne(tg => tg.Group)
                .WithMany()
                .HasForeignKey(tg => tg.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique constraints
            builder.Entity<Group>()
                .HasIndex(g => g.Name)
                .IsUnique();

            builder.Entity<Status>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Configure check constraint for Priority
            builder.Entity<Ticket>()
                .Property(t => t.Priority)
                .HasConversion<string>()
                .HasDefaultValue("Medium")
                .IsRequired();
        }
    }
} 