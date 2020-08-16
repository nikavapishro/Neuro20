#define SOUNDPLAYENABLE
#define ISUPGRADING

using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SciChart.Charting.Model.DataSeries;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using System.Windows.Media;
using NAudio.Wave;
using System.Windows.Input;
using FontAwesome.WPF;
using System.Reflection;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.IO;

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

        public void Clear()
        {
            _dataindex = 0;
            _timeindex = 0;
        }

        public decimal ConvertToTime(int index)
        {
            return (decimal)index * _periode * _scale;
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

        #region TCP connections
        private TcpClient _tcp_tcpClient;
        private delegate void tcpUpdateLogCallBack(string strMessage);
        Server _tcp_myserver;
        Thread _tcp_tServerListener;
        public bool _tcp_bTCPActivate = false;
        DispatcherTimer _tcp_tmHello;
        #endregion

        #region save holders
        private int _save_RefreshValue;
        private bool _save_SweepGraph;
        private string _save_ComportName;
        private int _save_ComportBaud;
        private int _save_SoundLatencyMargin;
        private int _save_AdcBitNum;
        private int _save_HardWaveVersion;
        private string _save_N20ServerIP;
        private int _save_N20ServerPort;
        private int _save_N14ServerPort;
        private string _save_N14ServerIP;
        #endregion

        #region Com port
        SerialPort _com_serial = new SerialPort();
        private DispatcherTimer _com_connecttimer;
        bool _com_bConnectionStatus = false;
        public bool _com_isBusy = false;
        string _com_recieved_data;
        bool _com_online = false;
        bool _com_FreezeChange = false;
        static StringBuilder _com_bufReceiedData = new StringBuilder() ;
        public ConcurrentQueue<Int32> nDataPure = new ConcurrentQueue<Int32>() ;
        #endregion

        #region Sound Player
        private BufferedWaveProvider _snd_bufferedWaveProvider;
        public ConcurrentQueue<Int32> nSoundPure = new ConcurrentQueue<Int32>();
        private DispatcherTimer _snd_playtimer;
        private WaveOut _snd_player;
        //private AsioOut _snd_AsioPlayer;
        private bool _snd_isPlaying;
        #endregion

        double nTotDataRecieved = 0;
        double nTotalOnlineTime = 0;
        double nTotPureDataReceied = 0;
        long nTotPacketRecieved = 0;
        public double nTimeDataRange ;

        int nIdxLowPassCombo = (int)Properties.Settings.Default.LOWCUTINDEX;
        int nIdxHighPassCombo = (int)Properties.Settings.Default.HIGHCUTINDEX;

        private bool isDrawingSignal;
        private DispatcherTimer _sci_timer;
        private XyDataSeries<double, double> _originalData;
        private FilterData _filterData;
        private TimeIndex _sci_timeIndex;
        private bool bResetGraph = false;
        private int nChannelNumber;

        #endregion


        public MainWindow()
        {
            DataContext = new BorderViewModel();
            InitializeComponent();
            LoadSettings();
            InitComponents();
            InitTCPConnection();
            StateChanged += MainWindowStateChangeRaised;
            LoadSettings();
            this.Loaded += onLoaded;
            setPage.Loaded += SetPage_Loaded;
            _com_bConnectionStatus = false;
            WindowState = _tcp_bTCPActivate ? WindowState.Minimized : WindowState.Maximized;
            MainWindowStateChangeRaised(null,null);
        }

        private void SetPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettingContent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSoundPlayer(Properties.Settings.Default.SAMPLERATE, 1, 1);
            cbxSound_Change(null, null);
            _adc_ConvertNum2Value = GetAdcCoef();
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        #region Windows Chrome
        // Can execute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            if(!_tcp_bTCPActivate)
                SystemCommands.CloseWindow(this);
        }

        // State change
        private void MainWindowStateChangeRaised(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness(8);
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness(0);
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Initialize Components
        private void InitComponents()
        {
            this.Title = "Neuro 2014 EMG Test Suit";
            strTitle.Text = "Neuro 2014 EMG Test Suit";
            lblComState.Text = "Disconnected";
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == Constants.NERO14SALUT)
                {
                    _tcp_bTCPActivate = true;
                    lblTCPStatus.Text = "Neuro 14 Connected!";
                    CloseButton.Visibility = Visibility.Hidden;
                }
            }
            btnSwitch2N14.Visibility = _tcp_bTCPActivate == true ? Visibility.Visible : Visibility.Hidden;
            rbChannelTwo.IsChecked = true;
            nChannelNumber = 2;
            isDrawingSignal = false;
            cbxSound.IsChecked = true;
            cbxNotch.IsChecked = true;
            _snd_isPlaying = false;
            _sci_timeIndex = new TimeIndex();
            cmbLowPassFilter.SelectedIndex = nIdxLowPassCombo;
            cmbHighPassFilter.SelectedIndex = nIdxHighPassCombo;
            numVoltDiv.nIsVoltDiv = 1;
            numVoltDiv.SetValue(Properties.Settings.Default.VOLTDIV);
            numTimeDiv.SetValue(Properties.Settings.Default.TIMEDIV);
            _sci_timeIndex.setScale(numTimeDiv.Value.ToDouble());
            _com_connecttimer = new DispatcherTimer(DispatcherPriority.Normal);
            _com_connecttimer.Interval = TimeSpan.FromMilliseconds(1000.0);
            _com_connecttimer.Tick += RefreshConnection;
            if (_tcp_bTCPActivate)
                _com_connecttimer.Stop();
            else
                _com_connecttimer.Start();
            _snd_playtimer = new DispatcherTimer(DispatcherPriority.Normal);
            _snd_playtimer.Interval = TimeSpan.FromMilliseconds(10.0);
            _snd_playtimer.Tick += SoundPlayTimer;
            _snd_playtimer.Start();
            setPage.btnShowHide.Click += SettingVisibilityChange;
            setPage.btnConnect.Click += Connect_Comm;
            setPage.btnCommand.Click += Command_btn_Click_Sound;
        }

        private void numVoltDiv_Loaded(object sender, RoutedEventArgs e)
        {
            numVoltDiv.nIsVoltDiv = 1;
        }

        #endregion

        #region TCP connection
        private void InitTCPConnection()
        {
            _tcp_tmHello = new DispatcherTimer();
            _tcp_tmHello.Interval = TimeSpan.FromMilliseconds(Constants.NEURO14SALUTINTERVAL);
            _tcp_tmHello.Tick += _tcp_SayHello;
            //Connection Formation NO2
            //_tcp_tmHello.Start();
            _tcp_tmHello.Stop();
            _tcp_myserver = new Server(_save_N20ServerIP, _save_N20ServerPort);
            _tcp_myserver.UpdateConnection += ChangeConnectionState;
            _tcp_tServerListener = new Thread(delegate () { _tcp_myserver.StartListener(); });
            _tcp_tServerListener.Start();

            lblTCPStatus.Text = "Server Started!";
        }

        public void ChangeConnectionState(Object Sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_tcp_myserver.bChangeComRequest == true) { 
                    _tcp_myserver.bChangeComRequest = false;
                    if (_tcp_myserver.nSW2Connection == 1)
                        _com_bConnectionStatus = false;
                    else
                        _com_bConnectionStatus = true;
                    Connect_Comm(null, null);
                    rbChannel_Checked(null, null);
                }
                if (_tcp_myserver.bCloseRequest == true)
                {
                    _tcp_myserver.bCloseRequest = false;
                    _tcp_CloseRequest();
                }
                if (_tcp_myserver.bChangeSizeRequest == true)
                {
                    _tcp_myserver.bChangeSizeRequest = false;
                    Application.Current.MainWindow.WindowState = _tcp_myserver.nWindowsState == 0 ? WindowState.Minimized : WindowState.Maximized;
                    Topmost = _tcp_myserver.nWindowsState == 0 ? false : true;
                    
                    //Disabled for Communication Method 2
                    //if (_tcp_myserver.nWindowsState == 0)
                    //Hide();
                    //else
                    //Show();
                }
            });
        }

        private void _tcp_CloseRequest()
        {
            Close();
        }

        private void _tcp_SayHello(object sender, EventArgs e)
        {
            if (!_tcp_bTCPActivate)
                return;

            _tcp_tcpClient = new TcpClient();
            try
            {
                _tcp_tcpClient.Connect(IPAddress.Parse(_save_N14ServerIP), _save_N14ServerPort);
                string strMessage = Constants.NERO20SALUT;
                //lbxStatus.Items.Add("HI. I AM NEURO 20!");
                Stream stm = _tcp_tcpClient.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ln = asen.GetBytes(strMessage.Length.ToString());
                byte[] ba = asen.GetBytes(strMessage);
                stm.WriteByte((byte)(strMessage.Length / 256));
                stm.WriteByte((byte)(strMessage.Length % 256));
                stm.Write(ba, 0, ba.Length);
                _tcp_tcpClient.Close();
            }
            catch (Exception exp)
            {
                //
            }
        }

        private void SwithcToN14SW(object sender, RoutedEventArgs e)
        {
            icoPlayPause.Icon = FontAwesomeIcon.PlayCircleOutline ;
            isDrawingSignal = false ;

            var scope = FocusManager.GetFocusScope(btnSwitch2N14); // elem is the UIElement to unfocus
            FocusManager.SetFocusedElement(scope, null); // remove logical focus
            Keyboard.ClearFocus(); // remove keyboard focus

            //Disabled for debugging
            //Hide();

            WindowState = WindowState.Minimized;
            _com_bConnectionStatus = true;
            Connect_Comm(null, null);
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

            _originalData.SeriesName = "Pure Data";
            
            //_filteredData.FilteredDataSeries.SeriesName = "Moving Average";
            _filterData._DataSeries.SeriesName = "Filterd Data";

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
                if (bResetGraph)
                {
                    _sci_timeIndex.Clear();
                    _originalData.Clear();
                    _filterData.Clear();
                    bResetGraph = false;
                }
                while (nDataPure.Count > Constants.BUFLENCHECK)
                {
                    Int32 localvalue;
                    if (nDataPure.TryDequeue(out localvalue))
                    {
                        data[idx] = (byte) localvalue;
                        idx++;

                        #region Sweeping Graph
                        if (_save_SweepGraph) 
                        {
                            //double _index = (double)_sci_timeIndex.ConvertToTime(_sci_timeIndex.getIndex_int() % _sci_timeIndex.getVisibleRange_int());
                            double _index = (int)(_sci_timeIndex.getIndex_int() % _sci_timeIndex.getVisibleRange_int());
                            if ((_originalData.Count < _sci_timeIndex.getVisibleRange_int()) && (_index >= _originalData.Count))
                            {
                                _originalData.Append(_index, (double)localvalue / (double)_adc_ConvertNum2Value.ToDouble());
                                _filterData.Append(_index, (double)localvalue / _adc_ConvertNum2Value.ToDouble());
                            }
                            else
                            {
                                _originalData.Update(_index, (double)localvalue / (double)_adc_ConvertNum2Value.ToDouble());
                                _filterData.Update(_index, (double)localvalue / _adc_ConvertNum2Value.ToDouble());
                            }
                        }
                        #endregion

                        #region Normal Graph
                        if (!_save_SweepGraph)
                        {
                            _originalData.Append(_sci_timeIndex.getIndex_double(), (double)localvalue / (double)_adc_ConvertNum2Value.ToDouble());
                            _filterData.Append(_sci_timeIndex.getIndex_double(), (double)localvalue / _adc_ConvertNum2Value.ToDouble());
                        }
                        #endregion

                        _sci_timeIndex.Increment();
                    }
                }
                if (_save_SweepGraph) 
                    sciChartSurface.XAxis.VisibleRange = new DoubleRange(0, _sci_timeIndex.getVisibleRange_int());
                else 
                    sciChartSurface.XAxis.VisibleRange = new DoubleRange(_sci_timeIndex.getIndex_double() - _sci_timeIndex.getVisibleRange_double(), _sci_timeIndex.getIndex_double());
            }

