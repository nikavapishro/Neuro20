#define SOUNDPLAYENABLE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.Filters;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using System.Linq;
using System.Windows.Media;
using System.Windows.Interop;
using NAudio.Wave;

namespace SciChartExamlpeOne
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    // Create the chart surface

    public enum FilterTypes
    {
        Buffer,
        HighPass,
        LowPass,
        BandPass,
        MovingAverage,
        Spline,
        Notch,
        FIRLowPass,
        FIRHighPass,
        FIRBandPass,
        FIRLP_MA,
        FIRBP_MA
    }

    public class TimeIndex {
        private decimal _timeindex;
        private decimal _dataindex;
        private decimal _smplerate;
        private decimal _periode;
        private const decimal _scale = 1000;    //convert to ms
        private decimal _timediv;

        public TimeIndex()
        {
            _dataindex = 0;
            _timeindex = 0;
            _smplerate = (decimal)Properties.Settings.Default.SAMPLERATE;
            _periode = 1.0M / (decimal)_smplerate;
            _timediv = 1.0M;
        }
        public void Increment() {
            _dataindex += 1;
            _timeindex += _periode * _scale;
        }

        public int getIndex_int() {
            return (int) _dataindex;
        }

        public double getIndex_double()
        {
            return _timeindex.ToDouble();
        }

        public void setScale(double divTime)
        {
            _timediv = (decimal) divTime;
        }

        public int getVisibleRange_int()
        {
            return (int)(_timediv * _smplerate);
        }

        public double getVisibleRange_double()
        {
            return (_timediv * _scale).ToDouble() ;
        }
    }

    public partial class MainWindow : Window
    {
        #region variables
        public decimal _adc_ConvertNum2Value;

        public IntPtr Handle;

        SerialPort _com_serial = new SerialPort();
        private DispatcherTimer _com_connecttimer;
        bool _com_bConnectionStatus = false;
        public bool _com_isBusy = false;
        string _com_recieved_data;
        bool _com_online = false;
        static StringBuilder _com_bufReceiedData = new StringBuilder() ;
        public ConcurrentQueue<Int32> nDataPure = new ConcurrentQueue<Int32>() ;

        #region Sound Player
        private BufferedWaveProvider _snd_bufferedWaveProvider;
        private WaveOut _snd_player;
        private bool _snd_isPlaying;
        #endregion

        double nTotDataRecieved = 0;
        double nTotalOnlineTime = 0;
        double nTotPureDataReceied = 0;
        long nTotPacketRecieved = 0;
        public double nTimeDataRange ;

        int nIdxLowPassCombo = (int)Properties.Settings.Default.LOWCUTINDEX;
        int nIdxHighPassCombo = (int)Properties.Settings.Default.HIGHCUTINDEX;

        private DispatcherTimer _sci_timer;
        private XyDataSeries<double, double> _originalData;
        //private XyDataSeries<double, double> _filteredData;
        private FilterData _filterData;
        private TimeIndex _sci_timeIndex;

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            InitComponents();
            this.Loaded += onLoaded;
            _com_bConnectionStatus = false;
            foreach (string strComName in SerialPort.GetPortNames()) {
                Comm_Port_Names.Items.Add(strComName);
            }
            Comm_Port_Names.SelectedIndex = 0;
            WindowState = WindowState.Maximized;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            InitSoundPlayer(Properties.Settings.Default.SAMPLERATE, 1, 1);
            _adc_ConvertNum2Value = GetAdcCoef();
        }

        #region Initialize Components
        private void InitComponents()
        {
            this.Title = "Neuro 2014 EMG Test Suit";
            lblComState.Text = "Disconnected";
            _snd_isPlaying = false;
            _sci_timeIndex = new TimeIndex();
            cmbLowPassFilter.SelectedIndex = nIdxLowPassCombo;
            cmbHighPassFilter.SelectedIndex = nIdxHighPassCombo;
            numVoltDiv.SetValue(Properties.Settings.Default.VOLTDIV);
            numTimeDiv.SetValue(Properties.Settings.Default.TIMEDIV);
            _sci_timeIndex.setScale(numTimeDiv.Value.ToDouble());
            _com_connecttimer = new DispatcherTimer(DispatcherPriority.Normal);
            _com_connecttimer.Interval = TimeSpan.FromMilliseconds(1000.0);
            _com_connecttimer.Tick += RefreshConnection;
            _com_connecttimer.Start();
        }

        #endregion

