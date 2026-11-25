using Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Application.Users;

public sealed record GetUserQuery(Guid Id) : IRequest<UserDetails>;

public sealed class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator() => RuleFor(query => query.Id).NotEmpty();
}

internal sealed class GetUserHandler(IUserRepository repository) : IRequestHandler<GetUserQuery, UserDetails>
{
    public async Task<UserDetails> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
        return UserDetails.From(user);
    }
}
