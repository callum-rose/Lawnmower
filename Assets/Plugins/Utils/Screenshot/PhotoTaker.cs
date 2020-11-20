#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Utils.Screenshot
{
    public class PhotoTaker : MonoBehaviour
    {
        [SerializeField] int scale = 1;
        [SerializeField] string suffix = "screen_shot";

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.P))
            //    TakePhoto();

            if (Input.GetKeyDown(KeyCode.S))
                TakeScreenShot();
        }

        public void TakeScreenShot()
        {
            string mainPath = Application.dataPath;

            if (!Directory.Exists(mainPath + "Photos"))
            {
                Directory.CreateDirectory(mainPath + "Photos");
            }

            int screenShotNum = PlayerPrefs.GetInt("screen_shot", 0);
            string path = string.Format(
                mainPath + @"Photos\{0}_{1}_{2}x{3}@{4}x.png",
                suffix,
                screenShotNum,
                Screen.currentResolution.width,
                Screen.currentResolution.height,
                scale
            );

            int temp = Screen.width;

            ScreenCapture.CaptureScreenshot(path, scale);

            PlayerPrefs.SetInt("screen_shot", ++screenShotNum);

            Debug.Log("Screenshot saved to path: " + path);
        }
    }
}
#endif