# 🛡️ Projet EasySave - Solution de Sauvegarde

Bienvenue sur le dépôt officiel d'**EasySave**, une solution de sauvegarde robuste développée en .NET 8. Ce projet a été conçu pour répondre aux besoins de l'entreprise ProSoft, alliant performance console et confort graphique.

---

## 📝 Release Notes (Historique des versions)

### v2.0 - Sécurité & Interface Graphique (GUI)
* **Interface Graphique :** Passage à une interface interactive moderne avec **Avalonia**.
* **Chiffrement :** Intégration du module **CryptoSoft** pour sécuriser les fichiers sensibles.
* **Monitoring Métier :** Suspension automatique des sauvegardes si un logiciel métier est détecté (ex: Calculatrice, Outlook) afin d'éviter les corruptions de données.

### v1.1 - Optimisation & Automatisation (Console)
* **Format de Logs :** Choix entre le format **JSON** et **XML** pour l'historique.
* **Moteur CLI :** Exécution via le terminal avec support des plages d'indices (`1-3`) et des listes (`1;3;5`).
* **Gestion CRUD :** Possibilité de modifier et supprimer des travaux de sauvegarde existants.

### v1.0 - Fondations (Console)
* **Cœur MVVM :** Architecture découplée pour une meilleure maintenabilité.
* **Sauvegardes :** Gestion des travaux complets et différentiels (5 emplacements).
* **Bilingue :** Support du Français et de l'Anglais.

---

## 🛠️ Support Technique

### Configuration minimale
* **Système :** Windows 10+ / macOS / Linux.
* **Runtime :** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou supérieur.

### Emplacements des fichiers par défaut
* **Configuration (`jobs.json`) :** Racine du dossier d'exécution.
* **Préférences (`settings.json`) :** Stockage de la langue et du format de log.
* **Logs :** Répertoire `/Logs/` (un fichier par jour).
* **État en temps réel :** Fichier `state.json` (progression en cours).

---

## 🚀 Installation & Lancement
1.  **Cloner le projet :**
    ```bash
    git clone [https://github.com/malaklalami/Projet_EasySave.git](https://github.com/malaklalami/Projet_EasySave.git)
    ```
2.  **Compiler la solution :**
    ```bash
    dotnet build
    ```
3.  **Lancer la console (v1.1) :**
    ```bash
    dotnet run --project EasyConsole
    ```