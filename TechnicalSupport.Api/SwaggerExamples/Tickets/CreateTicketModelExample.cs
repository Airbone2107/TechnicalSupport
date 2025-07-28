using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Tickets
{
    public class CreateTicketModelExample : IExamplesProvider<CreateTicketModel>
    {
        public CreateTicketModel GetExamples()
        {
            return new CreateTicketModel
            {
                Title = "Cannot connect to the network",
                Description = "My computer is showing 'No Internet Connection' despite being connected via Ethernet.",
                StatusId = 1, // 1: Open
                Priority = "High"
            };
        }
    }
} 