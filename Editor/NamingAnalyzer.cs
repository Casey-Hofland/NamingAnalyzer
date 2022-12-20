#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityExtras.Naming.Editor
{
    [Serializable]
    public struct NamingAnalyzer
    {
        public NamingRuleset ruleset;
        public string directory;

        public NamingAnalyzer(NamingRuleset ruleset, string directory)
        {
            this.ruleset = ruleset;
            this.directory = directory;
        }

        public static NamingAnalyzer CreateEmpty(string directory) => new(NamingRuleset.emptyRuleset, directory);

        public static void AnalyzeProject()
        {
            var namingAnalyzers = NamingAnalyzerSettings.namingAnalyzers.value;
            var excludeFromNamingAnalyzers = NamingAnalyzerSettings.excludeFromNamingAnalyzers.value;
            var excludePaths = excludeFromNamingAnalyzers.Select(excludeFromNamingAnalyzer => AssetDatabase.GetAssetPath(excludeFromNamingAnalyzer)).ToHashSet();

            namingAnalyzers = namingAnalyzers.OrderByDescending(namingAnalyzer => namingAnalyzer.directory).ToList();
            for (int i = 0; i < namingAnalyzers.Count; i++)
            {
                var namingAnalyzer = namingAnalyzers[i];
                var namingRegexByAssetType = namingAnalyzer.ruleset.namingRules.ToDictionary(namingRule => namingRule.assetType, namingRule => new Regex(namingRule.pattern));

                var path = Application.dataPath + (string.IsNullOrWhiteSpace(namingAnalyzer.directory) ? string.Empty : $"/{namingAnalyzer.directory}");

                EnumerateDirectoriesRecursive(path);

                void EnumerateDirectoriesRecursive(string path)
                {
                    try
                    {
                        foreach (var directory in Directory.EnumerateDirectories(path))
                        {
                            // If the directory is being analyzed by ANOTHER ruleset, skip analyzing this directory by THIS ruleset.
                            if (namingAnalyzers.FindIndex(0, i, namingAnalyzer => namingAnalyzer.directory == directory.Remove(0, Application.dataPath.Length + 1).Replace("\\", "/")) != -1)
                            {
                                break;
                            }

                            EnumerateDirectoriesRecursive(directory);

                            foreach (var filePath in Directory.EnumerateFiles(directory))
                            {
                                if (filePath.EndsWith(".meta"))
                                {
                                    continue;
                                }

                                var assetPath = filePath.Remove(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
                                if (excludePaths.Remove(assetPath))
                                {
                                    continue;
                                }

                                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                                string? assetName = null;
                                UnityEngine.Object? asset = null;
                                while (assetType != null)
                                {
                                    if (namingRegexByAssetType.TryGetValue(assetType, out var namingRegex))
                                    {
                                        assetName ??= assetPath[(assetPath.LastIndexOf('/') + 1)..assetPath.LastIndexOf('.')];
                                        if (!namingRegex.IsMatch(assetName))
                                        {
                                            if (asset == null)
                                            {
                                                asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                                            }
                                            Debug.LogError($"{asset} conflicts with the {assetType.FullName} rule. Go under Edit > Project Settings > Naming Analyzer to check this rules pattern.", asset);
                                        }
                                    }

                                    assetType = assetType.BaseType;
                                }
                            }
                        }
                    }
                    catch (DirectoryNotFoundException) { }
                }
            }
        }
    }
}
