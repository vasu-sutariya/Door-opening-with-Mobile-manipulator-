using UnityEngine;
using System.Collections.Generic;

public class test : MonoBehaviour
{
    public Unity_2F_85 unity_2F_85;
    public Robot_2f_85 robot_2f_85;
    public UnityTrajControl Unitytrajcontrol;
    public UnityRobotManager Unityrobotmanager;
    public UR16eInverseKinematics ur16eInverseKinematics;
    public UnityEncoder Unityencoder;
    public float[] jointangles = new float[6];
    public Vector3 position = new Vector3(0,0,0);
    public Vector3 rotation = new Vector3(0,0,0);
    public int gripperposition = 0;


  

    void Update()
    {



        if (Input.GetKeyDown(KeyCode.A))
        {

            Debug.Log("target: " + position + " rotation: " + rotation);
            List<float[]> solutions = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x,position.y,position.z,rotation.x,rotation.y,rotation.z ));
            Debug.Log("solutions: " + solutions.Count );
            if(solutions.Count > 5)
            {
                jointangles = solutions[5];
                Unitytrajcontrol.MoveJ(jointangles, 15f, 20f, 0, 0, false);
            }
            else if(solutions.Count < 5 && solutions.Count > 0)
            {
                jointangles = solutions[0];
                Unitytrajcontrol.MoveJ(jointangles, 15f, 20f, 0, 0, false);
            }
            else if(solutions.Count == 0)
            {
                Debug.Log("No IK solutions found");
            }
           
             
        }

        if (Input.GetKeyDown(KeyCode.B))
        {

            unity_2F_85.MoveGripperToPosition(gripperposition,20,false,255);
             
        }

        if (Input.GetKeyDown(KeyCode.C))
        {

            jointangles = Unityencoder.GetUnityAngles();
            Debug.Log("End Effector Pose: " + ur16eInverseKinematics.GetEndEffectorPose(jointangles));
             
        }
        
    }
}

