using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.VectorGraphics;
using System.IO;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class PresentationController : MonoBehaviour
{

    public RawImage presentationTexture;
    public AspectRatioFitter aspectRatioFitter;
    public RectTransform cursorTransform;
    public RectTransform canvasTransform;

    // Start is called before the first frame update
    void Start()
    {

        //UpdateSlide("");
        //UpdateCursor("{\"xPercent\":15,\"yPercent\":20}");
    }


    void UpdateSlide(string base64)
    {
        Texture2D texture = B64ToTex(base64);
        presentationTexture.texture = texture;
        aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
    }
   
    Texture2D B64ToTex(string base64)
    {
        byte[] Bytes = Convert.FromBase64String(base64.Split(',')[1]);
        var texture2d = new Texture2D(2, 2);
        texture2d.LoadImage(Bytes);
        return texture2d;
    }
    void BlankPresentation()
    {
        presentationTexture.texture = Texture2D.whiteTexture;
    }
    void UpdateCursor(string json)
    {
        // if we are the presenter we send not receive
        if (!MultiplayerController.settings.isPresenter)
        {
            // Prepare anchors for receiving data
            cursorTransform.anchorMax = Vector2.zero;
            cursorTransform.anchorMin = Vector2.zero;
            float maxX = canvasTransform.sizeDelta.x + presentationTexture.rectTransform.sizeDelta.x;
            float maxY = canvasTransform.sizeDelta.y + presentationTexture.rectTransform.sizeDelta.y;

            var cursorUpdate = JsonUtility.FromJson<CursorUpdate>(json);
            if (cursorUpdate.xPercent < 0 || cursorUpdate.yPercent < 0)
            {
                cursorTransform.gameObject.SetActive(false);
                return;
            }
            else
                cursorTransform.gameObject.SetActive(true);
            cursorTransform.anchoredPosition = new Vector2(maxX * (cursorUpdate.xPercent / 100), maxY * ((100 - cursorUpdate.yPercent) / 100));
        }
    }
    [Serializable]
    public class CursorUpdate
    {
        public float xPercent;
        public float yPercent;
    }
}


