using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core
{
    public abstract class ScriptableObjectWithCoroutines : ScriptableObject
    {
        private CoroutineSurrogate ___routiner;
        protected CoroutineSurrogate Routiner
        {
            get
            {
                AssertAppPlaying();
                return ___routiner != null ? ___routiner : ___routiner = CreateCoroutineSurrogate();
            }
        }

        #region Methods

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            AssertAppPlaying();
            return Routiner.StartCoroutine(routine);
        }

        protected void StopCoroutine(Coroutine routine)
        {
            AssertAppPlaying();

            if (routine == null)
            {
                return;
            }

            Routiner.StopCoroutine(routine);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private CoroutineSurrogate CreateCoroutineSurrogate()
        {
            AssertAppPlaying();

            CoroutineSurrogate routiner = new GameObject(nameof(CoroutineSurrogate) + "_" + name)
                .AddComponent<CoroutineSurrogate>();
            DontDestroyOnLoad(routiner);
            return routiner;
        }

        private static void AssertAppPlaying()
        {
            Assert.IsTrue(Application.isPlaying, "Application is not playing");
        }

        #endregion
    }
}
