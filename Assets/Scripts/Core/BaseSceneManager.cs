using UnityEngine;

namespace Core
{
    public abstract class BaseSceneManager : MonoBehaviour
    {
        public abstract void Begin(PassThroughData data);
    }
}
