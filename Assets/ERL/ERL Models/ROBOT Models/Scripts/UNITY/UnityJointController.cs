using UnityEngine;

public class UnityJointController : MonoBehaviour
{
    [SerializeField] private ArticulationBody[] joints = new ArticulationBody[6];
    
    [Header("Drive Settings")]
    [SerializeField] private float stiffness = 100000f;
    [SerializeField] private float damping = 500f;
    [SerializeField] private float forceLimit = 1000f; 
    
    [Header("Target Angles")]
    [SerializeField] private float[] UnityActualTargetAngles = new float[6];

    void Start()
    {
        SetUnityDriveParameters();
        // Initialize targets to current joint positions (degrees) to avoid snapping to zero
        for (int i = 0; i < 6; i++)
        {
            if (joints[i] != null)
            {
                UnityActualTargetAngles[i] = joints[i].jointPosition[0] * Mathf.Rad2Deg;
            }
        }
    }

    void Update()
    {
        SetUnityDriveParameters();
        SetUnityTargetAngles(UnityActualTargetAngles);
    }

    
    private void SetUnityDriveParameters()
    {
        for (int i = 0; i < 6; i++)
        {
            if (joints[i] != null)
            {
                ArticulationDrive drive = joints[i].xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                drive.forceLimit = forceLimit; 
                joints[i].xDrive = drive;
            }
        }
    }

 
    private void SetUnityTargetAngles(float[] UnityActualTargetAngles)
    {
        for (int i = 0; i < 6; i++)
        {
            if (joints[i] != null)
            {
                float targetDegrees = UnityActualTargetAngles[i];
                ArticulationDrive drive = joints[i].xDrive;
                drive.target = targetDegrees;
                joints[i].xDrive = drive;
            }
        }
    }

    public void ChangeUnityTargetAngles(float[] newUnityActualTargetAngles)
    {
        
        UnityActualTargetAngles = newUnityActualTargetAngles;
    }
}
