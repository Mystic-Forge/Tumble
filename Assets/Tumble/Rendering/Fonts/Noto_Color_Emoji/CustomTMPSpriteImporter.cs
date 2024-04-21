#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using TMPro.SpriteAssetUtilities;

namespace TMPro
{
    public class CustomTMP_SpriteAssetImporter : EditorWindow
    {
        // Create Sprite Asset Editor Window
        [MenuItem("Window/TextMeshPro/Custom Sprite Importer", false, 2026)]
        public static void ShowFontAtlasCreatorWindow()
        {
            var window = GetWindow<CustomTMP_SpriteAssetImporter>();
            window.titleContent = new GUIContent("Sprite Importer");
            window.Focus();
        }

        Texture2D m_SpriteAtlas;
        SpriteAssetImportFormats m_SpriteDataFormat = SpriteAssetImportFormats.TexturePackerJsonArray;
        TextAsset m_JsonFile;

        string m_CreationFeedback;

        TMP_SpriteAsset m_SpriteAsset;

        /// <summary>
        ///
        /// </summary>
        void OnEnable()
        {
            // Set Editor Window Size
            SetEditorWindowSize();
        }

        /// <summary>
        ///
        /// </summary>
        public void OnGUI()
        {
            DrawEditorPanel();
        }

        /// <summary>
        ///
        /// </summary>
        private void OnDisable()
        {
            // Clean up sprite asset object that may have been created and not saved.
            if (m_SpriteAsset != null && !EditorUtility.IsPersistent(m_SpriteAsset))
                DestroyImmediate(m_SpriteAsset);
        }

