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
using PRISM;
using PRISM.FileProcessor;
using ProteinCoverageSummarizer;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// This class uses ProteinCoverageSummarizer.dll to read in a protein FASTA file or delimited protein info file along with
    /// an accompanying file with peptide sequences to then compute the percent coverage of each of the proteins
    /// </summary>
    public class clsProteinCoverageSummarizerRunner : ProcessFilesBase
    {
        // Ignore Spelling: Nikša

        #region "Class wide variables"

        private bool mOptionsShown;

        private clsProteinCoverageSummarizer mProteinCoverageSummarizer;

        private string mStatusMessage;

        #endregion

        #region "Properties"

        /// <summary>
        /// Set this to true if the calling application will handle events
        /// </summary>
        public bool CallingAppHandlesEvents { get; set; }

        /// <summary>
        /// Protein coverage summarizer options
        /// </summary>
        public ProteinCoverageSummarizerOptions Options { get; }

        /// <summary>
        /// Protein to peptide map file path
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public string ProteinToPeptideMappingFilePath => mProteinCoverageSummarizer.ProteinToPeptideMappingFilePath;

        /// <summary>
        /// Results file path
        /// </summary>
        public string ResultsFilePath => mProteinCoverageSummarizer.ResultsFilePath;

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage => mStatusMessage;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsProteinCoverageSummarizerRunner()
        {
            Options = new ProteinCoverageSummarizerOptions();
            InitializeVariables();
        }

        /// <summary>
        /// Constructor that accepts an options class
        /// </summary>
        /// <param name="options"></param>
        public clsProteinCoverageSummarizerRunner(ProteinCoverageSummarizerOptions options)
        {
            Options = options;
            InitializeVariables();
        }

        /// <summary>
        /// Abort processing now
        /// </summary>
        public override void AbortProcessingNow()
        {
            base.AbortProcessingNow();
            mProteinCoverageSummarizer?.AbortProcessingNow();
        }

        /// <summary>
        /// Get the error message
        /// </summary>
        public override string GetErrorMessage()
        {
            return GetBaseClassErrorMessage();
        }

        private void InitializeVariables()
        {
            CallingAppHandlesEvents = false;

            AbortProcessing = false;
            mStatusMessage = string.Empty;

            mProteinCoverageSummarizer = new clsProteinCoverageSummarizer(Options);
            RegisterEvents(mProteinCoverageSummarizer);

            mProteinCoverageSummarizer.ProgressChanged += ProteinCoverageSummarizer_ProgressChanged;

            mProteinCoverageSummarizer.ProgressReset += ProteinCoverageSummarizer_ProgressReset;
        }

        /// <summary>
        /// Load settings from an XML-based parameter file
        /// </summary>
        /// <param name="parameterFilePath"></param>
        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            return mProteinCoverageSummarizer.LoadParameterFileSettings(parameterFilePath);
        }

        /// <summary>
        /// Process the file to compute protein sequence coverage
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="parameterFilePath"></param>
        /// <param name="resetErrorCode"></param>
        /// <returns>True if success, false if an error</returns>
        public override bool ProcessFile(string inputFilePath, string outputDirectoryPath, string parameterFilePath, bool resetErrorCode)
        {
            mStatusMessage = string.Empty;

            if (resetErrorCode)
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
            }

            try
            {
                if (!mOptionsShown)
                {
                    mOptionsShown = true;
                    Console.WriteLine("Processing Options");
                    Console.WriteLine();
                    Console.WriteLine("{0,-35} {1}", "Input File:", PathUtils.CompactPathString(inputFilePath, 80));
                    Console.WriteLine("{0,-35} {1}", "Output Directory:", PathUtils.CompactPathString(outputDirectoryPath, 80));
                    Console.WriteLine("{0,-35} {1}", "Proteins File:", PathUtils.CompactPathString(Options.ProteinInputFilePath, 80));

                    if (!string.IsNullOrWhiteSpace(parameterFilePath))
                    {
                        Console.WriteLine("{0,-35} {1}", "Parameter File:", parameterFilePath);
                    }

                    Console.WriteLine("{0,-35} {1} (type {2})", "Input File Format:", Options.PeptideFileFormatCode, (int)Options.PeptideFileFormatCode);

                    Console.WriteLine("{0,-35} {1}", "Skip first line (headers):", Options.PeptideFileSkipFirstLine || Options.PeptideFileFormatCode == ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames);
                    Console.WriteLine("{0,-35} {1}", "Ignore I/L Differences:", Options.IgnoreILDifferences);
                    Console.WriteLine("{0,-35} {1}", "Match Prefix and Suffix Residues:", Options.MatchPeptidePrefixAndSuffixToProtein);
                    Console.WriteLine("{0,-35} {1}", "Remove symbol characters:", Options.RemoveSymbolCharacters);
                    Console.WriteLine("{0,-35} {1}", "Search all proteins:", Options.SearchAllProteinsForPeptideSequence);
                    Console.WriteLine("{0,-35} {1}", "Skip Coverage Computation:", Options.SearchAllProteinsSkipCoverageComputationSteps);
                    Console.WriteLine("{0,-35} {1}", "Track peptide counts:", Options.TrackPeptideCounts);
                    Console.WriteLine();
                    Console.WriteLine("{0,-35} {1}", "Create _AllProteins.txt:", Options.SaveSourceDataPlusProteinsFile);
                    Console.WriteLine("{0,-35} {1}", "Create protein to peptide map file:", Options.SaveProteinToPeptideMappingFile);
                    Console.WriteLine("{0,-35} {1}", "Output Protein Sequence:", Options.OutputProteinSequence);

                    Console.WriteLine();
                }

                // Show the progress form
                if (!CallingAppHandlesEvents)
                {
                    Console.WriteLine(base.ProgressStepDescription);
                }

                // Call mProteinCoverageSummarizer.ProcessFile to perform the work
                mProteinCoverageSummarizer.Options.KeepDB = Options.KeepDB;
                var success = mProteinCoverageSummarizer.ProcessFile(inputFilePath, outputDirectoryPath, parameterFilePath, true);

                if (!success)
                {
                    switch (mProteinCoverageSummarizer.ErrorCode)
                    {
                        case clsProteinCoverageSummarizer.ProteinCoverageErrorCodes.InvalidInputFilePath:
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                            break;

                        case clsProteinCoverageSummarizer.ProteinCoverageErrorCodes.ErrorReadingParameterFile:
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                            break;

                        case clsProteinCoverageSummarizer.ProteinCoverageErrorCodes.FilePathError:
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError);
                            break;

                        default:
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.UnspecifiedError);
                            break;
                    }

                    mStatusMessage = mProteinCoverageSummarizer.ErrorMessage;
                }

                mProteinCoverageSummarizer.ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizerRunner.ProcessFile_Complete");

                return success;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error in ProcessFile:" + Environment.NewLine + ex.Message;
                OnErrorEvent(mStatusMessage, ex);
                return false;
            }
        }

        private void ProteinCoverageSummarizer_ProgressChanged(string taskDescription, float percentComplete)
        {
            UpdateProgress(taskDescription, percentComplete);

            // if (mUseProgressForm && mProgressForm != null)
            // {
            //     mProgressForm.UpdateCurrentTask(taskDescription);
            //     mProgressForm.UpdateProgressBar(percentComplete);
            //     Windows.Forms.Application.DoEvents();
            // }
        }

        private void ProteinCoverageSummarizer_ProgressReset()
        {
            ResetProgress(mProteinCoverageSummarizer.ProgressStepDescription);

            // if (mUseProgressForm && mProgressForm != null)
            // {
            //     mProgressForm.UpdateProgressBar(0, true);
            //     mProgressForm.UpdateCurrentTask(mProteinCoverageSummarizer.ProgressStepDescription);
            // }
        }
    }
}