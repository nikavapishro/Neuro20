using SciChart.Charting.Visuals.Axes.LabelProviders;
using System;

namespace SciChartExamlpeOne
{
    public class CustomNumericLabelProvider : NumericLabelProvider
    {
        /// <summary>
        /// Formats a label for the axis from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted label string
        /// </returns>
        public override string FormatLabel(IComparable dataValue)
        {
            // Note: Implement as you wish, converting Data-Value to string
            try
            {
                double dVal = (double) dataValue ;
                double udVal = Math.Abs(dVal);
                if (udVal == 0)
                    return "0";

                string suffix = (udVal == 0.0) ? string.Empty : (udVal >= 1.0) ? string.Empty : (udVal >= 0.001) ? " m" : (udVal >= 0.000001) ? " u" : " n";
                dVal = (udVal >= 1.0) ? dVal : (udVal >= 0.001) ? (dVal * 1000.0) : (udVal >= 0.000001) ? (dVal * 1000000.0) : (dVal * 1000000000.0);

                return dVal.ToString("#.##") + suffix;
            }
            catch
            {
                return string.Empty;
            }

            // NOTES:
            // dataValue is always a double.
            // For a NumericAxis this is the double-representation of the data
            // For a DateTimeAxis, the conversion to DateTime is new DateTime((long)dataValue)
            // For a TimeSpanAxis the conversion to TimeSpan is new TimeSpan((long)dataValue)
            // For a CategoryDateTimeAxis, dataValue is the index to the data-series
        }

        /// <summary>
        /// Formats a label for the cursor, from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted cursor label string
        /// </returns>
        public override string FormatCursorLabel(IComparable dataValue)
        {
            // Note: Implement as you wish, converting Data-Value to string
            try
            {
                double dVal = (double) dataValue;
                double udVal = Math.Abs(dVal);
                if (udVal == 0)
                    return "0";

                string suffix = (udVal == 0.0) ? string.Empty : (udVal >= 1.0) ? string.Empty : (udVal >= 0.001) ? " m" : (udVal >= 0.000001) ? " u" : " n";
                dVal = (udVal >= 1.0) ? dVal : (udVal >= 0.001) ? (dVal * 1000.0) : (udVal >= 0.000001) ? (dVal * 1000000.0) : (dVal * 1000000000.0);

                return dVal.ToString("#.##") + suffix;
            }
            catch
            {
                return string.Empty;
            }

            // NOTES:
            // dataValue is always a double.
            // For a NumericAxis this is the double-representation of the data
            // For a DateTimeAxis, the conversion to DateTime is new DateTime((long)dataValue)
            // For a TimeSpanAxis the conversion to TimeSpan is new TimeSpan((long)dataValue)
            // For a CategoryDateTimeAxis, dataValue is the index to the data-series
        }
    }
}
