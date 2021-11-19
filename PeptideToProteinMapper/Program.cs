// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Started September 2008
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
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
using System.Reflection;
using System.Threading;
using PeptideToProteinMapEngine;
using PRISM;
using PRISM.FileProcessor;
using PRISM.Logging;
using ProteinCoverageSummarizer;

namespace PeptideToProteinMapper
{
    /// <summary>
    /// <para>
    /// This program uses PeptideToProteinMapEngine.dll to read in a file with peptide sequences, then
    /// searches for the given peptides in a protein sequence file (.Fasta or tab-delimited text)
    /// using ProteinCoverageSummarizer.dll
    /// </para>
    /// <para>
    /// This program is similar to the ProteinCoverageSummarizer, but it is a console-only application
    /// In addition, this program supports reading Inspect output files
    /// </para>
    /// <para>
    /// Example command Line
    /// I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath
    /// </para>
    /// </summary>
    public static class Program
    {
        // Ignore Spelling: yyyy-MM-dd, hh:mm:ss tt

        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "August 14, 2021";

        private static string mParameterFilePath;
        private static string mInspectParameterFilePath;
        private static clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants mInputFileFormatCode;
        private static bool mLogMessagesToFile;
        private static string mLogFilePath = string.Empty;
        private static string mLogDirectoryPath = string.Empty;
        private static bool mVerboseLogging;
        private static StreamWriter mVerboseLogFile;
        private static string mVerboseLoggingMostRecentMessage = string.Empty;
        private static clsPeptideToProteinMapEngine mPeptideToProteinMapEngine;
        private static DateTime mLastProgressReportTime;
        private static DateTime mLastPercentDisplayed;

        private static void CreateVerboseLogFile()
        {
            try
            {
                var logFilePath = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
                logFilePath += "_VerboseLog_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

                var openingExistingFile = File.Exists(logFilePath);
                mVerboseLogFile = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

                if (!openingExistingFile)
                {
                    mVerboseLogFile.WriteLine("Date" + "\t" + "Percent Complete" + "\t" + "Message");
                }

                mVerboseLoggingMostRecentMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error creating verbose log file: " + ex.Message);
            }
        }

