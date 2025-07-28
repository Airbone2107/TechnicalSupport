using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Infrastructure.Persistence.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            if (!await roleManager.Roles.AnyAsync())
            {
                await roleManager.CreateAsync(new IdentityRole("Client"));
                await roleManager.CreateAsync(new IdentityRole("Technician"));
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Seed Statuses
            if (!await context.Statuses.AnyAsync())
            {
                var statuses = new List<Status>
                {
                    new Status { Name = "Open" },
                    new Status { Name = "In Progress" },
                    new Status { Name = "Resolved" },
                    new Status { Name = "Closed" }
                };
                await context.Statuses.AddRangeAsync(statuses);
                await context.SaveChangesAsync();
            }

            // Seed Users, Tickets, and Comments only if there are no users yet.
            if (!await userManager.Users.AnyAsync())
            {
                // Client User
                var clientUser = new ApplicationUser
                {
                    UserName = "client@example.com",
                    Email = "client@example.com",
                    DisplayName = "John Doe (Client)",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(clientUser, "Password123!");
                await userManager.AddToRoleAsync(clientUser, "Client");

                // Technician User
                var techUser = new ApplicationUser
                {
                    UserName = "tech@example.com",
                    Email = "tech@example.com",
                    DisplayName = "Jane Smith (Tech)",
                    Expertise = "Software",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(techUser, "Password123!");
                await userManager.AddToRoleAsync(techUser, "Technician");
                
                // Get statuses for ticket creation
                var openStatus = await context.Statuses.FirstAsync(s => s.Name == "Open");
                var inProgressStatus = await context.Statuses.FirstAsync(s => s.Name == "In Progress");

                // Seed Tickets
                var tickets = new List<Ticket>
                {
                    new Ticket
                    {
                        Title = "Cannot login to the system",
                        Description = "I am unable to login with my credentials. The system shows an 'Invalid credentials' error.",
                        CustomerId = clientUser.Id,
                        AssigneeId = techUser.Id, // Assigned ticket
                        StatusId = inProgressStatus.StatusId,
                        Priority = "High",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    },
                    new Ticket
                    {
                        Title = "Printer is not working",
                        Description = "My office printer is not responding. I have checked the power and network cables.",
                        CustomerId = clientUser.Id,
                        AssigneeId = null, // Unassigned ticket
                        StatusId = openStatus.StatusId,
                        Priority = "Medium",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    },
                     new Ticket
                    {
                        Title = "Software installation request",
                        Description = "Please install the latest version of Adobe Photoshop on my machine.",
                        CustomerId = clientUser.Id,
                        AssigneeId = techUser.Id,
                        StatusId = openStatus.StatusId,
                        Priority = "Low",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow,
                    }
                };
                await context.Tickets.AddRangeAsync(tickets);
                await context.SaveChangesAsync();

                // Seed Comments
                var firstTicket = await context.Tickets.FirstAsync();
                var comments = new List<Comment>
                {
                    new Comment
                    {
                        TicketId = firstTicket.TicketId,
                        UserId = clientUser.Id,
                        Content = "I've tried resetting my password, but it didn't work.",
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    },
                    new Comment
                    {
                        TicketId = firstTicket.TicketId,
                        UserId = techUser.Id,
                        Content = "Hi John, I'm looking into this. I have reset your account lockout status. Could you please try again?",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    }
                };
                await context.Comments.AddRangeAsync(comments);
                await context.SaveChangesAsync();
            }
        }
    }
} 