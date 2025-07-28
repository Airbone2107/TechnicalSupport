using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Tickets
{
    public class AddCommentModelExample : IExamplesProvider<AddCommentModel>
    {
        public AddCommentModel GetExamples()
        {
            return new AddCommentModel
            {
                Content = "I have already tried restarting the router, but it didn't help."
            };
        }
    }
} 