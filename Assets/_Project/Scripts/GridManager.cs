using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class PlacedDTO { public string id; public int x; public int y; }

public class GridManager : MonoBehaviour
{
    public float cellSize = 1f;

    private readonly Dictionary<(int,int), Guid> _cells = new();

    public readonly Dictionary<Guid, (string id, Vector2Int origin, Vector2Int size, GameObject go)> Instances = new();

    public void Init()
    {
        _cells.Clear();
        Instances.Clear();
    }

    public Vector2Int WorldToGrid(Vector3 w)
        => new(Mathf.RoundToInt(w.x / cellSize), Mathf.RoundToInt(w.y / cellSize));

    public Vector3 GridToWorld(Vector2Int g)
        => new(g.x * cellSize, g.y * cellSize, 0);

    public bool CanPlace(Vector2Int origin, Vector2Int size)
    {
        for (int x=0; x<size.x; x++)
        for (int y=0; y<size.y; y++)
            if (_cells.ContainsKey((origin.x + x, origin.y + y)))
                return false;
        return true;
    }

    public Guid Place(string id, Vector2Int origin, Vector2Int size, GameObject go)
    {
        var guid = Guid.NewGuid();
        Instances[guid] = (id, origin, size, go);
        for (int x=0; x<size.x; x++)
        for (int y=0; y<size.y; y++)
            _cells[(origin.x + x, origin.y + y)] = guid;
        return guid;
    }

    public bool TryGetAt(Vector2Int cell, out Guid guid) => _cells.TryGetValue((cell.x, cell.y), out guid);

    public void Remove(Guid guid)
    {
        if (!Instances.TryGetValue(guid, out var data)) return;

        for (int x=0; x<data.size.x; x++)
        for (int y=0; y<data.size.y; y++)
            _cells.Remove((data.origin.x + x, data.origin.y + y));

        if (data.go) GameObject.Destroy(data.go);
        Instances.Remove(guid);
    }
}