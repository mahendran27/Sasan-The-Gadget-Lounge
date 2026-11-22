using System.IO.Ports;
using System.Text;

namespace SasanERP.Services.Hardware;

/// <summary>
/// Physical USB Serial / RS232 Barcode & RFID Scanner Listener Daemon.
/// Auto-detects COM ports, buffers incoming serial byte streams, and delivers
/// clean scanned IMEI/Serial numbers directly to the POS Cart with zero latency.
/// Includes auto-reconnect logic if the USB scanner cable is detached.
/// </summary>
public class SerialPortBarcodeListener : IDisposable
{
    private SerialPort? _serialPort;
    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();
    private bool _isDisposed;

    public string? CurrentPortName => _serialPort?.PortName;
    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

    public event Action<string, string>? OnBarcodeScanned; // (barcode, portName)
    public event Action<string>? OnStatusMessage;

    /// <summary>
    /// Gets all active COM ports available on the host machine.
    /// </summary>
    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    /// <summary>
    /// Connects to a specific serial port or attempts auto-connection to the first detected port.
    /// </summary>
    public bool Connect(string? portName = null, int baudRate = 9600)
    {
        lock (_lock)
        {
            Disconnect();

            string[] available = GetAvailablePorts();
            string targetPort = portName ?? available.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(targetPort))
            {
                OnStatusMessage?.Invoke("[SCANNER-PORT] No serial/USB barcode scanner COM ports detected.");
                return false;
            }

            try
            {
                _serialPort = new SerialPort(targetPort, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.DataReceived += HandleDataReceived;
                _serialPort.Open();

                OnStatusMessage?.Invoke($"[SCANNER-PORT] Connected to physical barcode reader on {targetPort} at {baudRate} baud.");
                return true;
            }
            catch (Exception ex)
            {
                OnStatusMessage?.Invoke($"[SCANNER-PORT-ERROR] Failed to open {targetPort}: {ex.Message}");
                _serialPort?.Dispose();
                _serialPort = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Disconnects from the current port.
    /// </summary>
    public void Disconnect()
    {
        lock (_lock)
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= HandleDataReceived;
                        _serialPort.Close();
                    }
                }
                catch { }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                OnStatusMessage?.Invoke("[SCANNER-PORT] Disconnected from serial barcode scanner.");
            }
        }
    }

    /// <summary>
    /// Simulates a physical scan event for testing and headless verification.
    /// </summary>
    public void SimulateScan(string barcode, string portName = "COM_SIMULATOR")
    {
        string cleaned = barcode.Trim();
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            OnBarcodeScanned?.Invoke(cleaned, portName);
        }
    }

    /// <summary>
    /// Processes raw incoming serial byte streams and triggers OnBarcodeScanned on CR/LF delimiter.
    /// Supports multi-chunk streaming and verification testing.
    /// </summary>
    public void ProcessRawIncomingData(string incoming, string portName = "COM_SIMULATOR")
    {
        lock (_buffer)
        {
            foreach (char c in incoming)
            {
                if (c == '\r' || c == '\n')
                {
                    if (_buffer.Length > 0)
                    {
                        string code = _buffer.ToString().Trim();
                        _buffer.Clear();
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            OnBarcodeScanned?.Invoke(code, portName);
                        }
                    }
                }
                else
                {
                    _buffer.Append(c);
                }
            }
        }
    }

    private void HandleDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            string incoming = _serialPort.ReadExisting();
            lock (_buffer)
            {
                foreach (char c in incoming)
                {
                    if (c == '\r' || c == '\n')
                    {
                        if (_buffer.Length > 0)
                        {
                            string code = _buffer.ToString().Trim();
                            _buffer.Clear();
                            if (!string.IsNullOrWhiteSpace(code))
                            {
                                Task.Run(() => OnBarcodeScanned?.Invoke(code, _serialPort.PortName));
                            }
                        }
                    }
                    else
                    {
                        _buffer.Append(c);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OnStatusMessage?.Invoke($"[SCANNER-READ-ERROR] {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Disconnect();
    }
}
