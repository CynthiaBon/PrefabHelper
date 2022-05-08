using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class MaterialHelper : EditorWindow
{
    private string _texturesPath = "Textures";
    private string _materialsPath = "Materials";

    private List<MapName> _mapNames = null;
    private List<MaterialData> _materialDataList = null;

    private Vector2 _generalScrollPosition = Vector2.zero;
    private Vector2 _mapScrollPosition = Vector2.zero;

    private string search = "";

    #region Initialize

    private void OnEnable()
    {
        _mapNames = new List<MapName>()
        {
            new MapName("D", "_BaseColorMap", "Base map"),
            new MapName("M", "_MaskMap", "Mask map"),
            new MapName("NRM", "_NormalMap", "Normal map"),
            new MapName("E", "_EmissiveColorMap", "Emissive map")
        };

        GetMaterialDatas(Application.dataPath + "\\" + _texturesPath, "", "");
    }

    [MenuItem("Tools/MaterialHelper")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(MaterialHelper));
    }

    #endregion Initialize

    #region Display

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        _generalScrollPosition = GUILayout.BeginScrollView(_generalScrollPosition, false, true, GUILayout.ExpandHeight(true));

        GUILayout.Label("");
        TextureDisplay();
        DrawLine();
        MaterialDisplay();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void TextureDisplay()
    {
        _texturesPath = EditorGUILayout.TextField("Textures directory", _texturesPath);
        if (GUILayout.Button("Rename all textures"))
            RenameAllTextures(Application.dataPath + "\\" + _texturesPath);
    }

    private void MaterialDisplay()
    {
        _materialsPath = EditorGUILayout.TextField("Materials directoty", _materialsPath);
        DisplayMapNames();
        if (GUILayout.Button("Refresh materials list"))
            GetMaterialDatas(Application.dataPath + "\\" + _texturesPath, "", "");
        if (GUILayout.Button("Create materials"))
        {
            CreateAllMaterials();
            GetMaterialDatas(Application.dataPath + "\\" + _texturesPath, "", "");
        }
        if (_materialDataList != null)
            DisplayMaterialDatas();
    }

    #endregion Display

    #region Textures

    void RenameAllTextures(string directoryPath)
    {
        string[] filesNames = Directory.GetFiles(directoryPath);

        for (int i = 0; i < filesNames.Length; i++)
        {
            filesNames[i] = filesNames[i].Replace('/', '\\');
            if (filesNames[i].Split('.').Last() != "meta")
            {
                string[] splitPath = filesNames[i].Split('\\');
                string fileName = splitPath[splitPath.Length - 1];
                string[] splitFileName = fileName.Split('_');
                if (splitFileName.Length > 2 && splitFileName[2] == "DefaultMaterial")
                {
                    fileName = "T_" + splitFileName[1] + "_" + splitFileName[splitFileName.Length - 1];
                    if (splitFileName[splitFileName.Length - 1] == "Normal.png")
                        SetNormalMap(filesNames[i]);
                }
                File.Move(filesNames[i], directoryPath + "\\" + fileName);
                File.Move(filesNames[i] + ".meta", directoryPath + "\\" + fileName + ".meta");
            }
        }


        string[] subDirectoriesNames = Directory.GetDirectories(directoryPath);

        for (int y = 0; y < subDirectoriesNames.Length; y++)
        {
            RenameAllTextures(subDirectoriesNames[y]);
        }

        AssetDatabase.Refresh();
    }

    private void SetNormalMap(string path)
    {
        path = GetPathFromAssets(path);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.NormalMap;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private string GetPathFromAssets(string path)
    {
        string pathFromAssets = "";
        string[] splitPath = path.Split('\\');
        bool startConcat = false;

        for (int i = 0; i < splitPath.Length; i++)
        {
            if (startConcat || splitPath[i] == "Assets")
            {
                startConcat = true;
                pathFromAssets += splitPath[i];
                if (i != splitPath.Length - 1)
                    pathFromAssets += '/';
            }
        }
        return pathFromAssets;
    }

    #endregion Textures

    #region MapData

    private void DisplayMapNames()
    {
        string display = "Maps used :";

        for (int i = 0; i < _mapNames.Count; i++)
        {
            _mapNames[i].mapName = EditorGUILayout.TextField($"{_mapNames[i].displayName}: ", _mapNames[i].mapName);
            display += $"\n- T_Name_{_mapNames[i].mapName}.png";
        }

        GUILayout.Label(display);
    }

    private void GetMaterialDatas(string directoryPath, string materialsDirectory, string shader)
    {
        List<string> fileNames = GetAllFilesRecursivly(directoryPath, new List<string>());
        string baseMapName = _mapNames[0].mapName;
        List<string> baseMap = fileNames.FindAll(fileName => fileName.Split('_')[2].Split('.')[0] == baseMapName);
        CreateMaterialDataList(baseMap, fileNames);
    }

    private void DisplayMaterialDatas()
    {
        search = EditorGUILayout.TextField("Search", search);
        GUILayout.BeginVertical();
        _mapScrollPosition = GUILayout.BeginScrollView(_mapScrollPosition, false, true, GUILayout.ExpandHeight(true));
        _materialDataList.ForEach(materialData =>
        {
            string mapDisplay = GetMapDisplay(materialData);
            Regex regex = new Regex(search, RegexOptions.IgnoreCase);
            if (search == "" || regex.IsMatch(mapDisplay))
            {
                GUILayout.Label(mapDisplay);
                GUILayout.BeginHorizontal();
                materialData.IsUnlit = GUILayout.Toggle(materialData.IsUnlit, "Unlit ?");
                materialData.IsTransparent = GUILayout.Toggle(materialData.IsTransparent, "Transparent ?");
                GUILayout.EndHorizontal();
            }
        });
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private string GetMapDisplay(MaterialData materialData)
    {
        string display = materialData.Name + " (";
        for (int i = 0; i < materialData.Maps.Count; i++)
        {
            display += materialData.Maps[i].Split('_')[2].Split('.')[0];
            if (i != materialData.Maps.Count - 1)
                display += ", ";
            else
                display += ")";
        }
        return display;
    }

    private List<string> GetAllFilesRecursivly(string directoryPath, List<string> files)
    {
        string[] filesNames = null;
        try
        {
            filesNames = Directory.GetFiles(directoryPath);
            for (int i = 0; i < filesNames.Length; i++)
            {
                filesNames[i] = filesNames[i].Replace('/', '\\');
                string name = filesNames[i].Split('\\').Last();
                if (name.Split('_').First() == "T" && name.Split('.').Last() != "meta")
                    files.Add(filesNames[i]);
            }

            string[] subDirectoriesNames = Directory.GetDirectories(directoryPath);

            for (int y = 0; y < subDirectoriesNames.Length; y++)
            {
                files = GetAllFilesRecursivly(subDirectoriesNames[y], files);
            }
        }
        catch
        {
            Debug.LogWarning("Textures directory not found");
        }

        return files;
    }

    private void CreateMaterialDataList(List<string> baseMaps, List<string> fileNames)
    {
        _materialDataList = new List<MaterialData>();

        baseMaps.ForEach(baseMap =>
        {
            string[] splitPath = baseMap.Split('\\');
            List<string> path = splitPath.ToList();
            path.RemoveAt(splitPath.Length - 1);

            string name = splitPath.Last().Split('_')[1];

            string filePath = "";
            path.ForEach(part => filePath += (part + '\\'));

            List<string> maps = FillMaterialData(name, fileNames);


            string directoryPath = GetMaterialDirectoryPath(filePath, maps.Count);
            if (!File.Exists(directoryPath + "M_" + name + ".mat"))
            {
                MaterialData data = new MaterialData(name, filePath, maps);
                _materialDataList.Add(data);
            }
        });
    }

    private List<string> FillMaterialData(string materialName, List<string> fileNames)
    {
        List<string> maps = fileNames.FindAll(fileName => fileName.Split('_')[1] == materialName);
        for (int i = 0; i < maps.Count; i++)
        {
            maps[i] = maps[i].Split('\\').Last();
        }
        return maps;
    }

    #endregion MapData

    #region MaterialCreation

    private void CreateAllMaterials()
    {
        foreach (MaterialData materialData in _materialDataList)
        {
            string directoryPath = GetMaterialDirectoryPath(materialData.Path, materialData.Maps.Count);
            if (File.Exists(directoryPath + "M_" + materialData.Name + ".mat"))
                continue;

            if (!Directory.Exists(directoryPath))
                CreateDirectory(directoryPath);

            string pathFromAssetsTextures = GetPathFromAssets(materialData.Path);
            string pathFromAssetsMaterials = GetPathFromAssets(directoryPath);
            Material newMaterial = null;
            if (materialData.IsUnlit)
                newMaterial = new Material(Shader.Find("HDRP/Unlit"));
            else if (materialData.IsTransparent)
                newMaterial = new Material(AssetDatabase.LoadAssetAtPath("Assets/Resources/TransparentShader.mat", typeof(Material)) as Material);
            else
                newMaterial = new Material(Shader.Find("HDRP/Lit"));
            newMaterial.name = "M_" + materialData.Name;
            AssetDatabase.CreateAsset(newMaterial, pathFromAssetsMaterials + "M_" + materialData.Name + ".mat");
            Material material = AssetDatabase.LoadAssetAtPath(pathFromAssetsMaterials + "M_" + materialData.Name + ".mat", typeof(Material)) as Material;
            _mapNames.ForEach(mapName =>
            {
                AssignTexture(material, pathFromAssetsTextures, materialData, mapName.mapName, mapName.unityName);
            });
        }
    }

    private string GetMaterialDirectoryPath(string texturePath, int mapsCount)
    {
        string directoryPath = texturePath.Replace("Textures", "Materials");
        if (Directory.GetFiles(texturePath).Length / 2 == mapsCount)
        {
            List<string> splitPath = directoryPath.Split('\\').ToList();
            splitPath.RemoveAt(splitPath.Count - 1);
            splitPath.RemoveAt(splitPath.Count - 1);
            string subdirectoryPath = "";
            splitPath.ForEach(part => subdirectoryPath += (part + '\\'));
            directoryPath = subdirectoryPath;
        }

        return directoryPath;
    }

    private void AssignTexture(Material material, string pathFromAssetsTextures, MaterialData materialData, string mapName, string textureName)
    {
        string map = materialData.Maps.Find(name => name.Split('_')[2] == mapName + ".png");
        if (map != null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath(pathFromAssetsTextures + map, typeof(Texture2D)) as Texture2D;
            material.SetTexture(textureName, texture);
        }
    }

    private void CreateDirectory(string path)
    {
        List<string> splitPath = path.Split('\\').ToList();
        splitPath.RemoveAt(splitPath.Count - 1);
        splitPath.RemoveAt(splitPath.Count - 1);
        string subdirectoryPath = "";
        splitPath.ForEach(part => subdirectoryPath += (part + '\\'));
        if (!Directory.Exists(subdirectoryPath))
            CreateDirectory(subdirectoryPath);
        Directory.CreateDirectory(path);
    }

    #endregion MaterialCreation

    private void DrawLine()
    {
        GUILayout.Label("");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("");
    }
}
