using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using TechnicalSupport.Application.Features.Attachments.Abstractions;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence.Seed
{
    /// <summary>
    /// Cung cấp các phương thức tĩnh để khởi tạo dữ liệu mẫu cho cơ sở dữ liệu.
    /// </summary>
    public static class DataSeeder
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// Di chuyển vai trò cũ "Technician" sang vai trò mới "Agent" để đảm bảo tính tương thích ngược.
        /// </summary>
        public static async Task MigrateRoles(RoleManager<IdentityRole> roleManager)
        {
            var oldRole = await roleManager.FindByNameAsync("Technician");
            var newRoleExists = await roleManager.RoleExistsAsync("Agent");

            if (oldRole != null && !newRoleExists)
            {
                oldRole.Name = "Agent";
                oldRole.NormalizedName = "AGENT";
                await roleManager.UpdateAsync(oldRole);
            }
        }

        /// <summary>
        /// Phương thức chính để thực hiện việc seed dữ liệu.
        /// Nó sẽ seed các dữ liệu nền tảng (vai trò, trạng thái, nhóm) và chỉ seed dữ liệu người dùng/ticket nếu database trống.
        /// </summary>
        /// <param name="serviceProvider">Đối tượng IServiceProvider để resolve các dịch vụ cần thiết.</param>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var fileStorageService = serviceProvider.GetRequiredService<IFileStorageService>();

            // 1. Seed dữ liệu nền tảng (luôn chạy)
            await SeedRolesAsync(roleManager);
            await SeedStatusesAsync(context);
            await SeedGroupsAsync(context);
            await SeedProblemTypesAsync(context);

            // 2. Seed dữ liệu nghiệp vụ (chỉ khi chưa có người dùng nào)
            if (!await userManager.Users.AnyAsync())
            {
                var (clients, agents, managers) = await SeedUsersAsync(userManager, context);
                await SeedTicketsAndRelatedDataAsync(context, fileStorageService, clients, agents, managers);
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Client", "Agent", "Manager", "Admin", "Group Manager", "Ticket Manager" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedStatusesAsync(ApplicationDbContext context)
        {
            if (await context.Statuses.AnyAsync()) return;
            var statuses = new List<Status>
            {
                new Status { Name = "Open" }, new Status { Name = "In Progress" },
                new Status { Name = "Resolved" }, new Status { Name = "Closed" },
                new Status { Name = "On Hold" }
            };
            await context.Statuses.AddRangeAsync(statuses);
            await context.SaveChangesAsync();
        }
        
        private static async Task SeedGroupsAsync(ApplicationDbContext context)
        {
            if (await context.Groups.AnyAsync()) return;
            var groups = new List<Group>
            {
                new Group { Name = "Tier 1 Support", Description = "General hardware and software issues." },
                new Group { Name = "Network Operations", Description = "Handles all network connectivity issues." },
                new Group { Name = "Database Admins", Description = "Manages database access and performance." },
                new Group { Name = "Software Development", Description = "Handles application bugs and feature requests." }
            };
            await context.Groups.AddRangeAsync(groups);
            await context.SaveChangesAsync();
        }
        
        private static async Task SeedProblemTypesAsync(ApplicationDbContext context)
        {
            if (await context.ProblemTypes.AnyAsync()) return;
            var groups = await context.Groups.ToListAsync();
            var networkGroup = groups.First(g => g.Name == "Network Operations");
            var dbGroup = groups.First(g => g.Name == "Database Admins");
            var softwareGroup = groups.First(g => g.Name == "Software Development");

            var problemTypes = new List<ProblemType>
            {
                new ProblemType { Name = "Lỗi Phần cứng", Description = "Các vấn đề liên quan đến thiết bị vật lý (chuột, phím, màn hình)." },
                new ProblemType { Name = "Lỗi Hệ điều hành", Description = "Các vấn đề liên quan đến Windows, macOS." },
                new ProblemType { Name = "Lỗi Phần mềm", Description = "Các vấn đề liên quan đến ứng dụng, hệ điều hành.", GroupId = softwareGroup.GroupId},
                new ProblemType { Name = "Yêu cầu Mạng", Description = "Vấn đề về kết nối, VPN, WiFi.", GroupId = networkGroup.GroupId },
                new ProblemType { Name = "Yêu cầu Database", Description = "Vấn đề về truy cập, hiệu suất DB.", GroupId = dbGroup.GroupId },
                new ProblemType { Name = "Yêu cầu Tài khoản", Description = "Quên mật khẩu, khóa tài khoản, phân quyền."},
                new ProblemType { Name = "Không xác định", Description = "Sử dụng khi không chắc chắn về loại vấn đề." }
            };
            await context.ProblemTypes.AddRangeAsync(problemTypes);
            await context.SaveChangesAsync();
        }

        private static async Task<(List<ApplicationUser> clients, List<ApplicationUser> agents, List<ApplicationUser> managers)> SeedUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var firstNames = new[] { "Linh", "An", "Bao", "Chau", "Dung", "Giang", "Hieu", "Khanh", "Mai", "Nam" };
            var lastNames = new[] { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang" };
            
            var managers = new List<ApplicationUser>();
            var admin = await CreateUserAsync(userManager, "admin@example.com", "System Admin", "All", new[] { "Admin" });
            var userManagerUser = await CreateUserAsync(userManager, "usermanager@example.com", "HR Manager", "User Management", new[] { "Manager" });
            var ticketManager = await CreateUserAsync(userManager, "ticketmanager@example.com", "Triage Manager", "Triage", new[] { "Ticket Manager", "Agent" });
            managers.AddRange(new[] { admin, userManagerUser, ticketManager });

            var allGroups = await context.Groups.ToListAsync();
            var agents = new List<ApplicationUser>();
            int groupManagerCounter = 1;
            foreach(var group in allGroups)
            {
                var groupManager = await CreateUserAsync(userManager, $"group.manager{groupManagerCounter++}@example.com", $"{group.Name} Lead", "Team Lead", new[] { "Group Manager", "Agent" });
                await AddUserToGroupAsync(context, groupManager.Id, group.GroupId);
                agents.Add(groupManager);
                managers.Add(groupManager);
            }

            var expertises = new[] { "Hardware", "Software", "Networking", "Database", "Security" };
            for (int i = 0; i < 20; i++) 
            {
                var expertise = expertises[Rng.Next(expertises.Length)];
                var user = await CreateUserAsync(userManager, $"agent{i+1}@example.com", $"{lastNames[Rng.Next(lastNames.Length)]} {firstNames[Rng.Next(firstNames.Length)]}", expertise, new[] { "Agent" });
                agents.Add(user);
                await AddUserToGroupAsync(context, user.Id, allGroups[Rng.Next(allGroups.Count)].GroupId);
                 if (Rng.Next(0, 5) == 0)
                {
                    await AddUserToGroupAsync(context, user.Id, allGroups[Rng.Next(allGroups.Count)].GroupId);
                }
            }
            agents.Add(ticketManager);

            var clients = new List<ApplicationUser>();
            var defaultClient = await CreateUserAsync(userManager, "client@example.com", "Default Client", null, new[] { "Client" });
            clients.Add(defaultClient);
            for (int i = 0; i < 49; i++)
            {
                var user = await CreateUserAsync(userManager, $"client{i+1}@example.com", $"{lastNames[Rng.Next(lastNames.Length)]} {firstNames[Rng.Next(firstNames.Length)]}", null, new[] { "Client" });
                clients.Add(user);
            }

            return (clients, agents, managers);
        }

        private static async Task SeedTicketsAndRelatedDataAsync(ApplicationDbContext context, IFileStorageService fileStorage, List<ApplicationUser> clients, List<ApplicationUser> agents, List<ApplicationUser> managers)
        {
            var statuses = await context.Statuses.ToListAsync();
            var problemTypes = await context.ProblemTypes.ToListAsync();
            var priorities = new[] { "Low", "Medium", "High" };
            var ticketNouns = new[] { "Printer", "Monitor", "System", "Application", "Server", "Network", "Database", "Account" };
            var ticketVerbs = new[] { "is not working", "is slow", "cannot connect", "crashes", "needs access", "shows an error" };
            
            var ticketsToCreate = new List<Ticket>();

            for (int i = 0; i < 200; i++)
            {
                var customer = clients[Rng.Next(clients.Count)];
                var problemType = problemTypes[Rng.Next(problemTypes.Count)];
                var status = statuses[Rng.Next(statuses.Count)];
                
                var ticket = new Ticket
                {
                    Title = $"{ticketNouns[Rng.Next(ticketNouns.Length)]} {ticketVerbs[Rng.Next(ticketVerbs.Length)]}",
                    Description = $"Details about the issue with {problemType.Name.ToLower()}. User '{customer.DisplayName}' reports that they are unable to proceed. This issue has a priority of {priorities[Rng.Next(priorities.Length)]}. Please investigate as soon as possible.",
                    CustomerId = customer.Id,
                    StatusId = status.StatusId,
                    Priority = priorities[Rng.Next(priorities.Length)],
                    ProblemTypeId = problemType.ProblemTypeId,
                    GroupId = problemType.GroupId,
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-Rng.Next(0, 30))
                };
                
                if(ticket.GroupId.HasValue && status.Name != "Closed" && status.Name != "Resolved" && Rng.Next(0,2) == 0)
                {
                     var agentsInGroup = await context.TechnicianGroups
                        .Where(tg => tg.GroupId == ticket.GroupId.Value)
                        .Select(tg => tg.UserId)
                        .ToListAsync();

                    if(agentsInGroup.Any())
                    {
                        var agentIdToAssign = agentsInGroup[Rng.Next(agentsInGroup.Count)];
                        ticket.AssigneeId = agentIdToAssign;
                    }
                }

                ticketsToCreate.Add(ticket);
            }
            await context.Tickets.AddRangeAsync(ticketsToCreate);
            await context.SaveChangesAsync();

            foreach (var ticket in ticketsToCreate)
            {
                int commentCount = Rng.Next(0, 6);
                for (int j = 0; j < commentCount; j++)
                {
                    var commenterId = (j % 2 == 0) ? ticket.CustomerId : (ticket.AssigneeId ?? managers.First(m => m.Email == "ticketmanager@example.com").Id);
                    var comment = new Comment
                    {
                        TicketId = ticket.TicketId,
                        UserId = commenterId,
                        Content = $"This is an update on ticket #{ticket.TicketId}. Action required.",
                        CreatedAt = ticket.CreatedAt.AddHours(Rng.Next(1, 48))
                    };
                    context.Comments.Add(comment);
                }

                if (Rng.Next(0, 10) == 0)
                {
                    await CreateAttachmentAsync(fileStorage, context, ticket.TicketId, ticket.CustomerId);
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string displayName, string? expertise, string[] roles)
        {
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName, Expertise = expertise, EmailConfirmed = true };
            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRolesAsync(user, roles);
            return user;
        }

        private static async Task AddUserToGroupAsync(ApplicationDbContext context, string userId, int groupId)
        {
             var exists = await context.TechnicianGroups.AnyAsync(tg => tg.UserId == userId && tg.GroupId == groupId);
             if(!exists)
             {
                context.TechnicianGroups.Add(new TechnicianGroup { UserId = userId, GroupId = groupId });
                await context.SaveChangesAsync();
             }
        }
        
        private static async Task CreateAttachmentAsync(IFileStorageService fileStorage, ApplicationDbContext context, int ticketId, string uploaderId)
        {
            string content = $"Log file for ticket {ticketId}\nTimestamp: {DateTime.Now}\nRandom data: {Guid.NewGuid()}\nEnd of log.";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            var fileDto = new FileContentDto
            {
                Content = stream,
                FileName = $"log_{ticketId}_{Guid.NewGuid().ToString().Substring(0, 8)}.txt",
                ContentType = "text/plain"
            };

            var storedPath = await fileStorage.SaveFileAsync(fileDto, ticketId.ToString());

            var attachment = new Attachment
            {
                TicketId = ticketId,
                UploadedById = uploaderId,
                OriginalFileName = fileDto.FileName,
                StoredPath = storedPath,
                FileType = fileDto.ContentType,
                UploadedAt = DateTime.UtcNow
            };
            context.Attachments.Add(attachment);
        }
    }
} 