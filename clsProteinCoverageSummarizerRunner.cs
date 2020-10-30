// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
// Program started June 14, 2005
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
using ProteinCoverageSummarizer;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
    /// an accompanying file with peptide sequences to then compute the percent coverage of each of the proteins
    /// </summary>
    public class clsProteinCoverageSummarizerRunner : PRISM.FileProcessor.ProcessFilesBase
    {
        // Ignore Spelling: Nikša

        #region "Class wide variables"

        private clsProteinCoverageSummarizer mProteinCoverageSummarizer;

        private string mStatusMessage;

        #endregion

        #region "Properties"

        /// <summary>
        /// Set this to true if the calling app will handle events
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
        /// <returns></returns>
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

            this.mProteinCoverageSummarizer.ProgressChanged += this.ProteinCoverageSummarizer_ProgressChanged;

            this.mProteinCoverageSummarizer.ProgressReset += this.ProteinCoverageSummarizer_ProgressReset;
        }

        /// <summary>
        /// Load settings from an XML-based parameter file
        /// </summary>
        /// <param name="parameterFilePath"></param>
        /// <returns></returns>
        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            return mProteinCoverageSummarizer.LoadParameterFileSettings(parameterFilePath);
        }

        /// <summary>
        /// Process the file to compute protein sequence coverage
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputFolderPath"></param>
        /// <param name="parameterFilePath"></param>
        /// <param name="resetErrorCode"></param>
        /// <returns>True if success, false if an error</returns>
        public override bool ProcessFile(string inputFilePath, string outputFolderPath, string parameterFilePath, bool resetErrorCode)
        {
            mStatusMessage = string.Empty;

            if (resetErrorCode)
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
            }

            try
            {
                // Show the progress form
                if (!CallingAppHandlesEvents)
                {
                    Console.WriteLine(base.ProgressStepDescription);
                }

                // Call mProteinCoverageSummarizer.ProcessFile to perform the work
                mProteinCoverageSummarizer.Options.KeepDB = Options.KeepDB;
                var success = mProteinCoverageSummarizer.ProcessFile(inputFilePath, outputFolderPath, parameterFilePath, true);

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