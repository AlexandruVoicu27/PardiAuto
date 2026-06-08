using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    // Gestioneaza facturile si platile comenzilor finalizate.
    // Toate modificarile de status sunt sincronizate cu tabela Comanda.
    public partial class PlatiFacturiPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<PlataFactura> platiFacturi = new List<PlataFactura>();
        private readonly List<ComandaSelect> comenzi = new List<ComandaSelect>();

        
        // Initializeaza controalele, statusurile comune si datele afisate in pagina.
        public PlatiFacturiPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            GestionareComanda.SincronizeazaDateExistente();
            CmbMetoda.SelectedIndex = 0;
            CmbStatusPlata.ItemsSource = GestionareComanda.Statusuri;
            CmbStatusPlata.SelectedItem = GestionareComanda.Achitata;
            IncarcaComenzi();
            IncarcaPlatiFacturi();
        }

        // Reciteste comenzile facturabile si lista de facturi/plati.
        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaComenzi();
            IncarcaPlatiFacturi();
        }

        // Genereaza factura comenzii selectate, fara a crea un duplicat.
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
                GestionareComanda.GenereazaFactura(idComanda);
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

        // Creeaza o plata noua si sincronizeaza statusul ales in comanda, factura si plata.
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
                GestionareComanda.GenereazaFactura(idComanda);

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                Guid idFactura = ObtineFactura(conn, transaction, idComanda);

                // Validarea se face in aceeasi tranzactie cu inserarea pentru ca totalul platilor sa nu se schimbe intre verificare si salvare.
                if (!ValideazaSumaFactura(conn, transaction, idFactura, suma, null))
                {
                    transaction.Rollback();
                    return;
                }

                string query = @"
