using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum BuildMode { Place, Delete }

[Serializable] class SaveData { public List<PlacedDTO> buildings = new(); }

public class BuildController : MonoBehaviour
{
    public BuildMode Mode { get; private set; } = BuildMode.Place;
    public string ActiveBuildingId { get; private set; }

    private GameObject _ghost;
    private SpriteRenderer _ghostSr;
    private Vector2Int _cursorGrid;
    private int _ignoreFrames;

    public void Init()
    {
        if (Game.I.database.All.Count == 0)
        {
            Debug.LogError("[INIT] Нет конфигов. Проверь buildings.json.");
            return;
        }

        ActiveBuildingId = Game.I.database.All[0].id;
        SpawnGhost();
    }

    private void SpawnGhost()
    {
        _ghost = Instantiate(Game.I.ghostPrefab, Game.I.worldRoot);
        _ghostSr = _ghost.GetComponentInChildren<SpriteRenderer>(true);
        if (_ghostSr == null)
        {
            _ghostSr = _ghost.AddComponent<SpriteRenderer>();
            _ghostSr.sortingOrder = 5;
        }
        UpdateGhostSprite();
    }

    private Sprite LoadSpriteChecked(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
            Debug.LogError($"[SPRITE] Not found: Resources/{path}. " +
                           $"Проверь путь в buildings.json и что файл лежит в Assets/Resources/.. и имеет Texture Type = Sprite.");
        return sp;
    }

    private void UpdateGhostSprite()
    {
        var cfg = Game.I.database.Get(ActiveBuildingId);
        var sp = LoadSpriteChecked(cfg.sprite);
        if (_ghostSr == null)
        {
            _ghostSr = _ghost.GetComponentInChildren<SpriteRenderer>(true);
            if (_ghostSr == null) _ghostSr = _ghost.AddComponent<SpriteRenderer>();
            _ghostSr.sortingOrder = 5;
        }
        _ghostSr.sprite = sp;
        var c = _ghostSr.color; c.a = 0.6f; _ghostSr.color = c;
    }

