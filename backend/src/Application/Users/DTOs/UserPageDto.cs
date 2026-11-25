namespace Application.Users.DTOs;

public sealed record UserPageDto(IReadOnlyList<UserDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);
