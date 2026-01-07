// RuntimeTextEditor for Unity | Copyright (c) 2025 Patryk Kowalik | MIT License

using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to automatically add RuntimeTextEditor to all TextMeshPro components in prefabs.
/// Access via: Tools > Localization > Batch Add RuntimeTextEditor to Prefabs
/// </summary>
public class BatchRuntimeTextEditorSetup : EditorWindow
{
    private Vector2 scrollPosition;
    private List<PrefabTextInfo> foundTexts = new List<PrefabTextInfo>();
    private bool includeUIText = true;
    private bool includeWorldText = true;
    private bool skipIfAlreadyExists = true;
    private bool autoAddBoxCollider = true;
    private string searchFolder = "Assets";
    private bool showPreview = false;
    
    private class PrefabTextInfo
    {
        public GameObject prefab;
        public TextMeshProUGUI uiText;
        public TextMeshPro worldText;
        public string path;
        public bool hasRuntimeEditor;
        public bool selected = true;
        
        public TextMeshProUGUI GetUIText() => uiText;
        public TextMeshPro GetWorldText() => worldText;
        public bool IsUI => uiText != null;
        public string GetTextContent() => IsUI ? uiText.text : worldText.text;
    }
    
    [MenuItem("Tools/Localization/Batch Add RuntimeTextEditor to Prefabs")]
    public static void ShowWindow()
    {
        BatchRuntimeTextEditorSetup window = GetWindow<BatchRuntimeTextEditorSetup>("Batch RuntimeTextEditor Setup");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Batch RuntimeTextEditor Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool scans all prefabs in your project and adds RuntimeTextEditor component to TextMeshPro components.\n\n" +
            "Step 1: Configure search settings\n" +
            "Step 2: Click 'Scan Prefabs' to find all TextMeshPro components\n" +
            "Step 3: Review the list and deselect any you don't want to modify\n" +
            "Step 4: Click 'Apply RuntimeTextEditor to Selected' to modify prefabs",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Settings Section
        EditorGUILayout.LabelField("Search Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        
        searchFolder = EditorGUILayout.TextField("Search Folder", searchFolder);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder to Search", searchFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert absolute path to relative Assets path
                if (path.StartsWith(Application.dataPath))
                {
                    searchFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        if (GUILayout.Button("Reset to Assets", GUILayout.Width(120)))
        {
            searchFolder = "Assets";
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        includeUIText = EditorGUILayout.Toggle("Include TextMeshPro UI", includeUIText);
        includeWorldText = EditorGUILayout.Toggle("Include TextMeshPro 3D", includeWorldText);
        skipIfAlreadyExists = EditorGUILayout.Toggle("Skip if RuntimeTextEditor exists", skipIfAlreadyExists);
        autoAddBoxCollider = EditorGUILayout.Toggle("Auto-add BoxCollider2D (UI only)", autoAddBoxCollider);
        
        if (EditorGUI.EndChangeCheck())
        {
            foundTexts.Clear();
        }
        
        EditorGUILayout.Space(10);
        
        // Scan Button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Prefabs", GUILayout.Height(30)))
        {
            ScanPrefabs();
        }
        if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
        {
            foundTexts.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Results Section
        if (foundTexts.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {foundTexts.Count} TextMeshPro components", EditorStyles.boldLabel);
            
            int selectedCount = foundTexts.Count(t => t.selected);
            EditorGUILayout.LabelField($"Selected: {selectedCount} / {foundTexts.Count}");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (var text in foundTexts) text.selected = true;
            }
            if (GUILayout.Button("Deselect All"))
            {
                foreach (var text in foundTexts) text.selected = false;
            }
            if (GUILayout.Button("Invert Selection"))
            {
                foreach (var text in foundTexts) text.selected = !text.selected;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            showPreview = EditorGUILayout.Toggle("Show Text Preview", showPreview);
            EditorGUILayout.Space(5);
            
            // Scrollable list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            foreach (var textInfo in foundTexts)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                textInfo.selected = EditorGUILayout.Toggle(textInfo.selected, GUILayout.Width(20));
                
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(textInfo.prefab.name, EditorStyles.boldLabel, GUILayout.Width(250));
                
                // Type badge
                GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
                badgeStyle.normal.textColor = textInfo.IsUI ? new Color(0.3f, 0.7f, 1f) : new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField(textInfo.IsUI ? "[UI]" : "[3D]", badgeStyle, GUILayout.Width(40));
                
                if (textInfo.hasRuntimeEditor)
                {
                    GUIStyle hasStyle = new GUIStyle(EditorStyles.miniLabel);
                    hasStyle.normal.textColor = Color.green;
                    EditorGUILayout.LabelField("✓ Has RuntimeTextEditor", hasStyle, GUILayout.Width(150));
                }
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = textInfo.prefab;
                    EditorGUIUtility.PingObject(textInfo.prefab);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField(textInfo.path, EditorStyles.miniLabel);
                
                if (showPreview)
                {
                    string preview = textInfo.GetTextContent();
                    if (preview.Length > 60)
                        preview = preview.Substring(0, 60) + "...";
                    EditorGUILayout.LabelField($"Text: \"{preview}\"", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // Apply Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Apply RuntimeTextEditor to {selectedCount} Selected Items", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Batch Operation",
                    $"This will add RuntimeTextEditor component to {selectedCount} TextMeshPro components in prefabs.\n\n" +
                    "This operation can be undone with Ctrl+Z.\n\nContinue?",
                    "Yes, Apply",
                    "Cancel"))
                {
                    ApplyRuntimeTextEditor();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("No prefabs scanned yet. Click 'Scan Prefabs' to begin.", MessageType.Info);
        }
    }
    
    private void ScanPrefabs()
    {
        foundTexts.Clear();
        
        // Find all prefabs in the specified folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });
        
        EditorUtility.DisplayProgressBar("Scanning Prefabs", "Loading prefabs...", 0f);
        
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            EditorUtility.DisplayProgressBar("Scanning Prefabs", 
                $"Scanning {prefab.name}... ({i + 1}/{prefabGuids.Length})", 
                (float)i / prefabGuids.Length);
            
            // Check UI Text
            if (includeUIText)
            {
                TextMeshProUGUI[] uiTexts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var uiText in uiTexts)
                {
                    bool hasEditor = uiText.GetComponent<RuntimeTextEditor>() != null;
                    
                    if (skipIfAlreadyExists && hasEditor)
                        continue;
                    
                    foundTexts.Add(new PrefabTextInfo
                    {
                        prefab = prefab,
                        uiText = uiText,
                        path = path,
                        hasRuntimeEditor = hasEditor
                    });
                }
            }
            
            // Check 3D Text
            if (includeWorldText)
            {
                TextMeshPro[] worldTexts = prefab.GetComponentsInChildren<TextMeshPro>(true);
                foreach (var worldText in worldTexts)
                {
                    bool hasEditor = worldText.GetComponent<RuntimeTextEditor>() != null;
                    
                    if (skipIfAlreadyExists && hasEditor)
                        continue;
                    
                    foundTexts.Add(new PrefabTextInfo
                    {
                        prefab = prefab,
                        worldText = worldText,
                        path = path,
                        hasRuntimeEditor = hasEditor
                    });
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"[BatchRuntimeTextEditorSetup] Found {foundTexts.Count} TextMeshPro components in {prefabGuids.Length} prefabs");
    }
    
    private void ApplyRuntimeTextEditor()
    {
        var selectedTexts = foundTexts.Where(t => t.selected).ToList();
        int successCount = 0;
        int errorCount = 0;
        
        EditorUtility.DisplayProgressBar("Applying RuntimeTextEditor", "Processing...", 0f);
        
        for (int i = 0; i < selectedTexts.Count; i++)
        {
            var textInfo = selectedTexts[i];
            
            EditorUtility.DisplayProgressBar("Applying RuntimeTextEditor", 
                $"Processing {textInfo.prefab.name}... ({i + 1}/{selectedTexts.Count})", 
                (float)i / selectedTexts.Count);
            
            try
            {
                // Load prefab for editing
                string prefabPath = AssetDatabase.GetAssetPath(textInfo.prefab);
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
                
                // Find the text component in the instance
                Component textComponent = null;
                if (textInfo.IsUI)
                {
                    TextMeshProUGUI[] uiTexts = prefabInstance.GetComponentsInChildren<TextMeshProUGUI>(true);
                    textComponent = uiTexts.FirstOrDefault(t => t.text == textInfo.GetTextContent());
                }
                else
                {
                    TextMeshPro[] worldTexts = prefabInstance.GetComponentsInChildren<TextMeshPro>(true);
                    textComponent = worldTexts.FirstOrDefault(t => t.text == textInfo.GetTextContent());
                }
                
                if (textComponent != null)
                {
                    GameObject textObject = textComponent.gameObject;
                    
                    // Add RuntimeTextEditor if it doesn't exist
                    if (textObject.GetComponent<RuntimeTextEditor>() == null)
                    {
                        textObject.AddComponent<RuntimeTextEditor>();
                    }
                    
                    // Add BoxCollider2D for UI text if requested and doesn't exist
                    if (textInfo.IsUI && autoAddBoxCollider)
                    {
                        if (textObject.GetComponent<BoxCollider2D>() == null)
                        {
                            textObject.AddComponent<BoxCollider2D>();
                        }
                    }
                    
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                    successCount++;
                }
                
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BatchRuntimeTextEditorSetup] Error processing {textInfo.prefab.name}: {e.Message}");
                errorCount++;
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"Batch operation completed!\n\n" +
                        $"Successfully processed: {successCount}\n" +
                        $"Errors: {errorCount}";
        
        EditorUtility.DisplayDialog("Batch Operation Complete", message, "OK");
        
        Debug.Log($"[BatchRuntimeTextEditorSetup] Completed: {successCount} success, {errorCount} errors");
        
        ScanPrefabs();
    }
}