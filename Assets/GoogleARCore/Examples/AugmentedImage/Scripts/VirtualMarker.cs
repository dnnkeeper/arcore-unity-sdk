using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GoogleARCore;

public class VirtualMarker : MonoBehaviour
{
    public int databaseIndex;

    public AugmentedImageDatabase imageDatabase;

    private Material _imageMaterial;
    private Material imageMaterial{
        get {
            if (_imageMaterial == null)
            {
                _imageMaterial = new Material(Shader.Find("Diffuse"));
            }
            return _imageMaterial;
        }
    }

#if UNITY_EDITOR

    [ContextMenu("UpdateImageByIndex")]
    public void UpdateImage()
    {
        UpdateImage(databaseIndex);
    }

    public void UpdateImage(int idx)
    {
        var imageGameObject = transform.Find("MarkerRenderer");
        if (imageGameObject == null)
        {
            var newGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            newGameObject.name = "MarkerRenderer";
            imageGameObject = newGameObject.transform;
        }

        imageGameObject.parent = transform;
        imageGameObject.localPosition = Vector3.zero;
        imageGameObject.localRotation = Quaternion.Euler(90f, 0f, 0f);
        imageGameObject.localScale = Vector3.one * 0.3f;

        if (imageDatabase != null && idx >= 0 && imageDatabase.Count > idx)
        {
            var imgInfo = imageDatabase[idx];
            if (imgInfo.Texture != null)
            {
                imageMaterial.mainTexture = imageDatabase[idx].Texture;
                imageGameObject.GetComponent<Renderer>().material = imageMaterial;
                if (imgInfo.Width != 0f)
                {
                    imageGameObject.localScale = new Vector3(imgInfo.Width, imgInfo.Width * imgInfo.Texture.height / imgInfo.Texture.width, 1f);

                }
                Debug.Log("imgInfo.Width "+ imgInfo.Width);
            }
            else
            {
                Debug.LogWarning("imageDatabase[idx].Texture == null");
            }
        }
        else
        {
            imageMaterial.mainTexture = null;
            imageGameObject.GetComponent<Renderer>().material = imageMaterial;
        }
    }
#endif
}
