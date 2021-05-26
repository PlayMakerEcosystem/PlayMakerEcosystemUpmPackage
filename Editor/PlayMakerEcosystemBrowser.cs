using UnityEngine;
using UnityEditor;

namespace com.hutonggames.playmakereditor.addons.ecosystem
{
    public class PlayMakerEcosystemBrowser : EditorWindow
	{
        [MenuItem("PlayMaker/Addons/Ecosystem")]
        private static void InitWindow()
        {
            Debug.Log("Hello");
            PlayMakerEcosystemBrowser window = (PlayMakerEcosystemBrowser)EditorWindow.GetWindow(typeof(PlayMakerEcosystemBrowser));
            window.titleContent = new GUIContent("Ecosystem");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Welcome", EditorStyles.boldLabel);

        }
    }
}
