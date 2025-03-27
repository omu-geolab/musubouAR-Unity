using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Creates a wall that collapses as if pushed over by wind
/// </summary>
public class CollapsingWall : MonoBehaviour
{
    [Header("Wall Settings")]
    public GameObject wallPrefab;         // Drag your wall prefab here
    public float initialDelay = 1f;       // Time before the wall starts falling
    public float destroyDelay = 15f;      // Time before destroying and recreating the wall
    public Transform spawnPoint;          // Spawn position (if empty, will use this GameObject's position)

    [Header("Wind Effect Settings")]
    public bool continuousPositioning = true; // Whether to continuously adjust position during runtime
    public float windupDuration = 2.0f;   // How long the wind builds up before toppling the wall

    [Header("Falling Direction")]
    public float fallingForce = 2.0f;     // Force applied to make the wall fall
    public float torqueForce = 5.0f;      // Rotational force applied
    public enum FallDirection { Forward, Backward, Left, Right }
    public FallDirection fallDirection = FallDirection.Forward; // Direction to fall

    [Header("Sound Effects")]
    public AudioClip crackingSound;       // Sound played when the wall starts to fall
    public AudioClip impactSound;         // Sound played when the wall hits the ground
    public float volumeScale = 1.0f;      // Volume multiplier for all sounds

    // Private variables
    private GameObject currentWall;       // Reference to the current wall
    private AudioSource audioSource;      // Audio source component
    private float spawnTime;              // When the wall was spawned
    private Vector3 windDirection;        // Current direction of the wind

    // Public flag to indicate if the wall has started falling
    [HideInInspector]
    public bool hasStartedFalling = false;

    void Start()
    {
        // Get or add an AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Use current position if no spawn point is specified
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Set initial wind direction based on fall direction
        //UpdateWindDirection();    

        // Start the wall cycle
        SpawnNewWall();
    }

    void UpdateWindDirection()
    {
        // Set wind direction based on fall direction
        switch (fallDirection)
        {
            case FallDirection.Forward:
                windDirection = transform.forward;
                break;
            case FallDirection.Backward:
                windDirection = -transform.forward;
                break;
            case FallDirection.Left:
                windDirection = -transform.right;
                break;
            case FallDirection.Right:
                windDirection = transform.right;
                break;
        }
    }

    // Update for wind and wall movement
    void Update()
    {
        if (hasStartedFalling || currentWall == null)
            return;

        // Early exit if not using continuous positioning
        if (!continuousPositioning)
            return;

        // Update the wall's position to match the transform (for AR tracking)
        float currentY = currentWall.transform.position.y;
        Vector3 targetPosition = transform.position;
        targetPosition.y = currentY;
        currentWall.transform.position = targetPosition;

        // Calculate time elapsed since spawn
        float timeElapsed = Time.time - spawnTime;

        if (timeElapsed < initialDelay)
            return;  // Wait for initial delay before showing wind effects

        //Create wind effect by gradually tilting the wall
        if (timeElapsed < initialDelay + windupDuration)
        {
            // Calculate how far through the windup we are (0-1)
            float windupProgress = (timeElapsed - initialDelay) / windupDuration;
            windupProgress = Mathf.Clamp01(windupProgress);

            // Calculate rotation axis (perpendicular to wind direction)
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, windDirection).normalized;

            // Apply increasing tilt towards the wind direction
            float tiltAngle = windupProgress * 8.0f; // Max 8 degrees tilt

            // Add slight wobble to simulate wind
            float wobble = Mathf.Sin(Time.time * 6f) * 0.5f;

            // Create rotation for the wall
            Quaternion targetRotation = transform.rotation *
                                       Quaternion.AngleAxis(tiltAngle, rotationAxis) *
                                       Quaternion.Euler(wobble, 0, wobble);

            // Apply rotation with smooth damping
            currentWall.transform.rotation = Quaternion.Slerp(
                currentWall.transform.rotation,
                targetRotation,
                Time.deltaTime * 3.0f
            );
        }
    }

    // Creates a new wall
    void SpawnNewWall()
    {
        // Reset falling state
        hasStartedFalling = false;

        // Record spawn time
        spawnTime = Time.time;

        // Create wall at the current position
        currentWall = Instantiate(wallPrefab, transform.position, transform.rotation);
        currentWall.tag = "ARViewer";

        // Schedule events
        float fallTime = initialDelay + windupDuration;
        Invoke("MakeWallFall", fallTime);
        Invoke("DestroyAndRespawn", fallTime + destroyDelay);
    }

    // Makes the wall fall by activating physics
    void MakeWallFall()
    {
        // Mark as falling
        hasStartedFalling = true;

        // Safety check in case the wall was destroyed
        if (currentWall == null) return;

        // Play cracking sound if available
        if (crackingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(crackingSound, volumeScale);
        }

        // Update wind direction based on current fall direction
        //UpdateWindDirection();

        // Find all Rigidbodies on the wall and its children
        //Rigidbody[] rigidbodies = currentWall.GetComponentsInChildren<Rigidbody>();

        //// Activate physics for all Rigidbodies
        //foreach (Rigidbody rb in rigidbodies)
        //{
        //    rb.isKinematic = false;

        //    // Apply force in the wind direction
        //    rb.AddForce(windDirection * fallingForce, ForceMode.Impulse);

        //    // Apply torque for rotation (perpendicular to wind)
        //    Vector3 torqueAxis = Vector3.Cross(Vector3.up, windDirection).normalized;
        //    rb.AddTorque(torqueAxis * torqueForce, ForceMode.Impulse);
        //}

        // Schedule impact sound
        Invoke("PlayImpactSound", 1.0f);
    }

    // Plays the impact sound when the wall hits the ground
    void PlayImpactSound()
    {
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound, volumeScale);
        }
    }

    // Destroys the current wall and spawns a new one
    void DestroyAndRespawn()
    {
        // Safety check
        if (currentWall != null)
        {
            Destroy(currentWall);
        }

        // Create a new wall
        SpawnNewWall();
    }

    // Called when this component is destroyed
    private void OnDestroy()
    {
        // Cancel all pending invokes
        CancelInvoke();

        // Destroy the wall if it exists
        if (currentWall != null)
        {
            Destroy(currentWall);
        }
    }

    // Public method to restart the cycle
    public void RestartCycle()
    {
        CancelInvoke();

        if (currentWall != null)
        {
            Destroy(currentWall);
        }

        SpawnNewWall();
    }

    // Public method to change the falling direction
    public void SetFallDirection(FallDirection direction)
    {
        fallDirection = direction;
        UpdateWindDirection();
    }
}