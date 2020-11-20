using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    public class MaterialSwapper : MonoBehaviour
    {
        [SerializeField] private string initialShaderName = "UI/Default";
        [SerializeField] private Material finalMaterial;

#if UNITY_EDITOR
        public void SwapAll()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                CheckAndSwapMaterial(r);
            }

            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                CheckAndSwapMaterial(g);
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }

        private void CheckAndSwapMaterial(Renderer renderer)
        {
            if (renderer.sharedMaterial.shader.name.Equals(initialShaderName)) 
            {
                renderer.sharedMaterial = finalMaterial;

                Debug.Log($"{GetNameInHeirachy(renderer.transform)}: \"{initialShaderName}\" to \"{finalMaterial.name}\"");
            }
        }

        private void CheckAndSwapMaterial(Graphic graphic)
        {
            if (graphic.material.shader.name.Equals(initialShaderName))
            {
                graphic.material = finalMaterial;

                Debug.Log($"{GetNameInHeirachy(graphic.transform)}: \"{initialShaderName}\" to \"{finalMaterial.name}\"");
            }
        }

        private string GetNameInHeirachy(Transform trans)
        {
            Stack<Transform> transStack = new Stack<Transform>();
            transStack.Push(trans);

            void AddParent(Transform child)
            {
                if (child.parent == null)
                {
                    // done
                    return;
                }

                transStack.Push(child.parent);
                // recursion
                AddParent(child.parent);
            }

            AddParent(trans);

            StringBuilder sb = new StringBuilder();
            while (true)
            {
                sb.Append(transStack.Pop().name);

                if (transStack.Count == 0)
                {
                    break;
                }

                sb.Append(">");
            }

            return sb.ToString();
        }
#endif
    }
}