#region SciChart
        private void onLoaded(object sender, RoutedEventArgs routedEventArgs) {
            // Instantiate the ViewportManager here
            //sciChartSurface.ViewportManager = new ScrollingViewportManager(nTimeDataRange);

            // Create XyDataSeries to host data for our charts
            _originalData = new XyDataSeries<double, double>();
            //_originalData.FifoCapacity = _sci_timeIndex.getVisibleRange_int() ;
            
            //var _filteredData = new CustomFilter(_originalData);
            //_filterData = new FilterData(FilterTypes.LowPass);
            _filterData = new FilterData(FilterTypes.FIRBP_MA, Properties.Settings.Default.SAMPLERATE,
                 Constants.FIR_HIGHPASS_ORDER, cmbHighPassFilter.SelectedValue.ToString().ToDouble(),
                 Constants.FIR_LOWPASS_ORDER, cmbLowPassFilter.SelectedValue.ToString().ToDouble());
            _filterData.ResetFilter(FilterTypes.Notch, Properties.Settings.Default.SAMPLERATE, Constants.FIR_NOTCH, 50.0);
            _filterData.isNotchEnable = (bool)cbxNotch.IsChecked;

            _originalData.SeriesName = "CHANNEL 1";
            //_filteredData.FilteredDataSeries.SeriesName = "Moving Average";
            _filterData._DataSeries.SeriesName = "Filterd";

            //_originalData.FifoCapacity = nTimeDataRange;
            //lineData.FifoCapacity = 1000 ;

            // Assign dataseries to RenderSeries
            LineSeries.DataSeries = _originalData;
            //FilteredSeries.DataSeries = _filteredData.FilteredDataSeries ;
            FilteredSeries.DataSeries = _filterData._DataSeries;

            _sci_timer = new DispatcherTimer(DispatcherPriority.Render);
            _sci_timer.Interval = TimeSpan.FromMilliseconds((1000.0M / Properties.Settings.Default.RREFRESHFPS).ToDouble());
            _sci_timer.Tick += TimerElapsed;
            _sci_timer.Start();
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            // SuspendUpdates() ensures the chart is frozen
            // while you do updates. This ensures best performance
            int nLen = nDataPure.Count - Constants.BUFLENCHECK;
            if (nLen <= 0)
                return;

            int idx = 0;
            byte[] data = new byte[nLen];
            using (_originalData.SuspendUpdates())
            {
                while (nDataPure.Count > Constants.BUFLENCHECK)
                {
                    // Append a new data point;
                    Int32 localvalue;
                    if (nDataPure.TryDequeue(out localvalue))
                    {
                        data[idx] = (byte) localvalue;
                        idx++;
                        _originalData.Append(_sci_timeIndex.getIndex_double(), (double)localvalue / (double) _adc_ConvertNum2Value.ToDouble());
                        _filterData.Append(_sci_timeIndex.getIndex_double(), (double)localvalue / _adc_ConvertNum2Value.ToDouble());
                        _sci_timeIndex.Increment();
                    }
                    //scatterData.Append(i, Math.Cos(i * 0.1));
                }
                // Set VisibleRange to last 1,000 points
                sciChartSurface.XAxis.VisibleRange = new DoubleRange(_sci_timeIndex.getIndex_double() - _sci_timeIndex.getVisibleRange_double(), _sci_timeIndex.getIndex_double());
            }

#if SOUNDPLAYENABLE
            if(_snd_isPlaying)
            {
                _snd_bufferedWaveProvider.AddSamples(data, 0, nLen);
            }
#endif
        }

        private void numVoltDiv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            decimal nHighLimit = numVoltDiv.Value; // * 1000 ;
            sciChartSurface.YAxis.VisibleRange = new DoubleRange(-nHighLimit.ToDouble()/2.0, nHighLimit.ToDouble()/2.0);
            
            if ((bool)cbxAutoGain.IsChecked)
                if (_com_serial.IsOpen)
                {
                    int G1, G2;
                    CalcGains(out G1, out G2);
                    SendCommand(Constants.CMD_GAIN, G1, G2);
                    _adc_ConvertNum2Value = GetAdcCoef();
                }
        }

        private decimal CalcGains(out int nG1, out int nG2) {
            decimal MinG = Properties.Settings.Default.HWGAIN ;
            decimal MaxG = MinG * 10000;

            decimal vdiv = numVoltDiv.Value ;
            decimal G = Properties.Settings.Default.ADCREF / vdiv ;
            decimal G1, G2;
            G1 = 0;
            G2 = 0;

            if (G > MaxG)
                G = MaxG;
            if (G < MinG)
                G = MinG;

            G /= Properties.Settings.Default.HWGAIN ;


            G1 = (decimal) Math.Sqrt(G.ToDouble());

            int n1, n2;
            
            n1 = (int) Math.Round(-256.0104M / G1 + 256.0052M, 0);
            if (n1 < 0)
                n1 = 0;
            else if (n1 > 255)
                n1 = 255;
            G1 = 1.0M + ((decimal)n1 + 52.0M / 10e3M) / (256.0M - (decimal) n1 + 52.0M / 10e3M);

            G2 = G / G1;
            n2 = (int)Math.Round(256.0104M / G2 - 0.0052M, 0);
            if (n2 < 0)
                n2 = 0;
            else if (n2 > 255)
                n2 = 255;
            G2 = 1.0M + (256.0M - (decimal) n2 + 52.0M / 10e3M) / ((decimal)n2 + 52.0M / 10e3M);

            nG1 = n1;
            nG2 = n2;

            return G1 * G2 * Properties.Settings.Default.HWGAIN / Properties.Settings.Default.SWGAIN;
        }

        private decimal GetAdcCoef() {
            return CalcGains(out _, out _) * (decimal) ( 1 << Properties.Settings.Default.ADCBITNUM ) / (decimal) Properties.Settings.Default.ADCREF ;
        }

        private void numTimeDiv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _sci_timeIndex.setScale(numTimeDiv.Value.ToDouble());
            //sciChartSurface.ViewportManager = new ScrollingViewportManager(nTimeDataRange);
        }

        private void cmbFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!cmbLowPassFilter.IsLoaded)
                return;

            if (!cmbHighPassFilter.IsLoaded)
                return;

            if (Double.TryParse(cmbLowPassFilter.SelectedValue.ToString(), out double valH))
                if (Double.TryParse(cmbHighPassFilter.SelectedValue.ToString(), out double valL))
                    _filterData.ResetFilter(FilterTypes.FIRBP_MA, Properties.Settings.Default.SAMPLERATE,
                        Constants.FIR_HIGHPASS_ORDER, valL,
                        Constants.FIR_LOWPASS_ORDER, valH);
        }

        private void cbxNotch_Change(object sender, RoutedEventArgs e)
        {
            _filterData.isNotchEnable = (bool) cbxNotch.IsChecked;
        }

