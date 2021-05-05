/**************************************************************************************************/
/*!
\file   ValueSlider.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for a Slider that shows its current value.

\par Bug List

\par References
*/
/**************************************************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A <see cref="Slider"/> that displays its value on a text object.
  /// </summary>
  [RequireComponent(typeof(Slider))]
  public class ValueSlider : MonoBehaviour
  {
    /// <summary>The text object to display the <see cref="comSlider"/>'s value.</summary>
    [SerializeField] private TextMeshProUGUI tmpValue;
    /// <summary>The text formatting for the value.</summary>
    [SerializeField] private string format = "F2";

    /// <summary>The <see cref="Slider"/> component attached to this object.</summary>
    private Slider comSlider;

    private void Awake()
    {
      // Get the slider, and attach the listener.
      comSlider = GetComponent<Slider>();
      comSlider.onValueChanged.AddListener(OnValueChanged);
      OnValueChanged(comSlider.value);
    }

    /// <summary>
    /// A listener for when the <see cref="comSlider"/>'s value changes. This updates the display.
    /// </summary>
    /// <param name="value">The new value.</param>
    private void OnValueChanged(float value)
    {
      tmpValue.text = comSlider.wholeNumbers ? ((int)value).ToString(format) : value.ToString(format);
    }
  }
  /************************************************************************************************/
}