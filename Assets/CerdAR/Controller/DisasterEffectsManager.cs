using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using JSONModel;
using System.Linq;

/// <summary>
/// Manages all disaster visual effects in the AR environment
/// </summary>
public class DisasterEffectsManager
{
    // AR Components
    private readonly ARPlaneManager arPlaneManager;
    private readonly ARCameraManager arCameraManager;
    private readonly AROcclusionManager occlusionManager;
    private readonly ARMeshManager meshManager;
    private readonly MeshFilter nonePrefab;
    private readonly MeshFilter occPrefab;

    // Disaster effect game objects
    private readonly GameObject waterAction;
    private readonly GameObject liqueAction;

    // Audio sources
    private readonly AudioSource houseAudio;
    private readonly AudioSource rockFallAudio;

    // Effect state tracking
    private bool isFireActive = false;
    private bool isWaterActive = false;
    private bool isWallActive = false;
    private bool isLiqueActive = false;
    private bool isHillActive = false;
    private bool isHouseActive = false;

    // Plane tracking for effects
    private readonly HashSet<string> planeFireEffects = new HashSet<string>();
    private readonly HashSet<string> planeLiqueEffects = new HashSet<string>();
    private readonly HashSet<string> planeWallEffects = new HashSet<string>();

    // Reference to spawned wall objects for cleanup
    private readonly List<GameObject> wallObjects = new List<GameObject>();

    // Resource cache
    private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    // Wall tracking
    private Dictionary<string, float> planeLastUsedTime = new Dictionary<string, float>();
    private const float DEFAULT_WALL_LIFETIME = 7.0f;

    // Liquefaction modes
    public enum LiqueMode
    {
        OnlyManhole = 0,       // Only show manhole
        OnlyElectricPole = 1,  // Only show electric pole
        Both = 2               // Show both
    }

    public DisasterEffectsManager(
        ARPlaneManager arPlaneManager,
        ARCameraManager arCameraManager,
        AROcclusionManager occlusionManager,
        ARMeshManager meshManager,
        MeshFilter nonePrefab,
        MeshFilter occPrefab,
        GameObject waterAction,
        GameObject liqueAction,
        AudioSource houseAudio,
        AudioSource rockFallAudio)
    {
        this.arPlaneManager = arPlaneManager;
        this.arCameraManager = arCameraManager;
        this.occlusionManager = occlusionManager;
        this.meshManager = meshManager;
        this.nonePrefab = nonePrefab;
        this.occPrefab = occPrefab;
        this.waterAction = waterAction;
        this.liqueAction = liqueAction;
        this.houseAudio = houseAudio;
        this.rockFallAudio = rockFallAudio;
    }

    /// <summary>
    /// Apply the appropriate disaster effect based on the feature properties
    /// </summary>
    public void ApplyDisasterEffect(JSONModel.Feature feature)
    {
        if (feature == null) return;

        int riskType = (int)feature.properties.risk_type;

        switch (riskType)
        {
            case 0:  // Fire
                ApplyFireEffect(feature);
                break;
            case 1:  // Water/Flood
            case 7:  // Also water
                ApplyWaterEffect(feature.properties.water_level);
                break;
            case 2:  // Hill/Landslide
                ApplyHillEffect(feature);
                break;
            case 3:  // Building collapse
            case 4:
            case 6:
                ApplyHouseEffect(feature);
                break;
            case 5:  // Wall
                ApplyWallEffect(feature);
                break;
            case 8:  // Liquefaction
                ApplyLiquefactionEffect(feature);
                break;
        }

        UpdateWarningText(riskType);
        UpdateAudio(riskType);
    }

