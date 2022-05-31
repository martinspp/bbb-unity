using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WebXR;


public class Pointer : MonoBehaviour
{
    public Canvas canvas;
    
    public WebXRController RController;

    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public Camera handCamera;

    public RectTransform cursorTransform;
    public RectTransform presentationTransform;

    private float timer = 0.2f;
    [DllImport("__Internal")]
    private static extern void unityPresentationNextSlide(); 
    [DllImport("__Internal")]
    private static extern void unityPresentationPreviousSlide();

    [DllImport("__Internal")]
    private static extern void unityPresentationSendCursor(float xPercent, float yPercent);


    private void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (MultiplayerController.settings != null && MultiplayerController.settings.isPresenter)
        {
            // Prepare anchors for raycasting
            cursorTransform.anchorMax = new Vector2(0.5f ,1f);
            cursorTransform.anchorMin = new Vector2(0.5f, 1f);

            List<RaycastResult> results = new List<RaycastResult>();

            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = new Vector2(handCamera.pixelWidth/2, handCamera.pixelHeight/2);

            raycaster.Raycast(pointerEventData, results);

            Vector3 rectpos = new Vector3(-1,-1);
            foreach (RaycastResult result in results)
            {
                rectpos = presentationTransform.transform.InverseTransformPoint(result.worldPosition);
               
                cursorTransform.anchoredPosition = rectpos;

                timer += Time.deltaTime;
                
            }
            if (timer > 0.1)
            {
                if (results.Count > 0)
                {
                    // lets get min and max 
                    float maxX = presentationTransform.rect.width;
                    float maxY = presentationTransform.rect.height;

                    rectpos.x = rectpos.x + maxX / 2;
                    rectpos.y = -rectpos.y;

                    float percentX = (rectpos.x / maxX)*100;
                    float percentY = (rectpos.y / maxY)*100;

#if UNITY_WEBGL && !UNITY_EDITOR
                    unityPresentationSendCursor(percentX, percentY);
#else
                    Debug.Log(string.Format("Sending Cursor position: x: {0} {1} y: {2} {3} ", rectpos.x, percentX, rectpos.y, percentY));
#endif
                    
                }
                else
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    unityPresentationSendCursor(-1, -1);
#endif
                }

                timer = 0;
            }

            if (RController.GetButtonDown(WebXRController.ButtonTypes.ButtonA))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                unityPresentationPreviousSlide();
#else
                Debug.Log("A");
#endif
            }

            if (RController.GetButtonDown(WebXRController.ButtonTypes.Trigger))
            {

#if UNITY_WEBGL && !UNITY_EDITOR
                unityPresentationNextSlide();
#else
                Debug.Log("Next");
#endif
            }

        }
    }
}
