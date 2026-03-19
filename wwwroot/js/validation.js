// =========================================================================
// VALIDATION.JS - Amélioration [US-06-T08]
// Système de validation client robuste pour le projet Tournoi de Golf G06
// =========================================================================

document.addEventListener('DOMContentLoaded', function () {
    initValidation();
});

/**
 * Initialise la validation sur tous les formulaires de la page
 */
function initValidation() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        // Validation à l'envoi
        form.addEventListener('submit', function (event) {
            if (!validerFormulaire(this)) {
                event.preventDefault();
                event.stopPropagation();
                
                // Scroller vers la première erreur
                const premièreErreur = this.querySelector('.is-invalid');
                if (premièreErreur) {
                    premièreErreur.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        });

        // Validation en temps réel (uniquement après le premier essai d'envoi ou au blur)
        const inputs = form.querySelectorAll('input, textarea, select');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validerChamp(this);
            });
            
            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid') || this.classList.contains('is-valid')) {
                    validerChamp(this);
                }
            });
        });
    });
}

/**
 * Valide un formulaire complet
 * @param {HTMLFormElement} formulaire 
 * @returns {boolean}
 */
function validerFormulaire(formulaire) {
    let estValide = true;
    const champs = formulaire.querySelectorAll('input, textarea, select');
    
    champs.forEach(champ => {
        if (!validerChamp(champ)) {
            estValide = false;
        }
    });

    return estValide;
}

/**
 * Valide un champ individuel selon ses attributs (required, type, min, max, data-*)
 * @param {HTMLElement} champ 
 * @returns {boolean}
 */
function validerChamp(champ) {
    if (champ.type === 'hidden' || champ.readOnly || champ.disabled) return true;

    const valeur = champ.value.trim();
    let messageErreur = '';

    // 1. Validation "Requis"
    if (champ.hasAttribute('required') && valeur === '') {
        messageErreur = 'Ce champ est obligatoire.';
    }
    
    // 2. Validation Email
    else if (champ.type === 'email' && valeur !== '') {
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(valeur)) {
            messageErreur = 'Format d\'adresse courriel invalide.';
        }
    }
    
    // 3. Validation Téléphone
    else if (champ.type === 'tel' && valeur !== '') {
        if (!/^(\+\d{1,2}\s?)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$/.test(valeur)) {
            messageErreur = 'Format de téléphone invalide (ex: 555-555-5555).';
        }
    }

    // 4. Validation Nombre (Min/Max)
    else if (champ.type === 'number' && valeur !== '') {
        const num = parseFloat(valeur);
        const min = champ.getAttribute('min');
        const max = champ.getAttribute('max');
        
        if (min !== null && num < parseFloat(min)) {
            messageErreur = `La valeur doit être au moins ${min}.`;
        } else if (max !== null && num > parseFloat(max)) {
            messageErreur = `La valeur ne doit pas dépasser ${max}.`;
        }
    }

    // 5. Validation Dates
    else if (champ.type === 'date' && valeur !== '') {
        const dateSaisie = new Date(valeur);
        const aujourdhui = new Date();
        aujourdhui.setHours(0, 0, 0, 0);

        // Date dans le futur (min-today)
        if (champ.hasAttribute('min-today') && dateSaisie < aujourdhui) {
            messageErreur = 'La date ne peut pas être dans le passé.';
        }

        // Comparaison avec un autre champ (data-after)
        const afterFieldId = champ.getAttribute('data-after');
        if (afterFieldId) {
            const otherField = document.getElementById(afterFieldId);
            if (otherField && otherField.value) {
                const otherDate = new Date(otherField.value);
                if (dateSaisie <= otherDate) {
                    messageErreur = `Cette date doit être après le ${otherField.value}.`;
                }
            }
        }

        // Comparaison avec un autre champ (data-before)
        const beforeFieldId = champ.getAttribute('data-before');
        if (beforeFieldId) {
            const otherField = document.getElementById(beforeFieldId);
            if (otherField && otherField.value) {
                const otherDate = new Date(otherField.value);
                if (dateSaisie > otherDate) {
                    messageErreur = `La date limite doit être avant le tournoi (${otherField.value}).`;
                }
            }
        }
    }

    // 6. Validation Mots de passe
    else if (champ.id === 'ConfirmMotDePasse' || champ.id === 'ConfirmationMotDePasse') {
        const mdp = document.querySelector('#MotDePasse');
        if (mdp && valeur !== mdp.value) {
            messageErreur = 'Les mots de passe ne correspondent pas.';
        }
    }
    else if (champ.id === 'MotDePasse' && valeur !== '' && valeur.length < 6) {
        messageErreur = 'Le mot de passe doit faire au moins 6 caractères.';
    }

    // Application visuelle du résultat
    if (messageErreur) {
        appliquerStyleErreur(champ, messageErreur);
        return false;
    } else {
        appliquerStyleSucces(champ);
        return true;
    }
}

/**
 * Applique le style d'erreur Bootstrap (is-invalid) et affiche le message
 */
function appliquerStyleErreur(champ, message) {
    champ.classList.remove('is-valid');
    champ.classList.add('is-invalid');
    
    // Pour l'input type file (zone-upload), on change la bordure du parent
    if (champ.type === 'file' && champ.id === 'imageFile') {
        const zone = document.getElementById('zone-upload');
        if (zone) zone.style.borderColor = 'var(--danger)';
    }

    // Gestion du message d'erreur
    let feedback = champ.parentNode.querySelector('.invalid-feedback');
    if (!feedback) {
        feedback = document.createElement('div');
        feedback.className = 'invalid-feedback';
        feedback.style.display = 'block';
        feedback.style.fontSize = '0.8rem';
        feedback.style.marginTop = '4px';
        feedback.style.color = 'var(--danger)';
        champ.parentNode.appendChild(feedback);
    }
    feedback.textContent = message;
    
    // Style du champ
    champ.style.borderColor = 'var(--danger)';
}

/**
 * Enlève les erreurs et applique le style succès
 */
function appliquerStyleSucces(champ) {
    champ.classList.remove('is-invalid');
    champ.classList.add('is-valid');
    
    // Reset file upload zone
    if (champ.type === 'file' && champ.id === 'imageFile') {
        const zone = document.getElementById('zone-upload');
        if (zone) zone.style.borderColor = 'rgba(45, 106, 46, .35)';
    }

    const feedback = champ.parentNode.querySelector('.invalid-feedback');
    if (feedback) {
        feedback.remove();
    }
    
    champ.style.borderColor = 'rgba(45, 106, 46, .55)';
}
