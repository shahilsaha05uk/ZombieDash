#pragma warning disable IDE0051 // Remove unused private members

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    public class OnGUIPrompt : EditorWindow
    {

        /// <inheritdoc cref="Prompt(GUIContent, Action, out Action{bool}, Action, Action, bool, bool, string, RectOffset)"/>
        public static bool Prompt(GUIContent title, Action onGUI, Action onEnable = null, Action onDisable = null, Action onFocus = null, Action onLostFocus = null, bool drawDefaultFooter = true, bool show = true, string acceptButton = "Continue", RectOffset margin = default, Vector2? size = null) =>
            Prompt(title, onGUI, out _, out _, onEnable, onDisable, onFocus, onLostFocus, drawDefaultFooter, show, acceptButton, margin, size);

        /// <summary>Shows an editor window with the specified OnGUI callback.</summary>
        /// <param name="show">Determines whatever we should show window. Set to <see langword="false"/> to show manually.</param>
        /// <param name="setCanContinue">Sets whatever <paramref name="acceptButton"/> is enabled .You probably want to declare a variable before calling this and set that as out variable.</param>
        /// <param name="setResult">Sets the result and closes this prompt. Use in combination with <paramref name="drawDefaultFooter"/>: <see langword="false"/>. You probably want to declare a variable before calling this and set that as out variable.</param>
        /// <returns><see langword="true"/> if user pressed <paramref name="acceptButton"/>. No effect if <paramref name="drawDefaultFooter"/> is <see langword="false"/>.</returns>
        public static bool Prompt(GUIContent title, Action onGUI, out Action<bool> setResult, out Action<bool> setCanContinue, Action onEnable = null, Action onDisable = null, Action onFocus = null, Action onLostFocus = null, bool drawDefaultFooter = true, bool show = true, string acceptButton = "Continue", RectOffset margin = default, Vector2? size = null, Action<EditorWindow> setShow = null)
        {

            var w = CreateInstance<OnGUIPrompt>();

            w.titleContent = title;
            w.onGUI = onGUI;
            w.onEnable = onEnable;
            w.onDisable = onDisable;
            w.onFocus = onFocus;
            w.onLostFocus = onLostFocus;
            w.drawDefaultFooter = drawDefaultFooter;
            w.acceptButton = acceptButton;
            w.margin = margin ?? new RectOffset(22, 22, 22, 22);
            w.size = size;

            setResult = result =>
            {
                w.result = result;
                w.Close();
            };

            setCanContinue = canContinue =>
                w.canContinue = canContinue;

            if (show)
                w.ShowModal();
            else
                setShow?.Invoke(w);

            return w.result;

        }

        OnGUIPrompt()
        { }

        Action onGUI;
        Action onEnable;
        Action onDisable;
        Action onFocus;
        Action onLostFocus;
        bool drawDefaultFooter;
        string acceptButton;
        RectOffset margin;
        bool canContinue;

        bool hasSetSize;
        Vector2? size;

        bool result;

        GUIStyle style;

        Vector2 scroll;
        void OnGUI()
        {

            if (!hasSetSize)
                position = new Rect(position.position, size ?? position.size);
            hasSetSize = true;

            if (style == null)
                style = new GUIStyle() { margin = margin };

            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.BeginVertical(style);

            onGUI?.Invoke();
            if (drawDefaultFooter)
            {
                EditorGUILayout.Space();
                GUILayout.FlexibleSpace();
                Footer();
            }

            if (Event.current.type == EventType.MouseDown)
            {
                GUI.FocusControl("");
                Repaint();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

        }

        void Footer()
        {

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                result = false;
                Close();
            }

            GUILayout.FlexibleSpace();
            GUI.enabled = canContinue;
            if (GUILayout.Button(acceptButton))
            {
                result = true;
                Close();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

        }

        void OnEnable()
        {
            position = new Rect(position.position, size ?? position.size);
            onEnable?.Invoke();
        }

        void OnDisable() => onDisable?.Invoke();
        void OnFocus() => onFocus?.Invoke();
        void OnLostFocus() => onLostFocus?.Invoke();

    }

}
#endif
