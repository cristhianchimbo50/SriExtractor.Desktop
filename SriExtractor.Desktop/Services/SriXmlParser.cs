using SriExtractor.Desktop.Models;
using System.Globalization;
using System.Xml.Linq;

namespace SriExtractor.Desktop.Services;

public static class SriXmlParser
{
    public static (SriInvoiceHeader header, List<SriInvoiceItem> items) ParseFacturaAutorizada(string xmlContent, string xmlPath)
    {
        var doc = XDocument.Parse(xmlContent);

        var autorizacion = doc.Descendants("autorizacion").FirstOrDefault();
        if (autorizacion == null) throw new InvalidOperationException("XML inválido: no existe <autorizacion>.");

        var numeroAut = autorizacion.Element("numeroAutorizacion")?.Value ?? "";
        var fechaAutStr = autorizacion.Element("fechaAutorizacion")?.Value ?? "";
        DateTime.TryParse(fechaAutStr, out var fechaAut);

        var comprobanteCdata = autorizacion.Element("comprobante")?.Value;
        if (string.IsNullOrWhiteSpace(comprobanteCdata))
            throw new InvalidOperationException("XML inválido: no existe <comprobante>.");

        var facturaDoc = XDocument.Parse(comprobanteCdata);
        var factura = facturaDoc.Root;
        if (factura == null || factura.Name.LocalName != "factura")
            throw new InvalidOperationException("XML inválido: no existe <factura>.");

        string GetDesc(string name) => factura.Descendants(name).FirstOrDefault()?.Value ?? "";

        var infoTrib = factura.Descendants("infoTributaria").FirstOrDefault();
        var claveAcceso = infoTrib?.Element("claveAcceso")?.Value ?? "";

        var razonSocial = infoTrib?.Element("razonSocial")?.Value ?? "";
        var nombreComercial = infoTrib?.Element("nombreComercial")?.Value ?? "";
        var ruc = infoTrib?.Element("ruc")?.Value ?? "";

        var infoFactura = factura.Descendants("infoFactura").FirstOrDefault();
        var fechaEmisionStr = infoFactura?.Element("fechaEmision")?.Value ?? "";
        DateTime.TryParse(fechaEmisionStr, out var fechaEmision);

        var estab = infoTrib?.Element("estab")?.Value ?? "";
        var ptoEmi = infoTrib?.Element("ptoEmi")?.Value ?? "";
        var sec = infoTrib?.Element("secuencial")?.Value ?? "";
        var numeroFactura = $"{estab}-{ptoEmi}-{sec}";

        var totalStr = infoFactura?.Element("importeTotal")?.Value ?? "0";
        var total = ParseDec(totalStr);

        var header = new SriInvoiceHeader
        {
            XmlPath = xmlPath,
            ClaveAcceso = claveAcceso,
            NumeroAutorizacion = numeroAut,
            FechaAutorizacion = fechaAut,
            RucEmisor = ruc,
            RazonSocialEmisor = razonSocial,
            NombreComercialEmisor = nombreComercial,
            NumeroFactura = numeroFactura,
            FechaEmision = fechaEmision,
            Total = total
        };

        var items = new List<SriInvoiceItem>();
        var detalles = factura.Descendants("detalle");

        foreach (var det in detalles)
        {
            var codigoPrincipal = det.Element("codigoPrincipal")?.Value ?? "";
            var codigoAux = det.Element("codigoAuxiliar")?.Value ?? "";
            var descripcion = det.Element("descripcion")?.Value ?? "";

            var cantidad = ParseDec(det.Element("cantidad")?.Value ?? "0");
            var precioUnit = ParseDec(det.Element("precioUnitario")?.Value ?? "0");
            var descuento = ParseDec(det.Element("descuento")?.Value ?? "0");
            var precioTotalSinImp = ParseDec(det.Element("precioTotalSinImpuesto")?.Value ?? "0");

            decimal baseImp = 0;
            decimal iva = 0;

            var impIva = det.Descendants("impuesto")
                .FirstOrDefault(x => (x.Element("codigo")?.Value ?? "") == "2");

            if (impIva != null)
            {
                baseImp = ParseDec(impIva.Element("baseImponible")?.Value ?? "0");
                iva = ParseDec(impIva.Element("valor")?.Value ?? "0");
            }

            items.Add(new SriInvoiceItem
            {
                CodigoPrincipal = codigoPrincipal,
                CodigoAuxiliar = codigoAux,
                Descripcion = descripcion,
                Cantidad = cantidad,
                PrecioUnitario = precioUnit,
                Descuento = descuento,
                BaseImponible = baseImp,
                Iva = iva,
                TotalLinea = precioTotalSinImp
            });
        }

        return (header, items);
    }

    private static decimal ParseDec(string s)
    {
        s = s.Trim();
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv)) return inv;
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-EC"), out var ec)) return ec;
        return 0;
    }
}
