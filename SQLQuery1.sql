CREATE TABLE Utilizatori (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeComplet NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    ParolaHash NVARCHAR(255) NOT NULL,
    Rol NVARCHAR(20) DEFAULT 'Client'
);

CREATE TABLE DateUtilizatori (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UtilizatorId INT NOT NULL UNIQUE,
    NumarTelefon NVARCHAR(10) NULL,
    Adresa NVARCHAR(500) NULL,
    CONSTRAINT FK_DateUtilizatori_Utilizatori
        FOREIGN KEY (UtilizatorId) REFERENCES Utilizatori(Id)
        ON DELETE CASCADE
);
