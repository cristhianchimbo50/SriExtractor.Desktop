using SriExtractor.Desktop.Models;
using SriExtractor.Desktop.Services;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SriExtractor.Desktop;

public partial class InvoiceDetailWindow : Window
{
    public InvoiceDetailWindow(string xmlPath)
    {
        InitializeComponent();
        GridItems.PreviewKeyDown += GridItems_PreviewKeyDown;
        LoadXml(xmlPath);
    }

    private void LoadXml(string xmlPath)
    {
        var xmlContent = File.ReadAllText(xmlPath);
        var (header, items) = SriXmlParser.ParseFacturaAutorizada(xmlContent, xmlPath);

        var groupedItems = items
            .GroupBy(i => NormalizeCodeKey(i))
            .Select(g => BuildAggregatedItem(g))
            .ToList();

        var numeroFacturaSinGuiones = (header.NumeroFactura ?? string.Empty).Replace("-", "");

        TxtRazonSocial.Text = header.RazonSocialEmisor;
        TxtNombreComercial.Text = header.NombreComercialEmisor;
        TxtRuc.Text = header.RucEmisor;
        TxtFactura.Text = numeroFacturaSinGuiones;
        TxtFechaEmision.Text = header.FechaEmision.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        TxtTotal.Text = header.Total.ToString("0.##", CultureInfo.InvariantCulture);

        TxtAutorizacion.Text = header.NumeroAutorizacion;
        TxtFechaAutorizacion.Text = header.FechaAutorizacion.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        TxtClave.Text = header.ClaveAcceso;

        GridItems.ItemsSource = groupedItems;

        GridTotals.ItemsSource = new[]
        {
            new { Label = "SUBTOTAL 15%", Value = header.Subtotal15 },
            new { Label = "SUBTOTAL NO OBJETO DE IVA", Value = header.SubtotalNoObjetoIva },
            new { Label = "SUBTOTAL EXENTO DE IVA", Value = header.SubtotalExentoIva },
            new { Label = "SUBTOTAL SIN IMPUESTOS", Value = header.SubtotalSinImpuestos },
            new { Label = "TOTAL DESCUENTO", Value = header.TotalDescuento },
            new { Label = "IVA 15%", Value = header.Iva15 },
            new { Label = "VALOR TOTAL", Value = header.Total }
        };
    }

    private static string NormalizeCodeKey(SriInvoiceItem item)
    {
        var principal = item.CodigoPrincipal?.Trim();
        var aux = item.CodigoAuxiliar?.Trim();
        var key = string.IsNullOrWhiteSpace(principal) ? aux : principal;
        return key?.ToUpperInvariant() ?? string.Empty;
    }

    private static SriInvoiceItem BuildAggregatedItem(IGrouping<string, SriInvoiceItem> group)
    {
        var first = group.First();

        var cantidadSum = group.Sum(x => x.Cantidad);
        var totalLineaSum = group.Sum(x => x.TotalLinea);
        var descuentoSum = group.Sum(x => x.Descuento);
        var baseImponibleSum = group.Sum(x => x.BaseImponible);
        var ivaSum = group.Sum(x => x.Iva);

        var precioUnitarioEfectivo =
            cantidadSum == 0 ? 0 : totalLineaSum / cantidadSum;

        var precioUnitarioConDesc =
            cantidadSum == 0 ? 0 : (totalLineaSum + descuentoSum) / cantidadSum;

        return new SriInvoiceItem
        {
            CodigoPrincipal = first.CodigoPrincipal,
            CodigoAuxiliar = first.CodigoAuxiliar,
            Descripcion = first.Descripcion,
            Cantidad = cantidadSum,
            PrecioUnitario = precioUnitarioEfectivo,
            Descuento = descuentoSum,
            BaseImponible = baseImponibleSum,
            Iva = ivaSum,
            TotalLinea = totalLineaSum,
            PrecioUnitarioConDescuento = precioUnitarioConDesc
        };
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        text = (text ?? string.Empty).Trim();

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return true;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            return true;

        return false;
    }

    private static string FormatForClipboard(string text)
    {
        if (TryParseDecimal(text, out var dec))
            return dec.ToString("0.##", CultureInfo.InvariantCulture);

        return (text ?? string.Empty).Trim();
    }

    private static string FormatForAnsiLegacy(string text)
    {
        if (TryParseDecimal(text, out var dec))
            return dec.ToString("0.##", CultureInfo.InvariantCulture);

        return (text ?? string.Empty).Trim();
    }

    private void GridItems_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            CopySelectionToClipboard();
        }
    }

    private void CopySelectionToClipboard()
    {
        if (GridItems.SelectedCells == null || GridItems.SelectedCells.Count == 0)
            return;

        var cells = GridItems.SelectedCells
            .OrderBy(c => GridItems.Items.IndexOf(c.Item))
            .ThenBy(c => c.Column.DisplayIndex)
            .ToList();

        var sb = new StringBuilder();

        int currentRowIndex = -1;
        bool firstCellInRow = true;

        foreach (var cell in cells)
        {
            var rowIndex = GridItems.Items.IndexOf(cell.Item);

            if (rowIndex != currentRowIndex)
            {
                if (currentRowIndex != -1)
                    sb.AppendLine();

                currentRowIndex = rowIndex;
                firstCellInRow = true;
            }

            if (!firstCellInRow)
                sb.Append('\t');

            var raw = cell.Column.GetCellContent(cell.Item);

            string text = raw switch
            {
                TextBlock tb => tb.Text,
                TextBox tx => tx.Text,
                _ => cell.Column.OnCopyingCellClipboardContent(cell.Item)?.ToString() ?? ""
            };

            sb.Append(FormatForClipboard(text));
            firstCellInRow = false;
        }

        var finalText = sb.ToString();

        var dataObj = new DataObject();
        dataObj.SetData(DataFormats.UnicodeText, finalText);
        dataObj.SetData(DataFormats.Text, finalText);
        dataObj.SetData(DataFormats.OemText, finalText);

        Clipboard.SetDataObject(dataObj, true);
    }
}