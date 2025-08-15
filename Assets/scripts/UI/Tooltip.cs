using UnityEngine;
using TMPro;
using UnityEngine.UI; // Required for LayoutGroup and ContentSizeFitter

public class Tooltip : MonoBehaviour
{
    public TextMeshProUGUI tooltipText;
    public GameObject tooltipPanel;

    private RectTransform panelRectTransform;
    private LayoutElement layoutElement; // To control preferred width/height if needed

    [Range(0, 1000)]
    public float maxWidth = 200f; // Maximum width for the tooltip

    [Range(1, 50)]
    public float minFontSize = 8f; // Minimum font size for auto-sizing
    [Range(1, 50)]
    public float maxFontSize = 24f; // Maximum font size for auto-sizing

    void Awake()
    {
        panelRectTransform = tooltipPanel.GetComponent<RectTransform>();
        layoutElement = tooltipText.GetComponent<LayoutElement>();

        // Programmatically add and configure Layout Group and Content Size Fitter
        VerticalLayoutGroup layoutGroup = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        }
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // Or MiddleCenter, based on preference
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5); // Add some padding

        ContentSizeFitter contentFitter = tooltipPanel.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        }
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Configure TextMeshPro for auto-sizing
        if (tooltipText != null)
        {
            tooltipText.enableAutoSizing = true;
            tooltipText.fontSizeMin = minFontSize;
            tooltipText.fontSizeMax = maxFontSize;
        }

        // Ensure the tooltip panel is initially hidden
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }

        // Ensure the tooltip panel has a background image and set its color
        Image backgroundImage = tooltipPanel.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = tooltipPanel.AddComponent<Image>();
        }
        // Set a default background color (e.g., semi-transparent black or a solid light color)
        backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark grey with some transparency

        // New: Add and configure CanvasGroup to prevent mouse interaction
        CanvasGroup canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false; // Important: Make it transparent to raycasts
        canvasGroup.interactable = false; // Also make it non-interactable

        // Set tooltip panel to be the last sibling to ensure highest Z-order
        if (tooltipPanel.transform.parent != null)
        {
            tooltipPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowTooltip(string content)
    {
        Debug.Log($"ShowTooltip called with content: {content}");
        if (tooltipPanel != null)
        {
            if (tooltipText != null)
            {
                tooltipText.text = content;
                // Force text mesh pro to update its layout immediately
                tooltipText.ForceMeshUpdate(); 

                // Calculate the preferred width and height of the text
                Vector2 preferredTextSize = tooltipText.GetRenderedValues(false);

                // Adjust the preferred width of the LayoutElement for word wrapping
                if (layoutElement != null)
                {
                    layoutElement.preferredWidth = Mathf.Min(preferredTextSize.x, maxWidth);
                    // Set preferredHeight to -1 (disabled) to let ContentSizeFitter handle it
                    layoutElement.preferredHeight = -1;
                }

                // Optional: Position the tooltip relative to the mouse or hovered element
                // For now, let's keep it simple and ensure it's positioned at the mouse cursor
                Vector2 mousePos = Input.mousePosition;
                // Offset from mouse position for better visibility
                float xOffset = 10f; 
                float yOffset = -10f; 

                panelRectTransform.position = mousePos + new Vector2(xOffset, yOffset);

                // Ensure tooltip stays within screen bounds (more advanced, can be added later)
            }
            tooltipPanel.SetActive(true);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
