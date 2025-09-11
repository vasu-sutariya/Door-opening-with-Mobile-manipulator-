using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

public class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}

[System.Serializable]
public class MiR100Position
{
    public string guid;
    public string name;
    public string created_by_id;
    public string created_by_name;
    public string created_by_user_id;
    public string created_by_user_name;
    public string created_by_application;
    public string created_by_application_version;
    public string created;
    public string modified_by_id;
    public string modified_by_name;
    public string modified_by_user_id;
    public string modified_by_user_name;
    public string modified_by_application;
    public string modified_by_application_version;
    public string modified;
    public string map_id;
    public string map_name;
    public float pos_x;
    public float pos_y;
    public float orientation;
    public string type;
    public string[] allowed_operations;
    public string[] disallowed_operations;
    public string[] allowed_operations_to_target;
    public string[] disallowed_operations_to_target;
    public string[] allowed_operations_from_target;
    public string[] disallowed_operations_from_target;
}

[System.Serializable]
public class MiR100PositionList
{
    public MiR100Position[] positions;
}

[System.Serializable]
public class MiR100Action
{
    public string action_type;
    public string description;
    public string[] allowed_operations;
    public string[] disallowed_operations;
}

[System.Serializable]
public class MiR100ActionList
{
    public MiR100Action[] actions;
}

public class Robot_MiR100 : MonoBehaviour
{
    [Header("MiR100 Connection Settings")]
    public string mir100IP = "192.168.12.100";
    public string apiKey = "OmUzYjBjNDQyOThmYzFjMTQ5YWZiZjRjODk5NmZiOTI0MjdhZTQxZTQ2NDliOTM0Y2E0OTU5OTFiNzg1MmI4NTU=";
    public int port = 8080;
    
    [Header("UI Display")]
    public Text positionsListText;
    public Text connectionStatusText;
    public Button refreshButton;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private string baseURL;
    private List<MiR100Position> positions = new List<MiR100Position>();
    private List<MiR100Action> actions = new List<MiR100Action>();
    private bool isConnected = false;
    
    void Start()
    {
        baseURL = "https://mir.com/api/v2.0.0";
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(GetPositions);
        }
        
