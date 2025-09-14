namespace Fcg.Identity.Domain.Notifications;

public class Notification(string message)
{
    public string Message { get; } = message;
}