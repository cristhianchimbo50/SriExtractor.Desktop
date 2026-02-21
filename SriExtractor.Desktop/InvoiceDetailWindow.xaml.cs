using SriExtractor.Desktop.Models;
using SriExtractor.Desktop.Services;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace SriExtractor.Desktop;

public partial class InvoiceDetailWindow : Window
{
    public InvoiceDetailWindow(string xmlPath)
    {
        InitializeComponent();
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
}