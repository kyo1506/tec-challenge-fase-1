using System.Security.Claims;
using Fcg.Identity.Client.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ExampleMicroservice.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [RequirePermission("users", "manage")]
    public IActionResult GetUsers()
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        return Ok(
            new
            {
                message = "Lista de usuários acessada com sucesso!",
                authenticatedUser = new
                {
                    userId,
                    username,
                    email,
                    roles,
                },
                users = new[]
                {
                    new
                    {
                        id = 1,
                        name = "João Silva",
                        email = "joao@example.com",
                    },
                    new
                    {
                        id = 2,
                        name = "Maria Santos",
                        email = "maria@example.com",
                    },
                    new
                    {
                        id = 3,
                        name = "Pedro Oliveira",
                        email = "pedro@example.com",
                    },
                },
            }
        );
    }

    [HttpPost]
    [RequirePermission("users", "manage")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(
            new
            {
                message = "Usuário criado com sucesso!",
                createdBy = userId,
                user = new
                {
                    id = 4,
                    name = request.Name,
                    email = request.Email,
                },
            }
        );
    }

    [HttpDelete("{id}")]
    [RequirePermission("users", "manage")]
    public IActionResult DeleteUser(int id)
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new { message = $"Usuário {id} deletado com sucesso!", deletedBy = userId });
    }

    [HttpGet("public")]
    public IActionResult GetPublicInfo()
    {
        return Ok(
            new
            {
                message = "Esta é uma rota pública - não requer autenticação",
                timestamp = DateTime.UtcNow,
            }
        );
    }
}

public record CreateUserRequest(string Name, string Email);
