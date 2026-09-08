using Os.Api.Domain;

namespace Os.Api.Application;

public record ExportFile(byte[] Content, string ContentType, string FileName);
