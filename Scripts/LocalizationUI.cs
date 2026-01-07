// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public enum LocalizeType
{
    UNITY,
    DEMO,
    CUSTOM
}

/// <summary>
/// DEMO UI controller for the localization system.
/// Handles language switching and edit mode toggle for the demo.
/// </summary>
public class LocalizationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Toggle editModeToggle;
    [SerializeField] private Button saveButton;
    [SerializeField] private TextMeshProUGUI statusText;
    public LocalizeType localizeType;
    
    void Start()
    {
        SetupUI();
    }
    
    void Update()
    {
        // Update status text
        if (statusText != null)
        {
            if (RuntimeTextEditor.IsEditingText)
            {
                statusText.text = "Editing... (Shift+Enter to save, Esc to cancel)";
                statusText.color = Color.yellow;
            }
            else if (editModeToggle != null && editModeToggle.isOn)
            {
                statusText.text = "Edit Mode: Click any text to edit";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Game Mode";
                statusText.color = Color.white;
            }
        }
    }
    
    private void SetupUI()
    {
        if (localizeType == LocalizeType.DEMO)
        {
            SetupDemoLocalization();
        }
        else if (localizeType == LocalizeType.UNITY)
        {
#if UNITY_2019_1_OR_NEWER
            SetupUnityLocalization();
#else
            Debug.LogError("Unity Localization Package is not installed!");
#endif
        }
        
        // Setup edit mode toggle
        if (editModeToggle != null)
        {
            editModeToggle.isOn = false;
            editModeToggle.onValueChanged.AddListener(OnEditModeToggled);
        }
        
        // Setup save button
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
        }
    }
    
    private void SetupDemoLocalization()
    {
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            var languages = LocalizationManager.Instance.GetAvailableLanguages();
            languageDropdown.AddOptions(languages);
            
            // Set current language
            string currentLang = LocalizationManager.Instance.currentLanguage;
            int index = languages.IndexOf(currentLang);
            if (index >= 0)
            {
                languageDropdown.value = index;
            }
            
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }
    
#if UNITY_2019_1_OR_NEWER
    private void SetupUnityLocalization()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LocalizationUI] Language dropdown is null!");
            return;
        }
        
        languageDropdown.ClearOptions();
        
        // Wait for initialization to complete
        var initOp = LocalizationSettings.InitializationOperation;
        if (!initOp.IsDone)
        {
            Debug.Log("[LocalizationUI] Waiting for Unity Localization to initialize...");
            initOp.Completed += OnLocalizationInitialized;
        }
        else
        {
            // Already initialized, populate immediately
            PopulateUnityLocales();
        }
    }
    
    private void OnLocalizationInitialized(AsyncOperationHandle<LocalizationSettings> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("[LocalizationUI] Unity Localization initialized successfully");
            PopulateUnityLocales();
        }
        else
        {
            Debug.LogError($"[LocalizationUI] Unity Localization initialization failed: {obj.OperationException}");
        }
    }
    
    private void PopulateUnityLocales()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LocalizationUI] Language dropdown is null during populate!");
            return;
        }
        
        // Double check that AvailableLocales exists
        if (LocalizationSettings.AvailableLocales == null)
        {
            Debug.LogError("[LocalizationUI] AvailableLocales is null. Check Localization Settings in Project Settings.");
            return;
        }
        
        var availableLocales = LocalizationSettings.AvailableLocales.Locales;
        
        if (availableLocales == null || availableLocales.Count == 0)
        {
            Debug.LogWarning("[LocalizationUI] No locales available. Please add locales in: " +
                           "Edit > Project Settings > Localization > Locale Generator");
            
            // Add a placeholder option so the dropdown isn't empty
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "No Locales Available" });
            return;
        }
        
        Debug.Log($"[LocalizationUI] Found {availableLocales.Count} locales");
        
        List<string> localeNames = new List<string>();
        foreach (var locale in availableLocales)
        {
            localeNames.Add(locale.LocaleName);
            Debug.Log($"[LocalizationUI] Added locale: {locale.LocaleName}");
        }
        
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(localeNames);
        
        // Set current locale
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null)
        {
            int currentIndex = availableLocales.IndexOf(currentLocale);
            if (currentIndex >= 0)
            {
                languageDropdown.value = currentIndex;
                Debug.Log($"[LocalizationUI] Set dropdown to current locale: {currentLocale.LocaleName}");
            }
        }
        else
        {
            Debug.LogWarning("[LocalizationUI] No locale currently selected");
        }
        
        // Subscribe to dropdown changes
        languageDropdown.onValueChanged.AddListener(OnUnityLanguageChanged);
    }
    
    private void OnUnityLanguageChanged(int index)
    {
        var availableLocales = LocalizationSettings.AvailableLocales.Locales;
        if (availableLocales != null && index >= 0 && index < availableLocales.Count)
        {
            LocalizationSettings.SelectedLocale = availableLocales[index];
            
            Debug.Log($"[LocalizationUI] Switched to locale: {availableLocales[index].LocaleName}");
            
            // Refresh all Unity localization components
            UnityLocalizationIntegration[] integrations = FindObjectsOfType<UnityLocalizationIntegration>();
            foreach (var integration in integrations)
            {
                integration.RefreshDisplay();
            }
        }
    }
#endif
    
    private void OnLanguageChanged(int index)
    {
        if (languageDropdown != null && LocalizationManager.Instance != null)
        {
            string language = languageDropdown.options[index].text;
            LocalizationManager.Instance.SwitchLanguage(language);
            
            // Refresh all demo localizable texts
            DemoLocalizableText[] allTexts = FindObjectsOfType<DemoLocalizableText>();
            foreach (var text in allTexts)
            {
                text.UpdateDisplay();
            }
        }
    }
    
    private void OnEditModeToggled(bool isOn)
    {
        // Enable/disable edit mode for all RuntimeTextEditors
        RuntimeTextEditor[] editors = FindObjectsOfType<RuntimeTextEditor>();
        foreach (var editor in editors)
        {
            editor.EditModeEnabled = isOn;
        }
    }
    
    private void OnSaveClicked()
    {
        if (localizeType == LocalizeType.DEMO && LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SaveLocalizationData();
        }
#if UNITY_2019_1_OR_NEWER
        else if (localizeType == LocalizeType.UNITY)
        {
            // Unity Localization saves are handled automatically by UnityLocalizationIntegration
            Debug.Log("[LocalizationUI] Unity Localization changes saved to string tables");
        }
#endif
        
        if (statusText != null)
        {
            statusText.text = "Saved!";
            statusText.color = Color.cyan;
        }
    }
    
}