using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal class ScriptProcessor : UnityEditor.AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        return paths;
    }

    static void OnWillCreateAsset(string assetName)
    {
        if (Path.GetExtension(assetName) != "cs")
        {
            return;
        }

        var test = AssetDatabase.LoadAssetAtPath<TextAsset>(assetName);

        ProcessScriptNamespace(assetName, @"C:\Users\callu\Documents\GitHub\Lawnmower\Assets\Scripts");
    }

    public static void ProcessScriptNamespaces(string[] paths, string pathSectionToIgnore)
    {
        foreach (var path in paths)
        {
            ProcessScriptNamespace(path, pathSectionToIgnore);
        }

        AssetDatabase.Refresh();
    }

    private static void ProcessScriptNamespace(string path, string pathSectionToIgnore)
    {
        pathSectionToIgnore = Path.Combine(Path.GetDirectoryName(pathSectionToIgnore), Path.GetFileName(pathSectionToIgnore));

        const string ns = "namespace";

        List<string> lines = File.ReadAllLines(path).ToList();

        int namespaceLineNumber = lines.FindIndex(l => l.Contains(ns));

        if (namespaceLineNumber < 0 || namespaceLineNumber >= lines.Count)
        {
            Debug.LogWarning($"No namespace in file {path}");
            return;
        }

        string namespaceLine = lines[namespaceLineNumber];

        if (string.IsNullOrWhiteSpace(namespaceLine))
        {
            Debug.LogWarning($"Skipped {path} as it has no namespace");
            return;
        }

        bool doesContainBrace = namespaceLine.Contains('{');
        if (doesContainBrace)
        {
            Debug.LogWarning($"Skipped {path} as it has a brace on the same line");
            return;
        }

        string relativeDirectory = path.Replace(pathSectionToIgnore, "");
        string[] parts = relativeDirectory.Split(new[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar, Path.PathSeparator, Path.VolumeSeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
        string newNamespaceName = string.Join(".", parts.Take(parts.Length - 1));

        if (!Regex.IsMatch(newNamespaceName, @"^[a-zA-Z.]+$"))
        {
            Debug.LogWarning($"Skipped {path} as name {newNamespaceName} is invalid");
            return;
        }

        string newNamespaceLine = namespaceLine.Remove(namespaceLine.IndexOf(ns) + ns.Length) + " " + newNamespaceName;

        if (newNamespaceLine == namespaceLine)
        {
            // no change
            return;
        }

        lines[namespaceLineNumber] = newNamespaceLine;

        File.WriteAllLines(path, lines);

        Debug.Log($"Changed {path} from {namespaceLine} to {newNamespaceLine}");
    }
}
