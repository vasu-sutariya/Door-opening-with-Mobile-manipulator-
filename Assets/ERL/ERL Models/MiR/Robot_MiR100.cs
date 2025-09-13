using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Robot_MiR100 : MonoBehaviour
{
    [Header("MiR Robot Configuration")]
    public string ipAddress = "192.168.12.20";
    public string authorizationToken = "Basic ZGlzdHJpYnV0b3I6NjJmMmYwZjFlZmYxMGQzMTUyYzk1ZjZmMDU5NjU3NmU0ODJiYjhlNDQ4MDY0MzNmNGNmOTI5NzkyODM0YjAxNA==";
    
    [Header("Mission IDs")]
    public string relativeMoveMissionId = "5dbf8298-f102-11ee-8d89-00012983ef4e";
    public string relativeMoveActionId = "da39a61c-f104-11ee-8d89-00012983ef4e";
    public string deliveryWaypointId = "047263f0-6d1d-11f0-aecb-00012983ef4e";
    public string pickupWaypointId = "3fb86c6f-e3c5-11ef-9e78-00012983ef4e";
    
    private string host;
    private Dictionary<string, string> headers;
    
    [System.Serializable]
    public class RobotStatus
    {
        public string state;
        public PositionData position;
    }
    
    [System.Serializable]
    public class PositionData
    {
        public float orientation;
    }
    
    [System.Serializable]
    public class MissionRequest
    {
        public string mission_id;
    }
    
    [System.Serializable]
    public class RelativeMoveRequest
    {
        public MissionRequest mission_id;
        public int priority = 0;
        public Parameter[] parameters;
    }
    
    [System.Serializable]
    public class Parameter
    {
        public float value;
        public string id;
    }
    
    void Start()
    {
        host = "http://" + ipAddress + "/api/v2.0.0/";
        
        headers = new Dictionary<string, string>
        {
            {"Content-Type", "application/json"},
            {"Authorization", authorizationToken},
            {"Accept-Language", "en_US"}
        };
    }
    
    public void GetRobotStatus(System.Action<string, float> onStatusReceived)
    {
        StartCoroutine(GetStatusCoroutine(onStatusReceived));
    }
    
    private IEnumerator GetStatusCoroutine(System.Action<string, float> onStatusReceived)
    {
        string statusUrl = host + "status";
        
        using (UnityWebRequest request = UnityWebRequest.Get(statusUrl))
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    RobotStatus statusData = JsonUtility.FromJson<RobotStatus>(request.downloadHandler.text);
                    string missionState = statusData.state;
                    float yaw = statusData.position.orientation;
                    
                    Debug.Log("Mission State: " + missionState);
                    onStatusReceived?.Invoke(missionState, yaw);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing status data: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error getting robot status: " + request.error);
            }
        }
    }
    
    public void RelativelyMove(float x, float y, float th)
    {
        StartCoroutine(RelativelyMoveCoroutine(x, y, th));
    }
    
    private IEnumerator RelativelyMoveCoroutine(float x, float y, float th)
    {
        MissionRequest missionId = new MissionRequest { mission_id = relativeMoveMissionId };
        
        RelativeMoveRequest relativeMove = new RelativeMoveRequest
        {
            mission_id = missionId,
            priority = 0,
            parameters = new Parameter[]
            {
                new Parameter { value = x, id = "x" },
                new Parameter { value = y, id = "y" },
                new Parameter { value = th, id = "orientation" }
            }
        };
        
        string jsonData = JsonUtility.ToJson(relativeMove);
        string url = host + "missions/" + relativeMoveMissionId + "/actions/" + relativeMoveActionId + "/";
        
        using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Post to mission queue
                yield return StartCoroutine(PostToMissionQueue(missionId));
            }
            else
            {
                Debug.LogError("Error in relative move: " + request.error);
            }
        }
    }
    
    public void DeliveryWaypoint()
    {
        MissionRequest missionId = new MissionRequest { mission_id = deliveryWaypointId };
        StartCoroutine(PostToMissionQueue(missionId));
    }
    
    public void PickupWaypoint()
    {
        MissionRequest missionId = new MissionRequest { mission_id = pickupWaypointId };
        StartCoroutine(PostToMissionQueue(missionId));
    }
    
    private IEnumerator PostToMissionQueue(MissionRequest missionId)
    {
        string jsonData = JsonUtility.ToJson(missionId);
        string url = host + "mission_queue";
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, jsonData))
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Mission posted to queue successfully");
            }
            else
            {
                Debug.LogError("Error posting to mission queue: " + request.error);
            }
        }
    }
    
    // Example usage - can be called from other scripts or UI buttons
    public void TestRelativeMove()
    {
        RelativelyMove(0.2f, 0f, 0f);
    }
}
