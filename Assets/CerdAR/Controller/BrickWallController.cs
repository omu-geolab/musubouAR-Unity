using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class BrickWallController : MonoBehaviour
{
    // References
    [HideInInspector] public ARPlane wallPlane;
    [HideInInspector] public Vector3 wallNormal;

    // Time configuration
    public float initialDelay = 1f;
    public float totalLifetime = 7.0f;  // Total lifetime for the wall

    // Brick configuration
    public float brickWidth = 0.2f;
    public float brickHeight = 0.1f;
    public float brickDepth = 0.1f;

    // Wall configuration
    public float wallHeight = 2.0f;
    public float wallWidth = 2.0f;

    // Physics configuration
    [Range(1.0f, 5.0f)]
    public float fallingForce = 2.0f;

    // Material references
    public Material brickMaterial;

    // Audio configuration
    public string wallSoundResourcePath = "wallfall";  // Name of audio file in Resources folder
    public AudioClip wallCrumbleSound;    // This will be auto-loaded if not assigned
    public float volumeScale = 1.0f;      // Volume scale for sound effects

    // Private variables
    private List<GameObject> bricks = new List<GameObject>();
    private bool isFalling = false;
    private GameObject brickPrefab;
    private float creationTime;
    private AudioSource audioSource;

    void Start()
    {
        // Record creation time for tracking lifecycle
        creationTime = Time.time;

        // Set up audio source and load sounds
        SetupAudio();

        // Create brick prefab
        CreateBrickPrefab();

        // Build the brick wall
        BuildBrickWall();

        // Schedule wall collapse
        Invoke("InitiateFall", initialDelay);

        // Schedule wall destruction
        Invoke("DestroyWall", totalLifetime);
    }

    private void SetupAudio()
    {
        // Add AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;  // 3D sound
        audioSource.volume = volumeScale;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 20.0f;

        // Load sound from Resources if not already assigned
        if (wallCrumbleSound == null)
        {
            wallCrumbleSound = Resources.Load<AudioClip>(wallSoundResourcePath);
            if (wallCrumbleSound != null)
            {
                Debug.Log("Load sound f: " + wallSoundResourcePath);
            }
            else
            {
                Debug.Log("Not find sound from Resources " + wallSoundResourcePath);
            }
        }
    }

    private void CreateBrickPrefab()
    {
        // Create brick from primitive cube
        brickPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brickPrefab.name = "BrickPrefab";

        // Scale to desired brick size
        brickPrefab.transform.localScale = new Vector3(brickWidth, brickHeight, brickDepth);

        // Apply material
        Renderer renderer = brickPrefab.GetComponent<Renderer>();

        // Use the material assigned in inspector
        if (brickMaterial != null)
        {
            renderer.material = brickMaterial;
            Debug.Log("Using material assigned in inspector");
        }
        else
        {
            // Fallback to cement color
            Debug.LogWarning("No material assigned! Using default with cement color");

            // Create a URP compatible material if possible
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader != null)
            {
                renderer.material = new Material(litShader);
            }

            // Cement gray color - light gray with slight beige tint
            renderer.material.color = new Color(0.65f, 0.63f, 0.58f);

            // Add some roughness for concrete appearance
            if (renderer.material.HasProperty("_Smoothness"))
            {
                renderer.material.SetFloat("_Smoothness", 0.1f); // Low smoothness for rough cement
            }

            // Add slightly more metallic for concrete sheen if the property exists
            if (renderer.material.HasProperty("_Metallic"))
            {
                renderer.material.SetFloat("_Metallic", 0.05f); // Just a hint of metallic
            }
        }

        // Add rigidbody
        Rigidbody rb = brickPrefab.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Hide original prefab
        brickPrefab.SetActive(false);
    }

    private void BuildBrickWall()
    {
        // Calculate number of bricks per row and number of rows
        int bricksPerRow = Mathf.FloorToInt(wallWidth / brickWidth);
        int rowCount = Mathf.FloorToInt(wallHeight / brickHeight);

        // Calculate starting position
        Vector3 startPos = transform.position;
        startPos -= transform.right * (wallWidth / 2 - brickWidth / 2);

        // Move slightly forward from the wall plane to prevent clipping
        startPos += wallNormal * brickDepth / 2;

        // Offset downward to place wall centered on the wall plane
        startPos -= Vector3.up * (wallHeight / 2);

        // Build each row of bricks
        for (int row = 0; row < rowCount; row++)
        {
            // Each even row will be offset by half a brick
            float offsetX = (row % 2 == 0) ? 0 : brickWidth / 2;

            for (int col = 0; col < bricksPerRow; col++)
            {
                // Calculate brick position
                Vector3 brickPos = startPos +
                                   transform.right * (col * brickWidth + offsetX) +
                                   Vector3.up * (row * brickHeight);

                // Skip if brick would extend beyond wall width
                if (offsetX > 0 && col == bricksPerRow - 1)
                    continue;

                // Create new brick
                GameObject brick = Instantiate(brickPrefab, brickPos, transform.rotation);
                brick.transform.SetParent(transform);
                brick.SetActive(true);
                brick.tag = "ARViewer";

                // Add to list for management
                bricks.Add(brick);
            }
        }
    }

    private void InitiateFall()
    {
        if (isFalling) return;

        isFalling = true;

        // Play wall crumble sound if available
        if (wallCrumbleSound != null && audioSource != null)
        {
            audioSource.clip = wallCrumbleSound;
            audioSource.Play();
        }

        StartCoroutine(MakeWallFall());
    }

    private IEnumerator MakeWallFall()
    {
        // Make wall fall in the direction of the normal
        Vector3 fallDirection = wallNormal;
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;

        // Group bricks by row for sequential falling from top to bottom
        Dictionary<int, List<GameObject>> bricksByRow = new Dictionary<int, List<GameObject>>();

        foreach (GameObject brick in bricks)
        {
            if (brick == null) continue;

            // Calculate row based on height
            int row = Mathf.FloorToInt((brick.transform.position.y - (transform.position.y - wallHeight / 2)) / brickHeight);

            if (!bricksByRow.ContainsKey(row))
            {
                bricksByRow[row] = new List<GameObject>();
            }

            bricksByRow[row].Add(brick);
        }

        // Get list of rows and sort from top to bottom
        List<int> rows = new List<int>(bricksByRow.Keys);
        rows.Sort((a, b) => b.CompareTo(a)); // Sort descending

        // Make each row fall sequentially from top to bottom
        foreach (int row in rows)
        {
            if (!bricksByRow.ContainsKey(row)) continue;

            foreach (GameObject brick in bricksByRow[row])
            {
                if (brick == null) continue;

                Rigidbody rb = brick.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;

                    // Add falling force
                    rb.AddForce(fallDirection * fallingForce, ForceMode.Impulse);

                    // Add random torque for natural falling effect
                    rb.AddTorque(torqueAxis * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
                }
            }

            // Wait a short time before moving to next row
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void DestroyWall()
    {
        // Clean up all bricks
        foreach (GameObject brick in bricks)
        {
            if (brick != null)
            {
                Destroy(brick);
            }
        }

        bricks.Clear();

        if (brickPrefab != null)
        {
            Destroy(brickPrefab);
        }

        // Destroy this controller
        Destroy(gameObject);
    }

    // We can get the remaining lifetime of this wall
    public float GetRemainingLifetime()
    {
        float elapsedTime = Time.time - creationTime;
        return Mathf.Max(0, totalLifetime - elapsedTime);
    }

    // Check if the wall is still valid (hasn't exceeded its lifetime)
    public bool IsValid()
    {
        return GetRemainingLifetime() > 0;
    }

    private void OnDestroy()
    {
        // Cancel any scheduled activities
        CancelInvoke();
        StopAllCoroutines();

        // Clean up all bricks
        foreach (GameObject brick in bricks)
        {
            if (brick != null)
            {
                Destroy(brick);
            }
        }

        if (brickPrefab != null)
        {
            Destroy(brickPrefab);
        }
    }
}