using UnityEditor;
using UnityEditor.SceneManagement;


namespace ParkingCat
{
    [InitializeOnLoad]
    public class SceneBootstrapper
    {
        private const string k_PreviousScene = "PreviousScene";
        private const string k_ShouldLoadBootstrap = "LoadBootstrapScene";

        private const string k_LoadBootstrapMenu = "Tools/Load Bootstrap Scene On Play";
        private const string k_DontLoadBootstrapMenu = "Tools/Don't Load Bootstrap Scene On Play";

        private static string BootstrapScene => EditorBuildSettings.scenes[0].path;

        private static string PreviousScene
        {
            get => EditorPrefs.GetString(k_PreviousScene);
            set => EditorPrefs.SetString(k_PreviousScene, value);
        }

        private static bool ShouldLoadBootstrapScene
        {
            get => EditorPrefs.GetBool(k_ShouldLoadBootstrap, true);
            set => EditorPrefs.SetBool(k_ShouldLoadBootstrap, value);
        }

        static SceneBootstrapper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (!ShouldLoadBootstrapScene) { return; }

            switch (playModeStateChange)
            {
                case PlayModeStateChange.ExitingEditMode:
                    PreviousScene = EditorSceneManager.GetActiveScene().path;

                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() && IsSceneInBuildSettings(BootstrapScene))
                    {
                        EditorSceneManager.OpenScene(BootstrapScene);
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:

                    if (!string.IsNullOrEmpty(PreviousScene))
                    {
                        EditorSceneManager.OpenScene(PreviousScene);
                    }
                    break;
            }
        }

        [MenuItem(k_LoadBootstrapMenu)]
        private static void LoadBootstrapScene()
        {
            ShouldLoadBootstrapScene = true;
        }

        [MenuItem(k_LoadBootstrapMenu, true)]
        private static bool ValidateLoadBootstrapScene()
        {
            return !ShouldLoadBootstrapScene;
        }

        [MenuItem(k_DontLoadBootstrapMenu)]
        private static void DontLoadBootstrapScene()
        {
            ShouldLoadBootstrapScene = false;
        }
        
        [MenuItem(k_DontLoadBootstrapMenu, true)]
        private static bool ValidateDontLoadBootstrapScene()
        {
            return ShouldLoadBootstrapScene;
        }

        static bool IsSceneInBuildSettings(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath) &&
                   System.Array.Exists(EditorBuildSettings.scenes, scene => scene.path == scenePath);
        }
    }
}