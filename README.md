# 🛡️ Projet EasySave - Solution de Sauvegarde Multithreadée

Bienvenue sur le dépôt officiel d'**EasySave**, une solution de sauvegarde robuste développée en **.NET 8**. Ce projet a été conçu pour répondre aux besoins critiques de l'entreprise ProSoft, alliant performance multithreadée, sécurité des données et monitoring en temps réel.

## 📝 Release Notes (Historique des versions)

### **v3.0 - Performance & Haute Disponibilité (Multithreading)**
* **Moteur Parallèle :** Exécution simultanée des sauvegardes via un pool de workers (Threads) optimisé.
* **Contrôle en Temps Réel :** Fonctions **Pause**, **Resume** et **Stop** disponibles pour chaque travail ou pour l'ensemble des tâches.
* **Gestion des Priorités :** Files d'attente intelligentes traitant les extensions prioritaires en amont et limitation du parallélisme pour les fichiers volumineux.
* **Reporting Dynamique :** Mise à jour en temps réel du `state.json` incluant le nouveau statut `Stopped` et la liste des fichiers restants après interruption.

### **v2.0 - Sécurité & Interface Graphique (GUI)**
* **Interface Graphique :** Passage à une interface interactive moderne avec **Avalonia**.
* **Chiffrement :** Intégration du module **CryptoSoft** pour sécuriser les fichiers sensibles via une clé XOR.
* **Monitoring Métier :** Suspension automatique des sauvegardes si un logiciel métier est détecté (ex: Calculatrice, Outlook) afin d'éviter les corruptions.

### **v1.1 - Optimisation & Automatisation (Console)**
* **Format de Logs :** Choix entre le format **JSON** et **XML** pour l'historique quotidien.
* **Moteur CLI :** Support des plages d'indices (ex: `1-3`) et des sélections multiples (ex: `1;3;5`).
* **Gestion CRUD :** Interface de gestion complète pour ajouter, modifier et supprimer des travaux.

---

## 🛠️ Support Technique

### **Configuration minimale**
* **Système :** Windows 10+ / macOS / Linux.
* **Runtime :** .NET 8.0 SDK ou supérieur.

### **Emplacements des fichiers et persistance**
* **Configuration (`jobs.json`) :** Liste des travaux de sauvegarde.
* **Préférences (`settings.json`) :** Langue, format de log, extensions prioritaires et logiciels métier.
* **Logs (`/Logs/`) :** Historique détaillé des transferts (un fichier par jour).
* **État (`state.json`) :** Progression en temps réel et statuts des threads.

---

## 🚀 Installation & Lancement

1. **Cloner le projet :**
   ```bash
   git clone [https://github.com/malaklalami/Projet_EasySave.git](https://github.com/malaklalami/Projet_EasySave.git)