    /// <summary>
    /// Apply a demo effect based on the selected animation mode
    /// </summary>
    public void ApplyDemoEffect(GlobalAR.AnimationMode mode, JSONModel.Feature demoFeature = null)
    {
        // Create a demo feature if not provided
        if (demoFeature == null)
        {
            demoFeature = new JSONModel.Feature();
            demoFeature.properties = new JSONModel.Properties();

            // Set default parameters for demo
            demoFeature.properties.intensity = 1.0;
            demoFeature.properties.wall_count = 3;
            demoFeature.properties.lique_mode = (int)LiqueMode.Both;
            demoFeature.properties.water_level = 1.0;
        }

        // Set risk_type based on animation mode
        switch (mode)
        {
            case GlobalAR.AnimationMode.Fire:
                demoFeature.properties.risk_type = 0;
                GlobalAR.currentWarn = "fire";
                break;
            case GlobalAR.AnimationMode.Water:
                demoFeature.properties.risk_type = 1;
                demoFeature.properties.water_level = 1.0;
                GlobalAR.currentWarn = "water";
                break;
            case GlobalAR.AnimationMode.Stone:
                demoFeature.properties.risk_type = 2;
                GlobalAR.currentWarn = "hill";
                break;
            case GlobalAR.AnimationMode.Home:
                demoFeature.properties.risk_type = 3;
                GlobalAR.currentWarn = "house";
                break;
            case GlobalAR.AnimationMode.Wall:
                demoFeature.properties.risk_type = 5;
                GlobalAR.currentWarn = "wall";
                break;
            case GlobalAR.AnimationMode.Lique:
                demoFeature.properties.risk_type = 8;
                GlobalAR.currentWarn = "lique";
                break;
            default:
                ClearAllEffects();
                GlobalAR.currentWarn = "";
                return;
        }

        // Apply effect with the configured feature
        ApplyDisasterEffect(demoFeature);
        UpdateAudio(GlobalAR.currentWarn);
    }

    /// <summary>
    /// Clear all active disaster effects
    /// </summary>
    public void ClearAllEffects()
    {
        waterAction.SetActive(false);
        liqueAction.SetActive(false);
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
        meshManager.meshPrefab = nonePrefab;

        // Clear wall effects specifically
        ClearWallEffects();

        GlobalAR.currentWarn = "";
        houseAudio.Pause();
        rockFallAudio.Pause();
    }

    /// <summary>
    /// Reset all effect states
    /// </summary>
    public void Reset()
    {
        isFireActive = false;
        isWaterActive = false;
        isWallActive = false;
        isLiqueActive = false;
        isHillActive = false;
        isHouseActive = false;

        planeFireEffects.Clear();
        planeLiqueEffects.Clear();
        planeWallEffects.Clear();

        // Clear wall objects
        ClearWallEffects();

        ClearAllEffects();
    }

    #region Specific Effect Implementations

    private void ApplyFireEffect(JSONModel.Feature feature)
    {
        if (!isFireActive)
        {
            Reset();
            isFireActive = true;
            return;
        }

        GlobalAR.currentWarn = "fire";

        // Get intensity from feature properties
        float intensity = 1.0f;
        if (feature.properties.intensity > 0)
        {
            intensity = (float)feature.properties.intensity;
        }

        foreach (var plane in arPlaneManager.trackables)
        {
            string planeId = plane.trackableId.ToString();
            if (planeFireEffects.Contains(planeId)) continue;

            // Add fire to the plane
            GameObject firePrefab = GetCachedPrefab("Bonfire");

            // Randomize fire size based on intensity
            float scale = Random.Range(1.0f, 4.0f * intensity);
            firePrefab.transform.localScale = new Vector3(scale, scale, scale);

            // Position at plane center
            Vector3 position = plane.transform.position;
            GameObject.Instantiate(firePrefab, position, Quaternion.identity);

            // Add additional fires for floor planes based on intensity
            if (plane.classification == PlaneClassification.Floor)
            {
                // Number of fires depends on intensity
                int fireCount = Mathf.RoundToInt(8 * intensity);

                for (int i = 0; i < fireCount; i++)
                {
                    float randomScale = Random.Range(1.0f, 4.0f * intensity);
                    firePrefab.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                    Vector3 randomPosition = new Vector3(
                        position.x + Random.Range(-7f, 7f),
                        position.y,
                        position.z + Random.Range(-7f, 7f)
                    );

                    GameObject fireInstance = GameObject.Instantiate(firePrefab, randomPosition, Quaternion.identity);
                    fireInstance.tag = "ARViewer";
                }
            }

            planeFireEffects.Add(planeId);
        }
    }

    private void ApplyWaterEffect(double? waterLevel)
    {
        // Enable occlusion for better water effect
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
        meshManager.meshPrefab = occPrefab;

        if (!isWaterActive)
        {
            Reset();
            isWaterActive = true;
            return;
        }

        GlobalAR.currentWarn = "water";
        waterAction.SetActive(true);

        // Set water height
        GameObject water = waterAction.transform.Find("water")?.gameObject;
        if (water != null)
        {
            FloodController floodController = water.GetComponent<FloodController>();
            if (floodController != null)
            {
                float height = waterLevel.HasValue ? (float)waterLevel.Value : 1f;
                floodController.heigth = height;
            }
        }
    }

