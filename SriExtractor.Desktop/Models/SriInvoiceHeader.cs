namespace SriExtractor.Desktop.Models;

public class SriInvoiceHeader
{
    public string ClaveAcceso { get; set; } = "";
    public string NumeroAutorizacion { get; set; } = "";
    public DateTime FechaAutorizacion { get; set; }

    public string RucEmisor { get; set; } = "";
    public string RazonSocialEmisor { get; set; } = "";
    public string NombreComercialEmisor { get; set; } = "";

    public string NumeroFactura { get; set; } = "";
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }

    public string XmlPath { get; set; } = "";
}
