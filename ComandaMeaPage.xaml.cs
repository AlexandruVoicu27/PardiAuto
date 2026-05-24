using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class ComandaMeaPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<ComandaDetaliu> comenzi = new List<ComandaDetaliu>();

        public ComandaMeaPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            AsiguraStatusComanda();
            IncarcaComenzi();
        }

        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaComenzi();
        }

        private void BtnFinalizeazaComanda_Click(object sender, RoutedEventArgs e)
        {
            if (comenzi.Count == 0)
            {
                MessageBox.Show("Nu ai produse in comanda.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!AreAdresaLivrare())
            {
                MessageBox.Show("Trebuie sa iti completezi adresa in Profilul meu inainte sa finalizezi comanda.", "Adresa lipsa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
            UPDATE Comanda
            SET Status = 'Finalizata'
            WHERE ID_Utilizator = @ID_Utilizator
              AND Status = 'InCos'";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID_Utilizator", utilizatorCurent.ID);

                int randuri = cmd.ExecuteNonQuery();

                AppLogger.Scrie("Comanda finalizata", "Utilizator: " + utilizatorCurent.Email + ", comenzi finalizate: " + randuri);
                MessageBox.Show("Comanda a fost finalizata.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                IncarcaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la finalizarea comenzii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncarcaComenzi()
        {
            try
            {
                comenzi.Clear();

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
            SELECT c.ID AS IDComanda,
                   c.DataComanda,
                   p.Nume,
                   p.Categorie,
                   cp.Cantitate,
                   p.Pret
            FROM Comanda c
            INNER JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
            INNER JOIN Produs p ON cp.ID_Produs = p.ID
            WHERE c.ID_Utilizator = @ID_Utilizator
              AND c.Status = 'InCos'
            ORDER BY c.DataComanda DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID_Utilizator", utilizatorCurent.ID);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Guid idComanda = (Guid)reader["IDComanda"];
                    DateTime dataComanda = Convert.ToDateTime(reader["DataComanda"]);
                    string nume = Convert.ToString(reader["Nume"]) ?? "";
                    string categorie = Convert.ToString(reader["Categorie"]) ?? "";
                    int cantitate = Convert.ToInt32(reader["Cantitate"]);
                    decimal pret = Convert.ToDecimal(reader["Pret"]);

                    comenzi.Add(new ComandaDetaliu(idComanda, dataComanda, nume, categorie, cantitate, pret));
                }

                AfiseazaComenzi();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea comenzilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfiseazaComenzi()
        {
            decimal total = 0;

            foreach (ComandaDetaliu comanda in comenzi)
            {
                total += comanda.Total;
            }

            ComenziGrid.ItemsSource = null;
            ComenziGrid.ItemsSource = comenzi;
            TxtNumarProduse.Text = "Produse cumparate: " + comenzi.Count;
            TxtTotalComenzi.Text = "Total: " + total.ToString("0.00") + " lei";
        }

        private void AsiguraStatusComanda()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
            IF COL_LENGTH('Comanda', 'Status') IS NULL
            BEGIN
                ALTER TABLE Comanda
                ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Comanda_Status DEFAULT 'InCos'
            END";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la verificarea comenzilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AreAdresaLivrare()
        {
            using SqlConnection conn = new SqlConnection(ConnectionString);
            conn.Open();

            string query = "SELECT Adresa FROM DateUtilizatori WHERE UtilizatorId = @UtilizatorId";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UtilizatorId", utilizatorCurent.ID);

            object? rezultat = cmd.ExecuteScalar();
            string adresa = Convert.ToString(rezultat) ?? "";

            return !string.IsNullOrWhiteSpace(adresa);
        }
    }
}
