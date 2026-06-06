using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class AdministrareComenziPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<AdminComanda> comenzi = new List<AdminComanda>();
        private readonly List<UtilizatorSelect> utilizatori = new List<UtilizatorSelect>();

        public AdministrareComenziPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            DbSchema.AsiguraSchema();
            CmbStatus.SelectedIndex = 0;
            IncarcaUtilizatori();
            IncarcaComenzi();
        }

        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaUtilizatori();
            IncarcaComenzi();
        }

        private void BtnCreeaza_Click(object sender, RoutedEventArgs e)
        {
            if (CmbUtilizatori.SelectedValue == null)
            {
                MessageBox.Show("Selecteaza clientul pentru comanda.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
INSERT INTO Comanda (ID, ID_Utilizator, Status, Total, Observatii)
VALUES (@ID, @ID_Utilizator, @Status, 0, @Observatii)";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", Guid.NewGuid());
                cmd.Parameters.AddWithValue("@ID_Utilizator", Convert.ToInt32(CmbUtilizatori.SelectedValue));
                cmd.Parameters.AddWithValue("@Status", TextComboBox(CmbStatus));
                cmd.Parameters.AddWithValue("@Observatii", TxtObservatii.Text.Trim());
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Comanda creata", "Utilizator: " + utilizatorCurent.Email);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la crearea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizeaza_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out AdminComanda comanda))
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
UPDATE Comanda
SET Status = @Status,
    Observatii = @Observatii,
    Total = ISNULL((
        SELECT SUM(cp.Cantitate * p.Pret)
        FROM ComandaProduse cp
        INNER JOIN Produs p ON cp.ID_Produs = p.ID
        WHERE cp.ID_Comanda = @ID
    ), 0)
WHERE ID = @ID";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", comanda.ID);
                cmd.Parameters.AddWithValue("@Status", TextComboBox(CmbStatus));
                cmd.Parameters.AddWithValue("@Observatii", TxtObservatii.Text.Trim());
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Comanda actualizata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + comanda.ID);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSterge_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out AdminComanda comanda))
            {
                return;
            }

            if (MessageBox.Show("Sigur vrei sa stergi comanda selectata? Se sterg si produsele, platile si factura asociata.", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                Executa(conn, transaction, "DELETE FROM Plati WHERE ID_Comanda = @ID", comanda.ID);
                Executa(conn, transaction, "DELETE FROM Facturi WHERE ID_Comanda = @ID", comanda.ID);
                Executa(conn, transaction, "DELETE FROM ComandaProduse WHERE ID_Comanda = @ID", comanda.ID);
                Executa(conn, transaction, "DELETE FROM Comanda WHERE ID = @ID", comanda.ID);

                transaction.Commit();

                AppLogger.Scrie("Comanda stearsa", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + comanda.ID);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la stergerea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGenereazaFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out AdminComanda comanda))
            {
                return;
            }

            if (comanda.NumarProduse == 0)
            {
                MessageBox.Show("Nu poti genera factura pentru o comanda fara produse.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                GenereazaFactura(comanda.ID);
                AppLogger.Scrie("Factura generata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + comanda.ID);
                IncarcaComenzi();
                MessageBox.Show("Factura a fost generata sau era deja existenta.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la generarea facturii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComenziGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComenziGrid.SelectedItem is not AdminComanda comanda)
            {
                return;
            }

            TxtObservatii.Text = comanda.Observatii;
            SeteazaStatus(comanda.Status);
            CmbUtilizatori.SelectedValue = comanda.IDUtilizator;
        }

        private void IncarcaUtilizatori()
        {
            utilizatori.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT Id, NumeComplet, Email FROM Utilizatori ORDER BY NumeComplet";
                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["Id"]);
                    string nume = Convert.ToString(reader["NumeComplet"]) ?? "";
                    string email = Convert.ToString(reader["Email"]) ?? "";
                    utilizatori.Add(new UtilizatorSelect(id, nume + " (" + email + ")"));
                }

                CmbUtilizatori.ItemsSource = null;
                CmbUtilizatori.ItemsSource = utilizatori;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea utilizatorilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
       c.ID_Utilizator,
       u.NumeComplet,
       u.Email,
       c.DataComanda,
       c.Status,
       ISNULL(NULLIF(c.Total, 0), ISNULL(SUM(cp.Cantitate * p.Pret), 0)) AS Total,
       COUNT(cp.ID) AS NumarProduse,
       ISNULL(c.Observatii, '') AS Observatii,
       CASE WHEN MAX(CASE WHEN f.ID IS NULL THEN 0 ELSE 1 END) = 1 THEN 1 ELSE 0 END AS AreFactura
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs p ON cp.ID_Produs = p.ID
LEFT JOIN Facturi f ON c.ID = f.ID_Comanda
GROUP BY c.ID, c.ID_Utilizator, u.NumeComplet, u.Email, c.DataComanda, c.Status, c.Total, c.Observatii
ORDER BY c.DataComanda DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    comenzi.Add(new AdminComanda(
                        (Guid)reader["ID"],
                        Convert.ToInt32(reader["ID_Utilizator"]),
                        Convert.ToString(reader["NumeComplet"]) ?? "",
                        Convert.ToString(reader["Email"]) ?? "",
                        Convert.ToDateTime(reader["DataComanda"]),
                        Convert.ToString(reader["Status"]) ?? "",
                        Convert.ToDecimal(reader["Total"]),
                        Convert.ToInt32(reader["NumarProduse"]),
                        Convert.ToString(reader["Observatii"]) ?? "",
                        Convert.ToInt32(reader["AreFactura"]) == 1));
                }

                ComenziGrid.ItemsSource = null;
                ComenziGrid.ItemsSource = comenzi;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea comenzilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenereazaFactura(Guid comandaId)
        {
            using SqlConnection conn = new SqlConnection(ConnectionString);
            conn.Open();
            using SqlTransaction transaction = conn.BeginTransaction();

            string updateTotal = @"
UPDATE Comanda
SET Total = ISNULL((
    SELECT SUM(cp.Cantitate * p.Pret)
    FROM ComandaProduse cp
    INNER JOIN Produs p ON cp.ID_Produs = p.ID
    WHERE cp.ID_Comanda = @ID
), 0)
WHERE ID = @ID";
            Executa(conn, transaction, updateTotal, comandaId);

            string insertFactura = @"
IF NOT EXISTS (SELECT 1 FROM Facturi WHERE ID_Comanda = @ID)
BEGIN
    INSERT INTO Facturi (ID_Comanda, NumarFactura, Total, Status)
    SELECT ID, @NumarFactura, Total, 'Emisa'
    FROM Comanda
    WHERE ID = @ID
END";

            using SqlCommand cmd = new SqlCommand(insertFactura, conn, transaction);
            cmd.Parameters.AddWithValue("@ID", comandaId);
            cmd.Parameters.AddWithValue("@NumarFactura", "FA-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            cmd.ExecuteNonQuery();

            transaction.Commit();
        }

        private void Executa(SqlConnection conn, SqlTransaction transaction, string query, Guid id)
        {
            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@ID", id);
            cmd.ExecuteNonQuery();
        }

        private bool SelecteazaComanda(out AdminComanda comanda)
        {
            if (ComenziGrid.SelectedItem is AdminComanda comandaSelectata)
            {
                comanda = comandaSelectata;
                return true;
            }

            comanda = new AdminComanda(Guid.Empty, 0, "", "", DateTime.Now, "", 0, 0, "", false);
            MessageBox.Show("Selecteaza o comanda.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private string TextComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString() ?? "";
            }

            return "InCos";
        }

        private void SeteazaStatus(string status)
        {
            for (int i = 0; i < CmbStatus.Items.Count; i++)
            {
                if (CmbStatus.Items[i] is ComboBoxItem item && string.Equals(item.Content.ToString(), status, StringComparison.OrdinalIgnoreCase))
                {
                    CmbStatus.SelectedIndex = i;
                    return;
                }
            }

            CmbStatus.SelectedIndex = 0;
        }

        private class UtilizatorSelect
        {
            public int ID { get; set; }
            public string Afisare { get; set; }

            public UtilizatorSelect(int id, string afisare)
            {
                ID = id;
                Afisare = afisare;
            }
        }
    }
}
