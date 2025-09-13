using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public enum MovementType
{
    MoveJ,  // Joint movement
    MoveL   // Linear movement
}

public class UnityTrajControl : MonoBehaviour
{
    [Header("Trajectory Settings")]
    public float maxAngularVelocity = 30f; // degrees per second
    public float maxAngularAcceleration = 60f; // degrees per second squared
    public float maxLinearVelocity = 0.5f; // meters per second
    public float maxLinearAcceleration = 1.0f; // meters per second squared
    
    [Header("Set this to robot to unity movement ratio")]

    public float UnitytoRobotRatio = 1.0f;


    [Header("Blend Settings")]
    public float defaultBlendRadius = 0.0f; // meters
    
    private float[] startAngles;
    private float[] endAngles;
    private float[] AngleChanges = new float[6];
    
    private float totalTime;
    private float currentTime = 0f;
    private float[] currentTargetAngles = new float[6];
    public bool isTrajectoryActive = false;
    public MovementType currentMovementType ;
    // Trajectory completion timing
    private float trajectoryStartTime;
    private float actualCompletionTime = -1f;
    public UnityJointController robotController;
    public UnityEncoder encoder;
    public UR16eInverseKinematics inverseKinematics; // For MoveL calculations
    public UnityRobotManager Unityrobotmanager;

    // Trajectory planning state
    private TrajectoryType currentTrajectoryType = TrajectoryType.JointCubic;
    private bool currentTimeGiven = false;
    private bool currentBlendEnabled = false;
    private float currentVelocity = 30f;
    private float currentAcceleration = 60f;

    private class MovementCommand
    {
        public float[] targetAngles;
        public float velocity;
        public float acceleration;
        public float blendRadius;
        public float time;
        public MovementType movementType;
        public int sendToRobot;

        public MovementCommand(float[] angles, float vel, float acc, float blend, float t, MovementType type, int sendToRobot)
        {
            targetAngles = angles;
            velocity = vel;
            acceleration = acc;
            blendRadius = blend;
            time = t;
            movementType = type;
            sendToRobot = sendToRobot;
        }
    }

    void Start()
    {
        // Initialize currentAngles with the robot's actual position
        currentTargetAngles = encoder.GetUnityAngles(); 
    }

