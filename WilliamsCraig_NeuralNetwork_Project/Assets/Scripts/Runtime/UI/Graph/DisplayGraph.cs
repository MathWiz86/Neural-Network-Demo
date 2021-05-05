/**************************************************************************************************/
/*!
\file   DisplayGraph.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a UI display for several points on a graph, given a specific
  scaling and data points.

\par Bug List

\par References
*/
/**************************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuralNetworks.UI
{
  /************************************************************************************************/
  /// <summary>
  /// A graph display of plotted points. Useful for displaying information.
  /// </summary>
  public class DisplayGraph : MonoBehaviour
  {
    /// <summary>The object to instantiate <see cref="GraphPoint"/>s onto.</summary>
    [SerializeField] private RectTransform pointParent = null;
    /// <summary>The prefab for a <see cref="GraphPoint"/>.</summary>
    [SerializeField] private GraphPoint pointPrefab = null;
    /// <summary>The object to instantiate <see cref="GraphAxisValue"/>s onto. This should be the
    /// same size as the <see cref="pointParent"/>.</summary>
    [SerializeField] private RectTransform axisParent = null;
    /// <summary>The prefab for a <see cref="GraphAxisValue"/>.</summary>
    [SerializeField] private GraphAxisValue axisPrefab = null;
    /// <summary>The prefab for an axis line.</summary>
    [SerializeField] private Image linePrefab = null;


    /// <summary>The preferred X axis range to start with. This grows based on the data.</summary>
    [Header("Scale Settings")]
    [Space(20)]
    /// <summary>The extra scaling toa pply to each <see cref="GraphPoint"/>.</summary>
    [SerializeField] [Min(0.01f)] private float pointScaling = 1.0f;
    [SerializeField] private Vector2 preferredXAxis = new Vector2(0, 100);
    /// <summary>The number of sections to display on the X axis.</summary>
    [SerializeField] [Min(2)] private int xSections = 10;
    /// <summary>The preferred Y axis range to start with. This grows based on the data.</summary>
    [SerializeField] private Vector2 preferredYAxis = new Vector2(0, 100);
    /// <summary>The number of sections to display on the Y axis.</summary>
    [SerializeField] [Min(2)] private int ySections = 10;
    /// <summary>The distance to spawn <see cref="GraphAxisValue"/>s from their parent.</summary>
    [SerializeField] [Min(-5)] private float axisDistanceFromGraph = -5.0f;

    /// <summary>The current X axis, based on the current data.</summary>
    private Vector2 currentXAxis = new Vector2(0, 100);
    /// <summary>The current Y axis, based on the current data.</summary>
    private Vector2 currentYAxis = new Vector2(0, 100);
    /// <summary>The current <see cref="GraphPoint"/>s on display.</summary>
    private List<GraphPoint> plottedPoints = new List<GraphPoint>();
    /// <summary>The <see cref="GraphAxisValue"/>s on the X axis.</summary>
    private List<GraphAxisValue> xAxisValues = new List<GraphAxisValue>();
    /// <summary>The <see cref="GraphAxisValue"/>s on the Y axis.</summary>
    private List<GraphAxisValue> yAxisValues = new List<GraphAxisValue>();
    /// <summary>The internal scaling, applied based on the current axes.</summary>
    private float internalScaling = 1.0f;

    private void Awake()
    {
      // Initialize the current axes, based on preference.
      currentXAxis = preferredXAxis;
      currentYAxis = preferredYAxis;

      HandleAxesCreation();
    }

    /// <summary>
    /// A function for removing all data points from the graph and resetting the axes.
    /// </summary>
    public void ClearGraph()
    {
      // Destroy all plotted points.
      int count = plottedPoints.Count;
      for (int i = 0; i < count; i++)
        Destroy(plottedPoints[i].gameObject);
      plottedPoints.Clear();

      // Reset the axes and scaling.
      internalScaling = 1.0f;
      currentXAxis = preferredXAxis;
      currentYAxis = preferredYAxis;

      // Reset the axes values.
      SetAxesValues(xAxisValues, currentXAxis);
      SetAxesValues(yAxisValues, currentYAxis);
    }

    /// <summary>
    /// A function for adding a point to the graph, based on its values.
    /// </summary>
    /// <param name="x">The point's X value.</param>
    /// <param name="y">The point's Y value.</param>
    /// <param name="color">The color of the <see cref="GraphPoint"/>.</param>
    public void AddPointToGraph(double x, double y, Color color)
    {
      // Generate a new plot point and add it to the list of points.
      plottedPoints.Add(GeneratePlotPoint(new Vector2Do(x, y), color));
    }

    /// <summary>
    /// A function for adding a point to the graph, based on its values.
    /// </summary>
    /// <param name="value">The point's value.</param>
    /// <param name="color">The color of the <see cref="GraphPoint"/>.</param>
    public void AddPointToGraph(Vector2Do value, Color color)
    {
      // Generate a new plot point and add it to the list of points.
      plottedPoints.Add(GeneratePlotPoint(value, color));
    }

    /// <summary>
    /// A function for generating a <see cref="GraphPoint"/>, based on a value.
    /// </summary>
    /// <param name="value">The point's value.</param>
    /// <param name="color">The color of the <see cref="GraphPoint"/>.</param>
    /// <returns>Returns the created <see cref="GraphPoint"/>.</returns>
    private GraphPoint GeneratePlotPoint(Vector2Do value, Color color)
    {
      CheckGraphScaling(value); // Check the graph's scaling and update if necessary.
      
      // Generate the point and set it's anchor to the bottom-left.
      GraphPoint point = Instantiate(pointPrefab, pointParent);
      RectTransform rtr = point.ComRectTransform;
      rtr.anchorMin = new Vector2(0, 0);
      rtr.anchorMax = new Vector2(0, 0);

      // Initialize and plot the point.
      point.InitializePoint(value);
      PlotNewPoint(point, color);

      return point;
    }

    /// <summary>
    /// A function for plotting a new <see cref="GraphPoint"/> onto the graph.
    /// </summary>
    /// <param name="point">The <see cref="GraphPoint"/> to plot.</param>
    /// <param name="color">The color of the <see cref="GraphPoint"/>.</param>
    private void PlotNewPoint(GraphPoint point, Color color)
    {
      // Get the difference in the axis boundaries.
      float xDiff = currentXAxis.y - currentXAxis.x;
      float yDiff = currentYAxis.y - currentYAxis.x;

      // Get the relative position of the point, based on the axes and parent scale.
      float xPos = (((float)point.Index.x - currentXAxis.x) / xDiff) * pointParent.rect.width;
      float yPos = (((float)point.Index.y - currentYAxis.x) / yDiff) * pointParent.rect.height;

      // Set the position of the point, and scale the size.
      RectTransform rtr = point.ComRectTransform;
      rtr.anchoredPosition = new Vector2(xPos, yPos);
      rtr.sizeDelta = pointPrefab.ComRectTransform.sizeDelta * internalScaling * pointScaling;
      point.SpriteColor = color;
    }

    /// <summary>
    /// A helper function for checking the graph's current scaling, and updating it if necessary.
    /// </summary>
    /// <param name="value">The value to check against.</param>
    private void CheckGraphScaling(Vector2Do value)
    {
      // Get copies of the current axes.
      Vector2 xAxisCheck = currentXAxis;
      Vector2 yAxisCheck = currentYAxis;

      // Update the copies based on the test value.
      xAxisCheck.x = System.Math.Min(xAxisCheck.x, (float)value.x);
      xAxisCheck.y = System.Math.Max(xAxisCheck.y, (float)value.x);
      yAxisCheck.x = System.Math.Min(yAxisCheck.x, (float)value.y);
      yAxisCheck.y = System.Math.Max(yAxisCheck.y, (float)value.y);

      // If the axes are different, the graph must be recalculated.
      if (xAxisCheck != currentXAxis || yAxisCheck != currentYAxis)
      {
        // Update the axes.
        currentXAxis = xAxisCheck;
        currentYAxis = yAxisCheck;

        // Get the differences between the current and preferred axes.
        float curXDiff = currentXAxis.y - currentXAxis.x;
        float curYDiff = currentYAxis.y - currentYAxis.x;
        float prefXDiff = preferredXAxis.y - preferredXAxis.x;
        float prefYDiff = preferredYAxis.y - preferredYAxis.x;

        // Update the internal scaling.
        internalScaling = System.Math.Min(prefXDiff / curXDiff, prefYDiff / curYDiff);

        // Redraw the graph.
        RedrawGraph();
      }
    }

    /// <summary>
    /// A helper function for creating the axes and their lines.
    /// </summary>
    private void HandleAxesCreation()
    {
      // Calculate the spacing between the two axes.
      Vector2 xSpacing = new Vector2(pointParent.rect.width / ySections, 0.0f);
      Vector2 ySpacing = new Vector2(0.0f, pointParent.rect.height / xSections);

      // Create the axis displays.
      xAxisValues = CreateAxis(xSections, new Vector2(0.0f, axisDistanceFromGraph), new Vector2(0.5f, 1.0f), xSpacing, new Vector2(0.5f, 0.0f), new Vector2(5.0f, pointParent.rect.height));
      yAxisValues = CreateAxis(ySections, new Vector2(axisDistanceFromGraph, 0.0f), new Vector2(1.0f, 0.5f), ySpacing, new Vector2(0.0f, 0.5f), new Vector2(pointParent.rect.width, 5.0f));

      // Set the axis values.
      SetAxesValues(xAxisValues, currentXAxis);
      SetAxesValues(yAxisValues, currentYAxis);
    }

    /// <summary>
    /// A function for creating all <see cref="GraphAxisValue"/>s on a certain axis.
    /// </summary>
    /// <param name="sections">The number of sections on the axis.</param>
    /// <param name="distance">The distance from the <see cref="axisParent"/>.</param>
    /// <param name="displayPivot">The pivot of each <see cref="GraphAxisValue"/>.</param>
    /// <param name="linePivot">The pivot of each axis line.</param>
    /// <param name="spacing">The spacing between each <see cref="GraphAxisValue"/>.</param>
    /// <param name="lineSize">The size of each axis line.</param>
    /// <returns>Returns the finalized list of <see cref="GraphAxisValue"/>s.</returns>
    private List<GraphAxisValue> CreateAxis(int sections, Vector2 distance, Vector2 displayPivot, Vector2 spacing, Vector2 linePivot, Vector2 lineSize)
    {
      List<GraphAxisValue> values = new List<GraphAxisValue>();
      Vector2 lineDistance = Vector2.zero;

      // Iterate through all sections. An extra one is added for the initial value.
      for (int i = 0; i <= sections; i++)
      {
        // Create the axis.
        GraphAxisValue axis = Instantiate(axisPrefab, axisParent);
        RectTransform rtr = axis.ComRectTransform;

        // Set up the transform. The anchor is always in the bottom-left.
        rtr.anchorMin = Vector2.zero;
        rtr.anchorMax = Vector2.zero;
        rtr.pivot = displayPivot;
        rtr.anchoredPosition = distance;

        // Create the axis line.
        Image line = Instantiate(linePrefab, pointParent);
        RectTransform lineRTR = line.transform as RectTransform;

        // Set up the line's transform. The anchor is always in the bottom-left.
        lineRTR.anchorMin = Vector2.zero;
        lineRTR.anchorMax = Vector2.zero;
        lineRTR.pivot = linePivot;
        lineRTR.anchoredPosition = lineDistance;
        lineRTR.sizeDelta = lineSize;

        distance += spacing; // Update the axis spacing.
        lineDistance += spacing; // Update the line spacing.
        values.Add(axis); // Add the display to the list.

        
      }

      return values; // Return the list.
    }

    /// <summary>
    /// A function for setting the values of an axis' <see cref="GraphAxisValue"/>s, based
    /// on the current range.
    /// </summary>
    /// <param name="axesDisplays">The <see cref="GraphAxisValue"/>s to edit.</param>
    /// <param name="axisValues">The range of values.</param>
    private void SetAxesValues(List<GraphAxisValue> axesDisplays, Vector2 axisValues)
    {
      // Calculate the difference between each value.
      int count = axesDisplays.Count;
      float value = axisValues.x;
      float valueDiff = (axisValues.y - axisValues.x) / (count - 1);

      // For each display, set the current value, and increment the displayed value.
      for (int i = 0; i < count; i++)
      {
        axesDisplays[i].TMPTitle.text = value.ToString("F2");
        value += valueDiff;
      }

      // Ensure the last display shows the last value.
      axesDisplays[axesDisplays.Count - 1].TMPTitle.text = axisValues.y.ToString("F2");
    }

    /// <summary>
    /// A helper function for redrawing the entire graph. This is used when the scale changes.
    /// </summary>
    private void RedrawGraph()
    {
      // Replot every point.
      int count = plottedPoints.Count;
      for (int i = 0; i < count; i++)
        PlotNewPoint(plottedPoints[i], plottedPoints[i].SpriteColor);

      SetAxesValues(xAxisValues, currentXAxis);
      SetAxesValues(yAxisValues, currentYAxis);
    }
  }
  /************************************************************************************************/
}