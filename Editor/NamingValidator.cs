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
                    if (!Directory.Exists(path))
                    {
                        return;
                    }

                    var folderPath = path.Remove(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
                    if (!ValidateType(typeof(DefaultAsset), folderPath))
                    {
                        var folder = AssetDatabase.LoadMainAssetAtPath(folderPath);
                        Debug.LogError($"{folder} has incorrect naming. Check ruleset \"{namingValidator.ruleset.name}\" for allowed patterns.", folder);
                    }

                    foreach (var directory in Directory.EnumerateDirectories(path))
                    {
                        // If the directory is being analyzed by ANOTHER ruleset, skip validating this directory by THIS ruleset.
                        if (namingValidators.FindIndex(0, i, namingValidator => namingValidator.directory == directory.Remove(0, Application.dataPath.Length + 1).Replace("\\", "/")) != -1)
                        {
                            break;
                        }

                        EnumerateDirectoriesRecursive(directory);
                    }

                    foreach (var filePath in Directory.EnumerateFiles(path))
                    {
                        if (filePath.EndsWith(".meta") || filePath.EndsWith(".prefab"))
                        {
                            continue;
                        }

                        var assetPath = filePath.Remove(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
                        if (excludePaths.Remove(assetPath))
                        {
                            continue;
                        }

                        if (!ValidateType(AssetDatabase.GetMainAssetTypeAtPath(assetPath), assetPath))
                        {
                            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            Debug.LogError($"{asset} has incorrect naming. Check ruleset \"{namingValidator.ruleset.name}\" for allowed patterns.", asset);
                        }
                    }

                    bool ValidateType(Type assetType, string assetPath)
                    {
                        while (assetType != null)
                        {
                            if (namingRegexByAssetType.TryGetValue(assetType, out var namingRegex))
                            {
                                var lastIndexOfDot = assetPath.LastIndexOf('.');
                                var assetName = assetPath[(assetPath.LastIndexOf('/') + 1)..(lastIndexOfDot == -1 ? assetPath.Length : lastIndexOfDot)];
                                if (!namingRegex.IsMatch(assetName))
                                {
                                    return false;
                                }
                            }

                            assetType = assetType.BaseType;
                        }

                        return true;
                    }
                }
            }
        }
    }
}
