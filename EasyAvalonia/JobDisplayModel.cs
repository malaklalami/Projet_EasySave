using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySave.Models;

namespace EasyAvalonia.ViewModels;

public class JobDisplayModel : INotifyPropertyChanged
{
    public BackupJob Job { get; set; }
    public bool IsSelected { get; set; }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _status = "Prêt";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}