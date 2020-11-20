using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// This saves and restores animator properties on disable / enable.
    /// Make sure this component is higher than the animator component otherwise it won't work
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorRestorer : MonoBehaviour
    { 
        private Animator _animator;
        private List<AnimParam> _params = new List<AnimParam>();

        private bool _hasSavedParams = false;

        // ****************************************
        // UNITY
        // ****************************************

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (!_hasSavedParams)
            {
                return;
            }

            foreach (AnimParam p in _params)
            {
                switch (p.Type)
                {
                    case AnimatorControllerParameterType.Int:
                        _animator.SetInteger(p.ParamName, p.GetData<int>());
                        break;
                    case AnimatorControllerParameterType.Float:
                        _animator.SetFloat(p.ParamName, p.GetData<float>());
                        break;
                    case AnimatorControllerParameterType.Bool:
                        _animator.SetBool(p.ParamName, p.GetData<bool>());
                        break;
                }
            }
            _params.Clear();

            StartCoroutine(ExpediateAnimatorRoutine());
        }

        private void OnDisable()
        {
            SaveAnimator();
        }

        // ****************************************
        // ROUTINES
        // ****************************************

        private IEnumerator ExpediateAnimatorRoutine()
        {
            float origSpeed = _animator.speed;
            _animator.speed = 999;

            yield return null;
            yield return new WaitForEndOfFrame();

            _animator.speed = origSpeed;
        }

        // ****************************************
        // METHODS
        // ****************************************

        private void SaveAnimator()
        {
            for (int i = 0; i < _animator.parameters.Length; i++)
            {
                AnimatorControllerParameter p = _animator.parameters[i];
                AnimParam ap = new AnimParam(_animator, p.name, p.type);
                _params.Add(ap);
            }

            _hasSavedParams = true;
        }

        // ****************************************
        // CLASSES
        // ****************************************

        private class AnimParam
        {
            public AnimatorControllerParameterType Type { get; }
            public string ParamName { get; }
            public object Data { get; }

            public AnimParam(Animator anim, string paramName, AnimatorControllerParameterType type)
            {
                Type = type;
                ParamName = paramName;
            
                switch (type)
                {
                    case AnimatorControllerParameterType.Int:
                        Data = anim.GetInteger(paramName);
                        break;

                    case AnimatorControllerParameterType.Float:
                        Data = anim.GetFloat(paramName);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        Data = anim.GetBool(paramName);
                        break;
                }
            }

            public T GetData<T>()
            {
                return (T)Data;
            }
        }
    }
}
