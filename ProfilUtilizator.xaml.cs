using System;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for ProfilUtilizator.xaml
    /// </summary>
    public partial class ProfilUtilizator : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";

        private Utilizator utilizatorCurent;

        public ProfilUtilizator(Utilizator utilizator)
        {
            InitializeComponent();

            this.utilizatorCurent = utilizator;
            TxtId.Text = $"{utilizator.ID}";
            TxtNume.Text = utilizator.Nume;
            TxtEmail.Text = utilizator.Email;
            TxtRol.Text = $" {utilizator.Rol}";
            IncarcaDateContact();

            if (utilizatorCurent.Rol != RolUtilizator.Administrator && utilizatorCurent.Rol != RolUtilizator.Angajat)
            {
                ConfigurareAngajati.Visibility = Visibility.Collapsed;
            }
         
        }

        private void BtnDatePersonale_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ConfigurareAngajati_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ConfigurareAngajatiPage(utilizatorCurent));
        }

        private void EditNrTelefon_Click(object sender, RoutedEventArgs e)
        {
            EditINFO dialog = new EditINFO("Număr de Telefon", TxtTelefon.Text);
            
           
            if (dialog.ShowDialog() == true)
            {
                TxtTelefon.Text = dialog.ValoareIntrodusa;
                SalveazaDateContact();
            }
        }

        private void AdresaLivrareEdit_Click(object sender, RoutedEventArgs e)
        {
            EditINFO dialog = new EditINFO("Adresă de Livrare", TxtAdresa.Text);

            if (dialog.ShowDialog() == true)
            {
                TxtAdresa.Text = dialog.ValoareIntrodusa;
                SalveazaDateContact();
            }
        }

        private void IncarcaDateContact()
        {
            try
            {

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT NumarTelefon, Adresa FROM DateUtilizatori WHERE UtilizatorId = @UtilizatorId";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UtilizatorId", utilizatorCurent.ID);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    TxtTelefon.Text = reader["NumarTelefon"].ToString();
                    TxtAdresa.Text = reader["Adresa"].ToString();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la încărcarea datelor de contact: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalveazaDateContact()
        {
            try
            {

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
            IF EXISTS (SELECT 1 FROM DateUtilizatori WHERE UtilizatorId = @UtilizatorId)
            BEGIN
                UPDATE DateUtilizatori
                SET NumarTelefon = @NumarTelefon,
                    Adresa = @Adresa
                WHERE UtilizatorId = @UtilizatorId
            END
            ELSE
            BEGIN
                INSERT INTO DateUtilizatori (UtilizatorId, NumarTelefon, Adresa)
                VALUES (@UtilizatorId, @NumarTelefon, @Adresa)
            END";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UtilizatorId", utilizatorCurent.ID);
                cmd.Parameters.AddWithValue("@NumarTelefon",TxtTelefon.Text);
                cmd.Parameters.AddWithValue("@Adresa", TxtAdresa.Text);

                cmd.ExecuteNonQuery();
                AppLogger.Scrie("Date profil salvate", "Utilizator: " + utilizatorCurent.Email + ", telefon: " + TxtTelefon.Text + ", adresa: " + TxtAdresa.Text);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la salvarea datelor de contact: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       
    }
}
