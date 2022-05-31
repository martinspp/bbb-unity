using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Unity.WebRTC;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class ScreenShareController : MonoBehaviour
{
    WebSocket ws;
    MediaStream mediaStream;
    RTCPeerConnection peer;
    ShareSettings settings;
    RTCConfiguration configuration;
    public RawImage ScreenShareTexture;
    public RawImage PresentationTexture;

    [DllImport("__Internal")]
    private static extern void unityScreenShareStarted();
    [DllImport("__Internal")]
    private static extern void unityScreenShareWSConnected();

    private void Awake()
    {
        WebRTC.Initialize(EncoderType.Software);
        mediaStream = new MediaStream();
    }
    private void Start()
    {
        configuration = new RTCConfiguration();
        configuration.iceServers = new RTCIceServer[]
        {
            new RTCIceServer()
            {
                urls = new string[] { "stun:stun.l.google.com:19302" }
            }
        };
#if UNITY_WEBGL && !UNITY_EDITOR
        // disable WebGLInput.captureAllKeyboardInput so elements in web page can handle keyboard inputs
        WebGLInput.captureAllKeyboardInput = false;
        unityScreenShareStarted();
#endif
    }
    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (ws != null)
            {
                ws.DispatchMessageQueue();

            }
#endif
    }
    private void OnDestroy()
    {
        WebRTC.Dispose();
        mediaStream.Dispose();
        if(ws != null && ws.State == WebSocketState.Open)
            ws.Close();
    }
    async public void SettingsInit(string param)
    {
        settings = JsonUtility.FromJson<ShareSettings>(param);
        if (ws == null) {
#if UNITY_WEBGL && !UNITY_EDITOR
            ws = new WebSocket(settings.wsUrl);
#else
            ws = new WebSocket(settings.wsUrl, headers: new Dictionary<string, string>() { ["Cookie"] = settings.cookie });
#endif
            ws.OnMessage += handleMessage;
            ws.OnOpen += () => {
                unityScreenShareWSConnected();
                Debug.Log("ws open");
            };
            ws.OnError += (e) => {
                Debug.Log("error: " + e);
            };
            ws.OnClose += (e) => {
                Debug.Log("closed");
            };
            await ws.Connect();
        }
    }

    public void ScreenshareStart()
    {
        ScreenShareTexture.gameObject.SetActive(true);
        PresentationTexture.gameObject.SetActive(false);
        if(ws.State != WebSocketState.Open)
        {
            Debug.Log("websocket not open!");
            return;
        }
        
        peer = new RTCPeerConnection(ref configuration);
        
        peer.OnTrack = e =>
        {
            mediaStream.AddTrack(e.Track);
        };
        mediaStream.OnAddTrack = e =>
        {
            if (e.Track is VideoStreamTrack track)
            {
                ScreenShareTexture.texture = track.InitializeReceiver(1280, 720);
            }
        };
        peer.OnIceCandidate = candidate => handleCandidate(candidate);

        StartCoroutine(WebRTC.Update());
        SendInitialOffering();
        Debug.Log("start");
      
    }
    public void ScreenshareStop()
    {
        ScreenShareTexture.gameObject.SetActive(false);
        PresentationTexture.gameObject.SetActive(true);

        peer.Close();
        ScreenShareTexture.texture = Texture2D.whiteTexture;

    }
    private void handleMessage(byte[] bytes)
    {
        var message = System.Text.Encoding.UTF8.GetString(bytes);
        if (message.Contains("pong"))
            return;
        var decodedBase = JsonUtility.FromJson<KurentoBaseResponse>(message);
        switch (decodedBase.id)
        {
            case "startResponse":
                var startResponse = JsonUtility.FromJson<KurentoStartResponse>(message);
                var desc_rem = new RTCSessionDescription()
                {
                    type = RTCSdpType.Offer,
                    sdp = startResponse.sdpAnswer
                };
                peer.SetRemoteDescription(ref desc_rem);

                StartCoroutine(CreateDescription());

                break;
            case "iceCandidate":
                var iceCandidate = JsonUtility.FromJson<KurentoScreenShareIceCandidate>(message);
                RTCIceCandidateInit candidateInit = new RTCIceCandidateInit();
                candidateInit.candidate = iceCandidate.candidate.candidate;
                candidateInit.sdpMid = iceCandidate.candidate.sdpMid;
                candidateInit.sdpMLineIndex = iceCandidate.candidate.sdpMLineIndex;
                RTCIceCandidate candidate = new RTCIceCandidate(candidateInfo: candidateInit);
                peer.AddIceCandidate(candidate);
                break;
            default:
                break;
        }
    }
    private void handleCandidate(RTCIceCandidate candidate)
    {
        var iceCandidate = new KurentoOutgoingIceCandidate();
        var iceJson = new RTCIceCandidateJson();
        iceJson.candidate = candidate.Candidate;
        iceJson.sdpMid = candidate.SdpMid;
        iceJson.sdpMLineIndex = (int)candidate.SdpMLineIndex;
        iceJson.usernameFragment = candidate.UserNameFragment;
        iceCandidate.callerName = settings.callerName;
        iceCandidate.candidate = iceJson;
        ws.SendText(JsonUtility.ToJson(iceCandidate));
    }
    IEnumerator CreateDescription()
    {
        var answerOptions = new RTCOfferAnswerOptions()
        {
            iceRestart = false,
            voiceActivityDetection = false
        };

        var op = peer.CreateAnswer(ref answerOptions);
        yield return op;
        if (op.IsError)
            Debug.LogError(op.Error.message);
        else
        {
            yield return SetDescription(op.Desc);
        }
    }
    IEnumerator SetDescription(RTCSessionDescription desc)
    {
        var op = peer.SetLocalDescription(ref desc);
        yield return op;
        if (op.IsError)
        {
            Debug.LogError(op.Error.message);
        }
        else
        {
            var ans = new RTCAnswerJson()
            {
                answer = desc.sdp
            };
            ans.voiceBridge = settings.voiceBridge;
            ans.callerName = settings.callerName;
            ws.SendText(JsonUtility.ToJson(ans));

        }
    }
    async void SendInitialOffering()
    {
        if (ws.State == WebSocketState.Open)
        {
            var iko = new initialKurentoOffering();
            iko.id = "start";
            iko.type = "screenshare";
            iko.role = "recv";
            iko.InternalMeetingId = settings.InternalMeetingId;
            iko.voiceBridge = settings.voiceBridge;
            iko.userName = settings.userName;
            iko.callerName = settings.callerName;
            iko.hasAudio = false;
            await ws.SendText(JsonUtility.ToJson(iko));
        }
    }
    [Serializable]
    public class ShareSettings
    {
        public string wsUrl;
        public string cookie;
        public string callerName;
        public string InternalMeetingId;
        public string userName;
        public string voiceBridge;
    }
    public class initialKurentoOffering
    {
        public string id; //start
        public string type;  //screenshare
        public string role; // recv
        public string InternalMeetingId;
        public string voiceBridge;
        public string userName;
        public string callerName;
        public bool hasAudio;
    }
    [Serializable]
    public class KurentoScreenShareIceCandidate
    {
        public string type; // screenshare
        public string id; // iceCandidate
        public KurentoIceCandidateSignalingMessage candidate; //candidate
    }
    public class KurentoStartResponse
    {
        public string type;
        public string role;
        public string id;
        public string response;
        public string sdpAnswer;
    }
    [Serializable]
    public class KurentoIceCandidateSignalingMessage
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string __module__;
        public string __type__;
    }
    public class KurentoBaseResponse
    {
        public string type;
        public string id;
    }
    [Serializable]
    public class KurentoOutgoingIceCandidate
    {
        public string id = "iceCandidate";
        public string role = "recv";
        public string type = "screenshare";
        public string voiceBridge;
        public RTCIceCandidateJson candidate;
        public string callerName;
    }
    [Serializable]
    public class RTCIceCandidateJson
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string usernameFragment;
    }
    public class RTCAnswerJson
    {
        public string id = "subscriberAnswer";
        public string type = "screenshare";
        public string role = "recv";
        public string voiceBridge;
        public string callerName;
        public string answer;
    }
}
