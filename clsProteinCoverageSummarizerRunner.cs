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
using ProteinFileReader;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
    /// an accompanying file with peptide sequences to then compute the percent coverage of each of the proteins
    /// </summary>
    public class clsProteinCoverageSummarizerRunner : PRISM.FileProcessor.ProcessFilesBase
    {
        public clsProteinCoverageSummarizerRunner()
        {
            InitializeVariables();
        }

        #region "Class wide variables"

        private clsProteinCoverageSummarizer mProteinCoverageSummarizer;

        private string mStatusMessage;

        #endregion

        #region "Properties"

        public bool CallingAppHandlesEvents { get; set; }

        public bool IgnoreILDifferences
        {
            get => mProteinCoverageSummarizer.IgnoreILDifferences;
            set => mProteinCoverageSummarizer.IgnoreILDifferences = value;
        }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB
        {
            get => mProteinCoverageSummarizer.KeepDB;
            set => mProteinCoverageSummarizer.KeepDB = value;
        }

        public bool MatchPeptidePrefixAndSuffixToProtein
        {
            get => mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein;
            set => mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = value;
        }

        public bool OutputProteinSequence
        {
            get => mProteinCoverageSummarizer.OutputProteinSequence;
            set => mProteinCoverageSummarizer.OutputProteinSequence = value;
        }

        public clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode PeptideFileFormatCode
        {
            get => mProteinCoverageSummarizer.PeptideFileFormatCode;
            set => mProteinCoverageSummarizer.PeptideFileFormatCode = value;
        }

        public bool PeptideFileSkipFirstLine
        {
            get => mProteinCoverageSummarizer.PeptideFileSkipFirstLine;
            set => mProteinCoverageSummarizer.PeptideFileSkipFirstLine = value;
        }

        public char PeptideInputFileDelimiter
        {
            get => mProteinCoverageSummarizer.PeptideInputFileDelimiter;
            set => mProteinCoverageSummarizer.PeptideInputFileDelimiter = value;
        }

        public char ProteinDataDelimitedFileDelimiter
        {
            get => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileDelimiter;
            set => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileDelimiter = value;
        }

        public DelimitedFileReader.eDelimitedFileFormatCode ProteinDataDelimitedFileFormatCode
        {
            get => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileFormatCode;
            set => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileFormatCode = value;
        }

        public bool ProteinDataDelimitedFileSkipFirstLine
        {
            get => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileSkipFirstLine;
            set => mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileSkipFirstLine = value;
        }

        public bool ProteinDataRemoveSymbolCharacters
        {
            get => mProteinCoverageSummarizer.ProteinDataCache.RemoveSymbolCharacters;
            set => mProteinCoverageSummarizer.ProteinDataCache.RemoveSymbolCharacters = value;
        }

        public bool ProteinDataIgnoreILDifferences
        {
            get => mProteinCoverageSummarizer.ProteinDataCache.IgnoreILDifferences;
            set => mProteinCoverageSummarizer.ProteinDataCache.IgnoreILDifferences = value;
        }

        public string ProteinInputFilePath
        {
            get => mProteinCoverageSummarizer.ProteinInputFilePath;
            set => mProteinCoverageSummarizer.ProteinInputFilePath = value;
        }

        public string ProteinToPeptideMappingFilePath => mProteinCoverageSummarizer.ProteinToPeptideMappingFilePath;

        public bool RemoveSymbolCharacters
        {
            get => mProteinCoverageSummarizer.RemoveSymbolCharacters;
            set => mProteinCoverageSummarizer.RemoveSymbolCharacters = value;
        }

        public string ResultsFilePath => mProteinCoverageSummarizer.ResultsFilePath;

        public bool SaveProteinToPeptideMappingFile
        {
            get => mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile;
            set => mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile = value;
        }

        public bool SearchAllProteinsForPeptideSequence
        {
            get => mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence;
            set => mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence = value;
        }

        public bool UseLeaderSequenceHashTable
        {
            get => mProteinCoverageSummarizer.UseLeaderSequenceHashTable;
            set => mProteinCoverageSummarizer.UseLeaderSequenceHashTable = value;
        }

        public bool SearchAllProteinsSkipCoverageComputationSteps
        {
            get => mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps;
            set => mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps = value;
        }

        public string StatusMessage => mStatusMessage;

        public bool TrackPeptideCounts
        {
            get => mProteinCoverageSummarizer.TrackPeptideCounts;
            set => mProteinCoverageSummarizer.TrackPeptideCounts = value;
        }

        #endregion

        public override void AbortProcessingNow()
        {
            base.AbortProcessingNow();
            if (mProteinCoverageSummarizer != null)
            {
                mProteinCoverageSummarizer.AbortProcessingNow();
            }
        }

        public override string GetErrorMessage()
        {
            return GetBaseClassErrorMessage();
        }

        private void InitializeVariables()
        {
            CallingAppHandlesEvents = false;

            AbortProcessing = false;
            mStatusMessage = string.Empty;

            mProteinCoverageSummarizer = new clsProteinCoverageSummarizer();
            RegisterEvents(mProteinCoverageSummarizer);

            this.mProteinCoverageSummarizer.ProgressChanged += this.ProteinCoverageSummarizer_ProgressChanged;

            this.mProteinCoverageSummarizer.ProgressReset += this.ProteinCoverageSummarizer_ProgressReset;
        }

        public bool LoadParameterFileSettings(string strParameterFilePath)
        {
            return mProteinCoverageSummarizer.LoadParameterFileSettings(strParameterFilePath);
        }

        public override bool ProcessFile(string strInputFilePath, string strOutputFolderPath, string strParameterFilePath, bool blnResetErrorCode)
        {
            bool blnSuccess;

            if (blnResetErrorCode)
            {
                base.SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
            }

            try
            {
                // Show the progress form
                if (!CallingAppHandlesEvents)
                {
                    Console.WriteLine(base.ProgressStepDescription);
                }

                // Call mProteinCoverageSummarizer.ProcessFile to perform the work
                mProteinCoverageSummarizer.KeepDB = KeepDB;
                blnSuccess = mProteinCoverageSummarizer.ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, true);

                mProteinCoverageSummarizer.ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizerRunner.ProcessFile_Complete");
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error in ProcessFile:" + Environment.NewLine + ex.Message;
                OnErrorEvent(mStatusMessage, ex);
                blnSuccess = false;
            }

            return blnSuccess;
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