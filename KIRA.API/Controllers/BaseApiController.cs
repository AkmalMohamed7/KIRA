using KIRA.Shared.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KIRA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly IMediator _mediator;

        protected BaseApiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        protected static ActionResult<T> ToActionResult<T>(
            ResultT<T> result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result.Value);

            return ToProblem(result.Error);
        }

        protected static ActionResult ToActionResult(
            Result result)
        {
            if (result.IsSuccess)
                return new OkResult();

            return ToProblem(result.Error);
        }

        private static ObjectResult ToProblem(
            IReadOnlyList<Error> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                return new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "InternalServerError",
                    Detail = "An unexpected error occurred."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            var firstError = errors[0];

            var statusCode = firstError.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.BadRequest => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = firstError.Code,
                Detail = firstError.Description
            };

            problem.Extensions["errors"] = errors;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode
            };
        }
    }
}
