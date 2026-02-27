using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services;

/// <summary>
/// Client TCP pour envoyer les logs de sauvegarde vers ConsoleDeportee
/// </summary>
public class PersistentTcpLogger : IDisposable
{
    private TcpClient? _client;
    private StreamWriter? _writer;

    /// <summary>
    /// Se connecte au serveur de logs distant (ConsoleDeportee sur le port 11000)
    /// </summary>
    public async Task ConnectAsync(string ip = "127.0.0.1")
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, 11000);
            _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
        catch 
        { 
            // Si le serveur n'est pas dispo, on continue sans erreur (mode dégradé)
        }
    }

    /// <summary>
    /// Envoie une entrée de log au serveur distant
    /// </summary>
    public void SendLog(LogEntry entry)
    {
        if (_writer != null && _client is { Connected: true })
        {
            try
            {
                // Format: une ligne JSON par log (ReadLineAsync côté serveur)
                string json = JsonSerializer.Serialize(entry);
                _writer.WriteLine(json);
            }
            catch 
            { 
                // Perte réseau, on ne fait rien
            }
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _client?.Dispose();
    }
}