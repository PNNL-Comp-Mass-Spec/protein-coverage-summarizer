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
    /// <summary>
    /// Progress complete event handler delegate
    /// </summary>
    public delegate void ProgressCompleteEventHandler();

    /// <summary>
    /// This class tracks the first n letters of each peptide sent to it, while also
    /// tracking the peptides and the location of those peptides in the leader sequence hash table
    /// </summary>
    public class clsLeaderSequenceCache
    {
        // Ignore Spelling: structs, leucines, A-Za-z

        /// <summary>
        /// Constructor
        /// </summary>
        public clsLeaderSequenceCache()
        {
            InitializeVariables();
        }

        #region "Constants and Enums"

        /// <summary>
        /// Default leader sequence length
        /// </summary>
        public const int DEFAULT_LEADER_SEQUENCE_LENGTH = 5;

        /// <summary>
        /// Minimum allowed leader sequence length
        /// </summary>
        public const int MINIMUM_LEADER_SEQUENCE_LENGTH = 5;

        private const int INITIAL_LEADER_SEQUENCE_COUNT_TO_RESERVE = 10000;

        /// <summary>
        /// Maximum leader sequence count
        /// </summary>
        public const int MAX_LEADER_SEQUENCE_COUNT = 500000;

        #endregion

        #region "Structures"

        /// <summary>
        /// Peptide sequence info structure
        /// </summary>
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

        /// <summary>
        /// Keys in this dictionary are peptides, values are the corresponding index in mCachedPeptideToHashIndexPointer
        /// </summary>
        private Dictionary<string, int> mLeaderSequences;

        /// <summary>
        /// Count of cached peptides
        /// </summary>
        public int mCachedPeptideCount;

        /// <summary>
        /// Cached peptide info
        /// </summary>
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
        public event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// Progress complete event handler
        /// </summary>
        public event ProgressCompleteEventHandler ProgressComplete;

        private string mProgressStepDescription;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <remarks>
        /// Value between 0 and 100, but can contain decimal percentage values
        /// </remarks>
        private float mProgressPercentComplete;

        #endregion

        #region "Properties"

        /// <summary>
        /// Number of cached peptides
        /// </summary>
        public int CachedPeptideCount => mCachedPeptideCount;

        public string ErrorMessage => mErrorMessage;
        /// <summary>
        /// Error message
        /// </summary>

        /// <summary>
        /// When true, treat I and L residues equally
        /// </summary>
        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// Minimum leader sequence length
        /// </summary>
        public int LeaderSequenceMinimumLength { get; set; }

        /// <summary>
        /// Progress step description
        /// </summary>
        public string ProgressStepDescription => mProgressStepDescription;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <remarks>
        /// Value between 0 and 100, but can contain decimal percentage values
        /// </remarks>
        public float ProgressPercentComplete => Convert.ToSingle(Math.Round(mProgressPercentComplete, 2));

        #endregion

        /// <summary>
        /// Abort processing now
        /// </summary>
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

                mErrorMessage = string.Empty;

                // Make sure the residues are capitalized
                peptideSequence = peptideSequence.ToUpper();
                if (char.IsLetter(prefixResidue))
                    prefixResidue = char.ToUpper(prefixResidue);
                if (char.IsLetter(suffixResidue))
                    suffixResidue = char.ToUpper(suffixResidue);
                var leaderSequence = peptideSequence.Substring(0, LeaderSequenceMinimumLength);
                var prefixResidueLtoI = prefixResidue;
                var suffixResidueLtoI = suffixResidue;
                if (IgnoreILDifferences)
                {
                    // Replace all L characters with I
                    leaderSequence = leaderSequence.Replace('L', 'I');

                    if (prefixResidueLtoI == 'L')
                        prefixResidueLtoI = 'I';
                    if (suffixResidueLtoI == 'L')
                        suffixResidueLtoI = 'I';
                }

                // Look for leaderSequence in mLeaderSequences
                if (!mLeaderSequences.TryGetValue(leaderSequence, out var hashIndexPointer))
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
                mCachedPeptideCount++;
                mIndicesSorted = false;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CachePeptide", ex);
            }
        }

        /// <summary>
        /// Parse the input file, examining column number columnNumWithPeptideSequence to determine the minimum peptide sequence length present
        /// Updates mLeaderSequenceMinimumLength if successful, though the minimum length is not allowed to be less than MINIMUM_LEADER_SEQUENCE_LENGTH
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="terminatorSize"></param>
        /// <param name="peptideFileSkipFirstLine"></param>
        /// <param name="peptideInputFileDelimiter"></param>
        /// <param name="columnNumWithPeptideSequence">Should be 1 if the peptide sequence is in the first column, 2 if in the second, etc.</param>
        /// <returns>True if success, false if an error</returns>
        public bool DetermineShortestPeptideLengthInFile(
            string inputFilePath, int terminatorSize,
            bool peptideFileSkipFirstLine, char peptideInputFileDelimiter,
            int columnNumWithPeptideSequence)
        {
            // Define a RegEx to replace all of the non-letter characters
            var reReplaceSymbols = new Regex("[^A-Za-z]", RegexOptions.Compiled);

            try
            {
                var validPeptideCount = 0;
                var leaderSeqMinimumLength = 0;

                // Open the file and read in the lines
                using (var reader = new StreamReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var linesRead = 0;
                    long bytesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        if (mAbortProcessing)
                            break;

                        var lineIn = reader.ReadLine();
                        if (string.IsNullOrEmpty(lineIn))
                            continue;

                        bytesRead += lineIn.Length + terminatorSize;

                        var dataLine = lineIn.TrimEnd();

                        linesRead++;
                        if (linesRead % 100 == 1)
                        {
                            UpdateProgress("Scanning input file to determine minimum peptide length: " + linesRead,
                                           bytesRead / Convert.ToSingle(reader.BaseStream.Length) * 100);
                        }

                        if (linesRead == 1 && peptideFileSkipFirstLine)
                        {
                            // Do nothing, skip the first line
                            continue;
                        }

                        if (dataLine.Length == 0)
                            continue;

                        bool validLine;
                        var peptideSequence = string.Empty;

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
                        catch (Exception)
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

                                validPeptideCount++;
                            }
                        }
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

        /// <summary>
        /// Looks up the first index value in mCachedPeptideSeqInfo that matches leaderSequenceToFind
        /// </summary>
        /// <param name="leaderSequenceToFind"></param>
        /// <returns>The index value if found, or -1 if not found</returns>
        /// <remarks>Calls SortIndices if mIndicesSorted = False</remarks>
        public int GetFirstPeptideIndexForLeaderSequence(string leaderSequenceToFind)
        {
            if (!mLeaderSequences.TryGetValue(leaderSequenceToFind, out var targetHashIndex))
            {
                return -1;
            }

            // Item found in mLeaderSequences
            // Return the first peptide index value mapped to the leader sequence

            if (!mIndicesSorted)
            {
                SortIndices();
            }

            var cachedPeptideMatchIndex = Array.BinarySearch(mCachedPeptideToHashIndexPointer, 0, mCachedPeptideCount, targetHashIndex);

            while (cachedPeptideMatchIndex > 0 && mCachedPeptideToHashIndexPointer[cachedPeptideMatchIndex - 1] == targetHashIndex)
                cachedPeptideMatchIndex--;

            return cachedPeptideMatchIndex;
        }

        /// <summary>
        /// Get the next peptide with the given leader sequence
        /// </summary>
        /// <param name="cachedPeptideMatchIndexCurrent"></param>
        /// <returns></returns>
        public int GetNextPeptideWithLeaderSequence(int cachedPeptideMatchIndexCurrent)
        {
            if (intCachedPeptideMatchIndexCurrent < mCachedPeptideCount - 1)
            {
                if (mCachedPeptideToHashIndexPointer[intCachedPeptideMatchIndexCurrent + 1] == mCachedPeptideToHashIndexPointer[intCachedPeptideMatchIndexCurrent])
                {
                    return intCachedPeptideMatchIndexCurrent + 1;
                }

                return -1;
            }

            return -1;
        }

        /// <summary>
        /// Initialize the cached peptides
        /// </summary>
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

        /// <summary>
        /// Reset local variables to defaults
        /// </summary>
        ///
        /// <remarks>Calls InitializeCachedPeptides</remarks>
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

        private void ResetProgress()
        {
            ProgressReset?.Invoke();
        }

        private void ResetProgress(string strProgressStepDescription)
        {
            UpdateProgress(strProgressStepDescription, 0);
            ProgressReset?.Invoke();
        }

        private void UpdateProgress(string strProgressStepDescription)
        {
            UpdateProgress(strProgressStepDescription, mProgressPercentComplete);
        }

        private void UpdateProgress(float sngPercentComplete)
        {
            UpdateProgress(ProgressStepDescription, sngPercentComplete);
        }

        private void UpdateProgress(string strProgressStepDescription, float sngPercentComplete)
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

        private void OperationComplete()
        {
            ProgressComplete?.Invoke();
        }
    }
}