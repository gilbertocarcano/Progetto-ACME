using AcmeUI.Models;
using AcmeUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AcmeUI.ViewModels;

public class ArduinoSettingsViewModel : BaseViewModel
{
    private bool _arduinoListenersAttached = false;
    private List<CalibrationRow> _calibrationRows = new List<CalibrationRow>();

    // ---------------------------------------------------------
    // PROPRIETÀ PARAMETRI ARDUINO
    // ---------------------------------------------------------

    private float _stepsPerUnit;
    public float StepsPerUnit
    {
        get => _stepsPerUnit;
        set => SetProperty(ref _stepsPerUnit, value);
    }

    private float _stepLRSize;
    public float StepLRSize
    {
        get => _stepLRSize;
        set {SetProperty(ref _stepLRSize, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private float _stepHRSize;
    public float StepHRSize
    {
        get => _stepHRSize;
        set {SetProperty(ref _stepHRSize, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private float _startPosition;
    public float StartPosition
    {
        get => _startPosition;
        set { SetProperty(ref _startPosition, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private float _endPosition;
    public float EndPosition
    {
        get => _endPosition;
        set { SetProperty(ref _endPosition, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private int _homingSpeed;
    public int HomingSpeed
    {
        get => _homingSpeed;
        set => SetProperty(ref _homingSpeed, value);
    }

    private int _normalSpeed;
    public int NormalSpeed
    {
        get => _normalSpeed;
        set { SetProperty(ref _normalSpeed, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private int _delayBeforeRead;
    public int DelayBeforeRead
    {
        get => _delayBeforeRead;
        set { SetProperty(ref _delayBeforeRead, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    private int _delayAfterRead;
    public int DelayAfterRead
    {
        get => _delayAfterRead;
        set {SetProperty(ref _delayAfterRead, value); OnPropertyChanged(nameof(TotalScanTime)); }
    }

    public float TotalScanTime
    {
        get => ComputeTotalScanTime();
    }

    private float _vrefSx = 2.5f;
    public float VrefSx
    {
        get => _vrefSx;
        set => SetProperty(ref _vrefSx, value);
    }

    private float _vrefDx = 2.5f;
    public float VrefDx
    {
        get => _vrefDx;
        set => SetProperty(ref _vrefDx, value);
    }

    private float _vrefUp = 2.5f;
    public float VrefUp
    {
        get => _vrefUp;
        set => SetProperty(ref _vrefUp, value);
    }

    private float _vrefDw = 2.5f;
    public float VrefDw
    {
        get => _vrefDw;
        set => SetProperty(ref _vrefDw, value);
    }

    private float _vrefCenter = 2.5f;
    public float VrefCenter
    {
        get => _vrefCenter;
        set => SetProperty(ref _vrefCenter, value);
    }

    private float _sensitivitySx = 0.003125f;
    public float SensitivitySx
    {
        get => _sensitivitySx;
        set => SetProperty(ref _sensitivitySx, value);
    }

    private float _sensitivityDx = 0.003125f;
    public float SensitivityDx
    {
        get => _sensitivityDx;
        set => SetProperty(ref _sensitivityDx, value);
    }

    private float _sensitivityUp = 0.003125f;
    public float SensitivityUp
    {
        get => _sensitivityUp;
        set => SetProperty(ref _sensitivityUp, value);
    }

    private float _sensitivityDw = 0.003125f;
    public float SensitivityDw
    {
        get => _sensitivityDw;
        set => SetProperty(ref _sensitivityDw, value);
    }

    private float _sensitivityCenter = 0.003125f;
    public float SensitivityCenter
    {
        get => _sensitivityCenter;
        set => SetProperty(ref _sensitivityCenter, value);
    }


    // ---------------------------------------------------------
    // HR RANGES
    // ---------------------------------------------------------

    public ObservableCollection<HRRange> HRRanges { get; } = new();

    // ---------------------------------------------------------
    // COMANDI
    // ---------------------------------------------------------

    public IRelayCommand LoadFromArduinoCommand { get; }
    public IRelayCommand SaveToArduinoCommand { get; }
    public IRelayCommand AddHRRangeCommand { get; }
    public IRelayCommand CalibrateCommand { get; }


   
    private readonly SynchronizationContext? _uiContext;
    public ArduinoSettingsViewModel()
    {
        LoadFromArduinoCommand = new AsyncRelayCommand(LoadFromArduinoAsync);
        SaveToArduinoCommand = new AsyncRelayCommand(SaveToArduinoAsync);
        AddHRRangeCommand = new RelayCommand(AddHRRange);
        CalibrateCommand = new RelayCommand(OnCalibrateRequested);

        _uiContext = SynchronizationContext.Current;
        HRRanges.CollectionChanged += OnHRRangesCollectionChanged;
        foreach (var r in HRRanges)
            r.PropertyChanged += OnHRRangePropertyChanged;       
    }

    // ---------------------------------------------------------
    // CARICAMENTO PARAMETRI DA CONFIGSERVICE
    // ---------------------------------------------------------

    private async Task LoadFromArduinoAsync()
    {
        await ConfigService.LoadArduinoParametersAsync();

        var p = ConfigService.Arduino;

        StepsPerUnit = p.StepsPerUnit;
        StepLRSize = p.StepLRSize;
        StepHRSize = p.StepHRSize;
        StartPosition = p.StartPosition;
        EndPosition = p.EndPosition;

        HomingSpeed = p.HomingSpeed;
        NormalSpeed = p.NormalSpeed;
        DelayBeforeRead = p.DelayBeforeRead;
        DelayAfterRead = p.DelayAfterRead;

        VrefSx = p.HallVref[0];
        VrefDx = p.HallVref[1];
        VrefUp = p.HallVref[2];
        VrefDw = p.HallVref[3];
        VrefCenter = p.HallVref[4];

        SensitivitySx = p.HallSensitivity[0];
        SensitivityDx = p.HallSensitivity[1];
        SensitivityUp = p.HallSensitivity[2];
        SensitivityDw = p.HallSensitivity[3];
        SensitivityCenter = p.HallSensitivity[4];

        HRRanges.Clear();
        foreach (var r in p.HRRanges)
            HRRanges.Add(new HRRange { Start = r.Start, End = r.End });
    }

    // ---------------------------------------------------------
    // SALVATAGGIO PARAMETRI SU CONFIGSERVICE → ARDUINO
    // ---------------------------------------------------------

    public async Task SaveToArduinoAsync()
    {
        var p = ConfigService.Arduino;

        // Copia valori scalar
        p.StepsPerUnit = StepsPerUnit;
        p.StepLRSize = StepLRSize;
        p.StepHRSize = StepHRSize;
        p.StartPosition = StartPosition;
        p.EndPosition = EndPosition;

        p.HomingSpeed = HomingSpeed;
        p.NormalSpeed = NormalSpeed;
        p.DelayBeforeRead = DelayBeforeRead;
        p.DelayAfterRead = DelayAfterRead;

        p.HallVref[0] = VrefSx;
        p.HallVref[1] = VrefDx;
        p.HallVref[2] = VrefUp;
        p.HallVref[3] = VrefDw;
        p.HallVref[4] = VrefCenter;

        p.HallSensitivity[0] = SensitivitySx;
        p.HallSensitivity[1] = SensitivityDx;
        p.HallSensitivity[2] = SensitivityUp;
        p.HallSensitivity[3] = SensitivityDw;
        p.HallSensitivity[4] = SensitivityCenter;

        // Copia HRRanges
        p.HRRanges.Clear();
        foreach (var r in HRRanges)
            p.HRRanges.Add(new HRRange { Start = r.Start, End = r.End });

        // Salva su Arduino
        await ConfigService.SaveArduinoParametersAsync();

        // Ricarica per sicurezza
        await LoadFromArduinoAsync();
    }


    public List<string> ValidateBeforeSave()
    {
        var list = new List<string>();

        if (!(new float[] { 25, 50, 100, 200, 400 }).Any(v=>v==StepsPerUnit))
            list.Add("Il numero passi per millimetro deve avere uno dei seguenti valori: 25, 50, 100, 200, 400 (verificare il microstepping)");

        if (EndPosition <= StartPosition)
            list.Add("La posizione finale deve essere maggiore di quella iniziale.");

        if (EndPosition <= StartPosition)
            list.Add("La posizione finale non può superare 320 mm.");

        if (HomingSpeed <= 0)
            list.Add("La velocità homing deve essere compresa nell'intervallo [1 mm/s, 50 mm/s].");

        if (NormalSpeed <= 0)
            list.Add("La velocità normale deve essere compresa nell'intervallo [1 mm/s, 50 mm/s].");

        if (StepLRSize < 5 || StepLRSize > 10)
            list.Add("Il passo LR deve essere compreso nell'intervallo [5 mm,10 mm].");

        if (StepHRSize < 1 || StepHRSize > 4)
            list.Add("Il passo HR deve essere compreso nell'intervallo [1 mm,4 mm].");

        if (DelayBeforeRead < 0 || DelayBeforeRead > 2000)
            list.Add("Il delay prima della singola lettura deve essere compreso nell'intervallo [1 ms,2000 ms].");

        if (DelayAfterRead < 1 || DelayBeforeRead > 200)
            list.Add("La durata ciclo singola lettura deve essere compresa nell'intervallo [1 ms,200 ms].");

        // ============================
        // VALIDAZIONE HR RANGES
        // ============================

        float zMax = EndPosition - StartPosition;        

        float totalHRLength = 0;

        foreach (var r in HRRanges)
        {
            float len = r.End - r.Start;

            if (len <= 0)
                list.Add($"Range HR non valido: {r.Start} → {r.End}");

            totalHRLength += len;
        }

        float maxAllowed = 0.20f * zMax;

        if (totalHRLength > maxAllowed)
        {
            list.Add(
                $"La lunghezza totale dei segmenti HR ({totalHRLength:F2} mm) " +
                $"eccede il limite del 20% della corsa ({maxAllowed:F2} mm)."
            );
        }

        return list;
    }


    // ---------------------------------------------------------
    // GESTIONE HR RANGES
    // ---------------------------------------------------------

    private void AddHRRange()
    {
        if (HRRanges.Count < 10)
            HRRanges.Add(new HRRange { Start = 0, End = 0 });                
    }

    public void RemoveHRRange(HRRange? range)
    {
        if (range != null)
            HRRanges.Remove(range);        
    }

    private void OnHRRangesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (HRRange r in e.NewItems)
                r.PropertyChanged += OnHRRangePropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (HRRange r in e.OldItems)
                r.PropertyChanged -= OnHRRangePropertyChanged;
        }

        _uiContext?.Post(_ =>
        {
            OnPropertyChanged(nameof(TotalScanTime));
        }, null);
    }

    private void OnHRRangePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HRRange.Start) ||
            e.PropertyName == nameof(HRRange.End))
        {
            OnPropertyChanged(nameof(TotalScanTime));
        }
    }



    private void OnCalibrateRequested()
    {
        // Sottoscrizione eventi una sola volta
        if (!_arduinoListenersAttached)
        {
            SerialService.Instance.OnEventReceived += HandleArduinoEvent;
            _arduinoListenersAttached = true;
        }

        SerialService.Instance.SendCommand("CALIBRATE");        
    }
    private void HandleArduinoEvent(string evt, string[] args)
    {
        switch (evt)
        {
            case "CALIBRATION_STARTED":
                _calibrationRows.Clear();
                break;
            case "CALIBRATION_VALUES_READ":
                _calibrationRows.Add(
                    new CalibrationRow
                    {
                        Position = Convert.ToSingle(args[0], CultureInfo.InvariantCulture),
                        V1 = Convert.ToSingle(args[1], CultureInfo.InvariantCulture),
                        V2 = Convert.ToSingle(args[2], CultureInfo.InvariantCulture),
                        V3 = Convert.ToSingle(args[3], CultureInfo.InvariantCulture),
                        V4 = Convert.ToSingle(args[4], CultureInfo.InvariantCulture),
                        V5 = Convert.ToSingle(args[5], CultureInfo.InvariantCulture)
                    });                   
                break;
            case "CALIBRATION_COMPLETED":
                App.MainAppWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    VrefSx = (float)Math.Round(_calibrationRows.Any() ? _calibrationRows.Average(r => r.V1) : 0f, 3);
                    VrefDx = (float)Math.Round(_calibrationRows.Any() ? _calibrationRows.Average(r => r.V2) : 0f, 3);
                    VrefUp = (float)Math.Round(_calibrationRows.Any() ? _calibrationRows.Average(r => r.V3) : 0f, 3);
                    VrefDw = (float)Math.Round(_calibrationRows.Any() ? _calibrationRows.Average(r => r.V4) : 0f, 3);
                    VrefCenter = (float)Math.Round(_calibrationRows.Any() ? _calibrationRows.Average(r => r.V5) : 0f, 3);
                });
                break;
        }
    }

    private float ComputeTotalScanTime()
    {
        if (NormalSpeed <= 0)
            return 0;

        float pos = StartPosition;
        float totalTime = 0f;

        // Primo movimento: da start a start (nullo)
        // ma aggiungiamo DelayBeforeRead + DelayAfterRead
        totalTime += (DelayBeforeRead + DelayAfterRead) / 1000f;

        while (true)
        {
            // Determina step dinamico
            float step = IsInHRRange(pos) ? StepHRSize : StepLRSize;

            float nextPos = pos + step;
            if (nextPos > EndPosition)
                break;

            // Tempo di movimento (mm / mm/sec)
            float moveTime = step / NormalSpeed;

            totalTime += moveTime;
            totalTime += (DelayBeforeRead + DelayAfterRead) / 1000f;

            pos = nextPos;
        }

        return totalTime;
    }

    private bool IsInHRRange(float position)
    {
        foreach (var r in HRRanges)
        {
            if (position >= r.Start && position <= r.End)
                return true;
        }
        return false;
    }

}
