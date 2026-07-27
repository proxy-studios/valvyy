#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ValvyEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;

    // Color Palette
    private static readonly Color DarkBg = new Color(0.08f, 0.08f, 0.08f, 1f);      // #141414
    private static readonly Color CardBg = new Color(0.12f, 0.12f, 0.12f, 1f);      // #1e1e1e
    private static readonly Color PrimaryBlue = new Color(0f, 0.48f, 1f, 1f);       // #007aff

    [MenuItem("Valvy/Dashboard")]
    public static void ShowWindow()
    {
        ValvyEditorWindow window = GetWindow<ValvyEditorWindow>("valvy.");
        window.minSize = new Vector2(700, 750);
    }

    private void OnGUI()
    {
        // Full Window Background
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), DarkBg);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(20);
        DrawNavBar();

        GUILayout.Space(30);
        DrawHeroSection();

        GUILayout.Space(30);
        DrawActionSection();

        GUILayout.Space(40);

        GUILayout.Space(40);
        EditorGUILayout.EndScrollView();
    }

    private void DrawNavBar()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);

        // Logo
        GUIStyle logoStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };
        GUILayout.Label("valvy.", logoStyle, GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        // Nav Links
        GUIStyle navLinkStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        if (GUILayout.Button("Features", navLinkStyle)) { }
        GUILayout.Space(15);
        if (GUILayout.Button("Why Valvy", navLinkStyle)) { }
        GUILayout.Space(15);
        if (GUILayout.Button("Docs", navLinkStyle)) { }
        GUILayout.Space(20);

        // Get Started Button
        if (GUILayout.Button("Get Started", GetMainButtonStyle(PrimaryBlue, Color.white), GUILayout.Width(110), GUILayout.Height(32)))
        {
            Application.OpenURL("https://proxodev.itch.io/valvy");
        }

        GUILayout.Space(30);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawHeroSection()
    {
        // Version Tag Pill
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle pillStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        Rect versionRect = GUILayoutUtility.GetRect(new GUIContent("Valvy v1.0 Released"), pillStyle, GUILayout.Width(150), GUILayout.Height(26));
        EditorGUI.DrawRect(versionRect, new Color(0.18f, 0.18f, 0.18f, 1f));
        GUI.Label(versionRect, "Valvy v1.0 Released", pillStyle);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15);

        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 42,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUILayout.LabelField("Valvy.", titleStyle, GUILayout.Height(50));

        GUILayout.Space(5);

        // Subtitle
        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        EditorGUILayout.LabelField("if you whant to use gorilla locamotion u need to use orbit locamotion or it will not work.", subtitleStyle);
    }

    private void DrawActionSection()
    {
        // Main Action Buttons
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Download Package", GetMainButtonStyle(PrimaryBlue, Color.white), GUILayout.Width(160), GUILayout.Height(38)))
        {
            Application.OpenURL("https://proxodev.itch.io/valvy");
        }

        GUILayout.Space(12);

        if (GUILayout.Button("Read Docs", GetMainButtonStyle(CardBg, Color.white), GUILayout.Width(120), GUILayout.Height(38)))
        {
            Application.OpenURL("https://proxodev.itch.io/valvy");
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        // URL / Copy Field
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        Rect copyBarRect = GUILayoutUtility.GetRect(420, 36);
        EditorGUI.DrawRect(copyBarRect, CardBg);

        GUIStyle urlStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        GUI.Label(new Rect(copyBarRect.x + 15, copyBarRect.y, 300, 36), "https://proxodev.itch.io/valvy", urlStyle);

        // Copy Button inside bar
        GUIStyle copyBtnStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = PrimaryBlue }
        };

        if (GUI.Button(new Rect(copyBarRect.x + 345, copyBarRect.y + 3, 65, 30), "Copy", copyBtnStyle))
        {
            EditorGUIUtility.systemCopyBuffer = "https://proxodev.itch.io/valvy";
            ShowNotification(new GUIContent("Link copied to clipboard!"));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // --- HELPER STYLES ---

    private GUIStyle GetMainButtonStyle(Color bgColor, Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = textColor }
        };
        style.normal.background = MakeTexture(2, 2, bgColor);
        return style;
    }

    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
#endif