using UnityEngine;

// Список уровней по порядку + палитра (префабы и тайлы для постройки уровня).
// Лежит в Resources, чтобы меню и сцена Game находили его по имени.
[CreateAssetMenu(menuName = "Dungeon Knight/Level Catalog", fileName = "LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    public LevelData[] levels;
    public LevelPalette palette;

    static LevelCatalog instance;
    public static LevelCatalog Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<LevelCatalog>("LevelCatalog");
            return instance;
        }
    }
}
