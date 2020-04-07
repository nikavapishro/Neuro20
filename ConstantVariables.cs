using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciChartExamlpeOne
{
    static class Constants
    {
        public const byte HDRLEN = 5;
        public const byte FRMLEN = 80;
        public const byte PACKETLEN = FRMLEN + HDRLEN;
        public const int BUFLENCHECK = (HDRLEN + FRMLEN) * 2;
        public const string HDRSTR = "\xFF\x00\xFE\x01\xFD";

        //COMMAND
        public const byte CMD_LIVE = 0x11;
        public const byte CMD_SHOCK = 0x33;
        public const byte CMD_AUDIO = 0x3E;
        public const byte CMD_STOP = 0xF1;
        public const byte CMD_PAUSE = 0xF2;
        public const byte CMD_RESUME = 0xF3;
        public const byte CMD_GAIN = 0xEE;
        public const byte CMD_CURRENT = 0x44;
        public const byte CMD_COMTEST = 0xEF;
        public const byte CMD_SETCHANNEL = 0xF4;
        public const byte CMD_ENABLESTIM = 0x27;
        public const byte CMD_DISABLESTIM = 0x39;

        //Filters
        public const int FIR_LOWPASS_ORDER = 4;
        public const int FIR_HIGHPASS_ORDER = 1;
        public const int MOVINGAVERAGE_LENGTH = 11;
        public const int FIR_NOTCH = 3;
    }

    class ConstantVariables
    {
    }
}