        /// <summary>
        /// Main program
        /// </summary>
        /// <returns>0 if no error, error code if an error</returns>
        public static int Main()
        {
            var commandLineParser = new clsParseCommandLine();

            var returnCode = 0;

            mParameterFilePath = string.Empty;
            mInspectParameterFilePath = string.Empty;
            mInputFileFormatCode = clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.AutoDetermine;

            mLogMessagesToFile = false;
            mLogFilePath = string.Empty;
            mLogDirectoryPath = string.Empty;

            try
            {
                var options = new ProteinCoverageSummarizerOptions();

                var proceed = commandLineParser.ParseCommandLine() && SetOptionsUsingCommandLineParameters(commandLineParser, options);

                if (!proceed || commandLineParser.NeedToShowHelp || commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount == 0)
                {
                    ShowProgramHelp();
                    returnCode = -1;
                }
                else
                {
                    try
                    {
                        if (mVerboseLogging)
                        {
                            CreateVerboseLogFile();
                        }

                        if (string.IsNullOrWhiteSpace(options.PeptideInputFilePath))
                        {
                            ShowErrorMessage("Peptide input file must be defined via /I (or by listing the filename just after the .exe)");
                            return -1;
                        }

                        if (string.IsNullOrWhiteSpace(options.ProteinInputFilePath))
                        {
                            ShowErrorMessage("Protein input file must be defined via /R");
                            return -1;
                        }

                        mPeptideToProteinMapEngine = new clsPeptideToProteinMapEngine(options)
                        {
                            LogMessagesToFile = mLogMessagesToFile,
                            LogFilePath = mLogFilePath,
                            LogDirectoryPath = mLogDirectoryPath,
                            PeptideInputFileFormat = mInputFileFormatCode,
                            InspectParameterFilePath = mInspectParameterFilePath
                        };

                        RegisterEvents(mPeptideToProteinMapEngine);
                        mPeptideToProteinMapEngine.ProgressReset += PeptideToProteinMapEngine_ProgressReset;

                        mPeptideToProteinMapEngine.ProcessFilesWildcard(options.PeptideInputFilePath, options.OutputDirectoryPath, mParameterFilePath);

                        mVerboseLogFile?.Close();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error initializing the Peptide to Protein Mapper Options " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                returnCode = -1;
            }

            return returnCode;
        }

        private static void DisplayProgressPercent(string taskDescription, int percentComplete, bool addCarriageReturn)
        {
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }

            if (percentComplete > 100)
                percentComplete = 100;

            if (string.IsNullOrEmpty(taskDescription))
                taskDescription = "Processing";

            Console.Write("{0}: {1}% ", taskDescription, percentComplete);

            if (addCarriageReturn)
            {
                Console.WriteLine();
            }
        }

        private static string GetAppVersion()
        {
            return ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static void RegisterEvents(IEventNotifier processingClass)
        {
            processingClass.StatusEvent += PeptideToProteinMapEngine_StatusEvent;
            processingClass.ErrorEvent += PeptideToProteinMapEngine_ErrorEvent;
            processingClass.WarningEvent += PeptideToProteinMapEngine_WarningEvent;

            processingClass.ProgressUpdate += PeptideToProteinMapEngine_ProgressChanged;
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser, ProteinCoverageSummarizerOptions options)
        {
            // Returns True if no problems; otherwise, returns false
            // /I:PeptideInputFilePath /R: ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath

            var validParameters = new List<string> { "I", "O", "R", "P", "F", "N", "G", "H", "K", "A", "L", "LogDir", "LogFolder", "VerboseLog" };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(validParameters))
                {
                    ShowErrorMessage("Invalid command line parameters",
                        (from item in commandLineParser.InvalidParameters(validParameters) select ("/" + item)).ToList());
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

                if (commandLineParser.RetrieveValueForParameter("R", out var proteinInputFilePath))
                    options.ProteinInputFilePath = proteinInputFilePath;

                if (commandLineParser.RetrieveValueForParameter("P", out var parameterFilePath))
                    mParameterFilePath = parameterFilePath;

                if (commandLineParser.RetrieveValueForParameter("F", out var inputFileFormatCode))
                {
                    if (int.TryParse(inputFileFormatCode, out var inputFileFormatCodeValue))
                    {
                        try
                        {
                            mInputFileFormatCode = (clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants)inputFileFormatCodeValue;
                        }
                        catch (Exception)
                        {
                            // Conversion failed; leave mInputFileFormatCode unchanged
                        }
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("N", out var inspectParameterFilePath))
                    mInspectParameterFilePath = inspectParameterFilePath;

                if (commandLineParser.RetrieveValueForParameter("G", out _))
                    options.IgnoreILDifferences = true;

                if (commandLineParser.RetrieveValueForParameter("H", out _))
                    options.OutputProteinSequence = false;

                if (commandLineParser.RetrieveValueForParameter("K", out _))
                    options.SearchAllProteinsSkipCoverageComputationSteps = true;

                if (commandLineParser.RetrieveValueForParameter("A", out _))
                    options.SaveSourceDataPlusProteinsFile = true;

                if (commandLineParser.RetrieveValueForParameter("L", out var logFilePath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFilePath))
                    {
                        mLogFilePath = logFilePath;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("LogDir", out var logDirectoryPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logDirectoryPath))
                    {
                        mLogDirectoryPath = logDirectoryPath;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("LogFolder", out var logFolderPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFolderPath))
                    {
                        mLogDirectoryPath = logFolderPath;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("VerboseLog", out _))
                    mVerboseLogging = true;

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

        private static void ShowProgramHelp()
        {
            try
            {
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "This program reads in a text file containing peptide sequences. " +
                    "It then searches the specified FASTA or text file containing protein names and sequences " +
                    "(and optionally descriptions) to find the proteins that contain each peptide. " +
                    "It will also compute the sequence coverage percent for each protein (disable using /K)."));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(Assembly.GetExecutingAssembly().Location));
                Console.WriteLine(" /I:PeptideInputFilePath /R:ProteinInputFilePath");
                Console.WriteLine(" [/O:OutputDirectoryName] [/P:ParameterFilePath] [/F:FileFormatCode]");
                Console.WriteLine(" [/N:InspectParameterFilePath] [/G] [/H] [/K] [/A]");
                Console.WriteLine(" [/L[:LogFilePath]] [/LogDir:LogDirectoryPath] [/VerboseLog] [/Q]");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The input file path can contain the wildcard character *. If a wildcard is present, " +
                    "the same protein input file path will be used for each of the peptide input files matched."));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph("The output directory name is optional. " +
                                  "If omitted, the output files will be created in the same directory as the input file. " +
                                  "If included, then a subdirectory is created with the name OutputDirectoryName."));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph("The parameter file path is optional. " +
                                  "If included, it should point to a valid XML parameter file."));
                Console.WriteLine();

