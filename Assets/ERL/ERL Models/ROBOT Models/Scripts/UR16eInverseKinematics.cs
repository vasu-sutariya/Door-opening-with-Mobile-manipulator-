using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;



public class UR16eInverseKinematics : MonoBehaviour
{

    [Header("Encoder")]
    [Tooltip("Unity Encoder component to get joint angles")]
    public UnityEncoder encoder;
    private float[] jointAngles;

    // Base to origin transformation for UR3
    private Matrix4x4 baseToOriginUr3;

    [Header("TCP Configuration")]
    [Tooltip("End-effector TCP position offset (x, y, z)")]
    public Vector3 tcpOffset = Vector3.zero;
    [Header("Base Transform")]
    [Tooltip("Base transform assigned from inspector")]
    public Transform baseTransform;

    [Header("Robot Dimensions")]
    [Tooltip("Distance from base to shoulder (m)")]
    public float d1 = 	0.1807f;
    [Tooltip("Length of upper arm (m)")]
    public float a2 = 	-0.4784f;
    [Tooltip("Length of forearm (m)")]
    public float a3 = -0.36f;
    [Tooltip("Distance from elbow to wrist (m)")]
    public float d4 = 0.17415f;
    [Tooltip("Distance from wrist 1 to wrist 2 (m)")]
    public float d5 = 0.11985f;
    [Tooltip("Distance from wrist 2 to tool flange (m)")]
    public float d6 = 0.11655f;

     
   

   

    public List<float[]> CalculateIK(Matrix4x4 T_desired)
    {
        if (baseTransform != null)
        {
            // Convert from Unity coordinate system to standard robotics coordinate system
            // Unity: X-right, Y-up, Z-forward
            // Standard: X-forward, Y-left, Z-up
            Vector3 standardPosition = new Vector3(
                baseTransform.position.z,  // Unity Z -> Standard X (forward)
                -baseTransform.position.x, // Unity X -> Standard Y (left, negated)
                baseTransform.position.y   // Unity Y -> Standard Z (up)
            );
            Vector3 standardRotation = new Vector3(
                baseTransform.eulerAngles.z,  // Unity Z -> Standard X
                -baseTransform.eulerAngles.x, // Unity X -> Standard Y (negated)
                -baseTransform.eulerAngles.y  // Unity Y -> Standard Z (negated)
            );
            
            baseToOriginUr3 = AngleConvert.PoseToTransform(standardPosition.x, standardPosition.y, standardPosition.z,
                                                          standardRotation.x, standardRotation.y, standardRotation.z);
        }
        else
        {
            baseToOriginUr3 = Matrix4x4.identity;
        }
        Matrix4x4 T_base = baseToOriginUr3;
        Debug.Log("T_base: " + T_base);
        Matrix4x4 T_tool = AngleConvert.PoseToTransform(tcpOffset.x, tcpOffset.y, tcpOffset.z, 0, 0, 0);

        // Transform desired pose to base frame
        Matrix4x4 T_base_inv = T_base.inverse;
        Matrix4x4 T_desired_base = T_base_inv * T_desired;
        Matrix4x4 T_tool_inv = T_tool.inverse;
        Matrix4x4 T_desired_flange = T_desired_base * T_tool_inv;

        // Extract position and orientation
        float nx = T_desired_flange[0, 0], ny = T_desired_flange[1, 0], nz = T_desired_flange[2, 0];
        float ox = T_desired_flange[0, 1], oy = T_desired_flange[1, 1], oz = T_desired_flange[2, 1];
        float ax = T_desired_flange[0, 2], ay = T_desired_flange[1, 2], az = T_desired_flange[2, 2];
        float px = T_desired_flange[0, 3], py = T_desired_flange[1, 3], pz = T_desired_flange[2, 3];

        List<float[]> solutions = new List<float[]>();

        // θ1 (two solutions: shoulder left/right)
        float m = d6 * ay - py;
        float n = ax * d6 - px;
        float R = Mathf.Sqrt(m * m + n * n);
        if (R < Mathf.Abs(d4))
        {
            UnityEngine.Debug.Log("theta 1 error: R < d4");
            return solutions;
        }
        float[] theta1_options = new float[]
        {
            Mathf.Atan2(m, n) - Mathf.Atan2(d4, Mathf.Sqrt(R * R - d4 * d4)),
            Mathf.Atan2(m, n) - Mathf.Atan2(d4, -Mathf.Sqrt(R * R - d4 * d4))
        };

        foreach (float theta1 in theta1_options)
        {
            float s1 = Mathf.Sin(theta1), c1 = Mathf.Cos(theta1);

            // θ5 (two solutions: wrist up/down)
            float arg = ax * s1 - ay * c1;
            if (Mathf.Abs(arg) > 1)
            {
                UnityEngine.Debug.Log("theta 5 error: arg > 1");
                continue;
            }
            float[] theta5_options = new float[] { Mathf.Acos(arg), -Mathf.Acos(arg) };

            foreach (float theta5 in theta5_options)
            {
                float s5 = Mathf.Sin(theta5), c5 = Mathf.Cos(theta5);

                // θ6
                float mm = nx * s1 - ny * c1;
                float nn = ox * s1 - oy * c1;
                float theta6 = (Mathf.Abs(s5) > 1e-6) ? Mathf.Atan2(mm, nn) - Mathf.Atan2(s5, 0) : 0.0f;

                // Compute wrist position for θ2, θ3
                m = d5 * (Mathf.Sin(theta6) * (nx * c1 + ny * s1) + Mathf.Cos(theta6) * (ox * c1 + oy * s1)) -
                    d6 * (ax * c1 + ay * s1) + px * c1 + py * s1;
                n = pz - d1 - az * d6 + d5 * (oz * Mathf.Cos(theta6) + nz * Mathf.Sin(theta6));

                // θ3 (two solutions: elbow up/down)
                float D = (m * m + n * n - a2 * a2 - a3 * a3) / (2 * Mathf.Abs(a2) * Mathf.Abs(a3));
                if (Mathf.Abs(D) > 1.01f)
                {
                    UnityEngine.Debug.Log("theta 3 error: D > 1");
                    continue;
                }
                D = Mathf.Clamp(D, -1.0f, 1.0f);
                float[] theta3_options = new float[] { Mathf.Acos(D), -Mathf.Acos(D) };

                foreach (float theta3 in theta3_options)
                {
                    float s3 = Mathf.Sin(theta3), c3 = Mathf.Cos(theta3);

                    // θ2
                    float s2 = ((a3 * c3 + a2) * n - a3 * s3 * m) / (a2 * a2 + a3 * a3 + 2 * a2 * a3 * c3);
                    float c2 = (m + a3 * s3 * s2) / (a3 * c3 + a2);
                    float theta2 = Mathf.Atan2(s2, c2);

                    // θ4
                    float theta4 = Mathf.Atan2(
                        -Mathf.Sin(theta6) * (nx * c1 + ny * s1) - Mathf.Cos(theta6) * (ox * c1 + oy * s1),
                        oz * Mathf.Cos(theta6) + nz * Mathf.Sin(theta6)
                    ) - theta2 - theta3;

                    // Convert to degrees and negate all angles
                    float[] solution = new float[] {
                        theta1 * Mathf.Rad2Deg,
                        theta2 * Mathf.Rad2Deg,
                        theta3 * Mathf.Rad2Deg,
                        theta4 * Mathf.Rad2Deg,
                        theta5 * Mathf.Rad2Deg,
                        theta6 * Mathf.Rad2Deg
                    };
                    solutions.Add(solution);
                }
            }
        }

        return solutions;
    }

