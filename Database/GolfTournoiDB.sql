-- =============================================================
-- BASE DE DONNÉES : GolfTournoiDB
-- Projet : Tournoi de Golf G06 - La Cité
-- =============================================================

-- 1) Créer la base de données
IF DB_ID('GolfTournoiDB') IS NULL
    CREATE DATABASE GolfTournoiDB;
GO

USE GolfTournoiDB;
GO

-- 2) Supprimer les tables si elles existent (reset)
IF OBJECT_ID('Commandites', 'U') IS NOT NULL DROP TABLE Commandites;
IF OBJECT_ID('Participants', 'U') IS NOT NULL DROP TABLE Participants;
IF OBJECT_ID('Equipes', 'U') IS NOT NULL DROP TABLE Equipes;
IF OBJECT_ID('Tournois', 'U') IS NOT NULL DROP TABLE Tournois;
IF OBJECT_ID('Utilisateurs', 'U') IS NOT NULL DROP TABLE Utilisateurs;
GO

-- =============================================================
-- TABLES
-- =============================================================

-- 3) TABLE UTILISATEURS
CREATE TABLE Utilisateurs (
    UtilisateurId INT IDENTITY PRIMARY KEY,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    MotDePasseHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(30) NOT NULL, -- ADMIN / PARTICIPANT / COMMANDITAIRE
    Prenom NVARCHAR(60),
    Nom NVARCHAR(60),
    Telephone NVARCHAR(30),
    DateNaissance DATE,
    Adresse NVARCHAR(150),
    CreeLe DATETIME DEFAULT GETDATE()
);
GO

-- 4) TABLE TOURNOIS
CREATE TABLE Tournois (
    TournoiId INT IDENTITY PRIMARY KEY,
    Nom NVARCHAR(120),
    DateTournoi DATE,
    Lieu NVARCHAR(150),
    InscriptionsOuvertes BIT DEFAULT 0,
    NbEquipesMax INT,
    PlacesParticipantsMax INT,
    CreeLe DATETIME DEFAULT GETDATE()
);
GO

-- 5) TABLE EQUIPES
CREATE TABLE Equipes (
    EquipeId INT IDENTITY PRIMARY KEY,
    TournoiId INT,
    NomEquipe NVARCHAR(80),
    CodeSecret NVARCHAR(20) UNIQUE,
    CreeParUtilisateurId INT,
    CreeLe DATETIME DEFAULT GETDATE()
);
GO

-- 6) TABLE PARTICIPANTS
CREATE TABLE Participants (
    ParticipantId INT IDENTITY PRIMARY KEY,
    TournoiId INT,
    UtilisateurId INT,
    EquipeId INT NULL,
    StatutInscription NVARCHAR(20) DEFAULT 'CONFIRMEE',
    MontantPaye DECIMAL(10,2),
    CreeLe DATETIME DEFAULT GETDATE()
);
GO

-- 7) TABLE COMMANDITES
CREATE TABLE Commandites (
    CommanditeId INT IDENTITY PRIMARY KEY,
    TournoiId INT,
    UtilisateurId INT,
    TypeCommandite NVARCHAR(120),
    Montant DECIMAL(10,2),
    NomEntreprise NVARCHAR(150),
    CreeLe DATETIME DEFAULT GETDATE()
);
GO

-- =============================================================
-- CLÉS ÉTRANGÈRES
-- =============================================================

ALTER TABLE Equipes
ADD FOREIGN KEY (TournoiId) REFERENCES Tournois(TournoiId);

ALTER TABLE Equipes
ADD FOREIGN KEY (CreeParUtilisateurId) REFERENCES Utilisateurs(UtilisateurId);

ALTER TABLE Participants
ADD FOREIGN KEY (TournoiId) REFERENCES Tournois(TournoiId);

ALTER TABLE Participants
ADD FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId);

ALTER TABLE Participants
ADD FOREIGN KEY (EquipeId) REFERENCES Equipes(EquipeId);

ALTER TABLE Commandites
ADD FOREIGN KEY (TournoiId) REFERENCES Tournois(TournoiId);

ALTER TABLE Commandites
ADD FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId);
GO

-- =============================================================
-- INDEX
-- =============================================================

