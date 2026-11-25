using UnityEngine;

// Один уровень — один ассет. Сцена Game читает его и строит уровень (LevelLoader).
// Карта: каждая буква — клетка 1×1. Легенда:
//   #  камень        =  уступ (снизу можно запрыгнуть)   ^  шипы
//   P  старт героя   E  портал-выход   c  кристалл   +  сердце   T  факел   k  череп
//   s  слизень       b  летучая мышь   a  гоблин-лучник   B  босс   r  падающие камни
//   M  платформа ↔   L  лифт ↕         1..9  табличка с текстом из signs
[CreateAssetMenu(menuName = "Dungeon Knight/Level Data", fileName = "Level_01")]
public class LevelData : ScriptableObject
{
    public string title;
    [Tooltip("Сколько кристаллов нужно, чтобы открыть портал (0 — уровень с боссом).")]
    public int requiredCrystals;
    [Tooltip("Лимит времени в секундах. Время вышло — попытка проиграна. 0 — без лимита.")]
    public float timeLimit;
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume;

    [TextArea(18, 40)] public string map;
    [Tooltip("Таблички: цифра на карте и текст, например «1:MOVE: JOYSTICK / A D».")]
    public string[] signs;
}
