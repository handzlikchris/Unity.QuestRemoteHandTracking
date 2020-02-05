using System;
using System.Collections;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Utilities;
using UnityEngine;

namespace Assets.RemoteHandsTracking
{
    public class HandsDataSender : MonoBehaviour
    {
        public string SendToIp = "127.0.0.1";
        public int SendToPort = 27000;
        public bool CompressDataWithGzip = true;
        public int SendKeepAlivePacketEveryNSeconds = 5;

        private static int WaitNSecondsBetweenHandDataInitializationIfNotReady = 1;
        private static readonly int WaitNSecondsBetweenTCPSendingFailures = 5;
        private UDPSender _udpSender;
        private TCPSender _tcpSender;

        void Start()
        {
            _tcpSender = new TCPSender(SendToIp, SendToPort);
            _tcpSender.Connected += (sender, args) =>
            {
                PollSendSendSekeletonAndMeshData();
            };

#if !UNITY_EDITOR
            StartCoroutine(KeepTcpAlive());
#endif
        }

        private void PollSendSendSekeletonAndMeshData()
        {
            StartCoroutine(InitializeSkeletonAndSend(OVRPlugin.Hand.HandLeft));
            StartCoroutine(InitializeSkeletonAndSend(OVRPlugin.Hand.HandRight));

            StartCoroutine(InitializeMeshAndSend(OVRPlugin.Hand.HandLeft));
            StartCoroutine(InitializeMeshAndSend(OVRPlugin.Hand.HandRight));
        }

        private IEnumerator KeepTcpAlive()
        {
            while (true)
            {
                try
                {
                    _tcpSender.SendKeepAlivePacket();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Keep alive error, {e}");
                }

                yield return new WaitForSeconds(SendKeepAlivePacketEveryNSeconds);
            }

        }

        void Update()
        {
            PollAndSendHandTrackingData(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft);
            PollAndSendHandTrackingData(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight);
        }

        void FixedUpdate()
        {
            PollAndSendHandTrackingData(OVRPlugin.Step.Physics, OVRPlugin.Hand.HandLeft);
            PollAndSendHandTrackingData(OVRPlugin.Step.Physics, OVRPlugin.Hand.HandRight);
        }

        private void PollAndSendHandTrackingData(OVRPlugin.Step renderStep, OVRPlugin.Hand handType)
        {
            OVRPlugin.HandState handState = default(OVRPlugin.HandState);
            if (OVRPlugin.GetHandState(renderStep, handType, ref handState))
            {
                try
                {
                    SendDataUDP(XmlSerialize.Serialize(HandRelatedDataContainer.AsHandData(
                        new HandData(renderStep, handType, handState)
                    )));
                }
                catch (Exception e)
                {
                    Debug.Log($"Unable to send hand data: {e.ToString()}");
                }
            }

        }

        private IEnumerator InitializeSkeletonAndSend(OVRPlugin.Hand hand)
        {
            var skeletonType = GetSkeletonTypeFromHandType(hand);
            OVRPlugin.Skeleton skeleton;
            if (OVRPlugin.GetSkeleton(skeletonType, out skeleton))
            {
                try
                {
                    var skeletonDataJson = XmlSerialize.Serialize(HandRelatedDataContainer.AsSkeletonData(
                        new SkeletonData(skeleton, skeletonType)
                    ));
                    StartCoroutine(SendDataTCP(skeletonDataJson)); //skeleton data is too big for single UDP packet
                    yield break;
                }
                catch (Exception e)
                {
                    Debug.Log($"Unable to send skeleton ({hand.ToString()})data: {e.ToString()}");
                }
            }
            yield return new WaitForSeconds(WaitNSecondsBetweenHandDataInitializationIfNotReady);
        }

        private IEnumerator InitializeMeshAndSend(OVRPlugin.Hand hand)
        {
            var meshType = GetHandMeshTypeFromOVRHandType(hand);
            OVRPlugin.Mesh mesh;
            if (OVRPlugin.GetMesh(meshType, out mesh))
            {
                try
                {
                    var meshDataJson = XmlSerialize.Serialize(HandRelatedDataContainer.AsMeshData(
                        new MeshData(meshType, mesh)
                    ));
                    StartCoroutine(SendDataTCP(meshDataJson)); //mesh data is too big for single UDP packet
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to send mesh data: {e.ToString()}");
                }
            }
            yield return new WaitForSeconds(WaitNSecondsBetweenHandDataInitializationIfNotReady);
        }

        private void SendDataUDP(string data)
        {
            if (_udpSender == null)
            {
                _udpSender = new UDPSender(SendToIp, SendToPort);
                _udpSender.Connected += (sender, args) =>
                {
                    Debug.Log("UDP Connected hands data endpoint");
                };
            }

            _udpSender.Send(CompressDataWithGzip ? GzipCompression.Compress(data) : data);
        }

        private IEnumerator SendDataTCP(string data)
        {
            bool shouldRetry = false;
            do
            {
                try
                {
                    _tcpSender.Send(CompressDataWithGzip ? GzipCompression.Compress(data) : data);
                    shouldRetry = false;
                }
                catch (Exception e)
                {
                    shouldRetry = true;
                }

                if (shouldRetry)
                {
                    Debug.LogError("Unable to send TCP data, waiting");
                    yield return new WaitForSeconds(WaitNSecondsBetweenTCPSendingFailures);
                }
            } while (shouldRetry);

            yield break;
        }

        private static OVRPlugin.SkeletonType GetSkeletonTypeFromHandType(OVRPlugin.Hand hand)
        {
            return hand == OVRPlugin.Hand.HandLeft ?
                OVRPlugin.SkeletonType.HandLeft :
                hand == OVRPlugin.Hand.HandRight ?
                    OVRPlugin.SkeletonType.HandRight : OVRPlugin.SkeletonType.None;
        }

        private static OVRPlugin.MeshType GetHandMeshTypeFromOVRHandType(OVRPlugin.Hand hand)
        {
            return hand == OVRPlugin.Hand.HandLeft ?
                OVRPlugin.MeshType.HandLeft :
                hand == OVRPlugin.Hand.HandRight ?
                    OVRPlugin.MeshType.HandRight : OVRPlugin.MeshType.None;
        }
    }
}