CREATE INDEX IX_Tournois_Id ON Tournois(TournoiId);
CREATE INDEX IX_Participants_TournoiId ON Participants(TournoiId);
CREATE INDEX IX_Equipes_TournoiId ON Equipes(TournoiId);
GO

-- =============================================================
-- PROCÉDURES STOCKÉES
-- =============================================================

-- Ajouter un tournoi
CREATE OR ALTER PROCEDURE sp_AjouterTournoi
    @Nom NVARCHAR(120),
    @DateTournoi DATE,
    @Lieu NVARCHAR(150),
    @NbEquipesMax INT,
    @PlacesParticipantsMax INT
AS
BEGIN
    INSERT INTO Tournois
    VALUES (@Nom, @DateTournoi, @Lieu, 0, @NbEquipesMax, @PlacesParticipantsMax, GETDATE());
END;
GO

-- Ajouter un utilisateur
CREATE OR ALTER PROCEDURE sp_AjouterUtilisateur
    @Email NVARCHAR(150),
    @MotDePasseHash NVARCHAR(255),
    @Role NVARCHAR(30),
    @Prenom NVARCHAR(60),
    @Nom NVARCHAR(60),
    @DateNaissance DATE,
    @Telephone NVARCHAR(20)
AS
BEGIN
    INSERT INTO Utilisateurs (Email, MotDePasseHash, Role, Prenom, Nom, DateNaissance, Telephone)
    VALUES (@Email, @MotDePasseHash, @Role, @Prenom, @Nom, @DateNaissance, @Telephone);
END;
GO

-- Inscrire un participant
CREATE OR ALTER PROCEDURE sp_InscrireParticipant
    @TournoiId INT,
    @UtilisateurId INT,
    @Montant DECIMAL(10,2)
AS
BEGIN
    INSERT INTO Participants (TournoiId, UtilisateurId, MontantPaye)
    VALUES (@TournoiId, @UtilisateurId, @Montant);
END;
GO

-- =============================================================
-- DONNÉES DE TEST
-- =============================================================

-- Ajouter un tournoi de test
EXEC sp_AjouterTournoi
    @Nom = 'Tournoi La Cité 2026',
    @DateTournoi = '2026-06-10',
    @Lieu = 'Ottawa',
    @NbEquipesMax = 20,
    @PlacesParticipantsMax = 80;

-- Supprimer utilisateur test s'il existe
DELETE FROM Utilisateurs WHERE Email = 'participant@lacite.ca';

-- Ajouter un utilisateur participant de test
EXEC sp_AjouterUtilisateur
    @Email = 'participant@lacite.ca',
    @MotDePasseHash = 'hash123',
    @Role = 'PARTICIPANT',
    @Prenom = 'Chanez',
    @Nom = 'Meziane',
    @DateNaissance = '2002-09-02',
    @Telephone = '514-000-0000';

-- Inscrire le participant au tournoi
DECLARE @TournoiId INT;
DECLARE @UtilisateurId INT;

SELECT TOP 1 @TournoiId = TournoiId
FROM Tournois
ORDER BY TournoiId DESC;

SELECT TOP 1 @UtilisateurId = UtilisateurId
FROM Utilisateurs
WHERE Email = 'participant@lacite.ca';

EXEC sp_InscrireParticipant
    @TournoiId = @TournoiId,
    @UtilisateurId = @UtilisateurId,
    @Montant = 60.00;

-- Créer une équipe
INSERT INTO Equipes (TournoiId, NomEquipe, CodeSecret, CreeParUtilisateurId)
VALUES (@TournoiId, 'Equipe A', 'CODE123', @UtilisateurId);

-- Mettre le participant dans cette équipe
UPDATE Participants
SET EquipeId = (SELECT TOP 1 EquipeId FROM Equipes ORDER BY EquipeId DESC)
WHERE TournoiId = @TournoiId AND UtilisateurId = @UtilisateurId;

-- =============================================================
-- VÉRIFICATION
-- =============================================================

SELECT 'Base de données' AS Test, DB_NAME() AS Resultat;
SELECT * FROM Tournois;
SELECT * FROM Utilisateurs;
SELECT * FROM Equipes;
SELECT * FROM Participants;
