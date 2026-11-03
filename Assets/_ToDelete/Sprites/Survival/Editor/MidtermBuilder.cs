// ============================================================================
//  MIDTERM BUILDER — меню Midterm → Build Everything.
//  Этот Editor-скрипт сам собирает проект: импорт спрайтов, нарезка спрайт-листов,
//  тайлы, Animation Clips и Animator Controllers, префабы, 5 сцен, HUD с джойстиком,
//  Build Settings, ориентация Landscape и Unity Remote (iOS).
//  Игровая логика лежит в Assets/Script — этот файл только расставляет объекты.
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MidtermBuilder
{
    const string Root = "Assets/Midterm";
    const string Art = Root + "/Art";
    const string Gen = Root + "/Generated";
    const string ScenesDir = "Assets/Scenes/Midterm";
    const float OrthoSize = 7f;

    static readonly string[] SceneOrder = { "MainMenu", "Level1", "Level2", "Level3_Boss", "Win" };

    // Сколько кадров в каждом спрайт-листе (полоса кадров слева направо).
    static readonly Dictionary<string, int> Sheets = new Dictionary<string, int>
    {
        { "Hero_Idle", 4 }, { "Hero_Run", 6 }, { "Hero_Jump", 2 }, { "Slime_Walk", 4 }, { "Bat_Fly", 4 },
        { "Boss_Idle", 4 }, { "Boss_Attack", 2 }, { "Crystal_Spin", 6 }, { "Portal_Open", 6 }, { "Torch", 4 },
        { "Poof", 5 }, { "HitSpark", 3 },
    };
    static readonly HashSet<string> BottomPivot = new HashSet<string> { "Hero_Idle", "Hero_Run", "Hero_Jump", "Portal_Open", "Portal_Closed", "Pillar" };
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "UI_Panel", new Vector4(6, 6, 6, 6) }, { "UI_Button", new Vector4(5, 5, 5, 5) }, { "UI_ButtonGrey", new Vector4(5, 5, 5, 5) },
        { "UI_BarBack", new Vector4(3, 3, 3, 3) }, { "UI_BarFill", new Vector4(2, 2, 2, 2) },
    };

    static int groundLayer = 6;
    static TMP_FontAsset font;
    static Material spriteMat;
    static Color bgColor = new Color32(14, 11, 22, 255);

    // Префабы
    static GameObject pPlayer, pBullet, pOrb, pFireball, pWarning, pRock, pCrystal, pHeart, pSlime, pBat, pBoss,
        pSpikes, pPortal, pTorch, pPlatform, pPoof, pSpark, pHUD, pSystems;
    static Tile[] tTop, tInner;
    static Tile tLedge;
    static AnimatorController cHero, cBossCtrl, cPlatH, cPlatV;

    // ------------------------------------------------------------------ MENU
    [MenuItem("Midterm/Build Everything", priority = 0)]
    public static void BuildEverything()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        try
        {
            Step("Tags & layers", 0.05f); SetupTagsAndLayers();
            Step("Importing sprites", 0.15f); ImportArt();
            Step("Font", 0.25f); MakeFont();
            Step("Tiles", 0.3f); MakeTiles();
            Step("Animations", 0.4f); MakeAnimations();
            Step("Prefabs", 0.55f); MakePrefabs();
            Step("Scenes", 0.7f); BuildAllScenes();
            Step("Project settings", 0.95f); ConfigureProject();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
            Debug.Log("<color=#7CFC00><b>MIDTERM: всё собрано!</b></color> Открыта сцена MainMenu — нажми Play.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // Для запуска из командной строки: Unity.exe -batchmode -executeMethod MidtermBuilder.BatchBuild -quit
    public static void BatchBuild()
    {
        BuildEverything();
    }

    [MenuItem("Midterm/Open Main Menu Scene", priority = 20)]
    static void OpenMenu()
    {
        EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
    }

    [MenuItem("Midterm/Build Windows .exe (backup demo)", priority = 40)]
    static void BuildWindows()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = SceneOrder.Select(s => ScenesDir + "/" + s + ".unity").ToArray(),
            locationPathName = "Builds/Windows/DungeonKnight.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("Windows build: " + report.summary.result + " -> " + opts.locationPathName);
    }

    static void Step(string s, float p)
    {
        EditorUtility.DisplayProgressBar("Midterm Builder", s, p);
        Debug.Log("[Midterm] " + s);
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // ------------------------------------------------------------------ TAGS / LAYERS
    static void SetupTagsAndLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tm.FindProperty("tags");
        foreach (string tag in new[] { "Ground", "Enemy", "Exit", "Crystal" })
        {
            bool found = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) found = true;
            if (!found)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }
        var layers = tm.FindProperty("layers");
        int idx = -1;
        for (int i = 6; i < 32; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == "Ground") idx = i;
        if (idx < 0)
            for (int i = 6; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = "Ground";
                    idx = i;
                    break;
                }
        tm.ApplyModifiedProperties();
        groundLayer = idx;
    }

    // ------------------------------------------------------------------ ART IMPORT
    static void ImportArt()
    {
        AssetDatabase.Refresh();
        spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        foreach (string file in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = name.StartsWith("BG_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect; // FullRect нужен для Tiled/Sliced.
            st.spriteAlignment = (int)(BottomPivot.Contains(name) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            st.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(st);
            if (Borders.TryGetValue(name, out Vector4 border))
                ti.spriteBorder = border;

            bool sheet = Sheets.TryGetValue(name, out int frames);
            ti.spriteImportMode = sheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            ti.SaveAndReimport();

            if (sheet)
                SliceSheet(ti, name, frames, BottomPivot.Contains(name));
        }
        AssetDatabase.Refresh();
    }

    // Нарезка спрайт-листа на кадры (как Sprite Editor → Slice → Grid By Cell Count).
    static void SliceSheet(TextureImporter ti, string name, int frames, bool bottom)
    {
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        int fw = w / frames;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int i = 0; i < frames; i++)
        {
            rects.Add(new SpriteRect
            {
                name = name + "_" + i,
                rect = new Rect(i * fw, 0, fw, h),
                alignment = bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center,
                pivot = bottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            });
        }
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
        ti.SaveAndReimport();
    }

    static Sprite[] Frames(string rel)
    {
        string path = Art + "/" + rel + ".png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (sprites.Length == 0)
            throw new Exception("Sprite not found: " + path);
        return sprites.OrderBy(s => Suffix(s.name)).ToArray();
    }

    static int Suffix(string n)
    {
        int i = n.LastIndexOf('_');
        return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
    }

    static Sprite Spr(string rel) => Frames(rel)[0];

    static AudioClip Sfx(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + n + ".wav");

    // ------------------------------------------------------------------ FONT
    static void MakeFont()
    {
        if (AssetDatabase.IsValidFolder(Gen))
            AssetDatabase.DeleteAsset(Gen);
        Folder(Gen); Folder(Gen + "/Tiles"); Folder(Gen + "/Anim"); Folder(Gen + "/Prefabs"); Folder(ScenesDir);

        font = null;
        try
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/PressStart2P.ttf");
            font = TMP_FontAsset.CreateFontAsset(ttf, 32, 4, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false);
            font.name = "PressStart2P SDF";
            AssetDatabase.CreateAsset(font, Gen + "/PressStart2P SDF.asset");
            string chars = "";
            for (char c = ' '; c <= '~'; c++) chars += c;
            font.TryAddCharacters(chars);
            font.atlasTextures[0].name = "PressStart2P Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = "PressStart2P Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Pixel font failed, using TMP default: " + e.Message);
            font = TMP_Settings.defaultFontAsset;
        }
    }

    // ------------------------------------------------------------------ TILES
    static void MakeTiles()
    {
        tTop = new Tile[3];
        tInner = new Tile[3];
        for (int i = 0; i < 3; i++)
        {
            tTop[i] = MakeTile("Top_" + i, Spr("Tiles/Tile_Top_" + i));
            tInner[i] = MakeTile("Inner_" + i, Spr("Tiles/Tile_Inner_" + i));
        }
        tLedge = MakeTile("Ledge", Spr("Tiles/Tile_Ledge"));
    }

    static Tile MakeTile(string name, Sprite s)
    {
        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = s;
        t.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(t, Gen + "/Tiles/" + name + ".asset");
        return t;
    }

    // ------------------------------------------------------------------ ANIMATIONS
    static AnimationClip SpriteClip(string name, Sprite[] frames, float fps, bool loop, string path = "")
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve(path, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        SetLoop(clip, loop);
        AssetDatabase.CreateAsset(clip, Gen + "/Anim/" + name + ".anim");
        return clip;
    }

    // Клип, который двигает дочерний объект (платформа, сердце) — Animation Clip как в A5.
    static AnimationClip MoveClip(string name, string path, Vector3 offset, float halfTime)
    {
        var clip = new AnimationClip { frameRate = 60 };
        string[] axes = { "x", "y", "z" };
        for (int a = 0; a < 3; a++)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(halfTime, offset[a], 0f, 0f),
                new Keyframe(halfTime * 2f, 0f, 0f, 0f));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axes[a]), curve);
        }
        SetLoop(clip, true);
        AssetDatabase.CreateAsset(clip, Gen + "/Anim/" + name + ".anim");
        return clip;
    }

    static void SetLoop(AnimationClip clip, bool loop)
    {
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
    }

    static AnimatorController SimpleController(string name, AnimationClip clip)
    {
        var c = AnimatorController.CreateAnimatorControllerAtPath(Gen + "/Anim/" + name + ".controller");
        var st = c.layers[0].stateMachine.AddState(clip.name.Replace("Clip_", ""));
        st.motion = clip;
        c.layers[0].stateMachine.defaultState = st;
        return c;
    }

    static AnimatorStateTransition Trans(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        return t;
    }

    static readonly Dictionary<string, AnimatorController> Ctrls = new Dictionary<string, AnimatorController>();

    static void MakeAnimations()
    {
        Ctrls.Clear();
        // --- Hero: Idle / Run / Jump / Fall
        var idle = SpriteClip("Hero_Idle", Frames("Hero/Hero_Idle"), 6, true);
        var run = SpriteClip("Hero_Run", Frames("Hero/Hero_Run"), 12, true);
        var jf = Frames("Hero/Hero_Jump");
        var jump = SpriteClip("Hero_Jump", new[] { jf[0] }, 10, true);
        var fall = SpriteClip("Hero_Fall", new[] { jf[1] }, 10, true);

        cHero = AnimatorController.CreateAnimatorControllerAtPath(Gen + "/Anim/Hero.controller");
        cHero.AddParameter("Speed", AnimatorControllerParameterType.Float);
        cHero.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        cHero.AddParameter("VelY", AnimatorControllerParameterType.Float);
        var sm = cHero.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sRun = sm.AddState("Run"); sRun.motion = run;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        var sFall = sm.AddState("Fall"); sFall.motion = fall;
        sm.defaultState = sIdle;
        Trans(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Trans(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        foreach (var s in new[] { sIdle, sRun, sFall })
        {
            var t = Trans(s, sJump);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelY");
        }
        foreach (var s in new[] { sIdle, sRun, sJump })
        {
            var t = Trans(s, sFall);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, "VelY");
        }
        foreach (var s in new[] { sJump, sFall })
            Trans(s, sIdle).AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        // --- simple looping
        Ctrls["Slime"] = SimpleController("Slime", SpriteClip("Slime_Walk", Frames("Enemies/Slime_Walk"), 6, true));
        Ctrls["Bat"] = SimpleController("Bat", SpriteClip("Bat_Fly", Frames("Enemies/Bat_Fly"), 10, true));
        Ctrls["Crystal"] = SimpleController("Crystal", SpriteClip("Crystal_Spin", Frames("Pickups/Crystal_Spin"), 10, true));
        Ctrls["Torch"] = SimpleController("Torch", SpriteClip("Torch_Burn", Frames("Background/Torch"), 8, true));
        Ctrls["Poof"] = SimpleController("Poof", SpriteClip("Poof", Frames("FX/Poof"), 14, false));
        Ctrls["Spark"] = SimpleController("Spark", SpriteClip("HitSpark", Frames("FX/HitSpark"), 20, false));
        Ctrls["Heart"] = SimpleController("Heart", MoveClip("Heart_Bob", "Sprite", new Vector3(0, 0.2f, 0), 0.6f));
        cPlatH = SimpleController("Platform_H", MoveClip("Platform_MoveH", "Body", new Vector3(5f, 0, 0), 2.4f));
        cPlatV = SimpleController("Platform_V", MoveClip("Platform_Lift", "Body", new Vector3(0, 4f, 0), 2.0f));

        // --- Portal: Closed -> Open (bool)
        var pc = AnimatorController.CreateAnimatorControllerAtPath(Gen + "/Anim/Portal.controller");
        pc.AddParameter("Open", AnimatorControllerParameterType.Bool);
        var psm = pc.layers[0].stateMachine;
        var sClosed = psm.AddState("Closed"); sClosed.motion = SpriteClip("Portal_Closed", new[] { Spr("Level/Portal_Closed") }, 1, true);
        var sOpen = psm.AddState("Open"); sOpen.motion = SpriteClip("Portal_Open", Frames("Level/Portal_Open"), 10, true);
        psm.defaultState = sClosed;
        Trans(sClosed, sOpen).AddCondition(AnimatorConditionMode.If, 0, "Open");
        Ctrls["Portal"] = pc;

        // --- Boss: Idle <-> Attack (trigger)
        var bi = SpriteClip("Boss_Idle", Frames("Boss/Boss_Idle"), 6, true);
        var ba = Frames("Boss/Boss_Attack");
        var bat = SpriteClip("Boss_Attack", new[] { ba[0], ba[1], ba[0], ba[1] }, 8, false);
        cBossCtrl = AnimatorController.CreateAnimatorControllerAtPath(Gen + "/Anim/Boss.controller");
        cBossCtrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        var bsm = cBossCtrl.layers[0].stateMachine;
        var sbI = bsm.AddState("Idle"); sbI.motion = bi;
        var sbA = bsm.AddState("Attack"); sbA.motion = bat;
        bsm.defaultState = sbI;
        Trans(sbI, sbA).AddCondition(AnimatorConditionMode.If, 0, "Attack");
        var back = sbA.AddTransition(sbI);
        back.hasExitTime = true;
        back.exitTime = 1f;
        back.duration = 0f;
    }

    // ------------------------------------------------------------------ HELPERS
    static GameObject Go(string name, Transform parent = null)
    {
        var g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        return g;
    }

    static SpriteRenderer AddSR(GameObject g, Sprite s, int order, Color? color = null)
    {
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        if (spriteMat != null) sr.sharedMaterial = spriteMat;
        if (color.HasValue) sr.color = color.Value;
        return sr;
    }

    static Animator AddAnim(GameObject g, RuntimeAnimatorController c)
    {
        var a = g.AddComponent<Animator>();
        a.runtimeAnimatorController = c;
        return a;
    }

    static Rigidbody2D AddRB(GameObject g, RigidbodyType2D type, float gravity = 0f)
    {
        var rb = g.AddComponent<Rigidbody2D>();
        rb.bodyType = type;
        rb.gravityScale = gravity;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    static GameObject SavePrefab(GameObject g, string name)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(g, Gen + "/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(g);
        return p;
    }

    static GameObject Place(GameObject prefab, Vector3 pos, Transform parent = null)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.transform.position = pos;
        if (parent != null) g.transform.SetParent(parent, true);
        return g;
    }

    // ------------------------------------------------------------------ PREFABS
    static void MakePrefabs()
    {
        // --- эффекты
        var g = Go("Poof"); AddSR(g, Frames("FX/Poof")[0], 20); AddAnim(g, Ctrls["Poof"]);
        g.AddComponent<AutoDestroy>().time = 0.36f; g.transform.localScale = Vector3.one * 1.3f;
        pPoof = SavePrefab(g, "Poof");
        g = Go("HitSpark"); AddSR(g, Frames("FX/HitSpark")[0], 20); AddAnim(g, Ctrls["Spark"]);
        g.AddComponent<AutoDestroy>().time = 0.15f;
        pSpark = SavePrefab(g, "HitSpark");

        // --- снаряды (наследники Projectile)
        g = Go("Bullet"); AddSR(g, Spr("FX/Bullet"), 12); AddRB(g, RigidbodyType2D.Dynamic).collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cc = g.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.15f;
        var bl = g.AddComponent<Bullet>(); bl.speed = 16f; bl.lifetime = 1.4f; bl.damage = 1; bl.hitFx = pSpark;
        pBullet = SavePrefab(g, "Bullet");

        g = Go("EnemyOrb"); AddSR(g, Spr("FX/EnemyOrb"), 13); AddRB(g, RigidbodyType2D.Dynamic);
        cc = g.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.25f;
        var ep = g.AddComponent<EnemyProjectile>(); ep.speed = 6f; ep.lifetime = 7f; ep.hitFx = pSpark;
        pOrb = SavePrefab(g, "EnemyOrb");

        g = Go("Fireball"); AddRB(g, RigidbodyType2D.Dynamic);
        var fs = Go("Sprite", g.transform); AddSR(fs, Spr("FX/Fireball"), 13); fs.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        cc = g.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.3f;
        ep = g.AddComponent<EnemyProjectile>(); ep.speed = 9f; ep.lifetime = 4f; ep.hitFx = pPoof;
        pFireball = SavePrefab(g, "Fireball");

        // --- предупреждение атаки босса: красная колонна + «!»
        g = Go("Warning");
        var col = Go("Column", g.transform); AddSR(col, Spr("FX/WhitePixel"), 8, new Color(0.9f, 0.15f, 0.2f, 0.32f));
        col.transform.localScale = new Vector3(5f, 52f, 1f); col.transform.localPosition = new Vector3(0, 6.5f, 0);
        var ex = Go("Mark", g.transform); AddSR(ex, Spr("FX/Warning"), 9); ex.transform.localPosition = new Vector3(0, 2.2f, 0);
        ex.transform.localScale = Vector3.one * 1.4f;
        g.AddComponent<AutoDestroy>().time = 3f;
        pWarning = SavePrefab(g, "Warning");

        // --- падающий сталактит (Rock : Hazard, твой класс из A6)
        g = Go("Stalactite"); AddSR(g, Spr("FX/Stalactite"), 6); AddRB(g, RigidbodyType2D.Dynamic, 1.3f);
        var bc = g.AddComponent<BoxCollider2D>(); bc.size = new Vector2(0.55f, 0.9f);
        var rock = g.AddComponent<Rock>(); rock.lifetime = 6f; rock.breakOnGround = true; rock.breakFx = pPoof; rock.breakClip = Sfx("sfx_land_rock");
        pRock = SavePrefab(g, "Stalactite");

        // --- подбираемые предметы
        g = Go("Crystal"); g.tag = "Crystal"; AddSR(g, Frames("Pickups/Crystal_Spin")[0], 4); AddAnim(g, Ctrls["Crystal"]);
        cc = g.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.45f;
        var cr = g.AddComponent<Crystal>(); cr.clip = Sfx("sfx_pickup"); cr.fx = pSpark;
        var glow = Go("Glow", g.transform); AddSR(glow, Spr("Background/Glow"), 3, new Color(0.3f, 0.9f, 1f, 0.6f)); glow.transform.localScale = Vector3.one * 0.8f;
        pCrystal = SavePrefab(g, "Crystal");

        g = Go("Heart"); AddAnim(g, Ctrls["Heart"]);
        var hs = Go("Sprite", g.transform); AddSR(hs, Spr("Pickups/Heart"), 4);
        cc = g.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.45f;
        g.AddComponent<HeartPickup>().fx = pSpark;
        pHeart = SavePrefab(g, "Heart");

        // --- враги (наследники Enemy)
        g = Go("Slime"); g.tag = "Enemy"; AddSR(g, Frames("Enemies/Slime_Walk")[0], 5); AddAnim(g, Ctrls["Slime"]); AddRB(g, RigidbodyType2D.Kinematic);
        bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(0.9f, 0.65f); bc.offset = new Vector2(0, -0.1f);
        var sl = g.AddComponent<SlimeEnemy>(); sl.hp = 2; sl.speed = 2f; sl.deathFx = pPoof; sl.hitClip = Sfx("sfx_hit"); sl.dieClip = Sfx("sfx_enemy_die");
        pSlime = SavePrefab(g, "Slime");

        g = Go("Bat"); g.tag = "Enemy"; AddSR(g, Frames("Enemies/Bat_Fly")[0], 5); AddAnim(g, Ctrls["Bat"]); AddRB(g, RigidbodyType2D.Kinematic);
        bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(0.8f, 0.5f);
        var bt = g.AddComponent<BatEnemy>(); bt.hp = 1; bt.deathFx = pPoof; bt.hitClip = Sfx("sfx_hit"); bt.dieClip = Sfx("sfx_enemy_die");
        pBat = SavePrefab(g, "Bat");

        g = Go("SlimeKing"); g.tag = "Enemy"; AddSR(g, Frames("Boss/Boss_Idle")[0], 6); AddAnim(g, cBossCtrl); AddRB(g, RigidbodyType2D.Kinematic);
        var bcap = g.AddComponent<CapsuleCollider2D>(); bcap.isTrigger = true; bcap.direction = CapsuleDirection2D.Horizontal;
        bcap.size = new Vector2(2.6f, 2.0f); bcap.offset = new Vector2(0, -0.35f);
        var boss = g.AddComponent<Boss>(); boss.hp = 20; boss.deathFx = pPoof; boss.hitClip = Sfx("sfx_hit"); boss.dieClip = Sfx("sfx_enemy_die");
        boss.orbPrefab = pOrb.GetComponent<EnemyProjectile>(); boss.fireballPrefab = pFireball.GetComponent<EnemyProjectile>();
        boss.warningPrefab = pWarning; boss.shotClip = Sfx("sfx_boss_shot"); boss.warningClip = Sfx("sfx_warning"); boss.roarClip = Sfx("sfx_boss_roar");
        pBoss = SavePrefab(g, "SlimeKing");

        // --- окружение
        g = Go("Spikes"); var ssr = AddSR(g, Spr("Level/Spikes"), 1); ssr.drawMode = SpriteDrawMode.Tiled; ssr.size = new Vector2(1, 1);
        bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(0.8f, 0.45f); bc.offset = new Vector2(0, -0.25f);
        g.AddComponent<Spikes>();
        pSpikes = SavePrefab(g, "Spikes");

        g = Go("Portal"); g.tag = "Exit"; AddSR(g, Spr("Level/Portal_Closed"), 2); AddAnim(g, Ctrls["Portal"]);
        bc = g.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(1.1f, 1.8f); bc.offset = new Vector2(0, 0.9f);
        var portal = g.AddComponent<ExitPortal>(); portal.openClip = Sfx("sfx_portal"); portal.lockedClip = Sfx("sfx_locked"); portal.enterClip = Sfx("sfx_portal");
        glow = Go("Glow", g.transform); AddSR(glow, Spr("Background/Glow"), 1, new Color(0.6f, 0.4f, 1f, 0.8f));
        glow.transform.localPosition = new Vector3(0, 1.1f, 0); glow.transform.localScale = Vector3.one * 2.2f;
        pPortal = SavePrefab(g, "Portal");

        g = Go("Torch"); AddSR(g, Frames("Background/Torch")[0], -5); AddAnim(g, Ctrls["Torch"]);
        glow = Go("Glow", g.transform); AddSR(glow, Spr("Background/Glow"), -6, new Color(1f, 0.68f, 0.25f, 0.45f)); glow.transform.localScale = Vector3.one * 2.2f;
        glow.transform.localPosition = new Vector3(0, 0.3f, 0);
        pTorch = SavePrefab(g, "Torch");

        // Платформа: Animator на корне двигает дочерний Body клипом.
        g = Go("MovingPlatform"); var pan = AddAnim(g, cPlatH); pan.updateMode = AnimatorUpdateMode.Fixed;
        var body = Go("Body", g.transform); body.layer = groundLayer; body.tag = "Ground";
        AddSR(body, Spr("Level/MovingPlatform"), 1); AddRB(body, RigidbodyType2D.Kinematic).interpolation = RigidbodyInterpolation2D.None;
        bc = body.AddComponent<BoxCollider2D>(); bc.size = new Vector2(3f, 0.7f);
        body.AddComponent<MovingPlatform>();
        pPlatform = SavePrefab(g, "MovingPlatform");

        // --- герой
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Input/GameControls.inputactions");
        g = Go("Player"); g.tag = "Player";
        AddSR(g, Frames("Hero/Hero_Idle")[0], 10);
        AddAnim(g, cHero);
        var prb = AddRB(g, RigidbodyType2D.Dynamic, 3f); prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cap = g.AddComponent<CapsuleCollider2D>(); cap.size = new Vector2(0.7f, 1.25f); cap.offset = new Vector2(-0.05f, 0.63f);
        var nofr = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/NoFriction.physicsMaterial2D");
        if (nofr != null) cap.sharedMaterial = nofr;
        var pi = g.AddComponent<PlayerInput>();
        pi.actions = actions;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        var pl = g.AddComponent<Player2D>(); pl.groundMask = 1 << groundLayer; pl.jumpClip = Sfx("sfx_jump");
        var hp = g.AddComponent<Health>(); hp.hurtClip = Sfx("sfx_hurt"); hp.healClip = Sfx("sfx_heal"); hp.deathFx = pPoof;
        var sh = g.AddComponent<Shooter>(); sh.bulletPrefab = pBullet.GetComponent<Bullet>(); sh.shootClip = Sfx("sfx_shoot");
        pPlayer = SavePrefab(g, "Player");

        pHUD = MakeHUD();
        pSystems = MakeSystems();
    }

    // ------------------------------------------------------------------ UI HELPERS
    static RectTransform UI(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        var rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    static Image Img(string name, Transform parent, Sprite s, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, bool sliced = false)
    {
        var rt = UI(name, parent, anchor, pivot, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        if (sliced) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        return img;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, float size, Color color, Vector2 anchor, Vector2 pos, Vector2 box,
        TextAlignmentOptions align = TextAlignmentOptions.Center, bool shadow = true)
    {
        if (shadow)
        {
            var sh = Txt(name + "_Shadow", parent, text, size, new Color(0.07f, 0.05f, 0.12f, 0.9f), anchor, pos + new Vector2(size * 0.08f + 2, -size * 0.08f - 2), box, align, false);
            sh.raycastTarget = false;
        }
        var rt = UI(name, parent, anchor, new Vector2(0.5f, 0.5f), pos, box);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static Canvas MakeCanvas(GameObject g, int order)
    {
        var c = g.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var cs = g.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 1f;
        return c;
    }

    static Button MakeButton(string name, Transform parent, string label, Sprite bg, Vector2 pos, Vector2 size)
    {
        var img = Img(name, parent, bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, true);
        img.pixelsPerUnitMultiplier = 0.8f;
        var b = img.gameObject.AddComponent<Button>();
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        var colors = b.colors; colors.highlightedColor = new Color(1f, 0.95f, 0.8f); colors.pressedColor = new Color(0.75f, 0.7f, 0.65f); b.colors = colors;
        Txt("Label", img.transform, label, 52, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 4), size);
        return b;
    }

    // ------------------------------------------------------------------ HUD (джойстик, кнопки, сердца)
    static GameObject MakeHUD()
    {
        var g = Go("HUD");
        MakeCanvas(g, 10);
        g.AddComponent<GraphicRaycaster>();
        var hud = g.AddComponent<HUD>();

        var safe = UI("SafeArea", g.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeArea>();

        // Сердца (HP)
        hud.heartFull = Spr("UI/UI_HeartFull");
        hud.heartEmpty = Spr("UI/UI_HeartEmpty");
        hud.hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            hud.hearts[i] = Img("Heart" + i, safe, hud.heartFull, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40 + i * 100, -36), new Vector2(91, 85));
            hud.hearts[i].raycastTarget = false;
        }

        // Счётчик кристаллов
        var cg = UI("Crystals", safe, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -140), new Vector2(360, 90));
        var ci = Img("Icon", cg, Spr("UI/UI_Crystal"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(70, 80));
        ci.raycastTarget = false;
        hud.crystalGroup = cg.gameObject;
        hud.crystalText = Txt("Count", cg, "0/0", 46, Color.white, new Vector2(0, 0.5f), new Vector2(230, 0), new Vector2(260, 80), TextAlignmentOptions.Left, false);

        // Название уровня и сообщения
        hud.titleText = Txt("Title", safe, "", 64, new Color32(254, 174, 52, 255), new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(1600, 120), TextAlignmentOptions.Center, false);
        hud.messageText = Txt("Message", safe, "", 40, new Color32(199, 253, 255, 255), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1600, 90), TextAlignmentOptions.Center, false);

        // Полоска здоровья босса
        var bg = UI("BossBar", safe, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(820, 110));
        hud.bossGroup = bg.gameObject;
        Txt("Name", bg, "SLIME KING", 34, new Color32(246, 117, 122, 255), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(800, 50));
        var back = Img("Back", bg, Spr("UI/UI_BarBack"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(30, 6), new Vector2(720, 48), true);
        back.pixelsPerUnitMultiplier = 1f;
        var fill = Img("Fill", back.transform, Spr("UI/UI_BarFill"), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, true);
        fill.pixelsPerUnitMultiplier = 1f;
        var frt = fill.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(19, 19); frt.offsetMax = new Vector2(-19, -19);
        hud.bossFill = frt;
        Img("Skull", bg, Spr("UI/UI_Skull"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-355, 0), new Vector2(66, 66));

        // Экранный джойстик (On-Screen Stick -> <Gamepad>/leftStick)
        var joyBase = Img("Joystick", safe, Spr("UI/UI_JoyBase"), Vector2.zero, Vector2.zero, new Vector2(60, 40), new Vector2(330, 330));
        joyBase.color = new Color(1, 1, 1, 0.85f);
        joyBase.raycastTarget = false;
        var knob = Img("Knob", joyBase.transform, Spr("UI/UI_JoyKnob"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180));
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        var so = new SerializedObject(stick);
        so.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
        so.FindProperty("m_MovementRange").floatValue = 110f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Кнопки Fire и Jump (On-Screen Button)
        OnScreenBtn("FireButton", safe, Spr("UI/UI_BtnFire"), new Vector2(-50, 50), 230, "<Gamepad>/buttonWest");
        OnScreenBtn("JumpButton", safe, Spr("UI/UI_BtnJump"), new Vector2(-310, 130), 200, "<Gamepad>/buttonSouth");

        return SavePrefab(g, "HUD");
    }

    static void OnScreenBtn(string name, Transform parent, Sprite s, Vector2 pos, float size, string path)
    {
        var img = Img(name, parent, s, new Vector2(1, 0), new Vector2(1, 0), pos, new Vector2(size, size));
        img.color = new Color(1, 1, 1, 0.9f);
        var b = img.gameObject.AddComponent<OnScreenButton>();
        var so = new SerializedObject(b);
        so.FindProperty("m_ControlPath").stringValue = path;
        so.ApplyModifiedPropertiesWithoutUndo();
        img.gameObject.AddComponent<PressFeedback>();
    }

    // Музыка + затемнение между сценами
    static GameObject MakeSystems()
    {
        var g = Go("GameSystems");
        g.AddComponent<GameAudio>();
        var fader = g.AddComponent<SceneFader>();
        var cv = Go("FaderCanvas", g.transform);
        MakeCanvas(cv, 100);
        var black = Img("Black", cv.transform, Spr("FX/WhitePixel"), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(black.rectTransform);
        black.color = bgColor;
        black.raycastTarget = false;
        fader.black = black;
        return SavePrefab(g, "GameSystems");
    }

    // ------------------------------------------------------------------ SCENES
    static void BuildAllScenes()
    {
        BuildMenu();
        foreach (string lvl in new[] { "Level1", "Level2", "Level3_Boss" })
            BuildLevel(lvl);
        BuildWin();
    }

    static Camera MakeCamera(Vector3 pos)
    {
        var g = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        g.tag = "MainCamera";
        var cam = g.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = OrthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        g.transform.position = new Vector3(pos.x, pos.y, -10f);
        return cam;
    }

    static void MakeEventSystem()
    {
        var es = new GameObject("EventSystem", typeof(EventSystem));
        var mod = es.AddComponent<InputSystemUIInputModule>();
        mod.AssignDefaultActions();
    }

    static void MakeSystemsInstance(string music)
    {
        var s = (GameObject)PrefabUtility.InstantiatePrefab(pSystems);
        var ga = s.GetComponent<GameAudio>();
        ga.music = Sfx("music_" + music);
        ga.musicVolume = music == "menu" ? 0.45f : 0.35f;
    }

    static Tilemap MakeTilemap(Transform grid, string name, bool oneWay)
    {
        var g = Go(name, grid);
        g.layer = groundLayer;
        g.tag = "Ground";
        var tm = g.AddComponent<Tilemap>();
        var tr = g.AddComponent<TilemapRenderer>();
        tr.sortingOrder = oneWay ? -1 : 0;
        if (spriteMat != null) tr.sharedMaterial = spriteMat;
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

    // Принудительно строим коллайдеры после расстановки тайлов (иначе Composite остаётся пустым).
    static void FinishTilemap(Tilemap tm)
    {
        tm.CompressBounds();
        var tc = tm.GetComponent<TilemapCollider2D>();
        tc.ProcessTilemapChanges();
        var comp = tm.GetComponent<CompositeCollider2D>();
        comp.GenerateGeometry();
        EditorUtility.SetDirty(tm.gameObject);
    }

    // Фон подземелья: кирпичная стена, арки, колонны, знамёна, цепи (с параллаксом).
    static void MakeBackground(Transform parent, float x0, float x1, float groundY, float topY, Func<int, int, bool> free)
    {
        float w = x1 - x0, cx = (x0 + x1) / 2f;
        var wall = Go("BG_Wall", parent); var wsr = AddSR(wall, Spr("Background/BG_Wall"), -50, new Color(0.8f, 0.78f, 0.92f));
        wsr.drawMode = SpriteDrawMode.Tiled; wsr.size = new Vector2(w + 60f, topY - groundY + 40f);
        wall.transform.position = new Vector3(cx, (groundY + topY) / 2f, 0);
        wall.AddComponent<Parallax>().factor = 0.15f;

        var arches = Go("BG_Arches", parent); var asr = AddSR(arches, Spr("Background/BG_Arches"), -40, new Color(0.85f, 0.85f, 1f));
        asr.drawMode = SpriteDrawMode.Tiled; asr.size = new Vector2(w + 40f, 6f);
        arches.transform.position = new Vector3(cx, groundY + 3f, 0);
        arches.AddComponent<Parallax>().factor = 0.35f;

        var deco = Go("Decorations", parent);
        for (float x = x0 + 6f; x < x1 - 2f; x += 12f)
        {
            int ix = Mathf.FloorToInt(x);
            if (free(ix, (int)groundY) && free(ix, (int)groundY + 3) && !free(ix, (int)groundY - 1))
            {
                var p = Go("Pillar", deco.transform); AddSR(p, Spr("Background/Pillar"), -20, new Color(0.75f, 0.75f, 0.85f));
                p.transform.position = new Vector3(x, groundY, 0);
            }
            int bx = Mathf.FloorToInt(x + 6f);
            if (free(bx, (int)groundY + 6) && free(bx, (int)groundY + 7) && bx < x1 - 2)
            {
                var b = Go("Banner", deco.transform); AddSR(b, Spr("Background/Banner"), -19, new Color(0.85f, 0.8f, 0.85f));
                b.transform.position = new Vector3(x + 6f, groundY + 7.5f, 0);
            }
            if (free(ix + 3, (int)topY - 1) && free(ix + 3, (int)topY - 2))
            {
                var c = Go("Chain", deco.transform); AddSR(c, Spr("Background/Chain"), -19, new Color(0.7f, 0.7f, 0.8f));
                c.transform.position = new Vector3(x + 3f, topY - 1.25f, 0);
            }
        }
    }

    static void AddDust(string sprite, int count, Vector2 vel, Color[] colors, float scale = 1f, bool spin = false)
    {
        var d = new GameObject("Dust").AddComponent<FloatingDust>();
        d.sprite = Spr(sprite);
        d.count = count;
        d.area = new Vector2(36f, 18f);
        d.velocity = vel;
        d.colors = colors;
        d.scale = scale;
        d.spin = spin;
    }

    static void BuildLevel(string name)
    {
        // --- читаем текстовую карту уровня
        string[] lines = File.ReadAllLines(Root + "/Levels/" + name + ".txt");
        var header = new Dictionary<string, string>();
        var map = new List<string>();
        var signs = new Dictionary<char, string>();
        int mode = 0;
        foreach (string raw in lines)
        {
            string l = raw.TrimEnd('\r');
            if (l == "===") { mode = 1; continue; }
            if (l == "---") { mode = 2; continue; }
            if (mode == 0 && l.Contains("=")) { var kv = l.Split(new[] { '=' }, 2); header[kv[0]] = kv[1]; }
            else if (mode == 1) map.Add(l);
            else if (mode == 2 && l.Length > 2) signs[l[0]] = l.Substring(2);
        }
        int H = map.Count, W = map.Max(r => r.Length);
        char At(int x, int y) => (y < 0 || y >= H || x < 0 || x >= map[H - 1 - y].Length) ? (y < 0 ? '.' : '#') : map[H - 1 - y][x];
        bool Solid(int x, int y) => At(x, y) == '#';
        bool Free(int x, int y) => At(x, y) != '#' && At(x, y) != '=';

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = MakeCamera(new Vector3(10, 7, 0));
        MakeEventSystem();
        MakeSystemsInstance(header["music"]);

        var level = Go("Level");
        var grid = Go("Grid", level.transform).AddComponent<Grid>();
        var solid = MakeTilemap(grid.transform, "Stone", false);
        var ledges = MakeTilemap(grid.transform, "Ledges", true);
        var actors = Go("Actors").transform;
        var pickups = Go("Pickups").transform;
        var deco = Go("Decor", level.transform).transform;
        GameObject player = null;
        Vector3 bossPos = Vector3.zero;
        bool hasBoss = false;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                char ch = At(x, y);
                int v = (x * 7 + y * 13) % 3;
                Vector3 cell = new Vector3(x + 0.5f, y + 0.5f, 0);
                switch (ch)
                {
                    case '#': solid.SetTile(new Vector3Int(x, y, 0), Solid(x, y + 1) ? tInner[v] : tTop[v]); break;
                    case '=': ledges.SetTile(new Vector3Int(x, y, 0), tLedge); break;
                    case 'P': player = Place(pPlayer, new Vector3(x + 0.5f, y, 0)); break;
                    case 'c': Place(pCrystal, cell, pickups); break;
                    case '+': Place(pHeart, cell, pickups); break;
                    case 'T': Place(pTorch, cell, deco); break;
                    case 'k':
                        var sk = Go("Skull", deco); AddSR(sk, Spr("Background/Skull"), -2); sk.transform.position = new Vector3(x + 0.5f, y + 0.31f, 0);
                        break;
                    case 'E':
                        var portal = Place(pPortal, new Vector3(x + 0.5f, y, 0), actors).GetComponent<ExitPortal>();
                        portal.requiredCrystals = int.Parse(header["required"]);
                        portal.nextScene = header["next"];
                        break;
                    case 's':
                    {
                        int l = x, r = x;
                        while (Free(l - 1, y) && At(l - 1, y) != '^' && !Free(l - 1, y - 1)) l--;
                        while (Free(r + 1, y) && At(r + 1, y) != '^' && !Free(r + 1, y - 1)) r++;
                        var s = Place(pSlime, new Vector3(x + 0.5f, y + 0.44f, 0), actors).GetComponent<SlimeEnemy>();
                        s.leftX = l + 0.5f; s.rightX = r + 0.5f;
                        break;
                    }
                    case 'b':
                    {
                        int l = x, r = x;
                        while (l > x - 5 && Free(l - 1, y) && Free(l - 1, y + 1) && Free(l - 1, y - 1)) l--;
                        while (r < x + 5 && Free(r + 1, y) && Free(r + 1, y + 1) && Free(r + 1, y - 1)) r++;
                        var b = Place(pBat, cell, actors).GetComponent<BatEnemy>();
                        b.leftX = l + 0.5f; b.rightX = r + 0.5f;
                        break;
                    }
                    case 'M':
                    case 'L':
                    {
                        var p = Place(pPlatform, new Vector3(x + 1.5f, y + 0.375f, 0), actors);
                        p.GetComponent<Animator>().runtimeAnimatorController = ch == 'M' ? cPlatH : cPlatV;
                        p.name = ch == 'M' ? "MovingPlatform" : "Lift";
                        break;
                    }
                    case 'r':
                    {
                        var sp = Go("RockSpawner", actors).AddComponent<Spawner>();
                        sp.transform.position = cell;
                        sp.prefabs = new Hazard[] { pRock.GetComponent<Hazard>() };
                        sp.firstDelay = 0.5f; sp.interval = 1.1f; sp.halfWidth = 3.5f;
                        break;
                    }
                    case 'B': hasBoss = true; bossPos = cell; break;
                    case '^':
                        if (At(x - 1, y) != '^')
                        {
                            int n = 1;
                            while (At(x + n, y) == '^') n++;
                            var sp = Place(pSpikes, new Vector3(x + n / 2f, y + 0.5f, 0), actors);
                            sp.GetComponent<SpriteRenderer>().size = new Vector2(n, 1);
                            var bc = sp.GetComponent<BoxCollider2D>(); bc.size = new Vector2(n - 0.25f, 0.45f);
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
        MakeBackground(level.transform, 0, W, 2, H - 1, Free);
        AddDust("Background/Dust", 35, new Vector2(0.25f, 0.35f), new[] { new Color(1f, 0.85f, 0.6f, 0.35f), new Color(0.6f, 0.9f, 1f, 0.3f) });

        // HUD
        var hudGo = (GameObject)PrefabUtility.InstantiatePrefab(pHUD);
        var hud = hudGo.GetComponent<HUD>();
        hud.levelTitle = header["title"];
        hud.showCrystals = !hasBoss;

        if (hasBoss)
        {
            var b = Place(pBoss, bossPos, actors).GetComponent<Boss>();
            b.arenaLeft = 1f; b.arenaRight = W - 1f; b.floorY = 2f; b.ceilingY = H - 3f; b.winScene = header["next"];
        }

        // Камера сбоку следует за героем
        var follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = player.transform;
        follow.levelMinX = 0f; follow.levelMaxX = W;
        follow.minY = OrthoSize - 0.5f; follow.maxY = H - OrthoSize;
        player.GetComponent<Player2D>().deathY = -3f;
        cam.transform.position = new Vector3(Mathf.Max(player.transform.position.x, 12f), OrthoSize - 0.5f, -10f);

        EditorSceneManager.SaveScene(scene, ScenesDir + "/" + name + ".unity");
    }

    static void MakeSign(Transform parent, string text, Vector3 pos)
    {
        var g = Go("Sign", parent);
        g.transform.position = pos;
        float w = text.Length * 0.36f + 0.9f;
        var plate = Go("Plate", g.transform); var psr = AddSR(plate, Spr("UI/UI_Panel"), -4, new Color(1, 1, 1, 0.92f));
        psr.drawMode = SpriteDrawMode.Sliced; psr.size = new Vector2(w, 1.1f);
        var tg = Go("Text", g.transform);
        var t = tg.AddComponent<TextMeshPro>();
        t.font = font; t.text = text; t.fontSize = 3.4f; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 1f; t.fontSizeMax = 3.4f;
        t.color = new Color32(254, 231, 97, 255);
        t.rectTransform.sizeDelta = new Vector2(w, 1.1f);
        t.sortingOrder = -3;
    }

    // ------------------------------------------------------------------ MAIN MENU
    static void BuildMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Vector3(0, 6.5f, 0));
        MakeEventSystem();
        MakeSystemsInstance("menu");
        Diorama(out var heroGo);
        heroGo.transform.position = new Vector3(-6.5f, 2f, 0);

        var kingGo = Go("SlimeKing"); AddSR(kingGo, Frames("Boss/Boss_Idle")[0], 6); AddAnim(kingGo, cBossCtrl);
        kingGo.transform.position = new Vector3(8f, 3.65f, 0); kingGo.GetComponent<SpriteRenderer>().flipX = false;
        var s = Place(pSlime, new Vector3(2f, 2.44f, 0)).GetComponent<SlimeEnemy>(); s.leftX = -1f; s.rightX = 4f;
        Place(pCrystal, new Vector3(-9.5f, 4f, 0)); Place(pCrystal, new Vector3(-3.5f, 4.6f, 0));

        var cv = Go("MenuCanvas"); MakeCanvas(cv, 5); cv.AddComponent<GraphicRaycaster>();
        var mb = cv.AddComponent<MenuButtons>(); mb.clickClip = Sfx("sfx_click");
        var vig = Img("Vignette", cv.transform, Spr("UI/UI_Vignette"), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero); Stretch(vig.rectTransform); vig.raycastTarget = false;

        Txt("Title", cv.transform, "DUNGEON KNIGHT", 120, new Color32(254, 174, 52, 255), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(1800, 160));
        Txt("Subtitle", cv.transform, "RISE OF THE SLIME KING", 38, new Color32(199, 253, 255, 255), new Vector2(0.5f, 1), new Vector2(0, -320), new Vector2(1600, 60));

        var play = MakeButton("PlayButton", cv.transform, "PLAY", Spr("UI/UI_Button"), new Vector2(0, -20), new Vector2(560, 140));
        UnityEventTools.AddPersistentListener(play.onClick, mb.Play);
        var quit = MakeButton("QuitButton", cv.transform, "QUIT", Spr("UI/UI_ButtonGrey"), new Vector2(0, -190), new Vector2(560, 120));
        UnityEventTools.AddPersistentListener(quit.onClick, mb.Quit);

        Txt("Hint", cv.transform, "COLLECT CRYSTALS  -  OPEN PORTALS  -  DEFEAT THE KING", 24, new Color32(192, 203, 220, 255), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1800, 40));

        EditorSceneManager.SaveScene(scene, ScenesDir + "/MainMenu.unity");
    }

    // Общая «сцена-диорама» для меню и экрана победы.
    static void Diorama(out GameObject hero)
    {
        var level = Go("Diorama");
        var grid = Go("Grid", level.transform).AddComponent<Grid>();
        var tm = MakeTilemap(grid.transform, "Floor", false);
        for (int x = -20; x < 20; x++)
        {
            tm.SetTile(new Vector3Int(x, 1, 0), tTop[(x + 30) % 3]);
            tm.SetTile(new Vector3Int(x, 0, 0), tInner[(x + 31) % 3]);
            tm.SetTile(new Vector3Int(x, -1, 0), tInner[(x + 32) % 3]);
        }
        FinishTilemap(tm);
        MakeBackground(level.transform, -20, 20, 2, 14, (x, y) => y >= 2);
        foreach (float x in new[] { -11f, -3f, 3f, 11f })
            Place(pTorch, new Vector3(x, 8f, 0));
        AddDust("Background/Dust", 30, new Vector2(0.25f, 0.35f), new[] { new Color(1f, 0.85f, 0.6f, 0.35f) });

        hero = Go("Knight"); AddSR(hero, Frames("Hero/Hero_Idle")[0], 10); AddAnim(hero, cHero);
        hero.transform.localScale = Vector3.one * 2f;
    }

    // ------------------------------------------------------------------ WIN
    static void BuildWin()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Vector3(0, 6.5f, 0));
        MakeEventSystem();
        MakeSystemsInstance("menu");
        Diorama(out var hero);
        hero.transform.position = new Vector3(0f, 2f, 0);
        var crown = Go("Crown"); crown.transform.position = new Vector3(0, 5.7f, 0); crown.transform.localScale = Vector3.one * 1.5f;
        AddAnim(crown, Ctrls["Heart"]); // покачивание: тот же клип, что у сердца (двигает дочерний «Sprite»)
        AddSR(Go("Sprite", crown.transform), Spr("UI/UI_Crown"), 11);
        for (int i = 0; i < 5; i++)
            Place(pCrystal, new Vector3(-8f + i * 4f, 4.2f + (i % 2) * 0.8f, 0));

        AddDust("FX/Confetti", 90, new Vector2(0.6f, -1.6f), new Color[]
        {
            new Color32(254, 174, 52, 255), new Color32(44, 232, 245, 255), new Color32(228, 59, 68, 255),
            new Color32(99, 199, 77, 255), new Color32(181, 80, 136, 255)
        }, 1.6f, true);

        var cv = Go("WinCanvas"); MakeCanvas(cv, 5); cv.AddComponent<GraphicRaycaster>();
        var mb = cv.AddComponent<MenuButtons>(); mb.clickClip = Sfx("sfx_click"); mb.startClip = Sfx("sfx_win");
        var vig = Img("Vignette", cv.transform, Spr("UI/UI_Vignette"), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero); Stretch(vig.rectTransform); vig.raycastTarget = false;
        Txt("Title", cv.transform, "YOU WIN!", 140, new Color32(254, 231, 97, 255), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(1800, 170));
        Txt("Subtitle", cv.transform, "THE SLIME KING IS DEFEATED", 40, new Color32(199, 253, 255, 255), new Vector2(0.5f, 1), new Vector2(0, -320), new Vector2(1700, 60));
        var menu = MakeButton("MenuButton", cv.transform, "MAIN MENU", Spr("UI/UI_Button"), new Vector2(0, -330), new Vector2(620, 140));
        UnityEventTools.AddPersistentListener(menu.onClick, mb.BackToMenu);
        Txt("Thanks", cv.transform, "THANKS FOR PLAYING!", 26, new Color32(192, 203, 220, 255), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(1600, 40));

        EditorSceneManager.SaveScene(scene, ScenesDir + "/Win.unity");
    }

    // ------------------------------------------------------------------ PROJECT SETTINGS
    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = SceneOrder.Select(s => new EditorBuildSettingsScene(ScenesDir + "/" + s + ".unity", true)).ToArray();

        PlayerSettings.productName = "Dungeon Knight";
        PlayerSettings.companyName = "Student";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.student.dungeonknight");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.student.dungeonknight");

        // Иконка и Unity Remote — через reflection, чтобы не зависеть от версии API.
        try
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/UI/AppIcon.png");
            var m = typeof(PlayerSettings).GetMethod("SetIconsForTargetGroup", new[] { typeof(BuildTargetGroup), typeof(Texture2D[]) });
            if (icon != null && m != null) m.Invoke(null, new object[] { BuildTargetGroup.Unknown, new[] { icon } });
        }
        catch (Exception e) { Debug.LogWarning("Icon: " + e.Message); }

        try
        {
            var p = typeof(EditorSettings).GetProperty("unityRemoteDevice");
            if (p != null) p.SetValue(null, "Any iOS Device");
            // Качество картинки в Unity Remote: без уменьшения и без JPEG-сжатия.
            var res = typeof(EditorSettings).GetProperty("unityRemoteResolution");
            if (res != null) res.SetValue(null, "Normal");
            var comp = typeof(EditorSettings).GetProperty("unityRemoteCompression");
            if (comp != null) comp.SetValue(null, "PNG");
        }
        catch (Exception e) { Debug.LogWarning("Unity Remote setting: " + e.Message); }
    }
}
