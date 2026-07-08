using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Web.Api.Controllers.ApiBase;

//![Route("api/[controller]")] ----- No Need
[ApiController]
public abstract class ApiBaseController : ControllerBase
{
    protected ActionResult Problem(IReadOnlyList<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Problem();
        }
        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        return Problem(errors[0]);
    }

    private ObjectResult Problem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Failure or ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound                        => StatusCodes.Status404NotFound,
            ErrorType.Conflict                        => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized                    => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden                       => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected                      => StatusCodes.Status500InternalServerError,
            //? Health/status module: a required dependency (DB, cache, scheduler) is down — retry later.
            ErrorType.Unavailable                     => StatusCodes.Status503ServiceUnavailable,
            _                                         => StatusCodes.Status400BadRequest,
        };

        return Problem(statusCode: statusCode, title: error.Code, detail: error.Description);
    }

    private ActionResult ValidationProblem(IReadOnlyList<Error> errors)
    {
        var modelState = new ModelStateDictionary();

        foreach (Error error in errors)
        {
            modelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(modelState);
    }
}
