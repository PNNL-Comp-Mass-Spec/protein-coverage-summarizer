// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
// Started June 2005
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PRISM;
using ProteinFileReader;

namespace ProteinCoverageSummarizer
{
    public delegate void ProgressResetEventHandler();

    /// <summary>
    /// Progress changed event
    /// </summary>
    /// <param name="taskDescription"></param>
    /// <param name="percentComplete">Value between 0 and 100, but can contain decimal percentage values</param>
    public delegate void ProgressChangedEventHandler(string taskDescription, float percentComplete);

    /// <summary>
    /// This class will read in a protein FASTA file or delimited protein info file along with
    /// an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
    /// </summary>
    [CLSCompliant(true)]
    public class clsProteinCoverageSummarizer : EventNotifier
    {
        // Ignore Spelling: Nikša, udt, Lf, struct

        public clsProteinCoverageSummarizer()
        {
            InitializeVariables();
        }

        #region "Constants and Enums"

        public const string XML_SECTION_PROCESSING_OPTIONS = "ProcessingOptions";

        public const int OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER = 3;
        public const int OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER = 7;

        public const string FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING = "_ProteinToPeptideMapping.txt";
        public const string FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS = "_AllProteins.txt";

        protected const int PROTEIN_CHUNK_COUNT = 50000;

        public enum ePeptideFileColumnOrderingCode : int
        {
            SequenceOnly = 0,
            ProteinName_PeptideSequence = 1
        }

        public enum eProteinCoverageErrorCodes
        {
            NoError = 0,
            InvalidInputFilePath = 1,
            ErrorReadingParameterFile = 2,
            FilePathError = 16,
            UnspecifiedError = -1
        }

        // Note: if you add/remove any steps, update PERCENT_COMPLETE_LEVEL_COUNT and update the population of mPercentCompleteStartLevels()
        public enum eProteinCoverageProcessingSteps
        {
            Starting = 0,
            CacheProteins = 1,
            DetermineShortestPeptideLength = 2,
            CachePeptides = 3,
            SearchProteinsUsingLeaderSequences = 4,
            SearchProteinsAgainstShortPeptides = 5,
            ComputePercentCoverage = 6,
            WriteProteinCoverageFile = 7,
            SaveAllProteinsVersionOfInputFile = 8
        }

        #endregion

        #region "Structures"
        protected struct udtPeptideCountStatsType
        {
            public int UniquePeptideCount;
            public int NonUniquePeptideCount;
        }

        #endregion

        #region "Classwide variables"
        public clsProteinFileDataCache ProteinDataCache;
        private clsLeaderSequenceCache mLeaderSequenceCache;

        // This dictionary contains entries of the form 1234::K.ABCDEFR.A
        // where the number is the protein ID and the peptide is the peptide sequence
        // The value for each entry is the number of times the peptide is present in the given protein
        // This dictionary is only populated if mTrackPeptideCounts is true
        private Dictionary<string, int> mProteinPeptideStats;

        /// <summary>
        /// This is populated by function ProcessFile()
        /// </summary>
        private string mResultsFilePath;

        private string mProteinToPeptideMappingFilePath;
        private StreamWriter mProteinToPeptideMappingOutputFile;

        private eProteinCoverageErrorCodes mErrorCode;
        private string mErrorMessage;

        private bool mAbortProcessing;

        private int mCachedProteinInfoStartIndex = -1;
        private int mCachedProteinInfoCount;

        private clsProteinFileDataCache.udtProteinInfoType[] mCachedProteinInfo;

        private bool mKeepDB;

        private Dictionary<string, List<string>> mPeptideToProteinMapResults;

        private const int PERCENT_COMPLETE_LEVEL_COUNT = 9;

        /// <summary>
        /// Array that lists the percent complete value to report at the start
        /// of each of the various processing steps performed in this procedure
        /// </summary>
        /// <remarks>The percent complete values range from 0 to 100</remarks>
        protected float[] mPercentCompleteStartLevels;

        #endregion

        #region "Progress Events and Variables"
        public event ProgressResetEventHandler ProgressReset;

        /// <summary>
        /// Progress changed event
        /// </summary>
        /// <param name="taskDescription"></param>
        /// <param name="percentComplete">Value between 0 and 100, but can contain decimal percentage values</param>
        public event ProgressChangedEventHandler ProgressChanged;

        protected eProteinCoverageProcessingSteps mCurrentProcessingStep = eProteinCoverageProcessingSteps.Starting;
        protected string mProgressStepDescription = string.Empty;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <remarks>
        /// Value between 0 and 100, but can contain decimal percentage values
        /// </remarks>
        protected float mProgressPercentComplete;

        #endregion

        #region "Properties"

        public eProteinCoverageErrorCodes ErrorCode => mErrorCode;

        public string ErrorMessage => GetErrorMessage();

        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB
        {
            get => mKeepDB;
            set
            {
                mKeepDB = value;
                if (ProteinDataCache != null)
                {
                    ProteinDataCache.KeepDB = mKeepDB;
                }
            }
        }

        public bool MatchPeptidePrefixAndSuffixToProtein { get; set; }

        public bool OutputProteinSequence { get; set; }

        public ePeptideFileColumnOrderingCode PeptideFileFormatCode { get; set; }

        public bool PeptideFileSkipFirstLine { get; set; }

        public char PeptideInputFileDelimiter { get; set; }

        public virtual string ProgressStepDescription => mProgressStepDescription;

        /// <summary>
        /// Percent complete
        /// </summary>
        /// <returns></returns>
        /// <remarks>Value between 0 and 100, but can contain decimal percentage values</remarks>
        public float ProgressPercentComplete => Convert.ToSingle(Math.Round(mProgressPercentComplete, 2));

        public string ProteinInputFilePath { get; set; }

        public string ProteinToPeptideMappingFilePath => mProteinToPeptideMappingFilePath;

        public bool RemoveSymbolCharacters { get; set; }

        public string ResultsFilePath => mResultsFilePath;

        public bool SaveProteinToPeptideMappingFile { get; set; }

        public bool SaveSourceDataPlusProteinsFile { get; set; }

        public bool SearchAllProteinsForPeptideSequence { get; set; }

        public bool UseLeaderSequenceHashTable { get; set; }

        public bool SearchAllProteinsSkipCoverageComputationSteps { get; set; }

        public string StatusMessage => mErrorMessage;

        public bool TrackPeptideCounts { get; set; }

        #endregion

        public void AbortProcessingNow()
        {
            if (mLeaderSequenceCache != null)
            {
                mLeaderSequenceCache.AbortProcessingNow();
            }
        }

        private bool BooleanArrayContainsTrueEntries(IList<bool> arrayToCheck, int arrayLength)
        {
            bool containsTrueEntries = false;

            for (int index = 0; index < arrayLength; index++)
            {
                if (arrayToCheck[index])
                {
                    containsTrueEntries = true;
                    break;
                }
            }

            return containsTrueEntries;
        }

        private string CapitalizeMatchingProteinSequenceLetters(
            string proteinSequence,
            string peptideSequence,
            string proteinPeptideKey,
            char prefixResidue,
            char suffixResidue,
            out bool matchFound,
            out bool matchIsNew,
            out int startResidue,
            out int endResidue)
        {

            // Note: this function assumes peptideSequence, prefix, and suffix have all uppercase letters
            // prefix and suffix are only used if mMatchPeptidePrefixAndSuffixToProtein = true

            // Note: This is a count of the number of times the peptide is present in the protein sequence (typically 1); this value is not stored anywhere
            int peptideCount = 0;

            bool currentMatchValid;

            matchFound = false;
            matchIsNew = false;

            startResidue = 0;
            endResidue = 0;

            int charIndex;

            if (SearchAllProteinsSkipCoverageComputationSteps)
            {
                // No need to capitalize proteinSequence since it's already capitalized
                charIndex = proteinSequence.IndexOf(peptideSequence, StringComparison.Ordinal);
            }
            else
            {
                // Need to change proteinSequence to all caps when searching for peptideSequence
                charIndex = proteinSequence.ToUpper().IndexOf(peptideSequence, StringComparison.Ordinal);
            }

            if (charIndex >= 0)
            {
                startResidue = charIndex + 1;
                endResidue = startResidue + peptideSequence.Length - 1;

                matchFound = true;

                if (MatchPeptidePrefixAndSuffixToProtein)
                {
                    currentMatchValid = ValidatePrefixAndSuffix(proteinSequence, prefixResidue, suffixResidue, charIndex, endResidue - 1);
                }
                else
                {
                    currentMatchValid = true;
                }

                if (currentMatchValid)
                {
                    peptideCount += 1;
                }
                else
                {
                    startResidue = 0;
                    endResidue = 0;
                }
            }
            else
            {
                currentMatchValid = false;
            }

            if (matchFound && !SearchAllProteinsSkipCoverageComputationSteps)
            {
                while (charIndex >= 0)
                {
                    if (currentMatchValid)
                    {
                        int nextStartIndex = charIndex + peptideSequence.Length;

                        string newProteinSequence = string.Empty;
                        if (charIndex > 0)
                        {
                            newProteinSequence = proteinSequence.Substring(0, charIndex);
                        }

                        newProteinSequence += proteinSequence.Substring(charIndex, nextStartIndex - charIndex).ToUpper();
                        newProteinSequence += proteinSequence.Substring(nextStartIndex);
                        proteinSequence = string.Copy(newProteinSequence);
                    }

                    // Look for another occurrence of peptideSequence in this protein
                    charIndex = proteinSequence.ToUpper().IndexOf(peptideSequence, charIndex + 1, StringComparison.Ordinal);

                    if (charIndex >= 0)
                    {
                        if (MatchPeptidePrefixAndSuffixToProtein)
                        {
                            currentMatchValid = ValidatePrefixAndSuffix(proteinSequence, prefixResidue, suffixResidue, charIndex, charIndex + peptideSequence.Length - 1);
                        }
                        else
                        {
                            currentMatchValid = true;
                        }

                        if (currentMatchValid)
                        {
                            peptideCount += 1;

                            if (startResidue == 0)
                            {
                                startResidue = charIndex + 1;
                                endResidue = startResidue + peptideSequence.Length - 1;
                            }
                        }
                    }
                }
            }

            if (matchFound)
            {
                if (peptideCount == 0)
                {
                    // The protein contained peptideSequence, but mMatchPeptidePrefixAndSuffixToProtein = true and either prefixResidue or suffixResidue doesn't match
                    matchFound = false;
                }
                else if (TrackPeptideCounts)
                {
                    matchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey);
                }
                else
                {
                    // Must always assume the match is new since not tracking peptide counts
                    matchIsNew = true;
                }
            }

