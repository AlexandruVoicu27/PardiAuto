using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace AutoPartsShop
{
    /// <summary>
    /// Interaction logic for PieseAutoPage.xaml
    /// </summary>
    /// 
    public partial class PieseAutoPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<Produs> produse = new List<Produs>();

        public PieseAutoPage(Utilizator utilizator)
            : this(utilizator, "", "")
        {
        }

        public PieseAutoPage(Utilizator utilizator, string categorie)
            : this(utilizator, categorie, "")
        {
        }

        public PieseAutoPage(Utilizator utilizator, string categorie, string cautare)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;

            //doar adminu si angajatu  poate adauga sterge si modifica produsele
            bool poateAdmin = utilizatorCurent.Rol == RolUtilizator.Administrator || utilizatorCurent.Rol == RolUtilizator.Angajat;
            if (PanouAdministrare != null)
            {
                if (poateAdmin == true)
                {
                    PanouAdministrare.Visibility = Visibility.Visible;
                    PanouCumparare.Visibility= Visibility.Collapsed;
                }
                else
                {
                    PanouAdministrare.Visibility = Visibility.Collapsed;
                    PanouCumparare.Visibility = Visibility.Visible;
                }
            }

            SeteazaCategorie(CmbCategorieFiltru, categorie);
            TxtCautaProdus.Text = cautare;
            SchimbaTitluPagina();
            IncarcaProduse();
        }

        private void BtnReincarca_Click(object sender, RoutedEventArgs e)
        {
            IncarcaProduse();
        }

        private void BtnFiltreaza_Click(object sender, RoutedEventArgs e)
        {
            AplicaFiltre();
        }

        private void BtnReseteazaFiltre_Click(object sender, RoutedEventArgs e)
        {
            TxtCautaProdus.Clear();
            TxtPretMinim.Clear();
            TxtPretMaxim.Clear();
            CmbCategorieFiltru.SelectedIndex = 0;
            CmbStoc.SelectedIndex = 0;
            CmbSortare.SelectedIndex = 0;
            AplicaFiltre();
            SchimbaTitluPagina();
        }

        private void Filtru_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicaFiltre();
        }

        private void Filtru_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicaFiltre();
            SchimbaTitluPagina();
        }

        private void BtnAdauga_Click(object sender, RoutedEventArgs e)
        {
            if (!ValideazaProdus(out string nume, out decimal pret, out int cantitate))
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string categorie = TextComboBox(CmbCategorieProdus);

                string query = @"
            INSERT INTO Produs (Nume, Descriere, Categorie, Pret, Cantitate)
            VALUES (@Nume, @Descriere, @Categorie, @Pret, @Cantitate)";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nume", nume);
                cmd.Parameters.AddWithValue("@Descriere", TxtDescriereProdus.Text.Trim());
                cmd.Parameters.AddWithValue("@Categorie", categorie);
                cmd.Parameters.AddWithValue("@Pret", pret); 
                cmd.Parameters.AddWithValue("@Cantitate", cantitate);
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Produs adaugat", "Utilizator: " + utilizatorCurent.Email + ", produs: " + nume + ", categorie: " + categorie + ", pret: " + pret + ", cantitate: " + cantitate);

                CurataFormular();
                IncarcaProduse();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la adăugarea produsului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizeaza_Click(object sender, RoutedEventArgs e)
        {
            if (ProduseGrid.SelectedItem is not Produs produs)
            {
                MessageBox.Show("Selectează un produs pentru actualizare.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValideazaProdus(out string nume, out decimal pret, out int cantitate))
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string categorie = TextComboBox(CmbCategorieProdus);

                string query = @"
                UPDATE Produs
                SET Nume = @Nume,
                    Descriere = @Descriere,
                    Pret = @Pret,
                    Cantitate = @Cantitate,
                    Categorie = @Categorie
                WHERE ID = @ID";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", produs.ID);
                cmd.Parameters.AddWithValue("@Nume", nume);
                cmd.Parameters.AddWithValue("@Descriere", TxtDescriereProdus.Text.Trim());
                cmd.Parameters.AddWithValue("@Pret", pret);
                cmd.Parameters.AddWithValue("@Cantitate", cantitate);
                cmd.Parameters.AddWithValue("@Categorie", categorie);
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Produs actualizat", "Utilizator: " + utilizatorCurent.Email + ", ID produs: " + produs.ID + ", produs: " + nume + ", categorie: " + categorie + ", pret: " + pret + ", cantitate: " + cantitate);

                CurataFormular();
                IncarcaProduse();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea produsului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSterge_Click(object sender, RoutedEventArgs e)
        {

            if (ProduseGrid.SelectedItem is not Produs produs)
            {
                MessageBox.Show("Selectează un produs pentru ștergere.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Sigur vrei să ștergi produsul selectat?", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "DELETE FROM Produs WHERE ID = @ID";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", produs.ID);
                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Produs sters", "Utilizator: " + utilizatorCurent.Email + ", ID produs: " + produs.ID + ", produs: " + produs.Nume + ", categorie: " + produs.Categorie);

                CurataFormular();
                IncarcaProduse();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Produsul nu poate fi șters dacă apare deja într-o comandă. Detalii: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCumpara_Click(object sender, RoutedEventArgs e)
        {
            if (ProduseGrid.SelectedItem is not Produs produs)
            {
                MessageBox.Show("Selectează un produs pentru cumpărare.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtCantitateCumparare.Text.Trim(), out int cantitateCumparata) || cantitateCumparata <= 0)
            {
                MessageBox.Show("Cantitatea cumpărată trebuie să fie un număr pozitiv.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                CumparaProdus(produs.ID, cantitateCumparata);
                AppLogger.Scrie("Produs adaugat in comanda", "Utilizator: " + utilizatorCurent.Email + ", produs: " + produs.Nume + ", ID produs: " + produs.ID + ", cantitate: " + cantitateCumparata);
                MessageBox.Show("Produsul a fost adaugat in Comanda mea.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                IncarcaProduse();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Stoc insuficient", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la cumpărarea produsului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    //afiseaza toate datele in panoul adminului atunci cand vrei sa dai update 

        private void IncarcaDetaliiProdus(object sender, SelectionChangedEventArgs e)
        {
            if (ProduseGrid.SelectedItem is not Produs produs)
            {
                return;
            }

            TxtNumeProdus.Text = produs.Nume;
            TxtPretProdus.Text = produs.Pret.ToString("0.00");
            TxtCantitateProdus.Text = produs.Cantitate.ToString();
            SeteazaCategorie(CmbCategorieProdus, produs.Categorie);
            TxtDescriereProdus.Text = produs.Descriere;
        }
       
        //incarca produsele in panoul de produse si le ordoneaza alfabetic
        private void IncarcaProduse()
        {
            try
            {
                produse.Clear();

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
            SELECT ID, Nume, Descriere, Categorie, Pret, Cantitate
            FROM Produs
            ORDER BY Nume";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                //daca nu folosesc while ul pusca, ca trb sa actually existe ceva produse ca sa le citeasca.

                while (reader.Read())
                {
                    Guid ID = (Guid)reader["ID"];
                    string Nume = Convert.ToString(reader["Nume"]) ?? "";
                    string Descriere = Convert.ToString(reader["Descriere"]) ?? "";
                    string Categorie = Convert.ToString(reader["Categorie"]) ?? "";
                    decimal Pret = Convert.ToDecimal(reader["Pret"]);
                    int Cantitate = Convert.ToInt32(reader["Cantitate"]);

                    produse.Add(new Produs(ID, Nume, Descriere, Categorie, Pret, Cantitate));
                }

                AplicaFiltre();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la încărcarea produselor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicaFiltre()
        {
            if (ProduseGrid == null)
            {
                return;
            }

            string cautare = TxtCautaProdus.Text.Trim(); //text cautat
            string categorieAleasa = TextComboBox(CmbCategorieFiltru); //combobox categorie

            //transfrom textul in numar si il trimit direct in variabila
            bool arePretMinim = decimal.TryParse(TxtPretMinim.Text.Trim(), out decimal pretMinim); 
            bool arePretMaxim = decimal.TryParse(TxtPretMaxim.Text.Trim(), out decimal pretMaxim);

            List<Produs> produseFiltrate = new List<Produs>();

            foreach (Produs produs in produse)
            {
                bool sePotriveste = true;

                if (!string.IsNullOrWhiteSpace(cautare))
                {
                    //cautare in nume si descriere
                    bool gasitInNume = produs.Nume.IndexOf(cautare, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool gasitInDescriere = produs.Descriere.IndexOf(cautare, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (gasitInNume == false && gasitInDescriere == false)
                    {
                        sePotriveste = false;
                    }
                }

                if (arePretMinim == true && produs.Pret < pretMinim)
                {
                    sePotriveste = false;
                }

                if (arePretMaxim == true && produs.Pret > pretMaxim)
                {
                    sePotriveste = false;
                }

                //daca categoria nu este "Toate" iar produsul gasit nu are aceeasi categorie cu cea aleasa= false   
                if (categorieAleasa != "Toate" && !string.Equals(produs.Categorie, categorieAleasa, StringComparison.OrdinalIgnoreCase))
                {
                    sePotriveste = false;
                }

                if (CmbStoc.SelectedIndex == 1 && produs.Cantitate <= 0)
                {
                    sePotriveste = false;
                }

                if (CmbStoc.SelectedIndex == 2 && produs.Cantitate > 0)
                {
                    sePotriveste = false;
                }

                if (sePotriveste == true)
                {
                    produseFiltrate.Add(produs);
                }
            }

            SorteazaProduse(produseFiltrate);

            ProduseGrid.ItemsSource = null;
            ProduseGrid.ItemsSource = produseFiltrate;
            TxtNumarProduse.Text = "Produse afisate: " + produseFiltrate.Count;
        }

        private void SorteazaProduse(List<Produs> produseFiltrate)
        {
            //fcuntii lambda pentru a sorta dupa pret crescator, descrescator, stoc sau alfabetic
            if (CmbSortare.SelectedIndex == 1)
            {
                produseFiltrate.Sort((p1, p2) => p1.Pret.CompareTo(p2.Pret));
            }
            else if (CmbSortare.SelectedIndex == 2)
            {
                produseFiltrate.Sort((p1, p2) => p2.Pret.CompareTo(p1.Pret));
            }
            else if (CmbSortare.SelectedIndex == 3)
            {
                produseFiltrate.Sort((p1, p2) => p2.Cantitate.CompareTo(p1.Cantitate));
            }
            else
            {
                produseFiltrate.Sort((p1, p2) => string.Compare(p1.Nume, p2.Nume, StringComparison.OrdinalIgnoreCase));
            }
        }

        private string TextComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString() ?? "";  //da return la textul din combo box sau un string gol daca e null
            }

            return "";
        }

        private void SeteazaCategorie(ComboBox comboBox, string categorie)
        {
            if (string.IsNullOrWhiteSpace(categorie))
            {
                comboBox.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && string.Equals(item.Content.ToString(), categorie, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private void SchimbaTitluPagina()
        {
            if (TxtTitluPagina == null || TxtDescrierePagina == null || CmbCategorieFiltru == null)
            {
                return;
            }

            string categorie = TextComboBox(CmbCategorieFiltru);

            if (string.IsNullOrWhiteSpace(categorie) || categorie == "Toate")
            {
                TxtTitluPagina.Text = "Produse";
                TxtDescrierePagina.Text = "Alege categoria si filtrele potrivite.";
            }
            else
            {
                TxtTitluPagina.Text = categorie;
                TxtDescrierePagina.Text = "Produse filtrate dupa categoria selectata.";
            }
        }

        private void CumparaProdus(Guid produsId, int cantitateCumparata)
        { 
            

            using SqlConnection conn = new SqlConnection(ConnectionString);
            conn.Open();

            using SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                string stocQuery = "SELECT Cantitate FROM Produs WHERE ID = @ID";

                using SqlCommand stocCmd = new SqlCommand(stocQuery, conn, transaction);
                stocCmd.Parameters.AddWithValue("@ID", produsId);
              
                object? stocRezultat = stocCmd.ExecuteScalar();
                if (stocRezultat == null)
                {
                    throw new InvalidOperationException("Produsul selectat nu mai există.");
                }

                int stocCurent = Convert.ToInt32(stocRezultat, CultureInfo.InvariantCulture);
                if (stocCurent < cantitateCumparata)
                {
                    throw new InvalidOperationException("Stoc insuficient pentru produsul selectat.");
                }

                //comanda
                Guid comandaId = Guid.NewGuid();

                string comandaQuery = "INSERT INTO Comanda (ID, ID_Utilizator) VALUES (@ID, @ID_Utilizator)";
                using SqlCommand comandaCmd = new SqlCommand(comandaQuery, conn, transaction);

                comandaCmd.Parameters.AddWithValue("@ID", comandaId);
                comandaCmd.Parameters.AddWithValue("@ID_Utilizator", utilizatorCurent.ID);
                comandaCmd.ExecuteNonQuery();

                //bonul
                string detaliiQuery = @"
            INSERT INTO ComandaProduse (ID_Comanda, ID_Produs, Cantitate)
            VALUES (@ID_Comanda, @ID_Produs, @Cantitate)";


                using SqlCommand detaliiCmd = new SqlCommand(detaliiQuery, conn, transaction);
                detaliiCmd.Parameters.AddWithValue("@ID_Comanda", comandaId);
                detaliiCmd.Parameters.AddWithValue("@ID_Produs", produsId);
                detaliiCmd.Parameters.AddWithValue("@Cantitate", cantitateCumparata);
                detaliiCmd.ExecuteNonQuery();

                //Actualizam cantitatea produsului
                string updateStocQuery = "UPDATE Produs SET Cantitate = Cantitate - @Cantitate WHERE ID = @ID";
                using SqlCommand updateStocCmd = new SqlCommand(updateStocQuery, conn, transaction);
                updateStocCmd.Parameters.AddWithValue("@ID", produsId);
                updateStocCmd.Parameters.AddWithValue("@Cantitate", cantitateCumparata);
                updateStocCmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        //trateaza erori
        private bool ValideazaProdus(out string nume, out decimal pret, out int cantitate)
        {
            nume = TxtNumeProdus.Text.Trim();
            pret = 0;
            cantitate = 0;

            if (string.IsNullOrWhiteSpace(nume))
            {
                MessageBox.Show("Numele produsului este obligatoriu.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(TxtPretProdus.Text.Trim(), out pret) || pret < 0)
            {
                MessageBox.Show("Prețul trebuie să fie un număr pozitiv.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(TxtCantitateProdus.Text.Trim(), out cantitate) || cantitate < 0)
            {
                MessageBox.Show("Cantitatea trebuie să fie un număr întreg pozitiv.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        //pentru a da clear dupa ce introduci o piesa
        private void CurataFormular()
        {
            TxtNumeProdus.Clear();
            TxtPretProdus.Clear();
            TxtCantitateProdus.Clear();
            TxtDescriereProdus.Clear();
            CmbCategorieProdus.SelectedIndex = 0;
            ProduseGrid.SelectedItem = null;
        }
    }

}
