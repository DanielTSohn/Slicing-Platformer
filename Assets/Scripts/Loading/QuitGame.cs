using UnityEditor;
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public static void CallQuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
    }
}