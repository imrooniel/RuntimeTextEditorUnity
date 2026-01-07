// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using TMPro;

/// <summary>
/// DEMO INTEGRATION EXAMPLE - Shows how to integrate RuntimeTextEditor with a localization system.
/// This demonstrates ONE way to connect the runtime editor to your localization backend.
/// 
/// In your own project, you would:
/// 1. Add RuntimeTextEditor to your text
/// 2. Create your own integration script (like this one) for YOUR localization system
/// 3. Use SetRawTextProvider() to provide unformatted text for editing
/// 4. Subscribe to onTextSaved to update your localization database
/// </summary>
[RequireComponent(typeof(RuntimeTextEditor))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class DemoLocalizableText : MonoBehaviour
{
    [Header("Localization Reference")]
    [Tooltip("The unique ID for this text in the localization system")]
    public string textID;
    
    [Header("Placeholder Values (Demo)")]
    [Tooltip("Values to substitute into placeholders like {0}, {1}, etc.")]
    public string[] placeholderValues;
    
    private RuntimeTextEditor textEditor;
    private TextMeshProUGUI textComponent;
    
    void Awake()
    {
        textEditor = GetComponent<RuntimeTextEditor>();
        textComponent = GetComponent<TextMeshProUGUI>();
        
        // IMPORTANT: Set up raw text provider so editor gets unformatted text with placeholders
        textEditor.SetRawTextProvider(GetRawText);
        
        // IMPORTANT: Set up preview formatter so user sees formatted text while editing
        textEditor.SetPreviewFormatter(GetFormattedTextForPreview);
        
        // Subscribe to save events - this connects the editor to the demo localization system
        textEditor.onTextSaved += OnTextEdited;
    }
    
    void Start()
    {
        // Load and display formatted text
        RefreshText();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (textEditor != null)
        {
            textEditor.onTextSaved -= OnTextEdited;
            textEditor.ClearRawTextProvider();
        }
    }
    
    /// <summary>
    /// Get the RAW localization text (with placeholders like {0}, {1}, etc.)
    /// This is what the editor shows when user clicks to edit.
    /// </summary>
    private string GetRawText()
    {
        if (!string.IsNullOrEmpty(textID) && LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetText(textID);
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Get formatted text with placeholders replaced by actual values.
    /// This is what gets displayed to the player.
    /// </summary>
    private string GetFormattedText()
    {
        string rawText = GetRawText();
        
        if (string.IsNullOrEmpty(rawText))
            return rawText;
        
        if (placeholderValues != null && placeholderValues.Length > 0)
        {
            try
            {
                return string.Format(rawText, placeholderValues);
            }
            catch (System.FormatException)
            {
                Debug.LogWarning($"[DEMO] Format error for {textID}. Raw text: {rawText}");
                return rawText;
            }
        }
        
        return rawText;
    }
    
    /// <summary>
    /// Get formatted text for live preview during editing.
    /// Takes the current raw text being edited and formats it.
    /// </summary>
    private string GetFormattedTextForPreview(string currentRawText)
    {
        if (string.IsNullOrEmpty(currentRawText))
            return currentRawText;
        
        if (placeholderValues != null && placeholderValues.Length > 0)
        {
            try
            {
                return string.Format(currentRawText, placeholderValues);
            }
            catch (System.FormatException e)
            {
                return $"<color=red>Format Error: {e.Message}</color>";
            }
        }
        
        return currentRawText;
    }
    
    /// <summary>
    /// Load and display the formatted localized text.
    /// </summary>
    private void RefreshText()
    {
        string formattedText = GetFormattedText();
        if (!string.IsNullOrEmpty(formattedText))
        {
            textEditor.SetDisplayText(formattedText);
        }
    }
    
    /// <summary>
    /// Called when user saves edited text in the RuntimeTextEditor.
    /// THIS IS WHERE YOU INTEGRATE WITH YOUR LOCALIZATION SYSTEM.
    /// </summary>
    private void OnTextEdited(string newRawText)
    {
        // In this demo, we update the demo LocalizationManager
        // In your project, you would call YOUR localization system's API
        
        if (!string.IsNullOrEmpty(textID) && LocalizationManager.Instance != null)
        {
            // Save the RAW text (with placeholders) to the localization system
            LocalizationManager.Instance.UpdateText(textID, newRawText);
            
            Debug.Log($"[DEMO] Updated localization entry: {textID} = '{newRawText}'");
            
            RefreshText();
        }
    }
    
    /// <summary>
    /// Call this when language changes or placeholder values change to refresh the text.
    /// In your project, your localization system would call this.
    /// </summary>
    public void UpdateDisplay()
    {
        RefreshText();
    }
    
    /// <summary>
    /// Update placeholder values and refresh display.
    /// Example: scoreText.SetPlaceholderValues(new[] { playerScore.ToString() });
    /// </summary>
    public void SetPlaceholderValues(params string[] values)
    {
        placeholderValues = values;
        RefreshText();
    }
}