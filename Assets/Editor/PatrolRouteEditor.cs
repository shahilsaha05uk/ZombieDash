using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PatrolRoute))]
public class PatrolRouteEditor : Editor
{
    SerializedProperty sp_Route;

    private void OnEnable()
    {
        sp_Route = serializedObject.FindProperty("Route");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sp_Route, true);

        if (GUILayout.Button("Add Patrol Point"))
        {
            serializedObject.Update();
            sp_Route.arraySize++;
            serializedObject.ApplyModifiedProperties();

            // Create a new GameObject for the patrol point
            GameObject newPointObject = new GameObject("PatrolPoint" + (sp_Route.arraySize - 1));
            newPointObject.transform.parent = ((PatrolRoute)target).transform; // Set as child of the PatrolRoute object
            newPointObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            newPointObject.AddComponent<PatrolPoint>();

            // Add the new GameObject to the list
            sp_Route.GetArrayElementAtIndex(sp_Route.arraySize - 1).objectReferenceValue = newPointObject;
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Remove Patrol Point") && sp_Route.arraySize > 0)
        {
            serializedObject.Update();
            int lastIndex = sp_Route.arraySize - 1;
            GameObject removedObject = (GameObject)sp_Route.GetArrayElementAtIndex(lastIndex).objectReferenceValue;
            sp_Route.DeleteArrayElementAtIndex(lastIndex);
            serializedObject.ApplyModifiedProperties();

            // Destroy the corresponding GameObject
            if (removedObject != null)
            {
                DestroyImmediate(removedObject);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        PatrolRoute patrolRoute = (PatrolRoute)target;

        Handles.color = Color.yellow;

        for (int i = 0; i < patrolRoute.Route.Count; i++)
        {
            GameObject patrolPointObject = patrolRoute.Route[i];

            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.PositionHandle(patrolPointObject.transform.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(patrolPointObject.transform, "Move Patrol Point");
                patrolPointObject.transform.position = newPoint;
            }

            Handles.Label(patrolPointObject.transform.position, "Patrol Point " + i.ToString());
        }
    }
}