                Console.WriteLine("Use /F to specify the peptide input file format code.  Options are:");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.AutoDetermine + "=Auto Determine: Treated as /F:1 unless name ends in _inspect.txt, then /F:3");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.PeptideListFile + "=Peptide sequence in the 1st column (subsequent columns are ignored)");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.ProteinAndPeptideFile + "=Protein name in 1st column and peptide sequence 2nd column");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.InspectResultsFile + "=Inspect search results file (peptide sequence in the 3rd column)");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.MSGFPlusResultsFile + "=MS-GF+ search results file (peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan')");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.PHRPFile + "=Peptide Hit Results Processor (PHRP) file (for MS-GF+, X!Tandem, SEQUEST, or Inspect results)");
                Console.WriteLine("   " + (int)clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.TabDelimitedText + "=Generic tab-delimited text file; will look for column names that start with Peptide, Protein, and Scan");
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "When processing an Inspect search results file, use /N to specify the Inspect parameter file used " +
                    "(required for determining the mod names embedded in the identified peptides)."));
                Console.WriteLine();

                Console.WriteLine("Use /G to ignore I/L differences when finding peptides in proteins or computing coverage");
                Console.WriteLine("Use /H to suppress (hide) the protein sequence in the _coverage.txt file");
                Console.WriteLine("Use /K to skip the protein coverage computation steps (enabling faster processing)");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /A to create the _AllProteins.txt file, listing each of the peptides in the input file, " +
                    "plus one line per mapped protein for that peptide"));

                Console.WriteLine("Use /L to create a log file, optionally specifying the file name");
                Console.WriteLine("Use /LogDir to define the directory in which the log file should be created");
                Console.WriteLine("Use /VerboseLog to create a detailed log file");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();
                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics");
                Console.WriteLine();

                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                Thread.Sleep(750);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax: " + ex.Message);
            }
        }

        private static void PeptideToProteinMapEngine_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void PeptideToProteinMapEngine_WarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        private static void PeptideToProteinMapEngine_ErrorEvent(string message, Exception ex)
        {
            ShowErrorMessage(message);
        }

        private static void PeptideToProteinMapEngine_ProgressChanged(string taskDescription, float percentComplete)
        {
            const int PROGRESS_DOT_INTERVAL_MSEC = 250;

            if (DateTime.UtcNow.Subtract(mLastPercentDisplayed).TotalSeconds >= 15)
            {
                Console.WriteLine();

                DisplayProgressPercent(taskDescription, Convert.ToInt32(percentComplete), false);
                mLastPercentDisplayed = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC)
            {
                mLastProgressReportTime = DateTime.UtcNow;
                Console.Write(".");
            }

            if (mVerboseLogFile == null)
            {
                return;
            }

            taskDescription ??= string.Empty;

            if (taskDescription == (mVerboseLoggingMostRecentMessage ?? ""))
            {
                mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" +
                                          percentComplete + "\t" +
                                          ".");
            }
            else
            {
                mVerboseLoggingMostRecentMessage = string.Copy(taskDescription);

                mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" +
                                          percentComplete + "\t" +
                                          taskDescription);
            }
        }

        private static void PeptideToProteinMapEngine_ProgressReset()
        {
            mLastProgressReportTime = DateTime.UtcNow;
            mLastPercentDisplayed = DateTime.UtcNow;
        }
    }
}
