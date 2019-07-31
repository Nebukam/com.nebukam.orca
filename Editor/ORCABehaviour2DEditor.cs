using UnityEditor;

namespace Nebukam.ORCA.Ed
{

    [CustomEditor(typeof(ORCAAgentComponent2D))]
    [CanEditMultipleObjects]
    public class ORCABehaviour2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(ORCAEditorConstants.ORCABehaviorNotes, MessageType.Warning);
            base.OnInspectorGUI();
        }
    }

}