using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core
{
    [CreateAssetMenu(fileName = nameof(ViewManager), menuName = SONames.GameDir + nameof(ViewManager))]
    public class ViewManager : Singleton<ViewManager>
    {
        [Header("Scene To Load First")]
        [SerializeField, EnumToggleButtons, HideLabel]
        private UnityScene sceneToOpenFirst;

        private UnityScene CurrentScene { get; set; } = UnityScene.None;

        private LoadData _nextLoadData;

        private Coroutine _loadRoutine;

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            Instance.Load(Instance.sceneToOpenFirst, null);
        }

        #region API

        [Button(Expanded = true)]
        public void Load(UnityScene scene, PassThroughData data = null)
        {
            Assert.IsFalse(scene == UnityScene.None);

            _nextLoadData = new LoadData(scene, data);

            if (_loadRoutine == null)
            {
                _loadRoutine = StartCoroutine(ProcessLoadsRoutine());
            }
        }

        #endregion

        #region Routines

        private IEnumerator ProcessLoadsRoutine()
        {
            do
            {
                StartCoroutine(UnloadRoutine(CurrentScene));

                var temp = _nextLoadData;
                _nextLoadData = null;
                yield return StartCoroutine(LoadRoutine(temp));
            }
            while (_nextLoadData != null);

            _loadRoutine = null;
        }

        private IEnumerator LoadRoutine(LoadData data)
        {
            int buildIndex = GetBuildIndex(data.Scene);

            var op = SceneManager.LoadSceneAsync(buildIndex);
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

            AsyncOperation op = SceneManager.UnloadSceneAsync(buildIndex);
            yield return op;
        }

        #endregion

        #region Odin

        //[ShowInInspector, EnumToggleButtons] private UnityScene sceneToLoad;
        //[ShowInInspector] private GameSetupPassThroughData gameData = new GameSetupPassThroughData();

        #endregion

        #region Methods

        private static void FindAndStartBaseSceneManager(Scene loadedScene, PassThroughData data)
        {
            foreach (var root in loadedScene.GetRootGameObjects())
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
            public LoadData(UnityScene scene, PassThroughData data)
            {
                Scene = scene;
                Data = data;
            }

            public UnityScene Scene { get; }
            public PassThroughData Data { get; }
        }

        #endregion
    }
}
