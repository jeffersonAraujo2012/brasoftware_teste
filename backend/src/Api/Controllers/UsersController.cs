using Api.Contracts;
using Application.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new CreateUserCommand(request.FullName, request.Email, request.Cpf, request.Status),
            cancellationToken);
        return Created($"/api/users/{user.Id}", UserResponse.From(user));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListUsersResponse>> List(
        [FromQuery] ListUsersParameters parameters, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListUsersQuery(
            parameters.Page, parameters.PageSize, parameters.SortBy, parameters.SortDirection), cancellationToken);
        return Ok(ListUsersResponse.From(result));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetUserQuery(id), cancellationToken);
        return Ok(UserResponse.From(user));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update(
        Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new UpdateUserCommand(id, request.FullName, request.Email, request.Cpf, request.Status),
            cancellationToken);
        return Ok(UserResponse.From(user));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}
