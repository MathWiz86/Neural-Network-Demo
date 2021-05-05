/**************************************************************************************************/
/*!
\file   Vector2DoDrawer.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-14
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a PropertyDrawer for a Vector2Do.

\par Bug List

\par References
*/
/**************************************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NeuralNetworks.Editor
{
  /************************************************************************************************/
  /// <summary>
  /// A <see cref="PropertyDrawer"/> for a <see cref="Vector2Do"/>.
  /// </summary>
  [CustomPropertyDrawer(typeof(Vector2Do))]
  public class Vector2DoDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      Rect currentPos = position; // Copy the rect.
      Vector2Do reference; // This is purely to get names properly.

      // Draw the label field and adjust the rect.
      EditorGUI.LabelField(currentPos, label);
      currentPos.height = EditorGUIUtility.singleLineHeight;
      currentPos.width -= EditorGUIUtility.labelWidth;
      currentPos.x += EditorGUIUtility.labelWidth;
     
      // Determine spacings.
      float spacing = currentPos.width * 0.04f;
      float valueWidth = currentPos.width * 0.48f;
      currentPos.width = valueWidth;

      // Get the internal properties.
      SerializedProperty a = property.FindPropertyRelative(nameof(reference.x));
      SerializedProperty b = property.FindPropertyRelative(nameof(reference.y));

      // Draw the internal properties.
      a.doubleValue = EditorGUI.DoubleField(currentPos, a.doubleValue);
      currentPos.x += (valueWidth + spacing);
      b.doubleValue = EditorGUI.DoubleField(currentPos, b.doubleValue);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      return base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
    }
  }
  /************************************************************************************************/
}
#endif