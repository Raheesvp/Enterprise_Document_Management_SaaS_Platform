namespace shared.Domain.Common;

public interface ICurrentUser
{
    Guid UserId { get; }

    string Email { get; }

    string FullName { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);
}