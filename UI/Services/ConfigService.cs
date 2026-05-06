using AcmeUI.Models;
using AcmeUI.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public static class ConfigService
{
    private const string FileName = "localsettings.json";

    // ---------------------------------------------------------
    // PARAMETRI ARDUINO (solo in RAM)
    // ---------------------------------------------------------
    public static ArduinoParameters Arduino { get; } = new ArduinoParameters();

    private static bool _arduinoListenersAttached = false;
    public static event Action? ArduinoParametersUpdated;

    // ---------------------------------------------------------
    // CARICA I PARAMETRI DA ARDUINO
    // ---------------------------------------------------------
    public static async Task LoadArduinoParametersAsync()
    {
        var local = LoadSettings();
        if (local.Port is null)
            return;

        bool ok = SerialService.Instance.TryOpen(local.Port, local.BaudRate);
        if (!ok)
            return;

        // Sottoscrizione eventi una sola volta
        if (!_arduinoListenersAttached)
        {
            SerialService.Instance.OnEventReceived += HandleArduinoEvent;
            _arduinoListenersAttached = true;
        }

        // Richiesta parametri
        SerialService.Instance.SendCommand("GET_STEPSPERUNIT");
        SerialService.Instance.SendCommand("GET_STEPLRSIZE");
        SerialService.Instance.SendCommand("GET_STEPHRSIZE");
        SerialService.Instance.SendCommand("GET_STARTPOSITION");
        SerialService.Instance.SendCommand("GET_ENDPOSITION");

        SerialService.Instance.SendCommand("GET_HOMINGSPEED");
        SerialService.Instance.SendCommand("GET_NORMALSPEED");
        SerialService.Instance.SendCommand("GET_DELAYBEFOREREAD");
        SerialService.Instance.SendCommand("GET_DELAYAFTERREAD");

        SerialService.Instance.SendCommand("GET_HALLVREF");
        SerialService.Instance.SendCommand("GET_HALLSENSITIVITY");

        SerialService.Instance.SendCommand("GET_HRRANGES");

        await Task.Delay(200);
    }

    // ---------------------------------------------------------
    // SALVA I PARAMETRI SU ARDUINO
    // ---------------------------------------------------------
    public static async Task SaveArduinoParametersAsync()
    {
        var p = Arduino;

        SerialService.Instance.SendCommand("SET_STEPSPERUNIT", p.StepsPerUnit.ToString(CultureInfo.InvariantCulture));
        SerialService.Instance.SendCommand("SET_STEPLRSIZE", p.StepLRSize.ToString(CultureInfo.InvariantCulture));
        SerialService.Instance.SendCommand("SET_STEPHRSIZE", p.StepHRSize.ToString(CultureInfo.InvariantCulture));
        SerialService.Instance.SendCommand("SET_STARTPOSITION", p.StartPosition.ToString(CultureInfo.InvariantCulture));
        SerialService.Instance.SendCommand("SET_ENDPOSITION", p.EndPosition.ToString(CultureInfo.InvariantCulture));

        // Conversione mm/sec → passi/sec
        int homingStepsPerSec = (int)(p.HomingSpeed * p.StepsPerUnit);
        int normalStepsPerSec = (int)(p.NormalSpeed * p.StepsPerUnit);

        SerialService.Instance.SendCommand("SET_HOMINGSPEED", homingStepsPerSec.ToString());
        SerialService.Instance.SendCommand("SET_NORMALSPEED", normalStepsPerSec.ToString());

        SerialService.Instance.SendCommand("SET_DELAYBEFOREREAD", p.DelayBeforeRead.ToString());
        SerialService.Instance.SendCommand("SET_DELAYAFTERREAD", p.DelayAfterRead.ToString());

        string[] values = new string[5];
        for (int i = 0; i < 5; i++)
        {

            SerialService.Instance.SendCommand("SET_HALLVREF",
                new string[] { i.ToString(), p.HallVref[i].ToString(CultureInfo.InvariantCulture) });
        }

        for (int i = 0; i < 5; i++)
        {
            SerialService.Instance.SendCommand("SET_HALLSENSITIVITY",
                new string[] { i.ToString(), p.HallSensitivity[i].ToString(CultureInfo.InvariantCulture) });
        }


        // ---------------------------------------------------------
        // SALVATAGGIO HR RANGES
        // ---------------------------------------------------------
        if (p.HRRanges.Count == 0)
        {
            SerialService.Instance.SendCommand("SET_HRRANGES");
        }
        else
        {
            var parameters = new List<string>();

            foreach (var r in p.HRRanges)
            {
                parameters.Add(
                    $"{FromRelativePosition(r.Start).ToString(CultureInfo.InvariantCulture)}," +
                    $"{FromRelativePosition(r.End).ToString(CultureInfo.InvariantCulture)}"
                );
            }

            SerialService.Instance.SendCommand("SET_HRRANGES", parameters.ToArray());
        }

        await Task.Delay(200);
        SerialService.Instance.SendCommand("SAVE_CONFIG");
        await Task.Delay(200);
    }

    // ---------------------------------------------------------
    // EVENTI DA ARDUINO → aggiornano ConfigService.Arduino
    // ---------------------------------------------------------
    private static void HandleArduinoEvent(string evt, string[] args)
    {
        if (args.Length == 0)
            return;

        switch (evt)
        {
            case "STEPSPERUNIT":
                Arduino.StepsPerUnit = float.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "STEPLRSIZE":
                Arduino.StepLRSize = float.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "STEPHRSIZE":
                Arduino.StepHRSize = float.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "STARTPOSITION":
                Arduino.StartPosition = float.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "ENDPOSITION":
                Arduino.EndPosition = float.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "HOMINGSPEED":
                {
                    int stepsPerSec = int.Parse(args[0], CultureInfo.InvariantCulture);
                    if (Arduino.StepsPerUnit > 0)
                    {
                        Arduino.HomingSpeed = (int)(stepsPerSec / Arduino.StepsPerUnit);
                        RaiseArduinoParametersUpdated();
                    }
                    break;
                }

            case "NORMALSPEED":
                {
                    int stepsPerSec = int.Parse(args[0], CultureInfo.InvariantCulture);
                    if (Arduino.StepsPerUnit > 0)
                    {
                        Arduino.NormalSpeed = (int)(stepsPerSec / Arduino.StepsPerUnit);
                        RaiseArduinoParametersUpdated();
                    }
                    break;
                }

            case "DELAYBEFOREREAD":
                Arduino.DelayBeforeRead = int.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "DELAYAFTERREAD":
                Arduino.DelayAfterRead = int.Parse(args[0], CultureInfo.InvariantCulture);
                RaiseArduinoParametersUpdated();
                break;

            case "HRRANGES":
                Arduino.HRRanges.Clear();

                foreach (var p in args)
                {
                    var parts = p.Split(',');
                    if (parts.Length == 2 &&
                        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float s) &&
                        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float e))
                    {
                        Arduino.HRRanges.Add(new HRRange { Start = ToRelativePosition(s), End = ToRelativePosition(e) });
                    }
                }
                RaiseArduinoParametersUpdated();
                break;
            case "HALLVREF":
                Arduino.HallVref[0] = float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float s0) ? s0 : 0;
                Arduino.HallVref[1] = float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float s1) ? s1 : 0;
                Arduino.HallVref[2] = float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float s2) ? s2 : 0;
                Arduino.HallVref[3] = float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float s3) ? s3 : 0;
                Arduino.HallVref[4] = float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float s4) ? s4 : 0;
                break;
            case "HALLSENSITIVITY":
                Arduino.HallSensitivity[0] = float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float h0) ? h0 : 0;
                Arduino.HallSensitivity[1] = float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float h1) ? h1 : 0;
                Arduino.HallSensitivity[2] = float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float h2) ? h2 : 0;
                Arduino.HallSensitivity[3] = float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float h3) ? h3 : 0;
                Arduino.HallSensitivity[4] = float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float h4) ? h4 : 0;
                RaiseArduinoParametersUpdated();
                break;
        }
    }

    // ---------------------------------------------------------
    // SETTINGS LOCALI (persistenti)
    // ---------------------------------------------------------
    public static void SaveSettings(string? port, int baudRate)
    {
        var data = new LocalSettings { Port = port, BaudRate = baudRate };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FileName, json);
    }

    public static LocalSettings LoadSettings()
    {
        if (!File.Exists(FileName))
            return new LocalSettings { Port = null, BaudRate = 250000 };

        var json = File.ReadAllText(FileName);
        return JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
    }

    private static void RaiseArduinoParametersUpdated()
    {
        ArduinoParametersUpdated?.Invoke();
    }

    // Metodi helper per la conversione di coordinate da coordinate macchina (con home come origine)
    // a coordinate relative (con centro come origine) e viceversa
    public static float ToRelativePosition(float machinePosition)
    {
        return machinePosition - Arduino.CenterPosition;
    }

    public static float FromRelativePosition(float relativePosition)
    {
        return relativePosition + Arduino.CenterPosition;
    }
}

public class LocalSettings
{
    public string? Port { get; set; }
    public int BaudRate { get; set; }
}

public class ArduinoParameters
{
    public float StepsPerUnit { get; set; }
    public float StepLRSize { get; set; }
    public float StepHRSize { get; set; }
    public float StartPosition { get; set; }
    public float EndPosition { get; set; }
    public float CenterPosition { get { return (StartPosition + EndPosition) / 2; } }

    public int HomingSpeed { get; set; }      // mm/sec
    public int NormalSpeed { get; set; }      // mm/sec
    public int DelayBeforeRead { get; set; }
    public int DelayAfterRead { get; set; }

    public float[] HallVref { get; set; } = new float[5]
    {
        2.5f, 2.5f, 2.5f, 2.5f, 2.5f
    };

    public float[] HallSensitivity { get; set; } = new float[5]
    {
        3.3F, 3.3F, 3.3F, 3.3F, 3.3F
    };

    public List<HRRange> HRRanges { get; } = new();
}
