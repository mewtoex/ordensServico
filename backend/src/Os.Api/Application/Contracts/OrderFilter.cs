using Os.Api.Domain;

namespace Os.Api.Application;

public record OrderFilter
{
    public Guid? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
