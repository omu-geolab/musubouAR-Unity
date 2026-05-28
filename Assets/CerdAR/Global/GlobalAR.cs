using System.Collections;
using System.Collections.Generic;
using System.Device.Location;
using UnityEngine;
using JSONModel;

/// <summary>
/// Static class for managing global AR state and location data
/// </summary>
public static class GlobalAR
{
    // Data và resource
    public static float waterLevel = 0.0f;
    public static DataManager dataManager = new DataManager();

    // Location và GPS
    public static double userLat = 34.71470599836312;
    public static double userLng = 135.46561131809713;
    public static double lat2km = 111319.491;
    public static float heading = -67;
    public static float heightAR = 0;
    public static double disCam = 100;
    public static double kNearMsg = 5;
    public static float noticeUpdateTime = 4;
    public static GeoCoordinate currentlocation = new GeoCoordinate(34.71470599836312, 135.46561131809713);
    public static GeoCoordinate prelocation = new GeoCoordinate(0, 0);

    // UI và Dialog
    public static bool isViewDialog = false;
    public static bool isShowedDialog = false;
    public static Feature currentFeature;

    // Loại feature
    public static string kInfo = "info"; // Info type
    public static string kWarn = "warn"; // Warning type
    public static string kPhoto = "photo"; // Photo type
    public static string kMovie = "movie"; // Movie type

    // Trạng thái animation
    public static bool isWaterAction = false;
    public static bool isFireAction = false;

    // Danh sách features
    public static List<Feature> insideWarn = new List<Feature>();
    public static List<Feature> nearWarn = new List<Feature>();
    public static List<Feature> insideAR = new List<Feature>();
    public static List<Feature> outsideAR = new List<Feature>();

    // Game state
    public static string currentWarn = "";
    public static bool isWorldLoad = false;
    public static bool resetFlag = false;
    public static AnimationMode demoMode = 0;

    // Location tracking - Thêm các biến mới
    public static float lastGPSUpdateTime = 0;
    public static Vector2 moveDirection = Vector2.zero;
    public static float moveSpeed = 0;

    // Animation mode enum
    public enum AnimationMode
    {
        None,
        Water,
        Fire,
        Wall,
        Stone,
        Home,
        Lique
    }

    /// <summary>
    /// Update the heading (compass direction) with proper normalization
    /// </summary>
    /// <param name="newHeading">New heading value in degrees (0-359)</param>
    public static void UpdateHeading(float newHeading)
    {
        // Normalize heading to 0-359 range
        while (newHeading < 0)
            newHeading += 360;
        while (newHeading >= 360)
            newHeading -= 360;

        heading = newHeading;
    }

    /// <summary>
    /// Update position and calculate movement data
    /// </summary>
    public static void UpdatePosition(double latitude, double longitude, double altitude = 0)
    {
        // Save current time for movement calculations
        float currentTime = Time.time;
        float timeDelta = currentTime - lastGPSUpdateTime;

        // Save previous location before updating
        prelocation = new GeoCoordinate(currentlocation.Latitude, currentlocation.Longitude);

        // Update current location
        currentlocation = new GeoCoordinate(latitude, longitude, altitude);
        userLat = latitude;
        userLng = longitude;

        // Calculate movement metrics if we have previous location and reasonable time delta
        if (prelocation.Latitude != 0 && prelocation.Longitude != 0 && timeDelta > 0)
        {
            // Get distance moved
            double distance = currentlocation.GetDistanceTo(prelocation);

            // Calculate speed (m/s)
            moveSpeed = (float)(distance / timeDelta);

            // Calculate direction of movement using bearing
            double moveBearing = GeoTool.CalculateBearing(prelocation, currentlocation);

            // Convert bearing to 2D movement vector
            moveDirection = new Vector2(
                Mathf.Sin((float)GeoTool.ToRadian(moveBearing)),
                Mathf.Cos((float)GeoTool.ToRadian(moveBearing))
            );
        }

        // Update timestamp
        lastGPSUpdateTime = currentTime;
    }

    /// <summary>
    /// Predict where the user will be after a time interval, based on current movement
    /// </summary>
    /// <param name="secondsInFuture">Number of seconds to predict forward</param>
    /// <returns>Predicted GeoCoordinate position</returns>
    public static GeoCoordinate PredictFuturePosition(float secondsInFuture)
    {
        // If we have no movement data, return current position
        if (moveSpeed <= 0.1f || prelocation.Latitude == 0)
            return currentlocation;

        // Calculate distance that will be traveled
        float predictedDistance = moveSpeed * secondsInFuture;

        // Calculate bearing of movement
        float predictedBearing = (float)GeoTool.ToDegree(Mathf.Atan2(moveDirection.x, moveDirection.y));

        // Calculate new latitude/longitude using distance and bearing
        // This is a simplified version of the Great Circle formula for small distances
        double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * Mathf.Cos((float)GeoTool.ToRadian(userLat));

        double predictedLat = userLat + (predictedDistance * Mathf.Cos((float)GeoTool.ToRadian(predictedBearing))) / metersPerDegreeLat;
        double predictedLng = userLng + (predictedDistance * Mathf.Sin((float)GeoTool.ToRadian(predictedBearing))) / metersPerDegreeLon;

        return new GeoCoordinate(predictedLat, predictedLng);
    }

    /// <summary>
    /// Get a value indicating if the user is currently moving
    /// </summary>
    public static bool IsMoving()
    {
        // Consider the user moving if speed exceeds a small threshold (e.g., 0.5 m/s or about 1.8 km/h)
        return moveSpeed > 0.5f;
    }
}