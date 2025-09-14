using System.Net;
using Fcg.Identity.Domain.Interfaces;
using Fcg.Identity.Shared.Models.Dtos;
using Fcg.Identity.Shared.Models.Generics;
using Microsoft.AspNetCore.Authorization;
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
    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Root<LoginResponseDto>>> Login(LoginDto model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<LoginResponseDto>(ModelState);

        var response = await keycloakService.LoginAsync(model);

        if (response == null)
        {
            NotifyError("E-mail ou senha inválidos.");
            return CustomResponse<LoginResponseDto>(statusCode: HttpStatusCode.Unauthorized);
        }

        return CustomResponse(response);
    }

    /// <summary>
    /// Registra um novo usuário no sistema.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<UserDto>>> Register(CreateUserDto model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<UserDto>(ModelState);

        var createdUser = await keycloakService.CreateUserAsync(model);

        // CORREÇÃO APLICADA AQUI:
        // Se o usuário for nulo, o serviço já adicionou a notificação de erro.
        // O MainController.CustomResponse irá ler essa notificação e montar a resposta de erro.
        if (createdUser == null)
        {
            // O serviço de notificação (INotifier) é a única fonte da verdade.
            // O CustomResponse vai pegar o erro de lá. O status code aqui é apenas uma sugestão.
            // Se o serviço notificou um erro de conflito, o status code 409 seria mais semântico,
            // mas um 400 Bad Request genérico também é aceitável.
            return CustomResponse<UserDto>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse(createdUser, HttpStatusCode.Created);
    }

    /// <summary>
    /// Obtém uma lista de todos os usuários. (Requer scope 'users:manage')
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = "CanManageUsers")] // Usando a política baseada em scope
    [ProducesResponseType(typeof(Root<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<UserDto>>>> GetAllUsers()
    {
        var users = await keycloakService.GetUsersAsync();
        return CustomResponse(users);
    }

    /// <summary>
    /// Obtém um usuário específico pelo seu ID. (Requer scope 'users:read' ou 'users:manage')
    /// </summary>
    [HttpGet("users/{id:guid}")]
    [Authorize(Policy = "CanReadUsers")]
    [ProducesResponseType(typeof(Root<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserDto>>> GetUserById(Guid id)
    {
        var user = await keycloakService.GetUserByIdAsync(id);
        if (user == null)
        {
            NotifyError("Usuário não encontrado.");
            return CustomResponse<UserDto>(statusCode: HttpStatusCode.NotFound);
        }
        return CustomResponse(user);
    }

    /// <summary>
    /// Atualiza os dados de um usuário. (Requer scope 'users:manage')
    /// </summary>
    [HttpPut("users/{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<string>>> UpdateUser(Guid id, UserDto model)
    {
        var success = await keycloakService.UpdateUserAsync(id, model);

        if (!success)
        {
            // O serviço já notificou o erro, apenas retornamos a resposta customizada.
            return CustomResponse<string>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse<string>(statusCode: HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Desabilita um usuário (soft delete). (Requer scope 'users:manage')
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<string>>> DeleteUser(Guid id)
    {
        var success = await keycloakService.DeleteUserAsync(id);

        if (!success)
        {
            return CustomResponse<string>(statusCode: HttpStatusCode.BadRequest);
        }

        return CustomResponse<string>(statusCode: HttpStatusCode.NoContent);
    }
}
