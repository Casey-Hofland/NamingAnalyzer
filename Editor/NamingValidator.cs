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
                    var directoryInfo = new DirectoryInfo(path);
                    if (!ValidateInfo(directoryInfo))
                    {
                        return;
                    }

                    var folderPath = path.Remove(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
                    if (!ValidateType(typeof(DefaultAsset), directoryInfo.Name))
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
                        var fileInfo = new FileInfo(filePath);
                        if (!ValidateInfo(fileInfo))
                        {
                            continue;
                        }
                        if (fileInfo.Extension == ".meta"
                            || fileInfo.Extension == ".prefab")
                        {
                            continue;
                        }

                        var assetPath = filePath.Remove(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
                        if (excludePaths.Remove(assetPath))
                        {
                            continue;
                        }

                        if (!ValidateType(AssetDatabase.GetMainAssetTypeAtPath(assetPath), fileInfo.Name[..^fileInfo.Extension.Length]))
                        {
                            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            Debug.LogError($"{asset} has incorrect naming. Check ruleset \"{namingValidator.ruleset.name}\" for allowed patterns.", asset);
                        }
                    }

                    bool ValidateInfo(FileSystemInfo info)
                    {
                        if (!info.Exists)
                        {
                            return false;
                        }

                        var shortName = info.Name.AsSpan()[..^info.Extension.Length];
                        if (shortName.StartsWith(".")
                            || shortName.EndsWith("~")
                            || shortName == "cvs"
                            || info.Extension == ".tmp"
                            || info.Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            return false;
                        }

                        return true;
                    }

                    bool ValidateType(Type assetType, string assetName)
                    {
                        while (assetType != null)
                        {
                            if (namingRegexByAssetType.TryGetValue(assetType, out var namingRegex))
                            {
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
