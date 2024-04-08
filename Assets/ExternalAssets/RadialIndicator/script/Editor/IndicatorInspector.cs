using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(IndicatorController))]
public class IndicatorInspector : Editor {

	public override void OnInspectorGUI()
	{
		IndicatorController controller = (IndicatorController) target;
	}

}
