using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Assets.RemoteHandsTracking.Utilities
{
    public class UDPSender
    {
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public event EventHandler<string> DataSent;
        public event EventHandler Connected;

        private string _address;
        private int _port;


        public UDPSender(string address, int port)
        {
            _address = address;
            _port = port;
        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);

            if (!_socket.Connected)
            {
                _socket.Connect(IPAddress.Parse(_address), _port);
                Connected?.Invoke(null, EventArgs.Empty);
            }

            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                var bytes = _socket.EndSend(ar);
                DataSent?.Invoke(this, text);
            }, new object());
        }
    }
}