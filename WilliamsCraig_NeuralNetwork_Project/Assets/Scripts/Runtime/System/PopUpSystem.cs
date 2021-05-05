/**************************************************************************************************/
/*!
\file   PopUpSystem.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a singleton system for displaying Pop-Ups.

\par Bug List

\par References
*/
/**************************************************************************************************/

using UnityEngine;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A system for displaying <see cref="PopUp"/>s and managing their activity.
  /// </summary>
  public class PopUpSystem : MonoBehaviour
  {
    /// <summary>The singleton instance of the <see cref="PopUpSystem"/>.</summary>
    private static PopUpSystem singleton;

    /// <summary>The <see cref="Image"/> used to block all other UI.</summary>
    [SerializeField] private Image raycastBlock;
    /// <summary>The <see cref="PopUp"/> to create by default.</summary>
    [SerializeField] private PopUp popupPrefab;

    /// <summary>The current <see cref="PopUp"/> being displayed.</summary>
    private PopUp currentPopUp;
    /// <summary>The <see cref="RectTransform"/> of the <see cref="PopUpSystem"/>.</summary>
    private RectTransform comRectTransform;

    private void Awake()
    {
      if (!singleton)
      {
        singleton = this;
        comRectTransform = GetComponent<RectTransform>();
      }
      else
        Destroy(this.gameObject);
    }

    /// <summary>
    /// A function for creating the default <see cref="PopUp"/>. There must not be a
    /// <see cref="PopUp"/> already active.
    /// </summary>
    /// <returns>Returns the created, but not activated, <see cref="PopUp"/>.</returns>
    public static PopUp CreatePopUp()
    {
      if (singleton)
        return CreatePopUp(singleton.popupPrefab);

      return null;
    }

    /// <summary>
    /// A function for creating a specified <see cref="PopUp"/>. There must not be a
    /// <see cref="PopUp"/> already active.
    /// </summary>
    /// <param name="prefab">The prefab <see cref="PopUp"/> to create.</param>
    /// <returns>Returns the created, but not activated, <see cref="PopUp"/>.</returns>
    public static PopUp CreatePopUp(PopUp prefab)
    {
      // Make sure the singleton is valid, there is no pop up right now, and the prefab is valid.
      if (singleton && singleton.currentPopUp == null && prefab != null)
      {
        singleton.currentPopUp = Instantiate(prefab, singleton.comRectTransform);
        singleton.currentPopUp.gameObject.SetActive(false);
        return singleton.currentPopUp;
      }

      return null;
    }
    
    /// <summary>
    /// A function for activating the current <see cref="PopUp"/>.
    /// </summary>
    public static void ActivateCurrentPopup()
    {
      if (singleton && singleton.currentPopUp)
      {
        // Display the pop up and block all UI.
        singleton.currentPopUp.gameObject.SetActive(true);
        singleton.raycastBlock.raycastTarget = true;
      }

    }

    /// <summary>
    /// A function for deactivating and destroying the current <see cref="PopUp"/>.
    /// </summary>
    public static void DestroyCurrentPopup()
    {
      if (singleton && singleton.currentPopUp)
      {
        // Destroy the pop up and unblock all UI.
        Destroy(singleton.currentPopUp.gameObject);
        singleton.raycastBlock.raycastTarget = false;
      }

    }
  }
  /************************************************************************************************/
}