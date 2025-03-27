using UnityEngine;
using UnityEngine.UI;
using System;
public class SimpleYoutubePlayer : MonoBehaviour
{
    private GameObject dialogPanel;
    private Button closeButton;
    private WebViewObject webView;
    private Canvas canvas;

    // Track current orientation to detect changes
    private ScreenOrientation lastOrientation;

    private void Start()
    {
        // Store initial orientation
        lastOrientation = Screen.orientation;

        // Create UI elements on startup
        CreateUIElements();

        // Initialize the WebViewObject
        InitializeWebView();
    }

    private void Update()
    {
        // Check for orientation changes
        if (Screen.orientation != lastOrientation && dialogPanel.activeSelf)
        {
            // Update the last known orientation
            lastOrientation = Screen.orientation;

            // Wait a bit for the screen to settle after rotation
            StartCoroutine(UpdateLayoutAfterOrientationChange());
        }
    }

    private System.Collections.IEnumerator UpdateLayoutAfterOrientationChange()
    {
        // Wait for the screen to finish rotating and UI to update
        yield return new WaitForSeconds(0.5f);

        // Update WebView margins to match new orientation
        UpdateWebViewMargins();
    }

    private void CreateUIElements()
    {
        // Find canvas in the scene or create a new one if not found
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Create a new canvas if none exists
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        // Create the dialog panel
        dialogPanel = new GameObject("YouTubeDialogPanel");
        dialogPanel.transform.SetParent(canvas.transform, false);

        // Add image component as panel background
        Image panelImage = dialogPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f);

