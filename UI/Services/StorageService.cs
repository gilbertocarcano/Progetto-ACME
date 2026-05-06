using AcmeUI.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AcmeUI.Services
{
    public sealed class StorageService
    {
        private static readonly Lazy<StorageService> _instance =
            new(() => new StorageService());

        public static StorageService Instance => _instance.Value;

        private StorageService() { }

        // ---------------------------------------------------------
        // SAVE CSV (CommonSaveFileDialog)
        // ---------------------------------------------------------
        public async Task SaveCSVAsync(IEnumerable<ScanRow> rows)
        {
            var dialog = new CommonSaveFileDialog
            {
                Title = "Salva CSV",
                DefaultExtension = ".csv",
                AlwaysAppendDefaultExtension = true
            };

            dialog.Filters.Add(new CommonFileDialogFilter("CSV file", "*.csv"));

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            using var writer = new StreamWriter(dialog.FileName);

            writer.WriteLine("Position,B1,B2,B3,B4,B5,IsInHRRange");

            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",",
                    row.Position.ToString(CultureInfo.InvariantCulture),
                    row.B1.ToString(CultureInfo.InvariantCulture),
                    row.B2.ToString(CultureInfo.InvariantCulture),
                    row.B3.ToString(CultureInfo.InvariantCulture),
                    row.B4.ToString(CultureInfo.InvariantCulture),
                    row.B5.ToString(CultureInfo.InvariantCulture),
                    row.IsInHRRange ? "1" : "0"
                ));
            }
        }

        // ---------------------------------------------------------
        // LOAD CSV (CommonOpenFileDialog)
        // ---------------------------------------------------------
        public async Task LoadCSVAsync(Action<List<ScanRow>> onLoad)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Apri CSV",
                Multiselect = false
            };

            dialog.Filters.Add(new CommonFileDialogFilter("CSV file", "*.csv"));

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            var result = new List<ScanRow>();

            using var reader = new StreamReader(dialog.FileName);

            reader.ReadLine(); // skip header

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 7)
                    continue;

                result.Add(new ScanRow
                {
                    Position = float.Parse(parts[0], CultureInfo.InvariantCulture),
                    B1 = float.Parse(parts[1], CultureInfo.InvariantCulture),
                    B2 = float.Parse(parts[2], CultureInfo.InvariantCulture),
                    B3 = float.Parse(parts[3], CultureInfo.InvariantCulture),
                    B4 = float.Parse(parts[4], CultureInfo.InvariantCulture),
                    B5 = float.Parse(parts[5], CultureInfo.InvariantCulture),
                    IsInHRRange = parts[6] == "1"
                });
            }

            onLoad?.Invoke(result);
        }
    }
}
