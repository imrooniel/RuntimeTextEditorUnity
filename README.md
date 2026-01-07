# RuntimeTextEditorUnity
A plugin that turns any text field in the game editable, meant to streamline writing and localization, by providing text editing functionality within context of a game


## Quick Start

### Using the Batch Setup Tool

The fastest way to add RuntimeTextEditor to your project:

1. Open **Tools → Localization → Batch Add RuntimeTextEditor to Prefabs**
2. Configure search settings (folder, text types, collider options)
3. Click **Scan Prefabs** to find all TextMeshPro components
4. Review the list and deselect any components you don't want to modify
5. Click **Apply RuntimeTextEditor to Selected**

The tool automatically adds:
- RuntimeTextEditor component
- BoxCollider2D (for UI text, optional)

### Manual Setup

If you need to add components manually:
```csharp
GameObject textObject = /* your text object */;
textObject.AddComponent();
textObject.AddComponent(); // Required for click detection
```

## Integration Options

### Option 1: Demo Localization System

Use the included demo system for custom localization implementations.

**Setup:**

1. Place your localization JSON file in `StreamingAssets/`:
```json
{
    "entries": [
        {
            "textID": "welcome",
            "languageID": "en",
            "textContent": "Welcome to the game!"
        }
    ]
}
```

2. Add components to your text object (use batch tool or manually):
```csharp
textObject.AddComponent();
var localizable = textObject.AddComponent();
localizable.textID = "welcome";
```

3. Add to your scene:
- LocalizationManager component
- LocalizationUI component (for language switching and edit mode controls)

### Option 2: Unity Localization Package

For projects using Unity's official localization system.

**Setup:**

1. Install Unity Localization Package via Package Manager
2. Configure your string tables in Unity's Localization Settings
3. Add components (use batch tool or manually):
```csharp
textObject.AddComponent(); // Unity's component
textObject.AddComponent();
textObject.AddComponent();
```

4. Configure the LocalizeStringEvent with your table and entry references

**Note:** Currently works in editor only. Build support in progress.

### Option 3: Custom Localization System

Integrate with your own localization system:
```csharp
[RequireComponent(typeof(RuntimeTextEditor))]
public class CustomLocalizationIntegration : MonoBehaviour
{
    private RuntimeTextEditor textEditor;
    
    void Awake()
    {
        textEditor = GetComponent();
        
        // Provide raw text (with placeholders like {0})
        textEditor.SetRawTextProvider(() => 
            YourSystem.GetRawText(key)
        );
        
        // Provide formatted preview
        textEditor.SetPreviewFormatter(rawText => 
            string.Format(rawText, arg1, arg2)
        );
        
        // Handle saves
        textEditor.onTextSaved += (newText) => {
            YourSystem.UpdateText(key, newText);
            RefreshDisplay();
        };
    }
}
```

## Runtime Usage

**Controls:**
- Click text to edit (when edit mode is enabled)
- `Shift + Enter` to save
- `Esc` to cancel

**Enable edit mode:**
```csharp
runtimeTextEditor.EditModeEnabled = true;
```

**Prevent input conflicts:**
```csharp
void Update()
{
    if (RuntimeTextEditor.IsEditingText)
        return;
    
    // Your game input code
}
```

## API Reference

### RuntimeTextEditor

**Properties:**
```csharp
static bool IsEditingText { get; }
bool EditModeEnabled { get; set; }
```

**Methods:**
```csharp
void SetRawTextProvider(Func provider)
void SetPreviewFormatter(Func formatter)
void ClearRawTextProvider()
void SetShowLivePreview(bool show)
void StartEdit()
void SetDisplayText(string text)
string GetDisplayText()
```

**Events:**
```csharp
Action onTextSaved
Action onEditCancelled
Func onPreviewTextRequested
```
