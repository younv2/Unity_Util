#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializedDictionaryBase), true)]
public class SerializedDictionaryDrawer : PropertyDrawer
{
    private readonly Dictionary<string, ReorderableList> dict = new();
     
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var list = GetList(property);
        return list.GetHeight() + EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        float yOffset = EditorGUIUtility.singleLineHeight + 2f;
        var listRect = new Rect(position.x, position.y + yOffset, position.width, position.height - yOffset);

        var list = GetList(property);

        property.serializedObject.Update();
        list.DoList(listRect);
        property.serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList GetList(SerializedProperty property)
    {
        var key = property.propertyPath;
        if (dict.TryGetValue(key, out var cached)) return cached;

        var keysProp = property.FindPropertyRelative("keys");
        var valsProp = property.FindPropertyRelative("values");

        if (keysProp == null || valsProp == null)
        {
            var dummy = new ReorderableList(property.serializedObject, property, true, false, false, false);
            return dict[key] = dummy;
        }

        var list = new ReorderableList(property.serializedObject, keysProp, true, false, true, true)
        {
            elementHeight = EditorGUIUtility.singleLineHeight + 6f,
            drawHeaderCallback = null
        };
        list.drawElementCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            rect.y += 2f;
            float half = rect.width / 2f;
            var rKey = new Rect(rect.x, rect.y, half - 3f, EditorGUIUtility.singleLineHeight);
            var rVal = new Rect(rect.x + half + 3f, rect.y, half - 3f, EditorGUIUtility.singleLineHeight);

            if (index < keysProp.arraySize && index < valsProp.arraySize)
            {
                var k = keysProp.GetArrayElementAtIndex(index);
                var v = valsProp.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rKey, k, GUIContent.none, true);
                EditorGUI.PropertyField(rVal, v, GUIContent.none, true);
            }
        };
        list.drawElementBackgroundCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            if (index == -1) return;

            var counts = BuildKeyCounts(keysProp);
            string keyStr = GetKeyAsString(keysProp.GetArrayElementAtIndex(index));

            if (active)
            {
                Color sel = new Color(0.24f, 0.49f, 0.90f, 0.35f);
                EditorGUI.DrawRect(rect, sel);
                return;
            }
            if (!string.IsNullOrEmpty(keyStr) && counts.TryGetValue(keyStr, out int cnt) && cnt > 1)
            {
                var color = new Color(1f, 0.1f, 0.1f, 0.25f);
                EditorGUI.DrawRect(rect, color);
            }
        };

        list.onAddCallback = l =>
        {
            keysProp.arraySize++;
            int newIndex = keysProp.arraySize - 1;

            if (valsProp.arraySize < keysProp.arraySize)
                valsProp.arraySize = keysProp.arraySize;

            property.serializedObject.ApplyModifiedProperties();
        };

        list.onRemoveCallback = l =>
        {
            int i = l.index;
            if (i < 0) return;

            if (i < keysProp.arraySize)
            {
                keysProp.DeleteArrayElementAtIndex(i);
            }
            if (i < valsProp.arraySize)
            {
                valsProp.DeleteArrayElementAtIndex(i);
            }

            property.serializedObject.ApplyModifiedProperties();
        };

        list.onReorderCallbackWithDetails = (l, oldIndex, newIndex) =>
        {
            valsProp.MoveArrayElement(oldIndex, newIndex);
            property.serializedObject.ApplyModifiedProperties();
        };

        dict[key] = list;
        return list;
    }
    private static Dictionary<string, int> BuildKeyCounts(SerializedProperty keysProp)
    {
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < keysProp.arraySize; i++)
        {
            var k = keysProp.GetArrayElementAtIndex(i);
            string s = GetKeyAsString(k);
            if (string.IsNullOrEmpty(s)) continue;
            counts.TryGetValue(s, out int c);
            counts[s] = c + 1;
        }
        return counts;
    }
    private static string GetKeyAsString(SerializedProperty p)
    {
        if(p == null) return null;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Enum:
                return p.enumDisplayNames != null && p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length
                    ? p.enumDisplayNames[p.enumValueIndex]
                    : p.enumValueIndex.ToString();

            case SerializedPropertyType.Integer:
                return p.intValue.ToString();

            case SerializedPropertyType.String:
                return p.stringValue ?? string.Empty;

            case SerializedPropertyType.Boolean:
                return p.boolValue ? "true" : "false";

            case SerializedPropertyType.ObjectReference:
                return p.objectReferenceValue ? p.objectReferenceValue.GetInstanceID().ToString() : "null";

            case SerializedPropertyType.Float:
                return p.floatValue.ToString();

            default:
                return p.displayName ?? p.type;
        }
    }
}
#endif
