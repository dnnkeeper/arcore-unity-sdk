using UnityEngine;
using System.Collections;
//using UnityEngine.Rendering.PostProcessing;

public class CameraResolution : MonoBehaviour
{
    public float maxWidth = 720;

    void Start()
    {
        float maxCurrentResolution = Mathf.Max((float)Screen.width, (float)Screen.height);

        float scale = maxWidth / maxCurrentResolution;

        Screen.SetResolution((int)(Screen.width * scale), (int)(Screen.height * scale), FullScreenMode.FullScreenWindow, 60);

    }

    //private void OnGUI()
    //{
    //    GUILayout.BeginHorizontal();
    //    GUILayout.FlexibleSpace();
    //    //GUILayout.Label(Screen.width + ":" + Screen.height);
    //    GUILayout.EndHorizontal();
    //}
}