using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoPartsShop
{
    /// <summary>
    /// Interaction logic for CreateAccountPage.xaml
    /// </summary>
    public partial class CreateAccountPage : Page
    {
        public CreateAccountPage()
        {
            InitializeComponent();
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new LoginPage());
        }
        private void CreareCont_Click(object sender, RoutedEventArgs e)
        {
            string nume = NumeTextBox.Text;
            string email = EmailTextBox.Text;
            string parola = ParolaBox.Password;

            email=EmailTextBox.Text.Trim(); //elimina spatiile albe de la inceput si sfarsit

            if (string.IsNullOrWhiteSpace(nume) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(parola))
            {
                MessageBox.Show("Te rog să completezi toate câmpurile!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; 
            }

            // 2. Verificam formatul de email cu Regex
            string emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            
            if (!Regex.IsMatch(email, emailPattern))
            {
                MessageBox.Show("Adresa de email nu este validă! Trebuie să conțină '@' și un domeniu.", "Eroare Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            //si adaugam in DB

            SalvareInBazaDeDate(nume, email, Security.HashParola(parola));
            //TREBUIE ADAUGAT SA MA BAGE PE SITE AUTOMAT
        }

        private void SalvareInBazaDeDate(string nume, string email, string parolaHash)
        {
            string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open(); 

                    // Folosim parametri (@Nume, etc.) ca să prevenim atacurile de tip SQL Injection
                    string query = "INSERT INTO Utilizatori (NumeComplet, Email, ParolaHash,Rol) VALUES (@Nume, @Email, @Parola, @Rol)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nume", nume);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Parola", parolaHash);
                        cmd.Parameters.AddWithValue("@Rol", RolUtilizator.Client.ToString());

                        cmd.ExecuteNonQuery(); //folosesc functia asta pentru audit mai incolo
                        AppLogger.Scrie("Cont creat", "Nume: " + nume + ", email: " + email + ", rol: " + RolUtilizator.Client);

                        MessageBox.Show("Cont creat cu succes! Te poți autentifica acum.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                        NavigationService.GetNavigationService(this).Navigate(new LoginPage());
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627) // cod specific pentru duplicate din MSSQL
                    {
                        MessageBox.Show("Există deja un cont cu acest email!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Eroare la conectarea cu baza de date: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

    }

}
