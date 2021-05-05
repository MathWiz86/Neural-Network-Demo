/**************************************************************************************************/
/*!
\file   ResultsTable.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of classes necessary for holding results from a Neural Network.

\par Bug List

\par References
*/
/**************************************************************************************************/

using System;
using System.Collections.Generic;

namespace NeuralNetworks
{
  /************************************************************************************************/
  /// <summary>
  /// A variant of a Vector2 that uses <see cref="double"/>s instead of <see cref="float"/>s.
  /// </summary>
  [Serializable]
  public struct Vector2Do
  {
    /// <summary>The first value.</summary>
    public double x;
    /// <summary>The second value.</summary>
    public double y;

    /// <summary>
    /// A constructor for a <see cref="Vector2Do"/>.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    public Vector2Do(double x, double y)
    {
      this.x = x;
      this.y = y;
    }

#if UNITY_5_4_OR_NEWER
    /// <summary>
    /// A constructor for a <see cref="Vector2Do"/> specifically for <see cref="UnityEngine"/>.
    /// </summary>
    /// <param name="vector">The <see cref="UnityEngine.Vector2"/> to convert.</param>
    public Vector2Do(UnityEngine.Vector2 vector) : this(vector.x, vector.y) { }

    /// <summary>
    /// An implicit conversion from a <see cref="UnityEngine.Vector2"/> to <see cref="Vector2Do"/>.
    /// </summary>
    /// <param name="vector">The <see cref="UnityEngine.Vector2"/> to convert.</param>
    public static implicit operator Vector2Do(UnityEngine.Vector2 vector)
    {
      return new Vector2Do(vector);
    }

    /// <summary>
    /// An explicit conversion from a <see cref="Vector2Do"/> to <see cref="UnityEngine.Vector2"/>.
    /// This is explicit due to possible data loss.
    /// </summary>
    /// <param name="vector">The <see cref="Vector2Do"/> to convert.</param>
    public static explicit operator UnityEngine.Vector2(Vector2Do vector)
    {
      return new UnityEngine.Vector2((float)vector.x, (float)vector.y);
    }
#endif
  }
  /************************************************************************************************/
  /************************************************************************************************/
  /// <summary>
  /// A single row of data storing results of a <see cref="NeuralNetwork"/>.
  /// </summary>
  [Serializable]
  public class ResultsData
  {
    /// <summary>The input of the data.</summary>
    public double Input = 0;
    /// <summary>The expected output of the data.</summary>
    public double ExpectedOutput = 0;
    /// <summary>The actual output of the Neural Network.</summary>
    public double ActualOutput = 0;

    /// <summary>
    /// A default constructor for <see cref="ResultsData"/>.
    /// </summary>
    public ResultsData() { }

    /// <summary>
    /// A constructor for <see cref="ResultsData"/>.
    /// </summary>
    /// <param name="expectedData">A <see cref="Vector2Do"/> of the <see cref="Input"/> and
    /// <see cref="ExpectedOutput"/>.</param>
    public ResultsData(Vector2Do expectedData) : this (expectedData.x, expectedData.y, 0) { }

    /// <summary>
    /// A constructor for <see cref="ResultsData"/>.
    /// </summary>
    /// <param name="Input">The input of the data.</param>
    /// <param name="ExpectedOutput">The expected output of the data.</param>
    /// <param name="ActualOutput">The actual output of the Neural Network.</param>
    public ResultsData(double Input, double ExpectedOutput, double ActualOutput)
    {
      this.Input = Input;
      this.ExpectedOutput = ExpectedOutput;
      this.ActualOutput = ActualOutput;
    }

    /// <summary>
    /// A copy constructor for <see cref="ResultsData"/>.
    /// </summary>
    /// <param name="expectedData">The <see cref="ResultsData"/> to copy.</param>
    public ResultsData(ResultsData expectedData) : this(expectedData.Input, expectedData.ExpectedOutput, expectedData.ActualOutput) { }

#if UNITY_5_4_OR_NEWER
    /// <summary>
    /// A constructor for <see cref="ResultsData"/> specifically for <see cref="UnityEngine"/>.
    /// </summary>
    /// <param name="expectedData"></param>
    public ResultsData(UnityEngine.Vector2 expectedData) : this(expectedData.x, expectedData.y, 0) { }
#endif
  }
  /************************************************************************************************/
  /************************************************************************************************/
  /// <summary>
  /// A full table of results for an entire <see cref="NeuralNetwork"/>.
  /// </summary>
  [System.Serializable]
  public class ResultsTable
  {
    /// <summary>The size of the table.</summary>
    public int TableSize { get { return tableSize; } private set { tableSize = value; } }

#if UNITY_5_4_OR_NEWER
    /// <summary>The internal value of <see cref="TableSize"/>.</summary>
    [UnityEngine.SerializeField] private int tableSize;
    /// <summary>The <see cref="ResultsData"/> of the table.</summary>
    [UnityEngine.SerializeField] private List<ResultsData> data = null;
#else
    /// <summary>The internal value of <see cref="TableSize"/>.</summary>
    private int tableSize;
    /// <summary>The <see cref="ResultsData"/> of the table.</summary>
    private List<ResultsData> data = null;
#endif

