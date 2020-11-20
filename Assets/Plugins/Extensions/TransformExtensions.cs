using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalsamicBits.Extensions
{
    public static class TransformExtensions
    {
        public static Transform FindRecursively(this Transform transform, string n)
        {
            foreach (Transform child in transform)
            {
                if (child.name == n)
                    return child;

                Transform nextChild = FindRecursively(child, n);
                if (nextChild != null)
                    return nextChild;
            }

            return null;
        }

        public static void DestroyAllChildren(this Transform transform)
        {
            _DestroyAllChildren(transform, UnityEngine.Object.Destroy);

        }

        public static void DestroyAllChildrenImmediate(this Transform transform)
        {
            _DestroyAllChildren(transform, UnityEngine.Object.DestroyImmediate);
        }

        private static void _DestroyAllChildren(Transform transform, Action<UnityEngine.Object> destroyAction)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in transform)
            {
                children.Add(child);
            }
            children.ForEach(t => destroyAction(t.gameObject));
        }
    }
}
