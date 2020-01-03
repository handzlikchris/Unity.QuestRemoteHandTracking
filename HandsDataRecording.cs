using System;
using System.Collections.Generic;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Utilities;
using UnityEngine;

namespace Assets.RemoteHandsTracking
{
    public class InitHandData
    {
        public SkeletonData LeftHandSkeletonData { get; set; }
        public SkeletonData RightHandSkeletonData { get; set; }

        public MeshData RightHandMeshData { get; set; }
        public MeshData LeftHandMeshData { get; set; }

        public bool AreAllAssigned => LeftHandSkeletonData != null && RightHandSkeletonData != null &&
                                      LeftHandMeshData != null && RightHandMeshData != null;

        public InitHandData(SkeletonData leftHandSkeletonData, SkeletonData rightHandSkeletonData, MeshData rightHandMeshData, MeshData leftHandMeshData)
        {
            LeftHandSkeletonData = leftHandSkeletonData;
            RightHandSkeletonData = rightHandSkeletonData;
            RightHandMeshData = rightHandMeshData;
            LeftHandMeshData = leftHandMeshData;
        }

        [Obsolete("Required for serialization")]
        public InitHandData()
        {
        }
    }

    public class PersistedHandsDataRecording
    {
        public string Name { get; set; }
        public List<HandsDataRecordedFrame> HandsDataRecordedFrames { get; set; }
        public InitHandData InitHandData { get; set; }


        public PersistedHandsDataRecording(string name, List<HandsDataRecordedFrame> handsDataRecordedFrames, InitHandData initHandData)
        {
            Name = name;
            HandsDataRecordedFrames = handsDataRecordedFrames;
            InitHandData = initHandData;
        }

        [Obsolete("Required for serialization")]
        public PersistedHandsDataRecording()
        {
        }
    }

    public class HandsDataRecording : MonoBehaviour
    {
        public List<HandsDataRecordedFrame> HandsDataRecordedFrames { get; private set; }
        public string RecordingName { get; private set; }

        private Action<HandsDataRecording> _delete;
        private Action<HandsDataRecording> _play;
        public InitHandData InitHandData { get; private set; }


        public static HandsDataRecording Create(Transform parentContainer, string recordingName, List<HandsDataRecordedFrame> handsDataRecordedFrames, 
            InitHandData initHandData, Action<HandsDataRecording> delete, Action<HandsDataRecording> play)
        {
            var recording = new GameObject(recordingName);
            recording.transform.parent = parentContainer;

            var handsDataRecording = recording.AddComponent<HandsDataRecording>();
            handsDataRecording.HandsDataRecordedFrames = handsDataRecordedFrames;
            handsDataRecording._delete = delete;
            handsDataRecording._play = play;
            handsDataRecording.InitHandData = initHandData;
            handsDataRecording.RecordingName = recordingName;

            return handsDataRecording;
        }

        [EditorButton]
        public void Play()
        {
            _play(this);
        }

        [EditorButton]
        public void Delete()
        {
            _delete(this);
        }
    }
}