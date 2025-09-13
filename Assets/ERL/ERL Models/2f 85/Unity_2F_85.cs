using System.Linq;
using UnityEngine;
using UnityEditor;

public class Unity_2F_85 : MonoBehaviour
{

    [Header("UnitytoRobotRatio")]
    public int UnitytoRobotRatio = 1;

    public Robot_2f_85 robot_2f_85;

    // Private constants.
    //  The motion parameters of the Robotiq 2F-85 end-effector.
    //      Stroke in mm.
    private const float s_min = 0.0f;
    private const float s_max = 85.0f;
    //      Velocity in mm/s.
    private const float v_min = 0f;
    private const float v_max = 150.0f;
    //  Polynomial coefficients.
    private readonly float[] coefficients = new float[] { 1.4618779830656545e-08f, -2.221783401011447e-06f,
                                                          0.00012577975746560203f, 0.00704713293601611f,
                                                          2.1002081712618935e-07f};

    // Private variables.
    //  Motion parameters.
    private float speed;
    
    private float force;
    private float __stroke;
    private float __theta;
    private float __theta_i;

    public ArticulationBody joint1;
    public ArticulationBody joint2;
    public float Conv;

    //  Parts (left, right hand) to be transformed.
    private GameObject R_Arm_ID_0; private GameObject R_Arm_ID_1;
    private GameObject R_Arm_ID_2;
    private GameObject L_Arm_ID_0; private GameObject L_Arm_ID_1;
    private GameObject L_Arm_ID_2;
    //  Others.
    private int ctrl_state;

    // Public variables.
    public bool start_movemet;
    //  Input motion parameters. 
    public float stroke;

    private bool in_position;

    float unityStrokeMin = 0f;
    float unityStrokeMax = 85f;
    float realStrokeMin = 0f;
    float realStrokeMax = 255f;

    // Start is called before the first frame update
    void Start()
    {
        // Initialization of the end-effector movable parts.
        //  Right arm.
        R_Arm_ID_0 = transform.Find("R_Arm_ID_0").gameObject; R_Arm_ID_1 = transform.Find("R_Arm_ID_1").gameObject;
        R_Arm_ID_2 = R_Arm_ID_0.transform.Find("R_Arm_ID_2").gameObject;
        //  Left arm.
        L_Arm_ID_0 = transform.Find("L_Arm_ID_0").gameObject; L_Arm_ID_1 = transform.Find("L_Arm_ID_1").gameObject;
        L_Arm_ID_2 = L_Arm_ID_0.transform.Find("L_Arm_ID_2").gameObject;


        // Reset variables.
        ctrl_state = 0;
        //  Reset the read-only variables to null.
        in_position = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (ctrl_state)
        {
            case 0:
                {

                    
                    __stroke = Mathf.Clamp(stroke, s_min, s_max);
                    speed = Mathf.Clamp(speed, v_min, v_max);

                    if (start_movemet == true)
                    {
                        ctrl_state = 1;
                    }
                }
                break;

            case 1:
                {
                    // Reset variables.
                    in_position = false;

                    // Convert the stroke to the angle in degrees.
                    __theta = Polyval(coefficients, __stroke) * Mathf.Rad2Deg;

                    ctrl_state = 2;
                }
                break;

            case 2:
                {
                    // Interpolate the orientation between the current position and the target position.
                    __theta_i = Mathf.MoveTowards(__theta_i, __theta, speed * Time.deltaTime);

                    // Change the orientation of the end-effector arm.
                    //  Right arm.
                    
                    SetJointPositions(new float[] { -__theta_i*Conv, __theta_i*Conv });

                    R_Arm_ID_0.transform.localEulerAngles = new Vector3(0.0f, -__theta_i, 0.0f);
                    R_Arm_ID_1.transform.localEulerAngles = new Vector3(0.0f, -__theta_i, 0.0f);
                    R_Arm_ID_2.transform.localEulerAngles = new Vector3(0.0f, __theta_i, 0.0f);
                    //  Left arm.
                    L_Arm_ID_0.transform.localEulerAngles = new Vector3(0.0f, __theta_i, 0.0f);
                    L_Arm_ID_1.transform.localEulerAngles = new Vector3(0.0f, __theta_i, 0.0f);
                    L_Arm_ID_2.transform.localEulerAngles = new Vector3(0.0f, -__theta_i, 0.0f);
                    if(__theta_i == __theta)
                    {
                        in_position = true; start_movemet = false;
                        ctrl_state = 0;
                        Debug.Log("in_position: " + in_position + " start_movemet: " + start_movemet + " ctrl_state: " + ctrl_state);
                    }
                }
                break;

        }
    }

    public float Polyval(float[] coefficients, float x)
    {
        /*
             Description:
                A function to evaluate a polynomial at a specific value.

                Equation:
                    y = coeff[0]*x**(n-1) + coeff[1]*x**(n-2) + ... + coeff[n-2]*x + coeff[n-1]

            Args:
                (1) coefficients [Vector<float>]: Polynomial coefficients.
                (2) x [float]: An input value to be evaluated.

            Returns:
                (1) parameter [float]: The output value, which is evaluated using the input 
                                       polynomial coefficients.
         */

        float y = 0.0f; int n = coefficients.Length - 1;
        foreach (var (coeff_i, i) in coefficients.Select((coeff_i, i) => (coeff_i, i)))
        {

            y += coeff_i * Mathf.Pow(x, (n - i));
        }

        return y;
    }


    void SetJointPositions(float[] positions)
    {
        var drive = joint1.yDrive;
        drive.target = positions[0];  // angle in degrees
        joint1.yDrive = drive;
        var drive2 = joint2.yDrive;
        drive2.target = positions[1];  // angle in degrees
        joint2.yDrive = drive2;
    }


 
    public void MoveGripperToPosition(int targetStroke , int speed1 = 100 ,bool sendToRobot = false, int force1 = 255 )
    {
        Debug.Log("MoveGripperToPosition: " + targetStroke + " speed1: " + speed1 + " force1: " + force1);
        if (sendToRobot)
        {
            robot_2f_85.SetGripperPosition(targetStroke);
            robot_2f_85.SetGripperForce(force1);
            robot_2f_85.SetGripperSpeed(speed1);
        }

        // Convert from 0-255 (real robot) to 0-85 (Unity)
        float clampedStroke = Mathf.Clamp(targetStroke, realStrokeMin, realStrokeMax);
        float mappedStroke = (clampedStroke - realStrokeMin) / (realStrokeMax - realStrokeMin) * (unityStrokeMax - unityStrokeMin) + unityStrokeMin;
        
        stroke = Mathf.Clamp(85 - mappedStroke, s_min, s_max);
        force = force1;
        speed = speed1;
        start_movemet = true;
        Debug.Log(" targetStroke: " + stroke + " speed1: " + speed + " force1: " + force);
    }
 
    public bool IsGripperInPosition()
    {
        return in_position;
    }

 
    public float GetCurrentStroke()
    {
        return __stroke;
    }
}
