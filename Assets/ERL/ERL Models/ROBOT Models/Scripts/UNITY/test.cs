using UnityEngine;

public class test : MonoBehaviour
{
    public Unity_2F_85 unity_2F_85;
    public Robot_2f_85 robot_2f_85;
    public UnityTrajControl Unitytrajcontrol;
    public UnityRobotManager Unityrobotmanager;

    void Start()
    {
        Unitytrajcontrol.MoveJ(new float[] {0f,-90f,-90f,0f, 0f,0f}, 15f, 20f );
        Unityrobotmanager.SendMoveJCommand(new float[] {0f,-90f,-90f,0f, 0f,0f}, 15f, 20f);
    }

    void Update()
    {



        if (Input.GetKeyDown(KeyCode.A))
        {

            Unitytrajcontrol.MoveJ(new float[] {0f,-90f,-130f,0f, 0f,0f}, 15f, 20f );
            Unityrobotmanager.SendMoveJCommand(new float[] {0f,-90f,-130f,0f, 0f,0f}, 15f, 20f);
    







            robot_2f_85.SetGripperPosition(0);
            unity_2F_85.MoveGripperToPosition(0);
        }
        
        
    }
}

