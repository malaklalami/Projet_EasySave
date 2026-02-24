using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySave.Models;
using Avalonia.Media;

namespace EasyAvalonia.ViewModels;

public class JobDisplayModel : INotifyPropertyChanged
{
    public BackupJob Job { get; set; }
    public bool IsSelected { get; set; }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
            // Indispensable pour masquer/afficher les boutons en temps réel
            OnPropertyChanged(nameof(IsRunning));
        }
    }

    private string _status = "En attente";
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsRunning));
        }
    }

    private string _currentActionText = "";
    public string CurrentActionText
    {
        get => _currentActionText;
        set { _currentActionText = value; OnPropertyChanged(); }
    }

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsRunning));
        }
    }

    // Propriété pour la visibilité dynamique des boutons
    public bool IsRunning
    {
        get
        {
            // Le travail est considéré comme actif s'il a démarré et n'est pas fini
            return Progress > 0 && Progress < 100;
        }
    }

    public IBrush StatusColor
    {
        get
        {
            if (Progress >= 100) return Brushes.LimeGreen;
            if (IsPaused) return Brushes.Orange;
            if (Progress > 0) return Brushes.DodgerBlue;
            return Brushes.Gray;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}