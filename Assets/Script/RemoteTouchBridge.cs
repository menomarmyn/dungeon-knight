#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Только для демо через Unity Remote 5 в редакторе (в билд не попадает — #if UNITY_EDITOR).
// Unity Remote присылает касания iPhone в старый Input (Input.touches), а новый Input System их не видит.
// Этот мост создаёт виртуальный Touchscreen и пересылает в него каждое касание,
// поэтому экранный джойстик и кнопки (On-Screen Stick / Button) начинают работать с телефона.
public class RemoteTouchBridge : MonoBehaviour
{
    private Touchscreen screen;

    // Создаётся автоматически при запуске Play — в сцены добавлять не нужно.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("RemoteTouchBridge");
        DontDestroyOnLoad(go);
        go.AddComponent<RemoteTouchBridge>();
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        if (screen == null)
            screen = InputSystem.AddDevice<Touchscreen>("UnityRemoteTouch");

        for (int i = 0; i < Input.touchCount; i++)
        {
            UnityEngine.Touch t = Input.GetTouch(i);
            var state = new TouchState
            {
                touchId = t.fingerId + 1, // 0 в Input System зарезервирован
                position = t.position,
                delta = t.deltaPosition,
                pressure = 1f,
                phase = Convert(t.phase),
            };
            if (t.phase == UnityEngine.TouchPhase.Began)
                state.startPosition = t.position;
            InputSystem.QueueStateEvent(screen, state);
        }
    }

    static UnityEngine.InputSystem.TouchPhase Convert(UnityEngine.TouchPhase p)
    {
        switch (p)
        {
            case UnityEngine.TouchPhase.Began: return UnityEngine.InputSystem.TouchPhase.Began;
            case UnityEngine.TouchPhase.Moved: return UnityEngine.InputSystem.TouchPhase.Moved;
            case UnityEngine.TouchPhase.Stationary: return UnityEngine.InputSystem.TouchPhase.Stationary;
            case UnityEngine.TouchPhase.Ended: return UnityEngine.InputSystem.TouchPhase.Ended;
            default: return UnityEngine.InputSystem.TouchPhase.Canceled;
        }
    }

    void OnDestroy()
    {
        if (screen != null)
            InputSystem.RemoveDevice(screen);
    }
}
#endif
