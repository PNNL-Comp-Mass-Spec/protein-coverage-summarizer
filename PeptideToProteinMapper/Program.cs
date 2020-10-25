// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Started September 2008
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
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
using ProteinCoverageSummarizer;

namespace PeptideToProteinMapper
{
    /// <summary>
    /// This program uses PeptideToProteinMapEngine.dll to read in a file with peptide sequences, then
    /// searches for the given peptides in a protein sequence file (.Fasta or tab-delimited text)
    /// using ProteinCoverageSummarizer.dll
    ///
    /// This program is similar to the ProteinCoverageSummarizer, but it is a console-only application
    /// In addition, this program supports reading Inspect output files
    ///
    /// Example command Line
    /// I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath
    /// </summary>
    public static class Program
    {
        // Ignore Spelling: yyyy-MM-dd, hh:mm:ss tt

        public const string PROGRAM_DATE = "October 24, 2020";

        private static string mParameterFilePath;
        private static string mInspectParameterFilePath;
        private static clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants mInputFileFormatCode;
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
            string logFilePath;
            bool openingExistingFile;
            try
            {
                logFilePath = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
                logFilePath += "_VerboseLog_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                openingExistingFile = File.Exists(logFilePath);
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

        public static int Main()
        {
            // Returns 0 if no error, error code if an error
            var commandLineParser = new clsParseCommandLine();

            var returnCode = 0;

            mParameterFilePath = string.Empty;
            mInspectParameterFilePath = string.Empty;
            mInputFileFormatCode = clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.AutoDetermine;

            mLogMessagesToFile = false;
            mLogFilePath = string.Empty;
            mLogDirectoryPath = string.Empty;

            try
            {
                var options = new ProteinCoverageSummarizerOptions();

                var proceed = false;
                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser, options))
                        proceed = true;
                }

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
                            returnCode = -1;
                            return returnCode;
                        }

                        if (string.IsNullOrWhiteSpace(options.ProteinInputFilePath))
                        {
                            ShowErrorMessage("Protein input file must be defined via /R");
                            returnCode = -1;
                            return returnCode;
                        }

                        mPeptideToProteinMapEngine = new clsPeptideToProteinMapEngine(options)
                        {
                            LogMessagesToFile = mLogMessagesToFile,
                            LogFilePath = mLogFilePath,
                            LogDirectoryPath = mLogDirectoryPath,
                            PeptideInputFileFormat = mInputFileFormatCode,
                            InspectParameterFilePath = mInspectParameterFilePath
                        };

                        mPeptideToProteinMapEngine.StatusEvent += PeptideToProteinMapEngine_StatusEvent;
                        mPeptideToProteinMapEngine.ErrorEvent += PeptideToProteinMapEngine_ErrorEvent;
                        mPeptideToProteinMapEngine.WarningEvent += PeptideToProteinMapEngine_WarningEvent;

                        mPeptideToProteinMapEngine.ProgressUpdate += PeptideToProteinMapEngine_ProgressChanged;
                        mPeptideToProteinMapEngine.ProgressReset += PeptideToProteinMapEngine_ProgressReset;

                        mPeptideToProteinMapEngine.ProcessFilesWildcard(options.PeptideInputFilePath, options.OutputDirectoryPath, mParameterFilePath);

                        if (mVerboseLogFile != null)
                        {
                            mVerboseLogFile.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error initializing the Peptide to Protein Mapper Options " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in modMain->Main: " + Environment.NewLine + ex.Message);
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

