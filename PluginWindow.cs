using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// ... rest of your code
public class PluginManagerrrrrrrrr : EditorWindow
{
    [Serializable]
    public class PluginManifestEntry
    {
        public string pluginName = "New Plugin";
        public string className = "MyPluginClass";
        public string uploader = "Anonymous";
        public string description = "Custom Valvy plugin";
        [TextArea(5, 15)]
        public string sourceCode = "";
    }

    [Serializable]
    public class ProjectProfile
    {
        public string projectName = "Default Project";
        public List<PluginManifestEntry> plugins = new List<PluginManifestEntry>();
    }

    [Serializable]
    private class MultiProjectManifest
    {
        public int activeProjectIndex = 0;
        public List<ProjectProfile> projects = new List<ProjectProfile>();
    }

    private static readonly string MANIFEST_PATH = "Assets/valvy/ValvyPluginManifest.json";
    private static readonly string INSTALL_DIRECTORY = "Assets/valvy/Plugins/MyPlugins";

    private MultiProjectManifest manifest = new MultiProjectManifest();

    // UI States
    private bool showUploadSection = false;
    private int selectedTab = 0; // 0 = All Plugins, 1+ = Individual Plugins
    private Vector2 communityScrollPos;
    private Vector2 codeScrollPos;
    private Vector2 pasteScrollPos;

    // Inputs
    private string newProjectNameInput = "";
    private string uploadAuthorName = "DevUser";
    private string uploadDescription = "Uploaded Valvy plugin";
    private string pastedCodeInput = "";
    private string pastedClassNameInput = "";

    [MenuItem("Valvy/Plugin Manager")]
    public static void ShowWindow()
    {
        PluginManagerrrrrrrrr window = GetWindow<PluginManagerrrrrrrrr>("Valvy Plugin Manager");
        window.minSize = new Vector2(580, 580);
        window.Show();
    }

    private void OnEnable()
    {
        LoadManifest();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(5);
        DrawProjectSelector();

        EditorGUILayout.Space(10);
        DrawTopToolbar();

        if (showUploadSection)
        {
            EditorGUILayout.Space(5);
            DrawUploadAndPastePanel();
        }

        EditorGUILayout.Space(10);
        DrawPluginTabs();

        EditorGUILayout.Space(10);
        if (selectedTab == 0)
        {
            DrawCommunityUploadsTab();
        }
        else
        {
            DrawIndividualTabContent();
        }
    }

    private void DrawHeader()
    {
        Rect headerRect = EditorGUILayout.GetControlRect(false, 35);
        EditorGUI.DrawRect(headerRect, new Color(0.118f, 0.118f, 0.118f)); // #1E1E1E

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        GUI.Label(headerRect, "Valvy Multi-Project Plugin Manager", headerStyle);
    }

    private void DrawProjectSelector()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Active Project Profile:", EditorStyles.boldLabel, GUILayout.Width(140));

