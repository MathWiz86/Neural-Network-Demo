/**************************************************************************************************/
/*!
\file   ResultsTableDrawer.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-14
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a PropertyDrawer for a ResultsTable.

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
  /// A <see cref="PropertyDrawer"/> for a <see cref="ResultsTable"/>.
  /// </summary>
  [CustomPropertyDrawer(typeof(ResultsTable))]
  public class ResultsTableDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      SerializedProperty data = property.FindPropertyRelative("data");
      EditorGUI.PropertyField(position, data, true);
      SerializedProperty size = property.FindPropertyRelative("tableSize");
      size.intValue = data.arraySize;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      SerializedProperty data = property.FindPropertyRelative("data");
      return EditorGUI.GetPropertyHeight(data, true);
    }
  }
  /************************************************************************************************/
}
#endif