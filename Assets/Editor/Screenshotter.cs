using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

/*
	AUTHOR: Stijn Raaijmakers @bugshake
	
	Custom editor window which will save a screenshot in the play windows resolution.
	For example if you have a 1080p screen, but want a 4k screenshot, just set that resolution in the Game window
	and the file will be a full 3840x2160 pixels
*/
public class Screenshotter : EditorWindow
{
    List<string> fileHistory = new List<string>();
    Vector2 scrollPos = Vector2.zero;

    [MenuItem("Tools/Screenshotter...")]
    public static void ShowWindow()
    {
        var win = EditorWindow.GetWindow<Screenshotter>(false, "Screenshotter");
    }

    void OnGUI()
    {
        string screenshotPath = string.Format("{0}/Screenshots", Application.persistentDataPath);
        if (GUILayout.Button("Capture"))
        {
            Directory.CreateDirectory(screenshotPath);
            string path = string.Format("{0}/s_{1:yyyy_MM_dd hh_mm_ss}.png", screenshotPath, DateTime.Now);
            //ScreenCapture.CaptureScreenshot(path);
            SaveScreenshot(path);
            fileHistory.Add(path);

        }
        if (GUILayout.Button("SCREENSHOTS:"))
        {
            Directory.CreateDirectory(screenshotPath);
            EditorUtility.RevealInFinder(screenshotPath + "/");
        }
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100f));
        for (int i = fileHistory.Count; i-- > 0;)
        {
            GUILayout.Label(fileHistory[i]);
        }
        GUILayout.EndScrollView();
    }

    public int UpScale = 4;
    public bool AlphaBackground = true;

    Texture2D Screenshot()
    {
        var camera = Camera.main;
        int w = camera.pixelWidth * UpScale;
        int h = camera.pixelHeight * UpScale;
        var rt = new RenderTexture(w, h, 32);
        camera.targetTexture = rt;
        var screenShot = new Texture2D(w, h, TextureFormat.ARGB32, false);
        var clearFlags = camera.clearFlags;
        if (AlphaBackground)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
        }
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        screenShot.Apply();
        camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        camera.clearFlags = clearFlags;
        return screenShot;
    }


    public void SaveScreenshot(string path)
    {
        //var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //var filename = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".png";
        File.WriteAllBytes(path, Screenshot().EncodeToPNG());
    }
}
