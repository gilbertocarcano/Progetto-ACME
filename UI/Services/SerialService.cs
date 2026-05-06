using System;
using System.IO.Ports;
using System.Text;

namespace AcmeUI.Services;

public class SerialService
{
    private SerialPort? _port;
    private readonly StringBuilder _rxBuffer = new();

    public bool IsOpen => _port?.IsOpen ?? false;

    // EVENTI PUBBLICI
    public event Action<string, string[]>? OnEventReceived;   // EVT|...    
    public event Action<string>? OnRawLineReceived;

    // SINGLETON
    public static SerialService Instance { get; } = new SerialService();
    private SerialService() { }

    // APERTURA PORTA
    public void Open(string portName, int baudRate = 250000)
    {
        if (IsOpen)
            Close();

        _port = new SerialPort(portName, baudRate)
        {
            Encoding = Encoding.ASCII,
            NewLine = "\n",
            DtrEnable = true,
            RtsEnable = true
        };

        _port.DataReceived += SerialDataReceived;
        _port.Open();
    }

    public bool TryOpen(string portName, int baudRate = 250000)
    {
        try
        {
            Open(portName, baudRate);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // CHIUSURA PORTA
    public void Close()
    {
        if (!IsOpen)
            return;

        _port!.DataReceived -= SerialDataReceived;
        _port.Close();
        _port.Dispose();
        _port = null;
    }

    // INVIO COMANDO (PC → Arduino)
    public void SendCommand(string name, params string[] args)
    {
        if (!IsOpen)
            return;

        string line = "CMD|" + name;

        if (args.Length > 0)
            line += "|" + string.Join("|", args);

        _port!.WriteLine(line);
    }


    // RICEZIONE DATI (Arduino → PC)
    private void SerialDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null || !_port.IsOpen)
            return;

        try
        {
            string data = _port.ReadExisting();
            _rxBuffer.Append(data);

            while (true)
            {
                string buffer = _rxBuffer.ToString();
                int idx = buffer.IndexOf('\n');
                if (idx < 0)
                    break;

                string line = buffer[..idx].Trim();
                _rxBuffer.Remove(0, idx + 1);                

                ProcessLine(line);
            }
        }
        catch
        {
            // TODO: gestione errori
        }
    }

    // PARSING LINEA
    private void ProcessLine(string line)
    {
        OnRawLineReceived?.Invoke(line);

        string[] parts = line.Split('|');
        if (parts.Length == 0)
            return;

        string prefix = parts[0];
        string name = parts.Length > 1 ? parts[1] : "";
        string[] args = parts.Length > 2 ? parts[2..] : Array.Empty<string>();

        if (prefix == "EVT")
        {
            if (line.Contains("|LOG|"))
                System.Diagnostics.Debug.WriteLine(line);
            OnEventReceived?.Invoke(name, args);
        }
    }
}