        // Set panel size and position (larger panel with smaller margins)
        RectTransform panelRect = dialogPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f); // Changed from 0.1f to 0.05f (smaller margins)
        panelRect.anchorMax = new Vector2(0.95f, 0.95f); // Changed from 0.9f to 0.95f (smaller margins)
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Create close button with transparent background
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(dialogPanel.transform, false);

        // Add transparent image component for the button (for click detection)
        Image buttonImage = closeButtonObj.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0f); // Completely transparent

        // Add Text for the button
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeButtonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "×"; // Using the multiplication symbol
        buttonText.fontSize = 48; // Increased font size for larger button
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white; // White text as requested
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Set button size and position (larger size)
        RectTransform buttonRect = closeButtonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.sizeDelta = new Vector2(60, 60); // Increased from 44x44 to 60x60
        buttonRect.anchoredPosition = new Vector2(-15, -15);

        // Set text size and position
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Add shadow effect to text for better contrast against any background
        Shadow textShadow = textObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0, 0, 0, 0.7f);
        textShadow.effectDistance = new Vector2(1, -1);

        // Add button component and event listener
        closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.targetGraphic = buttonImage;

        // Create color transition that only affects the text
        ColorBlock colors = closeButton.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        closeButton.colors = colors;

        // Add click event
        closeButton.onClick.AddListener(CloseDialog);

        // Hide dialog when initialized
        dialogPanel.SetActive(false);
    }
    private void InitializeWebView()
    {
        // Create a new WebViewObject
        webView = new GameObject("WebViewObject").AddComponent<WebViewObject>();

        // Set up properties for the WebViewObject
        webView.Init(
            cb: (msg) => {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
            },
            err: (msg) => {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
            },
            httpErr: (msg) => {
                Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
            },
            started: (msg) => {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) => {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            ld: (msg) => {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
            },
            enableWKWebView: true
        );

        // Initial setup of margins
        UpdateWebViewMargins();

        // Additional configuration for WebView
        webView.SetVisibility(false); // Hide when initialized
        webView.SetScrollbarsVisibility(true);
        webView.SetAlertDialogEnabled(true);
    }

    // Calculate and update margins based on current screen orientation
    private void UpdateWebViewMargins()
    {
        if (webView == null) return;

        // Get current screen dimensions
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        // Calculate margins based on panel position and size (reduced margins)
        // Leave space for the close button
        int leftMargin = (int)(screenWidth * 0.08f);  // Reduced from 0.15f to 0.08f
        int topMargin = (int)(screenHeight * 0.08f) + 50; // Reduced from 0.15f to 0.08f, but added more space for larger button
        int rightMargin = (int)(screenWidth * 0.08f);  // Reduced from 0.15f to 0.08f
        int bottomMargin = (int)(screenHeight * 0.08f); // Reduced from 0.15f to 0.08f

        // Update WebView margins
        webView.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);
    }

    // Open dialog with YouTube URL
    public void OpenYouTubeDialog(string youtubeUrl)
    {
        // Show dialog
        dialogPanel.SetActive(true);

        // Update margins to ensure proper sizing for current orientation
        UpdateWebViewMargins();

        // Convert YouTube URL to embed URL
        string embedUrl = ConvertToEmbedUrl(youtubeUrl);

        // Show and load URL in WebView
        webView.SetVisibility(true);
        webView.LoadURL(embedUrl);
    }

    // Close dialog
    public void CloseDialog()
    {
        // Properly stop YouTube video by loading about:blank page
        webView.LoadURL("about:blank");

        // Execute JavaScript to ensure any iframes are also properly stopped
        webView.EvaluateJS(@"
            var iframes = document.getElementsByTagName('iframe');
            for (var i = 0; i < iframes.length; i++) {
                iframes[i].src = '';
            }
        ");

        // Then pause the WebView
        webView.Pause();

        // Hide UI elements
        dialogPanel.SetActive(false);
        webView.SetVisibility(false);
    }

    // Convert regular YouTube URL to embed URL
    private string ConvertToEmbedUrl(string youtubeUrl)
    {
        // Extract video ID from URL
        string videoId = "";

        // Handle empty or null URL
        if (string.IsNullOrEmpty(youtubeUrl))
        {
            Debug.LogError("YouTube URL is empty or null");
            return "";
        }

        try
        {
            if (youtubeUrl.Contains("youtu.be"))
            {
                int startIndex = youtubeUrl.LastIndexOf("/") + 1;
                int endIndex = youtubeUrl.IndexOf("?", startIndex);

                if (endIndex == -1)
                    videoId = youtubeUrl.Substring(startIndex);
                else
                    videoId = youtubeUrl.Substring(startIndex, endIndex - startIndex);
            }
            else if (youtubeUrl.Contains("youtube.com/watch"))
            {
                int startIndex = youtubeUrl.IndexOf("v=") + 2;
                int endIndex = youtubeUrl.IndexOf("&", startIndex);

                if (endIndex == -1)
                    videoId = youtubeUrl.Substring(startIndex);
                else
                    videoId = youtubeUrl.Substring(startIndex, endIndex - startIndex);
            }
            else if (youtubeUrl.Contains("youtube.com/embed"))
            {
                int startIndex = youtubeUrl.LastIndexOf("/") + 1;
                int endIndex = youtubeUrl.IndexOf("?", startIndex);

                if (endIndex == -1)
                    videoId = youtubeUrl.Substring(startIndex);
                else
                    videoId = youtubeUrl.Substring(startIndex, endIndex - startIndex);

                // If already an embed URL, we can just return it directly
                return youtubeUrl;
            }
            // Unknown format
            else
            {
                Debug.LogWarning("Unrecognized YouTube URL format: " + youtubeUrl);
                // Attempt to use the URL as-is
                return youtubeUrl;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing YouTube URL: " + e.Message);
            return youtubeUrl; // Return original URL as fallback
        }

        // Create embed URL with parameters for better mobile experience
        return "https://www.youtube.com/embed/" + videoId + "?autoplay=1&rel=0&playsinline=1";
    }

    private void OnDestroy()
    {
        // Clean up when component is destroyed
        if (webView != null)
        {
            Destroy(webView.gameObject);
        }
        if (dialogPanel != null)
        {
            Destroy(dialogPanel);
        }
    }
}