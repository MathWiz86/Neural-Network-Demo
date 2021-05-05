/**************************************************************************************************/
/*!
\file   GraphAxisValue.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for a value on a graph axis.

\par Bug List

\par References
*/
/**************************************************************************************************/

using TMPro;
using UnityEngine;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A UI display for a value on a <see cref="DisplayGraph"/> axis.
  /// </summary>
  [RequireComponent(typeof(TextMeshProUGUI))] [RequireComponent(typeof(RectTransform))]
  public class GraphAxisValue : MonoBehaviour
  {
    /// <summary>The <see cref="TextMeshProUGUI"/> for the axis.</summary>
    public TextMeshProUGUI TMPTitle { get { return tmpTitle; } }
    /// <summary>The <see cref="RectTransform"/> for the axis object</summary>
    public RectTransform ComRectTransform { get { return comRectTransform; } }

    /// <summary>The internal value for <see cref="TMPTitle"/>.</summary>
    [SerializeField] private TextMeshProUGUI tmpTitle;
    /// <summary>The internal value for <see cref="ComRectTransform"/>.</summary>
    [SerializeField] private RectTransform comRectTransform;
  }
  /************************************************************************************************/
}