/**************************************************************************************************/
/*!
\file   FileManager.cs
\author Craig Williams
\par    Unity Version
        2020.2.5
\par    Last Updated
        2021-04-18
\par    Copyright
        Copyright © 2021 Craig Joseph Williams, All Rights Reserved.

\brief
  A file containing several helpful functions for dealing with file I/O.

\par Bug List

\par References
*/
/**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace NeuralNetworks.Kits
{
  /************************************************************************************************/
  /// <summary>
  /// A type which contains information pointing to a file.
  /// </summary>
  [System.Serializable]
  public class FilePath
  {
    /// <summary> The directory of the file. This does not include the file's name and extension. </summary>
    public string directory;
    /// <summary> The file's name, including the extension. </summary>
    public string filename;

    /// <summary>
    /// A constructor for an FilePath. This passes in a full file path to use, which is broken up.
    /// </summary>
    /// <param name="filepath">The full filepath of the wanted file. This is broken up into its directory and filename.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned up. It is highly recommended to do so.</param>
    public FilePath(string filepath, bool cleanup = true)
    {
      FileManager.BreakupFilePath(filepath, out string dir, out string fi); // Break up the file path.

      // If cleaning up, clean up the file path.
      if (cleanup)
        FileManager.CleanupFilePath(ref dir, ref fi);

      // Set the information.
      directory = dir;
      filename = fi;
    }

    /// <summary>
    /// A constructor for an FilePath. This passes in the separated directory and filepath.
    /// </summary>
    /// <param name="d">The directory of the file.</param>
    /// <param name="f">The name and extension of the file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned up. It is highly recommended to do so.</param>
    public FilePath(string d, string f, bool cleanup = true)
    {
      // If cleaning up, clean up the file path.
      if (cleanup)
        FileManager.CleanupFilePath(ref d, ref f);

      // Set the information.
      directory = d;
      filename = f;
    }

    /// <summary>
    /// A function which converts information within the class into a string.
    /// </summary>
    /// <returns>Returns a representative string of the class's information.</returns>
    public override string ToString()
    {
      // Return a custom string representing the information inside.
      return "{ Directory: " + directory + ", File Name: " + filename + "}";
    }
  }
  /************************************************************************************************/
  /************************************************************************************************/
  /// <summary>
  /// A class contain several helpful functions for file I/O.
  /// </summary>
  public static partial class FileManager
  {
    /**********************************************************************************************/
    /// <summary>
    /// A struct containing information used for manipulating text files. Use these when using any of the file management functions.
    /// </summary>
    public struct ES_FileLine
    {
      /// <summary> The message for this line. </summary>
      public string line { get; private set; }
      /// <summary> The index of the line. This can never be less than 0. </summary>
      public long index { get; private set; }

      /// <summary>
      /// The constructor for a File Line.
      /// </summary>
      /// <param name="l">The string of this line. This is used for line insertion.</param>
      /// <param name="i">The number of this line. This is used for line insertion and reading.</param>
      public ES_FileLine(string l = "", long i = 0)
      {
        line = l != null ? l : string.Empty; // Enter either the string, or an empty string if none is provided.

        index = i < 0 ? 0 : i; // Enter the index, which cannot be less than 0.
      }

      /// <summary>
      /// A function which will set the text of this line.
      /// </summary>
      /// <param name="l">The new text of this line. If this is null, an empty string will be used.</param>
      public void SetLine(string l)
      {
        line = l != null ? l : string.Empty; // Update the line. If it is null, use an empty string instead.
      }

      /// <summary>
      /// A function which will set the index of this line.
      /// </summary>
      /// <param name="i">The new index. If this is less than 0, it will be set to 0.</param>
      public void SetIndex(long i)
      {
        index = i < 0 ? 0 : i; // Update the index. The index can never be less than 0.
      }
    }
    /**********************************************************************************************/

    /// <summary> An array of reserved words which cannnot be used in directories or file names. These are removed from paths upon cleanup </summary>
    public static readonly string[] file_ReservedWords = {"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2",
                                                          "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"};

    /// <summary> A shortcut variable to the game's current directory. </summary>
    public static string file_CurrentDirectory { get { return System.IO.Directory.GetCurrentDirectory(); } }
    /// <summary> The maximum line that can be accesed. This is used to prevent accidentally creating extremely large files. Change with caution. </summary>
    private const long file_MaxLineAccess = 1073742000;
    /// <summary> The maximum times a standard loop can occur in a routine before yielding. </summary>
    private const int file_MaxLoopCount = 1000;
    /// <summary> The maximum times a line can be accessed in a file in a routine before yielding. </summary>
    private const int file_MaxAccessLoopCount = 10000;
    /// <summary> The maximum times a byte can be written to a file in a frame before yielding. Change this at your own risk. </summary>
    private const int file_MaxWriteLoopCount = 20000;

    /// <summary>
    /// A function which takes an array of bytes and converts it into an object of a given type. Make sure the type is able to
    /// be acquired from the byte array. If not sure, pass in T as an object.
    /// </summary>
    /// <typeparam name="T">The type of the object the bytes will be converted to.</typeparam>
    /// <param name="bytes">The array of bytes to convert.</param>
    /// <param name="obj">The object which is represented by the bytes.</param>
    /// <returns>Returns if the byte array was successfully converted. If not, 'obj' will be the default value of 'T'.</returns>
    public static bool ConvertFromSerializedBytes<T>(byte[] bytes, out T obj)
    {
      obj = default; // Initialize the object.

      // If the byte array does not exist, immediately return false.
      if (bytes == null || bytes.Length <= 0)
        return false;

      // Create a binary and memory stream to read in all of the bytes.
      BinaryFormatter bstream = new BinaryFormatter();
      using (MemoryStream mstream = new MemoryStream())
      {
        mstream.Write(bytes, 0, bytes.Length);
        mstream.Seek(0, SeekOrigin.Begin);

        // Attempt to deserialize the bytes and convert to type T. If successful, return true. If false (i.e. an invalid cast, return false.
        try
        {
          obj = (T)bstream.Deserialize(mstream);

          return true;
        }
        catch
        {
          return false;
        }
      }
    }

    /// <summary>
    /// A function which takes an object and converts it into an array of bytes. Make sure the object is serializable.
    /// To do this, make sure the struct/enum/class is marked with the 'System.Serializable' property attribute.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert.</typeparam>
    /// <param name="obj">The object to convert to bytes.</param>
    /// <param name="bytes">The array of bytes representing the object.</param>
    /// <returns>Returns if the object was successfully converted or not.</returns>
    public static bool ConvertToSerializedBytes<T>(T obj, out byte[] bytes)
    {
      bytes = null; // Initialize the byte array.

      // Make sure the object is valid, and is serializable.
      if (obj != null && obj.GetType().IsSerializable)
      {
        // Create a binary stream and memory stream.
        BinaryFormatter bstream = new BinaryFormatter();
        using (MemoryStream mstream = new MemoryStream())
        {
          // Serialize the object into bytes.
          bstream.Serialize(mstream, obj);
          bytes = mstream.ToArray();
        }
        return true;
      }

      return false; // The object could not be serialized.
    }

    /// <summary>
    /// A function which combines several pieces of a full filepath together. If any pieces are illegal, nothing is returned.
    /// </summary>
    /// <param name="paths">The paths to combine together.</param>
    /// <returns>Returns the fixed up and combined path. If any pieces are illegal, nothing is returned.</returns>
    public static string CreateFilePath(string[] paths)
    {
      try
      {
        // Combine all the paths together and return it.
        return Path.Combine(paths);
      }
      catch
      {
        // If the paths are illegal, return the empty string.
        return string.Empty;
      }
    }

    /// <summary>
    /// A function which combines file information into a full filepath. If any pieces are illegal, nothing is returned.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <returns>Returns the fixed up and combined path. If any pieces are illegal, nothing is returned.</returns>
    public static string CreateFilePath(FilePath file)
    {
      // Create the file path with the given information.
      return file != null ? CreateFilePath(new string[] { file.directory, file.filename }) : string.Empty;
    }

    /// <summary>
    /// A function which attempts to create a temporary file.
    /// </summary>
    /// <param name="temp_path">The temporary file path. If an error occurs, this will be empty.</param>
    /// <returns>Returns if the temporary file was successfully created or not.</returns>
    public static bool CreateTempFilePath(out string temp_path)
    {
      try
      {
        // Attempt to get a temporary file path. One cannot be made if therea re already too many temp files.
        temp_path = Path.GetTempFileName();
        return true;
      }
      catch
      {
        // Upon any error, simply return an empty string.
        temp_path = string.Empty;
        return false;
      }
    }

    /// <summary>
    /// The internal function for attempting to create a directory. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="directory">The path of the directory to create. All subdirectories that don't exist are created as well.</param>
    /// <returns>Returns if the directory was successfully created or not.</returns>
    private static bool InternalCreateDirectory(ref string directory)
    {
      try
      {
        // Attempt to create the directory, and return if it was successfully created.
        DirectoryInfo info = Directory.CreateDirectory(directory);
        return info.Exists;
      }
      catch
      {
        // If an error occured, return false.
        return false;
      }
    }

    /// <summary>
    /// A function which will create a directory at the specified path.
    /// </summary>
    /// <param name="directory">The path of the directory to create. All subdirectories that don't exist are created as well.</param>
    /// <param name="cleanup">A bool determining if the path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the directory was successfully created or not.</returns>
    public static bool CreateDirectory(ref string directory, bool cleanup = false)
    {
      // Return false if there is no path.
      if (directory == string.Empty)
        return false;
      // Cleanup the path if requested.
      if (cleanup)
        CleanupDirectoryPath(ref directory);
      // Attempt to create the directory.
      return InternalCreateDirectory(ref directory);
    }

    /// <summary>
    /// A function which will create a directory at the specified path.
    /// </summary>
    /// <param name="file">The file to get the directory from.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the directory was successfully created or not.</returns>
    public static bool CreateDirectory(FilePath file, bool cleanup = false)
    {
      // Attempt to create the directory.
      return file != null ? CreateDirectory(ref file.directory, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to create a file at the specified path. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="directory">The directory of the file to create. This is used separately from the filepath.</param>
    /// <param name="filepath">The full path to the file.</param>
    /// <param name="create_directory">A bool determining if the directory should be created if it does not already exist.</param>
    /// <param name="overwrite">A bool determining if the file should be overwritten if it already exists.</param>
    /// <returns>Returns if the file was successfully created or not.</returns>
    private static bool InternalCreateFile(ref string directory, ref string filepath, bool create_directory, bool overwrite)
    {
      // If the directory does not exist, even after possibly creating it, return false immediately.
      if (!InternalCheckDirectory(ref directory, create_directory))
        return false;

      try
      {
        // If the file doesn't exist, or overwriting it, create the file.
        if (!File.Exists(filepath) || overwrite)
        {
          File.Create(filepath).Close(); // Close the file stream. We don't need it.
          return true; // The file was made.
        }
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }

      return false; // The file was not created.
    }

    /// <summary>
    /// A function which will create a file at the specified path.
    /// </summary>
    /// <param name="filepath">The full path to the file.</param>
    /// <param name="create_directory">A bool determining if the directory should be created if it does not already exist.</param>
    /// <param name="overwrite">A bool determining if the file should be overwritten if it already exists.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully created or not.</returns>
    public static bool CreateFile(ref string filepath, bool create_directory = false, bool overwrite = false, bool cleanup = false)
    {
      // If there is no file path, return false.
      if (filepath == string.Empty)
        return false;
      // Cleanup the file path if requested.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Break up the file path to get the directory.
      BreakupFilePath(filepath, out string dir, out string fi);
      // Attempt to create the file.
      return InternalCreateFile(ref dir, ref filepath, create_directory, overwrite);
    }

    /// <summary>
    /// A function which will create a file at the specified path.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file to create, extension included.</param>
    /// <param name="create_directory">A bool determining if the directory should be created if it does not already exist.</param>
    /// <param name="overwrite">A bool determining if the file should be overwritten if it already exists.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully created or not.</returns>
    public static bool CreateFile(ref string directory, ref string filename, bool create_directory = false, bool overwrite = false, bool cleanup = false)
    {
      // Cleanup the file path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      try
      {
        // Concatenate the path together and attempt to create the file.
        string path = Path.Combine(directory, filename);
        return InternalCreateFile(ref directory, ref path, create_directory, overwrite);
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }
    }

    /// <summary>
    /// A function which will create a file at the specified path.
    /// </summary>
    /// <param name="file">The file information to use to create the file.</param>
    /// <param name="create_directory">A bool determining if the directory should be created if it does not already exist.</param>
    /// <param name="overwrite">A bool determining if the file should be overwritten if it already exists.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully created or not.</returns>
    public static bool CreateFile(FilePath file, bool create_directory = false, bool overwrite = false, bool cleanup = false)
    {
      // Attempt to create the file.
      return file != null ? CreateFile(ref file.directory, ref file.filename, create_directory, overwrite, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to delete a file at the specified path. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The path of the file to delete.</param>
    /// <returns>Returns if the file was successfully deleted or not.</returns>
    private static bool InternalDeleteFile(ref string filepath)
    {
      try
      {
        // If the file exists, delete it and return true.
        if (File.Exists(filepath))
        {
          File.Delete(filepath);
          return true;
        }
        return false; // The file does not exist.
      }
      catch
      {
        // If an error occurs, return false.
        return false;
      }
    }

    /// <summary>
    /// A function which will attempt to delete a file at the specified path.
    /// </summary>
    /// <param name="filepath">The path of the file to delete.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned up. Do so if you haven't in the past.</param>
    /// <returns>Returns if the file was successfully deleted or not.</returns>
    public static bool DeleteFile(ref string filepath, bool cleanup = false)
    {
      // Cleanup the filepath if requested.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to delete the file.
      return InternalDeleteFile(ref filepath);
    }

    /// <summary>
    /// A function which will attempt to delete a file at the specified path.
    /// </summary>
    /// <param name="directory">The directory of the file to delete.</param>
    /// <param name="filename">The name of the file to delete.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned up. Do so if you haven't in the past.</param>
    /// <returns>Returns if the file was successfully deleted or not.</returns>
    public static bool DeleteFile(ref string directory, ref string filename, bool cleanup = false)
    {
      // Cleanup the filepath if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Create a path and attempt to delete the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return DeleteFile(ref path, false);
    }

    /// <summary>
    /// A function which will attempt to delete a file at the specified path.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned up. Do so if you haven't in the past.</param>
    /// <returns>Returns if the file was successfully deleted or not.</returns>
    public static bool DeleteFile(FilePath file, bool cleanup = false)
    {
      // Attempt to delete the file.
      return file != null ? DeleteFile(ref file.directory, ref file.filename, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to get a file's size, in bytes. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="size">The size of the file, in bytes. In the case of an error, this will return -1.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    private static bool InternalGetFileSize(ref string filepath, out long size)
    {
      size = -1; // Initialize the size to -1.

      try
      {
        // Check if the file exists.
        if (InternalCheckFile(ref filepath, false))
        {
          // Create a file stream to get the size.
          using (FileStream filestream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
          {
            // Get the file's size and return true.
            size = filestream.Length;
            return true;
          }
        }
        return false; // The file does not exist.
      }
      catch
      {
        // Upon any error, return false.
        return false;
      }
    }

    /// <summary>
    /// A function which gets a file's size, in bytes.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="size">The size of the file, in bytes. In the case of an error, this will return -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileSize(ref string filepath, out long size, bool cleanup = false)
    {
      // Cleanup the path, if requested.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to get the file's size.
      return InternalGetFileSize(ref filepath, out size);
    }

    /// <summary>
    /// A function which gets a file's size, in bytes.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="size">The size of the file, in bytes. In the case of an error, this will return -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileSize(ref string directory, ref string filename, out long size, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to get the file size.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalGetFileSize(ref path, out size);
    }

    /// <summary>
    /// A function which gets a file's size, in bytes.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="size">The size of the file, in bytes. In the case of an error, this will return -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileSize(FilePath file, out long size, bool cleanup = false)
    {
      size = -1; // Initialize the size.

      // Attempt to get the file size.
      return file != null ? GetFileSize(ref file.directory, ref file.filename, out size, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to get the number of lines stored in a file. A line is separated by a newline character. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="line_count">The number of lines in the file. An invalid file returns a -1.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    private static bool InternalGetFileLineCount(ref string filepath, out long line_count)
    {
      line_count = 0; // Initialize the size.
      // Check if the file exists.
      if (InternalCheckFile(ref filepath, false))
      {
        try
        {
          // Attempt to read every line one at a time. Once out of lines, we have the size.
          using (StreamReader stream_reader = new StreamReader(filepath))
          {
            while (stream_reader.ReadLine() != null)
              line_count++;
          }
          return true; // The file was successfully read.
        }
        catch
        {
          // In the event of an error, reset the count to -1 and return false.
          line_count = -1;
          return false;
        }
      }
      // The file does not exist. Set the count to -1 and return false.
      line_count = -1;
      return false;
    }

    /// <summary>
    /// A function which gets the number of lines stored in a file. A line is separated by a newline character.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="line_count">The number of lines in the file. An invalid file returns a -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileLineCount(ref string filepath, out long line_count, bool cleanup = false)
    {
      // If cleaning up, cleanup the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Get the line count of the file.
      return InternalGetFileLineCount(ref filepath, out line_count);
    }

    /// <summary>
    /// A function which gets the number of lines stored in a file. A line is separated by a newline character.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="line_count">The number of lines in the file. An invalid file returns a -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileLineCount(ref string directory, ref string filename, out long line_count, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to get the number of lines.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalGetFileLineCount(ref path, out line_count);
    }

    /// <summary>
    /// A function which gets the number of lines stored in a file. A line is separated by a newline character.
    /// </summary>
    /// <param name="file">The file path to go to.</param>
    /// <param name="line_count">The number of lines in the file. An invalid file returns a -1.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file exists, and was successfully accessed.</returns>
    public static bool GetFileLineCount(FilePath file, out long line_count, bool cleanup = false)
    {
      line_count = -1; // Initialize the size.
      // If the file information is valid, return the number of lines.
      return file != null ? GetFileLineCount(ref file.directory, ref file.filename, out line_count, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to copy a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath_from">The filepath of the file that is being copied.</param>
    /// <param name="filepath_to">The filepath that the file will be copied to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <returns>Returns if the file was successfully copied over.</returns>
    private static bool InternalCopyFile(ref string filepath_from, ref string filepath_to, bool overwrite)
    {
      // Check if the original file already exists.
      if (InternalCheckFile(ref filepath_from, false))
      {
        try
        {
          // Attempt to copy the file.
          File.Copy(filepath_from, filepath_to, overwrite);
          return true;
        }
        catch
        {
          // In the event of an error, return false.
          return false;
        }
      }
      // The original file does not exist. Return false.
      return false;
    }

    /// <summary>
    /// A function which copies a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="filepath_from">The filepath of the file that is being copied.</param>
    /// <param name="filepath_to">The filepath that the file will be copied to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully copied over.</returns>
    public static bool CopyFile(ref string filepath_from, ref string filepath_to, bool overwrite = false, bool cleanup = false)
    {
      // If cleaning up, clean up the two file paths.
      if (cleanup)
      {
        CleanupFilePath(ref filepath_from);
        CleanupFilePath(ref filepath_to);
      }
      // Attempt to copy the file over.
      return InternalCopyFile(ref filepath_from, ref filepath_to, overwrite);
    }

    /// <summary>
    /// A function which copies a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="directory_from">The directory of the file that is being copied.</param>
    /// <param name="filename_from">The filename of the file that is being copied, extension included.</param>
    /// <param name="directory_to">The directory that the file will be copied to.</param>
    /// <param name="filename_to">The filename that the file will be copied to, extension included.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully copied over.</returns>
    public static bool CopyFile(ref string directory_from, ref string filename_from, ref string directory_to, ref string filename_to, bool overwrite = false, bool cleanup = false)
    {
      // If cleaning up, clean up the file paths.
      if (cleanup)
      {
        CleanupFilePath(ref directory_from, ref filename_from);
        CleanupFilePath(ref directory_to, ref filename_to);
      }
      // Create the full filepaths for the two locations.
      string filepath_from = CreateFilePath(new string[] { directory_from, filename_from });
      string filepath_to = CreateFilePath(new string[] { directory_to, filename_to });
      // Attempt to copy the file.
      return InternalCopyFile(ref filepath_from, ref filepath_to, overwrite);
    }

    /// <summary>
    /// A function which copies a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="file_from">The file information of the file that is being copied.</param>
    /// <param name="file_to">The file information that the file will be copied to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully copied over.</returns>
    public static bool CopyFile(FilePath file_from, FilePath file_to, bool overwrite = false, bool cleanup = false)
    {
      // If both file information are valid, attempt to copy the file over.
      if (file_from != null && file_to != null)
        return CopyFile(ref file_from.directory, ref file_from.filename, ref file_to.directory, ref file_to.filename, overwrite, cleanup);

      return false; // Something was null, so return false.
    }

    /// <summary>
    /// The internal function for attempting to move a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath_from">The filepath of the file that is being moved.</param>
    /// <param name="filepath_to">The filepath that the file will be moved to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <returns>Returns if the file was successfully moved over.</returns>
    private static bool InternalMoveFile(ref string filepath_from, ref string filepath_to, bool overwrite)
    {
      // Check if the original file already exists.
      if (InternalCheckFile(ref filepath_from, false))
      {
        // If a file already exists at the original location, check if we can overwrite.
        if (InternalCheckFile(ref filepath_to, false))
        {
          // If overwriting, delete the old file. Else, return false immediately.
          if (overwrite)
            InternalDeleteFile(ref filepath_to);
          else
            return false;
        }

        try
        {
          // Attempt to move the file.
          File.Move(filepath_from, filepath_to);
          return true;
        }
        catch
        {
          // In the event of an error, return false.
          return false;
        }
      }
      // The original file does not exist. Return false.
      return false;
    }

    /// <summary>
    /// A function which moves a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="filepath_from">The filepath of the file that is being moved.</param>
    /// <param name="filepath_to">The filepath that the file will be moved to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully moved over.</returns>
    public static bool MoveFile(ref string filepath_from, ref string filepath_to, bool overwrite = false, bool cleanup = false)
    {
      // If cleaning up, clean up the file paths.
      if (cleanup)
      {
        CleanupFilePath(ref filepath_from);
        CleanupFilePath(ref filepath_to);
      }
      return InternalMoveFile(ref filepath_from, ref filepath_to, overwrite); // Attempt to move the file.
    }

    /// <summary>
    /// A function which moves a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="directory_from">The directory of the file that is being moved.</param>
    /// <param name="filename_from">The filename of the file that is being moved, extension included.</param>
    /// <param name="directory_to">The directory that the file will be moved to.</param>
    /// <param name="filename_to">The filename that the file will be moved to, extension included.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully moved over.</returns>
    public static bool MoveFile(ref string directory_from, ref string filename_from, ref string directory_to, ref string filename_to, bool overwrite = false, bool cleanup = false)
    {
      // If cleaning up, clean up the file paths.
      if (cleanup)
      {
        CleanupFilePath(ref directory_from, ref filename_from);
        CleanupFilePath(ref directory_to, ref filename_to);
      }
      // Create the full filepaths for the two locations.
      string filepath_from = CreateFilePath(new string[] { directory_from, filename_from });
      string filepath_to = CreateFilePath(new string[] { directory_to, filename_to });
      // Attempt to move the file.
      return InternalMoveFile(ref filepath_from, ref filepath_to, overwrite);
    }

    /// <summary>
    /// A function which moves a file from one filepath to another. This simply does some extra checks the standard function does not.
    /// </summary>
    /// <param name="file_from">The file information of the file that is being moved.</param>
    /// <param name="file_to">The file information that the file will be moved to.</param>
    /// <param name="overwrite">A bool determining that, if a file already exists at the 'to' filepath, it will be overwritten.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully moved over.</returns>
    public static bool MoveFile(FilePath file_from, FilePath file_to, bool overwrite = false, bool cleanup = false)
    {
      // If both file information are valid, attempt to move the file over.
      if (file_from != null && file_to != null)
        return MoveFile(ref file_from.directory, ref file_from.filename, ref file_to.directory, ref file_to.filename, overwrite, cleanup);

      return false; // Something was null, so return false.
    }

    /// <summary>
    /// The internal function for attempting to read byte data from a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The byte array to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    private static bool InternalReadBytesFromFile(ref string filepath, out byte[] bytes, int offset = 0, int data_count = 0)
    {
      bytes = null;

      try
      {
        // Make sure the file exists.
        if (InternalCheckFile(ref filepath, false))
        {
          // Using a file stream, read the bytes from the file.
          using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
          {
            // The stream needs to be more than 0 length or less than the max amount of data a list can read.
            if (stream.Length <= 0 || stream.Length > int.MaxValue || stream.Length - offset <= 0)
              return false;

            if (data_count == 0) // If the data count is 0, it signifies to read the whole file.
              data_count = (int)stream.Length;
            else if (data_count + offset > stream.Length) // If the data count plus the offset is greater than the file size, default to what data can be read.
              data_count = (int)stream.Length - offset;

            // Read in the data.
            bytes = new byte[data_count];
            stream.Read(bytes, offset, bytes.Length);
          }
          return true; // The file was successfully read.
        }
      }
      catch
      {
        // If any error occurs, return false.
        bytes = null;
        return false;
      }

      return false; // The file was not read.
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The byte array to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile(ref string filepath, out byte[] bytes, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      bytes = null; // Initialize the bytes array to be null.

      // If either the path is null, or the offsest or the data count are less than 0, this is an invalid read. Return false.
      if (filepath == null || offset < 0 || data_count < 0)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read the bytes from the file.
      return InternalReadBytesFromFile(ref filepath, out bytes, offset, data_count);
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="bytes">The byte array to send the data to.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile(ref string directory, ref string filename, out byte[] bytes, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadBytesFromFile(ref path, out bytes, offset, data_count);
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="bytes">The byte array to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile(FilePath file, out byte[] bytes, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      bytes = null; // Initialize the data.

      // Attempt to read the file.
      return file != null ? ReadBytesFromFile(ref file.directory, ref file.filename, out bytes, offset, data_count, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read byte data from a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    private static bool InternalReadBytesFromFile<T>(ref string filepath, out T obj, int offset = 0, int data_count = 0)
    {
      obj = default;
      // If we can read the bytes from the file, attempt to convert them into an object and return the object.
      if (InternalReadBytesFromFile(ref filepath, out byte[] bytes, offset, data_count))
        return ConvertFromSerializedBytes(bytes, out obj);

      return false; // The data could not be read.
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile<T>(ref string filepath, out T obj, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      // If cleaning up, clean up the file path.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read and convert the object.
      return InternalReadBytesFromFile(ref filepath, out obj, offset, data_count);
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="obj">The object to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile<T>(ref string directory, ref string filename, out T obj, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadBytesFromFile(ref path, out obj, offset, data_count);
    }

    /// <summary>
    /// A function which reads byte data from a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="obj">The object to send the data to.</param>
    /// <param name="offset">The byte count offset to start reading the file from. If unsure, leave at 0 to read from the start.</param>
    /// <param name="data_count">The amount of bytes to read from the file. If unsure, leave at 0 to read in the whole file.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad offset or data count will result in a false return.</returns>
    public static bool ReadBytesFromFile<T>(FilePath file, out T obj, int offset = 0, int data_count = 0, bool cleanup = false)
    {
      obj = default; // Initialize the object.

      // Attempt to read the file.
      return file != null ? ReadBytesFromFile(ref file.directory, ref file.filename, out obj, offset, data_count, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read all text in a file to a single string. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="text">The string to send the data to. If there is no text, this will return a null string.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    private static bool InternalReadAllTextFromFile(ref string filepath, out string text)
    {
      text = null; // Initialize the string.

      try
      {
        // See if the file exists.
        if (InternalCheckFile(ref filepath, false))
        {
          // Create a stream reader for the file.
          using (StreamReader read = new StreamReader(filepath))
          {
            text = read.ReadToEnd(); // Read all of the text in the file.
          }
        }
        // Return if the text was set or not.
        return (text != null);
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }
    }

    /// <summary>
    /// A function which reads all text in a file to a single string.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="text">The string to send the data to. If there is no text, this will return a null string.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadAllTextFromFile(ref string filepath, out string text, bool cleanup = false)
    {
      text = null; // Initialize the string.

      // If the filepath is null, return false.
      if (filepath == null)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read the entire file.
      return InternalReadAllTextFromFile(ref filepath, out text);
    }

    /// <summary>
    /// A function which reads all text in a file to a single string.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="text">The string to send the data to. If there is no text, this will return a null string.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadAllTextFromFile(ref string directory, ref string filename, out string text, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadAllTextFromFile(ref path, out text);
    }

    /// <summary>
    /// A function which reads all text in a file to a single string.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="text">The string to send the data to. If there is no text, this will return a null string.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadAllTextFromFile(FilePath file, out string text, bool cleanup = false)
    {
      text = string.Empty; // Initialize the string.

      // Attempt to read the file.
      return file != null ? ReadAllTextFromFile(ref file.directory, ref file.filename, out text, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read a specified string line from a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The string to send the data to. If there is no line, this will return a null string.</param>
    /// <param name="line_number">The line number to read. This starts at 0.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    private static bool InternalReadStringFromFile(ref string filepath, out string message, long line_number)
    {
      message = null; // Initialize the string.

      try
      {
        // See if the file exists.
        if (InternalCheckFile(ref filepath, false))
        {
          // Create a stream reader for the file.
          using (StreamReader read = new StreamReader(filepath))
          {
            // Skip ahead to the right line number.
            for (long i = 0; i < line_number; i++)
            {
              if (read.ReadLine() == null)
                return false;
            }

            // Read the line we want. At this point, we know that the line exists.
            message = read.ReadLine();
            return (message != null);
          }
        }
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }

      return false; // The file was not successfully read.
    }

    /// <summary>
    /// A function which reads a specified string line from a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The string to send the data to. If there is no line, this will return a null string.</param>
    /// <param name="line_number">The line number to read. This starts at 0.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringFromFile(ref string filepath, out string message, long line_number = 0, bool cleanup = false)
    {
      message = null; // Initialize the string.

      // If the filepath is null or the line number is bad, return false.
      if (filepath == null || line_number < 0 || line_number > file_MaxLineAccess)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read the string.
      return InternalReadStringFromFile(ref filepath, out message, line_number);
    }

    /// <summary>
    /// A function which reads a specified string line from a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="message">The string to send the data to. If there is no line, this will return a null string.</param>
    /// <param name="line_number">The line number to read. This starts at 0.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringFromFile(ref string directory, ref string filename, out string message, long line_number = 0, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadStringFromFile(ref path, out message, line_number);
    }

    /// <summary>
    /// A function which reads a specified string line from a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="message">The string to send the data to. If there is no line, this will return a null string.</param>
    /// <param name="line_number">The line number to read. This starts at 0.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringFromFile(FilePath file, out string message, long line_number = 0, bool cleanup = false)
    {
      message = string.Empty; // Initialize the string.

      // Attempt to read the file.
      return file != null ? ReadStringFromFile(ref file.directory, ref file.filename, out message, line_number, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read in specified strings from a file into an array.
    /// In the event of an error, such as the file being too short or inaccessible, a null array is returned.
    /// All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array to send the read strings to.</param>
    /// <param name="line_numbers">The line numbers to read from the file. The messages are returned in the same order.</param>
    /// <returns>Returns if the strings were successfully read. An error results in a false return and a null array.</returns>
    private static bool InternalReadStringsFromFile(ref string filepath, out string[] messages, IList<long> line_numbers)
    {
      messages = null;
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, false))
        {
          // Sort the lines from least to greatest.
          IList<long> sorted_lines = line_numbers.OrderBy(num => num).ToList();

          // If we are nullifying on an invalid line, and there are invalid line numbers, we can immediately return now.
          if (sorted_lines[sorted_lines.Count - 1] > file_MaxLineAccess)
            return false;

          // Create a number array of indexes, sorted to match the indexes sorted line numbers.
          int[] indexes = new int[line_numbers.Count()];
          for (int i = 0; i < line_numbers.Count; i++)
            indexes[i] = i;


          // Initialize the array to the amount of lines to read.
          messages = new string[line_numbers.Count()];

          using (StreamReader stream_reader = new StreamReader(filepath))
          {
            long current_line = 0; // The current line number.
            long current_index = 0; // The current index in the 'indexes' array. This is the index of the unsorted line number.
            long last_line = -100; // The previous line index searched.

            // Go through every line to get.
            foreach (int i in sorted_lines)
            {
              // If the wanted line matches the last line, simply copy the line over. This prevents having to reset the stream.
              if (i == last_line)
                messages[indexes[current_index]] = messages[indexes[current_index - 1]];
              else
              {
                // Read lines until the wanted line is reached.
                while (current_line < i)
                {
                  current_line++; // Increment the current line.

                  // If there are not enough lines, quit the function immediately.
                  if (stream_reader.ReadLine() == null)
                  {
                    messages = null;
                    return false;
                  }
                }

                current_line++; // Increment one more time.
                string line; // A temp storage for the wanted line.

                // If the wanted line exists, set it into the messages array at the proper index. Otherwise, quit.
                if ((line = stream_reader.ReadLine()) != null)
                  messages[indexes[current_index]] = line;
                else
                {
                  messages = null;
                  return false;
                }
              }

              last_line = i; // Set the last line.
              current_index++; // Increment the index of the 'indexes' array.
            }
          }
          return true; // The file was read.
        }
      }
      catch
      {
        // If any error occurs, return false.
        messages = null;
        return false;
      }
      return false; // The file was not appended to.
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being too short or inaccessible, a null array is returned.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array to send the read strings to.</param>
    /// <param name="line_numbers">The line numbers to read from the file. The messages are returned in the same order.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the strings were successfully read. An error results in a false return and a null array.</returns>
    public static bool ReadStringsFromFile(ref string filepath, out string[] messages, IList<long> line_numbers, bool cleanup = false)
    {
      messages = null; // Initialize the array.

      // If the filepath is null or there are no line numbers, return false.
      if (filepath == null || line_numbers == null || line_numbers.Count <= 0)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read all the strings from the file.
      return InternalReadStringsFromFile(ref filepath, out messages, line_numbers);
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being too short or inaccessible, a null array is returned.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="messages">The array to send the read strings to.</param>
    /// <param name="line_numbers">The line numbers to read from the file. The messages are returned in the same order.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the strings were successfully read. An error results in a false return and a null array.</returns>
    public static bool ReadStringsFromFile(ref string directory, ref string filename, out string[] messages, IList<long> line_numbers, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadStringsFromFile(ref path, out messages, line_numbers);
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being too short or inaccessible, a null array is returned.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="messages">The array to send the read strings to.</param>
    /// <param name="line_numbers">The line numbers to read from the file. The messages are returned in the same order.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the strings were successfully read. An error results in a false return and a null array.</returns>
    public static bool ReadStringsFromFile(FilePath file, out string[] messages, IList<long> line_numbers, bool cleanup = false)
    {
      messages = null; // Initialize the array.

      // Attempt to read the file.
      return file != null ? ReadStringsFromFile(ref file.directory, ref file.filename, out messages, line_numbers, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read in specified strings from a file into an array.
    /// In the event of an error, such as the file being inaccessible, a null array is returned.
    /// All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="line_start">The first line to start reading from the file. This is inclusive.</param>
    /// <param name="line_end">The last line to read from the file. This is exclusive.</param>
    /// <param name="allow_short_file">If true, if the file is shorter than 'line_end' lines, the array is shortened and returned normally.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    private static bool InternalReadStringsFromFile(ref string filepath, out string[] messages, int line_start, int line_end, bool allow_short_file = false)
    {
      messages = null;
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, false))
        {
          using (StreamReader stream_reader = new StreamReader(filepath))
          {
            // Read lines until reaching the start point.
            for (int i = 0; i < line_start; i++)
            {
              // If there aren't enough lines, return false.
              if (stream_reader.ReadLine() == null)
                return false;
            }

            messages = new string[line_end - line_start]; // Initialize the array, now that we're at a starting point.

            // Read every line from start to end.
            for (int i = line_start; i < line_end; i++)
            {
              string line;
              // If a line doesn't exist, the file is too short. Return false.
              if ((line = stream_reader.ReadLine()) != null)
                messages[i] = line;
              else
              {
                // If we allow a short file, resize the array to fit what was found.
                if (allow_short_file)
                  Array.Resize(ref messages, i - line_start);
                else
                  messages = null;

                return allow_short_file && messages.Length > 0; // Return the opposite of nullifying; if nullified, then nothing was read.
              }
            }
          }
          return true; // The file was successfully read.
        }
      }
      catch
      {
        // If any error occurs, return false.
        messages = null;
        return false;
      }
      // The file was not successfully read.
      return false;
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being inaccessible, a null array is returned.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="line_start">The first line to start reading from the file. This is inclusive.</param>
    /// <param name="line_end">The last line to read from the file. This is exclusive.</param>
    /// <param name="allow_short_file">If true, if the file is shorter than 'line_end' lines, the array is shortened and returned normally.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringsFromFile(ref string filepath, out string[] messages, int line_start, int line_end, bool allow_short_file = false, bool cleanup = false)
    {
      messages = null;

      // If the filepath or the indexes are bad, return false.
      if (filepath == null || line_start < 0 || line_end < line_start || line_end > file_MaxLineAccess)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read the specified lines from the file.
      return InternalReadStringsFromFile(ref filepath, out messages, line_start, line_end, allow_short_file);
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being inaccessible, a null array is returned.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="line_start">The first line to start reading from the file. This is inclusive.</param>
    /// <param name="line_end">The last line to read from the file. This is exclusive.</param>
    /// <param name="allow_short_file">If true, if the file is shorter than 'line_end' lines, the array is shortened and returned normally.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringsFromFile(ref string directory, ref string filename, out string[] messages, int line_start, int line_end, bool allow_short_file = false, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadStringsFromFile(ref path, out messages, line_start, line_end, allow_short_file);
    }

    /// <summary>
    /// A function which reads in specified strings from a file into an array.
    /// In the event of an error, such as the file being inaccessible, a null array is returned.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="line_start">The first line to start reading from the file. This is inclusive.</param>
    /// <param name="line_end">The last line to read from the file. This is exclusive.</param>
    /// <param name="allow_short_file">If true, if the file is shorter than 'line_end' lines, the array is shortened and returned normally.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. A bad line number will result in a false return.</returns>
    public static bool ReadStringsFromFile(FilePath file, out string[] messages, int line_start, int line_end, bool allow_short_file = false, bool cleanup = false)
    {
      messages = null; // Initialize the array.

      // Attempt to read the file.
      return file != null ? ReadStringsFromFile(ref file.directory, ref file.filename, out messages, line_start, line_end, allow_short_file, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to read an entire text file into a string array. All cleanup is handled in the public function.
    /// WARNING: Performing this takes longer the larger the file. If your file is large or has extremely long lines, a memory error will occur, resulting in a null return.
    /// If your file has more than 'file_MaxLineAccess' lines, the function will end early and return false.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <returns>Returns if the data was successfully read. Errors such as large file numbers will result in a false return.</returns>
    private static bool InternalReadAllStringsFromFile(ref string filepath, out string[] messages)
    {
      messages = null; // Initialize the array.

      try
      {
        // Check if the file exists.
        if (InternalCheckFile(ref filepath, false))
        {
          using (StreamReader stream = new StreamReader(filepath))
          {
            List<string> file_strings = new List<string>(); // The list of strings read from the file.

            // Read every line from start to end.
            while (true)
            {
              string line;
              // If a line doesn't exist, the file is too short. Return false.
              if ((line = stream.ReadLine()) != null)
              {
                // Make sure we didn't take in too much data. If clear, add the line to the list.
                if (file_strings.Count < (int)file_MaxLineAccess)
                  file_strings.Add(line);
                else
                  return false;
              }
              else
                break;
            }
            messages = file_strings.ToArray(); // After completion, convert the list into an array.
            return true; // The file was successfully read.
          }
        }
      }
      catch
      {
        return false; // In the event of an error, return false immediately.
      }

      return false; // The file was not successfully read.
    }

    /// <summary>
    /// A function which reads an entire text file into a string array.
    /// WARNING: Performing this takes longer the larger the file. If your file is large or has extremely long lines, a memory error will occur, resulting in a null return.
    /// If your file has more than 'file_MaxLineAccess' lines, the function will end early and return false.
    /// </summary>
    /// <param name="filepath">The path to the file to read.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. Errors such as large file numbers will result in a false return.</returns>
    public static bool ReadAllStringsFromFile(ref string filepath, out string[] messages, bool cleanup = false)
    {
      messages = null; // Initialize the array.
      // If the filepath doesn't exist, return false immediately.
      if (filepath == null)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to read all the lines in the file.
      return InternalReadAllStringsFromFile(ref filepath, out messages);
    }

    /// <summary>
    /// A function which reads an entire text file into a string array.
    /// WARNING: Performing this takes longer the larger the file. If your file is large or has extremely long lines, a memory error will occur, resulting in a null return.
    /// If your file has more than 'file_MaxLineAccess' lines, the function will end early and return false.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. Errors such as large file numbers will result in a false return.</returns>
    public static bool ReadAllStringsFromFile(ref string directory, ref string filename, out string[] messages, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to read the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalReadAllStringsFromFile(ref path, out messages);
    }

    /// <summary>
    /// A function which reads an entire text file into a string array.
    /// WARNING: Performing this takes longer the larger the file. If your file is large or has extremely long lines, a memory error will occur, resulting in a null return.
    /// If your file has more than 'file_MaxLineAccess' lines, the function will end early and return false.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="messages">The array of strings to send the data to.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the data was successfully read. Errors such as large file numbers will result in a false return.</returns>
    public static bool ReadAllStringsFromFile(FilePath file, out string[] messages, bool cleanup = false)
    {
      messages = null; // Initialize the array.

      // Attempt to read the file.
      return file != null ? ReadAllStringsFromFile(ref file.directory, ref file.filename, out messages, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append an array of bytes to a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendBytesToFile(ref string filepath, byte[] bytes, bool create_if_null = false)
    {
      try
      {
        // Make sure the file exists, or at least was created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Using a file stream, append the bytes to the file.
          using (FileStream stream = new FileStream(filepath, FileMode.Append, FileAccess.Write))
            stream.Write(bytes, 0, bytes.Length);

          return true; // The file was successfully appended to.
        }
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }

      return false; // The file was not appended to.
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile(ref string filepath, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // If any of the passed-in information is invalid, return false immediately.
      if (filepath == null || bytes == null || bytes.Length <= 0)
        return false;
      // If cleaning up, clean up the file path.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to append to the file.
      return InternalAppendBytesToFile(ref filepath, bytes, create_if_null);
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile(ref string directory, ref string filename, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // Cleanup the path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to append the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendBytesToFile(ref path, bytes, create_if_null);
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile(FilePath file, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the bytes to the file.
      return file != null ? AppendBytesToFile(ref file.directory, ref file.filename, bytes, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append an array of bytes to a given file. All cleanup is handled in the public function.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendBytesToFileSafely(ref string filepath, byte[] bytes, bool create_if_null = false)
    {
      // If a temporary file cannot be made, return false immediately.
      if (!CreateTempFilePath(out string temp_path))
        return false;

      try
      {
        // Make sure the file exists, or at least was created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to copy the file to the temporary file.
          if (InternalCopyFile(ref filepath, ref temp_path, true))
          {
            // Using a file stream, append the bytes to the file.
            using (FileStream filestream_temp = new FileStream(temp_path, FileMode.Append, FileAccess.Write))
              filestream_temp.Write(bytes, 0, bytes.Length);

            return InternalMoveFile(ref temp_path, ref filepath, true); // Move the file from the temporary location to the original location.
          }

          return false; // The file could not be copied.
        }
      }
      catch
      {
        // If any error occurs, return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file could not be appended to.
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely(ref string filepath, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // If the filepath or bytes do not exist, return false immediately.
      if (filepath == null || bytes == null || bytes.Length <= 0)
        return false;
      // If cleaning up, clean up the file path.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to safely append the bytes to the file.
      return InternalAppendBytesToFileSafely(ref filepath, bytes, create_if_null);
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely(ref string directory, ref string filename, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // Cleanup the path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to append the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendBytesToFileSafely(ref path, bytes, create_if_null);
    }

    /// <summary>
    /// A function which appends an array of bytes to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely(FilePath file, byte[] bytes, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the bytes to the file.
      return file != null ? AppendBytesToFileSafely(ref file.directory, ref file.filename, bytes, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to convert an object into an array of bytes before appending it to a given file. All cleanup is handled in the public function.
    /// The object MUST be fully serializable for this to work.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendBytesToFile<T>(ref string filepath, T obj, bool create_if_null = false)
    {
      // Check that the filepath is valid and attempt to convert the object to bytes.
      if (!ConvertToSerializedBytes(obj, out byte[] bytes))
        return false;
      // Attempt to append the byte array to the file.
      return InternalAppendBytesToFile(ref filepath, bytes, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile<T>(ref string filepath, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Check that the filepath and object are valid.
      if (filepath == null)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to append the object.
      return InternalAppendBytesToFile(ref filepath, obj, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile<T>(ref string directory, ref string filename, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Cleanup the path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to append the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendBytesToFile(ref path, obj, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFile<T>(FilePath file, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the bytes to the file.
      return file != null ? AppendBytesToFile(ref file.directory, ref file.filename, obj, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to convert an object an object into an array of bytes before appending it to a given file. All cleanup is handled in the public function.
    /// The object MUST be fully serializable for this to work.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendBytesToFileSafely<T>(ref string filepath, T obj, bool create_if_null = false)
    {
      // Check that the filepath is valid and attempt to convert the object to bytes.
      if (!ConvertToSerializedBytes(obj, out byte[] bytes))
        return false;
      // Attempt to append the bytes.
      return InternalAppendBytesToFileSafely(ref filepath, bytes, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely<T>(ref string filepath, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Check that the filepath is valid and attempt to convert the object to bytes.
      if (filepath == null)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to append the object to the file.
      return InternalAppendBytesToFileSafely(ref filepath, obj, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely<T>(ref string directory, ref string filename, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Cleanup the path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to append the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendBytesToFileSafely(ref path, obj, create_if_null);
    }

    /// <summary>
    /// A function which converts an object into an array of bytes before appending it to a given file.
    /// The object MUST be fully serializable for this to work.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="obj">The object to append. This object must be serializable.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendBytesToFileSafely<T>(FilePath file, T obj, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the bytes to the file.
      return file != null ? AppendBytesToFileSafely(ref file.directory, ref file.filename, obj, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append a string to a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendStringToFile(ref string filepath, string message, bool append_newline = true, bool create_if_null = false)
    {
      try
      {
        // See if the file exsists, or does so after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Using a StreamWriter, write to the file. 'WriteLine' automatically adds the newline.
          using (StreamWriter stream = new StreamWriter(filepath, true))
          {
            if (append_newline)
              stream.WriteLine(message);
            else
              stream.Write(message);
          }
          // The message was successfully appended to.
          return true;
        }
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }
      // The file was not successfully appended to.
      return false;
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFile(ref string filepath, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // If the filepath or message are null, return false immediately.
      if (filepath == null || message == null)
        return false;
      // If cleaning up, clean up the file path.
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Attempt to append the string to the file.
      return InternalAppendStringToFile(ref filepath, message, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFile(ref string directory, ref string filename, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      if (directory == null || filename == null || message == null)
        return false;
      // Cleanup the file path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to append the string to the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendStringToFile(ref path, message, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFile(FilePath file, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the string to the file.
      return file != null ? AppendStringToFile(ref file.directory, ref file.filename, message, append_newline, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append a string to a given file. All cleanup is handled in the public function.
    /// This version will first create a temporary file and attempt to append the string before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendStringToFileSafely(ref string filepath, string message, bool append_newline = true, bool create_if_null = false)
    {
      // If a temporary file cannot be made, return false immediately.
      if (!CreateTempFilePath(out string temp_path))
        return false;

      try
      {
        // Make sure the file exists, or at least was created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to copy the file to the temporary file.
          if (InternalCopyFile(ref filepath, ref temp_path, true))
          {
            // Using a file stream, append the string to the file.
            using (StreamWriter streamwriter_temp = new StreamWriter(temp_path, true))
            {
              // If appending a newline, use WriteLine. Otherwise, just use Write.
              if (append_newline)
                streamwriter_temp.WriteLine(message);
              else
                streamwriter_temp.Write(message);
            }

            return InternalMoveFile(ref temp_path, ref filepath, true); // Move the file from the temporary location to the original location.
          }

          return false; // The file could not be copied.
        }
      }
      catch
      {
        // If any error occurs, return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file could not be appended to.
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFileSafely(ref string filepath, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // If the filepath or message do not exist, return false immediately.
      if (filepath == null || message == null)
        return false;
      // If cleaning up, clean up the filepath.
      if (cleanup)
        CleanupFilePath(ref filepath);
      return InternalAppendStringToFileSafely(ref filepath, message, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFileSafely(ref string directory, ref string filename, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Cleanup the file path if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to append the string to the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendStringToFileSafely(ref path, message, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends a string to a given file.
    /// This version will first create a temporary file and attempt to append the bytes before overwriting the original file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="message">The message to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringToFileSafely(FilePath file, string message, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the string to the file.
      return file != null ? AppendStringToFileSafely(ref file.directory, ref file.filename, message, append_newline, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append several strings to a given file. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendStringsToFile(ref string filepath, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false)
    {
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Using a stream writer, write each individual line to the file. 'WriteLine' appends a newline automatically.
          using (StreamWriter stream = new StreamWriter(filepath, true))
          {
            foreach (string m in messages)
            {
              if (m != string.Empty)
              {
                if (append_newline)
                  stream.WriteLine(m);
                else
                  stream.Write(m);
              }
            }
          }

          return true; // The file was appended to.
        }
      }
      catch
      {
        // If any error occurs, return false.
        return false;
      }

      return false; // The file was not appended to.
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFile(ref string filepath, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      if (filepath == null || messages == null)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      return InternalAppendStringsToFile(ref filepath, messages, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFile(ref string directory, ref string filename, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to append to the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendStringsToFile(ref path, messages, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFile(FilePath file, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the strings to the file.
      return file != null ? AppendStringsToFile(ref file.directory, ref file.filename, messages, append_newline, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to append several strings to a given file. All cleanup is handled in the public function.
    /// This version will first create a temporary file and attempt to append the strings before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    private static bool InternalAppendStringsToFileSafely(ref string filepath, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false)
    {
      // If a temporary file cannot be made, return false immediately.
      if (!CreateTempFilePath(out string temp_path))
        return false;

      try
      {
        // Make sure the file exists, or at least was created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to copy the file to the temporary file.
          if (InternalCopyFile(ref filepath, ref temp_path, true))
          {
            // Using a file stream, append the strings to the file.
            using (StreamWriter streamwriter_temp = new StreamWriter(temp_path, true))
            {
              foreach (string m in messages)
              {
                if (m != string.Empty)
                {
                  if (append_newline)
                    streamwriter_temp.WriteLine(m);
                  else
                    streamwriter_temp.Write(m);
                }
              }
            }

            return InternalMoveFile(ref temp_path, ref filepath, true); // Move the file from the temporary location to the original location.
          }

          return false; // The file could not be copied.
        }
      }
      catch
      {
        // If any error occurs, return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file could not be appended to.
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// This version will first create a temporary file and attempt to append the strings before overwriting the original file.
    /// </summary>
    /// <param name="filepath">The file path to go to.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFileSafely(ref string filepath, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      if (filepath == null || messages == null)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      return InternalAppendStringsToFileSafely(ref filepath, messages, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// This version will first create a temporary file and attempt to append the strings before overwriting the original file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFileSafely(ref string directory, ref string filename, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to append to the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalAppendStringsToFileSafely(ref path, messages, append_newline, create_if_null);
    }

    /// <summary>
    /// A function which appends several strings to a given file.
    /// This version will first create a temporary file and attempt to append the strings before overwriting the original file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="messages">The messages to append to the file.</param>
    /// <param name="append_newline">A bool determining if a terminating newline should be added to the message.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully appended to.</returns>
    public static bool AppendStringsToFileSafely(FilePath file, IEnumerable<string> messages, bool append_newline = true, bool create_if_null = false, bool cleanup = false)
    {
      // Attempt to append the strings to the file.
      return file != null ? AppendStringsToFileSafely(ref file.directory, ref file.filename, messages, append_newline, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// A helper function which inserts a line into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// This version will create empty lines in the new file if the insertion is for a line that doesn't exist.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The line and placement to insert into the file. This will insert the line at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalInsertStringToFileNull(ref string filepath, ES_FileLine insertion, bool create_if_null = false)
    {
      // Create a temporary file. If something goes wrong, we don't edit the first file.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {

            using (StreamReader streamreader_original = new StreamReader(filepath)) // The reader for the original file.
            {
              using (StreamWriter streamreader_temp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                // Read up to the wanted index. Write all lines to the temp file. If a line doesn't exist, just write an empty line.
                for (long i = 0; i < insertion.index; i++)
                {
                  string line = string.Empty;
                  if ((line = streamreader_original.ReadLine()) != null)
                    streamreader_temp.WriteLine(line);
                  else
                    streamreader_temp.WriteLine(string.Empty);
                }

                // Insert the new line.
                streamreader_temp.WriteLine(insertion.line);

                // Write in all remaining lines.
                while (!streamreader_original.EndOfStream)
                {
                  streamreader_temp.WriteLine(streamreader_original.ReadLine());
                }
              }
            }

            // Delete the old file, and move the temp file in as the old file.
            InternalMoveFile(ref temp_path, ref filepath, true);
            return true;
          }
        }
      }
      catch
      {
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false;
    }

    /// <summary>
    /// A helper function which inserts a line into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// This version will not insert if the line number does not exist in the original file.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The line and placement to insert into the file. This will insert the line at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalInsertStringToFileNoNull(ref string filepath, ES_FileLine insertion, bool create_if_null = false)
    {
      // Create a temporary file. If something goes wrong, we don't edit the first file.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {
            using (StreamReader soriginal = new StreamReader(filepath)) // The reader for the original file.
            {
              bool temp_good = true; // A bool determining if the temp can be moved over, or if it should be deleted.

              using (StreamWriter stemp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                // Read up to the wanted index. Write all lines to the temp file.
                for (long i = 0; i < insertion.index; i++)
                {
                  // If the current line is not null, add it to the temp file. Otherwise, this means we cannot insert the line.
                  string line = string.Empty;
                  if ((line = soriginal.ReadLine()) != null)
                    stemp.WriteLine(line);
                  else
                  {
                    temp_good = false;
                    break;
                  }
                }

                if (temp_good)
                {
                  // Write the inserted line.
                  stemp.WriteLine(insertion.line);

                  // Write the rest of the file to the temporary file.
                  while (!soriginal.EndOfStream)
                  {
                    stemp.WriteLine(soriginal.ReadLine());
                  }
                }
              }

              if (!temp_good)
              {
                InternalDeleteFile(ref temp_path);
                return false;
              }
            }

            // Delete the old file and move in the new file.
            return InternalMoveFile(ref temp_path, ref filepath, true);
          }
        }
      }
      catch
      {
        // If any error occurs, delete the temp file if it was made and return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file was not successfully inserted into.
    }

    /// <summary>
    /// A function which inserts a given line into a file. This function creates a temporary file to insert the line.
    /// When inserting, the function will make the given sentence as the given index.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The line and placement to insert into the file. This will insert the line at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringToFile(ref string filepath, ES_FileLine insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      if (filepath == null)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      // Insert based on whether or not we can insert into the file while null lines are found.
      if (insert_if_null)
        return InternalInsertStringToFileNull(ref filepath, insertion, create_if_null);
      else
        return InternalInsertStringToFileNoNull(ref filepath, insertion, create_if_null);
    }

    /// <summary>
    /// A function which inserts a given line into a file. This function creates a temporary file to insert the line.
    /// When inserting, the function will make the given sentence as the given index.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="insertion">The line and placement to insert into the file. This will insert the line at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringToFile(ref string directory, ref string filename, ES_FileLine insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to append to the file.
      string path = CreateFilePath(new string[] { directory, filename });
      // Insert based on whether or not we can insert into the file while null lines are found.
      if (insert_if_null)
        return InternalInsertStringToFileNull(ref path, insertion, create_if_null);
      else
        return InternalInsertStringToFileNoNull(ref path, insertion, create_if_null);
    }

    /// <summary>
    /// A function which inserts a given line into a file. This function creates a temporary file to insert the line.
    /// When inserting, the function will make the given sentence as the given index.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="insertion">The line and placement to insert into the file. This will insert the line at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringToFile(FilePath file, ES_FileLine insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      // Attempt to insert a line into the file.
      return file != null ? InsertStringToFile(ref file.directory, ref file.filename, insertion, create_if_null, insert_if_null, cleanup) : false;
    }

    /// <summary>
    /// A helper function which inserts several lines into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// This version will create empty lines in the new file if the insertion is for a line that doesn't exist.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The lines and placements to insert into the file. This will insert the lines at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalInsertStringsToFileNull(ref string filepath, IList<ES_FileLine> insertion, bool create_if_null = false)
    {
      // If we cannot create a temp file, return false.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {
            // Sort the lines from least to greatest.
            IEnumerable<ES_FileLine> sorted_lines = insertion.OrderBy(line => line.index);

            // We do not want to process this if trying to insert past the maximum file line.
            if (sorted_lines.Last().index > file_MaxLineAccess)
              return false;

            using (StreamReader soriginal = new StreamReader(filepath)) // The reader for the original file.
            {
              using (StreamWriter stemp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                long current_line = 0; // The current line number.
                long last_line = -100; // The previous line index searched.

                foreach (ES_FileLine line in sorted_lines)
                {
                  // Simply insert the line again if we already have the current index.
                  if (last_line == line.index)
                    stemp.WriteLine(line.line);
                  else
                  {
                    // Read up to the wanted index. Write all lines to the temp file. If a line doesn't exist, just write an empty line.
                    while (current_line < line.index)
                    {
                      string message = string.Empty;
                      if ((message = soriginal.ReadLine()) != null)
                        stemp.WriteLine(message);
                      else
                        stemp.WriteLine(string.Empty);

                      current_line++;
                    }

                    stemp.WriteLine(line.line);
                    last_line = line.index;
                  }
                }

                // Write in all remaining lines.
                while (!soriginal.EndOfStream)
                {
                  string message = string.Empty;
                  if ((message = soriginal.ReadLine()) != null)
                    stemp.WriteLine(message);
                }
              }
            }

            // Delete the old file, and move the temp file in as the old file.
            return InternalMoveFile(ref temp_path, ref filepath, true);
          }
        }
      }
      catch
      {
        // Delete the temporary file if it existed.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false;
    }

    /// <summary>
    /// A helper function which inserts several lines into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// This version will not insert if the line number does not exist in the original file.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The lines and placements to insert into the file. This will insert the lines at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalInsertStringsToFileNoNull(ref string filepath, IList<ES_FileLine> insertion, bool create_if_null = false)
    {
      // If we cannot create a temp file, return false.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, create_if_null))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {
            // Sort the lines in order from least to greatest.
            IEnumerable<ES_FileLine> sorted_lines = insertion.OrderBy(line => line.index);

            // We don't want to go past the max access point, just in case. Return false if the attempt is made.
            if (sorted_lines.Last().index > file_MaxLineAccess)
              return false;

            using (StreamReader soriginal = new StreamReader(filepath)) // The reader for the original file.
            {
              bool temp_good = true; // A bool determining if the temp can be moved over, or if it should be deleted.

              using (StreamWriter stemp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                long current_line = 0; // The current line number.
                long last_line = -100; // The previous line index searched.

                foreach (ES_FileLine line in sorted_lines)
                {
                  // Simply insert the line again if we already have the current index.
                  if (last_line == line.index)
                    stemp.WriteLine(line.line);
                  else
                  {
                    // Read up to the wanted index. Write all lines to the temp file. If a line doesn't exist, we want to quit this insertion.
                    while (current_line < line.index)
                    {
                      string message = string.Empty;
                      if ((message = soriginal.ReadLine()) != null)
                        stemp.WriteLine(message);
                      else
                      {
                        temp_good = false;
                        break;
                      }

                      current_line++; // Increment the line count.
                    }

                    if (!temp_good)
                      break;

                    stemp.WriteLine(line.line);
                    last_line = line.index;
                  }
                }

                if (temp_good)
                {
                  // Write in all remaining lines.
                  while (!soriginal.EndOfStream)
                  {
                    string message = string.Empty;
                    if ((message = soriginal.ReadLine()) != null)
                      stemp.WriteLine(message);
                  }
                }
              }

              if (!temp_good)
              {
                InternalDeleteFile(ref temp_path);
                return false;
              }
            }

            // Delete the old file, and move the temp file in as the old file.
            InternalMoveFile(ref temp_path, ref filepath, true);
            return true;
          }
        }
      }
      catch
      {
        // Delete the temporary file if it existed.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false;
    }

    /// <summary>
    /// A function which inserts several lines into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// When inserting, the function will insert at the indexes of the UNEDITED file.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="insertion">The lines and placements to insert into the file. This will insert the lines at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringsToFile(ref string filepath, IList<ES_FileLine> insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      if (filepath == null)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      if (insert_if_null)
        return InternalInsertStringsToFileNull(ref filepath, insertion, create_if_null);
      else
        return InternalInsertStringsToFileNoNull(ref filepath, insertion, create_if_null);
    }

    /// <summary>
    /// A function which inserts several lines into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// When inserting, the function will insert at the indexes of the UNEDITED file.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="insertion">The lines and placements to insert into the file. This will insert the lines at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringsToFile(ref string directory, ref string filename, IList<ES_FileLine> insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);
      // Concatenate the path and attempt to insert into the file.
      string path = CreateFilePath(new string[] { directory, filename });
      if (insert_if_null)
        return InternalInsertStringsToFileNull(ref path, insertion, create_if_null);
      else
        return InternalInsertStringsToFileNoNull(ref path, insertion, create_if_null);
    }

    /// <summary>
    /// A function which inserts several lines into a file at a designated place.
    /// This function creates a temporary file to insert the line.
    /// When inserting, the function will insert at the indexes of the UNEDITED file.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="insertion">The lines and placements to insert into the file. This will insert the lines at the index listed.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it does not already exist.</param>
    /// <param name="insert_if_null">A bool determining if the line should still be inserted if the old file is not long enough. If true, empty lines will be added
    /// until reaching the desired line.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool InsertStringsToFile(FilePath file, IList<ES_FileLine> insertion, bool create_if_null = false, bool insert_if_null = false, bool cleanup = false)
    {
      return file != null ? InsertStringsToFile(ref file.directory, ref file.filename, insertion, create_if_null, insert_if_null, cleanup) : false;
    }

    /// <summary>
    /// The internal function for attempting to edit a line in a file. All cleanup is handled in the public function.
    /// This function creates a temporary file to edit the line.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="edited_line">The index of the line and what to replace it with.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalEditStringInFile(ref string filepath, ES_FileLine edited_line)
    {
      // Create a temporary file. If something goes wrong, we don't edit the first file.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, false))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {
            using (StreamReader soriginal = new StreamReader(filepath)) // The reader for the original file.
            {
              bool temp_good = true; // A bool determining if the temp can be moved over, or if it should be deleted.

              using (StreamWriter stemp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                // Read up to the wanted index. Write all lines to the temp file.
                for (long i = 0; i < edited_line.index; i++)
                {
                  // If the current line is not null, add it to the temp file. Otherwise, this means we cannot insert the line.
                  string line = string.Empty;
                  if ((line = soriginal.ReadLine()) != null)
                    stemp.WriteLine(line);
                  else
                  {
                    temp_good = false;
                    break;
                  }
                }

                // If the temporary file is still valid, write the edited line.
                if (temp_good)
                {

                  stemp.WriteLine(edited_line.line); // Write the edited line.
                  soriginal.ReadLine(); // Read ahead once to skip the overwritten line.
                  // Write the rest of the file to the temporary file.
                  while (!soriginal.EndOfStream)
                    stemp.WriteLine(soriginal.ReadLine());
                }
              }

              // If the temporary file is not valid, delete the temporary file.
              if (!temp_good)
              {
                InternalDeleteFile(ref temp_path);
                return false;
              }
            }

            // Delete the old file and move in the new file.
            return InternalMoveFile(ref temp_path, ref filepath, true);
          }
        }
      }
      catch
      {
        // If any error occurs, delete the temp file if it was made and return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file was not successfully inserted into.
    }

    /// <summary>
    /// A function which edits a line in a file.
    /// This function creates a temporary file to edit the line.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="edited_line">The index of the line and what to replace it with.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringInFile(ref string filepath, ES_FileLine edited_line, bool cleanup = false)
    {
      // If the filepath or index is bad, return false.
      if (filepath == null || edited_line.index < 0 || edited_line.index > file_MaxLineAccess)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      return InternalEditStringInFile(ref filepath, edited_line);
    }

    /// <summary>
    /// A function which edits a line in a file.
    /// This function creates a temporary file to edit the line.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="edited_line">The index of the line and what to replace it with.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringInFile(ref string directory, ref string filename, ES_FileLine edited_line, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to edit the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalEditStringInFile(ref path, edited_line);
    }

    /// <summary>
    /// A function which edits a line in a file.
    /// This function creates a temporary file to edit the line.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="edited_line">The index of the line and what to replace it with.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringInFile(FilePath file, ES_FileLine edited_line, bool cleanup = false)
    {
      // Attempt to edit the file.
      return file != null ? EditStringInFile(ref file.directory, ref file.filename, edited_line, cleanup) : false;
    }

    /// <summary>
    /// A function which edits several lines in a file.
    /// This function creates a temporary file to edit the lines.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="edited_lines">The lines to be edited. If multiple lines have the same index, the function will only use the first version of the edit.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    private static bool InternalEditStringsInFile(ref string filepath, IEnumerable<ES_FileLine> edited_lines)
    {
      // Create a temporary file. If something goes wrong, we don't edit the first file.
      if (!CreateTempFilePath(out string temp_path))
        return false;
      BreakupFilePath(temp_path, out string temp_dir, out string temp_fname);
      try
      {
        // Check if the file exists, or does after being created.
        if (InternalCheckFile(ref filepath, false))
        {
          // Attempt to create the temporary file.
          if (InternalCreateFile(ref temp_dir, ref temp_path, true, true))
          {
            IEnumerable<ES_FileLine> sorted_lines = edited_lines.OrderBy(line => line.index); // Sort the lines.

            // We don't want to go past the max access point, just in case. Return false if the attempt is made.
            if (sorted_lines.Last().index > file_MaxLineAccess)
              return false;

            using (StreamReader soriginal = new StreamReader(filepath)) // The reader for the original file.
            {
              bool temp_good = true; // A bool determining if the temp can be moved over, or if it should be deleted.

              using (StreamWriter stemp = new StreamWriter(temp_path)) // The writer for the temp file.
              {
                long current_line = 0; // The current line number.
                long last_line = -100; // The previous line index searched. Start this at the first editing line.
                string edit = string.Empty;

                foreach (ES_FileLine line in sorted_lines)
                {
                  // If already at the end of stream, we just want to stop here.
                  if (soriginal.EndOfStream)
                  {
                    temp_good = false;
                    break;
                  }

                  if (line.index != last_line)
                  {
                    // Read up to the wanted index. Write all lines to the temp file. If a line doesn't exist, we are at the end of the file.
                    while (current_line < line.index)
                    {
                      string message = string.Empty;
                      if ((message = soriginal.ReadLine()) != null)
                        stemp.WriteLine(message);
                      else
                      {
                        temp_good = false;
                        break;
                      }

                      current_line++;
                    }

                    if (!temp_good)
                      break;


                    // Write up the new line to the temp file instead, and read forward one line.
                    stemp.WriteLine(line.line);
                    soriginal.ReadLine();
                    current_line++;
                    last_line = line.index;
                  }
                }


                if (temp_good)
                {
                  // Write in all remaining lines.
                  while (!soriginal.EndOfStream)
                    stemp.WriteLine(soriginal.ReadLine());
                }
              }

              // If the temporary file failed, and we want to nullify changes, delete the temp file.
              if (!temp_good)
              {
                InternalDeleteFile(ref temp_path);
                return false;
              }
            }

            // Delete the old file and move in the new file.
            return InternalMoveFile(ref temp_path, ref filepath, true);
          }
        }
      }
      catch
      {
        // If any error occurs, delete the temp file if it was made and return false.
        InternalDeleteFile(ref temp_path);
        return false;
      }

      return false; // The file was not successfully inserted into.
    }

    /// <summary>
    /// A function which edits several lines in a file.
    /// This function creates a temporary file to edit the lines.
    /// </summary>
    /// <param name="filepath">The filepath of the file to edit.</param>
    /// <param name="edited_lines">The lines to be edited. If multiple lines have the same index, the function will only use the first version of the edit.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringsInFile(ref string filepath, IEnumerable<ES_FileLine> edited_lines, bool cleanup = false)
    {
      // If the lines do not exist, return false immediately.
      if (filepath == null || edited_lines == null || edited_lines.Count() <= 0)
        return false;
      if (cleanup)
        CleanupFilePath(ref filepath);
      return InternalEditStringsInFile(ref filepath, edited_lines);
    }

    /// <summary>
    /// A function which edits several lines in a file.
    /// This function creates a temporary file to edit the lines.
    /// </summary>
    /// <param name="directory">The directory of the file.</param>
    /// <param name="filename">The name of the file, extension included.</param>
    /// <param name="edited_lines">The lines to be edited. If multiple lines have the same index, the function will only use the first version of the edit.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringsInFile(ref string directory, ref string filename, IEnumerable<ES_FileLine> edited_lines, bool cleanup = false)
    {
      // Clean up the file path, if requested.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      // Concatenate the path and attempt to edit the file.
      string path = CreateFilePath(new string[] { directory, filename });
      return InternalEditStringsInFile(ref path, edited_lines);
    }

    /// <summary>
    /// A function which edits several lines in a file.
    /// This function creates a temporary file to edit the lines.
    /// </summary>
    /// <param name="file">The file information to use.</param>
    /// <param name="edited_lines">The lines to be edited. If multiple lines have the same index, the function will only use the first version of the edit.</param>
    /// <param name="cleanup">A bool determining if the file path should be cleaned. If you haven't before, you should now.</param>
    /// <returns>Returns if the file was successfully edited.</returns>
    public static bool EditStringsInFile(FilePath file, IEnumerable<ES_FileLine> edited_lines, bool cleanup = false)
    {
      // Attempt to edit the file.
      return file != null ? EditStringsInFile(ref file.directory, ref file.filename, edited_lines, cleanup) : false;
    }

    /// <summary>
    /// A function which takes a full filepath and attempts to break it up into the directory and filename.
    /// The filename will include its extension.
    /// </summary>
    /// <param name="filepath">The filepath to break up.</param>
    /// <param name="directory">The directory of the file path.</param>
    /// <param name="filename">The filename of the file path. Includes the extension.</param>
    public static void BreakupFilePath(string filepath, out string directory, out string filename)
    {
      // Initialize the strings.
      directory = string.Empty;
      filename = string.Empty;

      if (filepath == null)
        return;

      // Replace all slashes to use just a standard across all file platforms.
      filepath = filepath.Replace('/', Path.AltDirectorySeparatorChar);
      filepath = filepath.Replace('\\', Path.AltDirectorySeparatorChar);

      // If there is a slash at all, then there is a directory to separate.
      if (filepath.LastIndexOf(Path.AltDirectorySeparatorChar) >= 0)
      {
        directory = filepath.Substring(0, filepath.LastIndexOf(Path.AltDirectorySeparatorChar) + 1); // Break out the directory based on the last separator.
        filename = filepath.Substring(filepath.LastIndexOf(Path.AltDirectorySeparatorChar) + 1); // Break out the filename based on the last separator.
      }
      else
        filename = filepath; // Otherwise, assume the path is just a file name and extension.
    }

    /// <summary>
    /// A function which cleans up a directory path. Make sure it doesn't have a filename in it!.
    /// </summary>
    /// <param name="directory">The directory path to clean.</param>
    public static void CleanupDirectoryPath(ref string directory)
    {
      if (directory == null)
      {
        directory = string.Empty;
        return;
      }

      // Create an array of illegal characters, and format it into a REGEX. In a directory, ':', '/', and '\' are all valid characters.
      string illegalPath = Regex.Escape(new string((Path.GetInvalidFileNameChars().Where(c => c != ':' && c != '/' && c != '\\')).ToArray()));
      string illegalRegex = string.Format(@"[{0}]+", illegalPath);

      // Replace any slashes with the proper ones.
      directory = directory.Replace('/', Path.AltDirectorySeparatorChar);
      directory = directory.Replace('\\', Path.AltDirectorySeparatorChar);

      // Check if there is any colon at all. Please note that due to things like Volume Mount Points, the ':' is not necessarily at Index 1.
      if (directory.IndexOf(':') >= 0)
      {
        // Create a left and right substring. Only the right string needs to remove any colons.
        string left = directory.Substring(0, directory.IndexOf(':') + 1);
        string right = directory.Substring(directory.IndexOf(':') + 1);

        left = Regex.Replace(left, illegalRegex, ""); // Clean up the left side.
        right = Regex.Replace(right, illegalRegex, ""); // Clean up the right side.
        right = right.Replace(':'.ToString(), ""); // Remove additional colons from the right side.

        // Replace any reserved words.
        foreach (string word in file_ReservedWords)
        {
          string reservedRegex = string.Format("^{0}(\\.|$)", word);
          left = Regex.Replace(left, reservedRegex, "");
          right = Regex.Replace(right, reservedRegex, "");
        }

        // Stich the directory path together and finish by removing any extra slashes.
        directory = left + right;
        directory = Path.Combine(directory);

        if (directory.Last() != Path.AltDirectorySeparatorChar)
          directory += Path.AltDirectorySeparatorChar;
      }
      else
      {
        directory = Regex.Replace(directory, illegalRegex, ""); // Simply replace any bad characters.

        // Replace any reserved words.
        foreach (string word in file_ReservedWords)
        {
          string reservedRegex = string.Format("^{0}(\\.|$)", word);
          directory = Regex.Replace(directory, reservedRegex, "");
        }

        // Remove any extra slashes.
        directory = Path.Combine(directory);
      }
    }

    /// <summary>
    /// A function which cleans up a filename, plus extension. Make sure it doesn't have the directory in it!
    /// </summary>
    /// <param name="filename">The filename to clean.</param>
    public static void CleanupFileName(ref string filename)
    {
      if (filename == null)
      {
        filename = string.Empty;
        return;
      }

      // Create an array of illegal characters, and format it into a REGEX.
      string illegalPath = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
      string illegalRegex = string.Format(@"[{0}]+", illegalPath);

      filename = Regex.Replace(filename, illegalRegex, ""); // Simply replace any bad characters.

      // Replace any reserved words.
      foreach (string word in file_ReservedWords)
      {
        string reservedRegex = string.Format("^{0}(\\.|$)", word);
        filename = Regex.Replace(filename, reservedRegex, "$1");
      }
    }

    /// <summary>
    /// A function which cleans up a full file path, removing illegal and redundant characters.
    /// </summary>
    /// <param name="directory">The directory to clean up.</param>
    /// <param name="filename">The filename to clean up.</param>
    public static void CleanupFilePath(ref string directory, ref string filename)
    {
      CleanupDirectoryPath(ref directory); // Clean the directory.
      CleanupFileName(ref filename); // Clean the filename.

      string path = CreateFilePath(new string[] { directory, filename }); // Piece the two together.
      BreakupFilePath(path, out directory, out filename); // Break up the filepath again, now that a correct path has been made.
    }

    /// <summary>
    /// A function which cleans up a full file path, removing illegal and redundant characters.
    /// </summary>
    /// <param name="filepath">The filepath to clean up.</param>
    public static void CleanupFilePath(ref string filepath)
    {
      BreakupFilePath(filepath, out string directory, out string filename); // Break up the path.

      CleanupDirectoryPath(ref directory); // Clean the directory.
      CleanupFileName(ref filename); // Clean the filename.

      filepath = CreateFilePath(new string[] { directory, filename }); // Concatenate the path once more.
    }

    /// <summary>
    /// A function which cleans up a full file path, removing illegal and redundant characters.
    /// </summary>
    /// <param name="file">The file to edit and clean up.</param>
    public static void CleanupFilePath(FilePath file)
    {
      if (file != null)
        CleanupFilePath(ref file.directory, ref file.filename); // Clean up the file's directory and filename.
    }

    /// <summary>
    /// The internal function for attempting to check if a directory exists. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="directory">The path of the directory.</param>
    /// <param name="create_if_null">A bool determining if the directory should be created if it doesn't exist.</param>
    /// <returns>Returns if the directory exists or not.</returns>
    private static bool InternalCheckDirectory(ref string directory, bool create_if_null)
    {
      try
      {
        if (Directory.Exists(directory)) // If the directory exists, simply return true.
          return true;
        else if (create_if_null) // If it doesn't exist, but we can create it, return if the directory was created.
          return CreateDirectory(ref directory);
      }
      catch
      {
        // If any error happens, simply return false.
        return false;
      }

      return false; // Otherwise, the directory does not exist.
    }

    /// <summary>
    /// A function which checks if a directory exists.
    /// </summary>
    /// <param name="directory">The path of the directory.</param>
    /// <param name="create_if_null">A bool determining if the directory should be created if it doesn't exist.</param>
    /// <param name="cleanup">A bool determining if the directory string should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the directory exists or not.</returns>
    public static bool CheckDirectory(ref string directory, bool create_if_null = false, bool cleanup = false)
    {
      // Clean up the directory if necessary.
      if (cleanup)
        CleanupDirectoryPath(ref directory);

      return InternalCheckDirectory(ref directory, create_if_null);
    }

    /// <summary>
    /// A function which checks if a directory exists.
    /// </summary>
    /// <param name="file">The file to get the directory from.</param>
    /// <param name="create_if_null">A bool determining if the directory should be created if it doesn't exist.</param>
    /// <param name="cleanup">A bool determining if the entire file should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the directory exists or not.</returns>
    public static bool CheckDirectory(FilePath file, bool create_if_null = false, bool cleanup = false)
    {
      if (file == null)
        return false;

      // Cleanup the entire file.
      if (cleanup)
        CleanupFilePath(file);

      return CheckDirectory(ref file.directory, create_if_null, false); // Check the directory. We no longer need to clean.
    }

    /// <summary>
    /// The internal function for attempting to check if a file exists. All cleanup is handled in the public function.
    /// </summary>
    /// <param name="filepath">The full filepath to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it doesn't exist.</param>
    /// <returns>Returns if the file exists or not.</returns>
    private static bool InternalCheckFile(ref string filepath, bool create_if_null)
    {
      try
      {
        if (File.Exists(filepath)) // If the file exists, simply return true.
          return true;
        else if (create_if_null) // If it doesn't exist, but we can create it, return if the file was created.
          return CreateFile(ref filepath, true, true, false);
      }
      catch
      {
        // If any error happens, just return false.
        return false;
      }

      return false; // Otherwise, the file does not exist.
    }

    /// <summary>
    /// A function which checks if a file exists.
    /// </summary>
    /// <param name="filepath">The full filepath to the file.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it doesn't exist.</param>
    /// <param name="cleanup">A bool determining if the filepath should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the file exists or not.</returns>
    public static bool CheckFile(ref string filepath, bool create_if_null = false, bool cleanup = false)
    {
      // If cleaning, clean up the whole file path.
      if (cleanup)
        CleanupFilePath(ref filepath);

      return InternalCheckFile(ref filepath, create_if_null);
    }

    /// <summary>
    /// A function which checks if a file exists.
    /// </summary>
    /// <param name="directory">The directory of the file to check.</param>
    /// <param name="filename">The name of the file to check.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it doesn't exist.</param>
    /// <param name="cleanup">A bool determining if the directory and filename should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the file exists or not.</returns>
    public static bool CheckFile(ref string directory, ref string filename, bool create_if_null = false, bool cleanup = false)
    {
      // If cleaning, clean up the whole file path.
      if (cleanup)
        CleanupFilePath(ref directory, ref filename);

      string path = CreateFilePath(new string[] { directory, filename }); // Create a placeholder path with everything combined.

      return InternalCheckFile(ref path, create_if_null); // Check the file.
    }

    /// <summary>
    /// A function which checks if a file exists.
    /// </summary>
    /// <param name="file">The file to check the internal file of.</param>
    /// <param name="create_if_null">A bool determining if the file should be created if it doesn't exist.</param>
    /// <param name="cleanup">A bool determining if the filepath should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the file exists or not.</returns>
    public static bool CheckFile(FilePath file, bool create_if_null = false, bool cleanup = false)
    {
      // Get the directory and filename, and check the file.
      return file != null ? CheckFile(ref file.directory, ref file.filename, create_if_null, cleanup) : false;
    }

    /// <summary>
    /// A function which returns all the filepaths in a given directory. This does not return the names of sub-directories.
    /// </summary>
    /// <param name="directory">The directory to look into.</param>
    /// <param name="filepaths">The filepaths found in the directory. In the case of an error, a null array will be returned.</param>
    /// <param name="cleanup">A bool determining if the directory should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the files were successfully found or not.</returns>
    public static bool GetFileList(ref string directory, out string[] filepaths, bool cleanup = false)
    {
      filepaths = null; // Initialize the array.

      try
      {
        // Check if the directory exists.
        if (CheckDirectory(ref directory, false, cleanup))
        {
          filepaths = Directory.GetFiles(directory); // Get all the files.
          return filepaths != null; // Return if there were any fils.
        }

        return false; // No files were found.
      }
      catch
      {
        // In the event of an error, return false.
        filepaths = null;
        return false;
      }
    }

    /// <summary>
    /// A function which returns all the filepaths in a given directory. This does not return the names of sub-directories.
    /// </summary>
    /// <param name="directory">The directory to look into.</param>
    /// <param name="files">The files found in the directory. In the case of an error, a null array will be returned.</param>
    /// <param name="cleanup">A bool determining if the directory should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the files were successfully found or not.</returns>
    public static bool GetFileList(ref string directory, out FilePath[] files, bool cleanup = false)
    {
      files = null; // Initialize the array.

      // Attempt to get the file paths.
      if (GetFileList(ref directory, out string[] filepaths, cleanup))
      {
        files = new FilePath[filepaths.Length]; // Create a new array.

        // Create new FilePaths for each path.
        for (int i = 0; i < filepaths.Length; i++)
          files[i] = new FilePath(filepaths[i], cleanup);

        return true; // The files were found.
      }

      return false; // The files were not found.
    }

    /// <summary>
    /// A function which returns all the subdirectories in a given directory.
    /// </summary>
    /// <param name="directory">The directory to look into.</param>
    /// <param name="subdirectories">The subdirectories found in the directory. In the case of an error, a null array will be returned.</param>
    /// <param name="cleanup">A bool determining if the directory should be cleaned up. It is recommended to do so if you haven't previously.</param>
    /// <returns>Returns if the subdirectories were found or not.</returns>
    public static bool GetDirectoryList(ref string directory, out string[] subdirectories, bool cleanup = false)
    {
      subdirectories = null; // Initialize the array.

      try
      {
        // Check if the directory exists.
        if (CheckDirectory(ref directory, false, cleanup))
        {
          subdirectories = Directory.GetDirectories(directory); // Get all the files.
          return subdirectories != null; // Return if there were any fils.
        }

        return false; // No files were found.
      }
      catch
      {
        // In the event of an error, return false.
        subdirectories = null;
        return false;
      }
    }
  }
  /************************************************************************************************/
}