#if SOUNDPLAYENABLE
            //if (_snd_isPlaying)
            //{
            //    _snd_bufferedWaveProvider.AddSamples(data, 0, nLen);
            //}
#endif
        }

        private void numVoltDiv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            decimal nHighLimit = numVoltDiv.Value; // * 1000 ;
            sciChartSurface.YAxis.VisibleRange = new DoubleRange(-nHighLimit.ToDouble()/2.0, nHighLimit.ToDouble()/2.0);
            
            if ((bool)setPage.cbxAutoGain.IsChecked)
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
            if (!cbxNotch.IsLoaded)
                return;
            _filterData.isNotchEnable = (bool)cbxNotch.IsChecked;
        }

        #endregion

        #region Sound Play

        private void Command_btn_Click_Sound(object sender, RoutedEventArgs e)
        {
            //int nLen = 4800;
            //byte[] data = new byte[nLen];
            //for (int i = 0; i < nLen; i++)
            //    data[i] = (byte)(Math.Cos(2.0 * Math.PI * (double)i / (double)nLen * 5.0) * 255.0);
            //SoundPlayer.WriteSamples(data, nLen);
            //mainInit();
            SetChannel(-1); //Reverse Channels
        }
        private void cbxSound_Change(object sender, RoutedEventArgs e)
        {
            if (!cbxSound.IsLoaded)
                return;

            if ((bool)cbxSound.IsChecked)
                StartSoundServices();
            else
                StopSoundServices();
        }

        private void InitSoundPlayer(int _SmplRate, int nNoChannels, int nBytePerSample)
        {
            int _AvgBytesPerSec = nNoChannels * _SmplRate * nBytePerSample;
            int _BlockAlign = nNoChannels * nBytePerSample;

            WaveFormat _format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,
                    _SmplRate, nNoChannels, _AvgBytesPerSec, _BlockAlign, (nBytePerSample * 8));

            // set up our signal chain
            _snd_bufferedWaveProvider = new BufferedWaveProvider(_format);

            //// set up playback
            _snd_player = new WaveOut();
            _snd_player.Init(_snd_bufferedWaveProvider);
            _snd_player.DesiredLatency = 10 ;

            //_snd_AsioPlayer = new AsioOut();
            //_snd_AsioPlayer.Init(_snd_bufferedWaveProvider);

            StartSoundServices(); //--> Will Start when we grant it using _snd_isPlaying
        }

        private void StartSoundServices()
        {
            _snd_bufferedWaveProvider.ClearBuffer();
            _snd_player.Play();
            //_snd_AsioPlayer.Play();
            _snd_isPlaying = true;
        }

        private void StopSoundServices()
        {
            _snd_isPlaying = false;
            _snd_player.Stop();
            //_snd_AsioPlayer.Stop();
        }

        private void SoundPlayTimer(object sender, EventArgs e)
        {
            _snd_playtimer.Stop();
            int nLen = nSoundPure.Count;
            if (nLen > 0) {
                int idx = 0;
                byte[] data = new byte[nLen];
                while (nSoundPure.Count > 0)
                {
                    Int32 localvalue;
                    if (nSoundPure.TryDequeue(out localvalue))
                    {
                        data[idx] = (byte)localvalue;
                        idx++;
                    }
                }
                if (_snd_bufferedWaveProvider.BufferedDuration > TimeSpan.FromMilliseconds(_save_SoundLatencyMargin))
                    _snd_bufferedWaveProvider.ClearBuffer();
                _snd_bufferedWaveProvider.AddSamples(data, 0, nLen);
            }
            _snd_playtimer.Start();
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
            //if (_save_ComportName == "" )
            //    return;
            if (_com_FreezeChange)
                return;

            if (_com_bConnectionStatus == false)
            {
                if (!setPage.ConfigComport(ref _save_ComportName, ref _save_ComportBaud))
                    return;

                //Sets up serial port
                _com_serial.PortName = _save_ComportName;
                _com_serial.BaudRate = GetMaxBaud(_save_ComportName, _save_ComportBaud);
                _com_serial.Handshake = System.IO.Ports.Handshake.None;
                _com_serial.Parity = Parity.None;
                _com_serial.DataBits = 8;
                _com_serial.StopBits = StopBits.Two;
                _com_serial.ReadTimeout = 200;
                _com_serial.WriteTimeout = 50;
                _com_serial.Open();

                if (_com_serial.IsOpen)
                {
                    SendCommand(Constants.CMD_LIVE);
                    numVoltDiv_ValueChanged(null, null);
                    SetChannel(nChannelNumber);
                    //Sets button State and Creates function call on data recieved
                    setPage.btnConnect.Content = "Disconnect";
                    _com_bConnectionStatus = true;
                    lblComState.Text = "COM Port Connected on Port " + _save_ComportName;
                    _com_serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);
                }

            }
            else
            {
                try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
                {
                    SendCommand(Constants.CMD_STOP);
                    ComPortClose();
                    setPage.btnConnect.Content = "Connect";
                    lblComState.Text = "Disconnected";
                    _com_bConnectionStatus = false;
                }
                catch
                {
                }
            }
        }
        
        private Int32 GetMaxBaud(string comName, Int32 comBaud)
        {
            List<Int32> SupportedBaudRates = new List<Int32>{300,600,1200,2400,4800,9600,19200,38400,
                57600,115200,230400,460800,921600,1000000,2000000,4000000,10000000};
            Int32 maxBaudRate = 0;
            try
            {
                //SupportedBaudRates has the commonly used baudRate rates in it
                //flavor to taste
                foreach (Int32 baudRate in SupportedBaudRates)
                {
                    using (SerialPort port = new SerialPort(comName))
                    {
                        port.BaudRate = baudRate;
                        port.Open();
                        port.Close();
                    }
                    maxBaudRate = baudRate;
                }
            }
            catch
            {
                //ignored - traps exception generated by
                //baudRate rate not supported
            }
            return Math.Min(maxBaudRate, comBaud);
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
            int nNOByte = _save_AdcBitNum >> 3;
            int nBufLimit = (Constants.HDRLEN + Constants.FRMLEN * nNOByte) * 2 ;
            while ((index != -1) & (bufferString.Length > nBufLimit))
            {
                index = bufferString.IndexOf(Constants.HDRSTRN20);
                if (index > -1)
                {
                    nTotPacketRecieved++;
                    nTotPureDataReceied += Constants.FRMLEN;
                    for (int i=0; i<Constants.FRMLEN; i++)
                    {
                        
                        int idx = i * nNOByte + index + Constants.HDRLEN ;
                        int[] nValByte = new int[nNOByte];
                        for (int j = 0; j < nNOByte; j++)
                            nValByte[j] = Convert.ToInt32(bufferString[idx + j]);
                        //int value = Convert.ToInt32(bufferString[idx]);
                        int value = 0 ;
                        for (int j = 0; j < nNOByte; j++)
                            value = nValByte[j] + (value << 8) ;
                        if (isDrawingSignal)
                            nDataPure.Enqueue(value);
                        if (_snd_isPlaying & isDrawingSignal) 
                            nSoundPure.Enqueue(value);
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

        private void SetChannel(int nCH)
        {
            if (nCH == -1)
            {
                if (nChannelNumber == 1)
                    nChannelNumber = 2;
                else if (nChannelNumber == 2)
                    nChannelNumber = 1;
            }
            else
                nChannelNumber = nCH;

            if (nChannelNumber == 1)
            {
                SendCommand(Constants.CMD_SETCHANNEL1);
                lblChannelName.Text = "Channel 1";
            }
            else if (nChannelNumber == 2)
            {
                SendCommand(Constants.CMD_SETCHANNEL2);
                lblChannelName.Text = "Channel 2";
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
                Connect_Comm(null, null);
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
            bool bSaveValidated = _tcp_bTCPActivate;
            if (!_tcp_bTCPActivate)
            {
                MessageBoxResult result = MessageBox.Show(
                        "Would you like to save Settings?",
                        "Confirmation",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                    bSaveValidated = true;
            }
            
           
            if (bSaveValidated)
            {
                //Save Settings
                Properties.Settings.Default.LOWCUTINDEX = (int)cmbLowPassFilter.SelectedIndex;
                Properties.Settings.Default.HIGHCUTINDEX = (int)cmbHighPassFilter.SelectedIndex;
                Properties.Settings.Default.VOLTDIV = (decimal)numVoltDiv.Value;
                Properties.Settings.Default.TIMEDIV = (decimal)numTimeDiv.Value;
                SaveSettings();
                Properties.Settings.Default.Save(); // Saves settings in application configuration file    
            }

            _sci_timer.Stop();
            //Escape Handles
            if (_com_serial.IsOpen)
            {
                ComPortClose();
            }
            StopSoundServices();
            _snd_player.Dispose();
            //_snd_AsioPlayer.Dispose();

            _tcp_myserver.bCloseOrder = true;
            _tcp_myserver.server.Stop();
            _tcp_bTCPActivate = false;
        }

#endregion

#region Save and Load Routines
        private void LoadSettings()
        {
            _save_RefreshValue = (int)Properties.Settings.Default.RREFRESHFPS;
            _save_SweepGraph = Properties.Settings.Default.SWEEPGRAPH;
            _save_ComportName = Properties.Settings.Default.COMPORTNAME;
            _save_ComportBaud = Properties.Settings.Default.COMPORTBAUD;
            _save_SoundLatencyMargin = Properties.Settings.Default.SOUNDLATENCY;
            _save_AdcBitNum = Properties.Settings.Default.ADCBITNUM;
            _save_HardWaveVersion = Properties.Settings.Default.HWVERSION;
            _save_N20ServerPort = Properties.Settings.Default.N20SERVERPORT;
            _save_N20ServerIP = Properties.Settings.Default.N20SERVERIP;
            _save_N14ServerPort = Properties.Settings.Default.N14SERVERPORT;
            _save_N14ServerIP = Properties.Settings.Default.N14SERVERIP;
        }

        private void LoadSettingContent()
        {
            //Load to Setting Control
            setPage.sldRefreshFPS.Value = _save_RefreshValue;
            setPage.cbxSweepGraph.IsChecked = _save_SweepGraph;
            setPage.ConfigComport(ref _save_ComportName, ref _save_ComportBaud);
            setPage.edxSoundLatencyMargin.Text = _save_SoundLatencyMargin.ToString();
            setPage.ConfigCombo(_save_AdcBitNum, ref setPage.cmbADCRes, Constants.DEFAULTADCBIT);
            setPage.ConfigCombo(_save_HardWaveVersion, ref setPage.cmbHWVersion, Constants.DEFAULTHWVERSION);
            setPage.edxN20ServerIP.Text = _save_N20ServerIP;
            setPage.edxN20ServerPort.Text = _save_N20ServerPort.ToString();
            setPage.edxN14ServerIP.Text = _save_N14ServerIP;
            setPage.edxN14ServerPort.Text = _save_N14ServerPort.ToString();
        }

        private void SetSettings()
        {
            _save_RefreshValue = (int) setPage.sldRefreshFPS.Value;
            _save_SweepGraph = (bool) setPage.cbxSweepGraph.IsChecked ;
            _save_ComportName = setPage.cmbComportName.Text;
            _save_ComportBaud = Convert.ToInt32(setPage.cmbBaudRate.Text);
            _save_SoundLatencyMargin = Convert.ToInt32(setPage.edxSoundLatencyMargin.Text);
            _save_AdcBitNum = Convert.ToInt32(setPage.cmbADCRes.Text);
            _save_HardWaveVersion = Convert.ToInt32(setPage.cmbHWVersion.Text);
            _save_N20ServerIP = setPage.edxN20ServerIP.Text;
            _save_N20ServerPort = Convert.ToInt32(setPage.edxN20ServerPort.Text);
            _save_N14ServerIP = setPage.edxN14ServerIP.Text;
            _save_N14ServerPort = Convert.ToInt32(setPage.edxN14ServerPort.Text);
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.RREFRESHFPS = _save_RefreshValue;
            Properties.Settings.Default.SWEEPGRAPH = _save_SweepGraph;
            Properties.Settings.Default.COMPORTNAME = _save_ComportName;
            Properties.Settings.Default.COMPORTBAUD = _save_ComportBaud;
            Properties.Settings.Default.SOUNDLATENCY = _save_SoundLatencyMargin;
            Properties.Settings.Default.ADCBITNUM = _save_AdcBitNum;
            Properties.Settings.Default.HWVERSION = _save_HardWaveVersion;
            Properties.Settings.Default.N20SERVERPORT = _save_N20ServerPort;
            Properties.Settings.Default.N20SERVERIP = _save_N20ServerIP;
            Properties.Settings.Default.N14SERVERPORT = _save_N14ServerPort;
            Properties.Settings.Default.N14SERVERIP = _save_N14ServerIP;
        }

        private void ApplySettings()
        {
            if (_save_RefreshValue != (int)setPage.sldRefreshFPS.Value) {

                _sci_timer.Stop();
                _sci_timer.Interval = TimeSpan.FromMilliseconds((1000.0M / (decimal)setPage.sldRefreshFPS.Value).ToDouble());
                _sci_timer.Start();
            }
            if (_save_SweepGraph != (bool)setPage.cbxSweepGraph.IsChecked)
            {
                bResetGraph = true;
            }
            bool bRefreshConnection = false;
            if ((_save_ComportBaud != Convert.ToInt32(setPage.cmbBaudRate.Text)) | !_save_ComportName.Equals(setPage.cmbComportName.Text))
                bRefreshConnection = true;
            SetSettings();
            if (bRefreshConnection)
            {
                if(_com_bConnectionStatus)
                    Connect_Comm(null, null);
                while (_com_serial.IsOpen) ;
                Connect_Comm(null,null);
            }
        }
#endregion

#region Button Routins
        private void SettingVisibilityChange(object sender, RoutedEventArgs e)
        {
            if ((DataContext as BorderViewModel).SettingVisible == Visibility.Visible)
            {
                _com_FreezeChange = false;
                ApplySettings();
            }
            else
                _com_FreezeChange = true;
                
            (DataContext as BorderViewModel).SettingVisible = (DataContext as BorderViewModel).SettingVisible == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            icoPlayPause.Icon = isDrawingSignal ? FontAwesomeIcon.PlayCircleOutline : FontAwesomeIcon.PauseCircleOutline ;
            isDrawingSignal = !isDrawingSignal;
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                btnPlayPause_Click(null, null);
            }
            else if (e.Key == Key.F1)
            {
                numVoltDiv.SetValue(0.0005M);
            }
            else if (e.Key == Key.F2)
            {
                numVoltDiv.SetValue(0.002M);
            }
            else if (e.Key == Key.F3)
            {
                numVoltDiv.SetValue(0.005M);
            }
            e = null;
            btnPlayPause.Focus();
        }

        private void rbChannel_Checked(object sender, RoutedEventArgs e)
        {
            if (!rbChannelOne.IsLoaded)
                return;
            if (!rbChannelTwo.IsLoaded)
                return;
            if (rbChannelOne.IsChecked == true)
                SetChannel(1);
            else if (rbChannelTwo.IsChecked == true)
                SetChannel(2);
        }

        #endregion

    }
}
