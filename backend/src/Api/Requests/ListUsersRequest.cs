using System.ComponentModel;
using Application.Users.Queries.ListUsers;
using Microsoft.AspNetCore.Mvc;

namespace Api.Requests;

public sealed class ListUsersRequest
{
    [FromQuery(Name = "page")]
    [Description("Page number starting at 1. Defaults to 1.")]
    public int Page { get; init; } = ListUsersQuery.DefaultPage;

    [FromQuery(Name = "pageSize")]
    [Description("Number of users per page, from 1 to 100. Defaults to 20.")]
    public int PageSize { get; init; } = ListUsersQuery.DefaultPageSize;

    [FromQuery(Name = "sortBy")]
    [Description("Sort field: fullName, email, cpf, createdAt, or status. Defaults to createdAt.")]
    public string? SortBy { get; init; } = ListUsersQuery.DefaultSortBy;

    [FromQuery(Name = "sortDirection")]
    [Description("Primary sort direction: asc or desc. Defaults to desc. ID breaks ties in ascending order.")]
    public string? SortDirection { get; init; } = ListUsersQuery.DefaultSortDirection;
}
