using SriExtractor.Desktop.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SriExtractor.Desktop.Services;

public interface IOracleProveedorService
{
    List<ProveedorOracle> GetProveedores(bool testOnly = false);
}

public interface IOracleFacturaPagoService
{
    List<FacturaPagoRow> GetFacturasPago();
}

public interface ISriRecibidosService
{
    Task<List<SriReceivedRow>> DownloadAllByDateAsync(string storageStatePath, int year, int month, int day);
}

public interface ISriSessionService : IAsyncDisposable
{
    Task OpenLoginAutoSaveAndCloseAsync(string ruc, string password);
    Task SaveSessionAndCloseAsync();
}
