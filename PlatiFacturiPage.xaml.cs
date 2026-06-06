using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class PlatiFacturiPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<PlataFactura> platiFacturi = new List<PlataFactura>();
        private readonly List<ComandaSelect> comenzi = new List<ComandaSelect>();

        public PlatiFacturiPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            DbSchema.AsiguraSchema();
            CmbMetoda.SelectedIndex = 0;
            CmbStatusPlata.SelectedIndex = 0;
            IncarcaComenzi();
            IncarcaPlatiFacturi();
        }

        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaComenzi();
            IncarcaPlatiFacturi();
        }

        private void BtnGenereazaFactura_Click(object sender, RoutedEventArgs e)
        {
            if (CmbComenzi.SelectedValue == null)
            {
                MessageBox.Show("Selecteaza comanda pentru facturare.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Guid idComanda = (Guid)CmbComenzi.SelectedValue;

            try
            {
                GenereazaFactura(idComanda);
                AppLogger.Scrie("Factura generata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
                MessageBox.Show("Factura a fost generata sau era deja existenta.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la generarea facturii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnInregistreazaPlata_Click(object sender, RoutedEventArgs e)
        {
            if (CmbComenzi.SelectedValue == null)
            {
                MessageBox.Show("Selecteaza comanda/factura pentru plata.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CitesteSuma(out decimal suma))
            {
                return;
            }

            Guid idComanda = (Guid)CmbComenzi.SelectedValue;

            try
            {
                GenereazaFactura(idComanda);

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                Guid idFactura = ObtineFactura(conn, idComanda);
                string query = @"
INSERT INTO Plati (ID_Comanda, ID_Factura, Suma, Metoda, Status, Referinta)
VALUES (@ID_Comanda, @ID_Factura, @Suma, @Metoda, @Status, @Referinta)";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
                cmd.Parameters.AddWithValue("@ID_Factura", idFactura);
                cmd.Parameters.AddWithValue("@Suma", suma);
                cmd.Parameters.AddWithValue("@Metoda", TextComboBox(CmbMetoda));
                cmd.Parameters.AddWithValue("@Status", TextComboBox(CmbStatusPlata));
                cmd.Parameters.AddWithValue("@Referinta", "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                cmd.ExecuteNonQuery();

                ActualizeazaStatusFacturaSiComanda(conn, idFactura, idComanda);

                AppLogger.Scrie("Plata inregistrata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda + ", suma: " + suma);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la inregistrarea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizeazaPlata_Click(object sender, RoutedEventArgs e)
        {
            if (PlatiFacturiGrid.SelectedItem is not PlataFactura plata || plata.IDPlata == null)
            {
                MessageBox.Show("Selecteaza o plata existenta.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CitesteSuma(out decimal suma))
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "UPDATE Plati SET Suma = @Suma, Metoda = @Metoda, Status = @Status WHERE ID = @ID";
                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", plata.IDPlata.Value);
                cmd.Parameters.AddWithValue("@Suma", suma);
                cmd.Parameters.AddWithValue("@Metoda", TextComboBox(CmbMetoda));
                cmd.Parameters.AddWithValue("@Status", TextComboBox(CmbStatusPlata));
                cmd.ExecuteNonQuery();

                if (plata.IDFactura != null)
                {
                    ActualizeazaStatusFacturaSiComanda(conn, plata.IDFactura.Value, plata.IDComanda);
                }

                AppLogger.Scrie("Plata actualizata", "Utilizator: " + utilizatorCurent.Email + ", plata: " + plata.IDPlata);
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStergePlata_Click(object sender, RoutedEventArgs e)
        {
            if (PlatiFacturiGrid.SelectedItem is not PlataFactura plata || plata.IDPlata == null)
            {
                MessageBox.Show("Selecteaza o plata existenta.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Sigur vrei sa stergi plata selectata?", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "DELETE FROM Plati WHERE ID = @ID";
                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", plata.IDPlata.Value);
                cmd.ExecuteNonQuery();

                if (plata.IDFactura != null)
                {
                    ActualizeazaStatusFacturaSiComanda(conn, plata.IDFactura.Value, plata.IDComanda);
                }

                AppLogger.Scrie("Plata stearsa", "Utilizator: " + utilizatorCurent.Email + ", plata: " + plata.IDPlata);
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la stergerea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnuleazaFactura_Click(object sender, RoutedEventArgs e)
        {
            if (PlatiFacturiGrid.SelectedItem is not PlataFactura plata || plata.IDFactura == null)
            {
                MessageBox.Show("Selecteaza o factura.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "UPDATE Facturi SET Status = 'Anulata' WHERE ID = @ID";
                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", plata.IDFactura.Value);
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Factura anulata", "Utilizator: " + utilizatorCurent.Email + ", factura: " + plata.NumarFactura);
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la anularea facturii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlatiFacturiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatiFacturiGrid.SelectedItem is not PlataFactura plata)
            {
                return;
            }

            CmbComenzi.SelectedValue = plata.IDComanda;
            TxtSumaPlata.Text = plata.SumaPlatita > 0 ? plata.SumaPlatita.ToString("0.00") : plata.TotalFactura.ToString("0.00");
            SeteazaCombo(CmbMetoda, string.IsNullOrWhiteSpace(plata.Metoda) ? "Card" : plata.Metoda);
            SeteazaCombo(CmbStatusPlata, string.IsNullOrWhiteSpace(plata.StatusPlata) ? "InAsteptare" : plata.StatusPlata);
        }

        private void IncarcaComenzi()
        {
            comenzi.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
SELECT c.ID,
       u.NumeComplet,
       c.Status,
       ISNULL(NULLIF(c.Total, 0), ISNULL(SUM(cp.Cantitate * p.Pret), 0)) AS Total
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs p ON cp.ID_Produs = p.ID
WHERE c.Status IN ('Finalizata', 'InProcesare', 'Livrata', 'Achitata')
GROUP BY c.ID, u.NumeComplet, c.Status, c.Total
ORDER BY u.NumeComplet";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Guid id = (Guid)reader["ID"];
                    string client = Convert.ToString(reader["NumeComplet"]) ?? "";
                    string status = Convert.ToString(reader["Status"]) ?? "";
                    decimal total = Convert.ToDecimal(reader["Total"]);
                    comenzi.Add(new ComandaSelect(id, client + " - " + total.ToString("0.00") + " lei - " + status));
                }

                CmbComenzi.ItemsSource = null;
                CmbComenzi.ItemsSource = comenzi;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea comenzilor pentru facturare: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncarcaPlatiFacturi()
        {
            platiFacturi.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
SELECT p.ID AS IDPlata,
       f.ID AS IDFactura,
       c.ID AS IDComanda,
       ISNULL(f.NumarFactura, 'Fara factura') AS NumarFactura,
       u.NumeComplet AS Client,
       c.DataComanda,
       p.DataPlata,
       ISNULL(f.Total, ISNULL(NULLIF(c.Total, 0), ISNULL(SUM(cp.Cantitate * pr.Pret), 0))) AS TotalFactura,
       ISNULL(p.Suma, 0) AS SumaPlatita,
       ISNULL(p.Metoda, '') AS Metoda,
       ISNULL(p.Status, '') AS StatusPlata,
       ISNULL(f.Status, '') AS StatusFactura
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN Facturi f ON c.ID = f.ID_Comanda
LEFT JOIN Plati p ON f.ID = p.ID_Factura
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs pr ON cp.ID_Produs = pr.ID
WHERE f.ID IS NOT NULL
GROUP BY p.ID, f.ID, c.ID, f.NumarFactura, u.NumeComplet, c.DataComanda, p.DataPlata, f.Total, c.Total, p.Suma, p.Metoda, p.Status, f.Status
ORDER BY c.DataComanda DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Guid? idPlata = reader["IDPlata"] == DBNull.Value ? null : (Guid?)reader["IDPlata"];
                    Guid? idFactura = reader["IDFactura"] == DBNull.Value ? null : (Guid?)reader["IDFactura"];
                    DateTime? dataPlata = reader["DataPlata"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["DataPlata"]);

                    platiFacturi.Add(new PlataFactura(
                        idPlata,
                        idFactura,
                        (Guid)reader["IDComanda"],
                        Convert.ToString(reader["NumarFactura"]) ?? "",
                        Convert.ToString(reader["Client"]) ?? "",
                        Convert.ToDateTime(reader["DataComanda"]),
                        dataPlata,
                        Convert.ToDecimal(reader["TotalFactura"]),
                        Convert.ToDecimal(reader["SumaPlatita"]),
                        Convert.ToString(reader["Metoda"]) ?? "",
                        Convert.ToString(reader["StatusPlata"]) ?? "",
                        Convert.ToString(reader["StatusFactura"]) ?? ""));
                }

                PlatiFacturiGrid.ItemsSource = null;
                PlatiFacturiGrid.ItemsSource = platiFacturi;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea platilor/facturilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenereazaFactura(Guid idComanda)
        {
            using SqlConnection conn = new SqlConnection(ConnectionString);
            conn.Open();

            string updateTotal = @"
UPDATE Comanda
SET Total = ISNULL((
    SELECT SUM(cp.Cantitate * p.Pret)
    FROM ComandaProduse cp
    INNER JOIN Produs p ON cp.ID_Produs = p.ID
    WHERE cp.ID_Comanda = @ID
), 0)
WHERE ID = @ID";
            using (SqlCommand updateCmd = new SqlCommand(updateTotal, conn))
            {
                updateCmd.Parameters.AddWithValue("@ID", idComanda);
                updateCmd.ExecuteNonQuery();
            }

            string query = @"
IF NOT EXISTS (SELECT 1 FROM Facturi WHERE ID_Comanda = @ID)
BEGIN
    INSERT INTO Facturi (ID_Comanda, NumarFactura, Total, Status)
    SELECT ID, @NumarFactura, Total, 'Emisa'
    FROM Comanda
    WHERE ID = @ID
END";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ID", idComanda);
            cmd.Parameters.AddWithValue("@NumarFactura", "FA-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            cmd.ExecuteNonQuery();
        }

        private Guid ObtineFactura(SqlConnection conn, Guid idComanda)
        {
            string query = "SELECT ID FROM Facturi WHERE ID_Comanda = @ID_Comanda";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
            object? rezultat = cmd.ExecuteScalar();
            return (Guid)(rezultat ?? Guid.Empty);
        }

        private void ActualizeazaStatusFacturaSiComanda(SqlConnection conn, Guid idFactura, Guid idComanda)
        {
            decimal totalFactura;
            decimal totalPlatit;

            using (SqlCommand cmd = new SqlCommand("SELECT Total FROM Facturi WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", idFactura);
                totalFactura = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(Suma), 0) FROM Plati WHERE ID_Factura = @ID AND Status = 'Platita'", conn))
            {
                cmd.Parameters.AddWithValue("@ID", idFactura);
                totalPlatit = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            string statusFactura = totalPlatit <= 0 ? "Emisa" : totalPlatit >= totalFactura ? "Platita" : "Partiala";

            using (SqlCommand cmd = new SqlCommand("UPDATE Facturi SET Status = @Status WHERE ID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", idFactura);
                cmd.Parameters.AddWithValue("@Status", statusFactura);
                cmd.ExecuteNonQuery();
            }

            if (statusFactura == "Platita")
            {
                using SqlCommand cmd = new SqlCommand("UPDATE Comanda SET Status = 'Achitata' WHERE ID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", idComanda);
                cmd.ExecuteNonQuery();
            }
        }

        private bool CitesteSuma(out decimal suma)
        {
            if (!decimal.TryParse(TxtSumaPlata.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out suma) &&
                !decimal.TryParse(TxtSumaPlata.Text.Trim(), out suma))
            {
                MessageBox.Show("Suma trebuie sa fie un numar valid.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (suma <= 0)
            {
                MessageBox.Show("Suma trebuie sa fie mai mare decat 0.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private string TextComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString() ?? "";
            }

            return "";
        }

        private void SeteazaCombo(ComboBox comboBox, string text)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && string.Equals(item.Content.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private class ComandaSelect
        {
            public Guid ID { get; set; }
            public string Afisare { get; set; }

            public ComandaSelect(Guid id, string afisare)
            {
                ID = id;
                Afisare = afisare;
            }
        }
    }
}
