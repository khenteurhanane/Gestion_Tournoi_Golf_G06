// =============================================
// JavaScript - Tournoi de Golf G06
// Fonctionnalités côté client
// =============================================

// --- Onglets : Tournois à venir / Historique ---
function afficherOnglet(onglet) {
    var sectionAVenir = document.getElementById('sectionAVenir');
    var sectionPasses = document.getElementById('sectionPasses');
    var btnAVenir = document.getElementById('btnAVenir');
    var btnPasses = document.getElementById('btnPasses');

    if (!sectionAVenir || !sectionPasses) return;

    if (onglet === 'avenir') {
        sectionAVenir.style.display = 'block';
        sectionPasses.style.display = 'none';
        btnAVenir.className = 'btn onglet-actif';
        btnPasses.className = 'btn btn-secondary';
    } else {
        sectionAVenir.style.display = 'none';
        sectionPasses.style.display = 'block';
        btnAVenir.className = 'btn btn-secondary';
        btnPasses.className = 'btn onglet-actif';
    }
}

// --- Recherche dans la liste des tournois ---
function filtrerTournois() {
    var input = document.getElementById('rechercheTournoi');
    if (!input) return;

    var texte = input.value.toLowerCase();
    var lignes = document.querySelectorAll('.ligne-tournoi');

    lignes.forEach(function (ligne) {
        var contenu = ligne.textContent.toLowerCase();
        if (contenu.indexOf(texte) !== -1) {
            ligne.style.display = '';
        } else {
            ligne.style.display = 'none';
        }
    });
}

// --- Recherche dans la liste des utilisateurs (admin) ---
function filtrerUtilisateurs() {
    var input = document.getElementById('rechercheUtilisateur');
    if (!input) return;

    var texte = input.value.toLowerCase();
    var lignes = document.querySelectorAll('.ligne-utilisateur');

    lignes.forEach(function (ligne) {
        var contenu = ligne.textContent.toLowerCase();
        if (contenu.indexOf(texte) !== -1) {
            ligne.style.display = '';
        } else {
            ligne.style.display = 'none';
        }
    });
}

// --- Confirmation avant suppression ---
function confirmerSuppression(message) {
    return confirm(message || 'Êtes-vous sûr de vouloir supprimer ?');
}

// --- Animation au chargement de la page ---
document.addEventListener('DOMContentLoaded', function () {
    // Animer les cartes et stat-cards au chargement
    var elements = document.querySelectorAll('.card, .stat-card, .hero');
    elements.forEach(function (el, index) {
        el.style.opacity = '0';
        el.style.transform = 'translateY(20px)';
        el.style.transition = 'opacity 0.4s ease, transform 0.4s ease';

        setTimeout(function () {
            el.style.opacity = '1';
            el.style.transform = 'translateY(0)';
        }, 100 + (index * 80));
    });

    // Fermer automatiquement les messages de succès après 5 secondes
    var messages = document.querySelectorAll('.alert-success, .success');
    messages.forEach(function (msg) {
        setTimeout(function () {
            msg.style.transition = 'opacity 0.5s ease';
            msg.style.opacity = '0';
            setTimeout(function () {
                msg.style.display = 'none';
            }, 500);
        }, 5000);
    });

    // Mettre en surbrillance le lien actif dans la navbar
    var currentPath = window.location.pathname.toLowerCase();
    var navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(function (link) {
        var href = link.getAttribute('href');
        if (href) {
            var linkPath = href.toLowerCase();
            if (currentPath === linkPath || (linkPath !== '/' && currentPath.indexOf(linkPath) === 0)) {
                link.classList.add('active');
            }
        }
    });
});

// --- Retour en haut de page ---
window.addEventListener('scroll', function () {
    // Ajouter un bouton "retour en haut" quand on scroll
    var btnTop = document.getElementById('btnRetourHaut');
    if (!btnTop) {
        btnTop = document.createElement('button');
        btnTop.id = 'btnRetourHaut';
        btnTop.innerHTML = '&uarr;';
        btnTop.title = 'Retour en haut';
        btnTop.style.cssText = 'position:fixed; bottom:30px; right:30px; width:45px; height:45px; ' +
            'border-radius:50%; background:var(--accent); color:var(--card); border:none; ' +
            'font-size:1.3rem; cursor:pointer; display:none; z-index:999; box-shadow:0 4px 12px rgba(0,0,0,0.3);';
        btnTop.onclick = function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        };
        document.body.appendChild(btnTop);
    }

    if (window.scrollY > 300) {
        btnTop.style.display = 'block';
    } else {
        btnTop.style.display = 'none';
    }
});
