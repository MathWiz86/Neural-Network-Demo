/**************************************************************************************************/
/*!
\file   NetworkSystem.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-14
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a single neuron within a Neural Network.

\par Bug List

\par References
*/
/**************************************************************************************************/

namespace NeuralNetworks
{
  /************************************************************************************************/
  /// <summary>
  /// A single neuron used in a larger <see cref="NeuralNetwork"/>.
  /// </summary>
  public class Neuron
  {
    /// <summary>The phi function used for the <see cref="Neuron"/>.</summary>
    private System.Func<double, double> neuralFunction;
    /// <summary>The deriviation of the <see cref="neuralFunction"/>.</summary>
    private System.Func<double, double> derivedFunction;

    /// <summary>The initial output weight of the <see cref="Neuron"/>, for reference.</summary>
    private double startOutputWeight;
    /// <summary>The initial input weight of the <see cref="Neuron"/>, for reference.</summary>
    private double startInputWeight;
    /// <summary>The initial bias of the <see cref="Neuron"/>, for reference.</summary>
    private double startBias;

    /// <summary>The current output weight of the <see cref="Neuron"/>.</summary>
    private double currentOutputWeight;
    /// <summary>The current input weight of the <see cref="Neuron"/>.</summary>
    private double currentInputWeight;
    /// <summary>The current bias of the <see cref="Neuron"/>.</summary>
    private double currentBias;

    /// <summary>
    /// A constructor for a <see cref="Neuron"/>.
    /// </summary>
    /// <param name="function">The phi function used for the <see cref="Neuron"/>.</param>
    /// <param name="derived">The deriviation of the <see cref="neuralFunction"/>.</param>
    /// <param name="outputWeight">The initial output weight of the <see cref="Neuron"/>.</param>
    /// <param name="inputWeight">The initial input weight of the <see cref="Neuron"/>.</param>
    /// <param name="bias">The initial bias of the <see cref="Neuron"/>.</param>
    public Neuron(System.Func<double, double> function, System.Func<double, double> derived, double outputWeight, double inputWeight, double bias)
    {
      neuralFunction = function;
      derivedFunction = derived;
      startOutputWeight = currentOutputWeight = outputWeight;
      startInputWeight = currentInputWeight = inputWeight;
      startBias = currentBias = bias;
    }

    /// <summary>
    /// A function to get the output value of a <see cref="Neuron"/>.
    /// </summary>
    /// <param name="data">The <see cref="ResultsData"/> to get the input from, and store the
    /// output into.</param>
    public void GetValue(ResultsData data)
    {
      // Get the value and store the output.
      data.ActualOutput = GetOutput(data.Input);
    }

    /// <summary>
    /// A function to get the output value of a <see cref="Neuron"/>.
    /// </summary>
    /// <param name="x">The input for the function.</param>
    /// <returns>Returns the <see cref="Neuron"/>'s output.</returns>
    public double GetOutput(double x)
    {
      // Return the output, based on the bias and weights.
      return currentOutputWeight * neuralFunction.Invoke(currentInputWeight * x + currentBias);
    }

    /// <summary>
    /// A function for updating the weights and bias of the <see cref="Neuron"/>, based on the
    /// current data.
    /// </summary>
    /// <param name="currentResults">The current <see cref="ResultsTable"/> for the
    /// <see cref="NeuralNetwork"/>.</param>
    /// <param name="learningConstant">The constant speed of learning.</param>
    public void UpdateWeights(ResultsTable currentResults, double learningConstant)
    {
      double output = 0.0f; // The summed output derivative.
      double input = 0.0f; // The summed input derivative.
      double bias = 0.0f; // The summed bias derivative.

      int size = currentResults.TableSize; // Get the size of the results.

      // Iterate through the entire table.
      for (int i = 0; i < size; i++)
      {
        ResultsData data = currentResults[i]; // Get the data.

        // Calculate the standard values used in all calculations.
        double difference = data.ActualOutput - data.ExpectedOutput;
        double funcInput = currentInputWeight * data.Input + currentBias;

        // Update the output.
        output += difference * neuralFunction.Invoke(funcInput);

        // Update the input.
        input += (currentOutputWeight * difference * derivedFunction.Invoke(funcInput)) * data.Input;

        // Update the bias.
        bias += (currentOutputWeight * difference * derivedFunction.Invoke(funcInput));
      }

      // Update the current weights and bias based on the summations and learning speed.
      currentOutputWeight -= (learningConstant * output);
      currentInputWeight -= (learningConstant * input);
      currentBias -= (learningConstant * bias);
    }
  }
  /************************************************************************************************/
}