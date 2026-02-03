// Validation côté client pour les formulaires
// - Rayane

// Valide le formulaire avant l'envoi
function validerFormulaire(formulaire) {
    var estValide = true;

    // Récupérer tous les champs obligatoires
    var champs = formulaire.querySelectorAll("input[required], textarea[required]");

    // Vérifier chaque champ
    for (var i = 0; i < champs.length; i++) {
        var champ = champs[i];

        if (champ.value.trim() === "") {
            // Champ vide
            champ.style.border = "2px solid red";
            estValide = false;
        } else {
            // Champ rempli
            champ.style.border = "1px solid green";
        }
    }

    // Valider le format email si présent
    var champEmail = formulaire.querySelector("input[type='email']");
    if (champEmail && champEmail.value !== "") {
        if (validerEmail(champEmail.value) === false) {
            champEmail.style.border = "2px solid red";
            estValide = false;
        }
    }

    // Afficher message si invalide
    if (estValide === false) {
        alert("Veuillez remplir tous les champs obligatoires.");
    }

    return estValide;
}

// Vérifie le format email
function validerEmail(email) {
    // Format simple : texte@texte.texte
    var position = email.indexOf("@");
    var positionPoint = email.lastIndexOf(".");

    if (position > 0 && positionPoint > position) {
        return true;
    }
    return false;
}

// Vérifie le format téléphone
function validerTelephone(telephone) {
    // Accepte les chiffres, tirets et espaces
    var caracteres = "0123456789-() ";

    for (var i = 0; i < telephone.length; i++) {
        if (caracteres.indexOf(telephone[i]) === -1) {
            return false;
        }
    }
    return telephone.length >= 10;
}
