using SharpDX;
using SharpDX.DirectSound;
using SharpDX.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace SciChartExamlpeOne
{
    class SoundPlay
    {
        private Int32 _AvgBytesPerSec;
        private Int32 _BlockAlign;
        private Int32 _nChannels;
        private Int32 _SamplesPerSec;
        private Int32 _BytesPerSample;
        private Int32 _BuffLen;
        private Int32 m_BufOffset;
        private bool _isPlaying;
        private Int32 _nLimitBuffer;

        private DirectSound _directsound;
        private SecondarySoundBuffer _soundBuffer;

        public SoundPlay(int SamplesPerSec, int nChannels, int BytesPerSample, IntPtr hnd)
        {
            _nChannels = nChannels;
            _SamplesPerSec = SamplesPerSec;
            _BytesPerSample = BytesPerSample;
            _AvgBytesPerSec = nChannels * SamplesPerSec * BytesPerSample;
            _BlockAlign = nChannels * BytesPerSample;

            _BuffLen = _AvgBytesPerSec ;
            m_BufOffset = 0;
            _isPlaying = false;

            _nLimitBuffer = (int)(_BuffLen * 0.1); //10% of Buffer Length

            Init(hnd);
        }

        private void Init(IntPtr hnd)
        {
            _directsound = new DirectSound();
            _directsound.SetCooperativeLevel(hnd, CooperativeLevel.Priority);

            var primaryBufferDesc = new SoundBufferDescription();
            primaryBufferDesc.Flags = BufferFlags.PrimaryBuffer;
            primaryBufferDesc.AlgorithmFor3D = Guid.Empty;
            var primarySoundBuffer = new PrimarySoundBuffer(_directsound, primaryBufferDesc);
            // Play the PrimarySound Buffer
            primarySoundBuffer.Play(0, PlayFlags.Looping);

            WaveFormat _format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,
                    _SamplesPerSec, _nChannels, _AvgBytesPerSec, _BlockAlign, (_BytesPerSample * 8));

            SoundBufferDescription bufferDescription = new SoundBufferDescription();
            bufferDescription.BufferBytes = _BuffLen;
            bufferDescription.Format = _format;
            bufferDescription.Flags = BufferFlags.GetCurrentPosition2 | BufferFlags.ControlPositionNotify | BufferFlags.GlobalFocus |
                                        BufferFlags.ControlVolume | BufferFlags.StickyFocus;
            bufferDescription.AlgorithmFor3D = Guid.Empty;

            _soundBuffer = new SecondarySoundBuffer(_directsound, bufferDescription);

        }

        //public void WriteSamples(byte[] _samples, int N)
        //{
        //    int _N = N * _BytesPerSample * _nChannels;
        //    int _PlayCursor, _WriteCursor;
        //    _soundBuffer.GetCurrentPosition(out _PlayCursor, out _WriteCursor);

        //    DataStream waveBufferData2;
        //    var waveBufferData1 = _soundBuffer.Lock(0, _N, LockFlags.None, out waveBufferData2);

        //    // Copy the wave data into the buffer.
        //    waveBufferData1.Write(_samples, 0, N);

        //    // Unlock the secondary buffer after the data has been written to it.
        //    _soundBuffer.Unlock(waveBufferData1, waveBufferData2);
        //    if (!_isPlaying)
        //        Play();
        //}
        public void Play()
        {
            if (_directsound == null)
                return;
            _isPlaying = true;
            _soundBuffer.Play(0, PlayFlags.Looping);
        }
        public void WriteSamples(byte[] _samples, int N)
        {
            int size = 0;
            int _N = N;  // * _BytesPerSample * _nChannels;
            int _PlayCursor, _WriteCursor;
            _soundBuffer.GetCurrentPosition(out _PlayCursor, out _WriteCursor);

            int nFree = 0;
            if (_PlayCursor > m_BufOffset)
                nFree = _PlayCursor - m_BufOffset;
            else
                nFree = _BuffLen - m_BufOffset + _PlayCursor;

            if (N > nFree)
                size = nFree;
            else
                size = N;

            byte[] Data = new byte[size];
            for (int i = 0; i < N; i++)
                Data[i] = _samples[i];
            for (int i = N; i < size; i++)
                Data[i] = 0;

            DataStream waveBufferData2 ;
            var waveBufferData1 = _soundBuffer.Lock(m_BufOffset, size, LockFlags.None, out waveBufferData2);

            int _n1 = (int)waveBufferData1.RemainingLength;
            waveBufferData1.Write(_samples, 0,_n1);
            //if (size != waveBufferData1.Length)
            //    _n1 = _n1;

            //if (waveBufferData2 != null)
            //{
            //    int _n2 = _N - _n1;
            //    byte[] _samples_rem = new byte[_n2];
            //    for (int i = 0; i < _n2; i++)
            //        _samples_rem[i] = _samples[i + _n1];
            //    waveBufferData2.Write(_samples_rem, 0, _n2);
            //}
                

            // Copy the wave data into the buffer.
            //    waveBufferData1.Write(_samples, 0, N);

            
            //memcpy(Ptr1 , Data , NB1 );
            //if(Ptr2)
            //    memcpy(Ptr2 , ((BYTE*) Data)+NB1 , NB2 );
            
            _soundBuffer.Unlock(waveBufferData1,waveBufferData2);

            m_BufOffset += Math.Min(N, size);
            if (m_BufOffset >= _BuffLen)
                m_BufOffset -= _BuffLen;

            if (!_isPlaying)
            {
                int nDiff = m_BufOffset - _PlayCursor;
                if (nDiff >= _nLimitBuffer)
                    Play();
            }
        }
        void ShutdownSound()
        {
            // Release the primary sound buffer pointer.
            if (_soundBuffer != null)
            {
                _soundBuffer.Dispose();
                _soundBuffer = null;
            }

            // Release the direct sound interface pointer.
            if (_directsound != null)
            {
                _directsound.Dispose();
                _directsound = null;
            }
        }

        //private void mainInit()
        //{
        //    DirectSound _directsound = new DirectSound();
        //    //Handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
        //    _directsound.SetCooperativeLevel(Handle, CooperativeLevel.Priority);

        //    var primaryBufferDesc = new SoundBufferDescription();
        //    primaryBufferDesc.Flags = BufferFlags.PrimaryBuffer;
        //    primaryBufferDesc.AlgorithmFor3D = Guid.Empty;

        //    var primarySoundBuffer = new PrimarySoundBuffer(_directsound, primaryBufferDesc);

        //    // Play the PrimarySound Buffer
        //    primarySoundBuffer.Play(0, PlayFlags.Looping);

        //    // Default WaveFormat Stereo 44100 16 bit
        //    WaveFormat waveFormat = new WaveFormat();

        //    // Create SecondarySoundBuffer
        //    var secondaryBufferDesc = new SoundBufferDescription();
        //    secondaryBufferDesc.BufferBytes = waveFormat.ConvertLatencyToByteSize(60000);
        //    secondaryBufferDesc.Format = waveFormat;
        //    secondaryBufferDesc.Flags = BufferFlags.GetCurrentPosition2 | BufferFlags.ControlPositionNotify | BufferFlags.GlobalFocus |
        //                                BufferFlags.ControlVolume | BufferFlags.StickyFocus;
        //    secondaryBufferDesc.AlgorithmFor3D = Guid.Empty;
        //    var secondarySoundBuffer = new SecondarySoundBuffer(_directsound, secondaryBufferDesc);

        //    // Get Capabilties from secondary sound buffer
        //    var capabilities = secondarySoundBuffer.Capabilities;

        //    // Lock the buffer
        //    DataStream dataPart2;
        //    var dataPart1 = secondarySoundBuffer.Lock(0, capabilities.BufferBytes, LockFlags.EntireBuffer, out dataPart2);

        //    // Fill the buffer with some sound
        //    int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
        //    for (int i = 0; i < numberOfSamples; i++)
        //    {
        //        double vibrato = Math.Cos(2 * Math.PI * 10.0 * i / waveFormat.SampleRate);
        //        short value = (short)(Math.Cos(2 * Math.PI * (220.0 + 4.0 * vibrato) * i / waveFormat.SampleRate) * 16384); // Not too loud
        //        dataPart1.Write(value);
        //    }

        //    // Unlock the buffer
        //    secondarySoundBuffer.Unlock(dataPart1, dataPart2);

        //    // Play the song
        //    secondarySoundBuffer.Play(0, PlayFlags.Looping);
        //}
    }
}
