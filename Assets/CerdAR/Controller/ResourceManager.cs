using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JSONModel;

/// <summary>
/// Manages resource loading and caching for the application
/// </summary>
public class ResourceManager
{
    // Cache for prefabs to avoid repeated Resources.Load calls
    private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    /// <summary>
    /// Get a prefab from cache or load it if not cached
    /// </summary>
    /// <param name="prefabName">The name of the prefab to load</param>
    /// <returns>The loaded prefab or null if not found</returns>
    public GameObject GetPrefab(string prefabName)
    {
        // Check if already cached
        if (prefabCache.TryGetValue(prefabName, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        // Try to load the prefab
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
        {
            // Try fallback prefabs for common types
            if (prefabName.Contains("icon_warn") || prefabName.Contains("warn"))
            {
                prefab = Resources.Load<GameObject>("icon_warn3");
            }
            else if (prefabName.Contains("info"))
            {
                prefab = Resources.Load<GameObject>("icon_infoTag");
            }

            if (prefab == null)
            {
                Debug.LogWarning($"Failed to load prefab: {prefabName}");
                return null;
            }
        }

        // Cache for future use
        prefabCache[prefabName] = prefab;
        return prefab;
    }

    /// <summary>
    /// Clears the prefab cache to free memory
    /// </summary>
    public void ClearCache()
    {
        prefabCache.Clear();
        // Force garbage collection
        Resources.UnloadUnusedAssets();
    }
}
