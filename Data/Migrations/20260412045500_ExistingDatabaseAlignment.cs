using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace croupe_06_TournoiGolf.Data.Migrations
{
    [DbContext(typeof(GolfDbContext))]
    [Migration("20260412045500_ExistingDatabaseAlignment")]
    public partial class ExistingDatabaseAlignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL AND COL_LENGTH('Commandites', 'Commentaire') IS NULL
                BEGIN
                    ALTER TABLE [Commandites] ADD [Commentaire] NVARCHAR(500) NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL
                   AND COL_LENGTH('Commandites', 'Statut') IS NULL
                BEGIN
                    ALTER TABLE [Commandites] ADD [Statut] NVARCHAR(50) NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL
                   AND COL_LENGTH('Commandites', 'Statut') IS NOT NULL
                BEGIN
                    UPDATE [Commandites]
                    SET [Statut] = N'EN_ATTENTE_PAIEMENT'
                    WHERE [Statut] IS NULL;

                    ALTER TABLE [Commandites] ALTER COLUMN [Statut] NVARCHAR(50) NOT NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL
                   AND COL_LENGTH('Commandites', 'DateCreation') IS NULL
                BEGIN
                    ALTER TABLE [Commandites] ADD [DateCreation] DATETIME2 NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL
                   AND COL_LENGTH('Commandites', 'DateCreation') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Commandites', 'CreeLe') IS NOT NULL
                    BEGIN
                        UPDATE [Commandites]
                        SET [DateCreation] = COALESCE([DateCreation], CONVERT(DATETIME2, [CreeLe]));
                    END;

                    UPDATE [Commandites]
                    SET [DateCreation] = SYSDATETIME()
                    WHERE [DateCreation] IS NULL;

                    ALTER TABLE [Commandites] ALTER COLUMN [DateCreation] DATETIME2 NOT NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Utilisateurs]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Utilisateurs]')
                         AND name = N'IX_Utilisateurs_Email'
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Utilisateurs_Email] ON [Utilisateurs] ([Email]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Equipes]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Equipes]')
                         AND name = N'IX_Equipes_CodeSecret'
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Equipes_CodeSecret] ON [Equipes] ([CodeSecret]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Participants]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Participants]')
                         AND name = N'IX_Participants_TournoiId_UtilisateurId'
                   )
                BEGIN
                    CREATE INDEX [IX_Participants_TournoiId_UtilisateurId]
                    ON [Participants] ([TournoiId], [UtilisateurId]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Participants]', N'U') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Participants]')
                         AND name = N'IX_Participants_TournoiId_UtilisateurId'
                   )
                BEGIN
                    DROP INDEX [IX_Participants_TournoiId_UtilisateurId] ON [Participants];
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Equipes]', N'U') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Equipes]')
                         AND name = N'IX_Equipes_CodeSecret'
                   )
                BEGIN
                    DROP INDEX [IX_Equipes_CodeSecret] ON [Equipes];
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Utilisateurs]', N'U') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID(N'[Utilisateurs]')
                         AND name = N'IX_Utilisateurs_Email'
                   )
                BEGIN
                    DROP INDEX [IX_Utilisateurs_Email] ON [Utilisateurs];
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL AND COL_LENGTH('Commandites', 'DateCreation') IS NOT NULL
                BEGIN
                    ALTER TABLE [Commandites] DROP COLUMN [DateCreation];
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL AND COL_LENGTH('Commandites', 'Statut') IS NOT NULL
                BEGIN
                    ALTER TABLE [Commandites] DROP COLUMN [Statut];
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Commandites]', N'U') IS NOT NULL AND COL_LENGTH('Commandites', 'Commentaire') IS NOT NULL
                BEGIN
                    ALTER TABLE [Commandites] DROP COLUMN [Commentaire];
                END
                """);
        }
    }
}
