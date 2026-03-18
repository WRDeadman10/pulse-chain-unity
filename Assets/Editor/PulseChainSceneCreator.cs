using System.Collections.Generic;

using PulseChain.Core;
using PulseChain.Gameplay;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PulseChain.Editor {
    public static class PulseChainSceneCreator {
        private const string ScenePath = "Assets/Scenes/PulseChain.unity";

        [MenuItem("Pulse Chain/Create Gameplay Scene")]
        public static void CreateGameplaySceneMenu() {
            CreateGameplayScene();
        }

        public static void CreateGameplayScene() {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateRootObject<Camera>("Main Camera");
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.09f, 0.16f, 1.0f);
            camera.orthographic = true;
            camera.orthographicSize = 9.6f;
            camera.nearClipPlane = -10.0f;
            camera.farClipPlane = 10.0f;
            camera.transform.position = new Vector3(0.0f, 0.0f, -5.0f);

            CreateRootObject<GameManager>("GameManager");
            CreateRootObject<NodeSpawner>("NodeSpawner");
            CreateRootObject<PulseController>("PulseController");
            CreateRootObject<UIManager>("UIManager");
            CreateRootObject<AudioSystem>("AudioSystem");
            CreateRootObject<PersistenceSystem>("PersistenceSystem");
            CreateRootObject<AdHooksSystem>("AdHooksSystem");

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void CreateGameplaySceneBatch() {
            CreateGameplayScene();
            EditorApplication.Exit(0);
        }

        private static T CreateRootObject<T>(string objectName) where T : Component {
            GameObject rootObject = new GameObject(objectName);
            T component = rootObject.AddComponent<T>();
            return component;
        }

        private static void UpdateBuildSettings() {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            List<EditorBuildSettingsScene> updatedScenes = new List<EditorBuildSettingsScene>(existingScenes.Length + 1);
            bool pulseChainSceneFound = false;

            for (int i = 0; i < existingScenes.Length; i++) {
                EditorBuildSettingsScene existingScene = existingScenes[i];
                if (existingScene.path == ScenePath) {
                    pulseChainSceneFound = true;
                    updatedScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                } else {
                    updatedScenes.Add(existingScene);
                }
            }

            if (!pulseChainSceneFound) {
                updatedScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = updatedScenes.ToArray();
        }
    }
}