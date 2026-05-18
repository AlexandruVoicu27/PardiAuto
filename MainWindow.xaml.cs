using System.Text;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Utilizator utilizatorCurent;

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated; 
            // Aplicatia porneste direct cu pagina de Login
            MainFrame.Navigate(new LoginPage());
        }

        public MainWindow(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
        }

        private void MainFrame_Navigated(object sender,NavigationEventArgs e) //functie sa nu am butonul de deconectare vizibil cand sunt pe pagina de login sau creare cont
        {
            if (MainFrame.Content is LoginPage || MainFrame.Content is CreateAccountPage ||  MainFrame.Content == null )
            {
                BtnDeconectare.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnDeconectare.Visibility = Visibility.Visible;
            }
        }



    }

}