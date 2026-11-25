using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// Сцена Game одна на все уровни. LevelLoader берёт LevelData текущего уровня (GameSession)
// и строит его во время игры: тайлы, герой, враги, предметы, портал, фон, таблички.
// Тот же код загружает любой из трёх ассетов Level_01 / Level_02 / Level_03.
[DefaultExecutionOrder(-100)] // Строим уровень раньше, чем HUD, камера и враги начнут работать.
public class LevelLoader : MonoBehaviour
{
    public HUD hud;
    public CameraFollow2D cameraFollow;
    [Tooltip("Для теста в редакторе: если задан, грузится он, а не уровень из GameSession.")]
    public LevelData overrideLevel;

    public static LevelData Level { get; private set; }

    LevelPalette pal;
    List<string> rows;
    int W, H;
    float timeLeft;
    Health playerHealth;
    bool timeUp;

    char At(int x, int y)
    {
        if (y < 0) return '.';
        if (y >= H || x < 0 || x >= rows[H - 1 - y].Length) return '#';
        return rows[H - 1 - y][x];
    }
    bool Solid(int x, int y) => At(x, y) == '#';
    bool Free(int x, int y) => At(x, y) != '#' && At(x, y) != '=';

    void Awake()
    {
        Level = overrideLevel != null ? overrideLevel : GameSession.Current;
        pal = LevelCatalog.Instance.palette;
        if (Level == null || pal == null)
        {
            Debug.LogError("LevelLoader: no LevelData or palette (Resources/LevelCatalog).");
            return;
        }
        Build();
    }

