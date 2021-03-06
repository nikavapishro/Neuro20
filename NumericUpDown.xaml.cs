﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace SciChartExamlpeOne
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        private decimal[] BaseValue = { 10, 25, 50, 75 };
        private decimal[] PowerValue = { 0.0000001M, 0.000001M, 0.00001M, 0.0001M, 0.001M, 0.01M, 0.1M };
        private int nIndex = 8;
        public static decimal MinValue = 0;
        public static decimal MaxValue = 10;

        public NumericUpDown()
        {
            InitializeComponent();
            Value = 0.0001M;
            StringValue = "100 u";
        }

        /// <summary>
        /// Identifies the Value dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value", typeof(decimal), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(MinValue, new PropertyChangedCallback(OnValueChanged),
                                              new CoerceValueCallback(CoerceValue)));

        public static readonly DependencyProperty StringProperty =
            DependencyProperty.Register(
                "StringValue", typeof(string), typeof(NumericUpDown));

        /// <summary>
        /// Gets or sets the value assigned to the control.
        /// </summary>
        public void SetValue(decimal value) {
            nIndex = GetIndexFromValue(value);
            StringValue = CastValue(value);
            Value = value;
        }
        
        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string StringValue
        {
            get { return (string)GetValue(StringProperty); }
            set { SetValue(StringProperty, value); }
        }

        private static object CoerceValue(DependencyObject element, object value)
        {
            decimal newValue = (decimal)value;
            NumericUpDown control = (NumericUpDown)element;

            newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

            return newValue;
        }

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            NumericUpDown control = (NumericUpDown)obj;

            RoutedPropertyChangedEventArgs<decimal> e = new RoutedPropertyChangedEventArgs<decimal>(
                (decimal)args.OldValue, (decimal)args.NewValue, ValueChangedEvent);
            control.OnValueChanged(e);
        }

        /// <summary>
        /// Identifies the ValueChanged routed event.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumericUpDown));

        /// <summary>
        /// Occurs when the Value property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        /// <summary>
        /// Raises the ValueChanged event.
        /// </summary>
        /// <param name="args">Arguments associated with the ValueChanged event.</param>
        protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<decimal> args)
        {
            RaiseEvent(args);
        }

        private void upButton_Click(object sender, EventArgs e)
        {
            nIndex++;
            nIndex = Math.Min(nIndex, (BaseValue.Length * PowerValue.Length) - 1);
            Value = (decimal)CalValue(nIndex);
            StringValue = CastValue(Value);
        }

        private void downButton_Click(object sender, EventArgs e)
        {
            if (--nIndex < 0)
                nIndex = 0;
            Value = (decimal)CalValue(nIndex);
            StringValue = CastValue(Value);
        }

        private decimal CalValue(int idx)
        {
            int nBaseIndex = 0;
            int nPowIndex = Math.DivRem(idx, BaseValue.Length, out nBaseIndex);
            return (decimal)BaseValue.GetValue(nBaseIndex) * (decimal)PowerValue.GetValue(nPowIndex);
        }

        private string CastValue(decimal nValue)
        {
            string strResult = "NOT SET";
            if (nValue < 0.001M)
            {
                strResult = (nValue * 1000000).ToString("0.#");
                strResult += " u";
            }
            else if (nValue < 1.0M)
            {
                strResult = (nValue * 1000).ToString("0.#");
                strResult += " m";
            }
            else
            {
                strResult = (nValue).ToString("0.#");
            }
            return (string)strResult;
        }

        private int GetIndexFromValue(decimal value)
        {
            int nidx = 0;
            while (value > CalValue(nidx))
                nidx++;
            return nidx;
        }

    }
}
