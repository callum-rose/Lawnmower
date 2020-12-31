using UnityEngine;

namespace Game.Mowers
{
    public class MowerManager : MonoBehaviour
    {
        [SerializeField] private MowerMovementManager movement;

        public MowerMovementManager Movement => movement;
    }
}