#endregion

#region Sound Play

        private void Command_btn_Click_Sound(object sender, RoutedEventArgs e)
        {
            int nLen = 4800;
            byte[] data = new byte[nLen];
            for (int i = 0; i < nLen; i++)
                data[i] = (byte)(Math.Cos(2.0 * Math.PI * (double)i / (double)nLen * 5.0) * 255.0);
            //SoundPlayer.WriteSamples(data, nLen);
            //mainInit();
        }

        private void InitSoundPlayer(int _SmplRate, int nNoChannels, int nBytePerSample)
        {
            int _AvgBytesPerSec = nNoChannels * _SmplRate * nBytePerSample;
            int _BlockAlign = nNoChannels * nBytePerSample;

            WaveFormat _format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,
                    _SmplRate, nNoChannels, _AvgBytesPerSec, _BlockAlign, (nBytePerSample * 8));

            // set up our signal chain
            _snd_bufferedWaveProvider = new BufferedWaveProvider(_format);

            // set up playback
            _snd_player = new WaveOut();
            _snd_player.Init(_snd_bufferedWaveProvider);

            StartSoundServices();
        }

        private void StartSoundServices()
        {
            _snd_player.Play();
            _snd_isPlaying = true;
        }

        private void StopSoundServices()
        {
            _snd_isPlaying = false;
            _snd_player.Stop();
        }

        //For use when 24bit data sampling is achieved
        private void ConvertToWave(Int32[] samples, int nBitsPerData, int nBitsPerSample, out byte[] byteBuffer)
        {
            UInt32 sample4Byte;
            int nShifts = 1 << (sizeof(Int32) * 8 - nBitsPerData);
            byteBuffer = new byte[samples.Length * (nBitsPerSample / 8)];
            for (uint i = 0; i < samples.Length; i++)
            {
                Int32 intSample = (Int32)(samples[i] * nShifts);
                sample4Byte = (UInt32)samples[i];
                uint byteBufIndex = 0;

                switch (nBitsPerSample)
                {
                    case 24 :
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 8);
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 16);
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 24);
                        break;
                    case 16 :
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 16);
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 24);
                        break;
                    case 8:
                        byteBuffer[byteBufIndex++] = (byte)(sample4Byte >> 24);
                        break;
                }
            }
        }

        #endregion

