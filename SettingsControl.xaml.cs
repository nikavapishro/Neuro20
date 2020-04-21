using SciChart.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows.Controls;
using System.Xml;

namespace SciChartExamlpeOne
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
        }

        public bool ConfigComport(ref string _ComportName, ref int _ComportBaud)
        {
            bool bIsPortValid = true;
            cmbComportName.Items.Clear();
            foreach (string strComName in SerialPort.GetPortNames())
            {
                cmbComportName.Items.Add(strComName);
            }
            if (_ComportName == "")
                cmbComportName.SelectedIndex = 0;
            else
                cmbComportName.SelectedIndex = cmbComportName.Items.IndexOf(_ComportName);
            
            if (cmbComportName.SelectedIndex == -1)
                cmbComportName.SelectedIndex = 0;

            if (cmbComportName.Text != "")
                _ComportName = cmbComportName.Text;
            else
                bIsPortValid = false;


            List<string> strElm = new List<string>();
            foreach ( XmlElement elm in cmbBaudRate.Items)
                strElm.Add((string) elm.InnerText) ;

            int defIndex = strElm.IndexOf(Constants.DEFAULTBAUD.ToString());
            int selIndex = strElm.IndexOf(_ComportBaud.ToString());

            if (_ComportBaud == 0)
                cmbBaudRate.SelectedIndex = defIndex;
            else
                cmbBaudRate.SelectedIndex = selIndex;

            if (cmbBaudRate.SelectedIndex == -1)
                cmbBaudRate.SelectedIndex = defIndex;

            if (cmbBaudRate.Text != "")
                _ComportBaud = (int)cmbBaudRate.SelectedValue.ToString().ToDouble();

            return bIsPortValid;
        }
    }
}
