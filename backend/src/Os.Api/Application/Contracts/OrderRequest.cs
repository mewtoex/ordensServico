using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record OrderRequest(Guid CustomerId, Guid TechnicianId, [Required, StringLength(4000)] string Description);
