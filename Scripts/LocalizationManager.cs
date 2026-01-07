// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// DEMO localization system - simple example for demonstration purposes.
/// This showcases how the RuntimeTextEditor can integrate with a localization backend.
/// 
/// In production, you would replace this with your actual localization system:
/// - i18n libraries
/// - Google Sheets integration
/// - Custom server-side localization
/// - Asset bundle localization
/// - etc.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    
    [Header("Settings")]
    public string currentLanguage = "en";
    
    [Header("File Path")]
    [SerializeField] private string localizationFileName = "localization_data.json";
    
    private LocalizationData localizationData;
    private Dictionary<string, Dictionary<string, string>> textCache;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeLocalization()
    {
        textCache = new Dictionary<string, Dictionary<string, string>>();
        LoadLocalizationData();
        BuildCache();
    }
    
    private void LoadLocalizationData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, localizationFileName);
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            localizationData = JsonUtility.FromJson<LocalizationData>(json);
            Debug.Log($"[DEMO] Loaded {localizationData.entries.Count} localization entries");
        }
        else
        {
            Debug.LogWarning($"[DEMO] Localization file not found at {filePath}. Creating default data.");
            CreateDefaultData();
        }
    }
    
    private void CreateDefaultData()
    {
        localizationData = new LocalizationData();
        localizationData.entries = new List<LocalizationEntry>();
    }
    
    private void BuildCache()
    {
        textCache.Clear();
        
        foreach (var entry in localizationData.entries)
        {
            if (!textCache.ContainsKey(entry.textID))
            {
                textCache[entry.textID] = new Dictionary<string, string>();
            }
            
            textCache[entry.textID][entry.languageID] = entry.textContent;
        }
    }
    
    public string GetText(string textID)
    {
        if (textCache.ContainsKey(textID) && textCache[textID].ContainsKey(currentLanguage))
        {
            return textCache[textID][currentLanguage];
        }
        
        Debug.LogWarning($"[DEMO] Text not found for ID: {textID}, Language: {currentLanguage}");
        return $"{textID}";
    }
    
    public void UpdateText(string textID, string newText)
    {
        // Update in cache
        if (!textCache.ContainsKey(textID))
        {
            textCache[textID] = new Dictionary<string, string>();
        }
        textCache[textID][currentLanguage] = newText;
        
        // Update in data structure
        var entry = localizationData.entries.FirstOrDefault(e => 
            e.textID == textID && e.languageID == currentLanguage);
        
        if (entry != null)
        {
            entry.textContent = newText;
        }
        else
        {
            localizationData.entries.Add(new LocalizationEntry
            {
                textID = textID,
                languageID = currentLanguage,
                textContent = newText
            });
        }
        
        Debug.Log($"[DEMO] Updated text for {textID} ({currentLanguage}): {newText}");
    }
    
    public void SwitchLanguage(string languageID)
    {
        currentLanguage = languageID;
        Debug.Log($"[DEMO] Switched to language: {languageID}");
    }
    
    public void SaveLocalizationData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, localizationFileName);
        string json = JsonUtility.ToJson(localizationData, true);
        
        // Create directory if it doesn't exist
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(filePath, json);
        Debug.Log($"[DEMO] Saved localization data to {filePath}");
    }
    
    public List<string> GetAvailableLanguages()
    {
        var languages = new HashSet<string>();
        foreach (var entry in localizationData.entries)
        {
            languages.Add(entry.languageID);
        }
        return languages.ToList();
    }
}

[System.Serializable]
public class LocalizationData
{
    public List<LocalizationEntry> entries;
}

[System.Serializable]
public class LocalizationEntry
{
    public string textID;
    public string languageID;
    public string textContent;
}