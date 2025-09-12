using UnityEngine;
using System.IO;

public class Robot_2f_85 : MonoBehaviour
{
    [Header("Robot Manager Reference")]
    public UnityRobotManager robotManager;
    
    [Header("Script File Path")]
    public string gripperScriptPath = "Gripper.script";
    
    
    public void SetGripperPosition(int position)
    {
        // Clamp position to valid range
        position = Mathf.Clamp(position, 0, 220);
        
        try
        {
            // Get the full path to the script file in StreamingAssets
            // Remove "StreamingAssets/" prefix if present to avoid double path
            string fileName = gripperScriptPath.Replace("StreamingAssets/", "").Replace("StreamingAssets\\", "");
            string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
            
            // Read the current script file
            string scriptContent = File.ReadAllText(fullPath);
            
            // Replace the rq_move_and_wait line with new position
            // Use regex to find any rq_move_and_wait line regardless of current value
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"rq_move_and_wait\(\d+\)");
            string newLine = $"rq_move_and_wait({position})";
            
            if (regex.IsMatch(scriptContent))
            {
                scriptContent = regex.Replace(scriptContent, newLine);
                
                // Write the modified content back to the file
                File.WriteAllText(fullPath, scriptContent);
                
                Debug.Log($"Updated gripper script with position: {position}");
                
                // Send the modified script to the robot
                if (robotManager != null)
                {
                    robotManager.SendScriptFile(fullPath);
                }
                else
                {
                    Debug.LogError("Robot Manager reference is not assigned!");
                }
            }
            else
            {
                Debug.LogError("Could not find rq_move_and_wait line in script file");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting gripper position: {e.Message}");
        }
    }
    
    /// <summary>
    /// Convenience method to open gripper (position 0)
    /// </summary>
    public void OpenGripper()
    {
        SetGripperPosition(0);
    }
    
    /// <summary>
    /// Convenience method to close gripper (position 220)
    /// </summary>
    public void CloseGripper()
    {
        SetGripperPosition(220);
    }
}
