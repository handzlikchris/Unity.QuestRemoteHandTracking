using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.RemoteHandsTracking
{
    public class HandsDataReceiver : MonoBehaviour
    {
        [Serializable] public class HandDataReceivedUnityEvent : UnityEvent<HandData> { }
        [Serializable] public class SkeletonDataReceivedUnityEvent : UnityEvent<SkeletonData> { }
        [Serializable] public class MeshDataReceivedUnityEvent : UnityEvent<MeshData> { }

        public string ListenOnIp = "127.0.0.1";
        public int ListenOnPort = 27000;
        public bool IsDataGzipCompressed = true;

        public HandDataReceivedUnityEvent HandDataReceived = new HandDataReceivedUnityEvent();
        public SkeletonDataReceivedUnityEvent SkeletonDataReceived = new SkeletonDataReceivedUnityEvent();
        public MeshDataReceivedUnityEvent MeshDataReceived = new MeshDataReceivedUnityEvent();

        private HandData _lastLeftHandRenderUpdateDataToProcess;
        private HandData _lastLeftHandPhysicsUpdateDataToProcess;
        
        private HandData _lastRightHandRenderUpdateDataToProcess;
        private HandData _lastRightHandPhysicsUpdateDataToProcess;
        private FixedSizedQueue<SkeletonData> _skeletonUpdateDataToProcess = new FixedSizedQueue<SkeletonData>(50);
        private FixedSizedQueue<MeshData> _meshUpdateDataToProcess = new FixedSizedQueue<MeshData>(50);
        private TcpReciever _tcpReceiver;
        private IPEndPoint _ipEndPoint;
        private UdpClient _udpClient;
        private bool _stopListeningForUdpData;

        private void Start()
        {
            _udpClient = new UdpClient();
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(ListenOnIp), ListenOnPort);
            _udpClient.Client.Bind(_ipEndPoint);
            _udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            new Thread(ListenForUdpData).Start();
            
            _tcpReceiver = new TcpReciever(ListenOnPort, ListenOnIp);
            _tcpReceiver.DataReceived += (sender, data) =>
            {
                HandleIncomingData(data);
            };
            _tcpReceiver.StartListeningOnNewThread();
        }

        void OnApplicationQuit()
        {
            _stopListeningForUdpData = true;
            _tcpReceiver.Stop();
        }

        private void ListenForUdpData()
        {
            while (!_stopListeningForUdpData)
            {
                try
                {
                    var data = _udpClient.Receive(ref _ipEndPoint);
                    HandleIncomingData(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to get hand data, {e}");
                }
            }
        }

        private void HandleIncomingData(byte[] data)
        {
            var stringData = Encoding.ASCII.GetString(data);
            var handRelatedData = XmlSerialize.Deserialize<HandRelatedDataContainer>(IsDataGzipCompressed
                ? GzipCompression.Decompress(stringData)
                : stringData);

            switch (handRelatedData.HandRelatedData)
            {
                case HandRelatedData.Hand:
                    var handData = handRelatedData.HandData;
                    if (handData.Step == OVRPlugin.Step.Render)
                    {
                        if (handData.Hand == OVRPlugin.Hand.HandLeft) _lastLeftHandRenderUpdateDataToProcess = handData;
                        if (handData.Hand == OVRPlugin.Hand.HandRight) _lastRightHandRenderUpdateDataToProcess = handData;
                    }

                    if (handData.Step == OVRPlugin.Step.Physics)
                    {
                        if (handData.Hand == OVRPlugin.Hand.HandLeft) _lastLeftHandPhysicsUpdateDataToProcess = handData;
                        if (handData.Hand == OVRPlugin.Hand.HandRight) _lastRightHandPhysicsUpdateDataToProcess = handData;
                    }
                    break;

                case HandRelatedData.Skeleton:
                    _skeletonUpdateDataToProcess.Enqueue(handRelatedData.SkeletonData);
                    break;

                case HandRelatedData.Mesh:
                    _meshUpdateDataToProcess.Enqueue(handRelatedData.MeshData);
                    break;


                case HandRelatedData.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FixedUpdate()
        {
            ProcessHandData(ref _lastLeftHandPhysicsUpdateDataToProcess);
            ProcessHandData(ref _lastRightHandPhysicsUpdateDataToProcess);
        }

        private void Update()
        {
            if (_skeletonUpdateDataToProcess.TryDequeue(out var skeletonDataToProcess))
                SkeletonDataReceived?.Invoke(skeletonDataToProcess);

            if (_meshUpdateDataToProcess.TryDequeue(out var meshDataToProcess))
                MeshDataReceived?.Invoke(meshDataToProcess);

            ProcessHandData(ref _lastLeftHandRenderUpdateDataToProcess);
            ProcessHandData(ref _lastRightHandRenderUpdateDataToProcess);
        }

        private void ProcessHandData(ref HandData handData)
        {
            if (handData != null)
            {
                HandDataReceived?.Invoke(handData);
                handData = null;
            }
        }
    }
}