using System;
using Gtk;
using System.IO.Ports;
using System.Threading;

public partial class MainWindow : Gtk.Window
{
    Thread _serialUsbThread;
    SerialPort _serialPort;
    byte[] _rx_buff;
    volatile int _rx_head;
    volatile int _rx_tail;

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        PopulateComList();
        _rx_buff = new byte[4096];
        _rx_head = 0;
        _rx_tail = 0;
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnButton1Pressed(object sender, EventArgs e)
    {
        _serialPort = new SerialPort(combobox2.ActiveText, 115200, Parity.None, 8, StopBits.One);
        _serialPort.ReadBufferSize = 4096;
        try
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }

            if (_serialPort.IsOpen)
            {
                _serialUsbThread = new Thread(SerialWorker)
                {
                    Priority = ThreadPriority.Normal,
                    IsBackground = true
                };
                _serialUsbThread.Start();
            }
        }
        catch (Exception ex)
        {
            image_viewer.utils.ShowMessage(this, "Error", $"{ex}");
        }
    }

    private void SerialWorker(object obj)
    {
        while (_serialPort.IsOpen)
        {
            try
            {
                while (_serialPort.BytesToRead > 0)
                {
                    int _next_head = (_rx_head + 1 >= _rx_buff.Length) ? 0 : _rx_head + 1;
                    if (_next_head == _rx_tail)
                    {
                        //error buffer full or wait until tail is reset?
                        break;
                    }
                    else
                    {
                        _serialPort.Read(_rx_buff, _rx_head, 1);
                        _rx_head = _next_head;
                    }
                }

                if (_rx_head == _rx_tail)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    Gtk.Application.Invoke((sender, e) =>
                    {
                        string str = "";
                        do
                        {
                            str = string.Concat(str, System.Text.Encoding.UTF8.GetString(_rx_buff, _rx_tail, 1));
                            _rx_tail = (_rx_tail + 1 >= _rx_buff.Length) ? 0 : _rx_tail + 1;
                        } while (_rx_tail != _rx_head);

                        label1.Text = str;

                    });
                }
            }
            catch(Exception ex)
            {
                image_viewer.utils.ShowMessage(this, "Error", $"{ex}");
            }
        }
    }

    private void PopulateComList()
    {
        string[] sp_list = SerialPort.GetPortNames();
        foreach (string name in sp_list)
        {
            if (name.Contains("USB"))
            {
                combobox2.InsertText(0, name);
            }
        }
    }
}