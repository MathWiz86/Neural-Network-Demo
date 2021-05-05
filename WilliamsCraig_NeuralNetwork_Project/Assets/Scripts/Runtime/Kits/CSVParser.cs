/**************************************************************************************************/
/*!
\file   CSVParser.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file for the implementation of a reader and writer for a basic CSV file, and turning it into
  data for a neural network.

\par Bug List

\par References
*/
/**************************************************************************************************/

using System.Text;

namespace NeuralNetworks.Kits
{
  /************************************************************************************************/
  /// <summary>
  /// A helper class for parsing and writing a .csv file with a <see cref="ResultsTable"/> for a
  /// <see cref="NeuralNetwork"/>.
  /// </summary>
  public static class NeuralCSVManager
  {
    /// <summary>The standard title bar that should appear.</summary>
    private static readonly string StandardTitleBar = "INPUT,EXPECTED OUTPUT,ACTUAL OUTPUT";
    /// <summary>The comma character that separates each portion fo the file.</summary>
    private static readonly char CommaDelimiter = ',';
    /// <summary>The number of portions there should be on each line.</summary>
    private static readonly int PortionCount = 3;
    /// <summary>An event called when the parse fails.</summary>
    private static System.Action<int, string> OnParseFailed;

    /// <summary>
    /// A function to bind an event to <see cref="OnParseFailed"/>.
    /// </summary>
    /// <param name="action">The action to bind.</param>
    public static void BindToOnParseFailed(System.Action<int, string> action)
    {
      OnParseFailed += action;
    }

    /// <summary>
    /// A function to unbind an event from <see cref="OnParseFailed"/>.
    /// </summary>
    /// <param name="action">The action to unbind.</param>
    public static void UnBindFromOnParseFailed(System.Action<int, string> action)
    {
      OnParseFailed -= action;
    }

    /// <summary>
    /// A function that parses a .csv file at a given <paramref name="filepath"/> and turns it
    /// into a <see cref="ResultsTable"/>.
    /// </summary>
    /// <param name="filepath">The path to the .csv file.</param>
    /// <returns>Returns the resulting <see cref="ResultsTable"/>. Returns null if the file
    /// is not valid.</returns>
    public static ResultsTable ReadNeuralData(string filepath)
    {
      // Get all data from the file, if possible.
      if (FileManager.ReadAllStringsFromFile(ref filepath, out string[] messages, true) && messages.Length > 0)
      {
        // The first line should be all titles. If not, don't move the index ahead by 1.
        int lineIndex = ParseTitle(messages[0]) ? 1 : 0;

        // The table is the size of the file, minus 1 if the title exists.
        ResultsTable table = new ResultsTable(messages.Length - lineIndex);
        int tableIndex = 0;

        // Iterate through all lines.
        for (; lineIndex < messages.Length; lineIndex++)
        {
          // Attempt to parse the line. If successful, increment the table index.
          ResultsData data = table[tableIndex];
          if (ParseCSVLine(messages[lineIndex], ref data))
            tableIndex++;
          else
          {
            // On failure, invoke the callback and return null.
            OnParseFailed?.Invoke(lineIndex, messages[lineIndex]);
            return null;
          }
        }

        return table; // Return the resulting table.
      }

      OnParseFailed?.Invoke(-1, string.Empty);
      return null; // Return null by default.
    }

    /// <summary>
    /// A function for taking a <see cref="ResultsTable"/> and writing it to a .csv file.
    /// </summary>
    /// <param name="data">The <see cref="ResultsTable"/> to write to a file.</param>
    /// <param name="filepath">The path to the .csv file. This is overwritten.</param>
    /// <returns>Returns if the data was successfully written.</returns>
    public static bool WriteNeuralDataToFile(ResultsTable data, string filepath)
    {
      // Make sure the data isn't null and the file exists once overwritten.
      if (data != null && FileManager.CreateFile(ref filepath, true, true))
      {
        string[] dataStrings = new string[data.TableSize + 1]; // Create the string array
        dataStrings[0] = StandardTitleBar; // Add the title first.

        // Iterate through all points of data.
        for (int i = 0; i < data.TableSize; i++)
        {
          // Get the data to write and turn it into a .csv string.
          ResultsData result = data[i];
          StringBuilder line = new StringBuilder(result.Input.ToString()).Append(CommaDelimiter);
          line.Append(result.ExpectedOutput.ToString()).Append(CommaDelimiter).Append(result.ActualOutput.ToString());

          dataStrings[i + 1] = line.ToString(); // Append the string.
        }

        // Return if the write is successful.
        return FileManager.AppendStringsToFileSafely(ref filepath, dataStrings);
      }

      return false; // By default, return false.
    }

    /// <summary>
    /// A helper function for parsing the title bar of the .csv, if there is one.
    /// </summary>
    /// <param name="title">The line to parse.</param>
    /// <returns>Returns if the title was found.</returns>
    private static bool ParseTitle(string title)
    {
      // Compare the titles.
      return title.ToUpper() == StandardTitleBar.ToUpper();
    }

    /// <summary>
    /// A function for parsing a single line in the .csv file.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="data">The <see cref="ResultsData"/> to put the parsed data into.</param>
    /// <returns>Returns if the line was successfully parsed.</returns>
    private static bool ParseCSVLine(string line, ref ResultsData data)
    {
      string[] portions = line.Split(CommaDelimiter); // Split by the comma delimiter.

      // Make sure there are only three portions.
      if (portions.Length == PortionCount)
        return ParseCSVPortion(portions[0], out data.Input) && ParseCSVPortion(portions[1], out data.ExpectedOutput) && ParseCSVPortion(portions[2], out data.ActualOutput);

      return false; // If not the right number of portions, return false.
    }

    /// <summary>
    /// A function for parsing a single portion of a single line in the .csv file.
    /// </summary>
    /// <param name="portion">The portion of the line to parse.</param>
    /// <param name="value">The resulting value of the parse.</param>
    /// <returns>Returns if the portion was successfully parsed.</returns>
    private static bool ParseCSVPortion(string portion, out double value)
    {
      // Attempt to parse the double.
      bool success = double.TryParse(portion, out value);

      // If successful return true immediately.
      if (success)
        return true;

      // If there's nothing added, default to 0 and return success.
      if (portion == null || portion == string.Empty)
      {
        value = 0.0;
        return true;
      }
        
      // If something happens, return false.
      return false;
    }
  }
  /************************************************************************************************/
}