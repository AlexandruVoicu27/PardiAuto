using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class ConfigurareAngajatiPage : Page
    {
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly Utilizator utilizatorCurent;

        // Initializeaza pagina si limiteaza promovarea la administrator la utilizatorii administratori.
        public ConfigurareAngajatiPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            IncarcaUtilizatori();

            //daca e administrator, afiseaza butonul de promovare la administrator, altfel il ascunde
            BtnPromoveazaAdministrator.Visibility =
                utilizatorCurent.Rol == RolUtilizator.Administrator
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // Reincarca lista utilizatorilor din baza de date.
        private void BtnReincarcaUtilizatori_Click(object sender, RoutedEventArgs e)
        {
            IncarcaUtilizatori();
        }

        // Schimba rolul utilizatorului selectat in Angajat.
        private void BtnPromoveazaAngajat_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out DataRowView utilizator))
            {
                return;
            }

            if (utilizator["Rol"]?.ToString() == RolUtilizator.Angajat.ToString())
            {
                MessageBox.Show(
                    "Utilizatorul selectat este deja angajat.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            if (utilizator["Rol"]?.ToString()==RolUtilizator.Administrator.ToString())
            {
                MessageBox.Show(
                   "Utilizatorul selectat este Administrator.",
                   "Info",
                   MessageBoxButton.OK,
                   MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(
                Convert.ToInt32(utilizator["ID"]),
                RolUtilizator.Angajat.ToString());
        }

        // Schimba rolul utilizatorului selectat in Client.
        private void BtnTransformaClient_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out DataRowView utilizator))
            {
                return;
            }

            if (utilizator["Rol"]?.ToString() == RolUtilizator.Client.ToString())
            {
                MessageBox.Show(
                    "Utilizatorul selectat este deja client.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;

            }
            if (utilizator["Rol"]?.ToString() == RolUtilizator.Administrator.ToString())
            {
                MessageBox.Show(
                   "Utilizatorul selectat este Administrator.",
                   "Info",
                   MessageBoxButton.OK,
                   MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(
                Convert.ToInt32(utilizator["ID"]),
                RolUtilizator.Client.ToString());
        }

        // Schimba rolul utilizatorului selectat in Administrator.
        private void BtnPromoveazaAdministrator_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent.Rol != RolUtilizator.Administrator)
            {
                MessageBox.Show(
                    "Doar un administrator poate acorda rolul de administrator.",
                    "Acces interzis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!SelecteazaUtilizator(out DataRowView utilizator))
            {
                return;
            }

            if (utilizator["Rol"]?.ToString() == RolUtilizator.Administrator.ToString())
            {
                MessageBox.Show(
                    "Utilizatorul selectat este deja administrator.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(
                Convert.ToInt32(utilizator["ID"]),
                RolUtilizator.Administrator.ToString());
        }

        // Citeste utilizatorul selectat din tabel si afiseaza un mesaj daca nu exista selectie.
        private bool SelecteazaUtilizator(out DataRowView utilizator)
        {
            if (UtilizatoriGrid.SelectedItem is DataRowView randSelectat)
            {
                utilizator = randSelectat;
                return true;
            }

            utilizator = null!;
            MessageBox.Show(
                "Selecteaza un utilizator din lista.",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        // Citeste toti utilizatorii din baza de date si ii afiseaza in tabel.
        private void IncarcaUtilizatori()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    const string query = @"
                        SELECT
                            Id AS ID,
                            NumeComplet AS Nume,
                            Email,
                            Rol
                        FROM Utilizatori
                        ORDER BY
                            CASE Rol
                                WHEN 'Administrator' THEN 1
                                WHEN 'Angajat' THEN 2
                                ELSE 3
                            END,
                            Nume";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        UtilizatoriGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Eroare la incarcarea utilizatorilor: " + ex.Message,
                    "Eroare SQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Actualizeaza rolul utilizatorului in baza de date si reincarca lista.
        private void SchimbaRolUtilizator(int utilizatorId, string rolNou)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    const string query = @"
                        UPDATE Utilizatori
                        SET Rol = @Rol
                        WHERE Id = @ID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Rol", rolNou);
                        cmd.Parameters.AddWithValue("@ID", utilizatorId);
                        cmd.ExecuteNonQuery();
                    }
                }

                IncarcaUtilizatori();
                MessageBox.Show(
                    "Rolul utilizatorului a fost actualizat.",
                    "Succes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Eroare la actualizarea rolului: " + ex.Message,
                    "Eroare SQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
