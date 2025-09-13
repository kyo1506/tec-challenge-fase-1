using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Shared.Models.Dtos;

namespace TecChallenge.Application.V1.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[Produces("application/json")]
public class AuthController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IKeycloakAdminService keycloakAdminService
) : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    private readonly IKeycloakAdminService _keycloakAdminService = keycloakAdminService;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Root<LoginResponseDto>>> Login(LoginDto model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<LoginResponseDto>(ModelState);

        var response = await _keycloakAdminService.LoginAsync(model);

        if (response == null)
        {
            NotifyError("Invalid email or password.");
            return CustomResponse<LoginResponseDto>(statusCode: HttpStatusCode.Unauthorized);
        }

        return CustomResponse(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Root<UserDto>>> Register(CreateUserDto model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<UserDto>(ModelState);

        var result = await _keycloakAdminService.CreateUserAsync(model);

        if (!result.Success)
        {
            result.Errors.ForEach(NotifyError);
            return CustomResponse<UserDto>(statusCode: result.StatusCode);
        }

        return CustomResponse(result.Data, result.StatusCode);
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Root<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<UserDto>>>> GetAllUsers()
    {
        var users = await _keycloakAdminService.GetUsersAsync();
        return CustomResponse(users);
    }

    [HttpGet("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserDto>>> GetUserById(Guid id)
    {
        var user = await _keycloakAdminService.GetUserByIdAsync(id);
        if (user == null)
        {
            NotifyError("User not found.");
            return CustomResponse<UserDto>(statusCode: HttpStatusCode.NotFound);
        }
        return CustomResponse(user);
    }

    [HttpPut("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<string>>> UpdateUser(Guid id, UserDto model)
    {
        var success = await _keycloakAdminService.UpdateUserAsync(id, model);

        if (!success)
        {
            NotifyError("Failed to update user.");
            return CustomResponse<string>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse<string>(statusCode: HttpStatusCode.NoContent);
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<string>>> DeleteUser(Guid id)
    {
        var success = await _keycloakAdminService.DeleteUserAsync(id);

        if (!success)
        {
            NotifyError("Failed to delete user.");
            return CustomResponse<string>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse<string>(statusCode: HttpStatusCode.NoContent);
    }
}
