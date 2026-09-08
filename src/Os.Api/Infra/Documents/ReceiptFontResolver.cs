using PdfSharp.Fonts;

namespace Os.Api.Infra.Documents;

public class ReceiptFontResolver : IFontResolver
{
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(bold ? "VeraBd" : "Vera");

    public byte[] GetFont(string faceName)
    {
        using var stream = typeof(ReceiptFontResolver).Assembly.GetManifestResourceStream($"Os.Api.Resources.Fonts.{faceName}.ttf")
            ?? throw new InvalidOperationException("Fonte do comprovante não encontrada.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