            return proteinSequence;
        }

        /// <summary>
        /// Construct the output file path
        /// The output file is based on outputFileBaseName if defined, otherwise is based on inputFilePath with the suffix removed
        /// In either case, suffixToAppend is appended
        /// The Output directory is based on outputDirectoryPath if defined, otherwise it is the directory where inputFilePath resides
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="suffixToAppend"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="outputFileBaseName"></param>
        /// <returns></returns>
        public static string ConstructOutputFilePath(
            string inputFilePath,
            string suffixToAppend,
            string outputDirectoryPath,
            string outputFileBaseName)
        {
            string outputFileName;

            if (string.IsNullOrEmpty(outputFileBaseName))
            {
                outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + suffixToAppend;
            }
            else
            {
                outputFileName = outputFileBaseName + suffixToAppend;
            }

            string outputFilePath = Path.Combine(GetOutputDirectoryPath(outputDirectoryPath, inputFilePath), outputFileName);

            return outputFilePath;
        }

        private string ConstructPeptideSequenceForKey(string peptideSequence, char prefixResidue, char suffixResidue)
        {
            string peptideSequenceForKey;

            if (Convert.ToInt32(prefixResidue) == 0 && Convert.ToInt32(suffixResidue) == 0)
            {
                peptideSequenceForKey = string.Copy(peptideSequence);
            }
            else
            {
                if (char.IsLetter(prefixResidue))
                {
                    prefixResidue = char.ToUpper(prefixResidue);
                    peptideSequenceForKey = prefixResidue + "." + peptideSequence;
                }
                else
                {
                    peptideSequenceForKey = "-." + peptideSequence;
                }

                if (char.IsLetter(suffixResidue))
                {
                    suffixResidue = char.ToUpper(suffixResidue);
                    peptideSequenceForKey += "." + suffixResidue;
                }
                else
                {
                    peptideSequenceForKey += ".-";
                }
            }

            return peptideSequenceForKey;
        }

