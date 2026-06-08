using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace AutoPartsShop
{
   
    public static class GestionareComanda
    {
        // Valorile sunt definite intr-un singur loc pentru ca toate paginile
        // si toate tabelele din baza de date sa foloseasca aceleasi statusuri.
        public const string InCos = "InCos";
        public const string Finalizata = "Finalizata";
        public const string InProcesare = "InProcesare";
        public const string Livrata = "Livrata";
        public const string Anulata = "Anulata";
        public const string Achitata = "Achitata";

        //string cu toate statusurile 
        public static IReadOnlyList<string> Statusuri { get; } = new[]
        {
            InCos,
            Finalizata,
            InProcesare,
            Livrata,
            Anulata,
            Achitata
        };

        
        public static void SincronizeazaStatus(SqlConnection conn, SqlTransaction transaction, Guid idComanda, string status)
        {
            using SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Transaction = transaction;
            cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
            cmd.Parameters.AddWithValue("@Status", status);

            cmd.CommandText = "UPDATE Comanda SET Status = @Status WHERE ID = @ID_Comanda";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "UPDATE Facturi SET Status = @Status WHERE ID_Comanda = @ID_Comanda";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "UPDATE Plati SET Status = @Status WHERE ID_Comanda = @ID_Comanda";
            cmd.ExecuteNonQuery();
        }

        // Corecteaza datele mai vechi care aveau statusuri diferite intre comenzi, facturi si plati.
        public static void SincronizeazaDateExistente()
        {
            using SqlConnection conn = new SqlConnection(
                @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;");
            conn.Open();
            using SqlTransaction transaction = conn.BeginTransaction();

            // Mai intai transforma vechile statusuri de plata/factura in statusuri de comanda apoi copiaza statusul final al comenzii in Facturi si Plati.
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE c
SET Status = 'Achitata'
FROM Comanda c
WHERE EXISTS (
    SELECT 1
    FROM Plati p
    WHERE p.ID_Comanda = c.ID
      AND UPPER(LTRIM(RTRIM(p.Status))) IN ('PLATITA', 'ACHITATA')
);

UPDATE c
SET Status = 'Anulata'
FROM Comanda c
WHERE EXISTS (
    SELECT 1
    FROM Facturi f
    WHERE f.ID_Comanda = c.ID
      AND UPPER(LTRIM(RTRIM(f.Status))) = 'ANULATA'
);

UPDATE f
SET Status = c.Status
FROM Facturi f
INNER JOIN Comanda c ON c.ID = f.ID_Comanda;

UPDATE p
SET Status = c.Status
FROM Plati p
INNER JOIN Comanda c ON c.ID = p.ID_Comanda;", conn, transaction))
            {
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

       
        // Recalculeaza totalul comenzii si creeaza factura doar daca aceasta nu exista deja.
        // Factura noua mosteneste statusul curent al comenzii.
       
        public static void GenereazaFactura(Guid idComanda)
        {
            using SqlConnection conn = new SqlConnection(
                @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;");
            conn.Open();
            using SqlTransaction transaction = conn.BeginTransaction();

            // Recalcularea totalului si inserarea facturii, daca una esueaza, tranzactia nu salveaza o factura cu total incorect.
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE Comanda
SET Total = ISNULL((
    SELECT SUM(cp.Cantitate * p.Pret)
    FROM ComandaProduse cp
    INNER JOIN Produs p ON cp.ID_Produs = p.ID
    WHERE cp.ID_Comanda = @ID_Comanda
), 0)
WHERE ID = @ID_Comanda;

IF NOT EXISTS (SELECT 1 FROM Facturi WHERE ID_Comanda = @ID_Comanda)
BEGIN
    INSERT INTO Facturi (ID_Comanda, NumarFactura, Total, Status)
    SELECT ID, @NumarFactura, Total, Status
    FROM Comanda
    WHERE ID = @ID_Comanda
END", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
                cmd.Parameters.AddWithValue("@NumarFactura", "FA-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

     
    }
}
