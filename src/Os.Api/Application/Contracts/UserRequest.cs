using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record UserRequest([Required, StringLength(160)] string Name, [Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(128, MinimumLength = 12)] string Password, [EnumDataType(typeof(Role))] Role Role);