        private void CreateProteinCoverageFile(string peptideInputFilePath, string outputDirectoryPath, string outputFileBaseName)
        {
            const int INITIAL_PROTEIN_COUNT_RESERVE = 5000;

            // The data in mProteinPeptideStats is copied into array peptideStats for fast lookup
            // This is necessary since use of the enumerator returned by mProteinPeptideStats.GetEnumerator
            // for every protein in ProteinDataCache.mProteins leads to very slow program performance
            int peptideStatsCount = 0;
            udtPeptideCountStatsType[] udtPeptideStats = null;

            if (string.IsNullOrEmpty(mResultsFilePath))
            {
                if (peptideInputFilePath.Length > 0)
                {
                    mResultsFilePath = ConstructOutputFilePath(peptideInputFilePath, "_coverage.txt", outputDirectoryPath, outputFileBaseName);
                }
                else
                {
                    mResultsFilePath = Path.Combine(GetOutputDirectoryPath(outputDirectoryPath, string.Empty), "Peptide_coverage.txt");
                }
            }

            UpdateProgress("Creating the protein coverage file: " + Path.GetFileName(mResultsFilePath), 0,
                eProteinCoverageProcessingSteps.WriteProteinCoverageFile);

            using (var writer = new StreamWriter(new FileStream(mResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                // Note: If the column ordering is changed, be sure to update OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER and OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER
                string dataLine = "Protein Name" + "\t" +
                                  "Percent Coverage" + "\t" +
                                  "Protein Description" + "\t" +
                                  "Non Unique Peptide Count" + "\t" +
                                  "Unique Peptide Count" + "\t" +
                                  "Protein Residue Count";

                if (OutputProteinSequence)
                {
                    dataLine += "\t" + "Protein Sequence";
                }

                writer.WriteLine(dataLine);

                // Contains pointers to entries in udtPeptideStats()
                var proteinIDLookup = new Dictionary<int, int>();

                // Populate udtPeptideStats() using dictionary mProteinPeptideStats
                if (TrackPeptideCounts)
                {

                    // Initially reserve space for INITIAL_PROTEIN_COUNT_RESERVE proteins
                    udtPeptideStats = new udtPeptideCountStatsType[INITIAL_PROTEIN_COUNT_RESERVE];

                    var myEnumerator = mProteinPeptideStats.GetEnumerator();
                    while (myEnumerator.MoveNext())
                    {
                        string proteinPeptideKey = myEnumerator.Current.Key;

                        // proteinPeptideKey will be of the form 1234::K.ABCDEFR.A
                        // Look for the first colon
                        int colonIndex = proteinPeptideKey.IndexOf(':');

                        if (colonIndex > 0)
                        {
                            int proteinID = Convert.ToInt32(proteinPeptideKey.Substring(0, colonIndex));
                            int targetIndex;

                            if (!proteinIDLookup.TryGetValue(proteinID, out targetIndex))
                            {
                                // ID not found; so add it

                                targetIndex = peptideStatsCount;
                                peptideStatsCount += 1;

                                proteinIDLookup.Add(proteinID, targetIndex);

                                if (targetIndex >= udtPeptideStats.Length)
                                {
                                    // Reserve more space in the arrays
                                    var oldUdtPeptideStats = udtPeptideStats;
                                    udtPeptideStats = new udtPeptideCountStatsType[(udtPeptideStats.Length * 2)];
                                    Array.Copy(oldUdtPeptideStats, udtPeptideStats, Math.Min(udtPeptideStats.Length * 2, oldUdtPeptideStats.Length));
                                }
                            }

                            // Update the protein counts at targetIndex
                            // NOTE: The following is valid only because udtPeptideStats is an array, and not a generic collection
                            udtPeptideStats[targetIndex].UniquePeptideCount += 1;
                            udtPeptideStats[targetIndex].NonUniquePeptideCount += myEnumerator.Current.Value;
                        }
                    }

                    // Shrink udtPeptideStats
                    if (peptideStatsCount < udtPeptideStats.Length)
                    {
                        var oldUdtPeptideStats = udtPeptideStats;
                        udtPeptideStats = new udtPeptideCountStatsType[peptideStatsCount];
                        Array.Copy(oldUdtPeptideStats, udtPeptideStats, Math.Min(peptideStatsCount, oldUdtPeptideStats.Length));
                    }
                }
                else
                {
                    udtPeptideStats = new udtPeptideCountStatsType[0];
                }

                // Query the SQLite DB to extract the protein information
                int proteinsProcessed = 0;
                foreach (var udtProtein in ProteinDataCache.GetCachedProteins())
                {
                    int uniquePeptideCount = 0;
                    int nonUniquePeptideCount = 0;

                    if (TrackPeptideCounts)
                    {
                        int targetIndex;
                        if (proteinIDLookup.TryGetValue(udtProtein.UniqueSequenceID, out targetIndex))
                        {
                            uniquePeptideCount = udtPeptideStats[targetIndex].UniquePeptideCount;
                            nonUniquePeptideCount = udtPeptideStats[targetIndex].NonUniquePeptideCount;
                        }
                    }

                    dataLine = udtProtein.Name + "\t" +
                               Math.Round(udtProtein.PercentCoverage * 100, 3) + "\t" +
                               udtProtein.Description + "\t" +
                               nonUniquePeptideCount + "\t" +
                               uniquePeptideCount + "\t" +
                               udtProtein.Sequence.Length;

                    if (OutputProteinSequence)
                    {
                        dataLine += "\t" + udtProtein.Sequence;
                    }

                    writer.WriteLine(dataLine);

                    if (proteinsProcessed % 25 == 0)
                    {
                        UpdateProgress(proteinsProcessed / Convert.ToSingle(ProteinDataCache.GetProteinCountCached()) * 100,
                            eProteinCoverageProcessingSteps.WriteProteinCoverageFile);
                    }

                    if (mAbortProcessing)
                        break;
                    proteinsProcessed += 1;
                }
            }
        }

        private int DetermineLineTerminatorSize(string inputFilePath)
        {
            int terminatorSize = 2;

            try
            {
                // Open the input file and look for the first carriage return or line feed
                using (var fsInFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    while (fsInFile.Position < fsInFile.Length && fsInFile.Position < 100000)
                    {
                        int intByte = fsInFile.ReadByte();

                        if (intByte == 10)
                        {
                            // Found linefeed
                            if (fsInFile.Position < fsInFile.Length)
                            {
                                intByte = fsInFile.ReadByte();
                                if (intByte == 13)
                                {
                                    // LfCr
                                    terminatorSize = 2;
                                }
                                else
                                {
                                    // Lf only
                                    terminatorSize = 1;
                                }
                            }
                            else
                            {
                                terminatorSize = 1;
                            }

                            break;
                        }
                        else if (intByte == 13)
                        {
                            // Found carriage return
                            if (fsInFile.Position < fsInFile.Length)
                            {
                                intByte = fsInFile.ReadByte();
                                if (intByte == 10)
                                {
                                    // CrLf
                                    terminatorSize = 2;
                                }
                                else
                                {
                                    // Cr only
                                    terminatorSize = 1;
                                }
                            }
                            else
                            {
                                terminatorSize = 1;
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in DetermineLineTerminatorSize: " + ex.Message, ex);
            }

            return terminatorSize;
        }

        /// <summary>
        /// Searches for proteins that contain the peptides in peptideList
        /// If proteinNameForPeptide is blank or mSearchAllProteinsForPeptideSequence=True, searches all proteins
        /// Otherwise, only searches the protein specified by proteinNameForPeptide
        /// </summary>
        /// <param name="peptideList">Dictionary containing the peptides to search; peptides must be in the format Prefix.Peptide.Suffix where Prefix and Suffix are single characters; peptides are assumed to only contain letters (no symbols)</param>
        /// <param name="proteinNameForPeptides">The protein to search; only used if mSearchAllProteinsForPeptideSequence=False</param>
        /// <remarks></remarks>
        private void FindSequenceMatchForPeptideList(IDictionary<string, int> peptideList,
            string proteinNameForPeptides)
        {
            var proteinUpdated = new bool[PROTEIN_CHUNK_COUNT];

            try
            {
                // Make sure proteinNameForPeptide is a valid string
                if (proteinNameForPeptides == null)
                {
                    proteinNameForPeptides = string.Empty;
                }

                int expectedPeptideIterations = Convert.ToInt32(Math.Ceiling(ProteinDataCache.GetProteinCountCached() / (double)PROTEIN_CHUNK_COUNT)) * peptideList.Count;
                if (expectedPeptideIterations < 1)
                    expectedPeptideIterations = 1;

                UpdateProgress("Finding matching proteins for peptide list", 0,
                    eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides);

                int startIndex = 0;
                do
                {
                    // Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
                    // Store the information in the four local arrays
                    int proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, false);

                    int peptideIterationsComplete = 0;

                    // Iterate through the peptides in peptideList
                    var myEnumerator = peptideList.GetEnumerator();

                    while (myEnumerator.MoveNext())
                    {
                        char prefixResidue;
                        char suffixResidue;

                        // Retrieve the next peptide from peptideList
                        // Use GetCleanPeptideSequence() to extract out the sequence, prefix, and suffix letters (we're setting removeSymbolCharacters to False since that should have been done before the peptides were stored in peptideList)
                        // Make sure the peptide sequence has uppercase letters
                        string peptideSequenceClean = GetCleanPeptideSequence(myEnumerator.Current.Key, out prefixResidue, out suffixResidue, false).ToUpper();

                        string peptideSequenceForKeySource;
                        string peptideSequenceForKey;
                        string peptideSequenceToSearchOn;

                        if (MatchPeptidePrefixAndSuffixToProtein)
                        {
                            peptideSequenceForKeySource = ConstructPeptideSequenceForKey(peptideSequenceClean, prefixResidue, suffixResidue);
                        }
                        else
                        {
                            peptideSequenceForKeySource = string.Copy(peptideSequenceClean);
                        }

                        if (IgnoreILDifferences)
                        {
                            // Replace all L characters with I
                            peptideSequenceForKey = peptideSequenceForKeySource.Replace('L', 'I');

                            peptideSequenceToSearchOn = peptideSequenceClean.Replace('L', 'I');
                            if (prefixResidue == 'L')
                                prefixResidue = 'I';
                            if (suffixResidue == 'L')
                                suffixResidue = 'I';
                        }
                        else
                        {
                            peptideSequenceToSearchOn = string.Copy(peptideSequenceClean);

                            // I'm purposely not using String.Copy() here in order to obtain increased speed
                            peptideSequenceForKey = peptideSequenceForKeySource;
                        }

                        // Search for peptideSequence in the protein sequences
                        for (int proteinIndex = 0; proteinIndex < proteinCount; proteinIndex++)
                        {
                            bool matchFound = false;
                            var matchIsNew = default(bool);
                            var startResidue = default(int);
                            var endResidue = default(int);

                            if (SearchAllProteinsForPeptideSequence || proteinNameForPeptides.Length == 0)
                            {
                                // Search through all Protein sequences and capitalize matches for Peptide Sequence

                                string proteinPeptideKey = Convert.ToString(mCachedProteinInfo[proteinIndex].UniqueSequenceID) + "::" + peptideSequenceForKey;
                                // NOTE: The following is valid only because mCachedProteinInfo is an array, and not a generic collection
                                mCachedProteinInfo[proteinIndex].Sequence = CapitalizeMatchingProteinSequenceLetters(
                                    mCachedProteinInfo[proteinIndex].Sequence, peptideSequenceToSearchOn,
                                    proteinPeptideKey, prefixResidue, suffixResidue,
                                    out matchFound, out matchIsNew,
                                    out startResidue, out endResidue);
                            }
                            // Only search protein proteinNameForPeptide
                            else if ((mCachedProteinInfo[proteinIndex].Name ?? "") == (proteinNameForPeptides ?? ""))
                            {
                                // Define the peptide match key using the Unique Sequence ID, two colons, and the peptide sequence
                                string proteinPeptideKey = Convert.ToString(mCachedProteinInfo[proteinIndex].UniqueSequenceID) + "::" + peptideSequenceForKey;

                                // Capitalize matching residues in sequence
                                // NOTE: The following is valid only because mCachedProteinInfo is an array, and not a generic collection
                                mCachedProteinInfo[proteinIndex].Sequence = CapitalizeMatchingProteinSequenceLetters(
                                    mCachedProteinInfo[proteinIndex].Sequence, peptideSequenceToSearchOn,
                                    proteinPeptideKey, prefixResidue, suffixResidue,
                                    out matchFound, out matchIsNew,
                                    out startResidue, out endResidue);
                            }

                            if (matchFound)
                            {
                                if (!SearchAllProteinsSkipCoverageComputationSteps)
                                {
                                    proteinUpdated[proteinIndex] = true;
                                }

                                if (matchIsNew)
                                {
                                    if (SaveProteinToPeptideMappingFile)
                                    {
                                        WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo[proteinIndex].Name, peptideSequenceForKeySource, startResidue, endResidue);
                                    }

                                    if (SaveSourceDataPlusProteinsFile)
                                    {
                                        StorePeptideToProteinMatch(peptideSequenceClean, mCachedProteinInfo[proteinIndex].Name);
                                    }
                                }
                            }
                        }

                        peptideIterationsComplete += 1;
                        if (peptideIterationsComplete % 10 == 0)
                        {
                            UpdateProgress(Convert.ToSingle(peptideIterationsComplete / (double)expectedPeptideIterations * 100),
                                eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides);
                        }
                    }

                    // Store the updated protein sequence information in the database
                    UpdateSequenceDbDataValues(proteinUpdated, proteinCount);

                    // Increment startIndex to obtain the next chunk of proteins
                    startIndex += PROTEIN_CHUNK_COUNT;
                }
                while (startIndex < ProteinDataCache.GetProteinCountCached());
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in FindSequenceMatch:" + Environment.NewLine + ex.Message, ex);
            }
        }

        private void UpdateSequenceDbDataValues(IList<bool> proteinUpdated, int proteinCount)
        {
            try
            {
                if (!BooleanArrayContainsTrueEntries(proteinUpdated, proteinCount))
                {
                    // All of the entries in proteinUpdated() are False; nothing to update
                    return;
                }

                // Store the updated protein sequences in the SQLite database
                var sqlConnection = ProteinDataCache.ConnectToSQLiteDB(true);
                using (var dbTrans = sqlConnection.BeginTransaction())
                using (var cmd = sqlConnection.CreateCommand())
                {
                    // Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set Sequence = ? Where UniqueSequenceID = ?";

                    var SequenceFld = cmd.CreateParameter();
                    var UniqueSequenceIDFld = cmd.CreateParameter();
                    cmd.Parameters.Add(SequenceFld);
                    cmd.Parameters.Add(UniqueSequenceIDFld);

                    // Update each protein that has proteinUpdated(proteinIndex) = True
                    for (int proteinIndex = 0; proteinIndex < proteinCount; proteinIndex++)
                    {
                        if (proteinUpdated[proteinIndex])
                        {
                            UniqueSequenceIDFld.Value = mCachedProteinInfo[proteinIndex].UniqueSequenceID;
                            SequenceFld.Value = mCachedProteinInfo[proteinIndex].Sequence;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    dbTrans.Commit();
                }

                // Close the Sql Reader
                sqlConnection.Close();
                sqlConnection.Dispose();
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in UpdateSequenceDbDataValues: " + ex.Message, ex);
            }
        }

        public static string GetAppDirectoryPath()
        {
            // Could use Application.StartupPath, but .GetExecutingAssembly is better
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static readonly Regex reReplaceSymbols = new Regex("[^A-Za-z]", RegexOptions.Compiled);

        public static string GetCleanPeptideSequence(string peptideSequence,
            out char prefixResidue,
            out char suffixResidue,
            bool removeSymbolCharacters)
        {
            prefixResidue = default(char);
            suffixResidue = default;

            if (peptideSequence.Length >= 4)
            {
                if (peptideSequence[1] == '.' && peptideSequence[peptideSequence.Length - 2] == '.')
                {
                    prefixResidue = peptideSequence[0];
                    suffixResidue = peptideSequence[peptideSequence.Length - 1];
                    peptideSequence = peptideSequence.Substring(2, peptideSequence.Length - 4);
                }
            }

            if (removeSymbolCharacters)
            {
                peptideSequence = reReplaceSymbols.Replace(peptideSequence, string.Empty);
            }

            return peptideSequence;
        }

        public string GetErrorMessage()
        {
            // Returns String.Empty if no error

            string message;

            switch (ErrorCode)
            {
                case eProteinCoverageErrorCodes.NoError:
                    message = string.Empty;
                    break;
                case eProteinCoverageErrorCodes.InvalidInputFilePath:
                    message = "Invalid input file path";
                    break;
                // case eProteinCoverageErrorCodes.InvalidOutputDirectoryPath:
                //     message = "Invalid output directory path";
                //     break;
                // case eProteinCoverageErrorCodes.ParameterFileNotFound:
                //     message = "Parameter file not found";
                //     break;

                // case eProteinCoverageErrorCodes.ErrorReadingInputFile:
                //     message = "Error reading input file";
                //     break;
                // case eProteinCoverageErrorCodes.ErrorCreatingOutputFiles:
                //     message = "Error creating output files";
                //     break;

                case eProteinCoverageErrorCodes.ErrorReadingParameterFile:
                    message = "Invalid parameter file";
                    break;
                case eProteinCoverageErrorCodes.FilePathError:
                    message = "General file path error";
                    break;
                case eProteinCoverageErrorCodes.UnspecifiedError:
                    message = "Unspecified error";
                    break;
                default:
                    // This shouldn't happen
                    message = "Unknown error state";
                    break;
            }

            if (mErrorMessage.Length > 0)
            {
                if (message.Length > 0)
                {
                    message += "; ";
                }

                message += mErrorMessage;
            }

            return message;
        }

        [Obsolete("Use GetOutputDirectoryPath")]
        public static string GetOutputFolderPath(string outputFolderPath, string outputFilePath)
        {
            return GetOutputDirectoryPath(outputFolderPath, outputFilePath);
        }

        /// <summary>
        /// Determine the output directory path
        /// Uses outputDirectoryPath if defined
        /// Otherwise uses the directory where outputFilePath resides
        /// </summary>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="outputFilePath"></param>
        /// <returns></returns>
        /// <remarks>If an error, or unable to determine a directory, returns the directory with the application files</remarks>
        public static string GetOutputDirectoryPath(string outputDirectoryPath, string outputFilePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
                {
                    outputDirectoryPath = Path.GetFullPath(outputDirectoryPath);
                }
                else
                {
                    outputDirectoryPath = Path.GetDirectoryName(outputFilePath);
                }

                if (!Directory.Exists(outputDirectoryPath))
                {
                    Directory.CreateDirectory(outputDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                outputDirectoryPath = GetAppDirectoryPath();
            }

            return outputDirectoryPath;
        }

        private void GetPercentCoverage()
        {
            var proteinUpdated = new bool[PROTEIN_CHUNK_COUNT];

            UpdateProgress("Computing percent coverage", 0,
                eProteinCoverageProcessingSteps.ComputePercentCoverage);

            int startIndex = 0;
            int index = 0;
            do
            {
                // Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
                // Store the information in the four local arrays
                int proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, false);

                for (int proteinIndex = 0; proteinIndex < proteinCount; proteinIndex++)
                {
                    if (mCachedProteinInfo[proteinIndex].Sequence != null)
                    {
                        var charArray = mCachedProteinInfo[proteinIndex].Sequence.ToCharArray();
                        int capitalLetterCount = 0;
                        foreach (var character in charArray)
                        {
                            if (char.IsUpper(character))
                                capitalLetterCount += 1;
                        }

                        // NOTE: The following is valid only because mCachedProteinInfo is an array, and not a generic collection
                        mCachedProteinInfo[proteinIndex].PercentCoverage = capitalLetterCount / (double)mCachedProteinInfo[proteinIndex].Sequence.Length;
                        if (mCachedProteinInfo[proteinIndex].PercentCoverage > 0)
                        {
                            proteinUpdated[proteinIndex] = true;
                        }
                    }

                    if (index % 100 == 0)
                    {
                        UpdateProgress(index / Convert.ToSingle(ProteinDataCache.GetProteinCountCached()) * 100,
                            eProteinCoverageProcessingSteps.ComputePercentCoverage);
                    }

                    index += 1;
                }

                UpdatePercentCoveragesDbDataValues(proteinUpdated, proteinCount);

                // Increment startIndex to obtain the next chunk of proteins
                startIndex += PROTEIN_CHUNK_COUNT;
            }
            while (startIndex < ProteinDataCache.GetProteinCountCached());
        }

        private void UpdatePercentCoveragesDbDataValues(IList<bool> proteinUpdated, int proteinCount)
        {
            try
            {
                if (!BooleanArrayContainsTrueEntries(proteinUpdated, proteinCount))
                {
                    // All of the entries in proteinUpdated() are False; nothing to update
                    return;
                }

                // Store the updated protein coverage values in the SQLite database
                var sqlConnection = ProteinDataCache.ConnectToSQLiteDB(true);

                using (var dbTrans = sqlConnection.BeginTransaction())
                using (var cmd = sqlConnection.CreateCommand())
                {
                    // Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set PercentCoverage = ? Where UniqueSequenceID = ?";

                    var PercentCoverageFld = cmd.CreateParameter();
                    var UniqueSequenceIDFld = cmd.CreateParameter();
                    cmd.Parameters.Add(PercentCoverageFld);
                    cmd.Parameters.Add(UniqueSequenceIDFld);

                    // Update each protein that has proteinUpdated(proteinIndex) = True
                    for (int proteinIndex = 0; proteinIndex < proteinCount; proteinIndex++)
                    {
                        if (proteinUpdated[proteinIndex])
                        {
                            UniqueSequenceIDFld.Value = mCachedProteinInfo[proteinIndex].UniqueSequenceID;
                            PercentCoverageFld.Value = mCachedProteinInfo[proteinIndex].PercentCoverage;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    dbTrans.Commit();
                }

                // Close the Sql Reader
                sqlConnection.Close();
                sqlConnection.Dispose();
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in UpdatePercentCoveragesDbDataValues: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Increment the observation count for the given key in the given dictionary
        /// If the key is not defined, add it
        /// </summary>
        /// <param name="dictionaryToUpdate"></param>
        /// <param name="keyName"></param>
        /// <returns>True if the protein is new and was added tomProteinPeptideStats </returns>
        private bool IncrementCountByKey(IDictionary<string, int> dictionaryToUpdate, string keyName)
        {
            int value;

            if (dictionaryToUpdate.TryGetValue(keyName, out value))
            {
                dictionaryToUpdate[keyName] = value + 1;
                return false;
            }
            else
            {
                dictionaryToUpdate.Add(keyName, 1);
                return true;
            }
        }

        private void InitializeVariables()
        {
            mAbortProcessing = false;
            mErrorMessage = string.Empty;

            ProteinInputFilePath = string.Empty;
            mResultsFilePath = string.Empty;

            ProteinDataCache = new clsProteinFileDataCache() { KeepDB = KeepDB };

            RegisterEvents(ProteinDataCache);
            ProteinDataCache.ProteinCachedWithProgress += ProteinDataCache_ProteinCachedWithProgress;
            ProteinDataCache.ProteinCachingComplete += ProteinDataCache_ProteinCachingComplete;

            mCachedProteinInfoStartIndex = -1;

            PeptideFileSkipFirstLine = false;
            PeptideInputFileDelimiter = '\t';
            PeptideFileFormatCode = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence;

            OutputProteinSequence = true;
            SearchAllProteinsForPeptideSequence = true;
            SearchAllProteinsSkipCoverageComputationSteps = false;
            UseLeaderSequenceHashTable = true;

            SaveProteinToPeptideMappingFile = false;
            mProteinToPeptideMappingFilePath = string.Empty;

            SaveSourceDataPlusProteinsFile = false;

            TrackPeptideCounts = true;
            RemoveSymbolCharacters = true;
            MatchPeptidePrefixAndSuffixToProtein = false;
            IgnoreILDifferences = false;

            // Define the percent complete values to use for the start of each processing step

            mPercentCompleteStartLevels = new float[PERCENT_COMPLETE_LEVEL_COUNT];

            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.Starting] = 0;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.CacheProteins] = 1;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.DetermineShortestPeptideLength] = 45;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.CachePeptides] = 50;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences] = 55;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides] = 90;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.ComputePercentCoverage] = 95;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.WriteProteinCoverageFile] = 97;
            mPercentCompleteStartLevels[(int)eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile] = 98;
            mPercentCompleteStartLevels[PERCENT_COMPLETE_LEVEL_COUNT] = 100;
        }

        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parameterFilePath))
                {
                    // No parameter file specified; default settings will be used
                    return true;
                }

                if (!File.Exists(parameterFilePath))
                {
                    // See if parameterFilePath points to a file in the same directory as the application
                    string alternateFilePath = Path.Combine(GetAppDirectoryPath(), Path.GetFileName(parameterFilePath));
                    if (!File.Exists(alternateFilePath))
                    {
                        // Parameter file still not found
                        SetErrorMessage("Parameter file not found: " + parameterFilePath);
                        return false;
                    }
                    else
                    {
                        parameterFilePath = string.Copy(alternateFilePath);
                    }
                }

                var settingsFileReader = new XmlSettingsFileAccessor();

                if (settingsFileReader.LoadSettings(parameterFilePath))
                {
                    if (!settingsFileReader.SectionPresent(XML_SECTION_PROCESSING_OPTIONS))
                    {
                        OnWarningEvent("The node '<section name=\"" + XML_SECTION_PROCESSING_OPTIONS + "\"> was not found in the parameter file: " + parameterFilePath);
                    }
                    else
                    {
                        OutputProteinSequence = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", OutputProteinSequence);
                        SearchAllProteinsForPeptideSequence = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", SearchAllProteinsForPeptideSequence);
                        SaveProteinToPeptideMappingFile = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", SaveProteinToPeptideMappingFile);
                        SaveSourceDataPlusProteinsFile = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveSourceDataPlusProteinsFile", SaveSourceDataPlusProteinsFile);
                        TrackPeptideCounts = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", TrackPeptideCounts);
                        RemoveSymbolCharacters = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", RemoveSymbolCharacters);
                        MatchPeptidePrefixAndSuffixToProtein = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", MatchPeptidePrefixAndSuffixToProtein);
                        IgnoreILDifferences = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", IgnoreILDifferences);
                        PeptideFileSkipFirstLine = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", PeptideFileSkipFirstLine);
                        PeptideInputFileDelimiter = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", Convert.ToString(PeptideInputFileDelimiter))[0];
                        PeptideFileFormatCode = (ePeptideFileColumnOrderingCode)Convert.ToInt32(settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", (int)PeptideFileFormatCode));
                        ProteinDataCache.DelimitedFileSkipFirstLine = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", ProteinDataCache.DelimitedFileSkipFirstLine);
                        ProteinDataCache.DelimitedFileDelimiter = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", Convert.ToString(ProteinDataCache.DelimitedFileDelimiter))[0];
                        ProteinDataCache.DelimitedFileFormatCode = (DelimitedFileReader.eDelimitedFileFormatCode)Convert.ToInt32(settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", (int)ProteinDataCache.DelimitedFileFormatCode));
                    }
                }
                else
                {
                    SetErrorMessage("Error calling settingsFileReader.LoadSettings for " + parameterFilePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in LoadParameterFileSettings:" + ex.Message, ex);
                SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile);
                return false;
            }

            return true;
        }

        private bool ParsePeptideInputFile(
            string peptideInputFilePath,
            string outputDirectoryPath,
            string outputFileBaseName,
            out string proteinToPepMapFilePath)
        {
            const int MAX_SHORT_PEPTIDES_TO_CACHE = 1000000;

            proteinToPepMapFilePath = string.Empty;
            try
            {
                // Initialize delimiter array
                var sepChars = new[] { PeptideInputFileDelimiter };

                // Initialize some dictionaries

                var shortPeptideCache = new Dictionary<string, int>();

                if (mProteinPeptideStats == null)
                {
                    mProteinPeptideStats = new Dictionary<string, int>();
                }
                else
                {
                    mProteinPeptideStats.Clear();
                }

                if (mPeptideToProteinMapResults == null)
                {
                    mPeptideToProteinMapResults = new Dictionary<string, List<string>>();
                }
                else
                {
                    mPeptideToProteinMapResults.Clear();
                }

                if (!File.Exists(peptideInputFilePath))
                {
                    SetErrorMessage("File not found: " + peptideInputFilePath);
                    return false;
                }

                string progressMessageBase = "Reading peptides from " + Path.GetFileName(peptideInputFilePath);
                if (UseLeaderSequenceHashTable)
                {
                    progressMessageBase += " and finding leader sequences";
                }
                else if (!SearchAllProteinsSkipCoverageComputationSteps)
                {
                    progressMessageBase += " and computing coverage";
                }

                mProgressStepDescription = string.Copy(progressMessageBase);
                Console.WriteLine();
                OnStatusEvent("Parsing " + Path.GetFileName(peptideInputFilePath));

                UpdateProgress(mProgressStepDescription, 0,
                    eProteinCoverageProcessingSteps.DetermineShortestPeptideLength);

                // Open the file and read, at most, the first 100,000 characters to see if it contains CrLf or just Lf
                int terminatorSize = DetermineLineTerminatorSize(peptideInputFilePath);

                // Possibly open the file and read the first few line to make sure the number of columns is appropriate
                bool success = ValidateColumnCountInInputFile(peptideInputFilePath);
                if (!success)
                {
                    return false;
                }

                if (UseLeaderSequenceHashTable)
                {
                    // Determine the shortest peptide present in the input file
                    // This is a fast process that involves checking the length of each sequence in the input file

                    UpdateProgress("Determining the shortest peptide in the input file", 0,
                        eProteinCoverageProcessingSteps.DetermineShortestPeptideLength);
                    if (mLeaderSequenceCache == null)
                    {
                        mLeaderSequenceCache = new clsLeaderSequenceCache();
                        mLeaderSequenceCache.ProgressChanged += LeaderSequenceCache_ProgressChanged;
                        mLeaderSequenceCache.ProgressComplete += LeaderSequenceCache_ProgressComplete;
                    }
                    else
                    {
                        mLeaderSequenceCache.InitializeVariables();
                    }

                    mLeaderSequenceCache.IgnoreILDifferences = IgnoreILDifferences;

                    int columnNumWithPeptideSequence;
                    switch (PeptideFileFormatCode)
                    {
                        case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence:
                            columnNumWithPeptideSequence = 2;
                            break;
                        default:
                            // Includes ePeptideFileColumnOrderingCode.SequenceOnly
                            columnNumWithPeptideSequence = 1;
                            break;
                    }

                    mLeaderSequenceCache.DetermineShortestPeptideLengthInFile(peptideInputFilePath, terminatorSize, PeptideFileSkipFirstLine, PeptideInputFileDelimiter, columnNumWithPeptideSequence);

                    if (mAbortProcessing)
                    {
                        return false;
                    }
                    else
                    {
                        progressMessageBase += " (leader seq length = " + mLeaderSequenceCache.LeaderSequenceMinimumLength.ToString() + ")";

                        UpdateProgress(progressMessageBase);
                    }
                }

                int invalidLineCount = 0;

                // Open the peptide file and read in the lines
                using (var reader = new StreamReader(new FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    // Create the protein to peptide match details file
                    mProteinToPeptideMappingFilePath = ConstructOutputFilePath(peptideInputFilePath, FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                                                                               outputDirectoryPath, outputFileBaseName);

                    if (SaveProteinToPeptideMappingFile)
                    {
                        proteinToPepMapFilePath = string.Copy(mProteinToPeptideMappingFilePath);

                        UpdateProgress("Creating the protein to peptide mapping file: " + Path.GetFileName(mProteinToPeptideMappingFilePath));

                        mProteinToPeptideMappingOutputFile = new StreamWriter(new FileStream(mProteinToPeptideMappingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

                        mProteinToPeptideMappingOutputFile.WriteLine("Protein Name" + "\t" + "Peptide Sequence" + "\t" + "Residue Start" + "\t" + "Residue End");
                    }

                    int currentLine = 1;
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

                        if (currentLine % 500 == 0)
                        {
                            UpdateProgress("Reading peptide input file", Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100),
                                eProteinCoverageProcessingSteps.CachePeptides);
                        }

                        if (currentLine == 1 && PeptideFileSkipFirstLine)
                        {
                            // do nothing, skip the first line
                        }
                        else if (dataLine.Length > 0)
                        {
                            bool validLine = false;
                            string proteinName = "";
                            string peptideSequence = "";

                            try
                            {
                                // Split the line, but for efficiency purposes, only parse out the first 3 columns
                                var dataCols = dataLine.Split(sepChars, 3);

                                switch (PeptideFileFormatCode)
                                {
                                    case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence:
                                        proteinName = dataCols[0];

                                        if (dataCols.Length > 1 && !string.IsNullOrWhiteSpace(dataCols[1]))
                                        {
                                            peptideSequence = dataCols[1];
                                            validLine = true;
                                        }

                                        break;
                                    default:
                                        // Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                        peptideSequence = dataCols[0];
                                        proteinName = string.Empty;
                                        validLine = true;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                validLine = false;
                            }

                            if (validLine)
                            {
                                // Check for and remove prefix and suffix letters
                                // Also possibly remove symbol characters

                                char prefixResidue;
                                char suffixResidue;
                                peptideSequence = GetCleanPeptideSequence(peptideSequence, out prefixResidue, out suffixResidue, RemoveSymbolCharacters);

                                if (UseLeaderSequenceHashTable &&
                                    peptideSequence.Length >= mLeaderSequenceCache.LeaderSequenceMinimumLength)
                                {
                                    if (mLeaderSequenceCache.CachedPeptideCount >= clsLeaderSequenceCache.MAX_LEADER_SEQUENCE_COUNT)
                                    {
                                        // Need to step through the proteins and match them to the data in mLeaderSequenceCache
                                        SearchProteinsUsingLeaderSequences();
                                        mLeaderSequenceCache.InitializeCachedPeptides();
                                    }

                                    mLeaderSequenceCache.CachePeptide(peptideSequence, proteinName, prefixResidue, suffixResidue);
                                }
                                else
                                {
                                    // Either mUseLeaderSequenceHashTable is false, or the peptide sequence is less than MINIMUM_LEADER_SEQUENCE_LENGTH residues long
                                    // We must search all proteins for the given peptide

                                    // Cache the short peptides in shortPeptideCache
                                    if (shortPeptideCache.Count >= MAX_SHORT_PEPTIDES_TO_CACHE)
                                    {
                                        // Step through the proteins and match them to the data in shortPeptideCache
                                        SearchProteinsUsingCachedPeptides(shortPeptideCache);
                                        shortPeptideCache.Clear();
                                    }

                                    string peptideSequenceToCache = prefixResidue + "." + peptideSequence + "." + suffixResidue;

                                    IncrementCountByKey(shortPeptideCache, peptideSequenceToCache);
                                }
                            }
                            else
                            {
                                invalidLineCount += 1;
                            }
                        }

                        currentLine += 1;
                    }
                }

                if (UseLeaderSequenceHashTable)
                {
                    // Step through the proteins and match them to the data in mLeaderSequenceCache
                    if (mLeaderSequenceCache.CachedPeptideCount > 0)
                    {
                        SearchProteinsUsingLeaderSequences();
                    }
                }

                // Step through the proteins and match them to the data in shortPeptideCache
                SearchProteinsUsingCachedPeptides(shortPeptideCache);

                if (!mAbortProcessing & !SearchAllProteinsSkipCoverageComputationSteps)
                {
                    // Compute the residue coverage percent for each protein
                    GetPercentCoverage();
                }

                if (mProteinToPeptideMappingOutputFile != null)
                {
                    mProteinToPeptideMappingOutputFile.Close();
                    mProteinToPeptideMappingOutputFile = null;
                }

                if (SaveSourceDataPlusProteinsFile)
                {
                    // Create a new version of the input file, but with all of the proteins listed
                    SaveDataPlusAllProteinsFile(peptideInputFilePath, outputDirectoryPath, outputFileBaseName, sepChars, terminatorSize);
                }

                if (invalidLineCount > 0)
                {
                    switch (PeptideFileFormatCode)
                    {
                        case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence:
                            OnWarningEvent("Found " + invalidLineCount + " lines that did not have two columns (Protein Name and Peptide Sequence).  Those line(s) have been skipped.");
                            break;
                        default:
                            OnWarningEvent("Found " + invalidLineCount + " lines that did not contain a peptide sequence.  Those line(s) have been skipped.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in ParsePeptideInputFile: " + ex.Message, ex);
            }

            return !mAbortProcessing;
        }

        private bool ParseProteinInputFile()
        {
            bool success = false;
            try
            {
                mProgressStepDescription = "Reading protein input file";

                // Protein file options
                if (clsProteinFileDataCache.IsFastaFile(ProteinInputFilePath))
                {
                    // .fasta or .fsa file
                    ProteinDataCache.AssumeFastaFile = true;
                }
                else if ((Path.GetExtension(ProteinInputFilePath).ToLower() ?? "") == ".txt")
                {
                    ProteinDataCache.AssumeDelimitedFile = true;
                }
                else
                {
                    ProteinDataCache.AssumeFastaFile = false;
                }

                if (SearchAllProteinsSkipCoverageComputationSteps)
                {
                    // Make sure all of the protein sequences are uppercase
                    ProteinDataCache.ChangeProteinSequencesToLowercase = false;
                    ProteinDataCache.ChangeProteinSequencesToUppercase = true;
                }
                else
                {
                    // Make sure all of the protein sequences are lowercase
                    ProteinDataCache.ChangeProteinSequencesToLowercase = true;
                    ProteinDataCache.ChangeProteinSequencesToUppercase = false;
                }

                success = ProteinDataCache.ParseProteinFile(ProteinInputFilePath);
                if (!success)
                {
                    SetErrorMessage("Error parsing protein file: " + ProteinDataCache.StatusMessage);
                }
                else if (ProteinDataCache.GetProteinCountCached() == 0)
                {
                    success = false;
                    SetErrorMessage("Error parsing protein file: no protein entries were found in the file.  Please verify that the column order defined for the proteins file is correct.");
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in ParseProteinInputFile: " + ex.Message, ex);
            }

            return success;
        }

        public bool ProcessFile(string inputFilePath,
            string outputDirectoryPath,
            string parameterFilePath,
            bool resetErrorCode)
        {
            string proteinToPepMapFilePath = string.Empty;

            return ProcessFile(inputFilePath, outputDirectoryPath, parameterFilePath, resetErrorCode, out proteinToPepMapFilePath);
        }

        public bool ProcessFile(
            string inputFilePath,
            string outputDirectoryPath,
            string parameterFilePath,
            bool resetErrorCode,
            out string proteinToPepMapFilePath,
            string outputFileBaseName = "")
        {
            bool success;

            if (resetErrorCode)
            {
                SetErrorCode(eProteinCoverageErrorCodes.NoError);
            }

            OnStatusEvent("Initializing");
            proteinToPepMapFilePath = string.Empty;

            if (!LoadParameterFileSettings(parameterFilePath))
            {
                SetErrorMessage("Parameter file load error: " + parameterFilePath);

                if (mErrorCode == eProteinCoverageErrorCodes.NoError)
                {
                    SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile);
                }

                return false;
            }

            try
            {
                mCachedProteinInfoStartIndex = -1;
                ProteinDataCache.RemoveSymbolCharacters = RemoveSymbolCharacters;
                ProteinDataCache.IgnoreILDifferences = IgnoreILDifferences;

                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    OnErrorEvent("Input file name is empty");
                    SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath);
                    return false;
                }

                // Note that the results file path will be auto-defined in CreateProteinCoverageFile
                mResultsFilePath = string.Empty;

                if (string.IsNullOrWhiteSpace(ProteinInputFilePath))
                {
                    SetErrorMessage("Protein file name is empty");
                    SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath);
                    return false;
                }
                else if (!File.Exists(ProteinInputFilePath))
                {
                    SetErrorMessage("Protein input file not found: " + ProteinInputFilePath);
                    SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath);
                    return false;
                }

                ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizer.ProcessFile_Start", true);

                // First read the protein input file
                mProgressStepDescription = "Reading protein input file: " + Path.GetFileName(ProteinInputFilePath);
                UpdateProgress(mProgressStepDescription, 0, eProteinCoverageProcessingSteps.CacheProteins);

                success = ParseProteinInputFile();

                if (success)
                {
                    mProgressStepDescription = "Complete reading protein input file: " + Path.GetFileName(ProteinInputFilePath);
                    UpdateProgress(mProgressStepDescription, 100, eProteinCoverageProcessingSteps.CacheProteins);

                    // Now read the peptide input file
                    success = ParsePeptideInputFile(inputFilePath, outputDirectoryPath, outputFileBaseName, out proteinToPepMapFilePath);

                    if (success & !SearchAllProteinsSkipCoverageComputationSteps)
                    {
                        CreateProteinCoverageFile(inputFilePath, outputDirectoryPath, outputFileBaseName);
                    }

                    UpdateProgress("Processing complete; deleting the temporary SQLite database", 100,
                        eProteinCoverageProcessingSteps.WriteProteinCoverageFile);

                    // All done; delete the temporary SQLite database
                    ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizer.ProcessFile_Complete");

                    UpdateProgress("Done");

                    mProteinPeptideStats = null;
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in ProcessFile:" + Environment.NewLine + ex.Message, ex);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Read the next chunk of proteins from the database (SequenceID, ProteinName, ProteinSequence)
        /// </summary>
        /// <returns>The number of records read</returns>
        /// <remarks></remarks>
        private int ReadProteinInfoChunk(int startIndex, bool[] proteinUpdated, bool forceReload)
        {
            // We use a SQLite database to store the protein sequences (to avoid running out of memory when parsing large protein lists)
            // However, we will store the most recently loaded peptides in mCachedProteinInfoCount() and
            // will only reload them if startIndex is different than mCachedProteinInfoStartIndex

            // Reset the values in proteinUpdated()
            Array.Clear(proteinUpdated, 0, proteinUpdated.Length);

            if (!forceReload &&
                mCachedProteinInfoStartIndex >= 0 &&
                mCachedProteinInfoStartIndex == startIndex &&
                mCachedProteinInfo != null)
            {

                // The data loaded in memory is already valid; no need to reload
                return mCachedProteinInfoCount;
            }

            // Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
            // Store the information in the four local arrays

            int endIndex = startIndex + PROTEIN_CHUNK_COUNT - 1;

            mCachedProteinInfoStartIndex = startIndex;
            mCachedProteinInfoCount = 0;
            if (mCachedProteinInfo == null)
            {
                mCachedProteinInfo = new clsProteinFileDataCache.udtProteinInfoType[PROTEIN_CHUNK_COUNT];
            }

            foreach (var udtProtein in ProteinDataCache.GetCachedProteins(startIndex, endIndex))
            {
                var cached = mCachedProteinInfo[mCachedProteinInfoCount];
                // NOTE: cached is a struct, and therefore is a copy of the values in the array
                cached.UniqueSequenceID = udtProtein.UniqueSequenceID;
                cached.Description = udtProtein.Description;
                cached.Name = udtProtein.Name;
                cached.Sequence = udtProtein.Sequence;
                cached.PercentCoverage = udtProtein.PercentCoverage;
                // Update the values in the array. The other option would be to index the array for each of the assignments above.
                mCachedProteinInfo[mCachedProteinInfoCount] = cached;

                mCachedProteinInfoCount += 1;
            }

            return mCachedProteinInfoCount;
        }

        private void SaveDataPlusAllProteinsFile(
            string peptideInputFilePath,
            string outputDirectoryPath,
            string outputFileBaseName,
            char[] sepChars,
            int terminatorSize)
        {
            try
            {
                string dataPlusAllProteinsFile = ConstructOutputFilePath(peptideInputFilePath, FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS,
                    outputDirectoryPath, outputFileBaseName);

                UpdateProgress("Creating the data plus all-proteins output file: " + Path.GetFileName(dataPlusAllProteinsFile));

                using (var dataPlusProteinsWriter = new StreamWriter(new FileStream(dataPlusAllProteinsFile, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    int currentLine = 1;
                    long bytesRead = 0;

                    using (var reader = new StreamReader(new FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        while (!reader.EndOfStream)
                        {
                            string dataLine = reader.ReadLine();
                            if (dataLine == null)
                                continue;

                            bytesRead += dataLine.Length + terminatorSize;
                            dataLine = dataLine.TrimEnd();

                            if (currentLine % 500 == 0)
                            {
                                UpdateProgress("Creating the data plus all-proteins output file", Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100), eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile);
                            }

                            if (currentLine == 1 && PeptideFileSkipFirstLine)
                            {
                                // Print out the first line, but append a new column name
                                dataPlusProteinsWriter.WriteLine(dataLine + "\t" + "Protein_Name");
                            }
                            else if (dataLine.Length > 0)
                            {
                                bool validLine = false;
                                string peptideSequence = "";

                                try
                                {
                                    // Split the line, but for efficiency purposes, only parse out the first 3 columns
                                    var dataCols = dataLine.Split(sepChars, 3);

                                    switch (PeptideFileFormatCode)
                                    {
                                        case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence:
                                            // proteinName = dataCols(0)

                                            if (dataCols.Length > 1 && !string.IsNullOrWhiteSpace(dataCols[1]))
                                            {
                                                peptideSequence = dataCols[1];
                                                validLine = true;
                                            }

                                            break;
                                        default:
                                            // Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                            peptideSequence = dataCols[0];
                                            // proteinName = String.Empty
                                            validLine = true;
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    validLine = false;
                                }

                                if (!validLine)
                                {
                                    dataPlusProteinsWriter.WriteLine(dataLine + "\t" + "?");
                                }
                                else
                                {
                                    char prefixResidue;
                                    char suffixResidue;
                                    peptideSequence = GetCleanPeptideSequence(peptideSequence, out prefixResidue, out suffixResidue, RemoveSymbolCharacters);

                                    List<string> proteins = null;
                                    if (mPeptideToProteinMapResults.TryGetValue(peptideSequence, out proteins))
                                    {
                                        foreach (string protein in proteins)
                                            dataPlusProteinsWriter.WriteLine(dataLine + "\t" + protein);
                                    }
                                    else if (currentLine == 1)
                                    {
                                        // This is likely a header line
                                        dataPlusProteinsWriter.WriteLine(dataLine + "\t" + "Protein_Name");
                                    }
                                    else
                                    {
                                        dataPlusProteinsWriter.WriteLine(dataLine + "\t" + "?");
                                    }
                                }
                            }
                            else
                            {
                                dataPlusProteinsWriter.WriteLine();
                            }

                            currentLine += 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in SaveDataPlusAllProteinsFile: " + ex.Message, ex);
            }
        }

        private void SearchProteinsUsingLeaderSequences()
        {
            int leaderSequenceMinimumLength = mLeaderSequenceCache.LeaderSequenceMinimumLength;

            var proteinUpdated = new bool[PROTEIN_CHUNK_COUNT];

            // Step through the proteins in memory and compare the residues for each to mLeaderSequenceHashTable
            // If mSearchAllProteinsForPeptideSequence = False, require that the protein name in the peptide input file matches the protein being examined

            try
            {
                string progressMessageBase = "Comparing proteins to peptide leader sequences";
                OnStatusEvent(progressMessageBase);

                int proteinProcessIterations = 0;
                int proteinProcessIterationsExpected = Convert.ToInt32(Math.Ceiling(ProteinDataCache.GetProteinCountCached() / (double)PROTEIN_CHUNK_COUNT)) * PROTEIN_CHUNK_COUNT;
                if (proteinProcessIterationsExpected < 1)
                    proteinProcessIterationsExpected = 1;

                UpdateProgress(progressMessageBase, 0,
                    eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences);

                int startIndex = 0;
                do
                {
                    // Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
                    // Store the information in the four local arrays
                    int proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, false);

                    for (int proteinIndex = 0; proteinIndex < proteinCount; proteinIndex++)
                    {
                        string proteinSequence = string.Copy(mCachedProteinInfo[proteinIndex].Sequence);
                        bool proteinSequenceUpdated = false;

                        for (int proteinSeqCharIndex = 0; proteinSeqCharIndex <= proteinSequence.Length - leaderSequenceMinimumLength; proteinSeqCharIndex++)
                        {
                            int cachedPeptideMatchIndex;

                            // Call .GetFirstPeptideIndexForLeaderSequence to see if the sequence cache contains the leaderSequenceMinimumLength residues starting at proteinSeqCharIndex
                            if (SearchAllProteinsSkipCoverageComputationSteps)
                            {
                                // No need to capitalize proteinSequence since it's already capitalized
                                cachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength));
                            }
                            else
                            {
                                // Need to change proteinSequence to all caps when calling GetFirstPeptideIndexForLeaderSequence
                                cachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength).ToUpper());
                            }

                            if (cachedPeptideMatchIndex >= 0)
                            {
                                // mLeaderSequenceCache contains 1 or more peptides that start with proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength)
                                // Test each of the peptides against this protein

                                do
                                {
                                    bool testPeptide;

                                    if (SearchAllProteinsForPeptideSequence)
                                    {
                                        testPeptide = true;
                                    }
                                    // Make sure that the protein for cachedPeptideMatchIndex matches this protein name
                                    else if (string.Equals(mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].ProteinName, mCachedProteinInfo[proteinIndex].Name, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        testPeptide = true;
                                    }
                                    else
                                    {
                                        testPeptide = false;
                                    }

                                    // Cache the peptide length in peptideLength
                                    int peptideLength = mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence.Length;

                                    // Only compare the full sequence to the protein if:
                                    // a) the protein name matches (or mSearchAllProteinsForPeptideSequence = True) and
                                    // b) the peptide sequence doesn't pass the end of the protein
                                    if (testPeptide && proteinSeqCharIndex + peptideLength <= proteinSequence.Length)
                                    {
                                        // See if the full sequence matches the protein
                                        bool matchFound = false;
                                        if (SearchAllProteinsSkipCoverageComputationSteps)
                                        {
                                            // No need to capitalize proteinSequence since it's already capitalized
                                            if (IgnoreILDifferences)
                                            {
                                                if ((proteinSequence.Substring(proteinSeqCharIndex, peptideLength) ?? "") == (mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequenceLtoI ?? ""))
                                                {
                                                    matchFound = true;
                                                }
                                            }
                                            else if ((proteinSequence.Substring(proteinSeqCharIndex, peptideLength) ?? "") == (mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence ?? ""))
                                            {
                                                matchFound = true;
                                            }
                                        }
                                        // Need to change proteinSequence to all caps when comparing to .PeptideSequence
                                        else if (IgnoreILDifferences)
                                        {
                                            if ((proteinSequence.Substring(proteinSeqCharIndex, peptideLength).ToUpper() ?? "") == (mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequenceLtoI ?? ""))
                                            {
                                                matchFound = true;
                                            }
                                        }
                                        else if ((proteinSequence.Substring(proteinSeqCharIndex, peptideLength).ToUpper() ?? "") == (mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence ?? ""))
                                        {
                                            matchFound = true;
                                        }

                                        if (matchFound)
                                        {
                                            int endIndex = proteinSeqCharIndex + peptideLength - 1;
                                            if (MatchPeptidePrefixAndSuffixToProtein)
                                            {
                                                matchFound = ValidatePrefixAndSuffix(proteinSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PrefixLtoI, mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].SuffixLtoI, proteinSeqCharIndex, endIndex);
                                            }

                                            if (matchFound)
                                            {
                                                string peptideSequenceForKeySource;
                                                string peptideSequenceForKey;
                                                if (MatchPeptidePrefixAndSuffixToProtein)
                                                {
                                                    peptideSequenceForKeySource = ConstructPeptideSequenceForKey(mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].Prefix, mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].Suffix);
                                                }
                                                else
                                                {
                                                    // I'm purposely not using String.Copy() here in order to obtain increased speed
                                                    peptideSequenceForKeySource = mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence;
                                                }

                                                if (IgnoreILDifferences)
                                                {
                                                    // Replace all L characters with I
                                                    peptideSequenceForKey = peptideSequenceForKeySource.Replace('L', 'I');
                                                }
                                                else
                                                {
                                                    // I'm purposely not using String.Copy() here in order to obtain increased speed
                                                    peptideSequenceForKey = peptideSequenceForKeySource;
                                                }

                                                if (!SearchAllProteinsSkipCoverageComputationSteps)
                                                {
                                                    // Capitalize the protein sequence letters where this peptide matched
                                                    int nextStartIndex = endIndex + 1;
                                                    string newProteinSequence = string.Empty;
                                                    if (proteinSeqCharIndex > 0)
                                                    {
                                                        newProteinSequence = proteinSequence.Substring(0, proteinSeqCharIndex);
                                                    }

                                                    newProteinSequence += proteinSequence.Substring(proteinSeqCharIndex, nextStartIndex - proteinSeqCharIndex).ToUpper();
                                                    newProteinSequence += proteinSequence.Substring(nextStartIndex);
                                                    proteinSequence = string.Copy(newProteinSequence);

                                                    proteinSequenceUpdated = true;
                                                }

                                                bool matchIsNew;

                                                if (TrackPeptideCounts)
                                                {
                                                    string proteinPeptideKey = Convert.ToString(mCachedProteinInfo[proteinIndex].UniqueSequenceID) + "::" + peptideSequenceForKey;

                                                    matchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey);
                                                }
                                                else
                                                {
                                                    // Must always assume the match is new since not tracking peptide counts
                                                    matchIsNew = true;
                                                }

                                                if (matchIsNew)
                                                {
                                                    if (SaveProteinToPeptideMappingFile)
                                                    {
                                                        WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo[proteinIndex].Name, peptideSequenceForKeySource, proteinSeqCharIndex + 1, endIndex + 1);
                                                    }

                                                    if (SaveSourceDataPlusProteinsFile)
                                                    {
                                                        StorePeptideToProteinMatch(mLeaderSequenceCache.mCachedPeptideSeqInfo[cachedPeptideMatchIndex].PeptideSequence, mCachedProteinInfo[proteinIndex].Name);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    cachedPeptideMatchIndex = mLeaderSequenceCache.GetNextPeptideWithLeaderSequence(cachedPeptideMatchIndex);
                                }
                                while (cachedPeptideMatchIndex >= 0);
                            }
                        }

                        if (proteinSequenceUpdated)
                        {
                            // NOTE: The following is valid only because mCachedProteinInfo is an array, and not a generic collection
                            mCachedProteinInfo[proteinIndex].Sequence = string.Copy(proteinSequence);
                            proteinUpdated[proteinIndex] = true;
                        }

                        proteinProcessIterations += 1;
                        if (proteinProcessIterations % 100 == 0)
                        {
                            UpdateProgress(Convert.ToSingle(proteinProcessIterations / (double)proteinProcessIterationsExpected * 100), eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences);
                        }

                        if (mAbortProcessing)
                            break;
                    }

                    // Store the updated protein sequence information in the SQLite DB
                    UpdateSequenceDbDataValues(proteinUpdated, proteinCount);

                    // Increment startIndex to obtain the next chunk of proteins
                    startIndex += PROTEIN_CHUNK_COUNT;
                }
                while (startIndex < ProteinDataCache.GetProteinCountCached());
            }
            catch (Exception ex)
            {
                SetErrorMessage("Error in SearchProteinsUsingLeaderSequences: " + ex.Message, ex);
            }
        }

        private void SearchProteinsUsingCachedPeptides(IDictionary<string, int> shortPeptideCache)
        {
            if (shortPeptideCache.Count > 0)
            {
                UpdateProgress("Comparing proteins to short peptide sequences");

                // Need to step through the proteins and match them to the data in shortPeptideCache
                FindSequenceMatchForPeptideList(shortPeptideCache, string.Empty);
            }
        }

        private void StorePeptideToProteinMatch(string cleanPeptideSequence, string proteinName)
        {
            // Store the mapping between peptide sequence and protein name
            List<string> proteins = null;
            if (mPeptideToProteinMapResults.TryGetValue(cleanPeptideSequence, out proteins))
            {
                proteins.Add(proteinName);
            }
            else
            {
                proteins = new List<string>() { proteinName };
                mPeptideToProteinMapResults.Add(cleanPeptideSequence, proteins);
            }
        }

        private bool ValidateColumnCountInInputFile(string peptideInputFilePath)
        {
            bool success;
            if (PeptideFileFormatCode == ePeptideFileColumnOrderingCode.SequenceOnly)
            {
                // Simply return true; don't even pre-read the file
                // However, auto-switch mSearchAllProteinsForPeptideSequence to true if not true
                if (!SearchAllProteinsForPeptideSequence)
                {
                    SearchAllProteinsForPeptideSequence = true;
                }

                return true;
            }

            var argePeptideFileColumnOrdering = PeptideFileFormatCode;
            success = ValidateColumnCountInInputFile(peptideInputFilePath, ref argePeptideFileColumnOrdering, PeptideFileSkipFirstLine, PeptideInputFileDelimiter);
            PeptideFileFormatCode = argePeptideFileColumnOrdering;

            if (success)
            {
                if (PeptideFileFormatCode == ePeptideFileColumnOrderingCode.SequenceOnly)
                {
                    // Need to auto-switch to search all proteins
                    SearchAllProteinsForPeptideSequence = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Read the first two lines to check whether the data file actually has only one column when the user has
        /// specified mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
        /// If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly, the file isn't even opened
        /// </summary>
        /// <param name="peptideInputFilePath"></param>
        /// <param name="ePeptideFileColumnOrdering">Input / Output parameter</param>
        /// <param name="skipFirstLine"></param>
        /// <param name="columnDelimiter"></param>
        /// <returns>True if no problems; False if the user chooses to abort</returns>
        public static bool ValidateColumnCountInInputFile(
            string peptideInputFilePath,
            ref ePeptideFileColumnOrderingCode ePeptideFileColumnOrdering,
            bool skipFirstLine,
            char columnDelimiter)
        {

            // Open the file and read in the lines
            using (var reader = new StreamReader(new FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                int currentLine = 1;
                while (!reader.EndOfStream && currentLine < 3)
                {
                    string lineIn = reader.ReadLine();
                    if (lineIn == null)
                        continue;

                    string dataLine = lineIn.TrimEnd();

                    if (currentLine == 1 && skipFirstLine)
                    {
                        // do nothing, skip the first line
                    }
                    else if (dataLine.Length > 0)
                    {
                        try
                        {
                            var dataCols = dataLine.Split(columnDelimiter);
                            if (!skipFirstLine && currentLine == 1 ||
                                skipFirstLine && currentLine == 2)
                            {
                                if (dataCols.Length == 1 && ePeptideFileColumnOrdering == ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence)
                                {
                                    // Auto switch to ePeptideFileColumnOrderingCode.SequenceOnly
                                    ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ignore the error
                        }
                    }

                    currentLine += 1;
                }
            }

            return true;
        }

        private bool ValidatePrefixAndSuffix(string proteinSequence, char prefixResidue, char suffixResidue, int startIndex, int endIndex)
        {
            bool matchValid = true;

            if (char.IsLetter(prefixResidue))
            {
                if (startIndex >= 1)
                {
                    if (char.ToUpper(proteinSequence[startIndex - 1]) != prefixResidue)
                    {
                        matchValid = false;
                    }
                }
            }
            else if (prefixResidue == '-' && startIndex != 0)
            {
                matchValid = false;
            }

            if (matchValid)
            {
                if (char.IsLetter(suffixResidue))
                {
                    if (endIndex < proteinSequence.Length - 1)
                    {
                        if (char.ToUpper(proteinSequence[endIndex + 1]) != suffixResidue)
                        {
                            matchValid = false;
                        }
                    }
                    else
                    {
                        matchValid = false;
                    }
                }
                else if (suffixResidue == '-' && endIndex < proteinSequence.Length - 1)
                {
                    matchValid = false;
                }
            }

            return matchValid;
        }

        private void WriteEntryToProteinToPeptideMappingFile(string proteinName, string peptideSequenceForKey, int startResidue, int endResidue)
        {
            if (SaveProteinToPeptideMappingFile && mProteinToPeptideMappingOutputFile != null)
            {
                mProteinToPeptideMappingOutputFile.WriteLine(proteinName + "\t" + peptideSequenceForKey + "\t" + startResidue + "\t" + endResidue);
            }
        }

        protected void ResetProgress(string stepDescription)
        {
            mProgressStepDescription = string.Copy(stepDescription);
            mProgressPercentComplete = 0;
            ProgressReset?.Invoke();
        }

        protected void SetErrorCode(eProteinCoverageErrorCodes eNewErrorCode)
        {
            SetErrorCode(eNewErrorCode, false);
        }

        protected void SetErrorCode(eProteinCoverageErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            if (leaveExistingErrorCodeUnchanged && mErrorCode != eProteinCoverageErrorCodes.NoError)
            {
                // An error code is already defined; do not change it
            }
            else
            {
                mErrorCode = eNewErrorCode;
            }
        }

        protected void SetErrorMessage(string message, Exception ex = null)
        {
            if (message == null)
            {
                mErrorMessage = string.Empty;
            }
            else
            {
                mErrorMessage = message;
            }

            if (mErrorMessage.Length > 0)
            {
                OnErrorEvent(mErrorMessage, ex);
                UpdateProgress(mErrorMessage);
            }
        }

        protected void UpdateProgress(string stepDescription)
        {
            mProgressStepDescription = string.Copy(stepDescription);
            ProgressChanged?.Invoke(ProgressStepDescription, ProgressPercentComplete);
        }

        protected void UpdateProgress(float percentComplete, eProteinCoverageProcessingSteps eCurrentProcessingStep)
        {
            UpdateProgress(ProgressStepDescription, percentComplete, eCurrentProcessingStep);
        }

        protected void UpdateProgress(string stepDescription, float percentComplete, eProteinCoverageProcessingSteps eCurrentProcessingStep)
        {
            mProgressStepDescription = string.Copy(stepDescription);
            mCurrentProcessingStep = eCurrentProcessingStep;

            if (percentComplete < 0)
            {
                percentComplete = 0;
            }
            else if (percentComplete > 100)
            {
                percentComplete = 100;
            }

            float startPercent = mPercentCompleteStartLevels[(int)eCurrentProcessingStep];
            float endPercent = mPercentCompleteStartLevels[(int)eCurrentProcessingStep + 1];

            // Use the start and end percent complete values for the specified processing step to convert percentComplete to an overall percent complete value
            mProgressPercentComplete = startPercent + Convert.ToSingle(percentComplete / 100.0 * (endPercent - startPercent));

            ProgressChanged?.Invoke(ProgressStepDescription, ProgressPercentComplete);
        }

        private void LeaderSequenceCache_ProgressChanged(string taskDescription, float percentComplete)
        {
            UpdateProgress(percentComplete, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength);
        }

        private void LeaderSequenceCache_ProgressComplete()
        {
            UpdateProgress(100, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength);
        }

        private DateTime lastUpdate = DateTime.UtcNow;

        private void ProteinDataCache_ProteinCachedWithProgress(int proteinsCached, float percentFileProcessed)
        {
            const int CONSOLE_UPDATE_INTERVAL_SECONDS = 3;

            if (DateTime.UtcNow.Subtract(lastUpdate).TotalSeconds >= (double)CONSOLE_UPDATE_INTERVAL_SECONDS)
            {
                lastUpdate = DateTime.UtcNow;
                Console.Write(".");
            }

            UpdateProgress(percentFileProcessed, eProteinCoverageProcessingSteps.CacheProteins);
        }

        private void ProteinDataCache_ProteinCachingComplete()
        {
            UpdateProgress(100, eProteinCoverageProcessingSteps.CacheProteins);
        }
    }
}