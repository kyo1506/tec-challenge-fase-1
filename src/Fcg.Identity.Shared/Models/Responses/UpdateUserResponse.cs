namespace Fcg.Identity.Shared.Models.Responses;

public class UpdateUserResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ActionRequired { get; set; }
}
