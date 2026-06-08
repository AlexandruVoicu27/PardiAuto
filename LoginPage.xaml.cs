using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
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
    public partial class LoginPage : Page
    {
        // Initializeaza formularul de autentificare.
        public LoginPage()
        {
            InitializeComponent();
        }

        // Deschide pagina de creare a unui cont nou.
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new CreateAccountPage());
        }

        // Valideaza campurile, hash-uieste parola si porneste verificarea in baza de date.
        private void Conectare_Click(object sender, RoutedEventArgs e)
        {
            string username_email = Username_Mail_BOX.Text.Trim();
            string parola = Parola_Login_BOX.Password;

            if (string.IsNullOrEmpty(username_email) || string.IsNullOrEmpty(parola))
            {
                MessageBox.Show("Te rog să introduci email-ul sau username-ul și parola!", "Atenție", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string parola_introdusa = Security.HashParola(parola);
            VerificareInBazaDeDate(username_email, parola_introdusa);
        }

        // Cauta utilizatorul dupa nume sau email si initializeaza sesiunea daca parola corespunde.
        private void VerificareInBazaDeDate(string username_email, string parola_introdusa)
        {
            string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT Id,NumeComplet,Email, ParolaHash, Rol FROM Utilizatori WHERE (NumeComplet = @username_email OR Email = @username_email) AND ParolaHash = @parola_hash";
                    //anti sql injection:)

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username_email", username_email);
                        cmd.Parameters.AddWithValue("@parola_hash", parola_introdusa);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Extragem hash-ul salvat la înregistrare
                                string hashDB = reader["ParolaHash"].ToString();
                                string nume = reader["NumeComplet"].ToString();
                                string email = reader["Email"].ToString();

                                if (parola_introdusa == hashDB)
                                {
                                    int id = Convert.ToInt32(reader["Id"]);
                                    string rolText = reader["Rol"].ToString();
                                    

                                    RolUtilizator rol = (RolUtilizator)Enum.Parse(typeof(RolUtilizator), rolText);

                                    Utilizator utilizatorLogat = new Utilizator(id, nume, email, rol);


                                    MessageBox.Show($"Autentificare cu succes! Bine ai venit, {nume}!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Setam utilizatorul curent în MainWindow
                                    Window mainWindow = Application.Current.MainWindow;
                                    if (mainWindow is MainWindow mw)
                                    {
                                        mw.SetUtilizatorCurent(utilizatorLogat);
                                    }

                                }
                                else
                                {
                                    MessageBox.Show("Parolă incorectă! Mai încearcă.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Nu există niciun cont înregistrat cu acest email sau username!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Eroare la conectarea cu baza de date: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

