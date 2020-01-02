using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Utilities
{
    public class TCPSender
    {
        public event EventHandler Connected;

        private readonly TcpClient _tcpClient;
        private readonly string _ipEndpoint;
        private readonly int _portEndpoint;

        public TCPSender(string ipEndpoint, int portEndpoint)
        {
            _ipEndpoint = ipEndpoint;
            _portEndpoint = portEndpoint;

            _tcpClient = new TcpClient();
        }

        public void ConnectToTcpServer()
        {
            try
            {
                _tcpClient.Client.Disconnect(true);
                _tcpClient.Connect(IPAddress.Parse(_ipEndpoint), _portEndpoint);
                Connected?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Debug.Log("On client connect exception " + e);
            }
        }

        public void SendKeepAlivePacket()
        {
            Send(PacketProtocol.WrapKeepaliveMessage());
        }

        public void Send(string message) => Send(Encoding.ASCII.GetBytes(message));

        public void Send(byte[] message)
        {
            if (!_tcpClient.Connected)
            {
                ConnectToTcpServer();
            }

            SendInternal(message);
        }

        private void SendInternal(byte[] message)
        {
            if (_tcpClient == null)
            {
                return;
            }

            try
            {
                NetworkStream stream = _tcpClient.GetStream();
                if (stream.CanWrite)
                {
                    byte[] clientMessageAsByteArrayWrapped = PacketProtocol.WrapMessage(message);

                    stream.Write(clientMessageAsByteArrayWrapped, 0, clientMessageAsByteArrayWrapped.Length);
                    Debug.Log($"Message sent (size: {clientMessageAsByteArrayWrapped.Length})");
                }
            }
            catch (SocketException socketException)
            {
                Debug.LogError("Socket exception: " + socketException);
            }
        }
    }
}