#region COM CONNECTION
        private void Connect_Comm(object sender, RoutedEventArgs e)
        { 
            if (_com_bConnectionStatus == false)
            {
                //Sets up serial port
                _com_serial.PortName = Comm_Port_Names.Text;
                _com_serial.BaudRate = Convert.ToInt32(Baud_Rates.Text);
                _com_serial.Handshake = System.IO.Ports.Handshake.None;
                _com_serial.Parity = Parity.None;
                _com_serial.DataBits = 8;
                _com_serial.StopBits = StopBits.Two;
                _com_serial.ReadTimeout = 200;
                _com_serial.WriteTimeout = 50;
                _com_serial.Open();
                SendCommand(Constants.CMD_LIVE);

                //Sets button State and Creates function call on data recieved
                Connect_btn.Content = "Disconnect";
                _com_bConnectionStatus = true;
                lblComState.Text = "COM Port Connected on Port " + Comm_Port_Names.Text;
                _com_serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);

            }
            else
            {
                try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
                {
                    SendCommand(Constants.CMD_STOP);
                    ComPortClose();
                    Connect_btn.Content = "Connect";
                    lblComState.Text = "Disconnected";
                    _com_bConnectionStatus = false;
                }
                catch
                {
                }
            }
        }
        public bool ComPortClose() {
            if (_com_serial.IsOpen) {
                Thread CloseDown = new Thread(new ThreadStart(CloseSerialThread));
                CloseDown.Start();
            }
            return true;
        }
        private void CloseSerialThread()
        {
            try { _com_serial.Close(); }
            catch(Exception ex) { lblComState.Text = ex.Message; }
            //if (bIsCloseProgram) Dispatcher.Invoke(new EventHandler(NowClose)) ;
        }

