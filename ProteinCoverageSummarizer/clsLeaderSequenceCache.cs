// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Started August 2007
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
using System.Text.RegularExpressions;

namespace ProteinCoverageSummarizer
{
    public delegate void ProgressCompleteEventHandler();

    /// <summary>
    /// This class tracks the first n letters of each peptide sent to it, while also
    /// tracking the peptides and the location of those peptides in the leader sequence hash table
    /// </summary>
    public class clsLeaderSequenceCache
    {
        // Ignore Spelling: structs, leucines

        public clsLeaderSequenceCache()
        {
            InitializeVariables();
        }

        #region "Constants and Enums"

        public const int DEFAULT_LEADER_SEQUENCE_LENGTH = 5;
        public const int MINIMUM_LEADER_SEQUENCE_LENGTH = 5;

        private const int INITIAL_LEADER_SEQUENCE_COUNT_TO_RESERVE = 10000;
        public const int MAX_LEADER_SEQUENCE_COUNT = 500000;

        #endregion

        #region "Structures"

        public struct udtPeptideSequenceInfoType
        {
            /// <summary>
            /// Protein name (optional)
            /// </summary>
            public string ProteinName;

            /// <summary>
            /// Peptide amino acids (stored as uppercase letters)
            /// </summary>
            public string PeptideSequence;

            /// <summary>
            /// Prefix residue
            /// </summary>
            public char Prefix;

            /// <summary>
            /// Suffix residue
            /// </summary>
            public char Suffix;

            /// <summary>
            /// Peptide sequence where leucines have been changed to isoleucine
            /// </summary>
            /// <remarks>Only used if mIgnoreILDifferences is True</remarks>
            public string PeptideSequenceLtoI;

            /// <summary>
            /// Prefix residue; if leucine, changed to isoleucine
            /// </summary>
            /// <remarks>Only used if mIgnoreILDifferences is True</remarks>
            public char PrefixLtoI;

            /// <summary>
            /// Suffix residue; if leucine, changed to isoleucine
            /// </summary>
            /// <remarks>Only used if mIgnoreILDifferences is True</remarks>
            public char SuffixLtoI;

            /// <summary>
            /// Show the peptide sequence, including prefix and suffix
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Prefix.ToString()))
                {
                    return PeptideSequence;
                }

