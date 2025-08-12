using UnityEngine;
using TMPro; // For TextMeshPro

public class FloatingDamageNumbers : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeSpeed = 1f;
    public float displayDuration = 1.5f; // How long the number stays visible
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Offset from the unit's position
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color missColor = Color.gray; // New color for miss
    public TMP_FontAsset bebassNeueFontAsset; // Changed to TMP_FontAsset

    private TextMeshPro textMesh;
    private float currentDuration;
    private Camera mainCamera; // Reference to the main camera

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
        }
        else
        {
        }
    }

    void Start()
    {
        currentDuration = displayDuration;
        textMesh.color = damageColor; // Default to red for damage

        // Set up TextMeshPro properties
        textMesh.fontSize = 9;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        
        if (bebassNeueFontAsset != null)
        {
            textMesh.font = bebassNeueFontAsset;
        }
        else
        {
            Debug.LogWarning("BebasNeue-Regular SDF Font Asset not assigned in Inspector for FloatingDamageNumbers. Using default font.");
        }

        // Apply initial offset
        transform.position += offset;

        mainCamera = Camera.main; // Get reference to the main camera
    }

    void Update()
    {
        // Make sure the text always faces the camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        // Move upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Fade out
        currentDuration -= Time.deltaTime;
        float alpha = Mathf.Clamp01(currentDuration / displayDuration);
        Color newColor = textMesh.color;
        newColor.a = alpha;
        textMesh.color = newColor;

        // Destroy when faded out
        if (currentDuration <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(float value, bool isCritical = false, bool isHeal = false, bool isMiss = false)
    {
        if (isMiss)
        {
            textMesh.text = "MISS!";
            textMesh.color = missColor;
            textMesh.fontSize = 2.5f; // Slightly smaller for MISS
        }
        else
        {
            textMesh.text = value.ToString("F0");

            if (isHeal)
            {
                textMesh.color = healColor;
                textMesh.text = "+" + textMesh.text;
            }
            else
            {
                textMesh.color = damageColor;
            }

            if (isCritical)
            {
                textMesh.fontSize *= 1.5f; // Make critical hits larger
                textMesh.text += "!"; // Add an exclamation mark
            }
        }
    }
}
