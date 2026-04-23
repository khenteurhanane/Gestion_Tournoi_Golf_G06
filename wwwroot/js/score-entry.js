document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("score-entry-form");
    if (!form) {
        return;
    }

    var successBox = document.getElementById("message-confirmation");
    var successText = document.getElementById("texte-confirmation");
    var errorBox = document.getElementById("message-erreur");
    var errorText = document.getElementById("texte-erreur");
    var submitButton = document.getElementById("btn-saisir");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        masquerMessages();

        var formData = new FormData(form);
        var nbCoups = Number(formData.get("nbCoups"));
        if (!Number.isFinite(nbCoups) || nbCoups < 1 || nbCoups > 20) {
            afficherErreur("Le nombre de coups doit etre entre 1 et 20.");
            return;
        }

        submitButton.disabled = true;

        var token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
        var payload = new URLSearchParams();
        payload.set("tournoiId", String(formData.get("tournoiId") ?? ""));
        payload.set("equipeId", String(formData.get("equipeId") ?? ""));
        payload.set("numeroTrou", String(formData.get("numeroTrou") ?? ""));
        payload.set("nbCoups", String(formData.get("nbCoups") ?? ""));

        try {
            var response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: payload.toString()
            });

            var data = await lireJson(response);
            if (!response.ok || !data.succes) {
                afficherErreur(data.message || "Le serveur a refuse l'enregistrement du score.");
                return;
            }

            mettreAJourLigne(data.equipeId, data.numeroTrou, data.nbCoups, data.totalCoups);
            afficherConfirmation(data.message || "Score enregistre avec succes.");
        } catch (error) {
            afficherErreur(error.message || "Erreur de connexion.");
        } finally {
            submitButton.disabled = false;
        }
    });

    async function lireJson(response) {
        var contentType = response.headers.get("content-type") || "";
        if (contentType.indexOf("application/json") >= 0) {
            return await response.json();
        }

        var texte = await response.text();
        throw new Error(texte && texte.length < 180 ? texte : "Reponse inattendue du serveur.");
    }

    function mettreAJourLigne(equipeId, numeroTrou, nbCoups, totalCoups) {
        var cell = document.getElementById("cell-" + equipeId + "-" + numeroTrou);
        if (cell) {
            cell.textContent = String(nbCoups);
        }

        var totalCell = document.getElementById("total-" + equipeId);
        if (totalCell) {
            totalCell.textContent = String(totalCoups);
        }

        var row = document.getElementById("ligne-equipe-" + equipeId);
        if (row) {
            row.classList.add("ligne-mise-a-jour");
            window.setTimeout(function () {
                row.classList.remove("ligne-mise-a-jour");
            }, 1000);
        }
    }

    function masquerMessages() {
        successBox?.classList.add("d-none");
        errorBox?.classList.add("d-none");
    }

    function afficherConfirmation(message) {
        if (!successBox || !successText) {
            return;
        }

        successText.textContent = message;
        successBox.classList.remove("d-none");
        window.setTimeout(function () {
            successBox.classList.add("d-none");
        }, 3200);
    }

    function afficherErreur(message) {
        if (!errorBox || !errorText) {
            return;
        }

        errorText.textContent = message;
        errorBox.classList.remove("d-none");
        window.setTimeout(function () {
            errorBox.classList.add("d-none");
        }, 4200);
    }
});
