// Tableau de bord en temps réel — SignalR (GOLF-143)
// Se connecte au hub et met à jour le classement sans rechargement

var tournoiId = parseInt(document.getElementById('tournoi-id').value);

// Connexion au hub SignalR
var connexion = new signalR.HubConnectionBuilder()
    .withUrl("/scorehub")
    .withAutomaticReconnect()
    .build();

// Réception d'une mise à jour de classement
connexion.on("MiseAJourClassement", function (idTournoi, classement) {
    // On ne met à jour que si c'est le bon tournoi
    if (idTournoi !== tournoiId) return;
    afficherClassement(classement);
});

// Démarrer la connexion et charger le classement initial
connexion.start()
    .then(function () {
        chargerClassementInitial();
    })
    .catch(function (err) {
        console.error("Erreur connexion SignalR : " + err);
        // En cas d'échec, on essaie quand même de charger le classement
        chargerClassementInitial();
    });

// Charger le classement depuis le serveur au premier chargement de la page
function chargerClassementInitial() {
    fetch('/Score/ClassementJson/' + tournoiId)
        .then(function (response) { return response.json(); })
        .then(function (classement) { afficherClassement(classement); })
        .catch(function (err) { console.error("Erreur chargement classement : " + err); });
}

// Met à jour le tableau HTML avec le nouveau classement
function afficherClassement(classement) {
    var corps = document.getElementById('corps-classement');
    if (!corps) return;

    var html = '';

    if (!classement || classement.length === 0) {
        html = '<tr><td colspan="4" class="text-center text-muted py-4">Aucun score enregistré pour l\'instant.</td></tr>';
    } else {
        for (var i = 0; i < classement.length; i++) {
            var equipe = classement[i];
            var position = i + 1;
            var positionHtml = badgePosition(position);
            var totalAffiche = equipe.trousJoues > 0 ? equipe.totalCoups : '—';
            var trousAffiches = equipe.trousJoues > 0 ? equipe.trousJoues + ' / 18' : '—';

            html += '<tr id="ligne-' + equipe.equipeId + '">';
            html += '  <td class="text-center">' + positionHtml + '</td>';
            html += '  <td class="fw-bold">' + escapeHtml(equipe.nomEquipe) + '</td>';
            html += '  <td class="text-center">' + trousAffiches + '</td>';
            html += '  <td class="text-center fw-bold">' + totalAffiche + '</td>';
            html += '</tr>';
        }
    }

    corps.innerHTML = html;

    // Animation : flash vert sur toutes les lignes mises à jour
    var lignes = corps.querySelectorAll('tr');
    lignes.forEach(function (ligne) {
        ligne.classList.add('ligne-mise-a-jour');
        setTimeout(function () { ligne.classList.remove('ligne-mise-a-jour'); }, 1000);
    });
}

// Génère le badge de position (or/argent/bronze ou numéro)
function badgePosition(position) {
    var classe = position === 1 ? 'pos-1' : position === 2 ? 'pos-2' : position === 3 ? 'pos-3' : 'pos-autre';
    return '<span class="position-badge ' + classe + '">' + position + '</span>';
}

// Sécurité : éviter XSS lors de l'injection de texte dans le DOM
function escapeHtml(texte) {
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(texte));
    return div.innerHTML;
}
