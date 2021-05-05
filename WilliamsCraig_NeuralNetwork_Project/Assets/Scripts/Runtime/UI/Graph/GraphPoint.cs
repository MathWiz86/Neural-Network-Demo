/**************************************************************************************************/
/*!
\file   GraphPoint.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for a point on a graph.

\par Bug List

\par References
*/
/**************************************************************************************************/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A single point for plotting onto a <see cref="DisplayGraph"/>.
  /// </summary>
  [RequireComponent(typeof(Image))]
  public class GraphPoint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    /// <summary>The index of the point.</summary>
    public Vector2Do Index { get; private set; }
    /// <summary>The point's <see cref="RectTransform"/>.</summary>
    public RectTransform ComRectTransform { get { return comRectTransform; } }
    /// <summary>The color of the <see cref="comSprite"/>.</summary>
    public Color SpriteColor { get { return comSprite.color; } set { comSprite.color = value; } }

    /// <summary>The internal value for <see cref="ComRectTransform"/>.</summary>
    [SerializeField] private RectTransform comRectTransform;
    /// <summary>The <see cref="GraphPoint"/>'s sprite.</summary>
    [SerializeField] private Image comSprite;

    /// <summary>
    /// An initialization function for a <see cref="GraphPoint"/>.
    /// </summary>
    /// <param name="index">The index of the <see cref="GraphPoint"/>.</param>
    public void InitializePoint(Vector2Do index)
    {
      Index = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      // Show the node's value.
      NetworkSystem.HoverGraphNode(Index, SpriteColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      // Hide the value.
      NetworkSystem.UnHoverGraphNode();
    }
  }
  /************************************************************************************************/
}