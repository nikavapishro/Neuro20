using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciChartExamlpeOne
{

    class Command
    {
        public Byte Start;
        public Byte Type;
        public Byte nType;
        public Byte CheckSum;

        // Type:            SetGain     Shock         SetCurrent
        public Int32 Param1;  //   G1          nRep          Current
        public Int32 Param2;  //   G2          Period        0
        public Int32 Param3;  //   0           SampNum       0
        public Int32 Param4;  //   0           Duration      0

        public int GetSize() {
            int n = 0;
            n += sizeof(byte);  //Start
            n += sizeof(byte);  //Type
            n += sizeof(byte);  //nType
            n += sizeof(byte);  //CheckSum
            n += sizeof(Int32); //Param1
            n += sizeof(Int32); //Param2
            n += sizeof(Int32); //Param3
            n += sizeof(Int32); //Param4
            return n;
        }

        private void CalCheckSum()
        {
            CheckSum = 0;
        }

        public byte[] getCommand() {
            CalCheckSum();
            byte[] cmd = new byte[GetSize()];
            byte[] _sendBuffer = new byte[] { Start, Type, nType, CheckSum };
            byte[] _param1 = BitConverter.GetBytes(Param1);
            byte[] _param2 = BitConverter.GetBytes(Param2);
            byte[] _param3 = BitConverter.GetBytes(Param3);
            byte[] _param4 = BitConverter.GetBytes(Param4);
            int nLen = 0;
            System.Buffer.BlockCopy(_sendBuffer, 0, cmd, nLen,_sendBuffer.Length);
            nLen += _sendBuffer.Length;
            System.Buffer.BlockCopy(_param1, 0, cmd, nLen, _param1.Length);
            nLen += _param1.Length;
            System.Buffer.BlockCopy(_param2, 0, cmd, nLen, _param2.Length);
            nLen += _param2.Length;
            System.Buffer.BlockCopy(_param3, 0, cmd, nLen, _param3.Length);
            nLen += _param3.Length;
            System.Buffer.BlockCopy(_param4, 0, cmd, nLen, _param4.Length);
            return cmd;
        }

    }
}
