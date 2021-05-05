/**************************************************************************************************/
/*!
\file   ResultDataDisplay.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for some ResultsData. This can be edited in the
  program.

\par Bug List

\par References
*/
/**************************************************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A UI element for displaying a <see cref="ResultsData"/> from a <see cref="NeuralNetwork"/>.
  /// </summary>
  public class ResultDataDisplay : MonoBehaviour, IPointerClickHandler
  {
    /// <summary>The normal <see cref="Color"/> when the display is not selected.</summary>
    private static readonly Color NormalColor = Color.white;
    /// <summary>The <see cref="Color"/> when the display is selected.</summary>
    private static readonly Color SelectedColor = Color.blue;

    /// <summary>The <see cref="ResultsData"/> represented by the display.</summary>
    public ResultsData DisplayedData { get; private set; }

    /// <summary>The background <see cref="Image"/> of the display.</summary>
    [SerializeField] private Image background;
    /// <summary>The input field for the <see cref="ResultsData.Input"/>.</summary>
    [SerializeField] private TMP_InputField inputField;
    /// <summary>The input field for the <see cref="ResultsData.ExpectedOutput"/>.</summary>
    [SerializeField] private TMP_InputField exoutputField;
    /// <summary>The input field for the <see cref="ResultsData.ActualOutput"/>.</summary>
    [SerializeField] private TMP_InputField acoutputField;

    /// <summary>
    /// A function for initializing the <see cref="ResultDataDisplay"/>.
    /// </summary>
    /// <param name="data">The <see cref="ResultsData"/> represented by the display.</param>
    public void InitializeDisplay(ResultsData data)
    {
      // Set the data and update the display.
      DisplayedData = data;
      UpdateDisplay();
    }

    /// <summary>
    /// A function to update the UI display of the information stored.
    /// </summary>
    public void UpdateDisplay()
    {
      // Reset all of the input field text.
      inputField.text = DisplayedData.Input.ToString();
      exoutputField.text = DisplayedData.ExpectedOutput.ToString();
      acoutputField.text = DisplayedData.ActualOutput.ToString();
    }

    /// <summary>
    /// A callback function for when the <see cref="inputField"/> changes value.
    /// </summary>
    /// <param name="parseString">The <see cref="string"/> to parse a value out of.</param>
    public void OnInputChanged(string parseString)
    {
      // Parse the value if possible. If not, reset to the last saved value.
      if (double.TryParse(parseString, out double value))
        DisplayedData.Input = value;
      else
        inputField.text = DisplayedData.Input.ToString();

      NetworkSystem.CheckAutoSave();
    }

    /// <summary>
    /// A callback function for when the <see cref="exoutputField"/> changes value.
    /// </summary>
    /// <param name="parseString">The <see cref="string"/> to parse a value out of.</param>
    public void OnExpectedOutputChanged(string parseString)
    {
      // Parse the value if possible. If not, reset to the last saved value.
      if (double.TryParse(parseString, out double value))
        DisplayedData.ExpectedOutput = value;
      else
        exoutputField.text = DisplayedData.ExpectedOutput.ToString();

      NetworkSystem.CheckAutoSave();
    }

    /// <summary>
    /// A helper function for returning the display to the <see cref="NormalColor"/>.
    /// </summary>
    public void ReturnToNormalColor()
    {
      background.color = NormalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      // When clicked, make this the selected display.
      NetworkSystem.SelectDisplay(this);
      background.color = SelectedColor;
    }
  }
  /************************************************************************************************/
}