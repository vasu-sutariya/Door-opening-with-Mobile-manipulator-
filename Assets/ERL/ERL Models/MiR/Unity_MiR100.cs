using UnityEngine;

public class Unity_MiR100 : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private bool isMoving = true;
    [SerializeField] private bool moveForward = true;
    [SerializeField] private bool moveBackward = false;
    [SerializeField] private bool moveLeft = false;
    [SerializeField] private bool moveRight = false;
    
    [Header("Physics Settings")]
    [SerializeField] private float damping = 10f;
    
    private ArticulationBody articulationBody;
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    void Start()
    {
        // Get the ArticulationBody component
        articulationBody = GetComponent<ArticulationBody>();
        
        // If no ArticulationBody found on this GameObject, try to find it in children
        if (articulationBody == null)
        {
            articulationBody = GetComponentInChildren<ArticulationBody>();
        }
        
        // Store initial position and rotation
        if (articulationBody != null)
        {
            startPosition = articulationBody.transform.position;
            startRotation = articulationBody.transform.rotation;
            
            // Configure articulation body for physics-based movement
            articulationBody.linearDamping = damping;
            articulationBody.angularDamping = damping;
        }
        else
        {
            Debug.LogWarning("No ArticulationBody found on " + gameObject.name + " or its children!");
        }
    }
    
    void FixedUpdate()
    {
        if (articulationBody != null && isMoving)
        {
            MoveForward();
        }
    }
    
    private void MoveForward()
    {
        Vector3 totalForce = Vector3.zero;
        
        // Forward/Backward movement
        if (moveForward)
        {
            Vector3 forwardDirection = articulationBody.transform.forward;
            totalForce += forwardDirection * moveSpeed * articulationBody.mass;
        }
        
        if (moveBackward)
        {
            Vector3 backwardDirection = -articulationBody.transform.forward;
            totalForce += backwardDirection * moveSpeed * articulationBody.mass;
        }
        
        // Left/Right movement
        if (moveLeft)
        {
            Vector3 leftDirection = -articulationBody.transform.right;
            totalForce += leftDirection * moveSpeed * articulationBody.mass;
        }
        
        if (moveRight)
        {
            Vector3 rightDirection = articulationBody.transform.right;
            totalForce += rightDirection * moveSpeed * articulationBody.mass;
        }
        
        // Apply the combined force
        if (totalForce != Vector3.zero)
        {
            articulationBody.AddForce(totalForce);
        }
    }
    
    // Public methods to control movement
    public void StartMoving()
    {
        isMoving = true;
    }
    
    public void StopMoving()
    {
        isMoving = false;
    }
    
    public void ToggleMovement()
    {
        isMoving = !isMoving;
    }
    
    public void ResetPosition()
    {
        if (articulationBody != null)
        {
            articulationBody.TeleportRoot(startPosition, startRotation);
        }
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // Direction control methods
    public void SetMoveForward(bool enabled)
    {
        moveForward = enabled;
    }
    
    public void SetMoveBackward(bool enabled)
    {
        moveBackward = enabled;
    }
    
    public void SetMoveLeft(bool enabled)
    {
        moveLeft = enabled;
    }
    
    public void SetMoveRight(bool enabled)
    {
        moveRight = enabled;
    }
    
    // Convenience methods for common movement patterns
    public void MoveInDirection(Vector2 direction)
    {
        // direction.x controls left/right, direction.y controls forward/backward
        moveLeft = direction.x < 0;
        moveRight = direction.x > 0;
        moveBackward = direction.y < 0;
        moveForward = direction.y > 0;
    }
    
    public void StopAllMovement()
    {
        moveForward = false;
        moveBackward = false;
        moveLeft = false;
        moveRight = false;
    }
    
    // Method to stop the robot when it hits something
    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);
        // Optionally stop movement on collision
        // StopMoving();
    }
}
