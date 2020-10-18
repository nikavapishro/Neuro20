﻿using SciChart.Charting.Model.DataSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciChartExamlpeOne
{
    class FilterData
    {
        public XyDataSeries<double, double> _DataSeries;
        public bool isNotchEnable = false;
        public bool isFifoEnable = false;

        public int nFifoLen = 0;
        private int nLastIndex;
        private const int _length_MA = Constants.MOVINGAVERAGE_LENGTH;   //Moving Average Filter Length
        private List<double> _OriginalValues;
        private FilterTypes _filtertype = FilterTypes.Buffer;
        private LowpassFilterButterworthImplementation _firLowPass;
        private HighpassFilterButterworthImplementation _firHighPass;
        private BandpassFilterButterworthImplementation _firBandPass;
        private NotchFilterImplementation _firNotch;


        public FilterData()
        {
            _DataSeries = new XyDataSeries<double, double>();
            _OriginalValues = new List<double>();
            nLastIndex = 0;
        }

        public FilterData(FilterTypes _ftype)
        {
            _DataSeries = new XyDataSeries<double, double>();
            _OriginalValues = new List<double>();
            nLastIndex = 0;
            _filtertype = _ftype;
        }

        public FilterData(FilterTypes _ftype, double Fsample, int order1, double F3db1, int order2 = 0, double F3db2 = 0)
        {
            _DataSeries = new XyDataSeries<double, double>();
            nLastIndex = 0;
            _filtertype = _ftype;
            if (_filtertype == FilterTypes.FIRLowPass)
                _firLowPass = new LowpassFilterButterworthImplementation(F3db1, order1, Fsample);
            else if (_filtertype == FilterTypes.FIRLP_MA) {
                _firLowPass = new LowpassFilterButterworthImplementation(F3db1, order1, Fsample);
                _OriginalValues = new List<double>();
            }
            else if (_filtertype == FilterTypes.FIRHighPass)
                _firHighPass = new HighpassFilterButterworthImplementation(F3db1, order1, Fsample);
            else if (_filtertype == FilterTypes.FIRBandPass)
                _firBandPass = new BandpassFilterButterworthImplementation(F3db1, order1, F3db2, order2, Fsample);
            else if (_filtertype == FilterTypes.FIRBP_MA)
            {
                _firBandPass = new BandpassFilterButterworthImplementation(F3db1, order1, F3db2, order2, Fsample);
                _OriginalValues = new List<double>();
            }
            else if(_filtertype == FilterTypes.Notch)
            {
                _firNotch = new NotchFilterImplementation(F3db1, Fsample, order1);
            }
                
        }

        public void Clear()
        {
            _DataSeries.Clear();
            _OriginalValues.Clear();
            nLastIndex = 0;
        }

        public void ResetFilter(FilterTypes _ftype, double Fsample, int order1, double F3db1, int order2 = 0, double F3db2 = 0)
        {
            if (_ftype == FilterTypes.FIRLowPass || _ftype == FilterTypes.FIRLP_MA)
            {
                _firLowPass = new LowpassFilterButterworthImplementation(F3db1, order1, Fsample);
            }
            else if (_ftype == FilterTypes.FIRHighPass)
            {
                _firHighPass = new HighpassFilterButterworthImplementation(F3db1, order1, Fsample);
            }
            else if (_ftype == FilterTypes.FIRBandPass || _ftype == FilterTypes.FIRBP_MA)
            {
                _firBandPass = new BandpassFilterButterworthImplementation(F3db1, order1, F3db2, order2, Fsample);
            }
            else if (_ftype == FilterTypes.Notch)
            {
                _firNotch = new NotchFilterImplementation(F3db1, Fsample, order1);
            }
        }

        public void Append(double xValue, double yValue)
        {
            double result = 0.0;
            if (_filtertype == FilterTypes.MovingAverage)
            {
                result = MovingAverage(yValue);
            }
            else if (_filtertype == FilterTypes.LowPass)
            {
                result = LowPass(yValue);
            }
            else if (_filtertype == FilterTypes.FIRLowPass)
            {
                result = _firLowPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRHighPass)
            {
                result = _firHighPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRBandPass)
            {
                result = _firBandPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRLP_MA)
            {
                double val = _firLowPass.compute(yValue);
                result = MovingAverage(val);
            }
            else if (_filtertype == FilterTypes.FIRBP_MA)
            {
                double val = _firBandPass.compute(yValue);
                result = MovingAverage(val);
            }
            else
                result = yValue;

            if (isFifoEnable)
            {
                double val = isNotchEnable ? _firNotch.compute(result) : result;

                //Semi Fifo implementatio for decrease memory usage
                if (_DataSeries.Count >= nFifoLen * 2)
                    _DataSeries.RemoveRange(_DataSeries.Count - 2 * nFifoLen, nFifoLen);

                _DataSeries.Append(xValue, val);
            }
            else
            {
                if (isNotchEnable)
                {
                    _DataSeries.Append(xValue, _firNotch.compute(result));
                }
                else
                {
                    _DataSeries.Append(xValue, result);
                }
            }

            nLastIndex++;
        }

        public void Update(double xValue, double yValue)
        {
            double result = 0.0;
            if (_filtertype == FilterTypes.MovingAverage)
            {
                result = MovingAverage(yValue);
            }
            else if (_filtertype == FilterTypes.LowPass)
            {
                result = LowPass(yValue);
            }
            else if (_filtertype == FilterTypes.FIRLowPass)
            {
                result = _firLowPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRHighPass)
            {
                result = _firHighPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRBandPass)
            {
                result = _firBandPass.compute(yValue);
            }
            else if (_filtertype == FilterTypes.FIRLP_MA)
            {
                double val = _firLowPass.compute(yValue);
                result = MovingAverage(val);
            }
            else if (_filtertype == FilterTypes.FIRBP_MA)
            {
                double val = _firBandPass.compute(yValue);
                result = MovingAverage(val);
            }
            else
                result = yValue;

            if (isNotchEnable)
            {
                _DataSeries.Update(xValue, _firNotch.compute(result));
            }
            else
            {
                _DataSeries.Update(xValue, result);
            }

            nLastIndex++;
        }

        public int GetLastIndex()
        {
            return nLastIndex;
        }

        #region Moving Average
        private double MovingAverage(double yvalue)
        {
            _OriginalValues.Add(yvalue);
            return AverageOf(nLastIndex - _length_MA + 1);
        }

        private double AverageOf(int from)
        {
            double result = double.NaN;

            if (from >= 0)
            {
                result = 0.0;

                for (int i = 0; i < _length_MA; i++)
                {
                    result += _OriginalValues[i];
                }
                _OriginalValues.RemoveAt(0);
            }

            return result / _length_MA;
        }
        #endregion


        #region IIR Low Pass Filter
        private double LowPass(double yvalue)
        {
            const int _order = 1 ;
            const double beta = 0.2;
            //// Implementing a simple low pass filter https://kiritchatterjee.wordpress.com/2014/11/10/a-simple-digital-low-pass-filter-in-c/
            //for (int i = 1; i < _originalDataSeries.Count; i++)
            //{
            //    double xValue = _originalDataSeries.XValues[i];
            //    double yValue = beta * _originalDataSeries.YValues[i] + (1 - beta) * _filteredDataSeries.YValues[i - 1];
            //    _filteredDataSeries.Append(xValue, yValue);
            //}

            double res = 0.0;
            if (nLastIndex >= _order)
            {
                res = beta * yvalue + (1-beta) * _DataSeries.YValues[nLastIndex-1];
            }
            return res ;
        }
        #endregion
    }
}