                if (manifest.projects.Count > 0)
                {
                    List<string> projectNames = new List<string>();
                    for (int i = 0; i < manifest.projects.Count; i++)
                    {
                        projectNames.Add(manifest.projects[i].projectName);
                    }

                    int newIndex = EditorGUILayout.Popup(manifest.activeProjectIndex, projectNames.ToArray());
                    if (newIndex != manifest.activeProjectIndex)
                    {
                        manifest.activeProjectIndex = newIndex;
                        selectedTab = 0;
                        SaveManifest();
                    }
                }
            }

            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                newProjectNameInput = EditorGUILayout.TextField("New Project Name", newProjectNameInput);

                if (GUILayout.Button("Create Project", GUILayout.Width(110)))
                {
                    if (string.IsNullOrEmpty(newProjectNameInput))
                    {
                        EditorUtility.DisplayDialog("Error", "Please enter a valid Project Name!", "OK");
                        return;
                    }

                    manifest.projects.Add(new ProjectProfile
                    {
                        projectName = newProjectNameInput,
                        plugins = new List<PluginManifestEntry>()
                    });

                    manifest.activeProjectIndex = manifest.projects.Count - 1;
                    selectedTab = 0;
                    newProjectNameInput = "";
                    SaveManifest();
                }
            }
        }
    }

    private void DrawTopToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = showUploadSection ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Button(showUploadSection ? "▲ Hide Creation Panel" : "➕ Add / Paste / Upload New Plugin", GUILayout.Height(30)))
            {
                showUploadSection = !showUploadSection;
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawUploadAndPastePanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("1. Quick Details", EditorStyles.boldLabel);
            uploadAuthorName = EditorGUILayout.TextField("Author Name", uploadAuthorName);
            uploadDescription = EditorGUILayout.TextField("Description", uploadDescription);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. Paste C# Code Directly", EditorStyles.boldLabel);
            pastedClassNameInput = EditorGUILayout.TextField("Class Name (e.g. MyPlugin)", pastedClassNameInput);

            EditorGUILayout.LabelField("Paste C# Script Code Below:");
            pasteScrollPos = EditorGUILayout.BeginScrollView(pasteScrollPos, GUILayout.Height(100));
            pastedCodeInput = EditorGUILayout.TextArea(pastedCodeInput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("📋 Submit Pasted Code", GUILayout.Height(28)))
            {
                if (string.IsNullOrEmpty(pastedClassNameInput))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a Class Name for the pasted code!", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(pastedCodeInput))
                {
                    EditorUtility.DisplayDialog("Error", "Code box is empty!", "OK");
                    return;
                }

                string cleanClassName = pastedClassNameInput.Replace(" ", "");
                RegisterOrUpdatePlugin(cleanClassName, cleanClassName, uploadAuthorName, uploadDescription, pastedCodeInput);

                pastedCodeInput = "";
                pastedClassNameInput = "";
                showUploadSection = false;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. Or Import From File", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📁 Select C# Script File...", GUILayout.Height(25)))
                {
                    UploadScriptFromFileBrowser();
                }
            }

            Rect dropArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "\nDrag & Drop (.cs) Script File Here", EditorStyles.helpBox);

            Event evt = Event.current;
            if (dropArea.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObj in DragAndDrop.objectReferences)
                        {
                            if (draggedObj is MonoScript script)
                            {
                                AddPluginFromScript(script);
                            }
                        }
                    }
                    evt.Use();
                }
            }
        }
    }

    private ProjectProfile GetActiveProject()
    {
        if (manifest.projects == null || manifest.projects.Count == 0)
        {
            manifest.projects = new List<ProjectProfile>
            {
                new ProjectProfile { projectName = "Default Project" }
            };
            manifest.activeProjectIndex = 0;
        }

        if (manifest.activeProjectIndex < 0 || manifest.activeProjectIndex >= manifest.projects.Count)
        {
            manifest.activeProjectIndex = 0;
        }

        return manifest.projects[manifest.activeProjectIndex];
    }

    private void UploadScriptFromFileBrowser()
    {
        string filePath = EditorUtility.OpenFilePanel("Select C# Plugin Script", "", "cs");
        if (string.IsNullOrEmpty(filePath)) return;

        string code = File.ReadAllText(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        RegisterOrUpdatePlugin(fileName, fileName, uploadAuthorName, uploadDescription, code);
        showUploadSection = false;
    }

    private void AddPluginFromScript(MonoScript script)
    {
        Type scriptClass = script.GetClass();
        string className = scriptClass != null ? scriptClass.Name : script.name;
        string assetPath = AssetDatabase.GetAssetPath(script);
        string code = File.ReadAllText(assetPath);

        RegisterOrUpdatePlugin(script.name, className, uploadAuthorName, uploadDescription, code);
        showUploadSection = false;
    }

    private void RegisterOrUpdatePlugin(string name, string className, string author, string desc, string code)
    {
        ProjectProfile activeProject = GetActiveProject();

        foreach (var p in activeProject.plugins)
        {
            if (p.className == className)
            {
                p.sourceCode = code;
                p.uploader = author;
                p.description = desc;
                SaveManifest();
                EditorUtility.DisplayDialog("Updated!", $"Updated code for '{className}'!", "OK");
                return;
            }
        }

        activeProject.plugins.Add(new PluginManifestEntry
        {
            pluginName = name,
            className = className,
            uploader = author,
            description = desc,
            sourceCode = code
        });

        SaveManifest();
        selectedTab = activeProject.plugins.Count;
        EditorUtility.DisplayDialog("Success!", $"Added '{name}' to project!", "OK");
    }

    private void DrawPluginTabs()
    {
        ProjectProfile activeProject = GetActiveProject();
        List<string> tabNames = new List<string> { "🌐 All Project Plugins" };

        for (int i = 0; i < activeProject.plugins.Count; i++)
        {
            tabNames.Add(activeProject.plugins[i].pluginName);
        }

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames.ToArray(), GUILayout.Height(25));
    }

    private void DrawCommunityUploadsTab()
    {
        ProjectProfile activeProject = GetActiveProject();

        EditorGUILayout.LabelField($"Project: {activeProject.projectName} ({activeProject.plugins.Count} Plugins)", EditorStyles.boldLabel);

        if (activeProject.plugins.Count == 0)
        {
            EditorGUILayout.HelpBox($"No plugins saved under '{activeProject.projectName}' yet! Click 'Add / Paste / Upload New Plugin' above to get started.", MessageType.Info);
            return;
        }

        communityScrollPos = EditorGUILayout.BeginScrollView(communityScrollPos);

        for (int i = 0; i < activeProject.plugins.Count; i++)
        {
            var plugin = activeProject.plugins[i];

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"📦 {plugin.pluginName} (Class: {plugin.className})", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Author: {plugin.uploader}", EditorStyles.miniLabel, GUILayout.Width(180));
                }

                EditorGUILayout.LabelField($"Description: {plugin.description}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Install & Create Code", GUILayout.Height(30)))
                    {
                        InstallAndRecreateCode(plugin);
                    }

                    if (GUILayout.Button("View / Edit Code", GUILayout.Width(140), GUILayout.Height(30)))
                    {
                        selectedTab = i + 1;
                    }
                }
            }
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawIndividualTabContent()
    {
        ProjectProfile activeProject = GetActiveProject();
        int pluginIndex = selectedTab - 1;
        if (pluginIndex < 0 || pluginIndex >= activeProject.plugins.Count) return;

        PluginManifestEntry activePlugin = activeProject.plugins[pluginIndex];

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            activePlugin.pluginName = EditorGUILayout.TextField("Plugin Name", activePlugin.pluginName);
            activePlugin.className = EditorGUILayout.TextField("C# Class Name", activePlugin.className);
            activePlugin.uploader = EditorGUILayout.TextField("Author", activePlugin.uploader);
            activePlugin.description = EditorGUILayout.TextField("Description", activePlugin.description);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Source Code:", EditorStyles.boldLabel);

            codeScrollPos = EditorGUILayout.BeginScrollView(codeScrollPos, GUILayout.Height(180));
            activePlugin.sourceCode = EditorGUILayout.TextArea(activePlugin.sourceCode, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Install & Create Code", GUILayout.Height(35)))
                {
                    InstallAndRecreateCode(activePlugin);
                }

                if (GUILayout.Button("Save Changes", GUILayout.Height(35), GUILayout.Width(110)))
                {
                    SaveManifest();
                }

                if (GUILayout.Button("Delete Plugin", GUILayout.Height(35), GUILayout.Width(90)))
                {
                    activeProject.plugins.RemoveAt(pluginIndex);
                    SaveManifest();
                    selectedTab = 0;
                }
            }
        }
    }

    private void InstallAndRecreateCode(PluginManifestEntry pluginData)
    {
        if (string.IsNullOrEmpty(pluginData.className))
        {
            EditorUtility.DisplayDialog("Error", "Class Name is missing!", "OK");
            return;
        }

        string cleanClassName = pluginData.className.Replace(" ", "");

        if (!Directory.Exists(INSTALL_DIRECTORY))
        {
            Directory.CreateDirectory(INSTALL_DIRECTORY);
        }

        string filePath = Path.Combine(INSTALL_DIRECTORY, $"{cleanClassName}.cs");

        // 1. Generate file on disk if missing
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, pluginData.sourceCode);
            AssetDatabase.Refresh();

            ApplyPluginIconToScript(filePath);

            EditorUtility.DisplayDialog("Script Generated!", $"Created script '{cleanClassName}.cs' at {INSTALL_DIRECTORY}.\n\nClick 'Install & Create Code' again once Unity finishes compiling!", "OK");
            return;
        }

        ApplyPluginIconToScript(filePath);

        // 2. Search compiled assemblies
        Type scriptClass = FindTypeByName(cleanClassName);

        if (scriptClass == null)
        {
            EditorUtility.DisplayDialog("Compiling...", $"Script file exists at '{filePath}' but isn't compiled yet. Please wait a second and try again.", "OK");
            return;
        }

        // 3. Ensure MonoBehaviour
        if (!typeof(MonoBehaviour).IsAssignableFrom(scriptClass) && !typeof(Component).IsAssignableFrom(scriptClass))
        {
            EditorUtility.DisplayDialog("Notice", $"Script '{cleanClassName}.cs' created successfully!\n\nNote: Class does not inherit from MonoBehaviour, so it cannot be attached as a GameObject component.", "OK");
            return;
        }

        // 4. Attach component
        GameObject valvyObj = GameObject.Find("Valvy");
        if (valvyObj != null)
        {
            if (valvyObj.GetComponent(scriptClass) == null)
            {
                Undo.AddComponent(valvyObj, scriptClass);
                EditorUtility.DisplayDialog("Success!", $"Attached component '{scriptClass.Name}' to '{valvyObj.name}'!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Notice", $"Component '{scriptClass.Name}' is already attached to '{valvyObj.name}'.", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Notice", $"Script created and compiled, but could not find a 'Valvy' GameObject in the scene.", "OK");
        }
    }

    private void ApplyPluginIconToScript(string scriptPath)
    {
        MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/valvy/Icons/pluginvalvy.png");
        if (iconTexture == null)
        {
            iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/valvy/pluginvalvy.png");
        }

        if (scriptAsset != null && iconTexture != null)
        {
            EditorGUIUtility.SetIconForObject(scriptAsset, iconTexture);
            EditorUtility.SetDirty(scriptAsset);
        }
    }

    private Type FindTypeByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                        type.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return type;
                    }
                }
            }
            catch { }
        }
        return null;
    }

    private void SaveManifest()
    {
        string dir = Path.GetDirectoryName(MANIFEST_PATH);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(MANIFEST_PATH, json);
        AssetDatabase.Refresh();
    }

    private void LoadManifest()
    {
        if (File.Exists(MANIFEST_PATH))
        {
            string json = File.ReadAllText(MANIFEST_PATH);
            MultiProjectManifest loaded = JsonUtility.FromJson<MultiProjectManifest>(json);
            if (loaded != null && loaded.projects != null && loaded.projects.Count > 0)
            {
                manifest = loaded;
            }
        }

        GetActiveProject();
    }
}
#endif