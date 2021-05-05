/**************************************************************************************************/
/*!
\file   NeuralMath.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file containing several small math functions that can help with generating data in a
  Neural Network.

\par Bug List

\par References
*/
/**************************************************************************************************/

namespace NeuralNetworks.Kits
{
  /************************************************************************************************/
  /// <summary>
  /// A series of extra functions for help with generating <see cref="NeuralNetworks"/>.
  /// </summary>
  public static partial class NeuralMath
  {
    /// <summary>The standard random number generator for the program.</summary>
    private static System.Random Generator;

    /// <summary>
    /// The static constructor for the <see cref="NeuralMath"/>.
    /// </summary>
    static NeuralMath()
    {
      Generator = new System.Random(); // Initialize the generator.
    }

    /// <summary>
    /// A function to reset the <see cref="Generator"/> with a random seed.
    /// </summary>
    public static void ResetGenerator()
    {
      Generator = new System.Random();
    }

    /// <summary>
    /// A function to reset the <see cref="Generator"/> with a specific seed.
    /// </summary>
    /// <param name="seed">The new seed for the generator.</param>
    public static void ResetGenerator(int seed)
    {
      Generator = new System.Random(seed);
    }

    /// <summary>
    /// A function to get a random <see cref="double"/> within a range.
    /// </summary>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The barely exclusive maximum value.</param>
    /// <returns>Returns a random <see cref="double"/>.</returns>
    public static double GetRandomDoubleIE(double min, double max)
    {
      // Generate a random double and scale it.
      double value = Generator.NextDouble();
      return (max * value) + (min * (1d - value));
    }
  }
  /************************************************************************************************/
}