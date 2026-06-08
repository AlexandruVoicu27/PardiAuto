using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AutoPartsShop
{
    
    public partial class MainWindow : Window
    {
        private Utilizator? utilizatorCurent;

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated;
            MainFrame.Navigate(new LoginPage());
        }

        public MainWindow(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            ConfigureazaMeniuDupaRol();
        }

        public void SetUtilizatorCurent(Utilizator utilizator)
        {
            utilizatorCurent = utilizator;
            UserSalutText.Text = $"Salut, {utilizatorCurent.Nume}!";
            ConfigureazaMeniuDupaRol();

            if (utilizatorCurent.Rol == RolUtilizator.Administrator)
            {
                MainFrame.Navigate(new AdminDashboardPage(utilizatorCurent));
            }
            else
            {
                MainFrame.Navigate(new MainPage(utilizatorCurent));
            }
        }

        // Ascunde meniul pe paginile de autentificare si il reconfigureaza dupa navigare.
        
        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is LoginPage || MainFrame.Content is CreateAccountPage)
            {
                BtnDeconectare.Visibility = Visibility.Collapsed;
                UserSalutText.Visibility = Visibility.Collapsed;
                ProfilUtilizator.Visibility = Visibility.Collapsed;
                BtnAcasa.Visibility = Visibility.Collapsed;
                BtnDashboardAdmin.Visibility = Visibility.Collapsed;
                BtnAdminComenzi.Visibility = Visibility.Collapsed;
                BtnPlatiFacturi.Visibility = Visibility.Collapsed;
                BtnPieseAuto.Visibility = Visibility.Collapsed;
                BtnComandaMea.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnDeconectare.Visibility = Visibility.Visible;
                UserSalutText.Visibility = Visibility.Visible;
                ProfilUtilizator.Visibility = Visibility.Visible;
                ConfigureazaMeniuDupaRol();
            }
        }

        // Afiseaza numai butoanele permise administratorului, angajatului sau clientului.
        private void ConfigureazaMeniuDupaRol()
        {
            if (utilizatorCurent == null)
            {
                return;
            }

            bool esteAdmin = utilizatorCurent.Rol == RolUtilizator.Administrator;
            bool esteStaff = utilizatorCurent.Rol == RolUtilizator.Administrator || utilizatorCurent.Rol == RolUtilizator.Angajat;
            bool esteClient = utilizatorCurent.Rol == RolUtilizator.Client;

            BtnAcasa.Visibility = Visibility.Visible;
            BtnPieseAuto.Visibility = Visibility.Visible;
            BtnComandaMea.Visibility = esteClient ? Visibility.Visible : Visibility.Collapsed;

            BtnDashboardAdmin.Visibility = esteAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnAdminComenzi.Visibility = esteStaff ? Visibility.Visible : Visibility.Collapsed;
            BtnPlatiFacturi.Visibility = esteStaff ? Visibility.Visible : Visibility.Collapsed;
        }

        // Incheie sesiunea curenta si revine la autentificare.
        private void BtnDeconectare_Click(object sender, RoutedEventArgs e)
        {
            utilizatorCurent = null;
            MainFrame.Navigate(new LoginPage());
        }

        
        // Deschide dashboardul administratorului sau pagina principala a celorlalte roluri.
        private void BtnAcasa_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent == null)
            {
                MainFrame.Navigate(new LoginPage());
                return;
            }

            if (utilizatorCurent.Rol == RolUtilizator.Administrator)
            {
                MainFrame.Navigate(new AdminDashboardPage(utilizatorCurent));
            }
            else
            {
                MainFrame.Navigate(new MainPage(utilizatorCurent));
            }
        }

        // Navigheaza la dashboardul administratorului.
        private void BtnDashboardAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new AdminDashboardPage(utilizatorCurent));
            }
        }

        // Navigheaza la administrarea utilizatorilor.
        private void BtnAdminUtilizatori_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ConfigurareAngajatiPage(utilizatorCurent));
            }
        }

       // Navigheaza la administrarea comenzilor.
        private void BtnAdminComenzi_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new AdministrareComenziPage(utilizatorCurent));
            }
        }

        // Navigheaza la pagina de plati si facturi.
        private void BtnPlatiFacturi_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new PlatiFacturiPage(utilizatorCurent));
            }
        }

        // Navigheaza la catalogul complet de piese auto.
        private void BtnPieseAuto_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new PieseAutoPage(utilizatorCurent, "Toate"));
            }
        }

        // Navigheaza la cosul/comanda clientului.
        private void BtnComandaMea_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ComandaMeaPage(utilizatorCurent));
            }
        }

        // Navigheaza la profilul utilizatorului autentificat.
        private void ProfilUtilizator_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ProfilUtilizator(utilizatorCurent));
            }
        }
    }
}
