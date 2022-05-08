using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapName
{
    public string mapName = null;
    public string unityName = null;
    public string displayName = null;

    public MapName(string map, string unity, string display)
    {
        mapName = map;
        unityName = unity;
        displayName = display;
    }
}