        /// <summary>
        ///
        /// </summary>
        void DrawEditorPanel()
        {
            // label
            GUILayout.Label("Import Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // Sprite Texture Selection
            m_JsonFile = EditorGUILayout.ObjectField("Sprite Data Source", m_JsonFile, typeof(TextAsset), false) as TextAsset;

            m_SpriteDataFormat = (SpriteAssetImportFormats)EditorGUILayout.EnumPopup("Import Format", m_SpriteDataFormat);

            // Sprite Texture Selection
            m_SpriteAtlas = EditorGUILayout.ObjectField("Sprite Texture Atlas", m_SpriteAtlas, typeof(Texture2D), false) as Texture2D;

            if (EditorGUI.EndChangeCheck())
            {
                m_CreationFeedback = string.Empty;
            }

            GUILayout.Space(10);

            GUI.enabled = m_JsonFile != null && m_SpriteAtlas != null && m_SpriteDataFormat != SpriteAssetImportFormats.None;

            // Create Sprite Asset
            if (GUILayout.Button("Create Sprite Asset"))
            {
                m_CreationFeedback = string.Empty;

                // Clean up sprite asset object that may have been previously created.
                if (m_SpriteAsset != null && !EditorUtility.IsPersistent(m_SpriteAsset))
                    DestroyImmediate(m_SpriteAsset);

                // Read json data file
                if (m_JsonFile != null)
                {
                    switch (m_SpriteDataFormat)
                    {
                        case SpriteAssetImportFormats.TexturePackerJsonArray:
                            SpriteDataObject jsonData = null;
                            try
                            {
                                jsonData = JsonUtility.FromJson<SpriteDataObject>(m_JsonFile.text);
                            }
                            catch
                            {
                                m_CreationFeedback = "The Sprite Data Source file [" + m_JsonFile.name + "] appears to be invalid or incorrectly formatted.";
                            }

                            if (jsonData != null && jsonData.frames != null && jsonData.frames.Count > 0)
                            {
                                var spriteCount = jsonData.frames.Count;

                                // Update import results
                                m_CreationFeedback = "<b>Import Results</b>\n--------------------\n";
                                m_CreationFeedback += "<color=#C0ffff><b>" + spriteCount + "</b></color> Sprites were imported from file.";

                                // Create new Sprite Asset
                                m_SpriteAsset = CreateInstance<TMP_SpriteAsset>();

                                // Assign sprite sheet / atlas texture to sprite asset
                                m_SpriteAsset.spriteSheet = m_SpriteAtlas;

                                var spriteGlyphTable = new List<TMP_SpriteGlyph>();
                                var spriteCharacterTable = new List<TMP_SpriteCharacter>();

                                PopulateSpriteTables(jsonData, spriteCharacterTable, spriteGlyphTable);

                                m_SpriteAsset.spriteCharacterTable.AddRange(spriteCharacterTable);
                                m_SpriteAsset.spriteGlyphTable.AddRange(spriteGlyphTable);
                            }
                            break;
                    }
                }
            }

            GUI.enabled = true;

            // Creation Feedback
            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(60));
            {
                EditorGUILayout.TextArea(m_CreationFeedback, TMP_UIStyleManager.label);
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUI.enabled = m_JsonFile != null && m_SpriteAtlas && m_SpriteAsset != null;
            if (GUILayout.Button("Save Sprite Asset") && m_JsonFile != null)
            {
                var filePath = EditorUtility.SaveFilePanel("Save Sprite Asset File", new FileInfo(AssetDatabase.GetAssetPath(m_JsonFile)).DirectoryName, m_JsonFile.name, "asset");

                if (filePath.Length == 0)
                    return;

                SaveSpriteAsset(filePath);
            }
            GUI.enabled = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="spriteDataObject"></param>
        /// <param name="spriteCharacterTable"></param>
        /// <param name="spriteGlyphTable"></param>
        private static void PopulateSpriteTables(SpriteDataObject spriteDataObject, List<TMP_SpriteCharacter> spriteCharacterTable, List<TMP_SpriteGlyph> spriteGlyphTable)
        {
            var importedSprites = spriteDataObject.frames;

            var atlasHeight = spriteDataObject.meta.size.h;

            for (var i = 0; i < importedSprites.Count; i++)
            {
                var spriteData = importedSprites[i];

                var spriteGlyph = new TMP_SpriteGlyph();
                spriteGlyph.index = (uint)i;

                spriteGlyph.metrics   = new GlyphMetrics((int)spriteData.frame.w, (int)spriteData.frame.h, 0, spriteData.frame.h, (int)spriteData.frame.w);
                spriteGlyph.glyphRect = new GlyphRect((int)spriteData.frame.x, (int)(atlasHeight - spriteData.frame.h - spriteData.frame.y), (int)spriteData.frame.w, (int)spriteData.frame.h);
                spriteGlyph.scale     = .6f;

                spriteGlyphTable.Add(spriteGlyph);

                var spriteCharacter = new TMP_SpriteCharacter(0, spriteGlyph);
                spriteCharacter.name    = spriteData.filename.Split('.')[0];
                spriteCharacter.unicode = 0xE000 + spriteData.index;
                spriteCharacter.scale   = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="filePath"></param>
        void SaveSpriteAsset(string filePath)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            var dataPath = Application.dataPath;

            if (filePath.IndexOf(dataPath, System.StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                Debug.LogError("You're saving the font asset in a directory outside of this project folder. This is not supported. Please select a directory under \"" + dataPath + "\"");
                return;
            }

            var relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            var dirName = Path.GetDirectoryName(relativeAssetPath);
            var fileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            var pathNoExt = dirName + "/" + fileName;

            // Save Sprite Asset
            AssetDatabase.CreateAsset(m_SpriteAsset, pathNoExt + ".asset");

            // Set version number
            // m_SpriteAsset.version = "1.1.0";
            typeof(TMP_SpriteAsset).GetField("m_Version", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(m_SpriteAsset, "1.1.0");

            // Compute the hash code for the sprite asset.
            m_SpriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(m_SpriteAsset.name);

            // Add new default material for sprite asset.
            AddDefaultMaterial(m_SpriteAsset);
        }

        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        static void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
        {
            var shader = Shader.Find("TextMeshPro/Sprite");
            var material = new Material(shader);
            material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }

        /// <summary>
        /// Limits the minimum size of the editor window.
        /// </summary>
        void SetEditorWindowSize()
        {
            EditorWindow editorWindow = this;

            var currentWindowSize = editorWindow.minSize;

            editorWindow.minSize = new Vector2(Mathf.Max(230, currentWindowSize.x), Mathf.Max(300, currentWindowSize.y));
        }
    }
}

[System.Serializable]
public class SpriteDataObject
{
    public List<Frame> frames;
    public Meta        meta;
}

[System.Serializable]
public struct Frame
{
    public string      filename;
    public uint        index;
    public SpriteFrame frame;
    public bool        rotated;
    public bool        trimmed;
    public SpriteFrame spriteSourceSize;
    public SpriteSize  sourceSize;
    public Vector2     pivot;
}

[System.Serializable]
public struct Meta
{
    public string     app;
    public string     version;
    public string     image;
    public string     format;
    public SpriteSize size;
    public float      scale;
    public string     smartupdate;
}

[System.Serializable]
public struct SpriteFrame
{
    public float x;
    public float y;
    public float w;
    public float h;

    public override string ToString()
    {
        string s = "x: " + x.ToString("f2") + " y: " + y.ToString("f2") + " h: " + h.ToString("f2") + " w: " + w.ToString("f2");
        return s;
    }
}

[System.Serializable]
public struct SpriteSize
{
    public float w;
    public float h;

    public override string ToString()
    {
        string s = "w: " + w.ToString("f2") + " h: " + h.ToString("f2");
        return s;
    }
}
#endif