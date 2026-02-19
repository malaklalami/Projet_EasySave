using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsoleDeportee;

class Program
{
    // Mutex système pour garantir qu'un seul thread écrit dans le fichier à la fois
    private static readonly Mutex _fileMutex = new Mutex(false, "Global\\EasySave_WriteMutex");
    private static readonly string _logFolder = "ReceivedLogs";

    static async Task Main(string[] args)
    {
        if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);

        TcpListener listener = new TcpListener(IPAddress.Any, 11000);
        listener.Start();
        Console.WriteLine("=== SERVEUR DE LOGS MULTI-CLIENTS ===");

        while (true)
        {
            // On accepte un client et on lance un thread (Task) dédié pour ne pas bloquer les autres
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Inconnu";
        Console.WriteLine($"[CONNEXION] : {clientEndPoint}");

        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            try
            {
                while (!reader.EndOfStream)
                {
                    string? jsonData = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(jsonData)) continue;

                    // --- ZONE SÉCURISÉE PAR MUTEX ---
                    _fileMutex.WaitOne();
                    try
                    {
                        string filePath = Path.Combine(_logFolder, $"logs_{DateTime.Now:yyyy-MM-dd}.json");
                        await File.AppendAllTextAsync(filePath, jsonData + Environment.NewLine);
                        Console.WriteLine($"[LOG REÇU] Provient de : {clientEndPoint}");
                    }
                    finally
                    {
                        _fileMutex.ReleaseMutex();
                    }
                }
            }
            catch { /* Déconnexion */ }
        }
        Console.WriteLine($"[DÉCONNEXION] : {clientEndPoint}");
    }
}