# SriExtractor.Desktop

Aplicación WPF (.NET 8) para descargar comprobantes autorizados del SRI, almacenarlos localmente y cruzarlos con datos de Oracle.

## Funcionalidad
- Inicio de sesión en SRI y guardado de sesión local.
- Descarga de XML por fecha y almacenamiento en `%LOCALAPPDATA%\SriExtractor\Xml`.
- Carga y visualización de XML locales sin re-descargar.
- Cruce con:
  - Proveedores (`ProveedorService`)
  - Facturas de pago (`FacturaPagoService`)
