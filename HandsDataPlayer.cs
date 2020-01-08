using System;
using System.Collections.Generic;
using System.Linq;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.RemoteHandsTracking
{
    public class HandsDataPlayer : MonoBehaviour
    {
        private const int ManuallyMoveToFrameDisabledValue = -1;

        [Serializable] public class HandDataProcessingUnityEvent : UnityEvent<HandData> { }
        [Serializable] public class SkeletonDataProcessingUnityEvent : UnityEvent<SkeletonData> { }
        [Serializable] public class MeshDataProcessingUnityEvent : UnityEvent<MeshData> { }

        public HandDataProcessingUnityEvent HandDataProcessing = new HandDataProcessingUnityEvent();
        public SkeletonDataProcessingUnityEvent SkeletonDataProcessing = new SkeletonDataProcessingUnityEvent();
        public MeshDataProcessingUnityEvent MeshDataProcessing = new MeshDataProcessingUnityEvent();

        [ShowOnly]
        public int CurrentlyPlayedFrame;

        [Tooltip("Set to -1 to disable")]

        public int ManuallyMoveToFrame = ManuallyMoveToFrameDisabledValue;

        public HandsDataRecording CurrentPlayedRecording;
        
        private Queue<HandsDataRecordedFrame> _framesToPlay;
        private HandsDataRecordedFrame _currentlyPlayedFrame;
        private bool _isSkeletonDataProcessed;
        private bool _isMeshDataProcessed;

        public void Play(HandsDataRecording recording)
        {
            ManuallyMoveToFrame = ManuallyMoveToFrameDisabledValue;
            CurrentPlayedRecording = recording;
            _framesToPlay = new Queue<HandsDataRecordedFrame>(recording.HandsDataRecordedFrames);
            CurrentlyPlayedFrame = 0;
        }

        [EditorButton]
        public void Replay()
        {
            if (CurrentPlayedRecording != null)
            {
                Play(CurrentPlayedRecording);
            }
        }

        [EditorButton]
        public void StopPlaying()
        {
            _framesToPlay = null;
            _currentlyPlayedFrame = null;
        }

        private void Start()
        {
            ManuallyMoveToFrame = ManuallyMoveToFrameDisabledValue;
        }

        private void FixedUpdate()
        {
            SetCurrentlyPlayedFrame();

            if (_currentlyPlayedFrame == null) return;

            if (!_isSkeletonDataProcessed)
            {
                ProcessSkeletonData(CurrentPlayedRecording.InitHandData.LeftHandSkeletonData);
                ProcessSkeletonData(CurrentPlayedRecording.InitHandData.RightHandSkeletonData);
                _isSkeletonDataProcessed = true;
            }

            ProcessHandData(_currentlyPlayedFrame.LeftHandPhysicsUpdate);
            ProcessHandData(_currentlyPlayedFrame.RightHandPhysicsUpdate);
        }

        private void Update()
        {
            if (_currentlyPlayedFrame == null) return;

            if (!_isMeshDataProcessed)
            {
                ProcessMeshData(CurrentPlayedRecording.InitHandData.LeftHandMeshData);
                ProcessMeshData(CurrentPlayedRecording.InitHandData.RightHandMeshData);
                _isMeshDataProcessed = true;
            }

            ProcessHandData(_currentlyPlayedFrame.LeftHandRenderUpdate);
            ProcessHandData(_currentlyPlayedFrame.RightHandRenderUpdate);
        }

        private void SetCurrentlyPlayedFrame()
        {
            if (CurrentPlayedRecording != null && ManuallyMoveToFrame != ManuallyMoveToFrameDisabledValue)
            {
                ManuallyMoveToFrame = Mathf.Clamp(ManuallyMoveToFrame, ManuallyMoveToFrameDisabledValue, CurrentPlayedRecording.HandsDataRecordedFrames.Count);
                _currentlyPlayedFrame = CurrentPlayedRecording.HandsDataRecordedFrames[ManuallyMoveToFrame < 0 ? 0 : ManuallyMoveToFrame];
            }
            else
            {
                if (_framesToPlay != null && _framesToPlay.Any())
                {
                    _currentlyPlayedFrame = _framesToPlay.Dequeue();
                    CurrentlyPlayedFrame++;
                }
                else
                {
                    _currentlyPlayedFrame = null;
                }
            }
        }

        private void ProcessSkeletonData(SkeletonData skeletonData)
        {
            if (skeletonData != null) SkeletonDataProcessing?.Invoke(skeletonData);
        }

        private void ProcessMeshData(MeshData meshData)
        {
            if (meshData != null) MeshDataProcessing?.Invoke(meshData);
        }

        private void ProcessHandData(HandData handData)
        {
            if (handData != null) HandDataProcessing?.Invoke(handData);
        }
    }
}
