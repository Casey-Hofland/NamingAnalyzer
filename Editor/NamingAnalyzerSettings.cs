#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace UnityExtras.Naming.Editor
{
    public static class NamingAnalyzerSettings
    {
        private const string k_PackageName = "com.unityextras.naminganalyzer";

        private static Settings? _instance;
        internal static Settings instance => _instance ??= new Settings(k_PackageName);

        public static UserSetting<List<NamingAnalyzer>> namingAnalyzers = new(instance, nameof(namingAnalyzers), new());
        public static UserSetting<List<Object>> excludeFromNamingAnalyzers = new(instance, nameof(excludeFromNamingAnalyzers), new());

        [MenuItem("Assets/Obey Naming Analyzer")]
        private static void IncludeInNamingAnalyzer()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj.GetType() == typeof(DefaultAsset))
                {
                    var directory = AssetDatabase.GetAssetPath(obj).Remove(0, "Assets/".Length);
                    namingAnalyzers.value.RemoveAll(namingAnalyzer => namingAnalyzer.directory == directory);
                }
                else
                {
                    while (excludeFromNamingAnalyzers.value.Remove(obj)) { }
                }
            }

            excludeFromNamingAnalyzers.settings.Save();
        }

        [MenuItem("Assets/Ignore Naming Analyzer")]
        private static void ExcludeFromNamingAnalyzer()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj.GetType() == typeof(DefaultAsset)) 
                {
                    var directory = AssetDatabase.GetAssetPath(obj).Remove(0, "Assets/".Length);
                    namingAnalyzers.value.RemoveAll(namingAnalyzer => namingAnalyzer.directory == directory);
                    namingAnalyzers.value.Add(NamingAnalyzer.CreateEmpty(directory));
                }
                else
                {
                    while (excludeFromNamingAnalyzers.value.Remove(obj)) { }
                    excludeFromNamingAnalyzers.value.Add(obj);
                }
            }

            excludeFromNamingAnalyzers.settings.Save();
        }
    }
}