INSERT INTO Plati (ID_Comanda, ID_Factura, Suma, Metoda, Status, Referinta)
VALUES (@ID_Comanda, @ID_Factura, @Suma, @Metoda, @Status, @Referinta)";

                using SqlCommand cmd = new SqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
                cmd.Parameters.AddWithValue("@ID_Factura", idFactura);
                AdaugaParametruSuma(cmd, suma);
                cmd.Parameters.AddWithValue("@Metoda", TextComboBox(CmbMetoda));
                string status = TextComboBox(CmbStatusPlata);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Referinta", "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                cmd.ExecuteNonQuery();

                GestionareComanda.SincronizeazaStatus(conn, transaction, idComanda, status);

                // Plata si toate statusurile sunt salvate impreuna.
                transaction.Commit();

                AppLogger.Scrie("Plata inregistrata", "Utilizator: " + utilizatorCurent.Email + ", comanda: " + idComanda + ", suma: " + suma);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la inregistrarea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Modifica suma, metoda si statusul platii selectate.
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
                using SqlTransaction transaction = conn.BeginTransaction();

                // Plata editata este exclusa din suma existenta, apoi este adaugata noua valoare.
                if (plata.IDFactura == null ||
                    !ValideazaSumaFactura(conn, transaction, plata.IDFactura.Value, suma, plata.IDPlata))
                {
                    transaction.Rollback();
                    return;
                }

                string query = "UPDATE Plati SET Suma = @Suma, Metoda = @Metoda, Status = @Status WHERE ID = @ID";
                using SqlCommand cmd = new SqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@ID", plata.IDPlata.Value);
                AdaugaParametruSuma(cmd, suma);
                cmd.Parameters.AddWithValue("@Metoda", TextComboBox(CmbMetoda));
                string status = TextComboBox(CmbStatusPlata);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.ExecuteNonQuery();

                GestionareComanda.SincronizeazaStatus(conn, transaction, plata.IDComanda, status);
                transaction.Commit();

                AppLogger.Scrie("Plata actualizata", "Utilizator: " + utilizatorCurent.Email + ", plata: " + plata.IDPlata);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sterge plata selectata; daca nu mai exista plati, comanda revine la Finalizata.
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
                using SqlTransaction transaction = conn.BeginTransaction();

                string query = "DELETE FROM Plati WHERE ID = @ID";
                using SqlCommand cmd = new SqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@ID", plata.IDPlata.Value);
                cmd.ExecuteNonQuery();

                // Verifica daca factura mai are alte plati dupa stergerea randului selectat.
                using SqlCommand countCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Plati WHERE ID_Comanda = @ID_Comanda",
                    conn,
                    transaction);
                countCmd.Parameters.AddWithValue("@ID_Comanda", plata.IDComanda);
                int platiRamase = Convert.ToInt32(countCmd.ExecuteScalar());

                if (platiRamase == 0)
                {
                    GestionareComanda.SincronizeazaStatus(
                        conn,
                        transaction,
                        plata.IDComanda,
                        GestionareComanda.Finalizata);
                }

                transaction.Commit();

                AppLogger.Scrie("Plata stearsa", "Utilizator: " + utilizatorCurent.Email + ", plata: " + plata.IDPlata);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la stergerea platii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Marcheaza drept Anulata factura, comanda si platile asociate.
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
                using SqlTransaction transaction = conn.BeginTransaction();

                GestionareComanda.SincronizeazaStatus(
                    conn,
                    transaction,
                    plata.IDComanda,
                    GestionareComanda.Anulata);
                transaction.Commit();

                AppLogger.Scrie("Factura anulata", "Utilizator: " + utilizatorCurent.Email + ", factura: " + plata.NumarFactura);
                IncarcaComenzi();
                IncarcaPlatiFacturi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la anularea facturii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Completeaza formularul cu datele randului selectat din tabel.
        private void PlatiFacturiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatiFacturiGrid.SelectedItem is not PlataFactura plata)
            {
                return;
            }

            CmbComenzi.SelectedValue = plata.IDComanda;
            TxtSumaPlata.Text = plata.SumaPlatita > 0 ? plata.SumaPlatita.ToString("0.00") : plata.TotalFactura.ToString("0.00");
            SeteazaCombo(CmbMetoda, string.IsNullOrWhiteSpace(plata.Metoda) ? "Card" : plata.Metoda);
            SeteazaCombo(CmbStatusPlata, plata.Status);
        }

        // Incarca comenzile care au ajuns intr-un stadiu ce permite facturarea sau plata.
        private void IncarcaComenzi()
        {
            comenzi.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                // Totalul salvat este folosit daca este diferit de zero;
                // altfel este calculat din cantitatile si preturile produselor.
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

        // Incarca facturile si platile intr-o lista comuna pentru afisarea in DataGrid.
        private void IncarcaPlatiFacturi()
        {
            platiFacturi.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                // LEFT JOIN permite afisarea unei facturi chiar daca nu are inca plata.
                // Statusul este citit din Comanda, sursa unica folosita de interfata.
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
       c.Status
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN Facturi f ON c.ID = f.ID_Comanda
LEFT JOIN Plati p ON f.ID = p.ID_Factura
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs pr ON cp.ID_Produs = pr.ID
WHERE f.ID IS NOT NULL
GROUP BY p.ID, f.ID, c.ID, f.NumarFactura, u.NumeComplet, c.DataComanda, p.DataPlata, f.Total, c.Total, p.Suma, p.Metoda, c.Status
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
                        Convert.ToString(reader["Status"]) ?? ""));
                }

                PlatiFacturiGrid.ItemsSource = null;
                PlatiFacturiGrid.ItemsSource = platiFacturi;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea platilor/facturilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Returneaza ID-ul facturii asociate unei comenzi in tranzactia curenta.
        private Guid ObtineFactura(SqlConnection conn, SqlTransaction transaction, Guid idComanda)
        {
            string query = "SELECT ID FROM Facturi WHERE ID_Comanda = @ID_Comanda";
            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@ID_Comanda", idComanda);
            object? rezultat = cmd.ExecuteScalar();
            return (Guid)(rezultat ?? Guid.Empty);
        }

        // Verifica daca suma noua, impreuna cu celelalte plati, depaseste totalul facturii.
        // La editare, plata curenta este ignorata pentru a nu fi numarata de doua ori.
        private bool ValideazaSumaFactura(
            SqlConnection conn,
            SqlTransaction transaction,
            Guid idFactura,
            decimal suma,
            Guid? idPlataIgnorata)
        {
            decimal totalFactura;
            using (SqlCommand cmd = new SqlCommand(
                "SELECT Total FROM Facturi WHERE ID = @ID_Factura",
                conn,
                transaction))
            {
                cmd.Parameters.AddWithValue("@ID_Factura", idFactura);
                totalFactura = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            decimal altePlati;
            // Parametrul optional ID_Plata exclude plata editata din SUM.
            using (SqlCommand cmd = new SqlCommand(@"
SELECT ISNULL(SUM(Suma), 0)
FROM Plati
WHERE ID_Factura = @ID_Factura
  AND (@ID_Plata IS NULL OR ID <> @ID_Plata)", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@ID_Factura", idFactura);
                SqlParameter idPlataParametru = cmd.Parameters.Add("@ID_Plata", SqlDbType.UniqueIdentifier);
                idPlataParametru.Value = (object?)idPlataIgnorata ?? DBNull.Value;
                altePlati = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            if (altePlati + suma > totalFactura)
            {
                decimal disponibil = Math.Max(0, totalFactura - altePlati);
                MessageBox.Show(
                    "Suma depaseste totalul facturii. Mai poti inregistra maximum " +
                    disponibil.ToString("0.00") + " lei.",
                    "Suma prea mare",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // Citeste suma introdusa folosind virgula romaneasca sau punctul invariant si ii valideaza limitele.
        private bool CitesteSuma(out decimal suma)
        {
            string text = TxtSumaPlata.Text.Trim();

            // Daca textul contine virgula, este interpretat in format romanesc:
            // de exemplu 1000,50 inseamna o mie de lei si cincizeci de bani.
            CultureInfo cultura = text.Contains(',')
                ? CultureInfo.GetCultureInfo("ro-RO")
                : CultureInfo.InvariantCulture;

            if (!decimal.TryParse(text, NumberStyles.Number, cultura, out suma))
            {
                MessageBox.Show("Suma trebuie sa fie un numar valid.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (suma <= 0)
            {
                MessageBox.Show("Suma trebuie sa fie mai mare decat 0.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (decimal.Round(suma, 2) != suma)
            {
                MessageBox.Show("Suma poate avea maximum doua zecimale.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (suma > 99999999.99m)
            {
                MessageBox.Show("Suma maxima permisa este 99.999.999,99 lei.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // Adauga parametrul @Suma cu precizia DECIMAL(10,2) folosita de coloana SQL.
        private void AdaugaParametruSuma(SqlCommand cmd, decimal suma)
        {
            SqlParameter parametru = cmd.Parameters.Add("@Suma", SqlDbType.Decimal);
            parametru.Precision = 10;
            parametru.Scale = 2;
            parametru.Value = suma;
        }

        // Extrage textul selectat dintr-un ComboBox alimentat cu string-uri sau ComboBoxItem.
        private string TextComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is string text)
            {
                return text;
            }

            if (comboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString() ?? "";
            }

            return "";
        }

        // Selecteaza in ComboBox elementul care corespunde textului primit.
        private void SeteazaCombo(ComboBox comboBox, string text)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is string valoare &&
                    string.Equals(valoare, text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }

                if (comboBox.Items[i] is ComboBoxItem item && string.Equals(item.Content.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        // Model simplu pentru afisarea unei comenzi in ComboBox, pastrand separat ID-ul real.
        private class ComandaSelect
        {
            public Guid ID { get; set; }
            public string Afisare { get; set; }

            // Creeaza optiunea afisata in lista de comenzi.
            public ComandaSelect(Guid id, string afisare)
            {
                ID = id;
                Afisare = afisare;
            }
        }
    }
}
