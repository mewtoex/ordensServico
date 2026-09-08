using Os.Api.Domain;

namespace Os.Api.Application;

public record PagedResponse<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Data);