    private void ApplyHillEffect(JSONModel.Feature feature)
    {
        GlobalAR.currentWarn = "hill";

        // Get intensity from feature properties
        float intensity = 1.0f;
        if (feature.properties.intensity > 0)
        {
            intensity = (float)feature.properties.intensity;
        }

        // Spawn falling rocks with count based on intensity
        string[] rockPrefabs = { "rock1", "rock2", "rock3", "rock4", "rock5", "rock6" };

        // Adjust rock count based on intensity
        int minCount = Mathf.RoundToInt(5 * intensity);
        int maxCount = Mathf.RoundToInt(10 * intensity);

        foreach (string rockName in rockPrefabs)
        {
            AddRandomGameObjects(rockName, minCount, maxCount);
        }

        isHillActive = true;
    }

    private void ApplyHouseEffect(JSONModel.Feature feature)
    {
        GlobalAR.currentWarn = "house";

        // Get intensity from feature properties
        float intensity = 1.0f;
        if (feature.properties.intensity > 0)
        {
            intensity = (float)feature.properties.intensity;
        }

        // Adjust debris count based on intensity
        int minCount = Mathf.RoundToInt(5 * intensity);
        int maxCount = Mathf.RoundToInt(10 * intensity);

        // Spawn house debris
        string[] houseParts = {
            "house part 1",
            "house part 2",
            "house part 3",
            "house part 4",
            "house part 5"
        };

        foreach (string part in houseParts)
        {
            AddRandomGameObjects(part, minCount, maxCount);
        }

        isHouseActive = true;
    }

    private void ApplyWallEffect(JSONModel.Feature feature)
    {
        if (!isWallActive)
        {
            Reset();
            isWallActive = true;
            return;
        }

        GlobalAR.currentWarn = "wall";

        // Clean up invalid wall objects
        CleanupInvalidWalls();

        // Get maximum wall count from feature properties
        int maxWallCount = 3; // Default value
        if (feature.properties.wall_count > 0)
        {
            maxWallCount = (int)feature.properties.wall_count;
        }

        // Only proceed if we have less than max walls
        if (wallObjects.Count >= maxWallCount) return;

        // Get camera position for distance calculations
        Vector3 cameraPosition = arCameraManager.transform.position;

        // Find wall planes near the camera
        List<(ARPlane plane, float distance)> wallPlanes = new List<(ARPlane, float)>();

        // Get the current time
        float currentTime = Time.time;

        foreach (var plane in arPlaneManager.trackables)
        {
            // Only use wall planes
            if (plane.classification != PlaneClassification.Wall) continue;

            // Get the plane ID
            string planeId = plane.trackableId.ToString();

            // Check if this plane was recently used
            if (planeLastUsedTime.ContainsKey(planeId))
            {
                float timeSinceLastUsed = currentTime - planeLastUsedTime[planeId];
                if (timeSinceLastUsed < DEFAULT_WALL_LIFETIME)
                {
                    // Skip this plane as it was used too recently
                    continue;
                }
            }

            // Check if this plane already has an active wall
            bool hasActiveWall = false;
            foreach (var wall in wallObjects)
            {
                if (wall == null) continue;

                BrickWallController controller = wall.GetComponent<BrickWallController>();
                if (controller != null && controller.wallPlane == plane)
                {
                    hasActiveWall = true;
                    break;
                }
            }

            // Skip if already has an active wall
            if (hasActiveWall) continue;

            // Make sure plane is minimum size
            float minPlaneSize = 0.5f; // Minimum 0.5m in size
            if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize)
            {
                continue;
            }

            // Calculate distance to camera
            float distance = Vector3.Distance(plane.transform.position, cameraPosition);
            wallPlanes.Add((plane, distance));
        }

        // Sort planes by distance (closest first)
        wallPlanes.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Create new walls for the closest available planes
        int wallsToCreate = maxWallCount - wallObjects.Count;

