using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class AdminDashboardPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;

        public AdminDashboardPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            DbSchema.AsiguraSchema();
            IncarcaDashboard();
        }

        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaDashboard();
        }

        private void BtnGenereazaRaport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string detalii = $"Utilizatori: {TxtTotalUtilizatori.Text}, Produse: {TxtTotalProduse.Text}, Comenzi: {TxtTotalComenzi.Text}, Facturi: {TxtFacturi.Text}, Venit: {TxtVenit.Text}";
                string query = "INSERT INTO Rapoarte (TipRaport, GeneratDe, Detalii) VALUES (@TipRaport, @GeneratDe, @Detalii)";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TipRaport", "Dashboard administrator");
                cmd.Parameters.AddWithValue("@GeneratDe", utilizatorCurent.ID);
                cmd.Parameters.AddWithValue("@Detalii", detalii);
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Raport generat", "Administrator: " + utilizatorCurent.Email + ", " + detalii);
                IncarcaDashboard();
                MessageBox.Show("Raportul a fost generat.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la generarea raportului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncarcaDashboard()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                TxtTotalUtilizatori.Text = ScalarInt(conn, "SELECT COUNT(*) FROM Utilizatori").ToString();
                TxtTotalProduse.Text = ScalarInt(conn, "SELECT COUNT(*) FROM Produs").ToString();
                TxtStocRedus.Text = ScalarInt(conn, "SELECT COUNT(*) FROM Produs WHERE Cantitate <= 5").ToString();
                TxtTotalComenzi.Text = ScalarInt(conn, "SELECT COUNT(*) FROM Comanda").ToString();
                TxtFacturi.Text = ScalarInt(conn, "SELECT COUNT(*) FROM Facturi").ToString();
                TxtVenit.Text = ScalarDecimal(conn, "SELECT ISNULL(SUM(Suma), 0) FROM Plati WHERE Status = 'Platita'").ToString("0.00") + " lei";

                TopProduseGrid.ItemsSource = IncarcaTopProduse(conn).DefaultView;
                ComenziRecenteGrid.ItemsSource = IncarcaComenziRecente(conn).DefaultView;
                RapoarteGrid.ItemsSource = IncarcaRapoarte(conn).DefaultView;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea dashboardului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int ScalarInt(SqlConnection conn, string query)
        {
            using SqlCommand cmd = new SqlCommand(query, conn);
            object? rezultat = cmd.ExecuteScalar();
            return Convert.ToInt32(rezultat);
        }

        private decimal ScalarDecimal(SqlConnection conn, string query)
        {
            using SqlCommand cmd = new SqlCommand(query, conn);
            object? rezultat = cmd.ExecuteScalar();
            return Convert.ToDecimal(rezultat);
        }

        private DataTable IncarcaTopProduse(SqlConnection conn)
        {
            string query = @"
SELECT TOP 10 p.Nume AS Produs,
       ISNULL(SUM(cp.Cantitate), 0) AS Cantitate,
       ISNULL(SUM(cp.Cantitate * p.Pret), 0) AS Total
FROM Produs p
LEFT JOIN ComandaProduse cp ON p.ID = cp.ID_Produs
GROUP BY p.Nume
ORDER BY Cantitate DESC, Produs";

            return CitesteTabel(conn, query);
        }

        private DataTable IncarcaComenziRecente(SqlConnection conn)
        {
            string query = @"
SELECT TOP 10 c.DataComanda,
       u.NumeComplet AS Client,
       c.Status,
       ISNULL(NULLIF(c.Total, 0), ISNULL(SUM(cp.Cantitate * p.Pret), 0)) AS Total
FROM Comanda c
INNER JOIN Utilizatori u ON c.ID_Utilizator = u.Id
LEFT JOIN ComandaProduse cp ON c.ID = cp.ID_Comanda
LEFT JOIN Produs p ON cp.ID_Produs = p.ID
GROUP BY c.ID, c.DataComanda, u.NumeComplet, c.Status, c.Total
ORDER BY c.DataComanda DESC";

            return CitesteTabel(conn, query);
        }

        private DataTable IncarcaRapoarte(SqlConnection conn)
        {
            string query = "SELECT TOP 20 DataGenerare, TipRaport, Detalii FROM Rapoarte ORDER BY DataGenerare DESC";
            return CitesteTabel(conn, query);
        }

        private DataTable CitesteTabel(SqlConnection conn, string query)
        {
            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable tabel = new DataTable();
            adapter.Fill(tabel);
            return tabel;
        }
    }
}
