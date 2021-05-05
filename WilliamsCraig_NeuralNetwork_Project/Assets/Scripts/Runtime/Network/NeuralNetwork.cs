/**************************************************************************************************/
/*!
\file   NeuralNetwork.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-19
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a whole NeuralNetwork, based on passed-in data.

\par Bug List

\par References
*/
/**************************************************************************************************/

using NeuralNetworks.Kits;
using System.Runtime.CompilerServices;

namespace NeuralNetworks
{
  /************************************************************************************************/
  /// <summary>
  /// A full network of <see cref="Neuron"/>s to calculate output values.
  /// </summary>
  [System.Serializable]
  public class NeuralNetwork
  {
    /// <summary>The last error to be calculated.</summary>
    public double LastError { get; private set; }

#if UNITY_5_4_OR_NEWER
    /// <summary>The data for the <see cref="NeuralNetwork"/>.</summary>
    [UnityEngine.SerializeField] private ResultsTable data;
    /// <summary>The learning speed of the <see cref="NeuralNetwork"/>.</summary>
    [UnityEngine.SerializeField] [UnityEngine.Range(0.0001f, 0.001f)] private double learningConstant = 0.001f;
    /// <summary>The number of <see cref="Neuron"/>s in the <see cref="NeuralNetwork"/>.</summary>
    [UnityEngine.SerializeField] [UnityEngine.Range(1, 500)] private int neuronCount = 1;
    /// <summary>The maximum number of iterations to run through to train the network.</summary>
    [UnityEngine.SerializeField] [UnityEngine.Range(1, 50000)] private int maxIterations = 500;
    /// <summary>The maximum error allowed. Once below this, the network stops training.</summary>
    [UnityEngine.SerializeField] [UnityEngine.Range(0.0f, 0.1f)] private double maxError = 0.01;
    /// <summary>The range of values possible for the Output Weight.</summary>
    [UnityEngine.SerializeField] private Vector2Do outputWeightRange = new Vector2Do(0, 1);
    /// <summary>The range of values possible for the Input Weight.</summary>
    [UnityEngine.SerializeField] private Vector2Do inputWeightRange = new Vector2Do(0, 1);
    /// <summary>The range of values possible for the Bias.</summary>
    [UnityEngine.SerializeField] private Vector2Do biasRange = new Vector2Do(0, 1);
#else
    /// <summary>The data for the <see cref="NeuralNetwork"/>.</summary>
    private ResultsTable data;
    /// <summary>The learning speed of the <see cref="NeuralNetwork"/></summary>
    private double learningConstant = 1.0f;
    /// <summary>The number of <see cref="Neuron"/>s in the <see cref="NeuralNetwork"/>.</summary>
    private int neuronCount = 1;
    /// <summary>The maximum number of iterations to run through to train the network.</summary>
    private int maxIterations = 500;
    /// <summary>The maximum error allowed. Once below this, the network stops training.</summary>
    private double maxError = 0.01;
    /// <summary>The range of values possible for the Output Weight.</summary>
    private Vector2Do outputWeightRange = new Vector2Do(0, 1);
    /// <summary>The range of values possible for the Input Weight.</summary>
    private Vector2Do inputWeightRange = new Vector2Do(0, 1);
    /// <summary>The range of values possible for the Bias.</summary>
    private Vector2Do biasRange = new Vector2Do(0, 1);
#endif
    /// <summary>The <see cref="Neuron"/>s making up the <see cref="NeuralNetwork"/>.</summary>
    private Neuron[] neurons = null;
    /// <summary>The phi function used by the <see cref="Neuron"/>s.</summary>
    private System.Func<double, double> neuralFunction = null;
    /// <summary>The deriviation of the <see cref="neuralFunction"/>.</summary>
    private System.Func<double, double> derivedFunction = null;

    /// <summary>
    /// A constructor for a <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="neuralFunction">The phi function used by the <see cref="Neuron"/>s.</param>
    /// <param name="derivedFunction">The deriviation of the <see cref="neuralFunction"/>.</param>
    public NeuralNetwork(System.Func<double, double> neuralFunction, System.Func<double, double> derivedFunction) : this(neuralFunction, derivedFunction, 1, 1, 500, 0.01) { }

    /// <summary>
    /// A constructor for a <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="neuralFunction">The phi function used by the <see cref="Neuron"/>s.</param>
    /// <param name="derivedFunction">The deriviation of the <see cref="neuralFunction"/>.</param>
    /// <param name="learningConstant">The learning speed of the network.</param>
    /// <param name="neuronCount">The number of <see cref="Neuron"/>s.</param>
    /// <param name="maxIterations">The max number of iterations to run through for training</param>
    /// <param name="maxError">The maximum error allowed before training stops.</param>
    public NeuralNetwork(System.Func<double, double> neuralFunction, System.Func<double, double> derivedFunction, double learningConstant, int neuronCount, int maxIterations, double maxError)
    {
      SetNetworkFunctions(neuralFunction, derivedFunction);
      this.learningConstant = learningConstant;

      // There must be at least one Neuron.
      if (neuronCount > 0)
        this.neuronCount = neuronCount;
      else
        throw new System.ArgumentOutOfRangeException(nameof(neuronCount), string.Format("{0} is less than 0!", nameof(neuronCount)));
    }