    void Update()
    {
        if (isTrajectoryActive)
        {
            currentTime += Time.deltaTime;
            
            if (currentTime < totalTime)
            {
                if (currentMovementType == MovementType.MoveJ)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        currentTargetAngles[i] = TrajectoryCalculator.CalculateJointTrajectory(
                            startAngles[i], 
                            AngleChanges[i], 
                            currentTime, 
                            totalTime,
                            currentTrajectoryType,
                            currentTimeGiven,
                            currentBlendEnabled,
                            currentVelocity,
                            currentAcceleration
                        );

                    }
                    robotController.ChangeUnityTargetAngles(currentTargetAngles);
                }
                else if (currentMovementType == MovementType.MoveL)
                {
                    // For MoveL, we need to calculate the current pose and update joint angles
                    // This is a simplified implementation - in practice, you'd use proper inverse kinematics
                    for (int i = 0; i < 6; i++)
                    {
                        // Simple linear interpolation for now
                        float t = currentTime / totalTime;
                        currentTargetAngles[i] = Mathf.Lerp(startAngles[i], endAngles[i], t);
                    }
                    robotController.ChangeUnityTargetAngles(currentTargetAngles);
                }

            }
            else
            {
                //robotController.ChangeUnityTargetAngles(endAngles);
                actualCompletionTime = Time.time - trajectoryStartTime;
                isTrajectoryActive = false;
                Debug.Log($"Trajectory Complete! Planned time: {totalTime:F3}s, Actual time: {actualCompletionTime:F3}s");
            }
        }
    }
    
    public void Goto(float[] target, MovementType movementType = MovementType.MoveJ, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, int sendToRobot = 0)
    {

        currentMovementType = movementType;


        
        // Use default values if not specified
        float vel = velocity < 0 ? (movementType == MovementType.MoveJ ? maxAngularVelocity : maxLinearVelocity) : velocity;
        float acc = acceleration < 0 ? (movementType == MovementType.MoveJ ? maxAngularAcceleration : maxLinearAcceleration) : acceleration;
        float blend = blendRadius < 0 ? defaultBlendRadius : blendRadius;
        
        vel = UnitytoRobotRatio * vel;
        acc = UnitytoRobotRatio * acc;

        // Get current angles from encoder
        currentTargetAngles = encoder.GetUnityAngles();
        
        startAngles = new float[6];
        System.Array.Copy(currentTargetAngles, startAngles, 6);
        
        float maxDistance = 0f;
        
        if (movementType == MovementType.MoveJ)
        {
            endAngles =  new float[6];
            System.Array.Copy(target, endAngles, 6); 
            // For MoveJ: Calculate angular distance in degrees
            for (int i = 0; i < 6; i++)
            {
                AngleChanges[i] = ((endAngles[i] - startAngles[i] + 540) % 360) - 180;
                if (AngleChanges[i] < 0.001f && AngleChanges[i] > -0.001f)
                {
                    AngleChanges[i] = 0f;
                }
                float absDistance = Mathf.Abs(AngleChanges[i]);
                maxDistance = Mathf.Max(maxDistance, absDistance);
            }
          
        }
        else if (movementType == MovementType.MoveL)
        {
            // For MoveL: Convert target pose array to transform matrix
            // Assuming target array contains [x, y, z, roll, pitch, yaw]
            Matrix4x4 targetTransform = AngleConvert.PoseToTransform(target[0], target[1], target[2], 
                                                                   target[3], target[4], target[5]);
            
            // Calculate target joint angles using inverse kinematics
            var ikSolutions = inverseKinematics.CalculateIK(targetTransform);
            if (ikSolutions.Count > 0)
            {
                endAngles = new float[6];
                System.Array.Copy(ikSolutions[0], endAngles, 6); // Use first solution
            }
            else
            {
                // If no IK solution found, use current angles
                endAngles = new float[6];
                System.Array.Copy(startAngles, endAngles, 6);
                Debug.LogWarning("No IK solution found for target pose, using current position");
            }
            
            // Calculate linear distance in meters for trajectory timing
            // Simple approximation - in practice, you'd calculate actual Cartesian distance
            for (int i = 0; i < 3; i++) // Only use position components (x, y, z)
            {
                float distance = Mathf.Abs(target[i]);
                maxDistance = Mathf.Max(maxDistance, distance);
            }
        }
        
        // Handle 8 different trajectory planning cases
        bool timeGiven = time > 0;
        bool blendEnabled = blend > 0;
        
        // Set trajectory state variables
        currentTimeGiven = timeGiven;
        currentBlendEnabled = blendEnabled;
        currentVelocity = vel;
        currentAcceleration = acc;
            
        if (movementType == MovementType.MoveJ)
        {
            if (timeGiven)
            {
                if (!blendEnabled)
                {
                    // Case 1: MOVEJ - time given, blend radius 0 (ignore max vel, max acc)
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.JointCubic; // Use cubic trajectory with given time
                    Debug.Log($"Case 1: MOVEJ with cubic trajectory, explicit time={time:F2}s, no blending");
                    }
                     
                    if (sendToRobot == 2 )
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0);
                    }

                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0);
                    }

                    
                }
                else
                {
                    // Case 2: MOVEJ - time given, blend radius not 0 (ignore max vel, max acc)
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.JointCubicBlend; // Can be changed to appropriate type
                    Debug.Log($"Case 2: MOVEJ with explicit time={time:F2}s, blend radius={blend:F3}m");
                    }
                    if (sendToRobot == 2 || sendToRobot == 3)
                    {
                            Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0.1f);
                    }
                    
                }
            }
            else
            {
                if (!blendEnabled)
                {
                    
                    // Case 3: MOVEJ - time 0, blend radius 0
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = TrajectoryCalculator.CalculateTrapezoidalJointTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.JointTrapezoidal; // Use trapezoidal trajectory
                    Debug.Log($"Case 3: MOVEJ with trapezoidal trajectory, calculated time={totalTime:F2}s, no blending");
                    }
                    if (sendToRobot == 2)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0);
                    }
                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, 0, 0);
                    }
                
                }
                else
                {
                    // Case 4: MOVEJ - time 0, blend radius not 0
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = TrajectoryCalculator.CalculateTrapezoidalJointTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.JointCubicBlend; // Can be changed to appropriate type
                    Debug.Log($"Case 4: MOVEJ with calculated time={totalTime:F2}s, blend radius={blend:F3}m");
                    }
                    if (sendToRobot == 2 || sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, 0, 0.1f);
                    }
                }
            }
        }
        else if (movementType == MovementType.MoveL)
        {
            if (timeGiven)
            {
                if (!blendEnabled)
                {
                    // Case 5: MOVEL - time given, blend radius 0 (ignore max vel, max acc)
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.LinearCubic; // Linear for MoveL
                    Debug.Log($"Case 5: MOVEL with explicit time={time:F2}s, no blending");
                    }
                    if (sendToRobot == 2)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, totalTime, 0);
                    }
                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, time, 0);
                    }
                }
                else
                {
                    // Case 6: MOVEL - time given, blend radius not 0 (ignore max vel, max acc)
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.LinearCubicBlend; // Linear for MoveL
                    Debug.Log($"Case 6: MOVEL with explicit time={time:F2}s, blend radius={blend:F3}m");
                    }
                    if (sendToRobot == 2)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, time, 0.1f);
                    }
                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, time, 0.1f);
                    }
                }
            }
            else
            {
                if (!blendEnabled)
                {
                    // Case 7: MOVEL - time 0, blend radius 0
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = TrajectoryCalculator.CalculateCubicLinearRequiredTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.LinearCubic; // Linear for MoveL
                    Debug.Log($"Case 7: MOVEL with calculated time={totalTime:F2}s, no blending");
                    }
                    if (sendToRobot == 2)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, totalTime, 0);
                    }
                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, 0, 0);
                    }
                }
                else
                {
                    // Case 8: MOVEL - time 0, blend radius not 0
                    if (sendToRobot == 1 || sendToRobot == 2)
                    {
                    totalTime = TrajectoryCalculator.CalculateCubicLinearRequiredTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.LinearCubicBlend; // Linear for MoveL
                    Debug.Log($"Case 8: MOVEL with calculated time={totalTime:F2}s, blend radius={blend:F3}m");
                    }
                    if (sendToRobot == 2)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, totalTime, 0.1f);
                    }
                    if (sendToRobot == 3)
                    {
                        Unityrobotmanager.SendMoveLCommand(target, acceleration, velocity, 0, 0.1f);
                    }
                }
            }
        }
        
        currentTime = 0f;
        trajectoryStartTime = Time.time;
        actualCompletionTime = -1f; // Reset completion time
        isTrajectoryActive = true;
        Debug.Log($"Starting {movementType} trajectory: Target Angles={string.Join(", ", target)}, Velocity={vel:F2}°/s, Acceleration={acc:F2}°/s², Time={totalTime:F2}s, Blend={blend:F3}m");
    }

    // Convenience methods for MoveJ and MoveL
    public void MoveJ(float[] targetAngles, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, int sendToRobot = 0)
    {
        //Debug.Log($"MoveJ: {string.Join(", ", targetAngles)}, {velocity}, {acceleration}, {blendRadius}, {time}");     
        Goto(targetAngles, MovementType.MoveJ, velocity, acceleration, blendRadius, time, sendToRobot);
    }
    
    public void MoveL(float[] targetPose, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, int sendToRobot = 0)
    {
        //Debug.Log($"MoveL: {string.Join(", ", targetPose)}, {velocity}, {acceleration}, {blendRadius}, {time}");
        Goto(targetPose, MovementType.MoveL, velocity, acceleration, blendRadius, time, sendToRobot);
    }
}
