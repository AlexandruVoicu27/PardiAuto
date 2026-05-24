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
using System.Windows.Shapes;

namespace AutoPartsShop
{
    /// <summary>
    /// Interaction logic for EditINFO.xaml
    /// </summary>
    public partial class EditINFO : Window
    {
        public string ValoareIntrodusa { get; private set; }

        public EditINFO(string valoare, string valoareVeche)
        {
            InitializeComponent();

            this.Title = valoare;
            LblMesaj.Text = $"Introdu noul/noua {valoare.ToLower()}:";
            TxtInput.Text = valoareVeche;

            TxtInput.Focus();
            TxtInput.SelectAll();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if(this.Title == "Număr de Telefon")
            {
                if(TxtInput.Text.Length != 10 || TxtInput.Text.All(char.IsDigit)==false)
                {
                    MessageBox.Show("Numărul de telefon trebuie să conțină exact 10 cifre!", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            ValoareIntrodusa = TxtInput.Text.Trim();
            this.DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

}

