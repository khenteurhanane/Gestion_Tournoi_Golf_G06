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
    private static readonly BaseColor Navy = new(63, 96, 139);
    private static readonly BaseColor NavyDark = new(52, 86, 129);
    private static readonly BaseColor NeutralBorder = new(193, 201, 188);
    private static readonly BaseColor NeutralPanel = new(238, 244, 252);
    private static readonly BaseColor NeutralText = new(0, 0, 0);
    private static readonly BaseColor MutedText = new(80, 80, 80);

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

        var titreFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22f, NeutralText);
        var sousTitreFont = FontFactory.GetFont(FontFactory.HELVETICA, 11f, MutedText);
        var etiquetteFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, NavyDark);
        var valeurFont = FontFactory.GetFont(FontFactory.HELVETICA, 11f, NeutralText);
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
        table.AddCell(CreerCellule(etiquette, etiquetteFont, NeutralPanel));
        table.AddCell(CreerCellule(valeur, valeurFont, BaseColor.White));
    }

    private static PdfPCell CreerCellule(string contenu, Font font, BaseColor backgroundColor)
    {
        return new PdfPCell(new Phrase(contenu, font))
        {
            BackgroundColor = backgroundColor,
            BorderColor = NeutralBorder,
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

    public byte[] GenererRecuPdf(CommandeBoutique commande)
    {
        using var outputStream = new MemoryStream();
        using var document = new Document(PageSize.A4, 36f, 36f, 40f, 36f);
        PdfWriter.GetInstance(document, outputStream);

        document.Open();

        var titreFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22f, Navy);
        var sousTitreFont = FontFactory.GetFont(FontFactory.HELVETICA, 11f, MutedText);
        var etiquetteFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, NavyDark);
        var valeurFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, NeutralText);
        var itemsTableFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, NeutralText);
        var itemsHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, BaseColor.White);
        var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9f, new BaseColor(90, 90, 90));

        var titre = new Paragraph("Recu de transaction - Boutique Golf G06", titreFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 8f
        };
        document.Add(titre);

        var dateStr = commande.DateCommande.ToString("dddd d MMMM yyyy HH:mm", CultureInfo.GetCultureInfo("fr-CA"));
        var numCommande = $"#{commande.CommandeId:D6}";
        var sousTitre = new Paragraph($"Commande {numCommande} - Date : {dateStr}", sousTitreFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20f
        };
        document.Add(sousTitre);

        var clientNom = $"{commande.Utilisateur?.Prenom} {commande.Utilisateur?.Nom}".Trim();
        if (string.IsNullOrWhiteSpace(clientNom))
        {
            clientNom = commande.Utilisateur?.Email ?? "Client Boutique";
        }

        var clientInfo = new Paragraph($"Client : {clientNom}", etiquetteFont)
        {
            Alignment = Element.ALIGN_LEFT,
            SpacingAfter = 15f
        };
        document.Add(clientInfo);

        var tableItems = new PdfPTable(4)
        {
            WidthPercentage = 100,
            SpacingBefore = 10f,
            SpacingAfter = 15f
        };
        tableItems.SetWidths(new[] { 3.5f, 1f, 1f, 1f });

        tableItems.AddCell(CreerCellule("Article", itemsHeaderFont, Navy));
        tableItems.AddCell(CreerCellule("Prix unitaire", itemsHeaderFont, Navy));
        tableItems.AddCell(CreerCellule("Qte", itemsHeaderFont, Navy));
        tableItems.AddCell(CreerCellule("Total", itemsHeaderFont, Navy));

        foreach (var item in commande.Items)
        {
            tableItems.AddCell(CreerCellule(item.ArticleNom, itemsTableFont, BaseColor.White));
            tableItems.AddCell(CreerCellule($"{item.PrixUnitaire:F2} $", itemsTableFont, BaseColor.White));
            tableItems.AddCell(CreerCellule(item.Quantite.ToString(), itemsTableFont, BaseColor.White));
            tableItems.AddCell(CreerCellule($"{item.Total:F2} $", itemsTableFont, BaseColor.White));
        }

        document.Add(tableItems);

        var tableTotaux = new PdfPTable(2)
        {
            HorizontalAlignment = Element.ALIGN_RIGHT,
            WidthPercentage = 40,
            SpacingAfter = 30f
        };
        tableTotaux.SetWidths(new[] { 2f, 1.5f });

        AjouterLigneTotale(tableTotaux, "Sous-total", $"{commande.SousTotal:F2} $", extraction: false);
        if (commande.Rabais > 0)
        {
            AjouterLigneTotale(tableTotaux, "Rabais (etudiant)", $"-{commande.Rabais:F2} $", extraction: true);
        }
        AjouterLigneTotale(tableTotaux, "Taxes (TPS/TVQ)", $"{commande.Taxes:F2} $", extraction: false);
        AjouterLigneTotale(tableTotaux, "TOTAL FINAL", $"{commande.TotalFinal:F2} $", extraction: false, bold: true);

        document.Add(tableTotaux);

        var contenuQr = $"GOLF-G06-SHOP\nID={commande.CommandeId}\nTOTAL={commande.TotalFinal:F2}\nDATE={commande.DateCommande:yyyy-MM-dd}";
        var qrCodeBytes = GenererQrCode(contenuQr);
        var qrImage = Image.GetInstance(qrCodeBytes);
        qrImage.Alignment = Element.ALIGN_CENTER;
        qrImage.ScaleToFit(140f, 140f);
        qrImage.SpacingAfter = 10f;
        document.Add(qrImage);

        var footer = new Paragraph(
            $"Mode de paiement : {commande.ModePaiement.ToUpper()} | Merci de votre confiance !",
            footerFont)
        {
            Alignment = Element.ALIGN_CENTER
        };
        document.Add(footer);

        document.Close();
        return outputStream.ToArray();
    }

    private static void AjouterLigneTotale(PdfPTable table, string etiquette, string valeur, bool extraction, bool bold = false)
    {
        var font = bold ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10) : FontFactory.GetFont(FontFactory.HELVETICA, 10);
        var color = extraction ? new BaseColor(185, 28, 28) : BaseColor.Black;
        if (extraction)
        {
            font.Color = color;
        }

        var cellLabel = new PdfPCell(new Phrase(etiquette, font))
        {
            Border = Rectangle.NO_BORDER,
            Padding = 5f,
            HorizontalAlignment = Element.ALIGN_RIGHT
        };
        var cellValue = new PdfPCell(new Phrase(valeur, font))
        {
            Border = Rectangle.NO_BORDER,
            Padding = 5f,
            HorizontalAlignment = Element.ALIGN_RIGHT
        };

        if (bold)
        {
            cellLabel.BorderWidthTop = 1f;
            cellValue.BorderWidthTop = 1f;
            cellLabel.PaddingTop = 10f;
            cellValue.PaddingTop = 10f;
        }

        table.AddCell(cellLabel);
        table.AddCell(cellValue);
    }

    private static byte[] GenererQrCode(string contenuQr)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(contenuQr, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}
