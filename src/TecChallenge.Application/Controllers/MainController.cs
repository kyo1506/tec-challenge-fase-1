using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TecChallenge.Domain.Notifications;
using IUser = TecChallenge.Domain.Interfaces.IUser;

namespace TecChallenge.Application.Controllers;

[ApiController]
public abstract class MainController : ControllerBase
{
    private readonly IUser _appUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotifier _notifier;
    protected readonly IWebHostEnvironment WebHostEnvironment;

    protected MainController(
        INotifier notifier,
        IUser appUser,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment
    )
    {
        _notifier = notifier;
        _appUser = appUser;

        if (appUser.IsAuthenticated())
        {
            UserId = appUser.GetUserId();
            AuthenticatedUser = true;
        }

        _httpContextAccessor = httpContextAccessor;
        WebHostEnvironment = webHostEnvironment;
    }

    protected Guid UserId { get; set; }
    protected bool AuthenticatedUser { get; set; }

    private bool OperationValid()
    {
        return !_notifier.HasNotification();
    }

    protected ActionResult<Root<T>> CustomResponse<T>(
        T? data = null,
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
        where T : class
    {
        return OperationValid() ? SuccessResponse(data, statusCode) : ErrorResponse<T>(statusCode);
    }

    protected ActionResult<Root<T>> CustomModelStateResponse<T>(
        ModelStateDictionary modelState,
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
        where T : class
    {
        if (!modelState.IsValid)
            NotifyErrorInvalidModel(modelState);
        return CustomResponse<T>(statusCode: statusCode);
    }

    private ActionResult<Root<T>> SuccessResponse<T>(T? data, HttpStatusCode statusCode)
        where T : class
    {
        if ((int)statusCode == StatusCodes.Status204NoContent)
            return NoContent();

        return StatusCode(
            (int)statusCode,
            new Root<T>
            {
                StatusCode = (int)statusCode,
                Success = true,
                Data = data,
            }
        );
    }

    private ActionResult<Root<T>> ErrorResponse<T>(HttpStatusCode statusCode)
        where T : class
    {
        var errors = _notifier.GetNotifications().Select(n => n.Message).ToArray();

        return StatusCode(
            (int)statusCode,
            new Root<T>
            {
                StatusCode = (int)statusCode,
                Success = false,
                Errors = errors,
            }
        );
    }

    private void NotifyErrorInvalidModel(ModelStateDictionary modelState)
    {
        var errors = modelState.Values.SelectMany(e => e.Errors);
        foreach (var error in errors)
        {
            var errorMsg = error.Exception == null ? error.ErrorMessage : error.Exception.Message;
            NotifyError(errorMsg);
        }
    }

    protected void NotifyError(string message)
    {
        _notifier.Handle(new Notification(message));
    }

    protected virtual async Task<string> GetTemplateFile()
    {
        var filePath = Path.Combine(
            WebHostEnvironment.WebRootPath,
            "assets",
            "templates",
            "template.html"
        );

        return await System.IO.File.ReadAllTextAsync(filePath);
    }

    protected bool HasClaim(string claimName, string claimValue)
    {
        return _appUser.IsAuthenticated()
            && _appUser
                .GetClaimsIdentity()
                .Any(c => c.Type == claimName && c.Value.Contains(claimValue));
    }
}
