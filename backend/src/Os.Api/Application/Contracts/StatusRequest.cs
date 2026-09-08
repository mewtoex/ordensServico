using System.ComponentModel.DataAnnotations;
using Os.Api.Domain;

namespace Os.Api.Application;

public record StatusRequest([EnumDataType(typeof(OrderStatus))] OrderStatus Status);
