using Microsoft.Extensions.DependencyInjection;
using SriExtractor.Desktop.Infrastructure;
using SriExtractor.Desktop.Models;
using SriExtractor.Desktop.ViewModels;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SriExtractor.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        var sp = AppHost.Services;
        _vm = sp.GetRequiredService<MainViewModel>();
        _vm.DownloadCompleted += LoadFechasExtraidas;
        DataContext = _vm;
        LoadFechasExtraidas();
    }


    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not SriReceivedRow row) return;
        if (string.IsNullOrWhiteSpace(row.XmlPath) || !File.Exists(row.XmlPath))
        {
            LblStatus.Text = "No existe el XML para esta fila.";
            return;
        }

        var win = new InvoiceDetailWindow(row.XmlPath);
        win.Show();
    }

    private void BtnOpenProveedores_Click(object sender, RoutedEventArgs e)
    {
        var win = new ProveedoresWindow();
        win.Show();
    }

    private void BtnOpenFacturasBd_Click(object sender, RoutedEventArgs e)
    {
        var win = new FacturasPagoWindow();
        win.Show();
    }

    private void LoadFechasExtraidas()
    {
        try
        {
            TreeFechas.Items.Clear();

            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SriExtractor",
                "Xml");

            if (!Directory.Exists(baseFolder)) return;

            var culture = new CultureInfo("es-ES");
            var now = DateTime.Now;

            var years = Directory.GetDirectories(baseFolder)
                .OrderBy(y => y)
                .ToList();

            foreach (var yearDir in years)
            {
                var yearName = Path.GetFileName(yearDir);
                var yearItem = new TreeViewItem { Header = yearName };

                var monthDirs = Directory.GetDirectories(yearDir)
                    .OrderBy(m => m)
                    .Select(m => new { Path = m, Order = ParseMonthPrefix(Path.GetFileName(m)) })
                    .OrderBy(x => x.Order)
                    .ToList();

                foreach (var monthDir in monthDirs)
                {
                    var monthName = Path.GetFileName(monthDir.Path);
                    var monthItem = new TreeViewItem { Header = monthName };

                    var dayDirs = Directory.GetDirectories(monthDir.Path)
                        .Select(d => new
                        {
                            Path = d,
                            Date = DateTime.TryParseExact(Path.GetFileName(d), "dd-MM-yyyy", culture, DateTimeStyles.None, out var dt) ? dt : (DateTime?)null
                        })
                        .OrderBy(d => d.Date ?? DateTime.MaxValue)
                        .ToList();

                    foreach (var dayDir in dayDirs)
                    {
                        var dayName = Path.GetFileName(dayDir.Path);
                        var display = dayName;

                        if (dayDir.Date != null)
                        {
                            var dow = culture.DateTimeFormat.GetDayName(dayDir.Date.Value.DayOfWeek).ToUpperInvariant();
                            display = $"{dayName} {dow}";
                        }

                        monthItem.Items.Add(new TreeViewItem { Header = display, Tag = dayDir.Path });
                    }

                    if (monthItem.Items.Count > 0)
                        yearItem.Items.Add(monthItem);

                    if (int.TryParse(yearName, out var yInt) && yInt == now.Year && monthDir.Order == now.Month)
                    {
                        yearItem.IsExpanded = true;
                        monthItem.IsExpanded = true;
                    }
                }

                if (yearItem.Items.Count > 0)
                    TreeFechas.Items.Add(yearItem);
            }
        }
        catch
        {
        }
    }

    private static int ParseMonthPrefix(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var parts = name.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return 0;
        if (int.TryParse(parts[0], out var m)) return m;
        return 0;
    }

    private async void TreeFechas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (TreeFechas.SelectedItem is not TreeViewItem item)
                return;

            if (item.Tag is not string folder || !Directory.Exists(folder))
                return;

            var culture = new CultureInfo("es-ES");
            var dayName = Path.GetFileName(folder);

            if (!DateTime.TryParseExact(dayName, "dd-MM-yyyy", culture, DateTimeStyles.None, out var date))
                return;

            var monthDir = Path.GetDirectoryName(folder);
            var yearDir = Path.GetDirectoryName(monthDir ?? string.Empty);

            if (string.IsNullOrWhiteSpace(monthDir) || string.IsNullOrWhiteSpace(yearDir))
                return;

            var monthName = Path.GetFileName(monthDir);
            var yearName = Path.GetFileName(yearDir);

            if (!int.TryParse(yearName, out var year))
                return;

            var month = ParseMonthPrefix(monthName);
            if (month <= 0)
                return;

            _vm.SelectedYear = year;
            _vm.SelectedMonth = month;
            _vm.SelectedDay = date.Day;

            await _vm.LoadLocalAsync(year, month, date.Day);
        }
        catch
        {
        }
    }
}
