using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PackageWizard.Editor
{
    internal static class PackageWizardGUI
    {
        public class ColorScope : GUI.Scope
        {
            private readonly Color _previousColor;

            public ColorScope(Color color)
            {
                _previousColor = GUI.color;
                GUI.color = color;
            }
            
            protected override void CloseScope()
            {
                GUI.color = _previousColor;
                GUI.color = _previousColor;
            }
        }
        
        public class EnabledScope : GUI.Scope
        {
            private readonly bool _wasEnabled;

            public EnabledScope(bool enabled)
            {
                _wasEnabled = GUI.enabled;
                GUI.enabled = enabled;
            }
            
            protected override void CloseScope()
            {
                GUI.enabled = _wasEnabled;
            }
        }
        
        public static (Rect, Rect) SplitRectByWidthPercentages(Rect rect, float percentage1, float percentage2)
        {
            var rects = SplitRectByWidthPercentages(rect, new[] { percentage1, percentage2 });
            return (rects[0], rects[1]);
        }

        public static (Rect, Rect, Rect) SplitRectByWidthPercentages(Rect rect, float percentage1, float percentage2, float percentage3)
        {
            var rects = SplitRectByWidthPercentages(rect, new[] { percentage1, percentage2, percentage3 });
            return (rects[0], rects[1], rects[2]);
        }
        
        public static (Rect, Rect) SplitRectByWidthPercentagesWithSpacing(Rect rect, float elementSpacing, float percentage1, float percentage2)
        {
            var rects = SplitRectByWidthPercentagesWithSpacing(rect, elementSpacing, new[] { percentage1, percentage2 });
            return (rects[0], rects[1]);
        }

        public static (Rect, Rect, Rect) SplitRectByWidthPercentagesWithSpacing(Rect rect, float elementSpacing, float percentage1, float percentage2, float percentage3)
        {
            var rects = SplitRectByWidthPercentagesWithSpacing(rect, elementSpacing, new[] { percentage1, percentage2, percentage3 });
            return (rects[0], rects[1], rects[2]);
        }

        public static Rect[] SplitRectByWidthPercentages(Rect rect, float[] percentages)
        {
            Rect[] rects = new Rect[percentages.Length];
            float widthSum = 0f;
            for (int i = 0; i < percentages.Length; i++)
            {
                rects[i] = new Rect(rect);
                rects[i].width *= percentages[i];
                rects[i].x += widthSum;

                widthSum += rects[i].width;
            }

            return rects;
        }
        
        public static Rect[] SplitRectByWidthPercentagesWithSpacing(Rect rect, float elementSpacing, float[] percentages)
        {
            Rect[] rects = new Rect[percentages.Length];
            float widthSum = 0f;
            for (int i = 0; i < percentages.Length; i++)
            {
                rects[i] = new Rect(rect);
                rects[i].width *= percentages[i];
                rects[i].width -= elementSpacing;
                rects[i].x += widthSum;

                widthSum += rects[i].width + elementSpacing;
            }

            return rects;
        }

        public static string TextArea(string value, string placeholder, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(options);
            var output = EditorGUI.TextArea(rect, value);
            if (string.IsNullOrEmpty(value))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var placeholderRect = new Rect(rect);
                    placeholderRect.x += 2f;
                    placeholderRect.width -= 2f;
                    placeholderRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(placeholderRect, placeholder, EditorStyles.miniLabel);
                }
            }

            return output;
        }
        
        public static bool ValidatedTextField(string value, string placeholder, Regex validator, out string output)
            => ValidatedTextField(EditorGUILayout.GetControlRect(), value, placeholder, validator.IsMatch, out output);
        
        public static bool ValidatedTextField(Rect rect, string value, string placeholder, Regex validator, out string output)
            => ValidatedTextField(rect, value, placeholder, validator.IsMatch, out output);
        
        public static bool ValidatedTextField(string value, string placeholder, Func<string, bool> validator, out string output)
            => ValidatedTextField(EditorGUILayout.GetControlRect(), value, placeholder, validator, out output);
        
        public static bool ValidatedTextField(Rect rect, string value, string placeholder, Func<string, bool> validator, out string output)
        {
            if (!string.IsNullOrEmpty(value) && !validator.Invoke(value))
            {
                using (new ColorScope(Color.red))
                {
                    output = EditorGUI.TextField(rect, value);
                }
                return false;
            }
            
            output = EditorGUI.TextField(rect, value);
            if (string.IsNullOrEmpty(value))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var placeholderRect = new Rect(rect);
                    placeholderRect.x += 2f;
                    placeholderRect.width -= 2f;
                    EditorGUI.LabelField(placeholderRect, placeholder, EditorStyles.miniLabel);
                }
                return false;
            }

            return true;
        }

        public static string TextField(Rect rect, string value, string placeholder)
        {
            var output = EditorGUI.TextField(rect, value);
            if (string.IsNullOrEmpty(value))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var placeholderRect = new Rect(rect);
                    placeholderRect.x += 2f;
                    placeholderRect.width -= 2f;
                    EditorGUI.LabelField(placeholderRect, placeholder, EditorStyles.miniLabel);
                }
            }

            return output;
        }
        
        public static Rect IconPrefix(Rect rect, GUIContent icon, float padding = 0f, float iconHeight = -1f)
        {
            var iconRect = new Rect(rect);
            if (iconHeight < 0)
            {
                iconRect.width = iconRect.height;
            }
            else
            {
                iconRect.height = iconHeight;
                iconRect.width = iconHeight;
            }
            
            var contentRect = new Rect(rect);
            contentRect.width -= iconRect.width + padding;
            contentRect.x += iconRect.width + padding;
            
            GUI.Label(iconRect, icon);

            return contentRect;
        }
        
        public static void LabelWithIcon(GUIContent label, GUIContent icon, GUIStyle labelStyle = null)
        {
            var rect = EditorGUILayout.GetControlRect();
            var labelRect = IconPrefix(rect, icon);
            
            if (labelStyle != null)
            {
                GUI.Label(labelRect, label, labelStyle);
            }
            else
            {
                GUI.Label(labelRect, label);
            }
        }

        public static string TextFormatPreviewField(string value, string placeholder, Func<string, string> formatter)
            => TextFormatPreviewField(EditorGUILayout.GetControlRect(), value, placeholder, formatter);

        public static string TextFormatPreviewField(Rect rect, string value, string placeholder, Func<string, string> formatter)
        {
            var (fieldRect, iconRect, previewRect) = SplitRectByWidthPercentages(rect, 0.4625f, 0.075f, 0.4625f);
            var output = TextField(fieldRect, value, placeholder);
            
            EditorGUI.LabelField(iconRect, "→", EditorStyles.centeredGreyMiniLabel);

            using (new EnabledScope(false))
            {
                EditorGUI.TextField(previewRect,
                    string.IsNullOrEmpty(output) ? string.Empty : formatter.Invoke(output));
            }

            return output;
        }
        
        public static bool UnityVersionField(ref string major, ref string minor, ref string release)
            => UnityVersionField(EditorGUILayout.GetControlRect(), ref major, ref minor, ref release);
        
        public static bool UnityVersionField(Rect rect, ref string major, ref string minor, ref string release)
        {
            //var versionRect = IconPrefix(rect, EditorGUIUtility.IconContent("Profiler.NetworkOperations"), EditorGUIUtility.standardVerticalSpacing);
            var unityMajorVersionRegex = new Regex("^([1-9][0-9]{3})$");
            var unityMinorVersionRegex = new Regex("^([1-9])$");
            var unityReleaseVersionRegex = new Regex("^(0|[1-9]\\d*)([abfp])(0|[1-9]\\d*)$");

            var (majorRect, minorRect, releaseRect) = SplitRectByWidthPercentagesWithSpacing(
                rect, EditorGUIUtility.standardVerticalSpacing, 3f / 6f, 2f / 6f, 1f / 6f);

            bool isValid = true;
            isValid &= ValidatedTextField(majorRect, major, "major", unityMajorVersionRegex, out major);
            isValid &= ValidatedTextField(minorRect, minor, "minor", unityMinorVersionRegex, out minor);
            isValid &= ValidatedTextField(releaseRect, release, "release", unityReleaseVersionRegex, out release);
            
            return isValid;
        }
    }
}
