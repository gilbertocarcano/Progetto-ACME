using AcmeUI.Models;
using AcmeUI.Services;
using AcmeUI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Meta.Numerics.Functions;

namespace AcmeUI.ViewModels
{
    public partial class SensorAnalysisViewModel : ObservableObject
    {
        private readonly SensorAnalysis _window;
        private bool _arduinoListenersAttached = false;
        private float _currentCurrent;

        private TaskCompletionSource<bool>? _measurementCompletionSource;
        private bool _isAborted = false;

        public ObservableCollection<CalibrationRow> CalibrationRows { get; } = new();

        // ============================
        //     SERIE GRAFICO XY
        // ============================
        public ObservableCollection<ISeries> ChartSeries { get; } =
            new ObservableCollection<ISeries>
            {
                new LineSeries<(float X, float Y)>
                {
                    Values = new ObservableCollection<(float X, float Y)>(),
                    Mapping = (model, index) =>
                        new Coordinate(model.X, model.Y),
                    Name = "Vsx",
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    Fill = null,
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColors.Red, 2)
                },
                new LineSeries<(float X, float Y)>
                {
                    Values = new ObservableCollection<(float X, float Y)>(),
                    Mapping = (model, index) =>
                        new Coordinate(model.X, model.Y),
                    Name = "Vdx",
                    Stroke = new SolidColorPaint(SKColors.Blue, 2),
                    Fill = null,
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColors.Blue, 2)
                },
                new LineSeries<(float X, float Y)>
                {
                    Values = new ObservableCollection<(float X, float Y)>(),
                    Mapping = (model, index) =>
                        new Coordinate(model.X, model.Y),
                    Name = "Vup",
                    Stroke = new SolidColorPaint(SKColors.Green, 2),
                    Fill = null,
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColors.Green, 2)
                },
                new LineSeries<(float X, float Y)>
                {
                    Values = new ObservableCollection<(float X, float Y)>(),
                    Mapping = (model, index) =>
                        new Coordinate(model.X, model.Y),
                    Name = "Vdw",
                    Stroke = new SolidColorPaint(SKColors.Orange, 2),
                    Fill = null,
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColors.Orange, 2)
                },
                new LineSeries<(float X, float Y)>
                {
                    Values = new ObservableCollection<(float X, float Y)>(),
                    Mapping = (model, index) =>
                        new Coordinate(model.X, model.Y),
                    Name = "Vc",
                    Stroke = new SolidColorPaint(SKColors.Purple, 2),
                    Fill = null,
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColors.Purple, 2)
                }
            };


        public Axis[] ChartXAxes { get; } =
        {
            new Axis
            {
                Name = "Corrente (mA)",
                MinStep = 100
            }
        };

        public Axis[] ChartYAxes { get; } =
        {
            new Axis
            {
                Name = "Vout (V)"
            }
        };

        private float _maxCurrent;
        public float MaxCurrent
        {
            get => _maxCurrent;
            set => SetProperty(ref _maxCurrent, value);
        }

        private float _currentStep;
        public float CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public SensorAnalysisViewModel(SensorAnalysis window)
        {
            _window = window;
            MaxCurrent = 4000f;
            CurrentStep = 1000f;
        }

        // ============================
        //     AVVIO ANALISI
        // ============================
        [RelayCommand]
        private async Task StartAnalysisAsync()
        {
            double l = LateralFluxRate();
            if (CurrentStep <= 0 || MaxCurrent <= 0)
                return;

            _isAborted = false;
            CalibrationRows.Clear();

            // Pulisci grafico
            foreach (var s in ChartSeries)
                (s.Values as ObservableCollection<(float X, float Y)>)!.Clear();

            if (!_arduinoListenersAttached)
            {
                SerialService.Instance.OnEventReceived += HandleArduinoEvent;
                _arduinoListenersAttached = true;
            }

            // Ciclo da -MaxCurrent a +MaxCurrent
            for (float current = -MaxCurrent; current <= MaxCurrent; current += CurrentStep)
            {
                if (_isAborted)
                    break;

                _currentCurrent = current;

                bool? result = await _window.ShowAcquisitionDialogAsync(current);
                if (result != true || _isAborted)
                    break;

                _measurementCompletionSource = new TaskCompletionSource<bool>();

                SerialService.Instance.SendCommand("MEASURE");

                var completed = await Task.WhenAny(_measurementCompletionSource.Task, Task.Delay(5000));

                if (_isAborted)
                    break;

                if (completed != _measurementCompletionSource.Task)
                    break;

                await _measurementCompletionSource.Task;
            }
        }

        // ============================
        //     ABORT
        // ============================
        [RelayCommand]
        private void Abort()
        {
            _isAborted = true;
            _measurementCompletionSource?.TrySetCanceled();
            SerialService.Instance.SendCommand("ABORT");
        }

        // ============================
        //     EVENTI ARDUINO
        // ============================
        private void HandleArduinoEvent(string evt, string[] args)
        {
            if (_isAborted)
                return;

            switch (evt)
            {
                case "CALIBRATION_VALUES_READ":

                    float v1 = Convert.ToSingle(args[1], CultureInfo.InvariantCulture);
                    float v2 = Convert.ToSingle(args[2], CultureInfo.InvariantCulture);
                    float v3 = Convert.ToSingle(args[3], CultureInfo.InvariantCulture);
                    float v4 = Convert.ToSingle(args[4], CultureInfo.InvariantCulture);
                    float v5 = Convert.ToSingle(args[5], CultureInfo.InvariantCulture);

                    _window.DispatcherQueue.TryEnqueue(() =>
                    {
                        // Tabella
                        CalibrationRows.Add(new CalibrationRow
                        {
                            Position = _currentCurrent,
                            V1 = v1,
                            V2 = v2,
                            V3 = v3,
                            V4 = v4,
                            V5 = v5
                        });

                        (ChartSeries[0].Values as ObservableCollection<(float X, float Y)>)
                            !.Add((_currentCurrent, v1));

                        (ChartSeries[1].Values as ObservableCollection<(float X, float Y)>)
                            !.Add((_currentCurrent, v2));

                        (ChartSeries[2].Values as ObservableCollection<(float X, float Y)>)
                            !.Add((_currentCurrent, v3));

                        (ChartSeries[3].Values as ObservableCollection<(float X, float Y)>)
                            !.Add((_currentCurrent, v4));

                        (ChartSeries[4].Values as ObservableCollection<(float X, float Y)>)
                            !.Add((_currentCurrent, v5));

                    });

                    break;

                case "MEASUREMENT_COMPLETED":
                    _measurementCompletionSource?.TrySetResult(true);
                    break;
            }
        }

        private double LateralFluxRate()
        {
            // Geometria del tuo solenoide
            double R = 0.07;      // raggio solenoide [m]
            double L = 0.14;      // lunghezza solenoide [m]
            double r_laterale = 0.05; // raggio sensore laterale [m]
            double z = 0.0;       // piano centrale

            // Parametri solenoide
            int N = 10000;         // numero spire (non importa nel rapporto)
            double n = N / L;     // densità spire [1/m]

            // Risoluzione integrazione
            double dz = 0.0005;   // 0.5 mm

            double B_centrale = Bz_Solenoide(0.0, z, R, L, n, dz);
            double B_laterale = Bz_Solenoide(r_laterale, z, R, L, n, dz);

            double alpha = B_laterale / B_centrale;

            return alpha;
        }

        // ============================
        //   CAMPO DI UNA SINGOLA SPIRA
        // ============================
        static double Bz_Spira(double r, double z, double a)
        {
            const double mu0 = 4e-7 * Math.PI;   // permeabilità magnetica del vuoto

            // Caso r = 0 → formula assiale classica, più stabile
            if (r == 0.0)
            {
                double den = Math.Pow(a * a + z * z, 1.5);
                return mu0 * a * a / (2.0 * den);
            }

            double rp = a + r;
            double rm = a - r;
            double z2 = z * z;

            double k2 = 4.0 * a * r / (rp * rp + z2);   // parametro m = k^2

            double K = AdvancedMath.EllipticK(k2);      // K(m)
            double E = AdvancedMath.EllipticE(k2);      // E(m)

            double denom = Math.Sqrt(rp * rp + z2);
            double num = K + ((a * a - r * r - z2) / (rm * rm + z2)) * E;

            return (mu0 / (2.0 * Math.PI * denom)) * num;
        }

        // ============================
        //   CAMPO DEL SOLENOIDE
        // ============================
        static double Bz_Solenoide(double r, double z0, double R, double L, double n, double dz)
        {
            double B = 0.0;

            for (double z = -L / 2; z <= L / 2; z += dz)
            {
                B += Bz_Spira(r, z0 - z, R) * n * dz;
            }

            return B;
        }
    }
}
