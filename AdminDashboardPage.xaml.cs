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
        private const int PragStocRedus = 10;
        private readonly Utilizator utilizatorCurent;

       // Initializeaza dashboardul si incarca toate valorile din baza de date.
        public AdminDashboardPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            IncarcaDashboard();
        }

     
        // Reincarca indicatorii si tabelele dashboardului.
       
        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaDashboard();
        }

        // Salveaza in tabela Rapoarte o copie text a indicatorilor afisati in acel moment.
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

        // Incarca numerele sumarizate, venitul, produsele vandute si comenzile recente.
        private void IncarcaDashboard()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                // Fiecare ExecuteScalar citeste o singura valoare agregata.
                using SqlCommand cmdUtilizatori = new SqlCommand("SELECT COUNT(*) FROM Utilizatori", conn);
                TxtTotalUtilizatori.Text = Convert.ToInt32(cmdUtilizatori.ExecuteScalar()).ToString();

                using SqlCommand cmdProduse = new SqlCommand("SELECT COUNT(*) FROM Produs", conn);
                TxtTotalProduse.Text = Convert.ToInt32(cmdProduse.ExecuteScalar()).ToString();

                using SqlCommand cmdStoc = new SqlCommand("SELECT COUNT(*) FROM Produs WHERE Cantitate <= @PragStocRedus", conn);
                cmdStoc.Parameters.AddWithValue("@PragStocRedus", PragStocRedus);
                TxtStocRedus.Text = Convert.ToInt32(cmdStoc.ExecuteScalar()).ToString();
                
                using SqlCommand cmdComenzi = new SqlCommand("SELECT COUNT(*) FROM Comanda", conn);
                TxtTotalComenzi.Text = Convert.ToInt32(cmdComenzi.ExecuteScalar()).ToString();

                using SqlCommand cmdFacturi = new SqlCommand("SELECT COUNT(*) FROM Facturi", conn);
                TxtFacturi.Text = Convert.ToInt32(cmdFacturi.ExecuteScalar()).ToString();

                using SqlCommand cmdVenit = new SqlCommand(
                    "SELECT ISNULL(SUM(Total), 0) FROM Comanda WHERE Status = 'Achitata'",
                    conn);
                // Venitul include o singura data fiecare comanda achitata, chiar daca are mai multe plati.
                decimal venitIncasat = Convert.ToDecimal(cmdVenit.ExecuteScalar());
                TxtVenit.Text = venitIncasat.ToString("0.00") + " lei";

                TopProduseGrid.ItemsSource = IncarcaTopProduse(conn).DefaultView;
                ComenziRecenteGrid.ItemsSource = IncarcaComenziRecente(conn).DefaultView;
                RapoarteGrid.ItemsSource = IncarcaRapoarte(conn).DefaultView;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea dashboardului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       // Returneaza primele zece produse ordonate dupa cantitatea comandata.
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

        // Returneaza ultimele zece comenzi impreuna cu clientul, statusul si totalul lor.
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

        // Returneaza ultimele douazeci de rapoarte generate.
        private DataTable IncarcaRapoarte(SqlConnection conn)
        {
            string query = "SELECT TOP 20 DataGenerare, TipRaport, Detalii FROM Rapoarte ORDER BY DataGenerare DESC";
            return CitesteTabel(conn, query);
        }

        // Executa o interogare SELECT si transforma rezultatul intr-un DataTable pentru DataGrid.
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
