/**************************************************************************************************/
/*!
\file   PopUp.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for key information that must be presented to the
  user, without allowing other input.

\par Bug List

\par References
*/
/**************************************************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A UI element to display information, while blocking the screen. Use this with the
  /// <see cref="PopUpSystem"/>.
  /// </summary>
  public class PopUp : MonoBehaviour
  {
    /**********************************************************************************************/
    /// <summary>
    /// A struct that contains both a <see cref="Button"/> and its title.
    /// </summary>
    [System.Serializable]
    private struct PopUpButton
    {
      /// <summary>The <see cref="PopUp"/>'s <see cref="Button"/>.</summary>
      public Button button;
      /// <summary>The <see cref="button"/>'s title.</summary>
      public TextMeshProUGUI tmpTitle;
    }
    /**********************************************************************************************/

    /// <summary>The <see cref="PopUp"/>'s title object.</summary>
    [SerializeField] private TextMeshProUGUI tmpTitle;
    /// <summary>The <see cref="PopUp"/>'s message object.</summary>
    [SerializeField] private TextMeshProUGUI tmpMessage;
    /// <summary>The <see cref="Button"/>s attached to the <see cref="PopUp"/>.</summary>
    [SerializeField] private PopUpButton[] buttons;

    /// <summary>
    /// A function for setting the <see cref="PopUp"/>'s title.
    /// </summary>
    /// <param name="title">The new title.</param>
    public void SetTitle(string title)
    {
      tmpTitle.text = title;
    }

    /// <summary>
    /// A function for setting the <see cref="PopUp"/>'s message.
    /// </summary>
    /// <param name="message">The new message.</param>
    public void SetMessage(string message)
    {
      tmpMessage.text = message;
    }

    /// <summary>
    /// A function for initializing a <see cref="PopUp"/> <see cref="Button"/>.
    /// </summary>
    /// <param name="index">The index of the <see cref="Button"/> to initialize.</param>
    /// <param name="title">The <see cref="Button"/>'s title.</param>
    /// <param name="action">The function to call when the <see cref="Button"/> is clicked.</param>
    public void InitializeButton(int index, string title, UnityAction action)
    {
      // Validate the index.
      if (index >= 0 && index < buttons.Length)
      {
        // Initialize the button and set the listener.
        buttons[index].button.gameObject.SetActive(true);
        buttons[index].tmpTitle.text = title;
        buttons[index].button.onClick.AddListener(action);
      }
    }
  }
  /************************************************************************************************/
}