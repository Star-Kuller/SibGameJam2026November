using FMODUnity;
using UnityEngine;

namespace _Game.Code.Features.Audio
{
    [CreateAssetMenu(menuName = "Last Shift/Gate Audio Events")]
    public sealed class GateAudioEvents : ScriptableObject
    {
        public EventReference shot;
        public EventReference zombieHit;
        public EventReference zombieDeath;
        public EventReference gateHit;
        public EventReference destruction;
        public EventReference lose;
        public EventReference button;
        public EventReference gateOpen;
        public EventReference gateClose;
        public EventReference personDeath;
        public EventReference personSaved;
        public EventReference personReturned;
        public EventReference friendlyFire;
        public EventReference personDanger;
        public EventReference order;
        public EventReference waveWarning;
        public EventReference win;
        public EventReference ambience;
    }
}
