using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Utility
{

    public static class StyleExtensions
    {

        /// <summary>Sets border width.</summary>
        /// <remarks>Use <see cref="float.NaN"/> for auto.</remarks>
        public static void SetBorderWidth(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.borderRightWidth = GetLengthF(right);
            if (bottom.HasValue) style.borderBottomWidth = GetLengthF(bottom);
            if (left.HasValue) style.borderLeftWidth = GetLengthF(left);
            if (top.HasValue) style.borderTopWidth = GetLengthF(top);

        }

        /// <summary>Sets border color.</summary>
        public static void SetBorderColor(this IStyle style, Color? all = null, Color? vertical = null, Color? horizontal = null, Color? left = null, Color? right = null, Color? top = null, Color? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.borderRightColor = right.Value;
            if (bottom.HasValue) style.borderBottomColor = bottom.Value;
            if (left.HasValue) style.borderLeftColor = left.Value;
            if (top.HasValue) style.borderTopColor = top.Value;

        }

        /// <summary>Sets margin.</summary>
        /// <remarks>Use <see cref="float.NaN"/> for auto.</remarks>
        public static void SetMargin(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.marginRight = GetLength(right);
            if (bottom.HasValue) style.marginBottom = GetLength(bottom);
            if (left.HasValue) style.marginLeft = GetLength(left);
            if (top.HasValue) style.marginTop = GetLength(top);

        }

        /// <summary>Sets padding.</summary>
        /// <remarks>Use <see cref="float.NaN"/> for auto.</remarks>
        public static void SetPadding(this IStyle style, float? all = null, float? vertical = null, float? horizontal = null, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {

            if (!left.HasValue) left = horizontal ?? all;
            if (!right.HasValue) right = horizontal ?? all;
            if (!top.HasValue) top = vertical ?? all;
            if (!bottom.HasValue) bottom = vertical ?? all;

            if (right.HasValue) style.paddingRight = GetLength(right);
            if (bottom.HasValue) style.paddingBottom = GetLength(bottom);
            if (left.HasValue) style.paddingLeft = GetLength(left);
            if (top.HasValue) style.paddingTop = GetLength(top);

        }

        static StyleLength GetLength(float? value) =>
            float.IsNaN(value.Value) ? (StyleLength)StyleKeyword.Auto : (StyleLength)value.Value;

        static StyleFloat GetLengthF(float? value) =>
            float.IsNaN(value.Value) ? (StyleFloat)StyleKeyword.Auto : (StyleFloat)value.Value;

    }

}
