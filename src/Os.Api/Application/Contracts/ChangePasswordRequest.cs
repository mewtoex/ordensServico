using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record ChangePasswordRequest([Required, StringLength(128)] string CurrentPassword, [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
