using System;
using System.Linq;
using Assets.RemoteHandsTracking.Data;
using OculusSampleFramework;
using UnityEngine;

namespace Assets.RemoteHandsTracking
{
    public abstract class HandsDataFeederBase: MonoBehaviour
    {
        public abstract void ProcessData(HandData handData);
    }

    public class HandsDataProcessor: MonoBehaviour
    {
        public int HoldNHistoricDataEntries = 100;

        public FixedSizedQueue<HandData> HistoricData;
        public HandsDataFeederBase HandsDataFeederImplementation;

        public void Start()
        {
            HistoricData = new FixedSizedQueue<HandData>(HoldNHistoricDataEntries);
        }

        public void ProcessData(HandData handData)
        {
            ProcessData(handData, true);
        } 

        public void ProcessData(HandData handData, bool captureInHistory)
        {
            if (captureInHistory)
            {
                HistoricData.Enqueue(handData);
            }

            HandsDataFeederImplementation.ProcessData(handData);
        }

        public void ReprocessHistoricEntries()
        {
            var historicDataSnapshot = HistoricData.ToList();
            foreach (var historicHandData in historicDataSnapshot)
            {
                ProcessData(historicHandData, false);
            }
        }
    }
}