    void Build()
    {
        rows = Level.map.Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToList();
        H = rows.Count;
        W = rows.Max(r => r.Length);
        var signs = new Dictionary<char, string>();
        if (Level.signs != null)
            foreach (string s in Level.signs)
                if (s.Length > 2) signs[s[0]] = s.Substring(2);

        int groundLayer = LayerMask.NameToLayer("Ground");
        var level = new GameObject("Level").transform;
        var grid = new GameObject("Grid", typeof(Grid)).transform; grid.SetParent(level);
        Tilemap solid = MakeTilemap(grid, "Stone", false, groundLayer);
        Tilemap ledges = MakeTilemap(grid, "Ledges", true, groundLayer);
        Transform actors = new GameObject("Actors").transform;
        Transform pickups = new GameObject("Pickups").transform;
        Transform deco = new GameObject("Decor").transform; deco.SetParent(level);
        GameObject player = null;
        bool hasBoss = false;
        Vector3 bossPos = Vector3.zero;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                char ch = At(x, y);
                int v = (x * 7 + y * 13) % 3;
                Vector3 cell = new Vector3(x + 0.5f, y + 0.5f, 0);
                switch (ch)
                {
                    case '#': solid.SetTile(new Vector3Int(x, y, 0), Solid(x, y + 1) ? pal.innerTiles[v] : pal.topTiles[v]); break;
                    case '=': ledges.SetTile(new Vector3Int(x, y, 0), pal.ledgeTile); break;
                    case 'P': player = Instantiate(pal.player, new Vector3(x + 0.5f, y, 0), Quaternion.identity); player.name = "Player"; break;
                    case 'c': Instantiate(pal.crystal, cell, Quaternion.identity, pickups); break;
                    case '+': Instantiate(pal.heart, cell, Quaternion.identity, pickups); break;
                    case 'T': Instantiate(pal.torch, cell, Quaternion.identity, deco); break;
                    case 'k': Decor("Skull", pal.skull, new Vector3(x + 0.5f, y + 0.31f, 0), -2, Color.white, deco); break;
                    case 'E':
                        Instantiate(pal.portal, new Vector3(x + 0.5f, y, 0), Quaternion.identity, actors)
                            .GetComponent<ExitPortal>().Setup(Level.requiredCrystals);
                        break;
                    case 's':
                    {
                        int l = x, r = x;
                        while (Free(l - 1, y) && At(l - 1, y) != '^' && !Free(l - 1, y - 1)) l--;
                        while (Free(r + 1, y) && At(r + 1, y) != '^' && !Free(r + 1, y - 1)) r++;
                        float sy = y + 0.44f; // Маршрут патруля: края платформы.
                        var s = Instantiate(pal.slime, new Vector3(x + 0.5f, sy, 0), Quaternion.identity, actors).GetComponent<SlimeEnemy>();
                        s.waypoints = new[] { new Vector2(l + 0.5f, sy), new Vector2(r + 0.5f, sy) };
                        break;
                    }
                    case 'b':
                    {
                        int l = x, r = x;
                        while (l > x - 5 && Free(l - 1, y) && Free(l - 1, y + 1) && Free(l - 1, y - 1)) l--;
                        while (r < x + 5 && Free(r + 1, y) && Free(r + 1, y + 1) && Free(r + 1, y - 1)) r++;
                        var b = Instantiate(pal.bat, cell, Quaternion.identity, actors).GetComponent<BatEnemy>();
                        b.waypoints = new[] { new Vector2(l + 0.5f, y + 0.5f), new Vector2(r + 0.5f, y + 0.5f) };
                        break;
                    }
                    case 'M':
                    case 'L':
                    {
                        var p = Instantiate(pal.platform, new Vector3(x + 1.5f, y + 0.375f, 0), Quaternion.identity, actors);
                        p.GetComponent<Animator>().runtimeAnimatorController = ch == 'M' ? pal.platformHorizontal : pal.platformVertical;
                        p.name = ch == 'M' ? "MovingPlatform" : "Lift";
                        break;
                    }
                    case 'r':
                    {
                        var sp = new GameObject("RockSpawner").AddComponent<Spawner>();
                        sp.transform.SetParent(actors);
                        sp.transform.position = cell;
                        sp.prefabs = new[] { pal.rock };
                        sp.firstDelay = pal.items.rockFirstDelay;
                        sp.interval = pal.items.rockInterval;
                        sp.halfWidth = pal.items.rockSpreadX;
                        sp.warnTime = pal.items.rockWarnTime;
                        sp.warnClip = pal.rockWarnClip;
                        break;
                    }
                    case 'a':
                        // Смотрит влево — навстречу игроку, который идёт слева.
                        Instantiate(pal.goblin, new Vector3(x + 0.5f, y + 0.62f, 0), Quaternion.identity, actors).GetComponent<GoblinArcher>().SetFacing(false);
                        break;
                    case 'B': hasBoss = true; bossPos = cell; break;
                    case '^':
                        if (At(x - 1, y) != '^')
                        {
                            int n = 1;
                            while (At(x + n, y) == '^') n++;
                            var sp = Instantiate(pal.spikes, new Vector3(x + n / 2f, y + 0.5f, 0), Quaternion.identity, actors);
                            sp.GetComponent<SpriteRenderer>().size = new Vector2(n, 1);
                            sp.GetComponent<BoxCollider2D>().size = new Vector2(n - 0.25f, 0.45f);
                        }
                        break;
                    default:
                        if (signs.ContainsKey(ch)) MakeSign(deco, signs[ch], cell);
                        break;
                }
            }
        }

        FinishTilemap(solid);
        FinishTilemap(ledges);
        MakeBackground(level, W, 2, H - 1);

        // HUD
        hud.levelTitle = Level.title;
        hud.showCrystals = !hasBoss;

        if (hasBoss)
        {
            var b = Instantiate(pal.boss, bossPos, Quaternion.identity, actors).GetComponent<Boss>();
            b.arenaLeft = 1f; b.arenaRight = W - 1f; b.floorY = 2f; b.ceilingY = H - 3f;
        }

        // Камера сбоку следует за героем и не выходит за край уровня.
        Camera cam = cameraFollow.GetComponent<Camera>();
        float ortho = cam.orthographicSize;
        cameraFollow.target = player.transform;
        cameraFollow.levelMinX = 0f; cameraFollow.levelMaxX = W;
        cameraFollow.minY = ortho - 0.5f; cameraFollow.maxY = H - ortho;
        cam.transform.position = new Vector3(Mathf.Max(player.transform.position.x, 12f), ortho - 0.5f, -10f);
        player.GetComponent<Player2D>().deathY = -3f;
        playerHealth = player.GetComponent<Health>();

        // Музыка уровня (MusicManager сделает кроссфейд, если трек другой).
        if (Level.music != null)
            MusicManager.Play(Level.music, Level.musicVolume);

        timeLeft = Level.timeLimit;
    }

    // Лимит времени из LevelData.
    void Update()
    {
        if (Level == null || Level.timeLimit <= 0f || timeUp || PauseMenu.IsPaused)
        {
            if (Level != null && Level.timeLimit <= 0f) hud.SetTimer("");
            return;
        }
        timeLeft -= Time.deltaTime;
        hud.SetTimer(Mathf.CeilToInt(Mathf.Max(timeLeft, 0f)).ToString(), timeLeft <= 15f);
        if (timeLeft <= 0f)
        {
            timeUp = true;
            hud.ShowMessage("TIME'S UP!", 2f);
            playerHealth.GameOver();
        }
    }

    Tilemap MakeTilemap(Transform grid, string name, bool oneWay, int layer)
    {
        var g = new GameObject(name);
        g.transform.SetParent(grid);
        g.layer = layer;
        g.tag = "Ground";
        var tm = g.AddComponent<Tilemap>();
        var tr = g.AddComponent<TilemapRenderer>();
        tr.sortingOrder = oneWay ? -1 : 0;
        if (pal.spriteMaterial != null) tr.sharedMaterial = pal.spriteMaterial;
        var rb = g.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var tc = g.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comp = g.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        if (oneWay)
        {
            var eff = g.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            comp.usedByEffector = true;
        }
        return tm;
    }

    // Коллайдеры строим сразу после расстановки тайлов.
    static void FinishTilemap(Tilemap tm)
    {
        tm.CompressBounds();
        tm.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        tm.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    SpriteRenderer Decor(string name, Sprite s, Vector3 pos, int order, Color color, Transform parent)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent);
        g.transform.position = pos;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        sr.color = color;
        if (pal.spriteMaterial != null) sr.sharedMaterial = pal.spriteMaterial;
        return sr;
    }

    // Фон подземелья: стена и арки с параллаксом, колонны, знамёна, цепи.
    void MakeBackground(Transform parent, float x1, float groundY, float topY)
    {
        float x0 = 0f, w = x1 - x0, cx = (x0 + x1) / 2f;
        var wall = Decor("BG_Wall", pal.wall, new Vector3(cx, (groundY + topY) / 2f, 0), -50, new Color(0.8f, 0.78f, 0.92f), parent);
        wall.drawMode = SpriteDrawMode.Tiled; wall.size = new Vector2(w + 60f, topY - groundY + 40f);
        wall.gameObject.AddComponent<Parallax>().factor = 0.15f;

        var arches = Decor("BG_Arches", pal.arches, new Vector3(cx, groundY + 3f, 0), -40, new Color(0.85f, 0.85f, 1f), parent);
        arches.drawMode = SpriteDrawMode.Tiled; arches.size = new Vector2(w + 40f, 6f);
        arches.gameObject.AddComponent<Parallax>().factor = 0.35f;

        var deco = new GameObject("Decorations").transform; deco.SetParent(parent);
        int gy = (int)groundY;
        for (float x = x0 + 6f; x < x1 - 2f; x += 12f)
        {
            int ix = Mathf.FloorToInt(x);
            if (Free(ix, gy) && Free(ix, gy + 3) && !Free(ix, gy - 1))
                Decor("Pillar", pal.pillar, new Vector3(x, groundY, 0), -20, new Color(0.75f, 0.75f, 0.85f), deco);
            int bx = Mathf.FloorToInt(x + 6f);
            if (Free(bx, gy + 6) && Free(bx, gy + 7) && bx < x1 - 2)
                Decor("Banner", pal.banner, new Vector3(x + 6f, groundY + 7.5f, 0), -19, new Color(0.85f, 0.8f, 0.85f), deco);
            if (Free(ix + 3, (int)topY - 1) && Free(ix + 3, (int)topY - 2))
                Decor("Chain", pal.chain, new Vector3(x + 3f, topY - 1.25f, 0), -19, new Color(0.7f, 0.7f, 0.8f), deco);
        }

        var dust = new GameObject("Dust").AddComponent<FloatingDust>();
        dust.sprite = pal.dust;
        dust.count = 35;
        dust.area = new Vector2(36f, 18f);
        dust.velocity = new Vector2(0.25f, 0.35f);
        dust.colors = new[] { new Color(1f, 0.85f, 0.6f, 0.35f), new Color(0.6f, 0.9f, 1f, 0.3f) };
    }

    void MakeSign(Transform parent, string text, Vector3 pos)
    {
        var g = new GameObject("Sign").transform;
        g.SetParent(parent);
        g.position = pos;
        float w = text.Length * 0.36f + 0.9f;
        var plate = Decor("Plate", pal.signPlate, pos, -4, new Color(1, 1, 1, 0.92f), g);
        plate.drawMode = SpriteDrawMode.Sliced; plate.size = new Vector2(w, 1.1f);
        var tg = new GameObject("Text");
        tg.transform.SetParent(g, false);
        var t = tg.AddComponent<TextMeshPro>();
        t.font = pal.signFont; t.text = text; t.fontSize = 3.4f; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 1f; t.fontSizeMax = 3.4f;
        t.color = new Color32(254, 231, 97, 255);
        t.rectTransform.sizeDelta = new Vector2(w, 1.1f);
        t.sortingOrder = -3;
    }
}
