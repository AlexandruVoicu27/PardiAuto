using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    // Permite personalului sa consulte, actualizeze, factureze si sa stearga comenzile.
    public partial class AdministrareComenziPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;

        private ComboBox ComboStatus => (ComboBox)FindName("CmbStatus");

        // Configureaza statusurile disponibile, sincronizeaza datele vechi si incarca comenzile.
        public AdministrareComenziPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            ComboStatus.ItemsSource = GestionareComanda.Statusuri;
            ComboStatus.SelectedItem = GestionareComanda.Finalizata;
            GestionareComanda.SincronizeazaDateExistente();
            IncarcaComenzi();
        }

        // Reciteste comenzile din baza de date.
        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaComenzi();
        }

        // Salveaza observatiile si statusul comenzii selectate.
        // Statusul este trimis si catre factura si platile asociate.
        private void BtnActualizeaza_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out DataRowView comanda))
            {
                return;
            }

            try
            {
                Guid idComanda = (Guid)comanda["ID"];

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                // Totalul este recalculat din produsele comenzii, pentru a nu ramane
                // o valoare veche daca preturile sau cantitatile au fost modificate.
                string query = @"
UPDATE Comanda
SET Observatii = @Observatii,
    Total = ISNULL((
        SELECT SUM(cp.Cantitate * p.Pret)
        FROM ComandaProduse cp
        INNER JOIN Produs p ON cp.ID_Produs = p.ID
        WHERE cp.ID_Comanda = @ID
), 0)
WHERE ID = @ID";

                using SqlCommand cmd = new SqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@ID", idComanda);
                cmd.Parameters.AddWithValue("@Observatii", TxtObservatii.Text.Trim());
                cmd.ExecuteNonQuery();

                string status = CitesteStatusSelectat();
                GestionareComanda.SincronizeazaStatus(conn, transaction, idComanda, status);

                // Observatiile, totalul si statusurile sunt confirmate ca o singura operatie.
                transaction.Commit();

                AppLogger.Scrie("Comanda actualizata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSterge_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out DataRowView comanda))
            {
                return;
            }

            if (MessageBox.Show("Sigur vrei sa stergi comanda selectata? Se sterg si produsele, platile si factura asociata.", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Guid idComanda = (Guid)comanda["ID"];

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                using SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@ID", idComanda);

                // Stergem platile
                cmd.CommandText = "DELETE FROM Plati WHERE ID_Comanda = @ID";
                cmd.ExecuteNonQuery();

                // Stergem facturile
                cmd.CommandText = "DELETE FROM Facturi WHERE ID_Comanda = @ID";
                cmd.ExecuteNonQuery();

                // Stergem legaturile cu produsele
                cmd.CommandText = "DELETE FROM ComandaProduse WHERE ID_Comanda = @ID";
                cmd.ExecuteNonQuery();

                // La final, stergem comanda in sine
                cmd.CommandText = "DELETE FROM Comanda WHERE ID = @ID";
                cmd.ExecuteNonQuery();

                transaction.Commit();

                AppLogger.Scrie("Comanda stearsa", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la stergerea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Genereaza factura pentru comanda selectata, daca aceasta contine produse.
        private void BtnGenereazaFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaComanda(out DataRowView comanda))
            {
                return;
            }

            Guid idComanda = (Guid)comanda["ID"];
            int numarProduse = Convert.ToInt32(comanda["NumarProduse"]);

            if (numarProduse == 0)
            {
                MessageBox.Show("Nu poti genera factura pentru o comanda fara produse.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                GestionareComanda.GenereazaFactura(idComanda);
                AppLogger.Scrie("Factura generata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda);
                IncarcaComenzi();
                MessageBox.Show("Factura a fost generata sau era deja existenta.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la generarea facturii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Copiaza statusul si observatiile comenzii selectate in formularul de editare.
        private void ComenziGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComenziGrid.SelectedItem is not DataRowView comanda)
            {
                return;
            }

            TxtObservatii.Text = comanda["Observatii"].ToString();
            ComboStatus.SelectedItem = comanda["Status"].ToString();

            if (ComboStatus.SelectedIndex < 0)
            {
                ComboStatus.SelectedItem = GestionareComanda.Finalizata;
            }
        }

        // Citeste comenzile direct intr-un DataTable.
        private void IncarcaComenzi()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);

                string query = @"
SELECT c.ID,
       u.NumeComplet AS Client,
       u.Email,
       c.DataComanda,
       c.Status,
       ISNULL(NULLIF(c.Total, 0), ISNULL(SUM(cp.Cantitate * p.Pret), 0)) AS Total,
       COUNT(cp.ID) AS NumarProduse,
       ISNULL(c.Observatii, '') AS Observatii,
       CAST(CASE WHEN MAX(CASE WHEN f.ID IS NULL THEN 0 ELSE 1 END) = 1 THEN 1 ELSE 0 END AS BIT) AS AreFactura
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs p ON cp.ID_Produs = p.ID
LEFT JOIN Facturi f ON c.ID = f.ID_Comanda
GROUP BY c.ID, u.NumeComplet, u.Email, c.DataComanda, c.Status, c.Total, c.Observatii
ORDER BY c.DataComanda DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                DataTable tabelComenzi = new DataTable();
                adapter.Fill(tabelComenzi);

                ComenziGrid.ItemsSource = tabelComenzi.DefaultView;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea comenzilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Returneaza comanda selectata (ca rand din tabel) sau afiseaza un mesaj daca nu exista selectie.
        private bool SelecteazaComanda(out DataRowView comanda)
        {
            if (ComenziGrid.SelectedItem is DataRowView comandaSelectata)
            {
                comanda = comandaSelectata;
                return true;
            }

            comanda = null;
            MessageBox.Show("Selecteaza o comanda.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        // Returneaza statusul ales in ComboBox sau Finalizata ca valoare de rezerva.
        private string CitesteStatusSelectat()
        {
            if (ComboStatus.SelectedItem is string status)
            {
                return status;
            }

            return GestionareComanda.Finalizata;
        }
    }
}