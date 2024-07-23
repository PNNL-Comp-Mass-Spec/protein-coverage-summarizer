// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
// Program started June 14, 2005
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://www.pnnl.gov/integrative-omics
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause
//
// Copyright 2018 Battelle Memorial Institute

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PRISM;
using PRISM.FileProcessor;
using ProteinCoverageSummarizer;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// <para>
    /// This program uses clsProteinCoverageSummarizer to read in a file with protein sequences along with
    /// an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
    /// </para>
    /// <para>
    /// Example command Line
    /// I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath
    /// </para>
    /// </summary>
    public static class Program
    {
        // Ignore Spelling: Nikša

        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "July 22, 2024";

        private static string mParameterFilePath;

        private static DateTime mLastProgressReportTime;
        private static int mLastProgressReportValue;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        /// <summary>
        /// Main entry method
        /// </summary>
        /// <returns>0 if no error, error code if an issue</returns>
        // Enable single thread apartment (STA) mode
        [STAThread]
        public static int Main()
        {
            // Returns 0 if no error, error code if an error
            var commandLineParser = new clsParseCommandLine();

            mParameterFilePath = string.Empty;

            try
            {
                var options = new ProteinCoverageSummarizerOptions();

                var proceed = false;
                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser, options, out var invalidParameters))
                    {
                        proceed = true;
                    }

                    if (invalidParameters)
                        return -1;
                }

                if (!commandLineParser.NeedToShowHelp && string.IsNullOrEmpty(options.ProteinInputFilePath))
                {
                    ShowGUI(options);
                    return 0;
                }

                if (!proceed || commandLineParser.NeedToShowHelp || commandLineParser.ParameterCount == 0 || options.PeptideInputFilePath.Length == 0)
                {
                    ShowProgramHelp();
                    return -1;
                }

                if (string.IsNullOrWhiteSpace(mParameterFilePath) &&
                    !options.SaveProteinToPeptideMappingFile &&
                    options.SearchAllProteinsSkipCoverageComputationSteps)
                {
                    ConsoleMsgUtils.ShowWarning(ConsoleMsgUtils.WrapParagraph(
                        "You used /K to skip protein coverage computation but didn't specify /M " +
                        "to create a protein to peptide mapping file; no results will be saved"));
                    Console.WriteLine();
                    ConsoleMsgUtils.ShowWarning("It is advised that you use only /M (and don't use /K)");
                }

                try
                {
                    var proteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner(options)
                    {
                        CallingAppHandlesEvents = false
                    };

                    proteinCoverageSummarizer.StatusEvent += ProteinCoverageSummarizer_StatusEvent;
                    proteinCoverageSummarizer.ErrorEvent += ProteinCoverageSummarizer_ErrorEvent;
                    proteinCoverageSummarizer.WarningEvent += ProteinCoverageSummarizer_WarningEvent;

                    proteinCoverageSummarizer.ProgressUpdate += ProteinCoverageSummarizer_ProgressChanged;
                    proteinCoverageSummarizer.ProgressReset += ProteinCoverageSummarizer_ProgressReset;

                    var success = proteinCoverageSummarizer.ProcessFilesWildcard(options.PeptideInputFilePath, options.OutputDirectoryPath, mParameterFilePath);

                    if (success)
                    {
                        return 0;
                    }

                    ConsoleMsgUtils.ShowWarning("Processing failed");
                    return -1;
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error running the protein coverage summarizer: " + ex.Message);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                return -1;
            }
        }

        private static void DisplayProgressPercent(int percentComplete, bool addCarriageReturn)
        {
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }

            if (percentComplete > 100)
                percentComplete = 100;

            Console.Write("Processing: {0}% ", percentComplete);

            if (addCarriageReturn)
            {
                Console.WriteLine();
            }
        }

        private static string GetAppVersion()
        {
            return AppUtils.GetAppVersion(PROGRAM_DATE);
        }

        /// <summary>
        /// Set options using the command line
        /// </summary>
        /// <param name="commandLineParser"></param>
        /// <param name="options"></param>
        /// <param name="invalidParameters">True if an unrecognized parameter is found</param>
        /// <returns>True if no problems; false if an issue</returns>
        private static bool SetOptionsUsingCommandLineParameters(
            clsParseCommandLine commandLineParser, ProteinCoverageSummarizerOptions options, out bool invalidParameters)
        {
            var validParameters = new List<string>
            {
                "I", "O", "R", "P", "F", "SkipHeader", "SkipHeaders", "G", "H", "M", "K", "D", "Debug", "KeepDB"
            };

            invalidParameters = false;

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(validParameters))
                {
                    ShowErrorMessage("Invalid command line parameters",
                        (from item in commandLineParser.InvalidParameters(validParameters) select ("/" + item)).ToList());
                    invalidParameters = true;
                    return false;
                }

                // Query commandLineParser to see if various parameters are present
                if (commandLineParser.RetrieveValueForParameter("I", out var inputFilePath))
                {
                    options.PeptideInputFilePath = inputFilePath;
                }
                else if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    options.PeptideInputFilePath = commandLineParser.RetrieveNonSwitchParameter(0);
                }

                if (commandLineParser.RetrieveValueForParameter("O", out var outputDirectoryPath))
                    options.OutputDirectoryPath = outputDirectoryPath;

                if (commandLineParser.RetrieveValueForParameter("R", out var proteinFile))
                    options.ProteinInputFilePath = proteinFile;

                if (commandLineParser.RetrieveValueForParameter("P", out var parameterFile))
                    mParameterFilePath = parameterFile;

                if (commandLineParser.RetrieveValueForParameter("F", out var inputFileFormatCode))
                {
                    if (int.TryParse(inputFileFormatCode, out var inputFileFormatCodeValue))
                    {
                        try
                        {
                            options.PeptideFileFormatCode = (ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode)inputFileFormatCodeValue;
                        }
                        catch (Exception)
                        {
                            // Conversion failed; leave options.PeptideFileFormatCode unchanged
                        }
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("SkipHeader", out _) ||
                    commandLineParser.RetrieveValueForParameter("SkipHeaders", out _))
                {
                    options.PeptideFileSkipFirstLine = true;
                    options.ProteinDataOptions.DelimitedFileSkipFirstLine = true;
                }

                if (commandLineParser.RetrieveValueForParameter("H", out _))
                    options.OutputProteinSequence = false;

                options.IgnoreILDifferences = commandLineParser.IsParameterPresent("G");
                options.SaveProteinToPeptideMappingFile = commandLineParser.IsParameterPresent("M");
                options.SearchAllProteinsSkipCoverageComputationSteps = commandLineParser.IsParameterPresent("K");
                options.SaveSourceDataPlusProteinsFile = commandLineParser.IsParameterPresent("D");
                options.DebugMode = commandLineParser.IsParameterPresent("Debug");
                options.KeepDB = commandLineParser.IsParameterPresent("KeepDB");

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static void ShowErrorMessage(string message)
        {
            ConsoleMsgUtils.ShowError(message);
        }

        private static void ShowErrorMessage(string title, IEnumerable<string> errorMessages)
        {
            ConsoleMsgUtils.ShowErrors(title, errorMessages);
        }

        private static void ShowGUI(ProteinCoverageSummarizerOptions options)
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            try
            {
                var handle = GetConsoleWindow();

                if (!options.DebugMode)
                {
                    // Hide the console
                    ShowWindow(handle, SW_HIDE);
                }

                var objFormMain = new GUI
                {
                    KeepDB = options.KeepDB
                };

                objFormMain.ShowDialog();

                if (!options.DebugMode)
                {
                    // Show the console
                    ShowWindow(handle, SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Error in ShowGUI: " + ex.Message);
                ConsoleMsgUtils.ShowWarning(StackTraceFormatter.GetExceptionStackTraceMultiLine(ex));

                MessageBox.Show("Error in ShowGUI: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void ShowProgramHelp()
        {
            try
            {
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "This program reads in a .fasta or .txt file containing protein names and sequences (and optionally descriptions). " +
                    "The program also reads in a .txt file containing peptide sequences and protein names (though protein name is optional) " +
                    "then uses this information to compute the sequence coverage percent for each protein. " +
                    "Recognizes files where the first line is column headers, reading peptides from the first column that starts with 'Peptide' " +
                    "and proteins from the first column that starts with 'Protein'"));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(AppUtils.GetAppPath()));
                Console.WriteLine("  /I:PeptideInputFilePath /R:ProteinInputFilePath [/O:OutputDirectoryName]");
                Console.WriteLine("  [/P:ParameterFilePath] [/F:FileFormatCode] [/SkipHeader]");
                Console.WriteLine("  [/G] [/H] [/M] [/K] [/D] [/Debug] [/KeepDB]");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The input file path can contain the wildcard character *. If a wildcard is present, the same protein input file path " +
                    "will be used for each of the peptide input files matched."));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The output directory name is optional. If omitted, the output files will be created in the same directory as the input file. " +
                    "If included, a subdirectory is created with the name OutputDirectoryName."));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The parameter file path is optional. If included, it should point to a valid XML parameter file."));
                Console.WriteLine();

                Console.WriteLine("Use /F to specify the peptide input file format code.  Options are:");
                Console.WriteLine("   " + (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly + "=Peptide sequence in the 1st column (subsequent columns are ignored)");
                Console.WriteLine("   " + (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.ProteinName_PeptideSequence + "=Protein name in 1st column and peptide sequence 2nd column");
                Console.WriteLine("   " + (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames + "=Generic tab-delimited text file; will look for column names that start with Peptide, Protein, and Scan");
                Console.WriteLine();

                Console.WriteLine("Use /SkipHeader to skip the first line when the file format is Sequence Only or Protein Name and Sequence");

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /G to ignore I/L differences when finding peptides in proteins or computing coverage."));
                Console.WriteLine("Use /H to suppress (hide) the protein sequence in the _coverage.txt file.");
                Console.WriteLine("Use /M to enable the creation of a protein to peptide mapping file");
                Console.WriteLine("Use /K to skip protein coverage computation steps");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /D to duplicate the input file, but add a new column listing the mapped protein for each peptide. " +
                    "If a peptide maps to multiple proteins, multiple lines will be listed"));

                Console.WriteLine();
                Console.WriteLine("Use /Debug to keep the console open to see additional debug messages");
                Console.WriteLine("Use /KeepDB to keep the SQLite database after processing (by default it is deleted)");
                Console.WriteLine();

                Console.WriteLine("Program written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://www.pnnl.gov/integrative-omics");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax: " + ex.Message);
            }
        }

        private static void ProteinCoverageSummarizer_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void ProteinCoverageSummarizer_WarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        private static void ProteinCoverageSummarizer_ErrorEvent(string message, Exception ex)
        {
            ShowErrorMessage(message);
        }

        private static void ProteinCoverageSummarizer_ProgressChanged(string taskDescription, float percentComplete)
        {
            const int PERCENT_REPORT_INTERVAL = 25;
            const int PROGRESS_DOT_INTERVAL_MSEC = 250;

            if (percentComplete >= mLastProgressReportValue)
            {
                if (mLastProgressReportValue > 0)
                {
                    Console.WriteLine();
                }

                DisplayProgressPercent(mLastProgressReportValue, false);
                mLastProgressReportValue += PERCENT_REPORT_INTERVAL;
                mLastProgressReportTime = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC)
            {
                mLastProgressReportTime = DateTime.UtcNow;
                Console.Write(".");
            }
        }

        private static void ProteinCoverageSummarizer_ProgressReset()
        {
            mLastProgressReportTime = DateTime.UtcNow;
            mLastProgressReportValue = 0;
        }
    }
}