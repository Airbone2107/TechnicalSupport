using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<TechnicianGroup> TechnicianGroups { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<PermissionRequest> PermissionRequests { get; set; }
        public DbSet<TemporaryPermission> TemporaryPermissions { get; set; }
        public DbSet<ProblemType> ProblemTypes { get; set; } // Thêm DbSet mới

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TechnicianGroup>().HasKey(tg => new { tg.UserId, tg.GroupId });
            builder.Entity<Group>().HasIndex(g => g.Name).IsUnique();
            builder.Entity<Status>().HasIndex(s => s.Name).IsUnique();
            builder.Entity<TemporaryPermission>().HasIndex(tp => new { tp.UserId, tp.ClaimType, tp.ClaimValue });
            builder.Entity<ProblemType>().HasIndex(p => p.Name).IsUnique(); // Thêm index cho ProblemType

            // Mặc định: Không xóa xếp tầng từ ApplicationUser. Việc xóa người dùng phải do AdminService xử lý.
            foreach (var fk in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).Where(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser)))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
            
            // Xóa Ticket sẽ xóa các Comment và Attachment liên quan
            builder.Entity<Ticket>()
                .HasMany(t => t.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Ticket>()
                .HasMany(t => t.Attachments)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // SỬA LỖI: Thay đổi quy tắc xóa cho Comment -> Attachment
            // để phá vỡ chuỗi cascade delete.
            // Hành vi mặc định sẽ là Restrict (NO ACTION), ngăn việc xóa Comment
            // nếu nó vẫn còn Attachment liên quan.
            builder.Entity<Comment>()
                .HasMany<Attachment>() // Một comment có nhiều attachment
                .WithOne(a => a.Comment) 
                .HasForeignKey(a => a.CommentId)
                .IsRequired(false) 
                .OnDelete(DeleteBehavior.Restrict); // THAY ĐỔI TỪ Cascade -> Restrict

            builder.Entity<Ticket>().HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Ticket>().HasOne(t => t.Group).WithMany().HasForeignKey(t => t.GroupId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Ticket>().HasOne(t => t.ProblemType).WithMany().HasForeignKey(t => t.ProblemTypeId).OnDelete(DeleteBehavior.SetNull); // Thêm quan hệ cho Ticket và ProblemType

            // Khi một Group bị xóa, set GroupId trong ProblemType thành NULL
            builder.Entity<ProblemType>()
                .HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Ticket>()
                .Property(t => t.Priority)
                .HasConversion<string>()
                .HasDefaultValue("Medium")
                .IsRequired();
        }
    }
} 