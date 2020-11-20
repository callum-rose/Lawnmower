using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
 
namespace Tests
{
    public class SceneTests
    {
        [UnityTest]
        public IEnumerator AllScenesContainOneCanvasWithTagDialogCanvas()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var op = SceneManager.LoadSceneAsync(i);
                op.allowSceneActivation = true;

                yield return op;

                Scene scene = SceneManager.GetActiveScene();
                
                //GameObject.FindGameObjectsWithTag(UnityTags)
            }
        }
    }
}
