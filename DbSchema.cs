using Microsoft.Data.SqlClient;
using System;

namespace AutoPartsShop
{
    public static class DbSchema
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=PardiAutoDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static void AsiguraSchema()
        {
            using SqlConnection conn = new SqlConnection(ConnectionString);
            conn.Open();

            Executa(conn, @"
IF COL_LENGTH('Comanda', 'Total') IS NULL
BEGIN
    ALTER TABLE Comanda ADD Total DECIMAL(10, 2) NOT NULL CONSTRAINT DF_Comanda_Total DEFAULT 0
END");

            Executa(conn, @"
IF COL_LENGTH('Comanda', 'Observatii') IS NULL
BEGIN
    ALTER TABLE Comanda ADD Observatii NVARCHAR(500) NULL
END");

            Executa(conn, @"
IF OBJECT_ID('Facturi', 'U') IS NULL
BEGIN
    CREATE TABLE Facturi (
        ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        ID_Comanda UNIQUEIDENTIFIER NOT NULL UNIQUE,
        NumarFactura NVARCHAR(50) NOT NULL UNIQUE,
        DataEmitere DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Total DECIMAL(10, 2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Emisa',
        CONSTRAINT FK_Facturi_Comanda FOREIGN KEY (ID_Comanda) REFERENCES Comanda(ID)
    )
END");

            Executa(conn, @"
IF OBJECT_ID('Plati', 'U') IS NULL
BEGIN
    CREATE TABLE Plati (
        ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        ID_Comanda UNIQUEIDENTIFIER NOT NULL,
        ID_Factura UNIQUEIDENTIFIER NULL,
        DataPlata DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Suma DECIMAL(10, 2) NOT NULL,
        Metoda NVARCHAR(30) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'InAsteptare',
        Referinta NVARCHAR(100) NULL,
        CONSTRAINT FK_Plati_Comanda FOREIGN KEY (ID_Comanda) REFERENCES Comanda(ID),
        CONSTRAINT FK_Plati_Facturi FOREIGN KEY (ID_Factura) REFERENCES Facturi(ID)
    )
END");

            Executa(conn, @"
IF OBJECT_ID('Rapoarte', 'U') IS NULL
BEGIN
    CREATE TABLE Rapoarte (
        ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        TipRaport NVARCHAR(50) NOT NULL,
        DataGenerare DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        GeneratDe INT NULL,
        Detalii NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Rapoarte_Utilizatori FOREIGN KEY (GeneratDe) REFERENCES Utilizatori(Id)
    )
END");
        }

        private static void Executa(SqlConnection conn, string sql)
        {
            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
