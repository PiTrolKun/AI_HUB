using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AIHub.Models;
using WpfMessageBox = System.Windows.MessageBox;

namespace AIHub.Services;

public sealed class FileViewerService
{
    private static readonly HashSet<string> BuiltInExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".json", ".xml", ".csv", ".tsv", ".md", ".markdown", ".yaml", ".yml",
        ".html", ".htm", ".svg", ".css", ".cs", ".js", ".ts", ".py", ".cpp", ".h", ".sql",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".wdp",
        ".docx", ".pptx", ".xlsx", ".xlsm", ".pdf", ".epub",
        ".mp3", ".wav", ".wma", ".mp4", ".wmv", ".avi",
        ".zip", ".7z", ".rar", ".tar", ".gz", ".tgz",
        ".eml", ".mime", ".sqlite", ".sqlite3", ".db"
    };

    private readonly Dictionary<string, FileViewerWindow> _openWindows =
        new(StringComparer.OrdinalIgnoreCase);

    public string? SelectFile(Window owner, LocalizationService localization)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = localization.T("FileViewer.SelectTitle"),
            Filter = localization.T("FileViewer.AllFilesFilter"),
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public void Open(
        Window owner,
        string path,
        FileViewerSettings settings,
        bool isDarkTheme,
        LocalizationService localization)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            WpfMessageBox.Show(
                owner,
                localization.T("FileViewer.MissingFile"),
                "AI HUB",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var extension = Path.GetExtension(fullPath);
        if (CanOpenInternally(extension, settings))
        {
            OpenInternal(owner, fullPath, isDarkTheme, localization);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            OfferFallback(owner, fullPath, isDarkTheme, localization);
        }
        catch (InvalidOperationException)
        {
            OfferFallback(owner, fullPath, isDarkTheme, localization);
        }
    }

    public void ApplyTheme(bool isDarkTheme)
    {
        foreach (var window in _openWindows.Values.ToList())
        {
            window.ApplyTheme(isDarkTheme);
        }
    }

    public static bool CanOpenInternally(
        string extension,
        FileViewerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = extension.StartsWith('.')
            ? extension
            : $".{extension}";
        var preferInternal = settings.PreferInternalByExtension.TryGetValue(
            normalized,
            out var perExtension)
                ? perExtension
                : settings.PreferInternalViewers;
        return preferInternal && BuiltInExtensions.Contains(normalized);
    }

    private void OpenInternal(
        Window owner,
        string path,
        bool isDarkTheme,
        LocalizationService localization)
    {
        if (_openWindows.TryGetValue(path, out var existing) && existing.IsLoaded)
        {
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }
            existing.Activate();
            return;
        }

        var window = new FileViewerWindow(path, isDarkTheme, localization)
        {
            Owner = owner
        };
        _openWindows[path] = window;
        window.Closed += (_, _) => _openWindows.Remove(path);
        window.Show();
    }

    private void OfferFallback(
        Window owner,
        string path,
        bool isDarkTheme,
        LocalizationService localization)
    {
        var result = WpfMessageBox.Show(
            owner,
            localization.T("FileViewer.ExternalMissing"),
            "AI HUB",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Information);
        if (result == MessageBoxResult.Yes)
        {
            OpenInternal(owner, path, isDarkTheme, localization);
        }
        else if (result == MessageBoxResult.No)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            {
                UseShellExecute = true
            });
        }
    }
}
