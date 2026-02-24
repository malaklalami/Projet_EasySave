using Avalonia.Controls;
using Avalonia.Interactivity;
using EasyLibrary.ViewModels;
using EasySave.Core;
using System.Collections.Generic;
using System.Linq;

namespace EasyAvalonia.Views;

public partial class SettingsWindow : Window
{
    private MainViewModel _vm;

    public SettingsWindow() { InitializeComponent(); }

    public SettingsWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        // --- CHARGEMENT DES DONNÉES ---

        // Logique métier & Performance
        this.FindControl<TextBox>("BusinessSoftwareInput").Text = _vm.Settings.BusinessSoftware;
        this.FindControl<TextBox>("LargeFileThresholdInput").Text = _vm.Settings.LargeFileThreshold.ToString();
        this.FindControl<NumericUpDown>("MaxParallelInput").Value = _vm.Settings.MaxParallelFiles;

        // Format de Log (V1.1)
        var formatCombo = this.FindControl<ComboBox>("LogFormatCombo");
        formatCombo.SelectedIndex = _vm.Settings.LogFormat == LogFormat.Json ? 0 : 1;

        // Extensions (Transformation Liste -> Texte pour l'affichage)
        this.FindControl<TextBox>("CryptoExtensionsInput").Text = string.Join(", ", _vm.Settings.EncryptionExtensions);
        this.FindControl<TextBox>("PriorityExtensionsInput").Text = string.Join(", ", _vm.Settings.PriorityExtensions);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 1. Sauvegarde Logiciel Métier
        _vm.UpdateBusinessSoftware(this.FindControl<TextBox>("BusinessSoftwareInput").Text ?? "");

        // 2. Sauvegarde Seuil n Ko & Parallélisme
        if (long.TryParse(this.FindControl<TextBox>("LargeFileThresholdInput").Text, out long threshold))
            _vm.Settings.LargeFileThreshold = threshold;

        _vm.Settings.MaxParallelFiles = (int)(this.FindControl<NumericUpDown>("MaxParallelInput").Value ?? 4);

        // 3. Sauvegarde Log Format (JSON/XML)
        var formatCombo = this.FindControl<ComboBox>("LogFormatCombo");
        _vm.Settings.LogFormat = formatCombo.SelectedIndex == 0 ? LogFormat.Json : LogFormat.Xml;

        // 4. Sauvegarde Extensions Crypto
        _vm.Settings.EncryptionExtensions = ParseExtensions(this.FindControl<TextBox>("CryptoExtensionsInput").Text);

        // 5. Sauvegarde Extensions Prioritaires
        _vm.Settings.PriorityExtensions = ParseExtensions(this.FindControl<TextBox>("PriorityExtensionsInput").Text);

        // Enregistrement final sur le disque
        _vm.SaveSettings();
        this.Close();
    }

    private List<string> ParseExtensions(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return input.Split(',')
                    .Select(s => s.Trim().ToLower())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => s.StartsWith(".") ? s : "." + s) // Force le point devant
                    .ToList();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
}