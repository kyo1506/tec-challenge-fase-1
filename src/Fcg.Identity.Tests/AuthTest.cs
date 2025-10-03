using Fcg.Identity.Api.V1.Controllers;
using Fcg.Identity.Domain.Interfaces;
using Fcg.Identity.Shared.Models.Requests;
using Fcg.Identity.Shared.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fcg.Identity.Tests;

public class AuthTest
{
    private readonly Mock<INotifier> _mockNotifier;
    private readonly Mock<IUser> _mockAppUser;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<IKeycloakService> _mockKeycloakAdminService;
    private readonly AuthController _controller;

    public AuthTest()
    {
        _mockNotifier = new Mock<INotifier>();
        _mockAppUser = new Mock<IUser>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockKeycloakAdminService = new Mock<IKeycloakService>();

        _controller = new AuthController(
            _mockNotifier.Object,
            _mockAppUser.Object,
            _mockHttpContextAccessor.Object,
            _mockWebHostEnvironment.Object,
            _mockKeycloakAdminService.Object
        );
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginRequest { Email = "test@test.com", Password = "wrongpassword" };
        _mockKeycloakAdminService
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync((TokenResponse?)null);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.NotNull(result);
        _mockNotifier.Verify(
            n =>
                n.Handle(
                    It.Is<Domain.Notifications.Notification>(noti =>
                        noti.Message.Contains("Invalid email or password")
                    )
                ),
            Times.Once
        );
    }
}
