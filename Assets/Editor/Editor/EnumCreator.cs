#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Text;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;

internal abstract class EnumCreator : OdinEditorWindow
{
    [SerializeField, FolderPath(AbsolutePath = true)] public string savePath;
    [SerializeField] public string nameSpace;

    protected abstract string MenuPath { get; }
    protected abstract string EnumName { get; }

    [Button("Generate")]
    private void ButtonCreate()
    {
        Create(savePath, nameSpace, EnumName, GetValues(), MenuPath);
        Close();
    }

    protected abstract IList<KeyValuePair<string, int?>> GetValues();

    public static void Create(string savePath, string nameSpace, string enumName, IList<KeyValuePair<string, int?>> values, string menuPath = "")
    {
        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(menuPath))
        {
            sb.Append($"// Code generated. Use menu item \"{menuPath}\" to update\n\n");
        }

        if (nameSpace != null)
        {
            sb.Append("namespace " + nameSpace + "\n{\n");
        }

        sb.Append("public enum " + enumName + "\n{\n");

        foreach (var kv in values)
        {
            string name = kv.Key;

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            name = name.Replace(' ', '_');

            sb.Append(name);
            if (kv.Value.HasValue)
            {
                sb.Append(" = " + kv.Value.Value);
            }
            sb.Append(",\n");
        }

        sb.Append("}\n");

        if (nameSpace != null)
        {
            sb.Append("}");
        }

        string filePath = savePath + "/" + enumName + ".cs";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using (FileStream stream = File.Create(filePath))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(bytes, 0, bytes.Length);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();

        Debug.Log("Generated new enum at " + filePath);
    }
}
#endif
