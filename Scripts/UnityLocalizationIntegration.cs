// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using TMPro;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
#endif

/// <summary>
/// Integration component for Unity's Localization Package.
/// Connects RuntimeTextEditor with Unity's official localization system.
/// 
/// Requirements:
/// - Unity Localization Package installed
/// - LocalizeStringEvent component on the same GameObject
/// - Currently working only in-editor and not in build due to the way unity localization data is built
/// 
/// Features:
/// - Edits raw localization strings (preserves placeholders like {0})
/// - Saves changes back to string tables
/// - Supports Smart String formatting
/// - Handles locale switching
/// </summary>
[RequireComponent(typeof(RuntimeTextEditor))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UnityLocalizationIntegration : MonoBehaviour
{
#if UNITY_2019_1_OR_NEWER
    [Header("Localization Reference")]
    [Tooltip("Reference to the LocalizeStringEvent component (auto-detected if on same GameObject)")]
    public LocalizeStringEvent localizeStringEvent;
    
    [Header("Settings")]
    [Tooltip("Auto-refresh when locale changes")]
    public bool autoRefreshOnLocaleChange = true;
    
    private RuntimeTextEditor textEditor;
    private TextMeshProUGUI textComponent;
    private bool isInitialized = false;
    
    void Awake()
    {
        textEditor = GetComponent<RuntimeTextEditor>();
        textComponent = GetComponent<TextMeshProUGUI>();
        
        // Auto-detect LocalizeStringEvent if not assigned
        if (localizeStringEvent == null)
        {
            localizeStringEvent = GetComponent<LocalizeStringEvent>();
        }
        
        if (localizeStringEvent == null)
        {
            Debug.LogError($"[UnityLocalizationIntegration] No LocalizeStringEvent found on {gameObject.name}. " +
                          "This component requires Unity's LocalizeStringEvent to function.", this);
            enabled = false;
            return;
        }
        
        // Set up raw text provider
        textEditor.SetRawTextProvider(GetRawLocalizedString);
        
        // Set up preview formatter for live preview while editing
        textEditor.SetPreviewFormatter(GetFormattedPreview);
        
        // Subscribe to editor events
        textEditor.onTextSaved += OnTextEdited;
        textEditor.onEditCancelled += OnEditCancelled;
        
        // Subscribe to locale changes
        if (autoRefreshOnLocaleChange)
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }
        
        isInitialized = true;
    }
    
    void Start()
    {
        // Initial refresh - need to wait a frame for LocalizeStringEvent to initialize
        Invoke(nameof(RefreshDisplay), 0.1f);
    }
    
    void OnDestroy()
    {
        if (textEditor != null)
        {
            textEditor.onTextSaved -= OnTextEdited;
            textEditor.onEditCancelled -= OnEditCancelled;
            textEditor.ClearRawTextProvider();
        }
        
        if (autoRefreshOnLocaleChange)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }
    }
    
    /// <summary>
    /// Get the raw localized string from Unity's localization system.
    /// This includes placeholders like {0}, {1}, etc. WITHOUT formatting them.
    /// </summary>
    private string GetRawLocalizedString()
    {
        if (localizeStringEvent == null || localizeStringEvent.StringReference.IsEmpty)
        {
            return string.Empty;
        }
        
        try
        {
            // Get the current locale
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null)
            {
                Debug.LogWarning("[UnityLocalizationIntegration] No locale selected", this);
                return string.Empty;
            }
            
            // Get the table and entry references
            var tableReference = localizeStringEvent.StringReference.TableReference;
            var entryReference = localizeStringEvent.StringReference.TableEntryReference;
            
            // Get the string table for the current locale
            var stringTable = LocalizationSettings.StringDatabase.GetTable(tableReference, currentLocale) as StringTable;
            if (stringTable == null)
            {
                Debug.LogWarning($"[UnityLocalizationIntegration] String table not found: {tableReference}", this);
                return string.Empty;
            }
            
            // Get the entry - this gives us the RAW string with placeholders
            var entry = stringTable.GetEntry((string)entryReference);
            if (entry != null)
            {
                // Return the raw Value, not the LocalizedValue which would be formatted
                return entry.Value;
            }
            else
            {
                Debug.LogWarning($"[UnityLocalizationIntegration] Entry not found: {entryReference}", this);
                return string.Empty;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UnityLocalizationIntegration] Error getting raw string: {e.Message}", this);
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Get formatted preview for the live preview feature.
    /// Takes raw text being edited and formats it with current arguments.
    /// </summary>
    private string GetFormattedPreview(string currentRawText)
    {
        if (string.IsNullOrEmpty(currentRawText))
            return currentRawText;
        
        try
        {
            // If we have arguments set in the StringReference, use them for preview
            if (localizeStringEvent != null && 
                localizeStringEvent.StringReference != null && 
                localizeStringEvent.StringReference.Arguments != null &&
                localizeStringEvent.StringReference.Arguments.Count > 0)
            {
                // Convert Arguments to object array for string.Format
                object[] args = new object[localizeStringEvent.StringReference.Arguments.Count];
                for (int i = 0; i < localizeStringEvent.StringReference.Arguments.Count; i++)
                {
                    args[i] = localizeStringEvent.StringReference.Arguments[i];
                }
                return string.Format(currentRawText, args);
            }
        }
        catch (System.FormatException e)
        {
            return $"<color=red>Format Error: {e.Message}</color>";
        }
        
        return currentRawText;
    }
    
    /// <summary>
    /// Called when user saves edited text in RuntimeTextEditor.
    /// Updates the Unity Localization string table.
    /// </summary>
    private void OnTextEdited(string newRawText)
    {
        if (localizeStringEvent == null || localizeStringEvent.StringReference.IsEmpty)
        {
            Debug.LogWarning("[UnityLocalizationIntegration] Cannot save: No string reference set", this);
            return;
        }
        
        try
        {
            // Get the current locale
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null)
            {
                Debug.LogWarning("[UnityLocalizationIntegration] No locale selected", this);
                return;
            }
            
            // Get the table reference
            var tableReference = localizeStringEvent.StringReference.TableReference;
            var entryReference = localizeStringEvent.StringReference.TableEntryReference;
            
            // Get the string table
            var stringTable = LocalizationSettings.StringDatabase.GetTable(tableReference, currentLocale) as StringTable;
            if (stringTable == null)
            {
                Debug.LogWarning($"[UnityLocalizationIntegration] String table not found: {tableReference}", this);
                return;
            }
            
            // Update or add the entry
            var entry = stringTable.GetEntry((string)entryReference);
            if (entry != null)
            {
                entry.Value = newRawText;
            }
            else
            {
                stringTable.AddEntry(entryReference.ToString(), newRawText);
            }
            
            // Mark table as dirty for editor
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(stringTable);
            UnityEditor.EditorUtility.SetDirty(stringTable.SharedData);
            #endif
            
            Debug.Log($"[UnityLocalizationIntegration] Updated localization: " +
                     $"Table={tableReference}, Key={entryReference}, Value=\"{newRawText}\"", this);
            
            // Refresh the display to show formatted version
            RefreshDisplay();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UnityLocalizationIntegration] Error saving text: {e.Message}", this);
        }
    }
    
    private void OnEditCancelled()
    {
        // Refresh to restore original display
        RefreshDisplay();
    }
    
    private void OnLocaleChanged(Locale newLocale)
    {
        RefreshDisplay();
    }
    
    /// <summary>
    /// Refresh the displayed text from the localization system.
    /// This applies Smart String formatting with any arguments.
    /// </summary>
    public void RefreshDisplay()
    {
        if (!isInitialized || localizeStringEvent == null)
            return;
        
        // The LocalizeStringEvent will automatically update the text
        // We just need to trigger a refresh
        localizeStringEvent.RefreshString();
    }
    
    /// <summary>
    /// Update Smart String arguments and refresh display.
    /// Example: integration.SetArguments(playerName, playerLevel);
    /// </summary>
    public void SetArguments(params object[] arguments)
    {
        if (localizeStringEvent != null && localizeStringEvent.StringReference != null)
        {
            localizeStringEvent.StringReference.Arguments = arguments;
            RefreshDisplay();
        }
    }
    
    /// <summary>
    /// Get the current table and entry references.
    /// </summary>
    public (string table, string key) GetReferences()
    {
        if (localizeStringEvent == null || localizeStringEvent.StringReference.IsEmpty)
        {
            return (string.Empty, string.Empty);
        }
        
        return (
            localizeStringEvent.StringReference.TableReference.ToString(),
            localizeStringEvent.StringReference.TableEntryReference.ToString()
        );
    }
    
#else
    void Awake()
    {
        Debug.LogError("[UnityLocalizationIntegration] Unity Localization Package is not installed. " +
                      "Install it via Package Manager: Window > Package Manager > Localization", this);
        enabled = false;
    }
#endif
}