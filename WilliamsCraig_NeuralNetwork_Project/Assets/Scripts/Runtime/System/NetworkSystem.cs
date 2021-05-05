/**************************************************************************************************/
/*!
\file   NetworkSystem.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a system for handling a Neural Network and displaying its
  information.

\par Bug List

\par References
*/
/**************************************************************************************************/

using NeuralNetworks.Kits;
using NeuralNetworks.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeuralNetworks
{
  /************************************************************************************************/
  /// <summary>
  /// A system for handling the display and manipulation of the <see cref="NeuralNetwork"/>.
  /// </summary>
  public class NetworkSystem : MonoBehaviour
  {
    /// <summary>The path to the .csv file with the neural data.</summary>
    private static readonly string TargetPath = Directory.GetCurrentDirectory() + "\\_Data\\" + "neural_data.csv";
    /// <summary>The number of data points to add when there is no .csv file.</summary>
    private static readonly int BasicDataCount = 30;

    /// <summary>The singleton instance of the <see cref="NetworkSystem"/>.</summary>
    private static NetworkSystem singleton;

    [Header("Display Properties")]
    /// <summary>The <see cref="Button"/> for removing an element.</summary>
    [SerializeField] private Button buRemoveElement;
    /// <summary>The prefab for a <see cref="ResultDataDisplay"/>.</summary>
    [SerializeField] private ResultDataDisplay displayPrefab;
    /// <summary>The parent to instantiate <see cref="ResultDataDisplay"/>s to.</summary>
    [SerializeField] private RectTransform displayParent;
    /// <summary>The graph to plot the expected outputs vs. the actual outputs.</summary>
    [SerializeField] private DisplayGraph pointGraph = null;
    /// <summary>A text object for displaying the overall error.</summary>
    [SerializeField] private TextMeshProUGUI tmpError = null;
    /// <summary>The input field for the number of iterations.</summary>
    [SerializeField] private TMP_InputField iterationField;
    [SerializeField] private TextMeshProUGUI tmpHoveredNode;

    /// <summary>The <see cref="NeuralNetwork"/> being displayed.</summary>
    [Header("Neural Network Properties")]
    [Space(20)]
    [SerializeField] private NeuralNetwork network;

    /// <summary>A <see cref="PopUp"/> to display when the .csv file cannot be opened.</summary>
    [Header("Pop Up Properties")]
    [Space(20)]
    [SerializeField] private PopUp popupWarning;
    /// <summary>A <see cref="PopUp"/> to display when the .csv file is saved.</summary>
    [SerializeField] private PopUp popupSave;

    /// <summary>The <see cref="ResultDataDisplay"/>s currently viewable.</summary>
    private List<ResultDataDisplay> resultDisplays = new List<ResultDataDisplay>();
    /// <summary>The <see cref="ResultDataDisplay"/> currently clicked on.</summary>
    private ResultDataDisplay selectedDisplay = null;
    /// <summary>The function used for the <see cref="NeuralNetwork"/>.</summary>
    private System.Func<double, double> neuralFunction = input => input / System.Math.Sqrt(1 + (input * input));
    /// <summary>The deriviation of the <see cref="neuralFunction"/>.</summary>
    private System.Func<double, double> derivedFunction = (input) => 1 / ((1 + (input * input)) * (System.Math.Sqrt(1 + (input * input))));
    /// <summary>The function of the expected output.</summary>
    private System.Func<double, double> expectedFunction = (input) => System.Math.Sin(input);
    /// <summary>A toggle for auto-saving when any change is made to the data.</summary>
    private bool autoSaveWork = true;

    private void Awake()
    {
      // If the singleton is not set, set it and initialize the network and display.
      if (!singleton)
      {
        singleton = this;
        network.SetNeuralFunction(neuralFunction);
        network.SetDerivedFunction(derivedFunction);
        NeuralCSVManager.BindToOnParseFailed(HandleBadReadFile);
        InitializeData();
        InitializeDisplay();
      }
      else
        Destroy(gameObject);
    }

    /// <summary>
    /// A function for selecting a <see cref="ResultDataDisplay"/> in order to remove it.
    /// </summary>
    /// <param name="display">The <see cref="ResultDataDisplay"/> to select.</param>
    public static void SelectDisplay(ResultDataDisplay display)
    {
      if (singleton != null)
        singleton.InternalSelectDisplay(display);
    }

    /// <summary>
    /// A helper function for creating basic data when no file is present.
    /// </summary>
    public static void CreateBasicData()
    {
      if (singleton)
        singleton.StartCoroutine(singleton.InternalCreateBasicData());
    }
    
    /// <summary>
    /// A function to save the file silently, if <see cref="autoSaveWork"/> is <see cref="true"/>.
    /// </summary>
    public static void CheckAutoSave()
    {
      if (singleton)
        singleton.InternalCheckAutoSave();
    }

    /// <summary>
    /// A function for displaying the values of a hovered node on the <see cref="DisplayGraph"/>.
    /// </summary>
    /// <param name="value">The value of the hovered node.</param>
    /// <param name="color">The color of the <see cref="GraphPoint"/>.</param>
    public static void HoverGraphNode(Vector2Do value, Color color)
    {
      if (singleton)
      {
        singleton.tmpHoveredNode.text = new StringBuilder("(").Append(value.x.ToString()).Append(" , ").Append(value.y.ToString()).Append(")").ToString();
        singleton.tmpHoveredNode.color = color;
      }
        
    }

    /// <summary>
    /// A function for hiding the value of a hovered node on the <see cref="DisplayGraph"/>.
    /// </summary>
    public static void UnHoverGraphNode()
    {
      if (singleton)
        singleton.tmpHoveredNode.text = string.Empty;
    }

    /// <summary>
    /// A UI function for adding a <see cref="ResultsData"/> to the <see cref="NeuralNetwork"/>.
    /// </summary>
    public void Button_AddElement()
    {
      // Create some result data and initialize it into the table.
      ResultsData result = new ResultsData();
      result.ExpectedOutput = neuralFunction.Invoke(result.Input);
      network.AddData(result);
      result = network.GetData()[resultDisplays.Count];

      // Create the data display.
      ResultDataDisplay display = Instantiate(displayPrefab, displayParent);
      display.InitializeDisplay(result);
      resultDisplays.Add(display);

      InternalCheckAutoSave();
    }
    
    /// <summary>
    /// A UI function for removing a <see cref="ResultsData"/> from the <see cref="NeuralNetwork"/>.
    /// </summary>
    public void Button_RemoveElement()
    {
      // Make sure there is something selected.
      if (selectedDisplay != null)
      {
        // Remove the data.
        network.RemoveData(selectedDisplay.DisplayedData);

        // Remove the display. The remove button no longer can be clicked either.
        resultDisplays.Remove(selectedDisplay);
        Destroy(selectedDisplay.gameObject);
        selectedDisplay = null;
        buRemoveElement.interactable = false;

        InternalCheckAutoSave();
      }
    }

    /// <summary>
    /// A UI function for saving the <see cref="NeuralNetwork"/> to a .csv file.
    /// </summary>
    public void Button_SaveData()
    {
      // Create a pop-up depending on if the save is successful.
      if (NeuralCSVManager.WriteNeuralDataToFile(network.GetData(), TargetPath))
        CreateSavePopUp();
      else
        CreateBadSavePopUp();
    }

    /// <summary>
    /// A UI function for solving all of the <see cref="NeuralNetwork"/>'s inputs and changing
    /// its <see cref="ResultsData.ExpectedOutput"/>s.
    /// </summary>
    public void Button_AutoSolve()
    {
      ResultsTable data = network.GetData();

      // Solve every expected output with the neural function and update the display.
      for (int i = 0; i < data.TableSize; i++)
      {
        data[i].ExpectedOutput = expectedFunction.Invoke(data[i].Input);
        resultDisplays[i].UpdateDisplay();
      }

      InternalCheckAutoSave();
    }

    /// <summary>
    /// A UI function for running the network.
    /// </summary>
    public void Button_RunNetwork()
    {
      // Initialize and run the network.
      network.InitializeNetwork();
      network.RunNeuralNetwork();

      // Plot the data onto the graph and show the last error.
      PlotPointsOnGraph();
      tmpError.text = network.LastError.ToString("F3");

      // Update all displays.
      int count = resultDisplays.Count;
      for (int i = 0; i < count; i++)
        resultDisplays[i].UpdateDisplay();

      InternalCheckAutoSave();
    }

    /// <summary>
    /// A UI function for reloading all data last saved to the file.
    /// </summary>
    public void Button_ReloadData()
    {
      // Initialize the data and display again.
      InitializeData();
      InitializeDisplay();

      // Replot the last data.
      if (network.GetData() != null)
        PlotPointsOnGraph();
    }

    /// <summary>
    /// A UI function for quitting the application.
    /// </summary>
    public void Button_QuitApplication()
    {
      Application.Quit();
    }

    /// <summary>
    /// An options function for editing the <see cref="NeuralNetwork.learningConstant"/>.
    /// </summary>
    /// <param name="speed">The new learning constant.</param>
    public void Options_LearningSpeed(float speed)
    {
      network.SetLearningConstant(speed);
    }

    /// <summary>
    /// An options function for editing the <see cref="NeuralNetwork.neuronCount"/>.
    /// </summary>
    /// <param name="count">The new neuron count.</param>
    public void Options_NeuronCount(float count)
    {
      network.SetNeuronCount((int)count);
    }

    /// <summary>
    /// An options function for editing the <see cref="NeuralNetwork.maxIterations"/>.
    /// </summary>
    /// <param name="iterations">The new max iterations.</param>
    public void Options_Iterations(string iterations)
    {
      if (int.TryParse(iterations, out int result))
        network.SetMaxIterations(result);

      iterationField.text = network.GetMaxIterations().ToString();
    }

    /// <summary>
    /// An options function for editing the <see cref="NeuralNetwork.maxError"/>.
    /// </summary>
    /// <param name="error">The new max error.</param>
    public void Options_MaxError(float error)
    {
      network.SetMaxError(error);
    }

    /// <summary>
    /// An options function for editing <see cref="autoSaveWork"/>.
    /// </summary>
    /// <param name="save">The value for auto saving.</param>
    public void Options_AutoSave(bool save)
    {
      autoSaveWork = save;
    }

    /// <summary>
    /// A function for initializing the data from the .csv file.
    /// </summary>
    private void InitializeData()
    {
      // Read the neural data, if possible.
      ResultsTable data = NeuralCSVManager.ReadNeuralData(TargetPath);
      network.SetData(data == null ? new ResultsTable(0) : data);
    }

    /// <summary>
    /// A function for initializing the UI display.
    /// </summary>
    private void InitializeDisplay()
    {

      ResultsTable data = network.GetData();

      if (data != null)
      {
        // Destroy all old displays.
        int resultCount = resultDisplays.Count;
        for (int i = 0; i < resultCount; i++)
          Destroy(resultDisplays[i].gameObject);
        resultDisplays.Clear();

        // Create the new displays.
        resultCount = data.TableSize;
        for (int i = 0; i < resultCount; i++)
        {
          ResultDataDisplay display = Instantiate(displayPrefab, displayParent);
          display.InitializeDisplay(data[i]);
          resultDisplays.Add(display);
        }
      }
    }

    /// <summary>
    /// A helper function for plotting all data points in the <see cref="NeuralNetwork"/> onto the
    /// graph.
    /// </summary>
    private void PlotPointsOnGraph()
    {
      pointGraph.ClearGraph(); // Clear out the last graph.

      // Add all the data points to the graph.
      ResultsTable table = network.GetData();
      int count = table.TableSize;

      for (int i = 0; i < count; i++)
      {
        ResultsData data = table[i];
        pointGraph.AddPointToGraph(data.Input, data.ExpectedOutput, Color.white);
        pointGraph.AddPointToGraph(data.Input, data.ActualOutput, Color.red);
      }
    }

    /// <summary>
    /// A helper function for creating a bad file <see cref="PopUp"/>.
    /// </summary>
    private void CreateWarningPopUp()
    {
      PopUp popUp = PopUpSystem.CreatePopUp(popupWarning);
      popUp.InitializeButton(0, "YES", CreateBasicData);
      popUp.InitializeButton(0, "YES", PopUpSystem.DestroyCurrentPopup);
      popUp.InitializeButton(1, "NO", PopUpSystem.DestroyCurrentPopup);
      PopUpSystem.ActivateCurrentPopup();
    }

    /// <summary>
    /// A helper function for creating a successful save <see cref="PopUp"/>.
    /// </summary>
    private void CreateSavePopUp()
    {
      PopUp popUp = PopUpSystem.CreatePopUp(popupSave);
      popUp.InitializeButton(0, "OKAY", PopUpSystem.DestroyCurrentPopup);
      PopUpSystem.ActivateCurrentPopup();
    }

    /// <summary>
    /// A helper function for creating a bad save <see cref="PopUp"/>.
    /// </summary>
    private void CreateBadSavePopUp()
    {
      PopUp popUp = PopUpSystem.CreatePopUp(popupSave);
      popUp.InitializeButton(0, "OKAY", PopUpSystem.DestroyCurrentPopup);
      popUp.SetTitle("SAVE FAILED");
      popUp.SetMessage("Could not save file. Ensure the file is not open.");
      PopUpSystem.ActivateCurrentPopup();
    }

    /// <summary>
    /// A helper function for when a bad file is read.
    /// </summary>
    /// <param name="index">The index of the bad line.</param>
    /// <param name="message">The contents of the bad line.</param>
    private void HandleBadReadFile(int index, string message)
    {
      CreateWarningPopUp();
    }

    /// <summary>
    /// The internal function for <see cref="CreateBasicData"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerator InternalCreateBasicData()
    {
      // Create a new table, based on the basic data count.
      ResultsTable table = new ResultsTable(BasicDataCount + 1);
      double addition = 1.0f / BasicDataCount;

      // Initialize all basic values.
      for (int i = 0; i < table.TableSize; i++)
      {
        table[i].Input = System.Math.Round(i * addition, 3);
        table[i].ExpectedOutput = expectedFunction(table[i].Input);
      }
      network.SetData(table);
      yield return new WaitForSecondsRealtime(0.01f);
      // Save the file and display the values.
      Button_SaveData();
      InitializeDisplay();

      yield return null;
    }

    /// <summary>
    /// The internal function for <see cref="SelectDisplay(ResultDataDisplay)"/>.
    /// </summary>
    /// <param name="display">The <see cref="ResultDataDisplay"/> to select.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InternalSelectDisplay(ResultDataDisplay display)
    {
      if (display != null)
      {
        selectedDisplay?.ReturnToNormalColor(); // If the old display isn't null, reset it's color.
        selectedDisplay = display; // Set the new selection.
        buRemoveElement.interactable = true; // This selection can be removed.
      }
    }

    /// <summary>
    /// The internal function for <see cref="CheckAutoSave"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InternalCheckAutoSave()
    {
      if (autoSaveWork)
        NeuralCSVManager.WriteNeuralDataToFile(network.GetData(), TargetPath);
    }
  }
  /************************************************************************************************/
}