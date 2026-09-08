using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record UpdateUserRequest([Required, StringLength(160)] string Name, [Required, EmailAddress, StringLength(254)] string Email, [EnumDataType(typeof(Role))] Role Role);