    private Vector2 ReadPointer() => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    private bool ClickPressedThisFrame => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    private Vector2 ReadMoveAxis()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        int x = (k.rightArrowKey.isPressed || k.dKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed || k.aKey.isPressed ? 1 : 0);
        int y = (k.upArrowKey.isPressed || k.wKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed || k.sKey.isPressed ? 1 : 0);
        return new Vector2(x, y);
    }

    bool IsAppFocused()
    {
#if UNITY_EDITOR
        var w = EditorWindow.focusedWindow;
        if (w == null) return false;
        var title = w.titleContent != null ? w.titleContent.text : "";
        if (title != "Game" && title != "GameView") return false;
#endif
        return Application.isFocused;
    }

    bool IsPointerInsideGame()
    {
        var p = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.negativeInfinity;
        return p.x >= 0 && p.y >= 0 && p.x <= Screen.width && p.y <= Screen.height;
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public void SetMode(BuildMode mode)
    {
        Mode = mode;
        _ignoreFrames = 1;
        if (_ghost) _ghost.SetActive(Mode == BuildMode.Place);
        Game.I.ui.UpdateModeText(mode);
    }

    public void SetActiveBuilding(string id)
    {
        ActiveBuildingId = id;
        UpdateGhostSprite();
        Game.I.ui.SetDropdownTo(id);
    }

    private void Update()
    {
        if (!IsAppFocused()) return;
        if (!IsPointerInsideGame()) return;
        if (IsPointerOverUI()) return;
        if (_ignoreFrames > 0) { _ignoreFrames--; return; }

        var k = Keyboard.current;
        if (k != null)
        {
            if (k.bKey.wasPressedThisFrame) SetMode(BuildMode.Place);
            if (k.deleteKey.wasPressedThisFrame) SetMode(BuildMode.Delete);
        }

        var pos = ReadPointer();
        var world = Game.I.mainCamera.ScreenToWorldPoint(new Vector3(pos.x, pos.y, -Game.I.mainCamera.transform.position.z));
        var grid = Game.I.gridManager.WorldToGrid(world);
        var move = ReadMoveAxis();
        grid += new Vector2Int(Mathf.RoundToInt(move.x), Mathf.RoundToInt(move.y));
        _cursorGrid = grid;

        if (Mode == BuildMode.Place)
        {
            UpdateGhost();
            if (ClickPressedThisFrame) TryPlace();
        }
        else if (Mode == BuildMode.Delete)
        {
            if (ClickPressedThisFrame) TryDelete();
        }
    }

    private void UpdateGhost()
    {
        var cfg = Game.I.database.Get(ActiveBuildingId);
        bool can = Game.I.gridManager.CanPlace(_cursorGrid, new Vector2Int(cfg.w, cfg.h));
        _ghost.transform.position = Game.I.gridManager.GridToWorld(_cursorGrid);
        _ghostSr.color = can ? new Color(0.4f,1f,0.4f,0.6f) : new Color(1f,0.4f,0.4f,0.6f);
    }

    private void TryPlace()
    {
        var cfg = Game.I.database.Get(ActiveBuildingId);
        var size = new Vector2Int(cfg.w, cfg.h);
        if (!Game.I.gridManager.CanPlace(_cursorGrid, size)) return;

        var go = Instantiate(Game.I.buildingPrefab, Game.I.worldRoot);
        go.transform.position = Game.I.gridManager.GridToWorld(_cursorGrid);
        go.name = $"{cfg.name}";

        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null)
        {
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
        }
        sr.sprite = LoadSpriteChecked(cfg.sprite);
        sr.color = Color.white;

        Game.I.gridManager.Place(cfg.id, _cursorGrid, size, go);
    }

    private void TryDelete()
    {
        if (Game.I.gridManager.TryGetAt(_cursorGrid, out var guid))
            Game.I.gridManager.Remove(guid);
    }

    string SavePath => Path.Combine(Application.persistentDataPath, "layout.json");

    public void Save()
    {
        var data = new SaveData();
        foreach (var kv in Game.I.gridManager.Instances)
            data.buildings.Add(new PlacedDTO { id = kv.Value.id, x = kv.Value.origin.x, y = kv.Value.origin.y });
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json, Encoding.UTF8);
#if UNITY_EDITOR
        Debug.Log($"[SAVE] {SavePath}");
#endif
    }

    public void LoadOnStart()
    {
        if (!File.Exists(SavePath)) return;
        var json = File.ReadAllText(SavePath, Encoding.UTF8);
        var data = JsonUtility.FromJson<SaveData>(json);
        Apply(data);
    }

    public void ExportReadable() => Save();

    public void ImportFrom(string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path, Encoding.UTF8);
        var data = JsonUtility.FromJson<SaveData>(json);
        ClearAll();
        Apply(data);
    }

    private void Apply(SaveData data)
    {
        if (data?.buildings == null) return;

        foreach (var dto in data.buildings)
        {
            var cfg = Game.I.database.Get(dto.id);
            var size = new Vector2Int(cfg.w, cfg.h);
            var cell = new Vector2Int(dto.x, dto.y);
            if (!Game.I.gridManager.CanPlace(cell, size)) continue;

            var go = Instantiate(Game.I.buildingPrefab, Game.I.worldRoot);
            go.transform.position = Game.I.gridManager.GridToWorld(cell);
            go.name = $"{cfg.name}";

            var sr = go.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;
            }
            sr.sprite = LoadSpriteChecked(cfg.sprite);
            sr.color = Color.white;

            Game.I.gridManager.Place(cfg.id, cell, size, go);
        }
    }

    private void ClearAll()
    {
        foreach (var kv in new List<Guid>(Game.I.gridManager.Instances.Keys))
            Game.I.gridManager.Remove(kv);
    }
}