    public (Vector3 position, Vector3 rotation, Vector3 rotationVector) GetEndEffectorPose(float[] jointAngles)
    {
        // Convert joint angles from degrees to radians
        float[] theta = new float[6];
        for (int i = 0; i < 6; i++)
        {
            theta[i] = jointAngles[i] * Mathf.Deg2Rad; // Negate angles to match UR convention
        } 

        // DH parameters for UR3
        float[] a = new float[] { 0, a2, a3, 0, 0, 0 };
        float[] alpha = new float[] { Mathf.PI/2, 0, 0, Mathf.PI/2, -Mathf.PI/2, 0 };
        float[] d = new float[] { d1, 0, 0, d4, d5, d6 };


        // Start with base transform
        if (baseTransform != null)
        {
            // Convert from Unity coordinate system to standard robotics coordinate system
            // Unity: X-right, Y-up, Z-forward
            // Standard: X-forward, Y-left, Z-up
            Vector3 standardPosition = new Vector3(
                baseTransform.position.z,  // Unity Z -> Standard X (forward)
                -baseTransform.position.x, // Unity X -> Standard Y (left, negated)
                baseTransform.position.y   // Unity Y -> Standard Z (up)
            );
            Vector3 standardRotation = new Vector3(
                baseTransform.eulerAngles.z,  // Unity Z -> Standard X
                -baseTransform.eulerAngles.x, // Unity X -> Standard Y (negated)
                -baseTransform.eulerAngles.y  // Unity Y -> Standard Z (negated)
            );
            
            baseToOriginUr3 = AngleConvert.PoseToTransform(standardPosition.x, standardPosition.y, standardPosition.z,
                                                          standardRotation.x, standardRotation.y, standardRotation.z);
        }
        else
        {
            baseToOriginUr3 = Matrix4x4.identity;
        }
         
        Matrix4x4 T = baseToOriginUr3;
        // Calculate forward kinematics
        for (int i = 0; i < 6; i++)
        {
            T = T * AngleConvert.GetTransform(theta[i], a[i], alpha[i], d[i]);
            Vector3 position2 = new Vector3(T[0, 3], T[1, 3], T[2, 3]); 
        } 
        // Add tool transform
        Matrix4x4 T_tool = AngleConvert.PoseToTransform(tcpOffset.x, tcpOffset.y, tcpOffset.z, 0, 0, 0);
        T = T * T_tool;


        Pose6D pose = AngleConvert.TransformToPose(T);
        // Extract position
        Vector3 position = new Vector3(pose.position.x, pose.position.y, pose.position.z);
        // Extract rotation (roll, pitch, yaw)
        float roll = pose.rpy.x;
        float pitch = pose.rpy.y;
        float yaw = pose.rpy.z;
        Vector3 rotation = new Vector3(roll, pitch, yaw);
        // Compute rotation vector (axis-angle) directly from rotation matrix
        Vector3 rotationVector = AngleConvert.TranformtoRotaionMatrix(T);

        
        return (position, rotation, rotationVector);
    }
}