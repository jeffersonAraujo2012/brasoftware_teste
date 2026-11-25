namespace Application.Users;

public sealed class UserConflictException(string field, Exception? innerException = null)
    : Exception("A user with this value already exists.", innerException)
{
    public string Field { get; } = field;
}
