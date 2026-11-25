using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// Из чего строится уровень: префабы, тайлы и спрайты фона. Это не баланс, а «кубики» —
// ассет заполняет сборщик Midterm → Build Everything.
public class LevelPalette : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject player;
    public GameObject crystal, heart, torch, portal;
    public GameObject slime, bat, goblin, boss;
    public GameObject spikes, platform;
    public Hazard rock;

    [Header("Tiles")]
    public TileBase[] topTiles;
    public TileBase[] innerTiles;
    public TileBase ledgeTile;
    public Material spriteMaterial;

    [Header("Moving platforms")]
    public RuntimeAnimatorController platformHorizontal;
    public RuntimeAnimatorController platformVertical;

    [Header("Decor")]
    public Sprite skull, wall, arches, pillar, banner, chain, dust, signPlate;
    public TMP_FontAsset signFont;

    [Header("Data")]
    public ItemValues items;
    public AudioClip rockWarnClip;
}
