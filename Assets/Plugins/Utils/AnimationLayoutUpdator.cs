using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    [ExecuteInEditMode]
    public class AnimationLayoutUpdator : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var layout = GetComponentInChildren<LayoutGroup>();
            layout.enabled = false;
            layout.enabled = true;
        }
#endif
    }
}
