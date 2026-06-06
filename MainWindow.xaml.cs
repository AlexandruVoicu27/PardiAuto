using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AutoPartsShop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is LoginPage || MainFrame.Content is CreateAccountPage)
            {
                BtnDeconectare.Visibility = Visibility.Collapsed;
                UserSalutText.Visibility = Visibility.Collapsed;
                ProfilUtilizator.Visibility = Visibility.Collapsed;
                BtnAcasa.Visibility = Visibility.Collapsed;
                BtnDashboardAdmin.Visibility = Visibility.Collapsed;
                BtnAdminUtilizatori.Visibility = Visibility.Collapsed;
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
            BtnAdminUtilizatori.Visibility = esteAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnAdminComenzi.Visibility = esteStaff ? Visibility.Visible : Visibility.Collapsed;
            BtnPlatiFacturi.Visibility = esteStaff ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnDeconectare_Click(object sender, RoutedEventArgs e)
        {
            utilizatorCurent = null;
            MainFrame.Navigate(new LoginPage());
        }

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

        private void BtnDashboardAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new AdminDashboardPage(utilizatorCurent));
            }
        }

        private void BtnAdminUtilizatori_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ConfigurareAngajatiPage(utilizatorCurent));
            }
        }

        private void BtnAdminComenzi_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new AdministrareComenziPage(utilizatorCurent));
            }
        }

        private void BtnPlatiFacturi_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new PlatiFacturiPage(utilizatorCurent));
            }
        }

        private void BtnPieseAuto_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new PieseAutoPage(utilizatorCurent, "Toate"));
            }
        }

        private void BtnComandaMea_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ComandaMeaPage(utilizatorCurent));
            }
        }

        private void ProfilUtilizator_Click(object sender, RoutedEventArgs e)
        {
            if (utilizatorCurent != null)
            {
                MainFrame.Navigate(new ProfilUtilizator(utilizatorCurent));
            }
        }
    }
}
