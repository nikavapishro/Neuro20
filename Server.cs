using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SciChartExamlpeOne
{
    class Server
    {
        public EventHandler UpdateConnection;
        public TcpListener server = null;
        public int nSW1Connection, nSW2Connection;
        public bool bCloseRequest;
        public bool bCloseOrder;
        public bool bChangeSizeRequest;
        public bool bChangeComRequest;
        public int nWindowsState;
        Thread tHandleDevice;

        public Server(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            tHandleDevice = new Thread(new ParameterizedThreadStart(HandleDeivce));
            nSW1Connection = 0;
            nSW1Connection = 0;
            bCloseRequest = false;
            bCloseOrder = false;
            bChangeSizeRequest = false;
            bChangeComRequest = false;
            nWindowsState = 0;
        }

        public void StartListener()
        {
            try
            {
                while (!bCloseOrder)
                {
                    TcpClient client = server.AcceptTcpClient();
                    tHandleDevice = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    tHandleDevice.Start(client);
                    Thread.Sleep(3);
                }

            }
            catch (SocketException e)
            {
                server.Stop();
            }
        }

        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string imei = String.Empty;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i = 0;
            try
            {
                while (((i = stream.Read(bytes, 0, bytes.Length)) != 0) && !bCloseOrder)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    if (data == Constants.NEURO14CON)
                        nSW1Connection = 1;
                    else if (data == Constants.NEURO14DIS)
                        nSW1Connection = 0;
                    else if (data == Constants.NEURO20CON)
                    {
                        bChangeComRequest = true;
                        nSW2Connection = 1;
                    }
                    else if (data == Constants.NEURO20DIS)
                    {
                        bChangeComRequest = true;
                        nSW2Connection = 0;
                    }
                    else if (data == Constants.NEURO20BYEBYE)
                        bCloseRequest = true;
                    else if (data == Constants.NEURO20MAXI)
                    {
                        bChangeSizeRequest = true;
                        nWindowsState = 1;
                    }
                    else if (data == Constants.NEURO20MINI)
                    {
                        bChangeSizeRequest = true;
                        nWindowsState = 0;
                    }
                    OnMyMethod();
                    Thread.Sleep(3);
                    client.Close();
                    tHandleDevice.Abort();
                    if (bCloseOrder)
                        break;
                }

            }
            catch (Exception e)
            {
                client.Close();
            }

        }

        public void OnMyMethod()
        {
            // has the event handler been assigned?
            if (this.UpdateConnection != null)
            {
                // raise the event
                this.UpdateConnection(this, new EventArgs());
            }
        }
    }
}
