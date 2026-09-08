using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