            Console.Write(taskDescription + ": " + percentComplete.ToString() + "% ");
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }
        }

        private static string GetAppVersion()
        {
            return PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine CommandLineParser, ProteinCoverageSummarizerOptions options)
        {
            // Returns True if no problems; otherwise, returns false
            // /I:PeptideInputFilePath /R: ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath

            var validParameters = new List<string> { "I", "O", "R", "P", "F", "N", "G", "H", "K", "A", "L", "LogDir", "LogFolder", "VerboseLog" };

            try
            {
                // Make sure no invalid parameters are present
                if (CommandLineParser.InvalidParametersPresent(validParameters))
                {
                    ShowErrorMessage("Invalid command line parameters",
                        (from item in CommandLineParser.InvalidParameters(validParameters) select ("/" + item)).ToList());
                    return false;
                }

                // Query commandLineParser to see if various parameters are present
                if (CommandLineParser.RetrieveValueForParameter("I", out var inputFilePath))
                {
                    options.PeptideInputFilePath = inputFilePath;
                }
                else if (CommandLineParser.NonSwitchParameterCount > 0)
                {
                    options.PeptideInputFilePath = CommandLineParser.RetrieveNonSwitchParameter(0);
                }

                if (CommandLineParser.RetrieveValueForParameter("O", out var outputDirectoryPath))
                    options.OutputDirectoryPath = outputDirectoryPath;

                if (CommandLineParser.RetrieveValueForParameter("R", out var proteinInputFilePath))
                    options.ProteinInputFilePath = proteinInputFilePath;

                if (CommandLineParser.RetrieveValueForParameter("P", out var parameterFilePath))
                    mParameterFilePath = parameterFilePath;

                if (CommandLineParser.RetrieveValueForParameter("F", out var inputFileFormatCode))
                {
                    if (int.TryParse(inputFileFormatCode, out var inputFileFormatCodeValue))
                    {
                        try
                        {
                            mInputFileFormatCode = (clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants)inputFileFormatCodeValue;
                        }
                        catch (Exception)
                        {
                            // Conversion failed; leave mInputFileFormatCode unchanged
                        }
                    }
                }

                if (CommandLineParser.RetrieveValueForParameter("N", out var inspectParameterFilePath))
                    mInspectParameterFilePath = inspectParameterFilePath;

                if (CommandLineParser.RetrieveValueForParameter("G", out _))
                    options.IgnoreILDifferences = true;

                if (CommandLineParser.RetrieveValueForParameter("H", out _))
                    options.OutputProteinSequence = false;

                if (CommandLineParser.RetrieveValueForParameter("K", out _))
                    options.SearchAllProteinsSkipCoverageComputationSteps = true;

                if (CommandLineParser.RetrieveValueForParameter("A", out _))
                    options.SaveSourceDataPlusProteinsFile = true;

                if (CommandLineParser.RetrieveValueForParameter("L", out var logFilePath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFilePath))
                    {
                        mLogFilePath = logFilePath;
                    }
                }

                if (CommandLineParser.RetrieveValueForParameter("LogDir", out var logDirectoryPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logDirectoryPath))
                    {
                        mLogDirectoryPath = logDirectoryPath;
                    }
                }

                if (CommandLineParser.RetrieveValueForParameter("LogFolder", out var logFolderPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFolderPath))
                    {
                        mLogDirectoryPath = logFolderPath;
                    }
                }

                if (CommandLineParser.RetrieveValueForParameter("VerboseLog", out _))
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

        private static void ShowErrorMessage(string title, List<string> errorMessages)
        {
            ConsoleMsgUtils.ShowErrors(title, errorMessages);
        }

        private static void ShowProgramHelp()
        {
            try
            {
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "This program reads in a text file containing peptide sequences. " +
                    "It then searches the specified .fasta or text file containing protein names and sequences " +
                    "(and optionally descriptions) to find the proteins that contain each peptide. " +
                    "It will also compute the sequence coverage percent for each protein (disable using /K)."));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(Assembly.GetExecutingAssembly().Location));
                Console.WriteLine(" /I:PeptideInputFilePath /R:ProteinInputFilePath");
                Console.WriteLine(" [/O:OutputDirectoryName] [/P:ParameterFilePath] [/F:FileFormatCode] ");
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
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.AutoDetermine + "=Auto Determine: Treated as /F:1 unless name ends in _inspect.txt, then /F:3");
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.PeptideListFile + "=Peptide sequence in the 1st column (subsequent columns are ignored)");
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.ProteinAndPeptideFile + "=Protein name in 1st column and peptide sequence 2nd column");
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.InspectResultsFile + "=Inspect search results file (peptide sequence in the 3rd column)");
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.MSGFPlusResultsFile + "=MS-GF+ search results file (peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan')");
                Console.WriteLine("   " + clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.PHRPFile + "=SEQUEST, X!Tandem, Inspect, or MS-GF+ PHRP data file");
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "When processing an Inspect search results file, use /N to specify the Inspect parameter file used " +
                    "(required for determining the mod names embedded in the identified peptides)."));
                Console.WriteLine();

                Console.WriteLine("Use /G to ignore I/L differences when finding peptides in proteins or computing coverage");
                Console.WriteLine("Use /H to suppress (hide) the protein sequence in the _coverage.txt file");
                Console.WriteLine("Use /K to skip the protein coverage computation steps (enabling faster processing)");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /A to create a copy of the source file, but with a new column listing the mapped protein for each peptide. " +
                    "If a peptide maps to multiple proteins, then multiple lines will be listed"));

                Console.WriteLine("Use /L to create a log file, optionally specifying the file name");
                Console.WriteLine("Use /LogDir to define the directory in which the log file should be created");
                Console.WriteLine("Use /VerboseLog to create a detailed log file");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2008");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();
                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://omics.pnl.gov or https://panomics.pnl.gov/");
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

            if (mVerboseLogFile != null)
            {
                if (taskDescription == null)
                    taskDescription = string.Empty;
                if ((taskDescription ?? "") == (mVerboseLoggingMostRecentMessage ?? ""))
                {
                    mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" +
                                              percentComplete.ToString() + "\t" +
                                              ".");
                }
                else
                {
                    mVerboseLoggingMostRecentMessage = string.Copy(taskDescription);

                    mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" +
                                              percentComplete.ToString() + "\t" +
                                              taskDescription);
                }
            }
        }

        private static void PeptideToProteinMapEngine_ProgressReset()
        {
            mLastProgressReportTime = DateTime.UtcNow;
            mLastPercentDisplayed = DateTime.UtcNow;
        }
    }
}
