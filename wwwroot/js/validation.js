// Validation côté client pour les formulaires
// - Groupe 06

// Valide le formulaire avant l'envoi
function validerFormulaire(formulaire) {
    var estValide = true;

    // Enlever les anciennes erreurs
    var anciennesErreurs = formulaire.querySelectorAll('.erreur-validation');
    for (var i = 0; i < anciennesErreurs.length; i++) {
        anciennesErreurs[i].remove();
    }

    // Récupérer tous les champs obligatoires
    var champs = formulaire.querySelectorAll('input[required], textarea[required]');

    for (var i = 0; i < champs.length; i++) {
        var champ = champs[i];

        if (champ.value.trim() === '') {
            champ.style.borderColor = 'var(--danger)';
            afficherErreur(champ, 'Ce champ est obligatoire.');
            estValide = false;
        } else {
            champ.style.borderColor = 'rgba(45, 212, 191, 0.55)';
        }
    }

    // Valider le format email
    var champEmail = formulaire.querySelector("input[type='email']");
    if (champEmail && champEmail.value !== '' && !champEmail.readOnly) {
        if (validerEmail(champEmail.value) === false) {
            champEmail.style.borderColor = 'var(--danger)';
            afficherErreur(champEmail, 'Format d\'email invalide.');
            estValide = false;
        }
    }

    // Valider les mots de passe s'ils existent
    var mdp = formulaire.querySelector('#MotDePasse');
    var confirmMdp = formulaire.querySelector('#ConfirmMotDePasse');
    if (mdp && confirmMdp && mdp.value !== '' && confirmMdp.value !== '') {
        if (mdp.value.length < 6) {
            afficherErreur(mdp, 'Le mot de passe doit faire au moins 6 caractères.');
            mdp.style.borderColor = 'var(--danger)';
            estValide = false;
        }
        if (mdp.value !== confirmMdp.value) {
            afficherErreur(confirmMdp, 'Les mots de passe ne correspondent pas.');
            confirmMdp.style.borderColor = 'var(--danger)';
            estValide = false;
        }
    }

    return estValide;
}

// Affiche un message d'erreur sous un champ
function afficherErreur(champ, message) {
    var erreur = document.createElement('small');
    erreur.className = 'erreur-validation';
    erreur.style.color = '#fb7185';
    erreur.style.display = 'block';
    erreur.style.marginTop = '4px';
    erreur.textContent = message;
    champ.parentNode.appendChild(erreur);
}

// Vérifie le format email
function validerEmail(email) {
    // Regex simple pour valider le format email
    var regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

// Vérifie le format téléphone
function validerTelephone(telephone) {
    var caracteres = '0123456789-() ';
    for (var i = 0; i < telephone.length; i++) {
        if (caracteres.indexOf(telephone[i]) === -1) {
            return false;
        }
    }
    return telephone.length >= 10;
}
