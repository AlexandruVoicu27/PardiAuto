using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class ConfigurareAngajatiPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<Utilizator> utilizatori = new List<Utilizator>();

        public ConfigurareAngajatiPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            if(utilizator.Rol==RolUtilizator.Angajat)
            {
               BtnPromoveazaAdministrator.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (utilizator.Rol == RolUtilizator.Administrator)
                    BtnPromoveazaAdministrator.Visibility = Visibility.Visible;
            }
            IncarcaUtilizatori();
        }

        private void BtnReincarcaUtilizatori_Click(object sender, RoutedEventArgs e)
        {
            IncarcaUtilizatori();
        }

        private void BtnPromoveazaAngajat_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Rolul de administrator nu poate fi modificat de aici.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (utilizator.Rol == RolUtilizator.Angajat)
            {
                MessageBox.Show("Utilizatorul este deja angajat.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Angajat);
        }

        private void BtnTransformaClient_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Rolul de administrator nu poate fi modificat de aici.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (utilizator.Rol == RolUtilizator.Client)
            {
                MessageBox.Show("Utilizatorul este deja client.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Client);
        }

        private void BtnPromoveazaAdministrator_Click(object sender, RoutedEventArgs e)
        {
           
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

        
         
            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Utilizatorul este deja administrator.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Administrator);
        }

        private bool SelecteazaUtilizator(out Utilizator utilizator)
        {
            if (UtilizatoriGrid.SelectedItem is Utilizator utilizatorSelectat)
            {
                utilizator = utilizatorSelectat;
                return true;
            }

            utilizator = utilizatorCurent;
            MessageBox.Show("Selecteaza un utilizator.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private void IncarcaUtilizatori()
        {
            try
            {
                utilizatori.Clear();

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT Id, NumeComplet, Email, Rol FROM Utilizatori ORDER BY Rol, NumeComplet";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["Id"]);
                    string nume = Convert.ToString(reader["NumeComplet"]) ?? "";
                    string email = Convert.ToString(reader["Email"]) ?? "";
                    string rolText = Convert.ToString(reader["Rol"]) ?? RolUtilizator.Client.ToString();
                    RolUtilizator rol = (RolUtilizator)Enum.Parse(typeof(RolUtilizator), rolText);

                    utilizatori.Add(Utilizator.CreateByRol(id, nume, email, rol));
                }

                UtilizatoriGrid.ItemsSource = null;
                UtilizatoriGrid.ItemsSource = utilizatori;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea utilizatorilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SchimbaRolUtilizator(Utilizator utilizator, RolUtilizator rolNou)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "UPDATE Utilizatori SET Rol = @Rol WHERE Id = @Id";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", utilizator.ID);
                cmd.Parameters.AddWithValue("@Rol", rolNou.ToString());

                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Rol utilizator modificat", "Administrator: " + utilizatorCurent.Email + ", utilizator: " + utilizator.Email + ", rol vechi: " + utilizator.Rol + ", rol nou: " + rolNou);
                IncarcaUtilizatori();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la modificarea rolului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
