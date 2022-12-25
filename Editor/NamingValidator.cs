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
    public struct NamingValidator
    {
        public NamingRuleset ruleset;
        public string directory;

        public NamingValidator(NamingRuleset ruleset, string directory)
        {
            this.ruleset = ruleset;
            this.directory = directory;
        }

        public static NamingValidator CreateEmpty(string directory) => new(NamingRuleset.emptyRuleset, directory);

        public static void ValidateProject()
        {
            var namingValidators = NamingValidatorSettings.namingValidators.value;
            var excludeFromNamingValidators = NamingValidatorSettings.excludeFromNamingValidators.value;
            var excludePaths = excludeFromNamingValidators.Select(excludeFromNamingValidator => AssetDatabase.GetAssetPath(excludeFromNamingValidator)).ToHashSet();

            namingValidators = namingValidators.OrderByDescending(namingValidator => namingValidator.directory).ToList();
            for (int i = 0; i < namingValidators.Count; i++)
            {
                var namingValidator = namingValidators[i];
                var namingRegexByAssetType = namingValidator.ruleset.namingRules.ToDictionary(namingRule => namingRule.assetType, namingRule => new Regex(namingRule.pattern));

                var path = Application.dataPath + (string.IsNullOrWhiteSpace(namingValidator.directory) ? string.Empty : $"/{namingValidator.directory}");

                EnumerateDirectoriesRecursive(path);

                void EnumerateDirectoriesRecursive(string path)
                {
                    try
                    {
                        foreach (var directory in Directory.EnumerateDirectories(path))
                        {
                            // If the directory is being analyzed by ANOTHER ruleset, skip validating this directory by THIS ruleset.
                            if (namingValidators.FindIndex(0, i, namingValidator => namingValidator.directory == directory.Remove(0, Application.dataPath.Length + 1).Replace("\\", "/")) != -1)
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
                                            Debug.LogError($"Incorrect Naming Detected on {asset}! Please apply the right naming rules when naming your assets. Conflicts with rule ({assetType.FullName})", asset);
                                            break;
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
