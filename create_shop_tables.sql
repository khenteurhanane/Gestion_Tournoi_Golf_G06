USE GolfTournoiDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommandesBoutique')
BEGIN
    CREATE TABLE CommandesBoutique (
        CommandeId INT IDENTITY(1,1) PRIMARY KEY,
        UtilisateurId INT NULL,
        SousTotal DECIMAL(18,2) NOT NULL,
        Rabais DECIMAL(18,2) NOT NULL,
        Taxes DECIMAL(18,2) NOT NULL,
        TotalFinal DECIMAL(18,2) NOT NULL,
        ModePaiement NVARCHAR(50) NOT NULL,
        DateCommande DATETIME2 NOT NULL,
        CONSTRAINT FK_CommandesBoutique_Utilisateurs_UtilisateurId FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ItemsCommandesBoutique')
BEGIN
    CREATE TABLE ItemsCommandesBoutique (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        CommandeId INT NOT NULL,
        ArticleId INT NOT NULL,
        ArticleNom NVARCHAR(100) NOT NULL,
        PrixUnitaire DECIMAL(18,2) NOT NULL,
        Quantite INT NOT NULL,
        CONSTRAINT FK_ItemsCommandesBoutique_CommandesBoutique_CommandeId FOREIGN KEY (CommandeId) REFERENCES CommandesBoutique(CommandeId) ON DELETE CASCADE
    );
END
GO
