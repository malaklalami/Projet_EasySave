using System;
namespace EasySave.Model
{
    /// <summary> Représente un travail de sauvegarde et son état actuel. </summary>
    public class BackUpJob
    {
        /// <summary> Nom de la sauvegarde </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary> Dossier source de la sauvegarde </summary>
        public string SourceDir { get; set; } = string.Empty;

        /// <summary> Destination de la sauvegarde </summary>
        public string TargetDir { get; set; } = string.Empty;

        /// <summary> Type de sauvegarde </summary>
        public string BackUpType { get; set; } = string.Empty;

        // Partie necessaire pour les logs

        /// <summary> Etat en temps reel </summary>
        public string State { get; set; } = "Inactive";  // "Active" ou "Inactive" par défaut incative au moment de la création du job tant qu'il n'est pas lancé

        /// <summary> Nombre de fichiers </summary>
        public int TotalFiles { get; set; }

        /// <summary> Taille totale de fichiers à sauvegarder </summary>
        public long TotalSize { get; set; }

        /// <summary> Etat d'avancement de la sauvegarde </summary>
        public int Progress { get; set; }

        /// <summary> Constructeur sans paramètres requis par le Jsonserializer. </summary>
        public BackUpJob() { } // nécessaire pour le Jsonserializer qui permet de creer le json qui sauvegarde les jobs pour les avoir lors d'un nouveau lancement de console

        /// <summary> Initialiser un nouveau travail de sauvegarde avec ses paramètres de base. </summary>
        /// <param name="name">Nom du travail de sauvegarde.</param>
        /// <param name="source">Chemin du dossier source contenant les fichiers à copier.</param>
        /// <param name="target">Chemin du dossier destination où stocker la sauvegarde.</param>
        /// <param name="type">Type de sauvegarde (Complet ou Différentiel).</param>
        public BackUpJob(string name, string source, string target, string type)
        {
            Name = name;
            SourceDir = source;
            TargetDir = target;
            BackUpType = type;
            State = "Inactive"; // Par défaut au démarrage puis active lors du lancement du job
            Progress = 0; // La progession commence à 0 et va évoluer en temps réel
        }
    }
}

