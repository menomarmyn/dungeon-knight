using System.Collections.Generic;

// Всё, что сохраняется на телефоне. JsonUtility понимает поля-списки (List<int>),
// но не Dictionary — поэтому открытые уровни хранятся списком номеров.
// Значения после «=» — это значения по умолчанию для новой игры или битого файла.
[System.Serializable]
public class SaveData
{
    public int version = SaveSystem.CurrentVersion;
    public int bestScore = 0;
    public List<int> unlockedLevels = new List<int> { 1 };   // номера уровней с 1
    public List<int> completedLevels = new List<int>();
    public float musicVolume = 0.8f;                         // ползунок 0..1 (в микшер идёт в dB)
    public float sfxVolume = 0.8f;
}
