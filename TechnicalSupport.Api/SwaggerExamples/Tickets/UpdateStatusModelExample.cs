using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Tickets
{
    public class UpdateStatusModelExample : IExamplesProvider<UpdateStatusModel>
    {
        public UpdateStatusModel GetExamples()
        {
            return new UpdateStatusModel
            {
                StatusId = 2 // 2: In Progress
            };
        }
    }
} 