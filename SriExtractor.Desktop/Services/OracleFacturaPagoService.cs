using Oracle.ManagedDataAccess.Client;
using SriExtractor.Desktop.Models;
using System.Collections.Generic;

namespace SriExtractor.Desktop.Services;

public class OracleFacturaPagoService : IOracleFacturaPagoService
{
    private readonly string _connectionString;

    public OracleFacturaPagoService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<FacturaPagoRow> GetFacturasPago()
    {
        var list = new List<FacturaPagoRow>();

        const string query = @"SELECT M.CO_NUMERO,
       CASE
           WHEN PR.RF_CODIGO = 0
            AND PR.RF_CODIGO2 = 0
           THEN NULL
           ELSE MAX(P.PM_NROSEC)
       END AS PM_NROSEC,
       MAX(C.CO_FACPRO)  AS CO_FACPRO,
       MAX(PR.PV_RUCCI)  AS PV_RUCCI,
       MAX(PR.PV_RAZONS) AS PV_RAZONS,
       MAX(PR.RF_CODIGO)  AS RF_CODIGO,
       MAX(PR.RF_CODIGO2) AS RF_CODIGO2
FROM CP_MOVIM  M,
     CP_PAGO   P,
     IN_COMPRA C,
     IN_PROVE  PR
WHERE M.MP_CODIGO = P.MP_CODIGO
  AND M.CO_NUMERO = C.CO_NUMERO
  AND C.PV_CODIGO = PR.PV_CODIGO
  AND M.CO_NUMERO IS NOT NULL
GROUP BY M.CO_NUMERO, PR.RF_CODIGO, PR.RF_CODIGO2
ORDER BY M.CO_NUMERO DESC";

        using var connection = new OracleConnection(_connectionString);
        connection.Open();

        using var command = new OracleCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var rf1 = reader["RF_CODIGO"]?.ToString()?.Trim() ?? "";
            var rf2 = reader["RF_CODIGO2"]?.ToString()?.Trim() ?? "";
            var pm = reader["PM_NROSEC"]?.ToString()?.Trim() ?? "";
            var coNumero = reader["CO_NUMERO"]?.ToString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(coNumero))
                coNumero = "SIN REGISTRO";

            if ((rf1 == "0" || string.IsNullOrWhiteSpace(rf1)) && (rf2 == "0" || string.IsNullOrWhiteSpace(rf2)) && string.IsNullOrWhiteSpace(pm))
                pm = "GRAN CONTRIBUYENTE";

            list.Add(new FacturaPagoRow
            {
                CoNumero = coNumero,
                PmNrosec = pm,
                CoFacpro = reader["CO_FACPRO"]?.ToString()?.Trim() ?? "",
                PvRucci = reader["PV_RUCCI"]?.ToString()?.Trim() ?? "",
                PvRazons = reader["PV_RAZONS"]?.ToString()?.Trim() ?? "",
                RfCodigo = rf1,
                RfCodigo2 = rf2
            });
        }

        return list;
    }
}
