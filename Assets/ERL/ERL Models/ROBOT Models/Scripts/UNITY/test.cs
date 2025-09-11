using UnityEngine;

public class test : MonoBehaviour
{
    public UnityTrajControl unityTrajControl;
    public float [] targetAngles1 = new float[6];
    public float time = 0f;
    public float velocity = 30f;
    public float acceleration = 60f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            unityTrajControl.MoveJ(targetAngles1, velocity, acceleration, 0.0f, time);
        }
       
    }
}