        foreach (var (plane, _) in wallPlanes.Take(wallsToCreate))
        {
            // Create wall controller
            GameObject wallController = new GameObject("BrickWallController");

            // Get direction from wall plane
            Vector3 wallNormal = plane.transform.up;
            wallNormal.y = 0;
            wallNormal.Normalize();

            // Set position and rotation based on the wall plane
            Vector3 wallPosition = plane.transform.position;
            Quaternion wallRotation = Quaternion.LookRotation(wallNormal);

            wallController.transform.position = wallPosition;
            wallController.transform.rotation = wallRotation;
            wallController.tag = "ARViewer";

            // Add BrickWallController component
            BrickWallController brickWall = wallController.AddComponent<BrickWallController>();

            // Configure basic properties
            brickWall.wallPlane = plane;
            brickWall.initialDelay = 1.0f + Random.Range(0f, 1.0f);
            brickWall.totalLifetime = DEFAULT_WALL_LIFETIME;
            brickWall.wallNormal = wallNormal;

            // Calculate wall dimensions based on the plane size
            float wallWidth, wallHeight;

            // Calculate from plane size, but scale down slightly for better fit
            // Using horizontal dimension of the plane (width)
            wallWidth = Mathf.Min(plane.size.x * 0.9f, 3.0f); // Max width of 3 meters

            // Calculate from plane size, but scale down
            // Using vertical dimension of the plane (height)
            wallHeight = Mathf.Min(plane.size.y * 0.9f, 2.5f); // Max height of 2.5 meters

            // Ensure minimum height and width
            wallHeight = Mathf.Max(wallHeight, 1.0f); // Minimum 1 meter height
            wallWidth = Mathf.Max(wallWidth, 0.8f); // Minimum 0.8 meter width

            // Set wall dimensions
            brickWall.wallWidth = wallWidth;
            brickWall.wallHeight = wallHeight;

            // Scale brick size proportionally to the wall size
            // This keeps the number of bricks reasonable regardless of wall size
            float scaleFactor = Mathf.Max(wallWidth, wallHeight) / 2.0f; // Normalize by a 2-meter reference
            scaleFactor = Mathf.Clamp(scaleFactor, 0.7f, 1.3f); // Limit scaling range

            // Scale brick size proportionally to the wall size
            brickWall.brickWidth = 0.2f * scaleFactor;
            brickWall.brickHeight = 0.1f * scaleFactor;
            brickWall.brickDepth = 0.1f * scaleFactor;

            // Set falling force based on wall size
            // Larger walls fall with more force
            float sizeFactor = Mathf.Max(wallWidth, wallHeight) / 2.0f; // Normalized by 2m reference
            brickWall.fallingForce = Mathf.Clamp(2.0f * sizeFactor, 1.0f, 5.0f);

            // Add to tracking
            wallObjects.Add(wallController);

            // Record when this plane was used
            string planeId = plane.trackableId.ToString();
            planeLastUsedTime[planeId] = currentTime;
        }
    }
    private void CleanupInvalidWalls()
    {
        // Remove any null or destroyed walls from the list
        for (int i = wallObjects.Count - 1; i >= 0; i--)
        {
            if (wallObjects[i] == null)
            {
                wallObjects.RemoveAt(i);
            }
        }
    }

    private void ClearWallEffects()
    {
        // Destroy all wall objects
        foreach (GameObject wallObj in wallObjects)
        {
            if (wallObj != null)
            {
                GameObject.Destroy(wallObj);
            }
        }

        // Clear tracking collections
        wallObjects.Clear();
        planeWallEffects.Clear();
        planeLastUsedTime.Clear();
    }

    /// <summary>
    /// Finds the nearest floor plane to a given position
    /// </summary>
    private ARPlane FindNearestFloor(Vector3 position)
    {
        ARPlane nearestFloor = null;
        float minDistance = float.MaxValue;

        foreach (var plane in arPlaneManager.trackables)
        {
            if (plane.classification != PlaneClassification.Floor) continue;

            // Calculate horizontal distance
            Vector2 planePos2D = new Vector2(plane.transform.position.x, plane.transform.position.z);
            Vector2 targetPos2D = new Vector2(position.x, position.z);
            float horizontalDistance = Vector2.Distance(planePos2D, targetPos2D);

            // Only consider floors below the wall
            bool isFloorBelow = plane.transform.position.y < position.y;

            if (isFloorBelow && horizontalDistance < 1.5f && horizontalDistance < minDistance)
            {
                minDistance = horizontalDistance;
                nearestFloor = plane;
            }
        }

        return nearestFloor;
    }


    private void ApplyLiquefactionEffect(JSONModel.Feature feature)
    {
        // Enable occlusion for better effect
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
        meshManager.meshPrefab = occPrefab;

        if (!isLiqueActive)
        {
            Reset();
            isLiqueActive = true;
            return;
        }

        GlobalAR.currentWarn = "lique";

        // Get liquefaction mode from feature properties
        LiqueMode liqueMode = LiqueMode.Both; // Default - show both
        if (feature.properties.lique_mode >= 0)
        {
            liqueMode = (LiqueMode)(int)feature.properties.lique_mode;
        }

        // Determine what to display based on mode
        bool showPoles = (liqueMode == LiqueMode.OnlyElectricPole || liqueMode == LiqueMode.Both);
        bool showManhole = (liqueMode == LiqueMode.OnlyManhole || liqueMode == LiqueMode.Both);

        // List to keep track of all placed object positions
        List<Vector3> placedObjectPositions = new List<Vector3>();
        float minimumDistance = 3.0f; // Minimum distance between objects in meters

        foreach (var plane in arPlaneManager.trackables)
        {
            string planeId = plane.trackableId.ToString();
            if (planeLiqueEffects.Contains(planeId)) continue;

            if (plane.classification == PlaneClassification.Floor)
            {
                Vector3 position = plane.transform.position;

                // Check if this position is too close to any existing object
                bool tooClose = false;
                foreach (Vector3 existingPos in placedObjectPositions)
                {
                    if (Vector3.Distance(existingPos, position) < minimumDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                // Skip this plane if it's too close to existing objects
                if (tooClose)
                    continue;

                // Add this position to our tracking list
                placedObjectPositions.Add(position);

                // Use upright rotation
                Quaternion uprightRotation = Quaternion.Euler(-90f, 0f, 0f);

                // Create electric pole
                if (showPoles)
                {
                    GameObject treePrefab = GetCachedPrefab("TreeSwing");
                    GameObject treeInstance = GameObject.Instantiate(treePrefab, position, uprightRotation);
                    treeInstance.tag = "ARViewer";
                }

                // Create manhole
                if (showManhole)
                {
                    GameObject manholePrefab = GetCachedPrefab("manhole");
                    // Create manhole with slight offset if both are shown
                    Vector3 manholePos = showPoles ?
                        new Vector3(position.x + 1, position.y, position.z) :
                        position;

                    GameObject manholeInstance = GameObject.Instantiate(manholePrefab, manholePos, uprightRotation);
                    manholeInstance.tag = "ARViewer";
                }

                // Add to list of planes that already have effects
                planeLiqueEffects.Add(planeId);
            }
        }
    }
    #endregion

    #region Helper Methods

    private void AddRandomGameObjects(string prefabName, int minCount, int maxCount)
    {
        GameObject prefab = GetCachedPrefab(prefabName);
        if (prefab == null) return;

        int count = Random.Range(minCount, maxCount + 1);
        Vector3 cameraPosition = arCameraManager.transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = new Vector3(
                cameraPosition.x + Random.Range(-4.0f, 4.0f),
                cameraPosition.y + 5f, // Start above camera
                cameraPosition.z + Random.Range(-4.0f, 4.0f)
            );

            GameObject instance = GameObject.Instantiate(prefab, randomPosition, Random.rotation);
            instance.tag = "ARViewer";
        }
    }

    private GameObject GetCachedPrefab(string prefabName)
    {
        if (prefabCache.TryGetValue(prefabName, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab != null)
        {
            prefabCache[prefabName] = prefab;
        }
        else
        {
            Debug.LogWarning($"Could not load prefab: {prefabName}");
        }

        return prefab;
    }

    private void UpdateWarningText(int riskType)
    {
        string warning = "";

        switch (riskType)
        {
            case 0:
                warning = "fire";
                break;
            case 1:
            case 7:
                warning = "water";
                break;
            case 2:
                warning = "hill";
                break;
            case 3:
            case 4:
            case 6:
                warning = "house";
                break;
            case 5:
                warning = "wall";
                break;
            case 8:
                warning = "lique";
                break;
        }

        GlobalAR.currentWarn = warning;
    }

    private void UpdateAudio(int riskType)
    {
        bool playHouseAudio = (riskType == 3 || riskType == 4 || riskType == 6);
        bool playRockAudio = (riskType == 2);

        UpdateAudio(playHouseAudio, playRockAudio);
    }

    private void UpdateAudio(string warningType)
    {
        bool playHouseAudio = warningType == "house";
        bool playRockAudio = warningType == "hill";

        UpdateAudio(playHouseAudio, playRockAudio);
    }

    private void UpdateAudio(bool playHouseAudio, bool playRockAudio)
    {
        // Update house collapse audio
        if (playHouseAudio && !houseAudio.isPlaying)
        {
            houseAudio.Play();
        }
        else if (!playHouseAudio && houseAudio.isPlaying)
        {
            houseAudio.Pause();
        }

        // Update rockfall audio
        if (playRockAudio && !rockFallAudio.isPlaying)
        {
            rockFallAudio.Play();
        }
        else if (!playRockAudio && rockFallAudio.isPlaying)
        {
            rockFallAudio.Pause();
        }
    }

    #endregion
}