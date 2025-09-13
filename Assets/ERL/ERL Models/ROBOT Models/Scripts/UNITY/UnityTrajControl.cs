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
    
    // Trajectory completion timing
    private float trajectoryStartTime;
    private float actualCompletionTime = -1f;
    public UnityJointController robotController;
    public UnityEncoder encoder;
    public UR16eInverseKinematics inverseKinematics; // For MoveL calculations
    public UnityRobotManager Unityrobotmanager;

    // Trajectory planning state
    private TrajectoryType currentTrajectoryType = TrajectoryType.Cubic;
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
        public bool sendToRobot;

        public MovementCommand(float[] angles, float vel, float acc, float blend, float t, MovementType type)
        {
            targetAngles = angles;
            velocity = vel;
            acceleration = acc;
            blendRadius = blend;
            time = t;
            movementType = type;
            sendToRobot = true;
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
                for (int i = 0; i < 6; i++)
                {
                    currentTargetAngles[i] = TrajectoryCalculator.CalculateTrajectory(
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
            else
            {
                //robotController.ChangeUnityTargetAngles(endAngles);
                actualCompletionTime = Time.time - trajectoryStartTime;
                isTrajectoryActive = false;
                Debug.Log($"Trajectory Complete! Planned time: {totalTime:F3}s, Actual time: {actualCompletionTime:F3}s");
            }
        }
    }
    
    public void Goto(float[] target, MovementType movementType = MovementType.MoveJ, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, bool sendToRobot = false)
    {

        


        
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
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.Cubic; // Use cubic trajectory with given time
                    Debug.Log($"Case 1: MOVEJ with cubic trajectory, explicit time={time:F2}s, no blending");
                    if (sendToRobot)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0);
                    }

                    
                }
                else
                {
                    // Case 2: MOVEJ - time given, blend radius not 0 (ignore max vel, max acc)
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.Cubic; // Can be changed to appropriate type
                    Debug.Log($"Case 2: MOVEJ with explicit time={time:F2}s, blend radius={blend:F3}m");
                }
            }
            else
            {
                if (!blendEnabled)
                {
                    // Case 3: MOVEJ - time 0, blend radius 0
                    totalTime = TrajectoryCalculator.CalculateTrapezoidalTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.Trapezoidal; // Use trapezoidal trajectory
                    Debug.Log($"Case 3: MOVEJ with trapezoidal trajectory, calculated time={totalTime:F2}s, no blending");
                    if (sendToRobot)
                    {
                        Unityrobotmanager.SendMoveJCommand(target, acceleration, velocity, totalTime, 0);
                    }
                
                }
                else
                {
                    // Case 4: MOVEJ - time 0, blend radius not 0
                    totalTime = TrajectoryCalculator.CalculateRequiredTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.Cubic; // Can be changed to appropriate type
                    Debug.Log($"Case 4: MOVEJ with calculated time={totalTime:F2}s, blend radius={blend:F3}m");
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
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.Linear; // Linear for MoveL
                    Debug.Log($"Case 5: MOVEL with explicit time={time:F2}s, no blending");
                }
                else
                {
                    // Case 6: MOVEL - time given, blend radius not 0 (ignore max vel, max acc)
                    totalTime = time;
                    currentTrajectoryType = TrajectoryType.Linear; // Linear for MoveL
                    Debug.Log($"Case 6: MOVEL with explicit time={time:F2}s, blend radius={blend:F3}m");
                }
            }
            else
            {
                if (!blendEnabled)
                {
                    // Case 7: MOVEL - time 0, blend radius 0
                    totalTime = TrajectoryCalculator.CalculateRequiredTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.Linear; // Linear for MoveL
                    Debug.Log($"Case 7: MOVEL with calculated time={totalTime:F2}s, no blending");
                }
                else
                {
                    // Case 8: MOVEL - time 0, blend radius not 0
                    totalTime = TrajectoryCalculator.CalculateRequiredTime(maxDistance, vel, acc);
                    currentTrajectoryType = TrajectoryType.Linear; // Linear for MoveL
                    Debug.Log($"Case 8: MOVEL with calculated time={totalTime:F2}s, blend radius={blend:F3}m");
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
    public void MoveJ(float[] targetAngles, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, bool sendToRobot = false)
    {
        //Debug.Log($"MoveJ: {string.Join(", ", targetAngles)}, {velocity}, {acceleration}, {blendRadius}, {time}");     
        Goto(targetAngles, MovementType.MoveJ, velocity, acceleration, blendRadius, time, sendToRobot);
    }
    
    public void MoveL(float[] targetPose, float velocity = -1f, float acceleration = -1f, float blendRadius = -1f, float time = -1f, bool sendToRobot = false)
    {
        //Debug.Log($"MoveL: {string.Join(", ", targetPose)}, {velocity}, {acceleration}, {blendRadius}, {time}");
        Goto(targetPose, MovementType.MoveL, velocity, acceleration, blendRadius, time, sendToRobot);
    }
}