                return Prefix + "." + PeptideSequence + "." + Suffix;
            }
        }

        #endregion

        #region "Class wide Variables"

        private Dictionary<string, int> mLeaderSequences;

        public int mCachedPeptideCount;
        public udtPeptideSequenceInfoType[] mCachedPeptideSeqInfo = new udtPeptideSequenceInfoType[0];

        /// <summary>
        /// Parallel to mCachedPeptideSeqInfo
        /// </summary>
        private int[] mCachedPeptideToHashIndexPointer = new int[0];

        private bool mIndicesSorted;

        private string mErrorMessage;
        private bool mAbortProcessing;

        public event ProgressResetEventHandler ProgressReset;

        /// <summary>
        /// Progress changed event
        /// </summary>
        /// <param name="taskDescription"></param>
        /// <param name="percentComplete">Value between 0 and 100, but can contain decimal percentage values</param>
        public event ProgressChangedEventHandler ProgressChanged;

        public event ProgressCompleteEventHandler ProgressComplete;

        protected string mProgressStepDescription;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <remarks>
        /// Value between 0 and 100, but can contain decimal percentage values
        /// </remarks>
        protected float mProgressPercentComplete;

        #endregion

        #region "Properties"

        public int CachedPeptideCount => mCachedPeptideCount;

        public string ErrorMessage => mErrorMessage;

        public bool IgnoreILDifferences { get; set; }
        public int LeaderSequenceMinimumLength { get; set; }

        public string ProgressStepDescription => mProgressStepDescription;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <remarks>
        /// Value between 0 and 100, but can contain decimal percentage values
        /// </remarks>
        public float ProgressPercentComplete => Convert.ToSingle(Math.Round(mProgressPercentComplete, 2));

        #endregion

        public void AbortProcessingNow()
        {
            mAbortProcessing = true;
        }

        /// <summary>
        /// Caches the peptide and updates mLeaderSequences
        /// </summary>
        /// <param name="peptideSequence">Peptide sequence</param>
        /// <param name="proteinName">Protein name</param>
        /// <param name="prefixResidue">Prefix residue</param>
        /// <param name="suffixResidue">Suffix residue</param>
        /// <returns></returns>
        public bool CachePeptide(string peptideSequence, string proteinName, char prefixResidue, char suffixResidue)
        {
            try
            {
                if (peptideSequence == null || peptideSequence.Length < LeaderSequenceMinimumLength)
                {
                    // Peptide is too short; cannot process it
                    mErrorMessage = "Peptide length is shorter than " + LeaderSequenceMinimumLength.ToString() + "; unable to cache the peptide";
                    return false;
                }
                else
                {
                    mErrorMessage = string.Empty;
                }

                // Make sure the residues are capitalized
                peptideSequence = peptideSequence.ToUpper();
                if (char.IsLetter(prefixResidue))
                    prefixResidue = char.ToUpper(prefixResidue);
                if (char.IsLetter(suffixResidue))
                    suffixResidue = char.ToUpper(suffixResidue);
                string leaderSequence = peptideSequence.Substring(0, LeaderSequenceMinimumLength);
                char prefixResidueLtoI = prefixResidue;
                char suffixResidueLtoI = suffixResidue;
                if (IgnoreILDifferences)
                {
                    // Replace all L characters with I
                    leaderSequence = leaderSequence.Replace('L', 'I');

                    if (prefixResidueLtoI == 'L')
                        prefixResidueLtoI = 'I';
                    if (suffixResidueLtoI == 'L')
                        suffixResidueLtoI = 'I';
                }

                int hashIndexPointer;

                // Look for leaderSequence in mLeaderSequences
                if (!mLeaderSequences.TryGetValue(leaderSequence, out hashIndexPointer))
                {
                    // leaderSequence was not found; add it and initialize intHashIndexPointer
                    hashIndexPointer = mLeaderSequences.Count;
                    mLeaderSequences.Add(leaderSequence, hashIndexPointer);
                }

                // Expand mCachedPeptideSeqInfo if needed
                if (mCachedPeptideCount >= mCachedPeptideSeqInfo.Length && mCachedPeptideCount < MAX_LEADER_SEQUENCE_COUNT)
                {
                    var oldMCachedPeptideSeqInfo = mCachedPeptideSeqInfo;
                    mCachedPeptideSeqInfo = new udtPeptideSequenceInfoType[(mCachedPeptideSeqInfo.Length * 2)];
                    Array.Copy(oldMCachedPeptideSeqInfo, mCachedPeptideSeqInfo, Math.Min(mCachedPeptideSeqInfo.Length * 2, oldMCachedPeptideSeqInfo.Length));

                    var oldMCachedPeptideToHashIndexPointer = mCachedPeptideToHashIndexPointer;
                    mCachedPeptideToHashIndexPointer = new int[mCachedPeptideSeqInfo.Length];
                    Array.Copy(oldMCachedPeptideToHashIndexPointer, mCachedPeptideToHashIndexPointer, Math.Min(mCachedPeptideSeqInfo.Length, oldMCachedPeptideToHashIndexPointer.Length));
                }

                // Add peptideSequence to mCachedPeptideSeqInfo
                var pepSeq = mCachedPeptideSeqInfo[mCachedPeptideCount];
                pepSeq.ProteinName = string.Copy(proteinName);
                pepSeq.PeptideSequence = string.Copy(peptideSequence);
                pepSeq.Prefix = prefixResidue;
                pepSeq.Suffix = suffixResidue;
                pepSeq.PrefixLtoI = prefixResidueLtoI;
                pepSeq.SuffixLtoI = suffixResidueLtoI;
                if (IgnoreILDifferences)
                {
                    pepSeq.PeptideSequenceLtoI = peptideSequence.Replace('L', 'I');
                }
                // C# list of structs: copy value back to make sure it's stored.
                mCachedPeptideSeqInfo[mCachedPeptideCount] = pepSeq;

                // Update the peptide to Hash Index pointer array
                mCachedPeptideToHashIndexPointer[mCachedPeptideCount] = hashIndexPointer;
                mCachedPeptideCount += 1;
                mIndicesSorted = false;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CachePeptide", ex);
            }
        }

        public bool DetermineShortestPeptideLengthInFile(
            string inputFilePath, int terminatorSize,
            bool peptideFileSkipFirstLine, char peptideInputFileDelimiter,
            int columnNumWithPeptideSequence)
        {
            // Parses inputFilePath examining column columnNumWithPeptideSequence to determine the minimum peptide sequence length present
            // Updates mLeaderSequenceMinimumLength if successful, though the minimum length is not allowed to be less than MINIMUM_LEADER_SEQUENCE_LENGTH

            // columnNumWithPeptideSequence should be 1 if the peptide sequence is in the first column, 2 if in the second, etc.

            // Define a RegEx to replace all of the non-letter characters
            var reReplaceSymbols = new Regex("[^A-Za-z]", RegexOptions.Compiled);

            try
            {
                int validPeptideCount = 0;
                int leaderSeqMinimumLength = 0;

                // Open the file and read in the lines
                using (var reader = new StreamReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    int linesRead = 1;
                    long bytesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        if (mAbortProcessing)
                            break;
                        string dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;
                        bytesRead += dataLine.Length + terminatorSize;

                        dataLine = dataLine.TrimEnd();

                        if (linesRead % 100 == 1)
                        {
                            UpdateProgress("Scanning input file to determine minimum peptide length: " + linesRead.ToString(),
                                           bytesRead / Convert.ToSingle(reader.BaseStream.Length) * 100);
                        }

                        if (linesRead == 1 && peptideFileSkipFirstLine)
                        {
                            // Do nothing, skip the first line
                        }
                        else if (dataLine.Length > 0)
                        {
                            bool validLine;
                            string peptideSequence = "";

                            try
                            {
                                var dataCols = dataLine.Split(peptideInputFileDelimiter);

                                if (columnNumWithPeptideSequence >= 1 & columnNumWithPeptideSequence < dataCols.Length - 1)
                                {
                                    peptideSequence = dataCols[columnNumWithPeptideSequence - 1];
                                }
                                else
                                {
                                    peptideSequence = dataCols[0];
                                }

                                validLine = true;
                            }
                            catch (Exception ex)
                            {
                                validLine = false;
                            }

                            if (validLine)
                            {
                                if (peptideSequence.Length >= 4)
                                {
                                    // Check for, and remove any prefix or suffix residues
                                    if (peptideSequence[1] == '.' && peptideSequence[peptideSequence.Length - 2] == '.')
                                    {
                                        peptideSequence = peptideSequence.Substring(2, peptideSequence.Length - 4);
                                    }
                                }

                                // Remove any non-letter characters
                                peptideSequence = reReplaceSymbols.Replace(peptideSequence, string.Empty);

                                if (peptideSequence.Length >= MINIMUM_LEADER_SEQUENCE_LENGTH)
                                {
                                    if (validPeptideCount == 0)
                                    {
                                        leaderSeqMinimumLength = peptideSequence.Length;
                                    }
                                    else if (peptideSequence.Length < leaderSeqMinimumLength)
                                    {
                                        leaderSeqMinimumLength = peptideSequence.Length;
                                    }

                                    validPeptideCount += 1;
                                }
                            }
                        }

                        linesRead += 1;
                    }
                }

                bool success;

                if (validPeptideCount == 0)
                {
                    // No valid peptides were found; either no peptides are in the file or they're all shorter than MINIMUM_LEADER_SEQUENCE_LENGTH
                    LeaderSequenceMinimumLength = MINIMUM_LEADER_SEQUENCE_LENGTH;
                    success = false;
                }
                else
                {
                    LeaderSequenceMinimumLength = leaderSeqMinimumLength;
                    success = true;
                }

                OperationComplete();
                return success;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineShortestPeptideLengthInFile", ex);
            }
        }

        public int GetFirstPeptideIndexForLeaderSequence(string leaderSequenceToFind)
        {
            // Looks up the first index value in mCachedPeptideSeqInfo that matches strLeaderSequenceToFind
            // Returns the index value if found, or -1 if not found
            // Calls SortIndices if mIndicesSorted = False

            int targetHashIndex;

            if (!mLeaderSequences.TryGetValue(leaderSequenceToFind, out targetHashIndex))
            {
                return -1;
            }

            // Item found in mLeaderSequences
            // Return the first peptide index value mapped to the leader sequence

            if (!mIndicesSorted)
            {
                SortIndices();
            }

            int cachedPeptideMatchIndex = Array.BinarySearch(mCachedPeptideToHashIndexPointer, 0, mCachedPeptideCount, targetHashIndex);

            while (cachedPeptideMatchIndex > 0 && mCachedPeptideToHashIndexPointer[cachedPeptideMatchIndex - 1] == targetHashIndex)
                cachedPeptideMatchIndex -= 1;

            return cachedPeptideMatchIndex;
        }

        public int GetNextPeptideWithLeaderSequence(int intCachedPeptideMatchIndexCurrent)
        {
            if (intCachedPeptideMatchIndexCurrent < mCachedPeptideCount - 1)
            {
                if (mCachedPeptideToHashIndexPointer[intCachedPeptideMatchIndexCurrent + 1] == mCachedPeptideToHashIndexPointer[intCachedPeptideMatchIndexCurrent])
                {
                    return intCachedPeptideMatchIndexCurrent + 1;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        public void InitializeCachedPeptides()
        {
            mCachedPeptideCount = 0;
            mCachedPeptideSeqInfo = new udtPeptideSequenceInfoType[INITIAL_LEADER_SEQUENCE_COUNT_TO_RESERVE];
            mCachedPeptideToHashIndexPointer = new int[mCachedPeptideSeqInfo.Length];

            mIndicesSorted = false;

            if (mLeaderSequences == null)
            {
                mLeaderSequences = new Dictionary<string, int>();
            }
            else
            {
                mLeaderSequences.Clear();
            }
        }

        public void InitializeVariables()
        {
            LeaderSequenceMinimumLength = DEFAULT_LEADER_SEQUENCE_LENGTH;
            mErrorMessage = string.Empty;
            mAbortProcessing = false;

            IgnoreILDifferences = false;

            InitializeCachedPeptides();
        }

        private void SortIndices()
        {
            Array.Sort(mCachedPeptideToHashIndexPointer, mCachedPeptideSeqInfo, 0, mCachedPeptideCount);
            mIndicesSorted = true;
        }

        protected void ResetProgress()
        {
            ProgressReset?.Invoke();
        }

        protected void ResetProgress(string strProgressStepDescription)
        {
            UpdateProgress(strProgressStepDescription, 0);
            ProgressReset?.Invoke();
        }

        protected void UpdateProgress(string strProgressStepDescription)
        {
            UpdateProgress(strProgressStepDescription, mProgressPercentComplete);
        }

        protected void UpdateProgress(float sngPercentComplete)
        {
            UpdateProgress(ProgressStepDescription, sngPercentComplete);
        }

        protected void UpdateProgress(string strProgressStepDescription, float sngPercentComplete)
        {
            mProgressStepDescription = string.Copy(strProgressStepDescription);
            if (sngPercentComplete < 0)
            {
                sngPercentComplete = 0;
            }
            else if (sngPercentComplete > 100)
            {
                sngPercentComplete = 100;
            }

            mProgressPercentComplete = sngPercentComplete;

            ProgressChanged?.Invoke(ProgressStepDescription, ProgressPercentComplete);
        }

        protected void OperationComplete()
        {
            ProgressComplete?.Invoke();
        }
    }
}