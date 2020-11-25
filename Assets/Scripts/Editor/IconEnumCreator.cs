using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal class IconEnumCreator : EnumCreator
{
    private const string menuPath = "Callum/" + nameof(IconEnumCreator);
    protected override string MenuPath => menuPath;

    protected override string EnumName => "IconType";

    [MenuItem(menuPath)]
    private static void CreateWindow()
    {
        CreateWindow<IconEnumCreator>();
    }

    protected override IList<KeyValuePair<string, int?>> GetValues()
    {
        var ids  = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures" });
        var paths = ids.Select(id => AssetDatabase.GUIDToAssetPath(id));
        var kvs = paths
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Where(fn => fn.StartsWith("icon"))
            .Select(fn => fn.Replace("icon_", ""))
            .Select(fn => char.ToUpper(fn[0]) + fn.Substring(1, fn.Length - 1))
            .Select(fn => new KeyValuePair<string, int?>(fn, null))
            .ToList();

        kvs.Insert(0, new KeyValuePair<string, int?>("None", null));

        return kvs;
    }
}