        // Auto-connect on start
        GetPositions();
    }
    
    void Awake()
    {
        // Allow insecure connections for HTTP requests
        UnityEngine.Networking.UnityWebRequest.ClearCookieCache();
        
        // Enable insecure connections for development
        #if UNITY_EDITOR
        UnityEngine.Networking.UnityWebRequest.ClearCookieCache();
        #endif
    }
    
    public void GetPositions()
    {
        StartCoroutine(GetPositionsCoroutine());
    }
    
    private void CheckInsecureConnectionSettings()
    {
        #if !UNITY_EDITOR
        Debug.LogWarning("MiR100APIClient: Non-secure connections are disabled in Player Settings. " +
                        "To enable HTTP connections to your MiR100 robot, go to: " +
                        "Edit → Project Settings → Player → Publishing Settings → " +
                        "Configuration → Allow downloads over HTTP: Always");
        #endif
    }
    
    private IEnumerator GetPositionsCoroutine()
    {
        string url = $"{baseURL}/positions";
        
        if (showDebugInfo)
        {
            Debug.Log($"Requesting positions from: {url}");
        }
        
        // Create headers
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", $"Basic {apiKey}");
        headers.Add("Content-Type", "application/json");
        headers.Add("Accept-Language", "en_US");
        headers.Add("accept", "application/json");
        
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            // Set headers
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            // Disable SSL certificate validation for local development
            request.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Received response: {jsonResponse}");
                }
                
                try
                {
                    // Parse the JSON response
                    MiR100Position[] positionArray = JsonUtility.FromJson<MiR100PositionList>("{\"positions\":" + jsonResponse + "}").positions;
                    
                    positions.Clear();
                    positions.AddRange(positionArray);
                    
                    isConnected = true;
                    UpdateUI();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Successfully retrieved {positions.Count} positions");
                        foreach (var pos in positions)
                        {
                            Debug.Log($"Position: {pos.name} (GUID: {pos.guid}) - X:{pos.pos_x}, Y:{pos.pos_y}, Orientation:{pos.orientation}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing positions JSON: {e.Message}");
                    isConnected = false;
                    UpdateConnectionStatus("Error parsing response");
                }
            }
            else
            {
                string errorMessage = request.error;
                if (errorMessage.Contains("Insecure connection not allowed") || 
                    errorMessage.Contains("Non-secure network connections disabled"))
                {
                    Debug.LogError("MiR100APIClient: HTTP connections are disabled in Unity Player Settings. " +
                                 "To fix this, go to: Edit → Project Settings → Player → Publishing Settings → " +
                                 "Configuration → Allow downloads over HTTP: Always");
                    UpdateConnectionStatus("HTTP connections disabled in Player Settings");
                }
                else
                {
                    Debug.LogError($"Failed to get positions: {request.error}");
                    UpdateConnectionStatus($"Error: {request.error}");
                }
                isConnected = false;
            }
        }
    }
    
    public void GetPositionById(string positionId)
    {
        StartCoroutine(GetPositionByIdCoroutine(positionId));
    }
    
    public void GetActions()
    {
        StartCoroutine(GetActionsCoroutine());
    }
    
    private IEnumerator GetPositionByIdCoroutine(string positionId)
    {
        string url = $"{baseURL}/positions/{positionId}";
        
        if (showDebugInfo)
        {
            Debug.Log($"Requesting position {positionId} from: {url}");
        }
        
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", $"Basic {apiKey}");
        headers.Add("Content-Type", "application/json");
        headers.Add("Accept-Language", "en_US");
        
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            request.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                try
                {
                    MiR100Position position = JsonUtility.FromJson<MiR100Position>(jsonResponse);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Retrieved position: {position.name} - X:{position.pos_x}, Y:{position.pos_y}, Orientation:{position.orientation}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing position JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Failed to get position {positionId}: {request.error}");
            }
        }
    }
    
    private IEnumerator GetActionsCoroutine()
    {
        string url = $"{baseURL}/actions";
        
        if (showDebugInfo)
        {
            Debug.Log($"Requesting actions from: {url}");
        }
        
        // Create headers
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", $"Basic {apiKey}");
        headers.Add("Content-Type", "application/json");
        headers.Add("Accept-Language", "en_US");
        headers.Add("accept", "application/json");
        
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            // Set headers
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            // Disable SSL certificate validation for local development
            request.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Received actions response: {jsonResponse}");
                }
                
                try
                {
                    // Parse the JSON response
                    MiR100Action[] actionArray = JsonUtility.FromJson<MiR100ActionList>("{\"actions\":" + jsonResponse + "}").actions;
                    
                    actions.Clear();
                    actions.AddRange(actionArray);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Successfully retrieved {actions.Count} actions");
                        foreach (var action in actions)
                        {
                            Debug.Log($"Action: {action.action_type} - {action.description}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing actions JSON: {e.Message}");
                }
            }
            else
            {
                string errorMessage = request.error;
                if (errorMessage.Contains("Insecure connection not allowed") || 
                    errorMessage.Contains("Non-secure network connections disabled"))
                {
                    Debug.LogError("MiR100APIClient: HTTP connections are disabled in Unity Player Settings. " +
                                 "To fix this, go to: Edit → Project Settings → Player → Publishing Settings → " +
                                 "Configuration → Allow downloads over HTTP: Always");
                }
                else
                {
                    Debug.LogError($"Failed to get actions: {request.error}");
                }
            }
        }
    }
    
    private void UpdateUI()
    {
        UpdateConnectionStatus("Connected");
        UpdatePositionsList();
    }
    
    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = $"MiR100 Status: {status}";
        }
    }
    
    private void UpdatePositionsList()
    {
        if (positionsListText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MiR100 Positions ({positions.Count}):");
            sb.AppendLine("========================");
            
            if (positions.Count == 0)
            {
                sb.AppendLine("No positions found");
            }
            else
            {
                foreach (var pos in positions)
                {
                    sb.AppendLine($"• {pos.name}");
                    sb.AppendLine($"  GUID: {pos.guid}");
                    sb.AppendLine($"  Position: X={pos.pos_x:F2}, Y={pos.pos_y:F2}");
                    sb.AppendLine($"  Orientation: {pos.orientation:F2}°");
                    sb.AppendLine($"  Type: {pos.type}");
                    sb.AppendLine($"  Map: {pos.map_name}");
                    sb.AppendLine();
                }
            }
            
            positionsListText.text = sb.ToString();
        }
    }
    
    // Public methods for external access
    public List<MiR100Position> GetPositionsList()
    {
        return new List<MiR100Position>(positions);
    }
    
    public List<MiR100Action> GetActionsList()
    {
        return new List<MiR100Action>(actions);
    }
    
    public MiR100Position GetPositionByName(string name)
    {
        return positions.Find(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    public MiR100Position GetPositionByGuid(string guid)
    {
        return positions.Find(p => p.guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsConnected()
    {
        return isConnected;
    }
    
    public void SetConnectionSettings(string ip, string key, int portNumber = 8080)
    {
        mir100IP = ip;
        apiKey = key;
        port = portNumber;
        baseURL = $"https://mir.com/api/v2.0.0";
    }
    
    // Test connection method
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }
    
    private IEnumerator TestConnectionCoroutine()
    {
        string url = $"{baseURL}/status";
        
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", $"Basic {apiKey}");
        headers.Add("Content-Type", "application/json");
        headers.Add("Accept-Language", "en_US");
        
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            request.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("MiR100 connection test successful!");
                UpdateConnectionStatus("Connection Test: OK");
            }
            else
            {
                Debug.LogError($"MiR100 connection test failed: {request.error}");
                UpdateConnectionStatus($"Connection Test: Failed - {request.error}");
            }
        }
    }
}
