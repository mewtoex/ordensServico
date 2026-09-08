using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record RefreshRequest([Required, StringLength(64, MinimumLength = 64)] string RefreshToken);
