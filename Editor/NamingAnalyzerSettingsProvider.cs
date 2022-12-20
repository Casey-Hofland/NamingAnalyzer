#nullable enable
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityExtras.Naming.Editor
{
    public static class NamingAnalyzerSettingsProvider
    {
        private const string _settingsProviderPath = "Project/Naming Analyzer";

        private static readonly ReorderableList namingAnalyzers = new(NamingAnalyzerSettings.namingAnalyzers.value ??= new(), typeof(NamingAnalyzer))
        {
            drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Naming Analyzers"),
            drawElementCallback = DrawNamingAnalyzer,
        };

        private static readonly ReorderableList excludeFromNamingAnalyzers = new(NamingAnalyzerSettings.excludeFromNamingAnalyzers.value ??= new(), typeof(Object))
        {
            drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Exclude from Naming Analyzers"),
            drawElementCallback = DrawExcludeFromNamingAnalyzer,
        };

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(_settingsProviderPath, SettingsScope.Project)
            {
                guiHandler = OnGUI,
            };
        }

        private static void OnGUI(string searchContext)
        {
            using var changeCheck = new EditorGUI.ChangeCheckScope();

            if (EditorGUILayout.LinkButton("Try out regex patterns!"))
            {
                Application.OpenURL("https://regexr.com/");
            }
            EditorGUILayout.Space();

            namingAnalyzers.DoLayoutList();
            excludeFromNamingAnalyzers.DoLayoutList();

            if (changeCheck.changed)
            {
                NamingAnalyzerSettings.namingAnalyzers.SetValue(namingAnalyzers.list);
                NamingAnalyzerSettings.excludeFromNamingAnalyzers.SetValue(excludeFromNamingAnalyzers.list);
                NamingAnalyzerSettings.instance.Save();
            }
        }

        private static void DrawNamingAnalyzer(Rect rect, int index, bool isActive, bool isFocused)
        {
            const float buttonWidth = 36f;

            var namingAnalyzer = NamingAnalyzerSettings.namingAnalyzers.value[index];
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 64f;

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2f;

            var fullWidth = rect.width;
            rect.width = fullWidth * 0.4f;

            var ruleset = EditorGUI.ObjectField(rect, "Ruleset", namingAnalyzer.ruleset, typeof(NamingRuleset), false) as NamingRuleset;
            namingAnalyzer.ruleset = ruleset != null ? ruleset : NamingRuleset.emptyRuleset;

            // Draw the directory field and pass validation checks.
            {
                rect.x += (rect.width = fullWidth * 0.5f);
                rect.width -= (buttonWidth + 2f);

                namingAnalyzer.directory = EditorGUI.DelayedTextField(rect, "Directory", namingAnalyzer.directory) ?? string.Empty;
            }

            // Draw the choose directory button and pass validation checks.
            {
                rect.x += rect.width + 2f;
                rect.width = buttonWidth;
                if (GUI.Button(rect, EditorGUIUtility.IconContent("Folder Icon")))
                {
                    var folder = Application.dataPath + (AssetDatabase.IsValidFolder($"Assets/{namingAnalyzer.directory}") ? $"/{namingAnalyzer.directory}" : default);
                    folder = EditorUtility.OpenFolderPanel($"Choose the Naming Analyzer's Directory", folder, default);
                    if (!string.IsNullOrEmpty(folder))
                    {
                        if (!folder.StartsWith(Application.dataPath + "/"))
                        {
                            throw new InvalidOperationException($"Trying to select a Naming Analyzer Directory outside of the current Unity Project. This is not allowed.");
                        }

                        namingAnalyzer.directory = folder.Remove(0, Application.dataPath.Length + 1);
                    }
                }
            }

            // Make sure to use forward slashes for directory representation.
            namingAnalyzer.directory = namingAnalyzer.directory.Replace('\\', '/');

            EditorGUIUtility.labelWidth = labelWidth;
            NamingAnalyzerSettings.namingAnalyzers.value[index] = namingAnalyzer;
        }

        private static void DrawExcludeFromNamingAnalyzer(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2f;

            var label = NamingAnalyzerSettings.excludeFromNamingAnalyzers.value[index] ? NamingAnalyzerSettings.excludeFromNamingAnalyzers.value[index].name : $"Element {index}";
            NamingAnalyzerSettings.excludeFromNamingAnalyzers.value[index] =  EditorGUI.ObjectField(rect, label, NamingAnalyzerSettings.excludeFromNamingAnalyzers.value[index], typeof(Object), false);
        }
    }
}
