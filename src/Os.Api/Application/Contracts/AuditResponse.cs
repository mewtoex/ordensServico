using Os.Api.Domain;

namespace Os.Api.Application;

public record AuditResponse(Guid Id, Guid ActorId, string ActorName, string Action, string Detail, DateTimeOffset At);
