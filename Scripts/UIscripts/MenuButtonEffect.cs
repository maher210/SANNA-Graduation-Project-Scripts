using UnityEngine;
using UnityEngine.UI; // Required for the Image component
using UnityEngine.EventSystems;

public class MenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 hoveredScale = new Vector3(1.1f, 1.1f, 1.1f);

    [Header("Color/Brightness Settings")]
    [Tooltip("The default color of the button")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("The color when hovered/selected. Make this brighter or a different hue!")]
    [SerializeField] private Color hoveredColor = new Color(1.3f, 1.3f, 1.3f, 1f); // Values above 1.0f create HDR brightness

    [Header("Animation Speed")]
    [SerializeField] private float transitionSpeed = 15f;

    private Image buttonImage;
    private Vector3 targetScale;
    private Color targetColor;

    private void Awake()
    {
        // Automatically grab the Image component attached to this GameObject
        buttonImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Initialize scales
        transform.localScale = normalScale;
        targetScale = normalScale;

        // Initialize colors
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
            targetColor = normalColor;
        }
    }

    private void Update()
    {
        // Smoothly animate scale using unscaledDeltaTime so it works while paused
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        // Smoothly animate color using unscaledDeltaTime so it works while paused
        if (buttonImage != null)
        {
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    // --- MOUSE HOVER EVENTS ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        HighlightButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
        {
            ResetButton();
        }
    }

    // --- CONTROLLER / KEYBOARD NAVIGATION EVENTS ---
    public void OnSelect(BaseEventData eventData)
    {
        HighlightButton();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ResetButton();
    }

    // --- HELPER METHODS ---
    private void HighlightButton()
    {
        targetScale = hoveredScale;
        targetColor = hoveredColor;
    }

    private void ResetButton()
    {
        targetScale = normalScale;
        targetColor = normalColor;
    }

    private void OnDisable()
    {
        // Reset instantly if the menu panel is closed
        transform.localScale = normalScale;
        targetScale = normalScale;
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
            targetColor = normalColor;
        }
    }
}