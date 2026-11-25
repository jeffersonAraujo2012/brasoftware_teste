namespace Application.Exceptions;

public sealed class UserNotFoundException(Guid id, Exception? innerException = null)
    : Exception("User was not found.", innerException)
{
    public Guid Id { get; } = id;
}
