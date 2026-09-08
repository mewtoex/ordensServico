using System.Globalization;
using Os.Api.Application;
using Os.Api.Application.Abstractions;
using Os.Api.Domain;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Os.Api.Infra.Documents;

public class OrderPdfRenderer : IOrderPdfRenderer
{
    private static readonly Lazy<bool> Fonts = new(() =>
    {
        GlobalFontSettings.FontResolver ??= new ReceiptFontResolver();
        return true;
    });

    public byte[] Render(OrderResponse order)
    {
        _ = Fonts.Value;
        using var document = new PdfDocument();
        document.Info.Title = $"Ordem de Serviço {order.Id}";
        document.Info.Author = "Gestão de Ordens de Serviço";
        using (var layout = new ReceiptLayout(document, order))
        {
            layout.Draw();
        }
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private sealed class ReceiptLayout(PdfDocument document, OrderResponse order) : IDisposable
    {
        private const double Left = 42;
        private const double Width = 511;
        private const double Bottom = 770;
        private readonly XFont _text = new("Receipt", 9);
        private readonly XFont _bold = new("Receipt", 9, XFontStyleEx.Bold);
        private readonly XFont _title = new("Receipt", 18, XFontStyleEx.Bold);
        private readonly XFont _small = new("Receipt", 7);
        private readonly XBrush _ink = new XSolidBrush(XColor.FromArgb(24, 42, 60));
        private readonly XBrush _muted = new XSolidBrush(XColor.FromArgb(85, 100, 112));
        private readonly XBrush _light = new XSolidBrush(XColor.FromArgb(239, 244, 247));
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");
        private XGraphics? _graphics;
        private double _y;
        private XGraphics Graphics => _graphics!;

        public void Draw()
        {
            NewPage();
            Paragraph("CLIENTE", _bold, _muted);
            Paragraph(order.CustomerName, _text, _ink);
            _y += 9;
            Paragraph($"Abertura: {order.CreatedAt.UtcDateTime:dd/MM/yyyy HH:mm} UTC", _text, _muted);
            Paragraph($"Situação: {StatusLabel(order.Status)}", _bold, _ink);
            if (order.ClosedAt.HasValue)
            {
                Paragraph($"Encerramento: {order.ClosedAt.Value.UtcDateTime:dd/MM/yyyy HH:mm} UTC", _text, _muted);
            }
            _y += 12;
            Paragraph("DESCRIÇÃO DO ATENDIMENTO", _bold, _muted);
            Paragraph(order.Description, _text, _ink);
            _y += 16;
            TableHeader();
            foreach (var item in order.Items)
            {
                DrawItem(item);
            }
            if (order.Items.Count == 0)
            {
                Paragraph("Nenhum serviço ou peça adicionado.", _text, _muted);
            }
            EnsureSpace(78);
            _y += 15;
            Graphics.DrawRectangle(_light, Left, _y, Width, 43);
            Graphics.DrawString("TOTAL DA ORDEM", _bold, _ink, new XRect(Left + 12, _y, 250, 43), XStringFormats.CenterLeft);
            Graphics.DrawString(order.Total.ToString("C", _culture), _title, _ink, new XRect(Left + 260, _y, Width - 272, 43), XStringFormats.CenterRight);
            _y += 54;
            Paragraph("Valores calculados a partir dos serviços e peças registrados nesta ordem.", _small, _muted);
            _graphics!.Dispose();
            _graphics = null;
            for (var index = 0; index < document.PageCount; index++)
            {
                using var footer = XGraphics.FromPdfPage(document.Pages[index], XGraphicsPdfPageOptions.Append);
                footer.DrawLine(new XPen(XColor.FromArgb(210, 220, 227)), Left, 792, Left + Width, 792);
                footer.DrawString("COMPROVANTE DE ATENDIMENTO", _small, _muted, new XPoint(Left, 808));
                footer.DrawString($"Página {index + 1} de {document.PageCount}", _small, _muted, new XRect(Left, 799, Width, 12), XStringFormats.CenterRight);
            }
        }

        private void NewPage()
        {
            _graphics?.Dispose();
            var page = document.AddPage();
            page.Size = PageSize.A4;
            _graphics = XGraphics.FromPdfPage(page);
            Graphics.DrawRectangle(_ink, Left, 35, Width, 67);
            Graphics.DrawString("ORDEM DE SERVIÇO", _title, XBrushes.White, new XPoint(Left + 14, 64));
            Graphics.DrawString(order.Id.ToString().ToUpperInvariant(), _small, XBrushes.White, new XPoint(Left + 14, 84));
            _y = 125;
        }

        private void EnsureSpace(double height)
        {
            if (_y + height > Bottom)
                NewPage();
        }

        private void Paragraph(string text, XFont font, XBrush brush)
        {
            foreach (var line in Wrap(text, Width, font))
            {
                EnsureSpace(15);
                Graphics.DrawString(line, font, brush, new XPoint(Left, _y + 10));
                _y += 15;
            }
        }

        private void TableHeader()
        {
            EnsureSpace(45);
            Graphics.DrawRectangle(_light, Left, _y, Width, 25);
            Graphics.DrawString("SERVIÇO / PEÇA", _bold, _ink, new XPoint(Left + 8, _y + 16));
            Graphics.DrawString("QTD.", _bold, _ink, new XPoint(Left + 266, _y + 16));
            Graphics.DrawString("UNITÁRIO", _bold, _ink, new XPoint(Left + 310, _y + 16));
            Graphics.DrawString("SUBTOTAL", _bold, _ink, new XPoint(Left + 416, _y + 16));
            _y += 31;
        }

        private void DrawItem(OrderItemResponse item)
        {
            var lines = Wrap(item.Name, 247, _text).ToList();
            var height = Math.Max(32, lines.Count * 13 + 17);
            if (_y + height > Bottom)
            {
                NewPage();
                TableHeader();
            }
            for (var index = 0; index < lines.Count; index++)
            {
                Graphics.DrawString(lines[index], _text, _ink, new XPoint(Left + 8, _y + 10 + index * 13));
            }
            Graphics.DrawString(item.Kind == ItemKind.Servico ? "Mão de obra" : "Peça", _small, _muted, new XPoint(Left + 8, _y + height - 5));
            Graphics.DrawString(item.Quantity.ToString(_culture), _text, _ink, new XRect(Left + 263, _y, 35, 15), XStringFormats.CenterRight);
            Graphics.DrawString(item.UnitPrice.ToString("N2", _culture), _text, _ink, new XRect(Left + 300, _y, 92, 15), XStringFormats.CenterRight);
            Graphics.DrawString((item.UnitPrice * item.Quantity).ToString("N2", _culture), _text, _ink, new XRect(Left + 398, _y, Width - 406, 15), XStringFormats.CenterRight);
            _y += height + 5;
            Graphics.DrawLine(new XPen(XColor.FromArgb(224, 231, 236)), Left, _y, Left + Width, _y);
            _y += 8;
        }

        private IEnumerable<string> Wrap(string text, double width, XFont font)
        {
            foreach (var paragraph in text.Replace("\r", "").Split('\n'))
            {
                var line = "";
                foreach (var character in paragraph.Replace('\t', ' '))
                {
                    if (Graphics.MeasureString(line + character, font).Width > width && line.Length > 0)
                    {
                        var split = line.LastIndexOf(' ');
                        if (split > 0)
                        {
                            yield return line[..split];
                            line = line[(split + 1)..];
                        }
                        else
                        {
                            yield return line;
                            line = "";
                        }
                    }
                    line += character;
                }
                yield return line;
            }
        }

        private static string StatusLabel(OrderStatus status) => status switch
        {
            OrderStatus.Aberta => "Aberta",
            OrderStatus.EmAndamento => "Em andamento",
            OrderStatus.AguardandoPeca => "Aguardando peça",
            OrderStatus.Concluida => "Concluída",
            OrderStatus.Cancelada => "Cancelada",
            _ => status.ToString()
        };

        public void Dispose() => _graphics?.Dispose();
    }
}
