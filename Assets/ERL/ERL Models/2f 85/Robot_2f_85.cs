using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class Robot_2f_85 : MonoBehaviour
{ 
    
    [Header("Gripper Settings")]
    public int gripperSpeed = 255;
    public int gripperForce = 255;


    [Header("Robot Connection Settings")]
    public string robotIP = "192.168.12.100";
    public int gripperPort = 63352;    
    private TcpClient gripperClient;
    private NetworkStream gripperStream;
    private bool isGripperConnected = false;

    void Start()
    {
        //ConnectToGripper();
    }

    public void ConnectToGripper()
    {
        try
        {
            Debug.Log($"Attempting to connect to UR10 gripper port at {robotIP}:{gripperPort}");
            gripperClient = new TcpClient();
            gripperClient.Connect(robotIP, gripperPort);
            gripperStream = gripperClient.GetStream();
            isGripperConnected = true;
        }
        catch (SocketException e)
        {
            Debug.LogError($"Gripper port connection error: {e.Message}");
        }
    }

       // Send URScript string directly to robot
    public void SendURScript(string script)
    {
        if (!isGripperConnected || gripperStream == null)
        {
            Debug.LogWarning("Command connection not available");
            return;
        }

        try
        {
            // Send the script as a single command with proper formatting
            string formattedScript = script + "\n";
            byte[] scriptBytes = Encoding.UTF8.GetBytes(formattedScript);
            
            gripperStream.Write(scriptBytes, 0, scriptBytes.Length);
            gripperStream.Flush(); // Ensure data is sent immediately
            
            Debug.Log("Successfully sent URScript to gripper");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error sending URScript to gripper: {e.Message}");
        }
    }

    // Activate the gripper
    public void SetGripperActivate()
    {
            

        string activateScript = $@"
def activate_gripper():
    socket_open(""127.0.0.1"", 63352)
    sleep(0.1)
    socket_send_string(""SET ACT 1\n"")
    sleep(0.1)
    socket_send_string(""SET SPE {gripperSpeed}\n"")
    sleep(0.1)
    socket_send_string(""SET FOR {gripperForce}\n"")
    sleep(0.1)
    socket_close()
end
activate_gripper()";
        
        SendURScript(activateScript);
    }

    // Set gripper position (0-255)
    public void SetGripperPosition(int position)
    { 

        position = Mathf.Clamp(position, 0, 255);
        string moveScript = $@"
def move_gripper():
    socket_open(""127.0.0.1"", 63352)
    sleep(0.1)
    socket_send_string(""SET POS {position}\n"")
    sleep(0.1)
    socket_close()
end
move_gripper()";
        
        SendURScript(moveScript);
    }

    // Set gripper force (0-255)
    public void SetGripperForce(int force)
    { 

        force = Mathf.Clamp(force, 0, 255);
        string forceScript = $@"
def set_gripper_force():
    socket_open(""127.0.0.1"", 63352)
    sleep(0.1)
    socket_send_string(""SET FOR {force}\n"")
    sleep(0.1)
    socket_close()
end
set_gripper_force()";
        
        SendURScript(forceScript);
    }
            
    // Set gripper speed (0-255)
    public void SetGripperSpeed(int speed)
    {

        speed = Mathf.Clamp(speed, 0, 255);
        string speedScript = $@"
def set_gripper_speed():
    socket_open(""127.0.0.1"", 63352)
    sleep(0.1)
    socket_send_string(""SET SPE {speed}\n"")
    sleep(0.1)
    socket_close()
end
set_gripper_speed()";
        
        SendURScript(speedScript);
    }

 

    // Set gripper stroke in mm (0-85mm)
    public void SetGripperStroke(float stroke)
    {
        // Convert stroke (0-85mm) to gripper position (0-255)
        int position = Mathf.RoundToInt((stroke / 85.0f) * 255.0f);
        SetGripperPosition(position);
    }
}