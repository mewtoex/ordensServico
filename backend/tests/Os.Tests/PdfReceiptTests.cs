using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra.Documents;
using PdfSharp.Pdf.IO;
using Xunit;

namespace Os.Tests;

public class PdfReceiptTests
{
    [Fact]
    public void ReceiptPaginatesLongDescriptionsAndItemNames()
    {
        var items = Enumerable.Range(1, 45).Select(index => new OrderItemResponse(Guid.NewGuid(), Guid.NewGuid(),
            $"{index:D2}. Serviço de manutenção e substituição de peças - conferência do equipamento e avaliação de funcionamento após o reparo",
            index % 2 == 0 ? ItemKind.Servico : ItemKind.Peca, 2, 35.50m, 71m)).ToList();
        var order = new OrderResponse(Guid.Parse("c257a60c-ea1b-4231-bef2-68dbab6e6c19"), Guid.NewGuid(), "José da Conceição - Assistência Técnica",
            Guid.NewGuid(), string.Concat(Enumerable.Repeat("Diagnóstico: revisão do equipamento, limpeza dos componentes e substituição das peças danificadas. ", 24)),
            OrderStatus.Concluida, new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero), items.Sum(item => item.Subtotal), items);
        var bytes = new OrderPdfRenderer().Render(order);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount >= 3);
        Assert.Contains(order.Id.ToString(), document.Info.Title);
        var samplePath = Environment.GetEnvironmentVariable("PdfSample__OutputPath");
        if (!string.IsNullOrEmpty(samplePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(samplePath))!);
            File.WriteAllBytes(samplePath, bytes);
        }
    }
}
