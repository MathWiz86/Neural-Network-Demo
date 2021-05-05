/**************************************************************************************************/
/*!
\file   ResultsDataDrawer.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-14
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a PropertyDrawer for a ResultsData.

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
  [CustomPropertyDrawer(typeof(ResultsData))]
  public class ResultsDataDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      Rect currentPos = position; // Copy the rect.
      ResultsData reference; // This is purely to get names properly.

      // Draw the label field and adjust the rect.
      EditorGUI.LabelField(currentPos, label);
      currentPos.height = EditorGUIUtility.singleLineHeight;
      currentPos.width -= EditorGUIUtility.labelWidth;
      currentPos.x += EditorGUIUtility.labelWidth;

      // Determine spacings.
      float spacing = currentPos.width * 0.02f;
      float valueWidth = (currentPos.width - (spacing * 2)) / 3.0f;
      currentPos.width = valueWidth;

      EditorGUI.LabelField(currentPos, new GUIContent("INPUT"));
      currentPos.x += (valueWidth + spacing);
      EditorGUI.LabelField(currentPos, new GUIContent("EXPECTED"));
      currentPos.x += (valueWidth + spacing);
      EditorGUI.LabelField(currentPos, new GUIContent("ACTUAL"));

      currentPos.x = position.x + EditorGUIUtility.labelWidth;
      currentPos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

      // Get the internal properties.
      SerializedProperty input = property.FindPropertyRelative(nameof(reference.Input));
      SerializedProperty expected = property.FindPropertyRelative(nameof(reference.ExpectedOutput));
      SerializedProperty actual = property.FindPropertyRelative(nameof(reference.ActualOutput));

      // Draw the internal properties.
      input.doubleValue = EditorGUI.DoubleField(currentPos, input.doubleValue);
      currentPos.x += (valueWidth + spacing);
      expected.doubleValue = EditorGUI.DoubleField(currentPos, expected.doubleValue);
      currentPos.x += (valueWidth + spacing);
      actual.doubleValue = EditorGUI.DoubleField(currentPos, actual.doubleValue);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      return base.GetPropertyHeight(property, label) * 2.0f + EditorGUIUtility.standardVerticalSpacing;
    }
  }
  /************************************************************************************************/
}
#endif