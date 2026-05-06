using AcmeUI.Models;
using AcmeUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace AcmeUI.ViewModels;

public class MainViewModel : BaseViewModel
{
    private string _status = "Idle";
    private float _position = 0;
    private float _machinePosition = 0;

    private float _startPosition = 0;
    private float _endPosition = 0;
    private float _centerPosition = 0;

    public ObservableCollection<ScanRow> ScanRows { get; } = new();

    public ISeries[] ChartSeries { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public LegendPosition LegendPosition { get; set; } = LegendPosition.Right;

    public ISeries[] IntBSeries { get; set; }
    public ISeries[] DeltaIntBSeries { get; set; }

    public Axis[] IntBXAxis { get; set; }
    public Axis[] DeltaIntBXAxis { get; set; }
    public Axis[] MiniYAxis { get; set; }

    public float StartPosition
    {
        get => _startPosition;
        set => SetProperty(ref _startPosition, value);
    }
    public float EndPosition
    {
        get => _endPosition;
        set => SetProperty(ref _endPosition, value);
    }
    public float CenterPosition
    {
        get => _centerPosition;
        set => SetProperty(ref _centerPosition, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public float Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    public float MachinePosition
    {
        get => _machinePosition;
        set => SetProperty(ref _machinePosition, value);
    }

    public IRelayCommand GoHomeCommand { get; }
    public IRelayCommand GoStartCommand { get; }
    public IRelayCommand GoEndCommand { get; }
    public IRelayCommand GoZeroCommand { get; }
    public IRelayCommand StartScanCommand { get; }
    public IRelayCommand AbortCommand { get; }
    public IRelayCommand NormalizeCurvesCommand { get; }

    public event Action? AlarmRequested;

    private bool _isDataReady = false;
    public bool IsDataReady
    {
        get => _isDataReady;
        set => SetProperty(ref _isDataReady, value);
    }

    private bool _isAlarmPopupOpen = false;
    public bool IsAlarmPopupOpen
    {
        get => _isAlarmPopupOpen;
        set => SetProperty(ref _isAlarmPopupOpen, value);
    }

    public MainViewModel()
    {
        SerialService.Instance.OnEventReceived += HandleArduinoEvent;

        //OpenSettingsCommand = new RelayCommand(OpenSettings);
        GoHomeCommand = new RelayCommand(GoHome);
        GoStartCommand = new RelayCommand(GoStart);
        GoEndCommand = new RelayCommand(GoEnd);
        GoZeroCommand = new RelayCommand(GoZero);
        StartScanCommand = new RelayCommand(StartScan);
        AbortCommand = new RelayCommand(Abort);
        NormalizeCurvesCommand = new RelayCommand(NormalizeCurves);

        ChartSeries = new ISeries[]
        {
            new LineSeries<ScanRow>
            {
                Name = "X=-50, Y=0",
                Values = ScanRows,
                Mapping = (row, index) =>
                    new Coordinate(row.Position, row.B1),
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 }
            },

            new LineSeries<ScanRow>
            {
                Name = "X=50, Y=0",
                Values = ScanRows,
                Mapping = (row, index) =>
                    new Coordinate(row.Position, row.B2),
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 }
            },

            new LineSeries<ScanRow>
            {
                Name = "X=0, Y=50",
                Values = ScanRows,
                Mapping = (row, index) =>
                    new Coordinate(row.Position, row.B3),
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 }
            },

            new LineSeries<ScanRow>
            {
                Name = "X=0, Y=-50",
                Values = ScanRows,
                Mapping = (row, index) =>
                   new Coordinate(row.Position, row.B4),
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 }
            },

            new LineSeries<ScanRow>
            {
                Name = "X=0, Y=0",
                Values = ScanRows,
                Mapping = (row, index) =>
                    new Coordinate(row.Position, row.B5),
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 }
            }
        };

        XAxes = new Axis[]
        {
            new Axis { Name = "Z (mm)", MinLimit = null, MaxLimit = null }
        };

        YAxes = new Axis[]
        {
            new Axis { Name = "B (gauss)", MinLimit = null, MaxLimit = null }
        };

        ConfigService.ArduinoParametersUpdated += () =>
        {
            App.MainAppWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                StartPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.StartPosition);
                EndPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.EndPosition);
                CenterPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.CenterPosition);

                // Aggiorna asse X del grafico
                XAxes[0].MinLimit = ConfigService.ToRelativePosition(ConfigService.Arduino.StartPosition);
                XAxes[0].MaxLimit = ConfigService.ToRelativePosition(ConfigService.Arduino.EndPosition);
                OnPropertyChanged(nameof(XAxes));
            });
        };

        // Assi X con etichette piccole
        IntBXAxis = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "dx", "sx", "up", "dw", "C" },
                LabelsRotation = 0,
                TextSize = 14,
                MinStep = 1,
                UnitWidth = 1,
                ForceStepToMin = true
            }
        };

        DeltaIntBXAxis = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "dx", "sx", "up", "dw", "C" },
                LabelsRotation = 0,
                TextSize = 14,
                MinStep = 1,
                UnitWidth = 1,
                ForceStepToMin = true
            }
        };

        // Asse Y condiviso (senza numeri, molto compatto)
        MiniYAxis = new Axis[]
        {
            new Axis
            {
                MinLimit = null,
                MaxLimit = null,
                TextSize = 12,
                ShowSeparatorLines = false
            }
        };

        // Serie inizialmente vuote
        IntBSeries = new ISeries[]
        {
            new ColumnSeries<float>
            {
                Values = new float[5],
                Stroke = null,
                Fill = new SolidColorPaint(SKColors.SteelBlue)
            }
        };

        DeltaIntBSeries = new ISeries[]
        {
            new ColumnSeries<float>
            {
                Values = new float[5],
                Stroke = null,
                Fill = new SolidColorPaint(SKColors.OrangeRed)
            }
        };

    }

    public void IntegrateValues()
    {
        var intB = IntegrationService.Instance.IntB(ScanRows);
        var delta = IntegrationService.Instance.DeltaIntB(ScanRows, 4);
       
        for (int i = 0; i < intB.Length; i++)
            intB[i] = MathF.Round(intB[i], 2);

        for (int i = 0; i < delta.Length; i++)
            delta[i] = MathF.Round(delta[i], 3);

        (IntBSeries[0] as ColumnSeries<float>)!.Values = intB;
        (DeltaIntBSeries[0] as ColumnSeries<float>)!.Values = delta;

        OnPropertyChanged(nameof(IntBSeries));
        OnPropertyChanged(nameof(DeltaIntBSeries));
    }

    private void NormalizeCurves()
    {        
        GaussianFitService.FitAllSeries(ScanRows);
        
        // Aggiorna grafico e tabella
        OnPropertyChanged(nameof(ScanRows));

        IntegrateValues(); ;
    }

    //public void ClearIntBCharts()
    //{
    //    (IntBSeries[0] as ColumnSeries<float>)!.Values = new List<float>();
    //    (DeltaIntBSeries[0] as ColumnSeries<float>)!.Values = new List<float>();

    //    OnPropertyChanged(nameof(IntBSeries));
    //    OnPropertyChanged(nameof(DeltaIntBSeries));
    //}


    //private void OpenSettings()
    //{
    //    var win = new Views.SettingsWindow();
    //    win.Activate();
    //}

    private void GoHome()
    {
        SerialService.Instance.SendCommand("GOTO_HOME");
    }

    private void GoStart()
    {
        SerialService.Instance.SendCommand("GOTO_START");
    }

    private void GoEnd()
    {
        SerialService.Instance.SendCommand("GOTO_END");
    }

    private void GoZero()
    {
        SerialService.Instance.SendCommand("GOTO_CENTER");
    }

    private void StartScan()
    {
        SerialService.Instance.SendCommand("SCAN");
    }

    private void Abort()
    {
        SerialService.Instance.SendCommand("ABORT");
    }

    public void HandleArduinoEvent(string evt, string[] args)
    {
        App.MainAppWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            if (evt == "STATE_CHANGED")
            {
                string newState = args[0];
                switch (newState)
                {
                    case "HOMING":
                        Status = "Homing in corso...";
                        break;
                    case "MOVING":
                        Status = "Spostamento in corso...";
                        break;
                    case "SCANNING":
                        Status = "Rilevazione in corso...";
                        break;
                    case "CALIBRATING":
                        Status = "Calibrazione in corso...";
                        break;
                    case "IDLE":
                        Status = "Idle";
                        break;
                    case "ALARM":
                        Status = "In allarme";

                        // Se il popup è già aperto, ignora gli eventi successivi
                        if (IsAlarmPopupOpen)
                            break;

                        IsAlarmPopupOpen = true;

                        // Notifica la MainWindow che deve aprire il popup
                        AlarmRequested?.Invoke();

                        break;

                    default:
                        Status = "Stato sconosciuto";
                        break;
                }
            }
            else if (evt == "P")
            {
                float p = Convert.ToSingle(args[0], CultureInfo.InvariantCulture);
                MachinePosition = p;
                Position = ConfigService.ToRelativePosition(p);
            }
            else if (evt == "SCAN_STARTED")
            {
                ScanRows.Clear();
                IntegrateValues(); //Per svuotare i grafici;
                IsDataReady = false;
                //ClearIntBCharts();
            }
            else if (evt == "SCAN_COMPLETED")
            {
                IntegrateValues();
                IsDataReady = true;
            }
            else if (evt == "SCAN_VALUES_READ")
            {
                float position = ConfigService.ToRelativePosition(Convert.ToSingle(args[0], CultureInfo.InvariantCulture));
                float b1 = Convert.ToSingle(args[1], CultureInfo.InvariantCulture);
                float b2 = Convert.ToSingle(args[2], CultureInfo.InvariantCulture);
                float b3 = Convert.ToSingle(args[3], CultureInfo.InvariantCulture);
                float b4 = Convert.ToSingle(args[4], CultureInfo.InvariantCulture);
                float b5 = Convert.ToSingle(args[5], CultureInfo.InvariantCulture);

                bool isHR = ConfigService.Arduino.HRRanges.Any(r => Position >= r.Start && Position <= r.End);

                ScanRows.Add(new ScanRow
                {
                    Position = position,
                    IsInHRRange = isHR,
                    B1 = b1,
                    B2 = b2,
                    B3 = b3,
                    B4 = b4,
                    B5 = b5
                });

            }
        });
    }
}

