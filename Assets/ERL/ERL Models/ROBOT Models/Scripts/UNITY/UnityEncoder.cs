using UnityEngine;

public class UnityEncoder : MonoBehaviour
{
    [SerializeField] private ArticulationBody[] joints = new ArticulationBody[6];
    
    

    void Start()
    {
        GetUnityAngles();
    }

    void Update()
    {
        GetUnityAngles();
    }


    public float[] GetUnityAngles()
    {
        float[] UnityActualAngles = new float[6];
        
        for (int i = 0; i < 6; i++)
        {
            UnityActualAngles[i] = joints[i].jointPosition[0];
            UnityActualAngles[i] = UnityActualAngles[i] * Mathf.Rad2Deg;
        }
        
        //Debug.Log("Angles: " + string.Join(", ", angles));
        return UnityActualAngles;
    }
} 