    /// <summary>
    /// A getter for the <see cref="ResultsTable"/> of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <returns>Returns the network's <see cref="ResultsTable"/>.</returns>
    public ResultsTable GetData()
    {
      return data;
    }

    /// <summary>
    /// A getter for the <see cref="maxIterations"/>.
    /// </summary>
    /// <returns>Returns the network's <see cref="maxIterations"/>.</returns>
    public int GetMaxIterations()
    {
      return maxIterations;
    }

    /// <summary>
    /// A function for setting the data of the <see cref="NeuralNetwork"/>. This makes a copy of
    /// the passed-in data for safety/null-check reasons.
    /// </summary>
    /// <param name="newData">The <see cref="ResultsTable"/> to copy.</param>
    public void SetData(ResultsTable newData)
    {
      // Make a copy if there is data.
      if (newData != null)
        data = new ResultsTable(newData);
      else
        throw new System.ArgumentNullException(nameof(newData), string.Format("{0} is null!", nameof(newData)));
    }

    /// <summary>
    /// A function for setting the data of the <see cref="NeuralNetwork"/>. This makes a copy of
    /// the passed-in data for safety/null-check reasons.
    /// </summary>
    /// <param name="newData">The <see cref="ResultsData"/> to copy.</param>
    public void SetData(ResultsData[] newData)
    {
      // Make a copy if there is data.
      if (newData != null)
        data = new ResultsTable(newData);
      else
        throw new System.ArgumentNullException(nameof(newData), string.Format("{0} is null!", nameof(newData)));
    }

    /// <summary>
    /// A function for setting the data of the <see cref="NeuralNetwork"/>. This makes a copy of
    /// the passed-in data for safety/null-check reasons.
    /// </summary>
    /// <param name="newData">The <see cref="Vector2Do"/> data to copy.</param>
    public void SetData(Vector2Do[] newData)
    {
      // Make a copy if there is data.
      if (newData != null)
        data = new ResultsTable(newData);
      else
        throw new System.ArgumentNullException(nameof(newData), string.Format("{0} is null!", nameof(newData)));
    }

    /// <summary>
    /// A function for setting the <see cref="neuralFunction"/> and <see cref="derivedFunction"/>s
    /// of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="neuralFunction">The phi function used by the <see cref="Neuron"/>s.</param>
    /// <param name="derivedFunction">The deriviation of the <see cref="neuralFunction"/>.</param>
    public void SetNetworkFunctions(System.Func<double, double> neuralFunction, System.Func<double, double> derivedFunction)
    {
      SetNeuralFunction(neuralFunction);
      SetDerivedFunction(derivedFunction);
    }

    /// <summary>
    /// A function for setting the <see cref="neuralFunction"/> of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="neural">The phi function used by the <see cref="Neuron"/>s.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNeuralFunction(System.Func<double, double> neural)
    {
      if (neural != null)
        neuralFunction = neural;
      else
        throw new System.ArgumentNullException(nameof(neural), string.Format("{0} is null!", nameof(neural)));
    }

    /// <summary>
    /// A function for setting the <see cref="derivedFunction"/> of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="derived">The deriviation of the <see cref="neuralFunction"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDerivedFunction(System.Func<double, double> derived)
    {
      if (derived != null)
        derivedFunction = derived;
      else
        throw new System.ArgumentNullException(nameof(derived), string.Format("{0} is null!", nameof(derived)));
    }

    /// <summary>
    /// A function for setting the weights and biases allowed for the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="output">The range of values possible for the Output Weight.</param>
    /// <param name="input">The range of values possible for the Input Weight.</param>
    /// <param name="bias">The range of values possible for the Bias.</param>
    public void SetWeightAndBiasRanges(Vector2Do output, Vector2Do input, Vector2Do bias)
    {
      SetOutputWeight(output);
      SetInputWeight(input);
      SetBias(bias);
    }

    /// <summary>
    /// A function for setting the output weights allowed for the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="output">The range of values possible for the Output Weight.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOutputWeight(Vector2Do output)
    {
      if (output.x <= output.y)
        outputWeightRange = output;
      else
        throw new System.ArgumentOutOfRangeException(nameof(output), "Min is greater than Max!");
    }

    /// <summary>
    /// A function for setting the input weights allowed for the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="input">The range of values possible for the Input Weight.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetInputWeight(Vector2Do input)
    {
      if (input.x <= input.y)
        inputWeightRange = input;
      else
        throw new System.ArgumentOutOfRangeException(nameof(input), "Min is greater than Max!");
    }

    /// <summary>
    /// A function for setting the biases allowed for the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="bias">The range of values possible for the Bias.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBias(Vector2Do bias)
    {
      if (bias.x <= bias.y)
        biasRange = bias;
      else
        throw new System.ArgumentOutOfRangeException(nameof(bias), "Min is greater than Max!");
    }

