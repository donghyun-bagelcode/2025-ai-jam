using UnityEngine;

public class ScreenFlow : MonoBehaviour
{
    public static ScreenFlow Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
}