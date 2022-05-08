using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialData
{
    public MaterialData()
    {
        Maps = new List<string>();
    }

    public MaterialData(string name, string path, List<string> maps)
    {
        Name = name;
        Path = path;
        Maps = maps;
    }

    public string Name = null;
    public string Path = null;
    public List<string> Maps = null;
    public bool IsUnlit = false;
    public bool IsTransparent = false;
}
