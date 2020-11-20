using UnityEngine;

namespace Game.Mowers
{
    internal class MowerManager : MonoBehaviour
    {
        [SerializeField] private MowerMovementManager movement;

        public MowerMovementManager Movement => movement;
    }
}
