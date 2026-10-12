using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using System.Linq;
using UnityEditor;

internal class AutoSetScriptNamespacesToFolderName : OdinEditorWindow
{
    [ShowInInspector, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
    private string rootDirectory = @"C:\Users\callu\Documents\GitHub\Lawnmower\Assets\Scripts";

    [ShowInInspector]
    private string[] ignoreFolders = new string[] { "Editor" };

    [MenuItem("Callum/" + nameof(AutoSetScriptNamespacesToFolderName))]
    public static void Create()
    {
        GetWindow<AutoSetScriptNamespacesToFolderName>();
    }

    [Button]
    private void ProcessAllScriptNamespacesInDirectory()
    {
        string[] allScriptPaths = Directory
            .EnumerateFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(p => !ignoreFolders.Any(f => Path.GetDirectoryName(p).Contains(f)))
            .ToArray();

        ScriptProcessor.ProcessScriptNamespaces(allScriptPaths, rootDirectory);
    }

}
