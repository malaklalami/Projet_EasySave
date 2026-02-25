using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services;

public class PersistentTcpLogger : IDisposable
{
    private TcpClient? _client;
    private StreamWriter? _writer;

    public async Task ConnectAsync(string ip = "127.0.0.1")
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, 11000);
            _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
        catch { /* Serveur absent : pas graveon continue */ }
    }

    public void SendLog(LogEntry entry)
    {
        if (_writer != null && _client is { Connected: true })
        {
            try
            {
                // On envoie en une seule ligne pour le ReadLine du serveur
                string json = JsonSerializer.Serialize(entry);
                _writer.WriteLine(json);
            }
            catch { /* Erreur réseau */ }
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _client?.Dispose();
    }
}
// Gère la connexion réseau pour envoyer les logs en temps réel vers un serveur distant.
// Envoie chaque événement de sauvegarde au format JSON sur une seule ligne via le protocole TCP.