    /// <summary>
    /// A function for setting the number of <see cref="Neuron"/>s in the hidden layer.
    /// </summary>
    /// <param name="count">The number of <see cref="Neuron"/>s in the hidden layer.</param>
    public void SetNeuronCount(int count)
    {
      // There must be at least one Neuron.
      if (count > 0)
        neuronCount = count;
      else
        throw new System.ArgumentOutOfRangeException(nameof(count), string.Format("{0} is less than or equal to 0!", nameof(count)));
    }

    /// <summary>
    /// A function for setting the <see cref="learningConstant"/> of the
    /// <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="constant">The learning speed of the <see cref="NeuralNetwork"/>.</param>
    public void SetLearningConstant(double constant)
    {
      if (constant > 0)
        learningConstant = constant;
      else
        throw new System.ArgumentOutOfRangeException(nameof(constant), string.Format("{0} is less than or equal to 0!", nameof(constant)));
    }

    /// <summary>
    /// A function for setting the <see cref="maxIterations"/> of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="iterations">The max number of iterations to run through for training.</param>
    public void SetMaxIterations(int iterations)
    {
      // There must be at least one iteration.
      if (iterations <= 0)
        iterations = 1;
      if (iterations > 50000)
        iterations = 50000;

      maxIterations = iterations;
    }

    /// <summary>
    /// A function for setting the <see cref="maxError"/> of the <see cref="NeuralNetwork"/>.
    /// </summary>
    /// <param name="error">The maximum error allowed before training stops.</param>
    public void SetMaxError(double error)
    {
      if (error >= 0)
        maxError = error;
      else
        throw new System.ArgumentOutOfRangeException(nameof(error), string.Format("{0} is less than 0!", nameof(error)));
    }

    /// <summary>
    /// A function for initializing the <see cref="NeuralNetwork"/>, after information has been
    /// set.
    /// </summary>
    public void InitializeNetwork()
    {
      // Get random weights and biases to start with.
      double output = NeuralMath.GetRandomDoubleIE(outputWeightRange.x, outputWeightRange.y);
      double input = NeuralMath.GetRandomDoubleIE(inputWeightRange.x, inputWeightRange.y);
      double bias = NeuralMath.GetRandomDoubleIE(biasRange.x, biasRange.y);

      // Create the neurons and initialize them.
      neurons = new Neuron[neuronCount];

      for (int i = 0; i < neuronCount; i++)
        neurons[i] = new Neuron(neuralFunction, derivedFunction, output, input, bias);
    }

    /// <summary>
    /// A function for running the <see cref="NeuralNetwork"/> to completion.
    /// </summary>
    public void RunNeuralNetwork()
    {
      int dataCount = data.TableSize;

      // Run the network for the maximum number of iterations.
      for (int i = 0; i < maxIterations; i++)
      {
        // Iterate through every data input.
        for (int m = 0; m < dataCount; m++)
        {
          ResultsData result = data[m];
          result.ActualOutput = 0.0f;

          // Iterate through every neuron.
          for (int n = 0; n < neuronCount; n++)
          {
            // Calculate the actual output and add it to the summation.
            Neuron neuron = neurons[n];
            result.ActualOutput += neuron.GetOutput(result.Input);
          }
        }

        // Iterate through every neuron again and adjust the weights and bias.
        for (int n = 0; n < neuronCount; n++)
        {
          // Calculate the actual output and add it to the summation.
          Neuron neuron = neurons[n];
          neuron.UpdateWeights(data, learningConstant);
        }

        if (CalculateSquaredError() <= maxError)
          break;
      }
    }

    /// <summary>
    /// A function for adding a piece of <see cref="ResultsData"/> to the network. This makes
    /// a copy of the <paramref name="result"/>.
    /// </summary>
    /// <param name="result">The <see cref="ResultsData"/> to add.</param>
    /// <returns>Returns if the data was added successfully.</returns>
    public bool AddData(ResultsData result)
    {
      return data.AddData(result);
    }

    /// <summary>
    /// A function for removing a piece of <see cref="ResultsData"/> from the network.
    /// </summary>
    /// <param name="result">The <see cref="ResultsData"/> to remove.</param>
    /// <returns>Returns if the data was removed successfully.</returns>
    public bool RemoveData(ResultsData result)
    {
      return data.RemoveData(result);
    }

    /// <summary>
    /// A function for removing a piece of <see cref="ResultsData"/> from the network.
    /// </summary>
    /// <param name="index">The index of the <see cref="ResultsData"/> to remove.</param>
    /// <returns>Returns if the data was removed successfully.</returns>
    public bool RemoveData(int index)
    {
      return data.RemoveData(index);
    }

    /// <summary>
    /// A function for calculating the squared error of the current iteration.
    /// </summary>
    /// <returns>Returns the calculated error.</returns>
    private double CalculateSquaredError()
    {
      int count = data.TableSize; // Get the number of data elements.
      double error = 0.0; // The error value.

      // Iterate through all values.
      for (int i = 0; i < count; i++)
      {
        // Add the squared difference between the expected and actual outputs.
        ResultsData result = data[i];
        double currentError = result.ExpectedOutput - result.ActualOutput;
        error += (currentError * currentError);
      }

      LastError = error / 2.0;
      return LastError;
    }
  }
  /************************************************************************************************/
}