    /// <summary>
    /// A constructor for a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="size">The size of the table. This must be greater than 0!</param>
    public ResultsTable(int size)
    {
      // The table must have some sort of size.
      if (size < 0)
        throw new ArgumentOutOfRangeException(nameof(size), string.Format("{0} is less than 0.", nameof(size)));

      // Set the size and create the data.
      TableSize = size;
      data = new List<ResultsData>();

      for (int i = 0; i < size; i++)
        data.Add(new ResultsData());
    }

    /// <summary>
    /// A constructor for a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="expectedValues">The inputs and expected outputs already known. There
    /// must be at least one point of data!</param>
    public ResultsTable(params Vector2Do[] expectedValues)
    {
      if (expectedValues == null || expectedValues.Length < 0)
        throw new ArgumentOutOfRangeException(nameof(expectedValues), string.Format("{0} has no data!", nameof(expectedValues)));

      TableSize = expectedValues.Length;
      data = new List<ResultsData>();

      for (int i = 0; i < TableSize; i++)
        data.Add(new ResultsData(expectedValues[i]));
    }

    /// <summary>
    /// A constructor for a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="expectedValues">The <see cref="ResultsData"/> already known. There must be at
    /// least one point of data!</param>
    public ResultsTable(params ResultsData[] expectedValues)
    {
      if (expectedValues == null || expectedValues.Length < 0)
        throw new ArgumentOutOfRangeException(nameof(expectedValues), string.Format("{0} has no data!", nameof(expectedValues)));

      TableSize = expectedValues.Length;
      data = new List<ResultsData>();

      for (int i = 0; i < TableSize; i++)
        data.Add(new ResultsData(expectedValues[i]));
    }

    /// <summary>
    /// A constructor for a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="expectedValues">The <see cref="ResultsData"/> already known. There must be at
    /// least one point of data!</param>
    public ResultsTable(List<ResultsData> expectedValues)
    {
      if (expectedValues == null || expectedValues.Count < 0)
        throw new ArgumentOutOfRangeException(nameof(expectedValues), string.Format("{0} has no data!", nameof(expectedValues)));

      TableSize = expectedValues.Count;
      data = new List<ResultsData>();

      for (int i = 0; i < TableSize; i++)
        data.Add(new ResultsData(expectedValues[i]));
    }

    /// <summary>
    /// A copy constructor for a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="table">The original <see cref="ResultsTable"/> to copy.</param>
    public ResultsTable(ResultsTable table) : this(table.data) { }

    /// <summary>
    /// A function for adding a piece of <see cref="ResultsData"/> to the table. This makes
    /// a copy of the <paramref name="result"/>.
    /// </summary>
    /// <param name="result">The <see cref="ResultsData"/> to add.</param>
    /// <returns>Returns if the data was added successfully.</returns>
    public bool AddData(ResultsData result)
    {
      if (result == null)
        return false;

      data.Add(new ResultsData(result));
      tableSize++;
      return true;
    }

    /// <summary>
    /// A function for removing a piece of <see cref="ResultsData"/> from the table.
    /// </summary>
    /// <param name="result">The <see cref="ResultsData"/> to remove.</param>
    /// <returns>Returns if the data was removed successfully.</returns>
    public bool RemoveData(ResultsData result)
    {
      if (data.Remove(result))
      {
        tableSize--;
        return true;
      }

      return false;
    }

    /// <summary>
    /// A function for removing a piece of <see cref="ResultsData"/> from the table.
    /// </summary>
    /// <param name="index">The index of the <see cref="ResultsData"/> to remove.</param>
    /// <returns>Returns if the data was removed successfully.</returns>
    public bool RemoveData(int index)
    {
      if (index > 0 && index < data.Count)
      {
        data.RemoveAt(index);
        tableSize--;
        return true;
      }

      return false;
    }

    public ResultsData this[int i] { get { return data[i]; } }
  }
  /************************************************************************************************/
}