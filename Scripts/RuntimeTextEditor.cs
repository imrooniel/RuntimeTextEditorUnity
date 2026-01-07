// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Runtime text editing component - allows in-game text editing with click-to-edit functionality.
/// 
/// Features:
/// - Click text to edit at runtime
/// - Shift+Enter to save, Escape to cancel
/// - Preview shown in original text, input field overlays above
/// - Global IsEditingText flag for pausing game controls
/// - Supports raw text providers for proper handling of placeholders like {0}
/// - Callbacks for save/cancel events to integrate with your localization system
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(BoxCollider2D))]
public class RuntimeTextEditor : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    [SerializeField] private Color editHighlightColor = new Color(0.3f, 0.5f, 0.7f, 1f);
    [SerializeField] private bool editModeEnabled = false;
    [SerializeField] private bool showLivePreview = true;
    
    [Header("Events")]
    [Tooltip("Called when user saves edited text. Parameter: new raw text content")]
    public System.Action<string> onTextSaved;
    
    [Tooltip("Called when user cancels editing")]
    public System.Action onEditCancelled;
    
    [Tooltip("Called when text changes during editing. Parameter: current raw text. Return: formatted preview text")]
    public System.Func<string, string> onPreviewTextRequested;
    
    private TextMeshProUGUI textComponent;
    private BoxCollider2D hitbox;
    private TMP_InputField tempInputField;
    private GameObject inputFieldObj;
    private string originalText;
    private bool isEditing = false;
    private float previewUpdateTimer = 0f;
    private const float previewUpdateInterval = 0.3f; // Update preview every 0.3 seconds
    
    // Delegate for getting raw text from localization system (with placeholders like {0})
    private System.Func<string> getRawTextProvider;
    
    /// <summary>
    /// Global flag indicating if ANY text is currently being edited.
    /// Use this to pause game controls during text editing.
    /// </summary>
    public static bool IsEditingText { get; private set; } = false;
    
    /// <summary>
    /// Enable or disable edit mode for this text. When disabled, clicking does nothing.
    /// </summary>
    public bool EditModeEnabled
    {
        get => editModeEnabled;
        set => editModeEnabled = value;
    }
    
    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        hitbox = GetComponent<BoxCollider2D>();
        
        hitbox.isTrigger = true;
        UpdateHitbox();
    }
    
    void Update()
    {
        if (isEditing)
        {
            if (showLivePreview)
            {
                previewUpdateTimer += Time.deltaTime;
                if (previewUpdateTimer >= previewUpdateInterval)
                {
                    UpdatePreview();
                    previewUpdateTimer = 0f;
                }
            }
            
            // Check for save (Shift + Enter)
            if (IsShiftPressed() && IsEnterPressed())
            {
                SaveEdit();
            }
            // Check for cancel (Escape)
            else if (IsEscapePressed())
            {
                CancelEdit();
            }
        }
    }
    
    private bool IsShiftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && 
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
    
    private bool IsEnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }
    
    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isEditing && editModeEnabled)
        {
            StartEdit();
        }
    }
    
    /// <summary>
    /// Set a provider function that returns the RAW text (with placeholders like {0}).
    /// This is used when editing to show the unformatted localization string.
    /// If not set, will fall back to the displayed TextMeshPro text.
    /// 
    /// Example: editor.SetRawTextProvider(() => YourLocalizationSystem.GetRawText(key));
    /// </summary>
    public void SetRawTextProvider(System.Func<string> provider)
    {
        getRawTextProvider = provider;
    }
    
    /// <summary>
    /// Set a callback that formats raw text for preview display.
    /// This shows the user what the final text will look like with placeholders filled in.
    /// 
    /// Example: editor.SetPreviewFormatter(raw => string.Format(raw, playerName, score));
    /// </summary>
    public void SetPreviewFormatter(System.Func<string, string> formatter)
    {
        onPreviewTextRequested = formatter;
    }
    
    /// <summary>
    /// Clear the raw text provider, falling back to TextMeshPro text.
    /// </summary>
    public void ClearRawTextProvider()
    {
        getRawTextProvider = null;
    }
    
    /// <summary>
    /// Enable or disable live preview while editing.
    /// </summary>
    public void SetShowLivePreview(bool show)
    {
        showLivePreview = show;
    }
    
    /// <summary>
    /// Get the raw text for editing. Uses provider if available, otherwise falls back to displayed text.
    /// </summary>
    private string GetRawTextForEditing()
    {
        if (getRawTextProvider != null)
        {
            return getRawTextProvider.Invoke();
        }
        
        // Fallback to displayed text
        return textComponent != null ? textComponent.text : string.Empty;
    }
    
    /// <summary>
    /// Programmatically start editing this text.
    /// </summary>
    public void StartEdit()
    {
        if (isEditing) return;
        
        isEditing = true;
        IsEditingText = true;
        
        originalText = textComponent.text;
        
        CreateInputField();
        
        if (showLivePreview)
        {
            UpdatePreview();
        }
    }
    
    private void CreateInputField()
    {
        inputFieldObj = new GameObject("TempInputField");
        
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            inputFieldObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            inputFieldObj.transform.SetParent(transform.parent, false);
        }
        
        RectTransform inputRect = inputFieldObj.AddComponent<RectTransform>();
        RectTransform textRect = textComponent.rectTransform;
        
        LayoutElement layoutElement = inputFieldObj.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        
        inputRect.SetParent(canvas != null ? canvas.transform : transform.parent, true);
        
        Vector3[] worldCorners = new Vector3[4];
        textRect.GetWorldCorners(worldCorners);
        
        float width = Vector3.Distance(worldCorners[0], worldCorners[3]);
        float height = Vector3.Distance(worldCorners[0], worldCorners[1]);
        
        Vector3 centerPos = (worldCorners[0] + worldCorners[2]) / 2f;
        float offsetY = height + 10f;
        centerPos.y += offsetY;
        
        inputRect.position = centerPos;
        inputRect.sizeDelta = new Vector2(width, Mathf.Max(height, 40f));
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image bg = inputFieldObj.AddComponent<Image>();
        bg.color = editHighlightColor;
        RectTransform bgTransform = inputFieldObj.GetComponent<RectTransform>();
        Vector2 editBoxVec2 = new Vector2();
        editBoxVec2.x = Mathf.Max(200, textComponent.rectTransform.sizeDelta.x);
        editBoxVec2.y = Mathf.Max(20, textComponent.rectTransform.sizeDelta.y);
        bgTransform.sizeDelta = editBoxVec2;
        
        tempInputField = inputFieldObj.AddComponent<TMP_InputField>();
        
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputFieldObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(5, 5);
        textAreaRect.offsetMax = new Vector2(-5, -5);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textObjRect = textObj.AddComponent<RectTransform>();
        textObjRect.anchorMin = Vector2.zero;
        textObjRect.anchorMax = Vector2.one;
        textObjRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = textComponent.fontSize;
        inputText.font = textComponent.font;
        inputText.alignment = textComponent.alignment;
        inputText.color = Color.black;
        
        tempInputField.textViewport = textAreaRect;
        tempInputField.textComponent = inputText;
        tempInputField.text = GetRawTextForEditing(); // Get raw text with placeholders
        tempInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        
        // Subscribe to text changes for live preview
        if (showLivePreview)
        {
            tempInputField.onValueChanged.AddListener(OnInputFieldTextChanged);
        }
        
        // Focus the input field
        tempInputField.ActivateInputField();
        tempInputField.Select();
    }
    
    private void OnInputFieldTextChanged(string newText)
    {
        // Reset timer to update preview soon
        previewUpdateTimer = previewUpdateInterval * 0.8f; // Update quickly after change
    }
    
    private void UpdatePreview()
    {
        if (textComponent == null || tempInputField == null)
            return;
        
        string currentRawText = tempInputField.text;
        string formattedText;
        
        // Try to get formatted preview from callback
        if (onPreviewTextRequested != null)
        {
            try
            {
                formattedText = onPreviewTextRequested.Invoke(currentRawText);
            }
            catch (System.Exception e)
            {
                formattedText = $"<color=red>Preview Error: {e.Message}</color>";
            }
        }
        else
        {
            // No formatter provided, show raw text
            formattedText = currentRawText;
        }
        textComponent.text = formattedText;
    }
    
    private void SaveEdit()
    {
        if (tempInputField != null)
        {
            string newText = tempInputField.text;
            
            // Invoke callback with raw text - YOUR localization system handles the save and formatting
            onTextSaved?.Invoke(newText);
        }
        
        EndEdit();
    }
    
    private void CancelEdit()
    {
        textComponent.text = originalText;
        
        onEditCancelled?.Invoke();
        EndEdit();
    }
    
    private void EndEdit()
    {
        if (inputFieldObj != null)
        {
            Destroy(inputFieldObj);
        }
        
        previewUpdateTimer = 0f;
        
        isEditing = false;
        IsEditingText = false;
    }
    
    /// <summary>
    /// Update the displayed text programmatically (e.g., when language changes or text is updated).
    /// This sets the DISPLAYED text (after formatting/placeholders are applied).
    /// </summary>
    public void SetDisplayText(string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }
    
    /// <summary>
    /// Get current displayed text content.
    /// </summary>
    public string GetDisplayText()
    {
        return textComponent != null ? textComponent.text : string.Empty;
    }
    
    private void UpdateHitbox()
    {
        if (textComponent != null && hitbox != null)
        {
            RectTransform rect = textComponent.rectTransform;
            hitbox.size = rect.sizeDelta;
            hitbox.offset = Vector2.zero;
        }
    }
    
    void OnValidate()
    {
        UpdateHitbox();
    }
}