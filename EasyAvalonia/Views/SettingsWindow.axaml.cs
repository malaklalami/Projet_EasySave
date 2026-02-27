using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.ViewModels;
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

        // Langue
        var langCombo = this.FindControl<ComboBox>("LanguageCombo");
        if (langCombo != null)
        {
            langCombo.SelectedIndex = _vm.Settings.Language == "fr" ? 1 : 0;
        }

        // Logique métier & Performance
        this.FindControl<TextBox>("BusinessSoftwareInput").Text = _vm.Settings.BusinessSoftware;
        this.FindControl<TextBox>("LargeFileThresholdInput").Text = _vm.Settings.LargeFileThreshold.ToString();
        this.FindControl<NumericUpDown>("MaxParallelInput").Value = _vm.Settings.MaxParallelFiles;

        // Format de Log (V1.1)
        var formatCombo = this.FindControl<ComboBox>("LogFormatCombo");
        formatCombo.SelectedIndex = _vm.Settings.LogFormat == LogFormat.Json ? 0 : 1;

        // Cible des Logs (Local, Remote, Both)
        var logTargetCombo = this.FindControl<ComboBox>("LogTargetCombo");
        if (logTargetCombo != null)
        {
            logTargetCombo.SelectedIndex = _vm.Settings.LogTarget switch
            {
                LogTarget.Local => 0,
                LogTarget.Remote => 1,
                LogTarget.Both => 2,
                _ => 0
            };
        }

        // IP Distante
        this.FindControl<TextBox>("RemoteIpInput").Text = _vm.Settings.RemoteIp;

        // Extensions (Transformation Liste -> Texte pour l'affichage)
        this.FindControl<TextBox>("CryptoExtensionsInput").Text = string.Join(", ", _vm.Settings.EncryptionExtensions);
        this.FindControl<TextBox>("PriorityExtensionsInput").Text = string.Join(", ", _vm.Settings.PriorityExtensions);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 1. Langue
        var langCombo = this.FindControl<ComboBox>("LanguageCombo");
        if (langCombo != null)
        {
            _vm.UpdateLanguage(langCombo.SelectedIndex == 1 ? "fr" : "en");
        }

        // 2. Sauvegarde Logiciel Métier
        _vm.UpdateBusinessSoftware(this.FindControl<TextBox>("BusinessSoftwareInput").Text ?? "");

        // 3. Sauvegarde Seuil n Ko & Parallélisme
        if (long.TryParse(this.FindControl<TextBox>("LargeFileThresholdInput").Text, out long threshold))
            _vm.Settings.LargeFileThreshold = threshold;

        _vm.Settings.MaxParallelFiles = (int)(this.FindControl<NumericUpDown>("MaxParallelInput").Value ?? 4);

        // 4. Sauvegarde Log Format (JSON/XML)
        var formatCombo = this.FindControl<ComboBox>("LogFormatCombo");
        _vm.Settings.LogFormat = formatCombo.SelectedIndex == 0 ? LogFormat.Json : LogFormat.Xml;

        // 5. Sauvegarde Cible des Logs
        var logTargetCombo = this.FindControl<ComboBox>("LogTargetCombo");
        if (logTargetCombo != null)
        {
            _vm.Settings.LogTarget = logTargetCombo.SelectedIndex switch
            {
                1 => LogTarget.Remote,
                2 => LogTarget.Both,
                _ => LogTarget.Local
            };
        }

        // 6. Sauvegarde IP Distante
        _vm.Settings.RemoteIp = this.FindControl<TextBox>("RemoteIpInput").Text ?? "127.0.0.1";

        // 7. Sauvegarde Extensions Crypto
        _vm.Settings.EncryptionExtensions = ParseExtensions(this.FindControl<TextBox>("CryptoExtensionsInput").Text);

        // 8. Sauvegarde Extensions Prioritaires
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

// Interface de configuration permettant de modifier les paramètres globaux (logiciel métier, seuil de fichiers, parallélisme).
// Assure la conversion des entrées textuelles en listes d'extensions valides et synchronise les choix avec le fichier de réglages JSON.