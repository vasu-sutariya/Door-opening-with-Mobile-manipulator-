using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class test : MonoBehaviour
{
    public Unity_2F_85 unity_2F_85;
    public Robot_2f_85 robot_2f_85;
    public UnityTrajControl Unitytrajcontrol;
    public UnityRobotManager Unityrobotmanager;
    public UR16eInverseKinematics ur16eInverseKinematics;
    public UnityJointController Unityjointcontroller;
    public UnityEncoder Unityencoder; 
    public float[] jointangles = new float[6];
    public Vector3 position = new Vector3(0,0,0);
    public Vector3 rotation = new Vector3(0,0,0);
    public int gripperposition = 0;
    public int gripperposition2 = 85;

    private bool isExecutingSequence = false;

    void Update()
    {

        
        Unityjointcontroller.ChangeUnityTargetAngles(Unityrobotmanager.GetJointPositions());

        if (Input.GetKeyDown(KeyCode.A))
        {

            Debug.Log("target: " + position + " rotation: " + rotation);
            List<float[]> solutions = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x,position.y,position.z,rotation.x,rotation.y,rotation.z ));
            Debug.Log("solutions: " + solutions.Count );
            if(solutions.Count > 5)
            {
                jointangles = solutions[5];
                Unitytrajcontrol.MoveJ(jointangles, 15f, 20f, 0, 0, 3);
            }
            else if(solutions.Count < 5 && solutions.Count > 0)
            {
                jointangles = solutions[0];
                Unitytrajcontrol.MoveJ(jointangles, 15f, 20f, 0, 0, 3);
            }
            else if(solutions.Count == 0)
            {
                Debug.Log("No IK solutions found");
            }
           
             
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (isExecutingSequence)
            {
                Debug.Log("Sequence already executing. Please wait for completion.");
                return;
            }
            
            StartCoroutine(ExecutePickAndPlaceSequence());
        }

        if (Input.GetKeyDown(KeyCode.L))
        {

            Unitytrajcontrol.Goto(jointangles, MovementType.MoveL, 15f, 20f, 0, 0, 3);
           
             
        }


        if (Input.GetKeyDown(KeyCode.B))
        {

            unity_2F_85.MoveGripperToPosition(gripperposition,20,false,255);
             
        }
        if (Input.GetKeyDown(KeyCode.N))
        {

            unity_2F_85.MoveGripperToPosition(gripperposition2,20,false,255);
             
        }

        if (Input.GetKeyDown(KeyCode.C))
        {

            jointangles = Unityencoder.GetUnityAngles();
            Debug.Log("End Effector Pose: " + ur16eInverseKinematics.GetEndEffectorPose(jointangles));
             
        }
        
    }

    private IEnumerator ExecutePickAndPlaceSequence()
    {
        isExecutingSequence = true;
        Debug.Log("Starting pick and place sequence...");

        // Move to first pose and open gripper
        Debug.Log("Step 1: Moving to first pose and opening gripper");
        position = new Vector3(0.24f, -0.52f, 1.05f);
        rotation = new Vector3(90f, -90f, 0f);
        List<float[]> solutions1 = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x, position.y, position.z, rotation.x, rotation.y, rotation.z));
        if (solutions1.Count > 0)
        {
            jointangles = solutions1[0];
            Unitytrajcontrol.MoveJ(jointangles, 5f, 5f, 0, 0, 3); // sendToRobot = 3
            yield return StartCoroutine(WaitForMovementComplete());
        }
        unity_2F_85.MoveGripperToPosition(gripperposition, 20, false, 255); // Gripper open
        yield return StartCoroutine(WaitForGripperComplete());

        // Move to second pose and close gripper
        Debug.Log("Step 2: Moving to second pose and closing gripper");
        position = new Vector3(0.24f, -0.57f, 1.05f);
        rotation = new Vector3(90f, -90f, 0f);
        List<float[]> solutions2 = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x, position.y, position.z, rotation.x, rotation.y, rotation.z));
        if (solutions2.Count > 0)
        {
            jointangles = solutions2[0];
            Unitytrajcontrol.MoveJ(jointangles, 5f, 5f, 0, 0, 3); // sendToRobot = 3
            yield return StartCoroutine(WaitForMovementComplete());
        }
        unity_2F_85.MoveGripperToPosition(gripperposition2, 20, false, 255); // Gripper close
        yield return StartCoroutine(WaitForGripperComplete());

        // Move to third pose
        Debug.Log("Step 3: Moving to third pose");
        position = new Vector3(0.24f, -0.57f, 0.99f);
        rotation = new Vector3(90f, -69f, 0f);
        List<float[]> solutions3 = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x, position.y, position.z, rotation.x, rotation.y, rotation.z));
        if (solutions3.Count > 0)
        {
            jointangles = solutions3[0];
            Unitytrajcontrol.MoveJ(jointangles, 5f, 5f, 0, 0, 3); // sendToRobot = 3
            yield return StartCoroutine(WaitForMovementComplete());
        }

        // Move to fourth pose
        Debug.Log("Step 4: Moving to fourth pose");
        position = new Vector3(0.24f, -0.52f, 0.99f);
        rotation = new Vector3(90f, -69f, 0f);
        List<float[]> solutions4 = ur16eInverseKinematics.CalculateIK(AngleConvert.PoseToTransform(position.x, position.y, position.z, rotation.x, rotation.y, rotation.z));
        if (solutions4.Count > 0)
        {
            jointangles = solutions4[0];
            Unitytrajcontrol.MoveJ(jointangles, 5f, 5f, 0, 0, 3); // sendToRobot = 3
            yield return StartCoroutine(WaitForMovementComplete());
        }

        Debug.Log("Pick and place sequence completed!");
        isExecutingSequence = false;
    }

    private IEnumerator WaitForGripperComplete()
    {
        if (unity_2F_85 != null)
        {
            float maxWaitTime = 5.0f; // maximum wait time in seconds
            float waitTime = 0f;
            bool gripperStartedMoving = false;
            
            Debug.Log("Waiting for gripper to complete movement...");
            
            // First, wait for gripper to start moving (in_position becomes false)
            while (unity_2F_85.IsGripperInPosition() && waitTime < 1.0f)
            {
                yield return new WaitForSeconds(0.05f);
                waitTime += 0.05f;
            }
            
            if (!unity_2F_85.IsGripperInPosition())
            {
                gripperStartedMoving = true;
                Debug.Log("Gripper started moving");
            }
            
            // Reset wait time for completion check
            waitTime = 0f;
            
            // Now wait for gripper to complete movement (in_position becomes true)
            if (gripperStartedMoving)
            {
                while (!unity_2F_85.IsGripperInPosition() && waitTime < maxWaitTime)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
                
                if (unity_2F_85.IsGripperInPosition())
                {
                    Debug.Log("Gripper movement completed");
                }
                else
                {
                    Debug.LogWarning($"Gripper movement timeout after {maxWaitTime}s");
                }
            }
            else
            {
                Debug.LogWarning("Gripper did not start moving within 1 second");
            }
        }
        else
        {
            // Fallback: wait a fixed time if no gripper
            Debug.LogWarning("No gripper found, using fallback wait time");
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator WaitForMovementComplete()
    {
        if (Unityencoder != null)
        {
            float tolerance = 0.01f; // degrees tolerance for angle matching
            bool anglesMatch = false;
            int maxWaitTime = 10; // maximum wait time in seconds
            float waitTime = 0f;
            
            Debug.Log($"Waiting for movement to complete. Target angles: [{string.Join(", ", jointangles)}]");
            
            while (!anglesMatch && waitTime < maxWaitTime)
            {
                float[] currentAngles = Unityrobotmanager.GetJointPositions();
                anglesMatch = true;
                
                // Check if all angles are within tolerance
                for (int i = 0; i < 6; i++)
                {
                    float angleDifference = Mathf.Abs(currentAngles[i] - jointangles[i]);
                    // Handle angle wrapping (e.g., 359° and 1° should be considered close)
                    if (angleDifference > 180f)
                    {
                        angleDifference = 360f - angleDifference;
                    }
                    
                    if (angleDifference > tolerance)
                    {
                        
                        anglesMatch = false;
                        break;
                    }
                }
                
                if (!anglesMatch)
                {
                    yield return new WaitForSeconds(0.1f); // Check every 100ms
                    waitTime += 0.1f;
                }
            }
            
            if (anglesMatch)
            {
                Debug.Log("Movement completed - angles match target");
            }
            else
            {
                Debug.LogWarning($"Movement timeout after {maxWaitTime}s - angles may not have reached target");
            }
        }
        else
        {
            // Fallback: wait a fixed time if no encoder
            Debug.LogWarning("No encoder found, using fallback wait time");
            yield return new WaitForSeconds(3.0f);
        }
    }
}

