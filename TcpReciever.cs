using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Assets.RemoteHandsTracking
{
    public class TcpReciever
    {
        public event EventHandler<byte[]> DataReceived;

        private TcpListener tcpListener;
        private Thread tcpListenerThread;
        private TcpClient connectedTcpClient;

        private readonly int _listenOnPort;
        private readonly string _listenOnIp;

        private bool _stopListenerThread;

        public TcpReciever(int listenOnPort, string listenOnIp)
        {
            _listenOnPort = listenOnPort;
            _listenOnIp = listenOnIp;
        }

        public void StartListeningOnNewThread()
        {
            tcpListenerThread = new Thread(() =>
            {
                tcpListener = new TcpListener(IPAddress.Parse(_listenOnIp), _listenOnPort);
                tcpListener.Start();
                Debug.Log("Server is listening");
                tcpListener.BeginAcceptTcpClient(StartReadingTcpConnectionData, new object());
            });
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        }

        public void Stop()
        {
            tcpListener.Stop();
            _stopListenerThread = true;
        }

        const int OneMbInBits = 1 * 8 * 1000 * 1000;
        private void StartReadingTcpConnectionData(IAsyncResult result)
        {
            try
            {
                connectedTcpClient = tcpListener.EndAcceptTcpClient(result);
                Debug.Log("TCP Connection Accepted");
                tcpListener.BeginAcceptTcpClient(StartReadingTcpConnectionData, new object());

                var packetProtocol = new PacketProtocol(OneMbInBits, (message) =>
                {
                    if (message.Length > 0)
                        DataReceived?.Invoke(this, message);
                });

                var buffer = new Byte[connectedTcpClient.ReceiveBufferSize];
                while (!_stopListenerThread)
                {
                    var receivedDataByteCount = connectedTcpClient.Client.Receive(buffer);
                    var readBytes = new byte[receivedDataByteCount];
                    Array.Copy(buffer, readBytes, receivedDataByteCount);
                    packetProtocol.DataReceived(readBytes);
                }
            }
            catch (SocketException socketException)
            {
                Debug.LogError("SocketException " + socketException.ToString());
            }
        }
    }
}