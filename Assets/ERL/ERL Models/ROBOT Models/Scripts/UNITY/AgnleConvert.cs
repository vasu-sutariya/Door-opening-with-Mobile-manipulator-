using UnityEngine;

[System.Serializable]
public struct Pose6D
{
    public Vector3 position;   // (x, y, z)
    public Vector3 rpy;        // (roll, pitch, yaw)

    public Pose6D(Vector3 pos, Vector3 rpyAngles)
    {
        position = pos;
        rpy = rpyAngles;
    }
}



public class AngleConvert : MonoBehaviour
{
    public static Vector3 TranformtoRotaionMatrix(Matrix4x4 T)
    {
        Quaternion q = T.rotation;

        // Convert quaternion to axis-angle
        q.ToAngleAxis(out float angleDeg, out Vector3 axis);

        // Convert to radians
        float angleRad = angleDeg * Mathf.Deg2Rad;

        // Rotation vector = axis * angle
        return axis.normalized * angleRad;
    }


    public static Matrix4x4 GetTransform(float theta, float a, float alpha, float d)
    {
        float cosTheta = Mathf.Cos(theta);
        float sinTheta = Mathf.Sin(theta);
        float cosAlpha = Mathf.Cos(alpha);
        float sinAlpha = Mathf.Sin(alpha);

        Matrix4x4 T = new Matrix4x4();
        T.SetRow(0, new Vector4(cosTheta, -sinTheta * cosAlpha, sinTheta * sinAlpha, a * cosTheta));
        T.SetRow(1, new Vector4(sinTheta, cosTheta * cosAlpha, -cosTheta * sinAlpha, a * sinTheta));
        T.SetRow(2, new Vector4(0.0f, sinAlpha, cosAlpha, d));
        T.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        return T;
    }

    public static Matrix4x4 PoseToTransform(float x, float y, float z, float roll, float pitch, float yaw)
    {
        // Convert angles to radians
        roll *= Mathf.Deg2Rad;
        pitch *= Mathf.Deg2Rad;
        yaw *= Mathf.Deg2Rad;

        // Rotation matrices
        Matrix4x4 Rx = Matrix4x4.identity;
        Rx.SetRow(1, new Vector4(0, Mathf.Cos(roll), -Mathf.Sin(roll), 0));
        Rx.SetRow(2, new Vector4(0, Mathf.Sin(roll), Mathf.Cos(roll), 0));

        Matrix4x4 Ry = Matrix4x4.identity;
        Ry.SetRow(0, new Vector4(Mathf.Cos(pitch), 0, Mathf.Sin(pitch), 0));
        Ry.SetRow(2, new Vector4(-Mathf.Sin(pitch), 0, Mathf.Cos(pitch), 0));

        Matrix4x4 Rz = Matrix4x4.identity;
        Rz.SetRow(0, new Vector4(Mathf.Cos(yaw), -Mathf.Sin(yaw), 0, 0));
        Rz.SetRow(1, new Vector4(Mathf.Sin(yaw), Mathf.Cos(yaw), 0, 0));

        // Combined rotation: R = Rz * Ry * Rx
        Matrix4x4 R = Rz * Ry * Rx;

        // Set translation
        R.SetColumn(3, new Vector4(x, y, z, 1.0f));
        return R;
    }

    public static Pose6D TransformToPose(Matrix4x4 R)
    {
        float x = R[0, 3];
        float y = R[1, 3];
        float z = R[2, 3];
        float roll = Mathf.Atan2(R[2, 1], R[2, 2]) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(-R[2, 0], Mathf.Sqrt(R[2, 1] * R[2, 1] + R[2, 2] * R[2, 2])) * Mathf.Rad2Deg;
        float yaw = Mathf.Atan2(R[1, 0], R[0, 0]) * Mathf.Rad2Deg;
        return new Pose6D(new Vector3(x, y, z), new Vector3(roll, pitch, yaw));
    }
}
