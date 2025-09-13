using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using TecChallenge.Application.V1.Controllers;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Shared.Models.Dtos;

namespace TecChallenge.Tests;

public class AuthTest
{
    private readonly Mock<INotifier> _mockNotifier;
    private readonly Mock<IUser> _mockAppUser;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<IKeycloakAdminService> _mockKeycloakAdminService;
    private readonly AuthController _controller;

    public AuthTest()
    {
        _mockNotifier = new Mock<INotifier>();
        _mockAppUser = new Mock<IUser>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockKeycloakAdminService = new Mock<IKeycloakAdminService>();

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
        var loginDto = new LoginDto { Email = "test@test.com", Password = "wrongpassword" };
        _mockKeycloakAdminService
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync((LoginResponseDto?)null);

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
