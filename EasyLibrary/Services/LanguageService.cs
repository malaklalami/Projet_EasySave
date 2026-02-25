using System.Text.Json;

namespace EasySave.Services;

public class LanguageService
{
    private Dictionary<string, string> _translations = new();

    public void Load(string lang)
    {

        string fileName = $"{lang}.json";
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch
            {
                // Si le JSON est cassé, on évite le crash
                _translations = new Dictionary<string, string>();
            }
        }
        else
        {
            // Optionnel : Message de debug si le fichier n'est pas trouvé
            Console.WriteLine($"[Warning] Language file not found: {path}");
            _translations = new Dictionary<string, string>();
        }
    }

    public string Get(string key)
    {
        // Si la clé existe, on renvoie la trad, sinon on renvoie la clé elle-même (pour repérer les oublis)
        return _translations.ContainsKey(key) ? _translations[key] : key;
    }
}
// Charge les fichiers de traduction JSON pour permettre de changer la langue de l'application.
// Récupère le texte traduit associé à une clé (ou affiche la clé si la traduction est manquante).