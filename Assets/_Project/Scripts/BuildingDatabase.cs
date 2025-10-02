using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable] public class BuildingCfg { public string id; public string name; public string sprite; public int w; public int h; }
[Serializable] class BuildingRoot { public List<BuildingCfg> buildings = new(); }

public class BuildingDatabase : MonoBehaviour
{
    public List<BuildingCfg> All = new();

    public void LoadConfigs()
    {
        var path = Path.Combine(Application.dataPath, "_Project/Config/buildings.json");
        if (!File.Exists(path))
        {
            Debug.LogError($"[CONFIG] Не найден: {path}");
            All = new List<BuildingCfg>();
            return;
        }
        var json = File.ReadAllText(path, Encoding.UTF8);
        var root = JsonUtility.FromJson<BuildingRoot>(json);
        All = root?.buildings ?? new List<BuildingCfg>();
        if (All.Count == 0)
            Debug.LogWarning("[CONFIG] Список buildings пуст.");
    }

    public BuildingCfg Get(string id) => All.First(b => b.id == id);
    public Sprite LoadSprite(BuildingCfg cfg) => Resources.Load<Sprite>(cfg.sprite);
}