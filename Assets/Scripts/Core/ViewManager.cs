using System;
using Sirenix.OdinInspector;
using System.Collections;
using Game.Core;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core
{
    [CreateAssetMenu(fileName = nameof(ViewManager), menuName = SONames.GameDir + nameof(ViewManager))]
    public partial class ViewManager : Singleton<ViewManager>
    {
        private UnityScene CurrentScene { get; set; } = UnityScene.None;

        private LoadData _nextLoadData;

        private Coroutine _loadRoutine;

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if (!WasAppStartedByViewManager)
            {
                return;
            }

            SetAppWasStartedByViewManager(false);
            
            Instance.Load(Instance.sceneToOpen, Instance.GetData());
        }

        #region API

        public void Load(UnityScene scene, object data = null)
        {
            Assert.IsFalse(scene == UnityScene.None);

            _nextLoadData = new LoadData(scene, data);

            _loadRoutine ??= StartCoroutine(ProcessLoadsRoutine());
        }

        #endregion

        #region Routines

        private IEnumerator ProcessLoadsRoutine()
        {
            do
            {
                StartCoroutine(UnloadRoutine(CurrentScene));

                LoadData temp = _nextLoadData;
                _nextLoadData = null;
                yield return StartCoroutine(LoadRoutine(temp));
            }
            while (_nextLoadData != null);

            _loadRoutine = null;
        }

        private IEnumerator LoadRoutine(LoadData data)
        {
            int buildIndex = GetBuildIndex(data.Scene);

            AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
            op.allowSceneActivation = true;
            yield return op;

            CurrentScene = data.Scene;

            Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);

            try
            {
                FindAndStartBaseSceneManager(loadedScene, data.Data);
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private IEnumerator UnloadRoutine(UnityScene scene)
        {
            if (scene == UnityScene.None)
            {
                yield break;
            }

            int buildIndex = GetBuildIndex(scene);

            AsyncOperation op = null;
            try
            {
                op = SceneManager.UnloadSceneAsync(buildIndex);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (op == null)
            {
                yield break;
            }

            yield return op;
        }

        #endregion

        #region Methods

        private static void FindAndStartBaseSceneManager(Scene loadedScene, object data)
        {
            foreach (GameObject root in loadedScene.GetRootGameObjects())
            {
                BaseSceneManager bsm = root.GetComponentInChildren<BaseSceneManager>();
                if (bsm != null)
                {
                    bsm.Begin(data);
                    break;
                }
            }
        }

        private static int GetBuildIndex(UnityScene scene)
        {
            return (int)scene;
        }

        #endregion

        #region Classes

        private class LoadData
        {
            public LoadData(UnityScene scene, object data)
            {
                Scene = scene;
                Data = data;
            }

            public UnityScene Scene { get; }
            public object Data { get; }
        }

        #endregion
    }
}
