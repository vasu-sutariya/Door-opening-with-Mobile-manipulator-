using UnityEngine;
using System;

public enum TrajectoryType
{
    JointCubic,
    JointTrapezoidal,
    JointCubicBlend,
    JointTrapezoidalBlend,
    LinearCubic,
    LinearTrapezoidal,
    LinearCubicBlend,
    LinearTrapezoidalBlend,

}

public static class TrajectoryCalculator
{
    

    

    public static float CalculateCubicLinearRequiredTime(float distance, float maxVelocity, float maxAcceleration)
    {
        // For linear movement, use the same calculation as joint movement
        return CalculateCubicJointRequiredTime(distance, maxVelocity, maxAcceleration);
    }
    public static float CalculateCubicJointRequiredTime(float distance, float maxVelocity, float maxAcceleration)
{
    return CalculateCubicJointTrajectory(0, distance, 0, 0);
}
    // Trapezoidal trajectory time calculation
    public static float CalculateTrapezoidalJointTime(float distance, float maxVelocity, float maxAcceleration)
    {
        float dq = Mathf.Abs(distance);
        float direction = Mathf.Sign(distance);
        
        // Time to accelerate to vmax
        float t_acc = maxVelocity / maxAcceleration;
        float d_acc = 0.5f * maxAcceleration * t_acc * t_acc;
        
        float T;
        if (2 * d_acc >= dq)  // Triangular profile (can't reach vmax)
        {
            t_acc = Mathf.Sqrt(dq / maxAcceleration);
            T = 2 * t_acc;  // total time
        }
        else  // Trapezoidal profile
        {
            float d_flat = dq - 2 * d_acc;
            float t_flat = d_flat / maxVelocity;
            T = 2 * t_acc + t_flat;  // total time
        }
        
            
        return T;
    }

    // Centralized trajectory calculation function
    public static float CalculateJointTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime, 
                TrajectoryType trajectoryType, bool timeGiven = false, bool blendEnabled = false, float maxVelocity = 30f, float maxAcceleration = 60f)
    {
        switch (trajectoryType)
        {
            case TrajectoryType.JointCubic:
                return CalculateCubicJointTrajectory(startAngle, shortestAngle, currentTime, totalTime);
            
            case TrajectoryType.JointTrapezoidal:
                return CalculateTrapezoidalJointTrajectory(startAngle, shortestAngle, currentTime, totalTime, timeGiven, maxVelocity, maxAcceleration);
            
            case TrajectoryType.JointCubicBlend:
                return CalculateCubicJointBlendTrajectory(startAngle, shortestAngle, currentTime, totalTime, blendEnabled);
            
            
            
            default:
                return CalculateCubicJointTrajectory(startAngle, shortestAngle, currentTime, totalTime);
        }
    }

    

    public static float CalculateCubicJointTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime)
    {
        return (float)(startAngle +
            ((3 / Math.Pow(totalTime, 2)) * shortestAngle * (currentTime * currentTime)) +
            ((-2 / Math.Pow(totalTime, 3)) * shortestAngle * Math.Pow(currentTime, 3)));
    }

    public static float CalculateCubicJointBlendTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime, bool blendEnabled)
    {
        return CalculateCubicJointTrajectory(startAngle, shortestAngle, currentTime, totalTime);
    }
    // Trapezoidal trajectory calculation
    public static float CalculateTrapezoidalJointTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime, bool timeGiven, float maxVelocity = 30f, float maxAcceleration = 60f)
    {
        // Filter out very small angles
        if (Mathf.Abs(startAngle) < 0.01f) startAngle = 0f;
        if (Mathf.Abs(shortestAngle) < 0.01f) shortestAngle = 0f;
        
        // If no movement needed, return start angle
        if (Mathf.Abs(shortestAngle) < 0.01f)
        {
            return startAngle;
        }
        
        //Debug.Log($"CalculateTrapezoidalTrajectory: {startAngle}, {shortestAngle}, {currentTime}, {totalTime}, {timeGiven}, vel={maxVelocity}, acc={maxAcceleration}");
        float dq = Mathf.Abs(shortestAngle);
        float direction = Mathf.Sign(shortestAngle);
        
        // Time to accelerate to vmax
        float t_acc = maxVelocity / maxAcceleration;
        float d_acc = 0.5f * maxAcceleration * t_acc * t_acc;
        
        float t_flat = 0f;
        if (2 * d_acc >= dq)  // Triangular profile (can't reach vmax)
        {
            //Debug.Log($"Triangular profile: dq={dq}, maxAcceleration={maxAcceleration}");
            t_acc = Mathf.Sqrt(dq / maxAcceleration);
            t_flat = 0f;
            //Debug.Log($"Triangular profile: t_acc={t_acc}, t_flat={t_flat}");
        }
        else  // Trapezoidal profile
        {
            //Debug.Log($"Trapezoidal profile: dq={dq}, maxVelocity={maxVelocity}");
            float d_flat = dq - 2 * d_acc;
            t_flat = d_flat / maxVelocity;
        }
        
        float TotalTimeLocal = 2 * t_acc + t_flat;
        float qi;
        if (currentTime < t_acc)  // Acceleration phase
        {
            qi = startAngle + direction * 0.5f * maxAcceleration * currentTime * currentTime;
            
        }
        else if (currentTime < t_acc + t_flat)  // Constant velocity
        {
            qi = startAngle + direction * (d_acc + maxVelocity * (currentTime - t_acc));
            
        }
        else if (currentTime < TotalTimeLocal)  // Deceleration phase
        {
            float td = currentTime - (t_acc + t_flat);
            qi = startAngle + shortestAngle - direction * 0.5f * maxAcceleration * (t_acc - td) * (t_acc - td);
        }
        else
        {
            qi = startAngle + shortestAngle;
            
        }
        //Debug.Log($"CalculateTrapezoidalTrajectory: qi={qi}");
        return qi;
    }



    // Centralized trajectory calculation function
    public static float CalculateLinearTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime, 
                 TrajectoryType trajectoryType, bool timeGiven = false, bool blendEnabled = false, float maxVelocity = 30f, float maxAcceleration = 60f)
    {
        switch (trajectoryType)
        {
            case TrajectoryType.LinearCubic:
                return CalculateCubicLinearTrajectory(startAngle, shortestAngle, currentTime, totalTime);
            
            case TrajectoryType.LinearTrapezoidal:
                return CalculateTrapezoidalLinearTrajectory(startAngle, shortestAngle, currentTime, totalTime, timeGiven, maxVelocity, maxAcceleration);
            
           
            default:
                return CalculateCubicLinearTrajectory(startAngle, shortestAngle, currentTime, totalTime);
        }
    }


    // Linear trajectory calculation (same as cubic for now)
    public static float CalculateCubicLinearTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime)
    {
        return (float)(startAngle +
            ((3 / Math.Pow(totalTime, 2)) * shortestAngle * (currentTime * currentTime)) +
            ((-2 / Math.Pow(totalTime, 3)) * shortestAngle * Math.Pow(currentTime, 3)));
    }

    public static float CalculateTrapezoidalLinearTrajectory(float startAngle, float shortestAngle, float currentTime, float totalTime, bool timeGiven, float maxVelocity = 30f, float maxAcceleration = 60f)
    {
        // For linear movement, use the same trapezoidal calculation as joint movement
        return CalculateTrapezoidalJointTrajectory(startAngle, shortestAngle, currentTime, totalTime, timeGiven, maxVelocity, maxAcceleration);
    }

} 