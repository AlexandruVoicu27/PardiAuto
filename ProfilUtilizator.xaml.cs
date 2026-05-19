using System;
using System.Collections.Generic;
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
        private Utilizator utilizatorCurent;

        public ProfilUtilizator(Utilizator utilizator)
        {
            InitializeComponent();

            this.utilizatorCurent = utilizator;
            TxtId.Text = $"{utilizator.ID}";
            TxtNume.Text = utilizator.Nume;
            TxtEmail.Text = utilizator.Email;
            TxtRol.Text = $" {utilizator.Rol}";

            if (utilizatorCurent.Rol != RolUtilizator.Administrator)
            {
                ConfigurareAngajati.Visibility = Visibility.Collapsed;
            }   
         
        }
        private void EditNrTelefon_Click(object sender, RoutedEventArgs e)
        {
            EditINFO dialog = new EditINFO("Număr de Telefon", TxtTelefon.Text);

            if (dialog.ShowDialog() == true)
            {
                TxtTelefon.Text = dialog.ValoareIntrodusa;

                //aici trb sa dau update si in baza de date;
            }
        }

        private void AdresaLivrareEdit_Click(object sender, RoutedEventArgs e)
        {
            EditINFO dialog = new EditINFO("Adresă de Livrare", TxtAdresa.Text);

            if (dialog.ShowDialog() == true)
            {
                TxtAdresa.Text = dialog.ValoareIntrodusa;

                //tot pt baza de date trb aici
            }
        }
    }
}
