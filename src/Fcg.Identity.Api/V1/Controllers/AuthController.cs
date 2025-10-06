using System.Net;
using Fcg.Identity.Shared.Models.Requests;
using Fcg.Identity.Shared.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Identity.Api.V1.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[ApiController]
public class AuthController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IKeycloakService keycloakService
) : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    private readonly IKeycloakService keycloakService = keycloakService;
    private readonly IUser appUser = appUser;

    /// <summary>
    /// Authenticates a user with email/username and password.
    /// </summary>
    /// <remarks>
    /// On success, returns an access token and a refresh token.
    /// </remarks>
    /// <param name="model">Object containing login credentials.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Root<TokenResponse>>> Login(LoginRequest model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<TokenResponse>(ModelState);

        var response = await keycloakService.LoginAsync(model);

        if (response == null)
        {
            NotifyError("Invalid email or password.");
            return CustomResponse<TokenResponse>(statusCode: HttpStatusCode.Unauthorized);
        }

        return CustomResponse(response);
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <remarks>
    /// This endpoint is public and allows creation of new users with a default role.
    /// </remarks>
    /// <param name="model">Object with data for creating the new user.</param>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Root<UserResponse>>> Register(CreateUserRequest model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<UserResponse>(ModelState);

        var createdUser = await keycloakService.CreateUserAsync(model);

        if (createdUser == null)
        {
            // Error notification (e.g.: conflict) is handled by the service.
            return CustomResponse<UserResponse>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse(createdUser, HttpStatusCode.Created);
    }

    /// <summary>
    /// Gets the list of all users in the system.
    /// </summary>
    /// <remarks>
    /// ⚠️ **Requires the permission (scope) `users:manage`.**
    /// </remarks>
    [HttpGet("users")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(typeof(Root<IEnumerable<UserResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<UserResponse>>>> GetAllUsers()
    {
        var users = await keycloakService.GetUsersAsync();
        return CustomResponse(users);
    }

    /// <summary>
    /// Searches for a specific user by their ID.
    /// </summary>
    /// <remarks>
    /// ⚠️ **Requires the permission (scope) `users:read` or `users:manage`.**
    /// </remarks>
    /// <param name="id">The ID (GUID) of the user to be searched.</param>
    [HttpGet("users/{id:guid}")]
    [Authorize(Policy = "CanReadUsers")]
    [ProducesResponseType(typeof(Root<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserResponse>>> GetUserById(Guid id)
    {
        var user = await keycloakService.GetUserByIdAsync(id);
        if (user == null)
        {
            NotifyError("User not found.");
            return CustomResponse<UserResponse>(statusCode: HttpStatusCode.NotFound);
        }
        return CustomResponse(user);
    }

    /// <summary>
    /// Gets the authenticated user's profile data.
    /// </summary>
    /// <remarks>
    /// This endpoint returns the information of the user making the request.
    /// Users can only access their own profile information.
    /// ⚠️ **Requires the permission (scope) `profiles:manage`.**
    /// </remarks>
    [HttpGet("profile")]
    [Authorize(Policy = "CanManageProfile")]
    [ProducesResponseType(typeof(Root<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserResponse>>> GetProfile()
    {
        var user = await keycloakService.GetUserByIdAsync(appUser.GetUserId());

        if (user == null)
        {
            NotifyError("User not found.");
            return CustomResponse<UserResponse>(statusCode: HttpStatusCode.NotFound);
        }

        return CustomResponse(user);
    }

    /// <summary>
    /// Allows the authenticated user to update their own profile.
    /// </summary>
    /// <remarks>
    /// When updating the own profile, the current JWT token becomes obsolete. The response will indicate the need for re-authentication.
    /// ⚠️ **Requires the permission (scope) `profiles:manage`.**
    /// </remarks>
    /// <param name="model">Object with the profile data to be updated.</param>
    [HttpPut("profile")]
    [Authorize(Policy = "CanManageProfile")]
    [ProducesResponseType(typeof(Root<UpdateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<UpdateUserResponse>>> UpdateProfile(UpdateUserRequest model)
    {
        var response = await keycloakService.UpdateUserAsync(appUser.GetUserId(), model);

        return response is not null
            ? CustomResponse(response)
            : CustomResponse<UpdateUserResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Updates the data of a specific user (administrator action).
    /// </summary>
    /// <remarks>
    /// If an administrator updates themselves, the response will indicate the need for re-authentication.
    /// ⚠️ **Requires the permission (scope) `users:manage`.**
    /// </remarks>
    /// <param name="id">The ID (GUID) of the user to be updated.</param>
    /// <param name="model">Object with the data to be updated.</param>
    [HttpPut("users/{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Root<UpdateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<UpdateUserResponse>>> UpdateUser(
        Guid id,
        UpdateUserRequest model
    )
    {
        var response = await keycloakService.UpdateUserAsync(id, model);

        return response is not null
            ? CustomResponse(response)
            : CustomResponse<UpdateUserResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Disables a user's account (soft delete).
    /// </summary>
    /// <remarks>
    /// An administrator cannot disable their own account.
    /// ⚠️ **Requires the permission (scope) `users:manage`.**
    /// </remarks>
    /// <param name="id">The ID (GUID) of the user to be disabled.</param>
    [HttpDelete("users/{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<object>>> DeleteUser(Guid id)
    {
        var authenticatedUserId = appUser.GetUserId();

        if (authenticatedUserId == id)
        {
            NotifyError(
                "Administrators cannot disable their own account. This action must be performed by another administrator."
            );
            return CustomResponse<object>(statusCode: HttpStatusCode.Forbidden);
        }

        return await keycloakService.DeleteUserAsync(id)
            ? CustomResponse<object>(statusCode: HttpStatusCode.NoContent)
            : CustomResponse<object>(statusCode: HttpStatusCode.BadRequest);
    }
}
