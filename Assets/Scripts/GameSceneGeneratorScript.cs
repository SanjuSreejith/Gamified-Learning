using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldBuilder2D : EditorWindow
{
    public RuleTile groundRule;
    public RuleTile platformRule;
    public Tile ladderTile;
    public Tile decorationTile;

    [MenuItem("Tools/2D World Builder")]
    static void Open() => GetWindow<WorldBuilder2D>("2D World Builder");

    void OnGUI()
    {
        GUILayout.Label("Assign Tiles", EditorStyles.boldLabel);

        groundRule = (RuleTile)EditorGUILayout.ObjectField("Ground RuleTile", groundRule, typeof(RuleTile), false);
        platformRule = (RuleTile)EditorGUILayout.ObjectField("Platform RuleTile", platformRule, typeof(RuleTile), false);
        ladderTile = (Tile)EditorGUILayout.ObjectField("Ladder Tile", ladderTile, typeof(Tile), false);
        decorationTile = (Tile)EditorGUILayout.ObjectField("Decoration Tile", decorationTile, typeof(Tile), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Platformer World"))
            CreateWorld();

        if (GUILayout.Button("Paint Sample Layout"))
            PaintSample();
    }

    void CreateWorld()
    {
        GameObject grid = new GameObject("WorldGrid");
        grid.AddComponent<Grid>();

        CreateLayer("Ground", grid, true, false);
        CreateLayer("Platform", grid, true, true);
        CreateLayer("Ladder", grid, true, false, true);
        CreateLayer("Decoration", grid, false, false);
    }

    void CreateLayer(string name, GameObject parent, bool collision, bool oneWay, bool trigger = false)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent.transform;

        var tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();

        if (!collision) return;

        var collider = go.AddComponent<TilemapCollider2D>();
        collider.usedByComposite = true;
        collider.isTrigger = trigger;

        var composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    void PaintSample()
    {
        var ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        var platform = GameObject.Find("Platform").GetComponent<Tilemap>();
        var ladder = GameObject.Find("Ladder").GetComponent<Tilemap>();
        var deco = GameObject.Find("Decoration").GetComponent<Tilemap>();

        for (int x = -20; x <= 20; x++)
            ground.SetTile(new Vector3Int(x, -6, 0), groundRule);

        for (int x = -5; x <= 5; x++)
            platform.SetTile(new Vector3Int(x, 0, 0), platformRule);

        for (int y = -5; y <= 0; y++)
            ladder.SetTile(new Vector3Int(0, y, 0), ladderTile);

        deco.SetTile(new Vector3Int(-10, -5, 0), decorationTile);
    }
}
