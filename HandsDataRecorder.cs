using System;
using System.Collections.Generic;
using System.IO;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Utilities;
using UnityEngine;

namespace Assets.RemoteHandsTracking
{
    public class HandsDataRecordedFrame
    {
        public HandData LeftHandRenderUpdate { get; set; }
        public HandData RightHandRenderUpdate { get; set; }
        public HandData LeftHandPhysicsUpdate { get; set; }
        public HandData RightHandPhysicsUpdate { get; set; }

        public bool HasAnyData => LeftHandRenderUpdate != null || RightHandRenderUpdate != null 
                                                               || LeftHandPhysicsUpdate != null || RightHandPhysicsUpdate != null;
    }

    public class HandsDataRecorder : MonoBehaviour
    {
        public static string LocalPersistPath => $"{Application.persistentDataPath}/PersistedHandDataRecordings";

        public string NewRecordingName;
        public HandsDataPlayer HandsDataPlayer;

        [ShowOnly]
        public bool IsRecording;

        private List<HandsDataRecordedFrame> _handDataRecordedFrames = new List<HandsDataRecordedFrame>();
        private HandsDataRecordedFrame _currentHandDataRecorderFrame = new HandsDataRecordedFrame();

        private InitHandData _initHandData = new InitHandData();

        private void LateUpdate()
        {
            if (_currentHandDataRecorderFrame != null && _currentHandDataRecorderFrame.HasAnyData)
            {
                _handDataRecordedFrames.Add(_currentHandDataRecorderFrame);
            }
            
            _currentHandDataRecorderFrame = new HandsDataRecordedFrame();
        }

        private void Start()
        {
            LoadPersistedHandDataRecordings();
        }

        [EditorButton]
        public void StartRecording()
        {
            if (!_initHandData.AreAllAssigned)
            {
                Debug.LogError($"hand skeleton and mesh data is not loaded, headset should send those before you can start recording.");
            } 
            else
            {
                IsRecording = true;
                Debug.Log($"Recording: '{NewRecordingName}' started.");
            }
        }

        [EditorButton]
        public void StopRecording()
        {
            if (string.IsNullOrEmpty(NewRecordingName))
            {
                Debug.LogError($"You need to specify '{nameof(NewRecordingName)}'");
                return;
            }

            IsRecording = false;
            if (_handDataRecordedFrames.Count == 0)
            {
                Debug.LogError($"Recording: '{NewRecordingName}' captured 0 frames, it won't be saved.");
                return;
            }

            var recording = HandsDataRecording.Create(this.transform, NewRecordingName, _handDataRecordedFrames, _initHandData, DeleteLocalRecording, PlayRecording);
            PersistDataLocallySafe(recording.RecordingName, recording.HandsDataRecordedFrames);
            Debug.Log($"Recording: '{NewRecordingName}' competed, captured {_handDataRecordedFrames.Count} frames.");

            _currentHandDataRecorderFrame = null;
            _handDataRecordedFrames = new List<HandsDataRecordedFrame>();
            NewRecordingName = string.Empty;
        }

        public void ProcessHandData(HandData handData)
        {
            if (!IsRecording) return;

            if (handData.Step == OVRPlugin.Step.Render)
            {
                if (handData.Hand == OVRPlugin.Hand.HandLeft) _currentHandDataRecorderFrame.LeftHandRenderUpdate = handData;
                if (handData.Hand == OVRPlugin.Hand.HandRight) _currentHandDataRecorderFrame.RightHandRenderUpdate = handData;
            }

            if (handData.Step == OVRPlugin.Step.Physics)
            {
                if (handData.Hand == OVRPlugin.Hand.HandLeft) _currentHandDataRecorderFrame.LeftHandPhysicsUpdate = handData;
                if (handData.Hand == OVRPlugin.Hand.HandRight) _currentHandDataRecorderFrame.RightHandPhysicsUpdate = handData;
            }
        }

        public void ProcessSkeletonData(SkeletonData skeletonData)
        {
            switch (skeletonData.SkeletonType)
            {
                case OVRPlugin.SkeletonType.HandLeft: _initHandData.LeftHandSkeletonData = skeletonData; break;
                case OVRPlugin.SkeletonType.HandRight: _initHandData.RightHandSkeletonData = skeletonData; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ProcessMeshData(MeshData meshData)
        {
            switch (meshData.MeshType)
            {
                case OVRPlugin.MeshType.HandLeft: _initHandData.LeftHandMeshData = meshData; break;
                case OVRPlugin.MeshType.HandRight: _initHandData.RightHandMeshData = meshData; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadPersistedHandDataRecordings()
        {
            new DirectoryInfo(LocalPersistPath).Create();

            foreach (var filePath in System.IO.Directory.EnumerateFiles(LocalPersistPath))
            {
                try
                {
                    var fileContents = System.IO.File.ReadAllText(filePath);
                    var persistedHandsDataRecording = XmlSerialize.Deserialize<PersistedHandsDataRecording>(fileContents);
                    HandsDataRecording.Create(this.transform,
                        persistedHandsDataRecording.Name, persistedHandsDataRecording.HandsDataRecordedFrames, persistedHandsDataRecording.InitHandData,
                        DeleteLocalRecording, PlayRecording
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to {nameof(LoadPersistedHandDataRecordings)}");
                }
            }
        }

        private void PlayRecording(HandsDataRecording rec)
        {
            HandsDataPlayer.Play(rec);

#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = HandsDataPlayer.gameObject;
#endif
        }

        private static void DeleteLocalRecording(HandsDataRecording rec)
        {
            File.Delete(GenerateRecordingFilePath(rec.RecordingName));
            GameObject.Destroy(rec.gameObject);
        }

        private void PersistDataLocallySafe(string recordingName, List<HandsDataRecordedFrame> handsDataRecordedFrames)
        {
            try
            {
                var xml = XmlSerialize.Serialize(new PersistedHandsDataRecording(recordingName, handsDataRecordedFrames, _initHandData));
                var recordingFileInfo = new FileInfo(GenerateRecordingFilePath(recordingName));
                recordingFileInfo.Directory.Create();
                File.WriteAllText(recordingFileInfo.FullName, xml);
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to {nameof(PersistDataLocallySafe)}");
            }
        }

        private static string GenerateRecordingFilePath(string recordingName)
        {
            return $"{LocalPersistPath}/{recordingName}.xml";
        }
    }
}