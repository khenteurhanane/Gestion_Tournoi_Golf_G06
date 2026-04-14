using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using croupe_06_TournoiGolf.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using QRCoder;

namespace croupe_06_TournoiGolf.Services;

public class TicketService
{
    public byte[] GenererBilletPdf(Participant participant)
    {
        if (participant.Tournoi is null)
        {
            throw new InvalidOperationException("Le tournoi doit etre charge pour generer un billet.");
        }

        var nomParticipant = ConstruireNomParticipant(participant);
        var codeBillet = ConstruireCodeBillet(participant);
        var contenuQr = ConstruireContenuQr(participant, nomParticipant, codeBillet);
        var qrCodeBytes = GenererQrCode(contenuQr);

        using var outputStream = new MemoryStream();
        using var document = new Document(PageSize.A4, 36f, 36f, 40f, 36f);
        PdfWriter.GetInstance(document, outputStream);

        document.Open();

        var titreFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22f, new BaseColor(0, 0, 0));
        var sousTitreFont = FontFactory.GetFont(FontFactory.HELVETICA, 11f, new BaseColor(80, 80, 80));
        var etiquetteFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, new BaseColor(26, 46, 18));
        var valeurFont = FontFactory.GetFont(FontFactory.HELVETICA, 11f, new BaseColor(0, 0, 0));
        var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9f, new BaseColor(90, 90, 90));

        var titre = new Paragraph("Billet d'entree - Tournoi G06", titreFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 8f
        };
        document.Add(titre);

        var sousTitre = new Paragraph("Presentez ce billet a l'entree le jour du tournoi.", sousTitreFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 18f
        };
        document.Add(sousTitre);

        var table = new PdfPTable(2)
        {
            WidthPercentage = 100,
            SpacingAfter = 18f
        };
        table.SetWidths(new[] { 1.2f, 2.8f });

        AjouterLigne(table, "Participant", nomParticipant, etiquetteFont, valeurFont);
        AjouterLigne(table, "Tournoi", participant.Tournoi.Nom, etiquetteFont, valeurFont);
        AjouterLigne(
            table,
            "Date",
            participant.Tournoi.DateTournoi.ToString("dddd d MMMM yyyy", CultureInfo.GetCultureInfo("fr-CA")),
            etiquetteFont,
            valeurFont);
        AjouterLigne(table, "Lieu", participant.Tournoi.Lieu, etiquetteFont, valeurFont);
        AjouterLigne(table, "Code billet", codeBillet, etiquetteFont, valeurFont);

        document.Add(table);

        var qrTitre = new Paragraph("QR Code de validation", etiquetteFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 10f
        };
        document.Add(qrTitre);

        var qrImage = Image.GetInstance(qrCodeBytes);
        qrImage.Alignment = Element.ALIGN_CENTER;
        qrImage.ScaleToFit(190f, 190f);
        qrImage.SpacingAfter = 12f;
        document.Add(qrImage);

        var footer = new Paragraph(
            "Le QR code contient un identifiant unique associe a cette inscription. Conservez ce billet jusqu'a l'evenement.",
            footerFont)
        {
            Alignment = Element.ALIGN_CENTER
        };
        document.Add(footer);

        document.Close();
        return outputStream.ToArray();
    }

    private static void AjouterLigne(PdfPTable table, string etiquette, string valeur, Font etiquetteFont, Font valeurFont)
    {
        table.AddCell(CreerCellule(etiquette, etiquetteFont, new BaseColor(245, 249, 242)));
        table.AddCell(CreerCellule(valeur, valeurFont, new BaseColor(255, 255, 255)));
    }

    private static PdfPCell CreerCellule(string contenu, Font font, BaseColor backgroundColor)
    {
        return new PdfPCell(new Phrase(contenu, font))
        {
            BackgroundColor = backgroundColor,
            BorderColor = new BaseColor(220, 228, 214),
            Padding = 10f,
            VerticalAlignment = Element.ALIGN_MIDDLE
        };
    }

    private static string ConstruireNomParticipant(Participant participant)
    {
        var prenom = participant.Utilisateur?.Prenom ?? participant.Prenom ?? string.Empty;
        var nom = participant.Utilisateur?.Nom ?? participant.Nom ?? string.Empty;
        var nomComplet = $"{prenom} {nom}".Trim();

        if (!string.IsNullOrWhiteSpace(nomComplet))
        {
            return nomComplet;
        }

        return participant.Email
            ?? participant.Utilisateur?.Email
            ?? $"Participant #{participant.ParticipantId}";
    }

    private static string ConstruireCodeBillet(Participant participant)
    {
        var empreinte = string.Join(
            "|",
            participant.ParticipantId,
            participant.TournoiId,
            participant.UtilisateurId,
            participant.Email ?? participant.Utilisateur?.Email ?? string.Empty,
            participant.CreeLe.ToUniversalTime().Ticks);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(empreinte));
        return Convert.ToHexString(hash)[..24];
    }

    private static string ConstruireContenuQr(Participant participant, string nomParticipant, string codeBillet)
    {
        return string.Join(
            "\n",
            "GOLF-G06",
            $"BILLET={codeBillet}",
            $"PARTICIPANT_ID={participant.ParticipantId}",
            $"TOURNOI_ID={participant.TournoiId}",
            $"NOM={nomParticipant}",
            $"DATE={participant.Tournoi!.DateTournoi:yyyy-MM-dd}",
            $"LIEU={participant.Tournoi.Lieu}");
    }

    private static byte[] GenererQrCode(string contenuQr)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(contenuQr, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}
