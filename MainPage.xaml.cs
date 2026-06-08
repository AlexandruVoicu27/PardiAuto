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
    
    public partial class MainPage : Page
    {
        private Utilizator utilizatorCurent;

        
        public MainPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
        }

       
        private void BtnCauta_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PieseAutoPage(utilizatorCurent, "", SearchBox.Text.Trim()));
        }

       
        private void Uleiuri_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new PieseAutoPage(utilizatorCurent, "Uleiuri & Fluide"));
        }

        
        private void FiltreAuto_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new PieseAutoPage(utilizatorCurent, "Filtre Auto"));
        }

        
        private void SistemFranare_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new PieseAutoPage(utilizatorCurent, "Sistem Frânare"));
        }

        private void SuspensieDirectie_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(new PieseAutoPage(utilizatorCurent, "Suspensie & Direcție"));
        }
    }
}
