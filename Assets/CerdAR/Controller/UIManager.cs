using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JSONModel;

/// <summary>
/// Manages all UI elements and interactions
/// </summary>
public class UIManager
{
    private readonly GameObject dialogPanel;
    private readonly GameObject noticePanel;

    private readonly List<string> noticeMessages = new List<string>();

    public UIManager(GameObject dialogPanel, GameObject noticePanel)
    {
        this.dialogPanel = dialogPanel;
        this.noticePanel = noticePanel;
    }

    /// <summary>
    /// Hide all UI panels
    /// </summary>
    public void HideAll()
    {
        dialogPanel.SetActive(false);
        noticePanel.SetActive(false);
    }

    /// <summary>
    /// Show the information dialog
    /// </summary>
    public void ShowDialog()
    {
        dialogPanel.SetActive(true);
        GlobalAR.isShowedDialog = true;
    }

    /// <summary>
    /// Hide the information dialog
    /// </summary>
    public void HideDialog()
    {
        dialogPanel.SetActive(false);
        GlobalAR.isShowedDialog = false;
        GlobalAR.isViewDialog = false;
    }

    /// <summary>
    /// Update the notice messages from warning features
    /// </summary>
    public void UpdateNoticeMessages(List<Feature> insideWarnings, List<Feature> nearWarnings)
    {
        bool hasWarnings = (insideWarnings.Count > 0 || nearWarnings.Count > 0);

        // Add new messages if needed
        if (hasWarnings && noticeMessages.Count == 0)
        {
            // Add messages from features inside warning zone
            foreach (Feature feature in insideWarnings)
            {
                if (!string.IsNullOrEmpty(feature.properties.message2))
                {
                    noticeMessages.Add(feature.properties.message2);
                }
            }

            // Add messages from features near warning zone
            foreach (Feature feature in nearWarnings)
            {
                if (!string.IsNullOrEmpty(feature.properties.message1))
                {
                    noticeMessages.Add(feature.properties.message1);
                }
            }
        }

        // Update notice visibility
        noticePanel.SetActive(hasWarnings || noticeMessages.Count > 0);

        // Display a message if available
        if (noticeMessages.Count > 0)
        {
            Text noticeText = GameObject.Find("status")?.GetComponent<Text>();
            if (noticeText != null)
            {
                string message = noticeMessages[0];
                noticeText.text = message;
                noticeMessages.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Add a custom notice message
    /// </summary>
    public void AddNotice(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            noticeMessages.Add(message);
            noticePanel.SetActive(true);
        }
    }

    /// <summary>
    /// Reset the UI state
    /// </summary>
    public void Reset()
    {
        noticeMessages.Clear();
        HideAll();
        GlobalAR.isShowedDialog = false;
        GlobalAR.isViewDialog = false;
    }
}