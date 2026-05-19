using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Extensions;

public static class ControllerResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceResultStatus.Ok when result.Value is not null => controller.Ok(result.Value),
            ServiceResultStatus.Created when result.Value is not null => controller.Ok(result.Value),
            ServiceResultStatus.BadRequest => controller.BadRequest(result.Error),
            ServiceResultStatus.Unauthorized => controller.Unauthorized(result.Error),
            ServiceResultStatus.Forbidden => controller.Forbid(),
            ServiceResultStatus.NotFound => controller.NotFound(result.Error),
            ServiceResultStatus.NoContent => controller.NoContent(),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, "Невідома помилка сервера.")
        };
    }

    public static IActionResult ToIActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceResultStatus.Ok when result.Value is not null => controller.Ok(result.Value),
            ServiceResultStatus.Created when result.Value is not null => controller.Ok(result.Value),
            ServiceResultStatus.BadRequest => controller.BadRequest(result.Error),
            ServiceResultStatus.Unauthorized => controller.Unauthorized(result.Error),
            ServiceResultStatus.Forbidden => controller.Forbid(),
            ServiceResultStatus.NotFound => controller.NotFound(result.Error),
            ServiceResultStatus.NoContent => controller.NoContent(),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, "Невідома помилка сервера.")
        };
    }

    public static IActionResult ToActionResult(this ControllerBase controller, ServiceResult result)
    {
        return result.Status switch
        {
            ServiceResultStatus.Ok => controller.Ok(),
            ServiceResultStatus.NoContent => controller.NoContent(),
            ServiceResultStatus.BadRequest => controller.BadRequest(result.Error),
            ServiceResultStatus.Unauthorized => controller.Unauthorized(result.Error),
            ServiceResultStatus.Forbidden => controller.Forbid(),
            ServiceResultStatus.NotFound => controller.NotFound(result.Error),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, "Невідома помилка сервера.")
        };
    }
}