#region Receive Data

        private delegate void UpdateUiTextDelegate(string text);
        private void Recieve(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_com_serial.IsOpen)
                {
                    // Collecting the characters received to our 'buffer' (string).
                    int nLen = _com_serial.BytesToRead;
                    byte[] dataBuffer = new byte[nLen];
                    _com_serial.Read(dataBuffer, 0, nLen);
                    var encoding = Encoding.GetEncoding("iso-8859-1");
                    _com_recieved_data = encoding.GetString(dataBuffer);
                    nTotDataRecieved += _com_recieved_data.Length;
                    _com_online = true;
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), _com_recieved_data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                //put other, more interesting error handling here.
            }
            //Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), _com_recieved_data);
        }

        private void WriteData(string strData)
        {
            _com_bufReceiedData.Append(strData);

            string bufferString = _com_bufReceiedData.ToString();
            int index = 0;
            while ((index != -1) & (bufferString.Length > Constants.BUFLENCHECK))
            {
                index = bufferString.IndexOf(Constants.HDRSTR);
                if (index > -1)
                {
                    nTotPacketRecieved++;
                    nTotPureDataReceied += Constants.FRMLEN;
                    for (int i=0; i<Constants.FRMLEN; i++)
                    {
                        int idx = i + index + Constants.HDRLEN ;
                        int value = Convert.ToInt32(bufferString[idx]);
                        nDataPure.Enqueue(value);
                    }
                    bufferString = bufferString.Remove(0, index + Constants.PACKETLEN);
                }
            }
            _com_bufReceiedData = new StringBuilder(bufferString);

            //lblComDataReceived.Text = nTotPacketRecieved.ToString();   //nDataPure.Count.ToString();
        }

#endregion

#region Send Data
        private void SendCommand(Byte Cmd, Int32 _param1=0, Int32 _param2=0, Int32 _param3=0, Int32 _param4=0) {

            if (_com_serial.IsOpen)
            {
                try
                {
                    Command CMD = new Command();
                    CMD.Start = 0xE5;
                    CMD.Type = Cmd;
                    CMD.nType = (byte) ~CMD.Type;
                    CMD.CheckSum = 0;
                    CMD.Param1 = _param1;
                    CMD.Param2 = _param2;
                    CMD.Param3 = _param3;
                    CMD.Param4 = _param4;
                    CMD.CheckSum = 0;

                    _com_serial.Write(CMD.getCommand(), 0, CMD.GetSize());
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
            }
        }
        
        private void Command_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_com_online)
            {
                SendCommand(Constants.CMD_STOP, 10, 10);
                _com_online = false;
            }
            else
                SendCommand(Constants.CMD_LIVE, 10, 10);
            //SendCommand(Constants.CMD_CURRENT, 10, 10);

        }

        private void RefreshConnection(object sender, EventArgs e)
        {
            _com_connecttimer.Stop();
            if (_com_online == false)
            {
                lblComState.Text = "Device offline";
                lblComState.Foreground = Brushes.Red;
                //refresh connection
            }
            else
            {
                _com_online = false;
                lblComState.Text = "Device Online";
                lblComState.Foreground = Brushes.Black;
                if (nTotalOnlineTime == 0)
                {
                    nTotPureDataReceied = 0;
                    nTotalOnlineTime = 1;
                }
                else
                {
                    nTotalOnlineTime += _com_connecttimer.Interval.TotalMilliseconds.ToDouble();
                    lblComDataReceived.Text = ((double)nTotPureDataReceied / (double)nTotalOnlineTime).ToString("0.##");
                }
            }
                
            _com_connecttimer.Interval = TimeSpan.FromMilliseconds(1000.0);
            _com_connecttimer.Start();
        }

#endregion

#endregion

#region Closing Routine

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Settings
            Properties.Settings.Default.LOWCUTINDEX = (int)cmbLowPassFilter.SelectedIndex;
            Properties.Settings.Default.HIGHCUTINDEX = (int)cmbHighPassFilter.SelectedIndex;
            Properties.Settings.Default.VOLTDIV = (decimal)numVoltDiv.Value;
            Properties.Settings.Default.TIMEDIV = (decimal)numTimeDiv.Value;
            Properties.Settings.Default.Save(); // Saves settings in application configuration file

            _sci_timer.Stop();
            //Escape Handles
            if (_com_serial.IsOpen)
            {
                ComPortClose();
            }
            StopSoundServices();
        }

        #endregion

    }
}
