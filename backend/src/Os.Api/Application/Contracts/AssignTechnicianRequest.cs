using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record AssignTechnicianRequest(Guid TechnicianId);
