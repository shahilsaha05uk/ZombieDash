using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RadialIndicatorController))]
public class RadialIndicatorInspector : IndicatorInspector {

	public bool config = true;
	public bool configBase = false;
	public bool configNeedle = false;
	public bool configCenter = false;
	public bool configMarker = false;
	public bool configNick = false;
	public bool set = true;

	public override void OnInspectorGUI()
	{
		RadialIndicatorController controller = (RadialIndicatorController) target;

		GUI.changed = false;

		config = EditorGUILayout.Foldout (config, "Setup");
		if (config)
		{
			Color color = GUI.color;
			GUI.color = Color.white;
			float newBase = (float) EditorGUILayout.IntSlider("Base (degrees)", (int)controller.Base, 0, 360);
			if(newBase != controller.Base)
			{
				controller.Base = newBase;
			}
			float newRange = (float) EditorGUILayout.IntSlider("Range (degrees)", (int)controller.Range, 0, 360);
			if(newRange != controller.Range)
			{
				controller.Range = newRange;
			}
			RadialIndicatorController.RadialIndicatorTurn newTurn = (RadialIndicatorController.RadialIndicatorTurn) EditorGUILayout.EnumPopup("Turn", controller.Turn);
			if(controller.Turn != newTurn)
			{
				controller.Turn = newTurn;
			}
			IndicatorController.IndicatorVelocity newVelocity = (IndicatorController.IndicatorVelocity) EditorGUILayout.EnumPopup("Needle Velocity", controller.Velocity);
			if(controller.Velocity != newVelocity)
			{
				controller.Velocity = newVelocity;
			}
			GUI.color = color;

			if(controller.Range == 0F)
			{
				EditorGUILayout.HelpBox("Range is zero", MessageType.Warning);
			}

			Sprite newSprite = null;
			Color newColor = Color.clear;

			configBase = EditorGUILayout.Foldout (configBase, "Base");
			if (configBase) {
				EditorGUILayout.BeginHorizontal ();
				newSprite = (Sprite)EditorGUILayout.ObjectField ("", controller.SpriteBase, typeof(Sprite));
				if (newSprite != controller.SpriteBase) {
					controller.SpriteBase = newSprite;
				}
				newColor = EditorGUILayout.ColorField ("", controller.ColorBase);
				if (newColor != controller.ColorBase) {
					controller.ColorBase = newColor;
				}
				EditorGUILayout.EndHorizontal ();
			}

			configNeedle = EditorGUILayout.Foldout (configNeedle, "Needle");
			if (configNeedle) {
				EditorGUILayout.BeginHorizontal ();
				newSprite = (Sprite)EditorGUILayout.ObjectField ("", controller.SpriteNeedle, typeof(Sprite));
				if (newSprite != controller.SpriteNeedle) {
					controller.SpriteNeedle = newSprite;
				}
				newColor = EditorGUILayout.ColorField ("", controller.ColorNeedle);
				if (newColor != controller.ColorNeedle)
				{
					controller.ColorNeedle = newColor;
				}
				EditorGUILayout.EndHorizontal ();
			}

			configCenter = EditorGUILayout.Foldout (configCenter, "Center");
			if (configCenter) {
				EditorGUILayout.BeginHorizontal ();
				newSprite = (Sprite)EditorGUILayout.ObjectField ("", controller.SpriteBaseNeedle, typeof(Sprite));
				if (newSprite != controller.SpriteBaseNeedle) {
					controller.SpriteBaseNeedle = newSprite;
				}
				newColor = EditorGUILayout.ColorField ("", controller.ColorBaseNeedle);
				if (newColor != controller.ColorBaseNeedle) {
					controller.ColorBaseNeedle = newColor;
				}
				EditorGUILayout.EndHorizontal ();
			}

			configMarker = EditorGUILayout.Foldout (configMarker, "Marker");
			if (configMarker) {
				EditorGUILayout.BeginHorizontal ();
				newSprite = (Sprite)EditorGUILayout.ObjectField ("", controller.SpriteAlarm, typeof(Sprite));
				if (newSprite != controller.SpriteAlarm) {
					controller.SpriteAlarm = newSprite;
				}
				newColor = EditorGUILayout.ColorField ("", controller.ColorAlarm);
				if (newColor != controller.ColorAlarm) {
					controller.ColorAlarm = newColor;
				}
				EditorGUILayout.EndHorizontal ();
			}

			configNick = EditorGUILayout.Foldout (configNick, "Nick");
			if (configNick) {
				EditorGUILayout.BeginHorizontal ();
				newSprite = (Sprite)EditorGUILayout.ObjectField ("", controller.SpriteNick, typeof(Sprite));
				if (newSprite != controller.SpriteNick) {
					controller.SpriteNick = newSprite;
				}
				newColor = EditorGUILayout.ColorField ("", controller.ColorNick);
				if (newColor != controller.ColorNick) {
					controller.ColorNick = newColor;
				}
				EditorGUILayout.EndHorizontal ();
			}
		}

		set = EditorGUILayout.Foldout (set, "Set");
		if (set)
		{
			controller.Percent = EditorGUILayout.Slider("Value (0 : 1)", controller.Percent, 0F, 1F);
			//	GUI.enabled = !controller.isRun;
			//	if (GUILayout.Button ("Shake")) {
			//		controller.Shake ();
			//	}
			//	GUI.enabled = true;
		}
			
		base.OnInspectorGUI();

//		DrawDefaultInspector();

		if (GUI.changed)
			EditorUtility.SetDirty(controller);
		
	}

}