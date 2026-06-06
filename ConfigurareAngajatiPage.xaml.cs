using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsShop
{
    public partial class ConfigurareAngajatiPage : Page
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly Utilizator utilizatorCurent;
        private readonly List<Utilizator> utilizatori = new List<Utilizator>();

        public ConfigurareAngajatiPage(Utilizator utilizator)
        {
            InitializeComponent();
            utilizatorCurent = utilizator;
            CmbRol.SelectedIndex = 0;

            if (utilizator.Rol == RolUtilizator.Angajat)
            {
                BtnPromoveazaAdministrator.Visibility = Visibility.Collapsed;
            }
            else if (utilizator.Rol == RolUtilizator.Administrator)
            {
                BtnPromoveazaAdministrator.Visibility = Visibility.Visible;
            }

            IncarcaUtilizatori();
        }

        private void BtnReincarcaUtilizatori_Click(object sender, RoutedEventArgs e)
        {
            IncarcaUtilizatori();
        }

        private void BtnCreeazaUtilizator_Click(object sender, RoutedEventArgs e)
        {
            if (!ValideazaFormular(necesitaParola: true, out string nume, out string email, out string parola, out RolUtilizator rol))
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
INSERT INTO Utilizatori (NumeComplet, Email, ParolaHash, Rol)
OUTPUT INSERTED.Id
VALUES (@NumeComplet, @Email, @ParolaHash, @Rol)";

                int idNou;
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NumeComplet", nume);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@ParolaHash", Security.HashParola(parola));
                    cmd.Parameters.AddWithValue("@Rol", rol.ToString());
                    idNou = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (SqlCommand cmd = new SqlCommand("INSERT INTO DateUtilizatori (UtilizatorId) VALUES (@UtilizatorId)", conn))
                {
                    cmd.Parameters.AddWithValue("@UtilizatorId", idNou);
                    cmd.ExecuteNonQuery();
                }

                AppLogger.Scrie("Utilizator creat", "Administrator: " + utilizatorCurent.Email + ", utilizator: " + email + ", rol: " + rol);
                CurataFormular();
                IncarcaUtilizatori();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la crearea utilizatorului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizeazaUtilizator_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (!ValideazaFormular(necesitaParola: false, out string nume, out string email, out string parola, out RolUtilizator rol))
            {
                return;
            }

            if (utilizator.ID == utilizatorCurent.ID && rol != utilizatorCurent.Rol)
            {
                MessageBox.Show("Nu iti schimba propriul rol din aceasta pagina.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = string.IsNullOrWhiteSpace(parola)
                    ? "UPDATE Utilizatori SET NumeComplet = @NumeComplet, Email = @Email, Rol = @Rol WHERE Id = @Id"
                    : "UPDATE Utilizatori SET NumeComplet = @NumeComplet, Email = @Email, ParolaHash = @ParolaHash, Rol = @Rol WHERE Id = @Id";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", utilizator.ID);
                cmd.Parameters.AddWithValue("@NumeComplet", nume);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Rol", rol.ToString());

                if (!string.IsNullOrWhiteSpace(parola))
                {
                    cmd.Parameters.AddWithValue("@ParolaHash", Security.HashParola(parola));
                }

                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Utilizator actualizat", "Administrator: " + utilizatorCurent.Email + ", utilizator: " + email + ", rol: " + rol);
                CurataFormular();
                IncarcaUtilizatori();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la actualizarea utilizatorului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStergeUtilizator_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.ID == utilizatorCurent.ID)
            {
                MessageBox.Show("Nu iti poti sterge propriul cont cat esti logat.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Sigur vrei sa stergi utilizatorul selectat? Se sterg si comenzile, platile si facturile lui.", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();
                DbSchema.AsiguraSchema();
                using SqlTransaction transaction = conn.BeginTransaction();

                Executa(conn, transaction, "DELETE p FROM Plati p INNER JOIN Comanda c ON p.ID_Comanda = c.ID WHERE c.ID_Utilizator = @Id", utilizator.ID);
                Executa(conn, transaction, "DELETE f FROM Facturi f INNER JOIN Comanda c ON f.ID_Comanda = c.ID WHERE c.ID_Utilizator = @Id", utilizator.ID);
                Executa(conn, transaction, "DELETE cp FROM ComandaProduse cp INNER JOIN Comanda c ON cp.ID_Comanda = c.ID WHERE c.ID_Utilizator = @Id", utilizator.ID);
                Executa(conn, transaction, "DELETE FROM Comanda WHERE ID_Utilizator = @Id", utilizator.ID);
                Executa(conn, transaction, "DELETE FROM DateUtilizatori WHERE UtilizatorId = @Id", utilizator.ID);
                Executa(conn, transaction, "DELETE FROM Utilizatori WHERE Id = @Id", utilizator.ID);

                transaction.Commit();

                AppLogger.Scrie("Utilizator sters", "Administrator: " + utilizatorCurent.Email + ", utilizator: " + utilizator.Email);
                CurataFormular();
                IncarcaUtilizatori();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la stergerea utilizatorului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPromoveazaAngajat_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Rolul de administrator nu poate fi modificat de aici.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (utilizator.Rol == RolUtilizator.Angajat)
            {
                MessageBox.Show("Utilizatorul este deja angajat.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Angajat);
        }

        private void BtnTransformaClient_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Rolul de administrator nu poate fi modificat de aici.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (utilizator.Rol == RolUtilizator.Client)
            {
                MessageBox.Show("Utilizatorul este deja client.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Client);
        }

        private void BtnPromoveazaAdministrator_Click(object sender, RoutedEventArgs e)
        {
            if (!SelecteazaUtilizator(out Utilizator utilizator))
            {
                return;
            }

            if (utilizator.Rol == RolUtilizator.Administrator)
            {
                MessageBox.Show("Utilizatorul este deja administrator.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SchimbaRolUtilizator(utilizator, RolUtilizator.Administrator);
        }

        private void UtilizatoriGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UtilizatoriGrid.SelectedItem is not Utilizator utilizator)
            {
                return;
            }

            TxtNume.Text = utilizator.Nume;
            TxtEmail.Text = utilizator.Email;
            TxtParola.Clear();
            SeteazaRol(utilizator.Rol);
        }

        private bool SelecteazaUtilizator(out Utilizator utilizator)
        {
            if (UtilizatoriGrid.SelectedItem is Utilizator utilizatorSelectat)
            {
                utilizator = utilizatorSelectat;
                return true;
            }

            utilizator = utilizatorCurent;
            MessageBox.Show("Selecteaza un utilizator.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private void IncarcaUtilizatori()
        {
            try
            {
                utilizatori.Clear();

                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT Id, NumeComplet, Email, Rol FROM Utilizatori ORDER BY Rol, NumeComplet";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["Id"]);
                    string nume = Convert.ToString(reader["NumeComplet"]) ?? "";
                    string email = Convert.ToString(reader["Email"]) ?? "";
                    string rolText = Convert.ToString(reader["Rol"]) ?? RolUtilizator.Client.ToString();
                    RolUtilizator rol = (RolUtilizator)Enum.Parse(typeof(RolUtilizator), rolText);

                    utilizatori.Add(Utilizator.CreateByRol(id, nume, email, rol));
                }

                UtilizatoriGrid.ItemsSource = null;
                UtilizatoriGrid.ItemsSource = utilizatori;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la incarcarea utilizatorilor: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SchimbaRolUtilizator(Utilizator utilizator, RolUtilizator rolNou)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = "UPDATE Utilizatori SET Rol = @Rol WHERE Id = @Id";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", utilizator.ID);
                cmd.Parameters.AddWithValue("@Rol", rolNou.ToString());

                cmd.ExecuteNonQuery();

                AppLogger.Scrie("Rol utilizator modificat", "Administrator: " + utilizatorCurent.Email + ", utilizator: " + utilizator.Email + ", rol vechi: " + utilizator.Rol + ", rol nou: " + rolNou);
                IncarcaUtilizatori();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Eroare la modificarea rolului: " + ex.Message, "Eroare SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Executa(SqlConnection conn, SqlTransaction transaction, string query, int id)
        {
            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        private bool ValideazaFormular(bool necesitaParola, out string nume, out string email, out string parola, out RolUtilizator rol)
        {
            nume = TxtNume.Text.Trim();
            email = TxtEmail.Text.Trim();
            parola = TxtParola.Password;
            rol = CitesteRolDinCombo();

            if (string.IsNullOrWhiteSpace(nume) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Numele si emailul sunt obligatorii.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (necesitaParola && string.IsNullOrWhiteSpace(parola))
            {
                MessageBox.Show("Parola este obligatorie pentru utilizator nou.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!email.Contains("@"))
            {
                MessageBox.Show("Emailul nu pare valid.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private RolUtilizator CitesteRolDinCombo()
        {
            if (CmbRol.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return (RolUtilizator)Enum.Parse(typeof(RolUtilizator), item.Content.ToString() ?? "Client");
            }

            return RolUtilizator.Client;
        }

        private void SeteazaRol(RolUtilizator rol)
        {
            for (int i = 0; i < CmbRol.Items.Count; i++)
            {
                if (CmbRol.Items[i] is ComboBoxItem item && item.Content != null && item.Content.ToString() == rol.ToString())
                {
                    CmbRol.SelectedIndex = i;
                    return;
                }
            }

            CmbRol.SelectedIndex = 0;
        }

        private void CurataFormular()
        {
            TxtNume.Clear();
            TxtEmail.Clear();
            TxtParola.Clear();
            CmbRol.SelectedIndex = 0;
        }
    }
}
