using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class OpenFinalDemoSceneOnLoad
{
    private const string TargetScenePath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";
    private const string SessionKey = "VehicleCollisionSim.OpenedFinalDemoScene";

    [InitializeOnLoadMethod]
    private static void OpenFinalDemoSceneOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == TargetScenePath)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            if (activeScene.isDirty)
            {
                return;
            }

            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            SessionState.SetBool(SessionKey, true);
        };
    }
}
