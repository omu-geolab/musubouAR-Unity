using System.Collections;
using UnityEngine;
using System.Device.Location;

/// <summary>
/// Location tracking class for AR application
/// </summary>
public class LocationData : MonoBehaviour
{
    /// <summary>GPS update interval in seconds</summary>
    private const float GPS_INTERVAL = 0.5f;

    /// <summary>Heading update interval in seconds</summary>
    private const float HEADING_INTERVAL = 0.1f; // Update heading more frequently

    /// <summary>Minimum distance change threshold (meters)</summary>
    private const float MIN_DISTANCE_CHANGE = 0.5f;

    /// <summary>Location service status</summary>
    private LocationServiceStatus locationServiceStatus;

    /// <summary>Smoothing factor for heading (0-1)</summary>
    [Range(0.1f, 1.0f)]
    public float headingSmoothingFactor = 0.3f;

    /// <summary>Previous heading value for smoothing</summary>
    private float previousHeading = 0f;

    /// <summary>Debug mode to log location updates</summary>
    public bool debugMode = false;

    /// <summary>Start coroutines when component initializes</summary>
    void Start()
    {
        // Start GPS updates with slightly delayed initialization
        StartCoroutine(InitLocationServices());
    }

    /// <summary>Initialize location services with proper permission checking</summary>
    private IEnumerator InitLocationServices()
    {
        // Check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services are not enabled by user");
            yield break;
        }

        // Start compass
        Input.compass.enabled = true;

        // Start location service with desired accuracy
        Input.location.Start(1.0f, MIN_DISTANCE_CHANGE);

        // Wait until service initializes
        int maxWait = 20; // Maximum seconds to wait
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Check if service initialization timed out or failed
        if (maxWait <= 0)
        {
            Debug.LogWarning("Location services initialization timed out");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("Location services failed to initialize");
            yield break;
        }

        // Start continuous update coroutines
        StartCoroutine(UpdateGPSData());
        StartCoroutine(UpdateHeading());

        if (debugMode)
            Debug.Log("Location services initialized successfully");
    }

    /// <summary>Update GPS location data at regular intervals</summary>
    private IEnumerator UpdateGPSData()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                // Get current location data
                double currentLat = Input.location.lastData.latitude;
                double currentLng = Input.location.lastData.longitude;
                double currentAlt = Input.location.lastData.altitude;
                float currentAccuracy = Input.location.lastData.horizontalAccuracy;

                // Only update if accuracy is acceptable
                if (currentAccuracy < 100) // Less than 100 meters accuracy
                {
                    // Update Global AR location data
                    GlobalAR.UpdatePosition(currentLat, currentLng, currentAlt);

                    if (debugMode)
                    {
                        Debug.Log($"GPS: Lat: {currentLat}, Lng: {currentLng}, " +
                                 $"Accuracy: {currentAccuracy}m, Speed: {GlobalAR.moveSpeed}m/s");
                    }
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"Low GPS accuracy: {currentAccuracy}m - data not used");
                }
            }

            // Wait before next update
            yield return new WaitForSeconds(GPS_INTERVAL);
        }
    }

    /// <summary>Update heading/compass data at regular intervals</summary>
    private IEnumerator UpdateHeading()
    {
        while (true)
        {
            if (Input.compass.enabled)
            {
                // Get current heading (0-359 degrees, 0 = North, clockwise)
                float rawHeading = Input.compass.trueHeading;

                // Apply smoothing between previous and new heading
                float smoothedHeading = ApplyHeadingSmoothing(rawHeading);

                // Update global heading value
                GlobalAR.UpdateHeading(smoothedHeading);

                // Store current heading as previous for next update
                previousHeading = smoothedHeading;

                if (debugMode && Time.frameCount % 100 == 0)
                {
                    Debug.Log($"Heading: {smoothedHeading}? (Raw: {rawHeading}?)");
                }
            }

            // Wait before next update - more frequent than GPS updates
            yield return new WaitForSeconds(HEADING_INTERVAL);
        }
    }

    /// <summary>Apply smoothing to heading values to prevent jittering</summary>
    private float ApplyHeadingSmoothing(float newHeading)
    {
        // If this is the first reading, just use the raw value
        if (previousHeading == 0f)
            return newHeading;

        // Handle the case where heading crosses 0/360 boundary
        float diff = Mathf.Abs(newHeading - previousHeading);
        if (diff > 180)
        {
            // We're crossing the 0/360 boundary
            if (newHeading > previousHeading)
                previousHeading += 360;
            else
                newHeading += 360;
        }

        // Apply smoothing factor
        float smoothedHeading = Mathf.Lerp(previousHeading, newHeading, headingSmoothingFactor);

        // Normalize back to 0-360 range
        while (smoothedHeading >= 360)
            smoothedHeading -= 360;
        while (smoothedHeading < 0)
            smoothedHeading += 360;

        return smoothedHeading;
    }
}