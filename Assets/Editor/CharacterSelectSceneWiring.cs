#if UNITY_EDITOR
using DiskGolf.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiskGolf.EditorTools
{
    public static class CharacterSelectSceneWiring
    {
        [MenuItem("Disk Golf/Wire Character Select Scene")]
        public static void WireOpenScene()
        {
            var screen = Object.FindFirstObjectByType<CharacterSelectScreen>();
            if (screen == null)
            {
                Debug.LogWarning("[Disk Golf] CharacterSelectScreen not found in the open scene.");
                return;
            }

            TextMeshProUGUI label = null;
            foreach (var tmp in screen.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.gameObject.name == "P1Label")
                {
                    label = tmp;
                    break;
                }
            }

            var so = new SerializedObject(screen);
            var prop = so.FindProperty("selectedPlayerLabel");
            if (prop != null && label != null)
            {
                prop.objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Disk Golf] Character select scene references wired.");
        }
    }
}
#endif
