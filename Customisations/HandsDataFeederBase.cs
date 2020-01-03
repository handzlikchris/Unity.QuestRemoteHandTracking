using Assets.RemoteHandsTracking.Data;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Customisations
{
    public abstract class HandsDataFeederBase : MonoBehaviour
    {
        public abstract void ProcessData(HandData handData);
    }
}