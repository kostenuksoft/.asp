namespace Registry.Dal.Auditing;

public interface ICurrentUser
{
    string? UserName { get; }
}
