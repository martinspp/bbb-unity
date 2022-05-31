using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;
using System.Runtime.InteropServices;
using TMPro;


public class MultiplayerController : MonoBehaviour
{
    WebSocket ws;
    public static Settings settings;
    private Dictionary<String,Transform[]> otherplayers = new Dictionary<string, Transform[]>(); //Head, L, R, Root, Text
    
    private Dictionary<String,Vector3[]> from_posTargets = new Dictionary<string,Vector3[]>();
    private Dictionary<String, Vector3[]> to_posTargets = new Dictionary<string, Vector3[]>();

    private Dictionary<String, Vector3[]> from_rotTargets = new Dictionary<string, Vector3[]>();
    private Dictionary<String, Vector3[]> to_rotTargets = new Dictionary<string, Vector3[]>();

    public Transform pasniedzejuGalds;
    public Transform publicPoint;
    public List<Transform> galdi;
    public Transform Head;
    public Transform LController;
    public Transform RController;
    public Transform WebXRCameraSet;
    public GameObject Prefab;
    bool on = false;
    float lerp = 0.0f;
    float serverUpdateTime = 0.1f;

    [DllImport("__Internal")]
    private static extern void unityMultiplayerStarted();
    // Start is called before the first frame update
    void Start()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        string meetingId = "\"96adfba374267bf44310db6a1d42bd5c5bf74e69-1652989867314\"";
        SettingsInit("{\"username\":\"testuser\",\"isPresenter\":true,\"id\": \"add\",\"meetingId\": "+meetingId+",\"playerId\": \"yyyy\",\"wsUrl\":\"wss://bbb.4eks1s.com:8765\"}");
        #endif
        #if UNITY_WEBGL && !UNITY_EDITOR
            unityMultiplayerStarted();
        #endif
    }
    private void OnDestroy()
    {
        ws.Close();
    }

    public async void SettingsInit(string json)
    {
        settings = JsonUtility.FromJson<Settings>(json);

        ws = new WebSocket(settings.wsUrl);
        ws.OnMessage += Ws_OnMessage;
        ws.OnOpen += () => {
            Debug.Log("connection opened");
            ws.SendText(JsonUtility.ToJson(new WSAddMessage()
            {
                meetingId = settings.meetingId,
                playerId = settings.playerId,
                username = settings.username,
                isPresenter = settings.isPresenter,
            }));
            on = true;
        };
        ws.OnError += (e) =>
        {
            Debug.Log(e);
        };
        await ws.Connect();
        Debug.Log(ws.State.ToString());
    }
    public void UpdatePresenter(string userId)
    {
        ws.SendText(JsonUtility.ToJson(new WSUpdatePresenterSendMessage()
        {
            meetingId = settings.meetingId,
            playerId = userId,
            username = "",
            isPresenter = true,
        }));
        
    }

    private void Ws_OnMessage(byte[] bytes)
    {
        //Debug.Log(Time.de)
        var message = System.Text.Encoding.UTF8.GetString(bytes);
        var updateMessge = JsonUtility.FromJson<WSUpdateRecieveMessage>(message);   
        var found = false;
        for (int i = 0; i < updateMessge.seating.Length; i++)
        {
            // Update This player seating
            if(updateMessge.seating[i] == settings.playerId)
            {
                found = true;
                if(i == 0)
                {
                    WebXRCameraSet.position = pasniedzejuGalds.position;
                    MultiplayerController.settings.isPresenter = true;
                    break;
                }
                else{
                    WebXRCameraSet.position = galdi[i-1].position;
                    MultiplayerController.settings.isPresenter = false;
                    break;
                }
            }
        }
        if (!found)
        {
            WebXRCameraSet.position = publicPoint.position;
            MultiplayerController.settings.isPresenter = false;
        }


        Dictionary<string, bool> playerExistance = new Dictionary<string, bool>();
        foreach (var player in otherplayers)
        {
            playerExistance.Add(player.Key, false);
        }


        foreach (var player in updateMessge.update)
        {
            
            
            if (player.playerId == settings.playerId)
                continue;
            if(otherplayers.TryGetValue(player.playerId, out var otherPlayer))
            {
                playerExistance[player.playerId] = true;

                from_posTargets.TryGetValue(player.playerId, out var from_positions);
                from_rotTargets.TryGetValue(player.playerId, out var from_rotations);

                to_posTargets.TryGetValue(player.playerId, out var to_positions);
                to_rotTargets.TryGetValue(player.playerId, out var to_rotations);

                from_posTargets[player.playerId] = (Vector3[])to_positions.Clone();
                from_rotTargets[player.playerId] = (Vector3[])to_rotations.Clone();


                to_positions[0] = new Vector3((float)player.Head[0], (float)player.Head[1], (float)player.Head[2]);
                to_positions[1] = new Vector3((float)player.LController[0], (float)player.LController[1], (float)player.LController[2]);
                to_positions[2] = new Vector3((float)player.RController[0], (float)player.RController[1], (float)player.RController[2]);

                to_rotations[0] = new Vector3((float)player.Head[3], (float)player.Head[4], (float)player.Head[5]);
                to_rotations[1] = new Vector3((float)player.LController[3], (float)player.LController[4], (float)player.LController[5]);
                to_rotations[2] = new Vector3((float)player.RController[3], (float)player.RController[4], (float)player.RController[5]);

                otherPlayer[4].rotation = Quaternion.LookRotation(otherPlayer[4].position - Head.position);
                
            }
            else // Player doesnt exist needs to be created
            {
                playerExistance.Add(player.playerId, true);
                var t = new Transform[5];
                
                var prefab = GameObject.Instantiate(Prefab);

                //var transforms = prefab.transform.GetChild(0);
                
                t[0] = prefab.transform.GetChild(0);
                t[1] = prefab.transform.GetChild(1);
                t[2] = prefab.transform.GetChild(2);
                t[3] = prefab.transform;
                // set username
                t[4] = prefab.transform.GetChild(0).GetChild(0).GetComponent<Transform>();
                prefab.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>().text = player.username;

                otherplayers.Add(player.playerId, t);
                from_posTargets.Add(player.playerId, new Vector3[3] {
                    new Vector3 ((float)player.Head[0], (float)player.Head[1], (float)player.Head[2]),
                    new Vector3 ((float)player.LController[0], (float)player.LController[1], (float)player.LController[2]),
                    new Vector3 ((float)player.RController[0], (float)player.RController[1], (float)player.RController[2]),
                });
                from_rotTargets.Add(player.playerId, new Vector3[3] {
                    new Vector3((float)player.Head[3], (float)player.Head[4],(float) player.Head[5]),
                    new Vector3((float)player.LController[3], (float)player.LController[4], (float)player.LController[5]),
                    new Vector3((float)player.RController[3], (float)player.RController[4], (float)player.RController[5]),
                });

                to_posTargets.Add(player.playerId, new Vector3[3] {
                    new Vector3 ((float)player.Head[0], (float)player.Head[1], (float)player.Head[2]),
                    new Vector3 ((float)player.LController[0], (float)player.LController[1], (float)player.LController[2]),
                    new Vector3 ((float)player.RController[0], (float)player.RController[1], (float)player.RController[2]),
                });
                to_rotTargets.Add(player.playerId, new Vector3[3] {
                    new Vector3((float)player.Head[3], (float)player.Head[4],(float) player.Head[5]),
                    new Vector3((float)player.LController[3], (float)player.LController[4], (float)player.LController[5]),
                    new Vector3((float)player.RController[3], (float)player.RController[4], (float)player.RController[5]),
                });

            }
        }
        foreach (var player in playerExistance)
        {
            if (!player.Value)
            {
                otherplayers.TryGetValue(player.Key,out var playerTransform);
                if (playerTransform != null)
                    Destroy(playerTransform[3].gameObject);
                otherplayers.Remove(player.Key);
                from_posTargets.Remove(player.Key);
                from_rotTargets.Remove(player.Key);
                to_posTargets.Remove(player.Key);
                to_rotTargets.Remove(player.Key);
            }
        }
        lerp = 0.0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (ws.State != WebSocketState.Open || !on)
            return;
        var sendMessage = new WSUpdateSendMessage()
        {
            meetingId = settings.meetingId,
            playerId = settings.playerId,
            Head = TransformToArray(Head),
            LController = TransformToArray(LController),
            RController = TransformToArray(RController)
        };
        ws.SendText(JsonUtility.ToJson(sendMessage));
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
#endif
    }
    
    private void Update()
    {
        foreach (KeyValuePair<string, Transform[]> entry in otherplayers)
        {
            from_posTargets.TryGetValue(entry.Key, out var from_positions);
            to_posTargets.TryGetValue(entry.Key, out var to_positions);

            entry.Value[0].position = Vector3.Lerp(from_positions[0], to_positions[0], lerp / serverUpdateTime);
            entry.Value[1].position = Vector3.Lerp(from_positions[1], to_positions[1], lerp / serverUpdateTime);
            entry.Value[2].position = Vector3.Lerp(from_positions[2], to_positions[2], lerp / serverUpdateTime);


            from_rotTargets.TryGetValue(entry.Key, out var from_rotations);
            to_rotTargets.TryGetValue(entry.Key, out var to_rotations);

            
            entry.Value[0].rotation = Quaternion.Slerp(Quaternion.Euler(from_rotations[0]), Quaternion.Euler(to_rotations[0]), lerp / serverUpdateTime);
            entry.Value[1].rotation = Quaternion.Slerp(Quaternion.Euler(from_rotations[1]), Quaternion.Euler(to_rotations[1]), lerp / serverUpdateTime);
            entry.Value[2].rotation = Quaternion.Slerp(Quaternion.Euler(from_rotations[2]), Quaternion.Euler(to_rotations[2]), lerp / serverUpdateTime);
            lerp += Time.deltaTime;
        }
    }
    float[] TransformToArray(Transform transform)
    {
        return new float[] { transform.position.x, transform.position.y, transform.position.z, transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z };
    }



    [Serializable]
    public class Settings
    {
        public string playerId;
        public string meetingId;
        public string username;
        public string wsUrl;
        public bool isPresenter;
    }

    [Serializable]
    public class WSAddMessage
    {
        public string id = "add";
        public string meetingId;
        public string playerId;
        public bool isPresenter;
        public string username;
    }
    [Serializable]
    public class WSUpdateSendMessage
    {
        public string id = "update";
        public string meetingId;
        public string playerId;
        public string username;
        public float[] LController;
        public float[] RController;
        public float[] Head;
    }
    [Serializable]
    public class WSUpdatePresenterSendMessage
    {
        public string id = "updatePresenter";
        public string meetingId;
        public string playerId;
        public string username;
        public bool isPresenter;
    }
    [Serializable]
    public class WSUpdateRecieveMessage
    {
        public string[] seating;
        public WSUpdatePlayerReceiveMessage[] update;
    }

    [Serializable]
    public class WSUpdatePlayerReceiveMessage
    {
        public string playerId;
        public float[] LController;
        public float[] RController;
        public float[] Head;
        public string username;
    }
}