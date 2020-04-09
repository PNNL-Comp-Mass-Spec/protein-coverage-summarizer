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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using PHRPReader;
using ProteinCoverageSummarizer;
using ProteinFileReader;

namespace PeptideToProteinMapEngine
{
    /// <summary>
    /// This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
    /// an accompanying file with peptide sequences to find the proteins that contain each peptide
    /// It will also optionally compute the percent coverage of each of the proteins
    /// </summary>
    public class clsPeptideToProteinMapEngine : PRISM.FileProcessor.ProcessFilesBase
    {
        public clsPeptideToProteinMapEngine()
        {
            InitializeVariables();
        }

        #region "Constants and Enums"

        public const string FILENAME_SUFFIX_INSPECT_RESULTS_FILE = "_inspect.txt";
        public const string FILENAME_SUFFIX_MSGFDB_RESULTS_FILE = "_msgfdb.txt";
        public const string FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE = "_msgfplus.txt";

        public const string FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING = "_PepToProtMap.txt";

        protected const string FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES = "_peptides";

        // The following are the initial % complete value displayed during each of these stages
        protected const float PERCENT_COMPLETE_PREPROCESSING = 0;
        protected const float PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER = 5;
        protected const float PERCENT_COMPLETE_POSTPROCESSING = 95;

        public enum ePeptideInputFileFormatConstants
        {
            Unknown = -1,
            AutoDetermine = 0,
            PeptideListFile = 1,             // First column is peptide sequence
            ProteinAndPeptideFile = 2,       // First column is protein name, second column is peptide sequence
            InspectResultsFile = 3,          // Inspect results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
            [Obsolete("Old Name")]
            MSGFDBResultsFile = 4,
            MSGFPlusResultsFile = 4,         // MS-GF+ results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
            PHRPFile = 5                    // Sequest, Inspect, X!Tandem, or MS-GF+ synopsis or first-hits file created by PHRP; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
        }

        #endregion

        #region "Structures"

        protected struct udtProteinIDMapInfoType
        {
            public int ProteinID;
            public string Peptide;
            public int ResidueStart;
            public int ResidueEnd;

            /// <summary>
            /// Show the peptide sequence
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Peptide + ", Protein ID " + ProteinID;
            }
        }

        protected struct udtPepToProteinMappingType
        {
            public string Peptide;
            public string Protein;
            public int ResidueStart;
            public int ResidueEnd;

            /// <summary>
            /// Show the peptide sequence
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Peptide + ", Protein " + Protein;
            }
        }
        #endregion

        #region "Classwide variables"
        protected clsProteinCoverageSummarizer mProteinCoverageSummarizer;

        // When processing an inspect search result file, if you provide the inspect parameter file name,
        // then this program will read the parameter file and look for the "mod," lines.  The user-assigned mod
        // names will be extracted and used when "cleaning" the peptides prior to looking for matching proteins
        private string mInspectParameterFilePath;

        private string mStatusMessage;

        // The following is used when the input file is Sequest, X!Tandem, Inspect, or MS-GF+ results file
        // Keys are peptide sequences; values are Lists of scan numbers that each peptide was observed in
        // Keys may have mod symbols in them; those symbols will be removed in PreProcessDataWriteOutPeptides
        private SortedList<string, SortedSet<int>> mUniquePeptideList;

        // Mod names must be lower case, and 4 characters in length (or shorter)
        // Only used with Inspect since mods in MS-GF+ are simply numbers, e.g. R.DNFM+15.995SATQAVEYGLVDAVM+15.995TK.R
        // while mods in Sequest and XTandem are symbols (*, #, @)
        private List<string> mInspectModNameList;

        #endregion

        #region "Properties"

        // ReSharper disable UnusedMember.Global

        /// <summary>
        /// Legacy property; superseded by DeleteTempFiles
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool DeleteInspectTempFiles
        {
            get => DeleteTempFiles;
            set => DeleteTempFiles = value;
        }

        public bool DeleteTempFiles { get; set; }

        public bool IgnoreILDifferences
        {
            get => mProteinCoverageSummarizer.IgnoreILDifferences;
            set => mProteinCoverageSummarizer.IgnoreILDifferences = value;
        }

        public string InspectParameterFilePath
        {
            get => mInspectParameterFilePath;
            set
            {
                if (value == null)
                    value = string.Empty;
                mInspectParameterFilePath = value;
            }
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

        public ePeptideInputFileFormatConstants PeptideInputFileFormat { get; set; }

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
            set
            {
                if (value == null)
                    value = string.Empty;
                mProteinCoverageSummarizer.ProteinInputFilePath = value;
            }
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

        public bool SaveSourceDataPlusProteinsFile
        {
            get => mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile;
            set => mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile = value;
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

        // ReSharper restore UnusedMember.Global

        #endregion

        public override void AbortProcessingNow()
        {
            base.AbortProcessingNow();
            if (mProteinCoverageSummarizer != null)
            {
                mProteinCoverageSummarizer.AbortProcessingNow();
            }
        }

        public ePeptideInputFileFormatConstants DetermineResultsFileFormat(string filePath)
        {
            // Examine the filePath to determine the file format

            if (Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_INSPECT_RESULTS_FILE.ToLower()))
            {
                return ePeptideInputFileFormatConstants.InspectResultsFile;
            }
            else if (Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_MSGFDB_RESULTS_FILE.ToLower()))
            {
                return ePeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }
            else if (Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE.ToLower()))
            {
                return ePeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }
            else if (PeptideInputFileFormat != ePeptideInputFileFormatConstants.AutoDetermine & PeptideInputFileFormat != ePeptideInputFileFormatConstants.Unknown)
            {
                return PeptideInputFileFormat;
            }

            string baseNameLCase = Path.GetFileNameWithoutExtension(filePath);
            if (baseNameLCase.EndsWith("_MSGFDB", StringComparison.OrdinalIgnoreCase) ||
                baseNameLCase.EndsWith("_MSGFPlus", StringComparison.OrdinalIgnoreCase))
            {
                return ePeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }

            var eResultType = clsPHRPReader.AutoDetermineResultType(filePath);
            if (eResultType != clsPHRPReader.ePeptideHitResultType.Unknown)
            {
                return ePeptideInputFileFormatConstants.PHRPFile;
            }

            ShowMessage("Unable to determine the format of the input file based on the filename suffix; will assume the first column contains peptide sequence");
            return ePeptideInputFileFormatConstants.PeptideListFile;
        }

        public bool ExtractModInfoFromInspectParamFile(string inspectParamFilePath, ref List<string> inspectModNames)
        {
            try
            {
                if (inspectModNames == null)
                {
                    inspectModNames = new List<string>();
                }
                else
                {
                    inspectModNames.Clear();
                }

                if (inspectParamFilePath == null || inspectParamFilePath.Length == 0)
                {
                    return false;
                }

                ShowMessage("Looking for mod definitions in the Inspect param file: " + Path.GetFileName(inspectParamFilePath));

                // Read the contents of inspectParamFilePath
                using (var reader = new StreamReader(new FileStream(inspectParamFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    while (!reader.EndOfStream)
                    {
                        string lineIn = reader.ReadLine();
                        if (lineIn == null)
                            continue;

                        lineIn = lineIn.TrimEnd();

                        if (lineIn.Length > 0)
                        {
                            if (lineIn[0] == '#')
                            {
                                // Comment line; skip it
                            }
                            else if (lineIn.ToLower().StartsWith("mod"))
                            {
                                // Modification definition line

                                // Split the line on commas
                                var splitLine = lineIn.Split(',');

                                if (splitLine.Length >= 5 && (splitLine[0].ToLower().Trim() ?? "") == "mod")
                                {
                                    string modName;
                                    modName = splitLine[4].ToLower();

                                    if (modName.Length > 4)
                                    {
                                        // Only keep the first 4 characters of the modification name
                                        modName = modName.Substring(0, 4);
                                    }

                                    inspectModNames.Add(modName);
                                    ShowMessage("Found modification: " + lineIn + "   -->   Mod Symbol \"" + modName + "\"");
                                }
                            }
                        }
                    }
                }

                Console.WriteLine();

                return true;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error reading the Inspect parameter file (" + Path.GetFileName(inspectParamFilePath) + ")";
                HandleException(mStatusMessage, ex);
            }

            return false;
        }

        public override string GetErrorMessage()
        {
            return GetBaseClassErrorMessage();
        }

        private void InitializeVariables()
        {
            PeptideInputFileFormat = ePeptideInputFileFormatConstants.AutoDetermine;
            DeleteTempFiles = true;

            mInspectParameterFilePath = string.Empty;

            AbortProcessing = false;
            mStatusMessage = string.Empty;

            mProteinCoverageSummarizer = new clsProteinCoverageSummarizer();
            RegisterEvents(mProteinCoverageSummarizer);
            mProteinCoverageSummarizer.ProgressChanged += ProteinCoverageSummarizer_ProgressChanged;
            mProteinCoverageSummarizer.ProgressReset += ProteinCoverageSummarizer_ProgressReset;

            mInspectModNameList = new List<string>();

            mUniquePeptideList = new SortedList<string, SortedSet<int>>();
        }

        /// <summary>
        /// Open the file and read the first line
        /// Examine it to determine if it looks like a header line
        /// </summary>
        /// <returns></returns>
        private bool IsHeaderLinePresent(string filePath, ePeptideInputFileFormatConstants eInputFileFormat)
        {
            var sepChars = new char[] { '\t' };

            try
            {
                bool headerFound = false;

                // Read the contents of filePath
                using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    if (!reader.EndOfStream)
                    {
                        string dataLine = reader.ReadLine();

                        if (!string.IsNullOrEmpty(dataLine))
                        {
                            var dataCols = dataLine.Split(sepChars);

                            if (eInputFileFormat == ePeptideInputFileFormatConstants.ProteinAndPeptideFile)
                            {
                                if (dataCols.Length > 1 && dataCols[1].StartsWith("peptide", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    headerFound = true;
                                }
                            }
                            else if (eInputFileFormat == ePeptideInputFileFormatConstants.PeptideListFile)
                            {
                                if (dataCols[0].StartsWith("peptide", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    headerFound = true;
                                }
                            }
                            else if (dataCols.Any(dataColumn => dataColumn.ToLower().StartsWith("peptide")))
                            {
                                headerFound = true;
                            }
                        }
                    }
                }

                return headerFound;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error looking for a header line in " + Path.GetFileName(filePath);
                HandleException(mStatusMessage, ex);
                return false;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            return mProteinCoverageSummarizer.LoadParameterFileSettings(parameterFilePath);
        }

        protected bool PostProcessPSMResultsFile(string peptideListFilePath,
                                                 string proteinToPepMapFilePath,
                                                 bool deleteWorkingFiles)
        {
            const string UNKNOWN_PROTEIN_NAME = "__NoMatch__";

            string[] proteins;
            int[] proteinIDPointerArray;

            udtProteinIDMapInfoType[] proteinMapInfo;

            try
            {
                Console.WriteLine();

                ShowMessage("Post-processing the results files");

                if (mUniquePeptideList == null || mUniquePeptideList.Count == 0)
                {
                    mStatusMessage = "Error in PostProcessPSMResultsFile: mUniquePeptideList is empty; this is unexpected; unable to continue";

                    HandleException(mStatusMessage, new Exception("Empty Array"));

                    return false;
                }

                proteins = new string[1];
                proteinIDPointerArray = new int[1];
                proteinMapInfo = new udtProteinIDMapInfoType[1];

                PostProcessPSMResultsFileReadMapFile(proteinToPepMapFilePath, ref proteins, ref proteinIDPointerArray, ref proteinMapInfo);

                // Sort proteinMapInfo on peptide, then on protein
                Array.Sort(proteinMapInfo, new ProteinIDMapInfoComparer());

                string peptideToProteinMappingFilePath;

                // Create the final result file
                if (proteinToPepMapFilePath.Contains(FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES + clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING))
                {
                    // This was an old name format that is no longer used
                    // This code block should, therefore, never be reached
                    peptideToProteinMappingFilePath = proteinToPepMapFilePath.Replace(
                        FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES + clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                        FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING);
                }
                else
                {
                    peptideToProteinMappingFilePath = proteinToPepMapFilePath.Replace(
                        clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                        FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING);

                    if (string.Equals(proteinToPepMapFilePath, peptideToProteinMappingFilePath))
                    {
                        // The filename was not in the exacted format
                        peptideToProteinMappingFilePath = clsProteinCoverageSummarizer.ConstructOutputFilePath(
                            proteinToPepMapFilePath, FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING,
                            Path.GetDirectoryName(proteinToPepMapFilePath), "");
                    }
                }

                LogMessage("Creating " + Path.GetFileName(peptideToProteinMappingFilePath));

                using (var writer = new StreamWriter(new FileStream(peptideToProteinMappingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    // Write the headers
                    writer.WriteLine("Peptide" + "\t" +
                        "Protein" + "\t" +
                        "Residue_Start" + "\t" +
                        "Residue_End");

                    // Initialize the Binary Search comparer
                    var proteinMapPeptideComparer = new ProteinIDMapInfoPeptideSearchComparer();

                    // Assure that proteinIDPointerArray and proteins are sorted in parallel
                    Array.Sort(proteinIDPointerArray, proteins);

                    // Initialize cachedData
                    var cachedData = new List<udtPepToProteinMappingType>();

                    // Initialize cachedDataComparer
                    var cachedDataComparer = new PepToProteinMappingComparer();
                    foreach (var peptideEntry in mUniquePeptideList)
                    {
                        var prefixResidue = default(char);
                        var suffixResidue = default(char);

                        // Construct the clean sequence for this peptide
                        var cleanSequence = clsProteinCoverageSummarizer.GetCleanPeptideSequence(
                            peptideEntry.Key,
                            out prefixResidue,
                            out suffixResidue,
                            mProteinCoverageSummarizer.RemoveSymbolCharacters);

                        if (mInspectModNameList.Count > 0)
                        {
                            cleanSequence = RemoveInspectMods(cleanSequence, ref mInspectModNameList);
                        }

                        // Look for cleanSequence in proteinMapInfo
                        int matchIndex = Array.BinarySearch(proteinMapInfo, cleanSequence, proteinMapPeptideComparer);
                        if (matchIndex < 0)
                        {
                            // Match not found; this is unexpected
                            // However, this code will be reached if the peptide is not present in any of the proteins in the protein data file
                            writer.WriteLine(
                                peptideEntry.Key + "\t" +
                                UNKNOWN_PROTEIN_NAME + "\t" +
                                0.ToString() + "\t" +
                                0.ToString());
                        }
                        else
                        {
                            // Decrement matchIndex until the first match in proteinMapInfo is found
                            while (matchIndex > 0 && proteinMapInfo[matchIndex - 1].Peptide == cleanSequence)
                                matchIndex -= 1;

                            // Now write out each of the proteins for this peptide
                            // We're caching results to cachedData so that we can sort by protein name
                            cachedData.Clear();

                            do
                            {
                                // Find the Protein for ID proteinMapInfo(matchIndex).ProteinID
                                int proteinIDMatchIndex = Array.BinarySearch(proteinIDPointerArray, proteinMapInfo[matchIndex].ProteinID);
                                string protein;

                                if (proteinIDMatchIndex >= 0)
                                {
                                    protein = proteins[proteinIDMatchIndex];
                                }
                                else
                                {
                                    protein = UNKNOWN_PROTEIN_NAME;
                                }

                                try
                                {
                                    if ((protein ?? "") != (proteins[proteinMapInfo[matchIndex].ProteinID] ?? ""))
                                    {
                                        // This is unexpected
                                        ShowMessage("Warning: Unexpected protein ID lookup array mismatch for ID " + proteinMapInfo[matchIndex].ProteinID.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // This code shouldn't be reached
                                    // Ignore errors occur
                                }

                                var cachedDataEntry = new udtPepToProteinMappingType()
                                {
                                    Peptide = string.Copy(peptideEntry.Key),
                                    Protein = string.Copy(protein),
                                    ResidueStart = proteinMapInfo[matchIndex].ResidueStart,
                                    ResidueEnd = proteinMapInfo[matchIndex].ResidueEnd
                                };

                                cachedData.Add(cachedDataEntry);

                                matchIndex += 1;
                            }
                            while (matchIndex < proteinMapInfo.Length && proteinMapInfo[matchIndex].Peptide == cleanSequence);
                            if (cachedData.Count > 1)
                            {
                                cachedData.Sort(cachedDataComparer);
                            }

                            for (int cacheIndex = 0; cacheIndex < cachedData.Count; cacheIndex++)
                            {
                                var data = cachedData[cacheIndex];
                                writer.WriteLine(data.Peptide + "\t" +
                                    data.Protein + "\t" +
                                    data.ResidueStart.ToString() + "\t" +
                                    data.ResidueEnd.ToString());
                            }
                        }
                    }
                }

                if (deleteWorkingFiles)
                {
                    try
                    {
                        LogMessage("Deleting " + Path.GetFileName(peptideListFilePath));
                        File.Delete(peptideListFilePath);
                    }
                    catch (Exception ex)
                    {
                    }

                    try
                    {
                        LogMessage("Deleting " + Path.GetFileName(proteinToPepMapFilePath));
                        File.Delete(proteinToPepMapFilePath);
                    }
                    catch (Exception ex)
                    {
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error writing the Inspect or MS-GF+ peptide to protein map file in PostProcessPSMResultsFile";
                HandleException(mStatusMessage, ex);
            }

            return false;
        }

        protected bool PostProcessPSMResultsFileReadMapFile(string proteinToPepMapFilePath,
            ref string[] proteins,
            ref int[] proteinIDPointerArray,
            ref udtProteinIDMapInfoType[] proteinMapInfo)
        {
            int terminatorSize = 2;

            try
            {
                // Initialize the protein list dictionary
                var proteinList = new Dictionary<string, int>();

                int proteinMapInfoCount = 0;

                // Initialize the protein to peptide mapping array
                // We know the length will be at least as long as mUniquePeptideList, and easily twice that length
                proteinMapInfo = new udtProteinIDMapInfoType[(mUniquePeptideList.Count * 2)];

                LogMessage("Reading " + Path.GetFileName(proteinToPepMapFilePath));

                // Read the contents of proteinToPepMapFilePath
                using (var reader = new StreamReader(new FileStream(proteinToPepMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    string currentProtein = string.Empty;

                    int currentLine = 0;
                    long bytesRead = 0;

                    int currentProteinID = 0;

                    while (!reader.EndOfStream)
                    {
                        currentLine += 1;

                        if (AbortProcessing)
                            break;
                        string lineIn = reader.ReadLine();
                        if (lineIn == null)
                            continue;
                        bytesRead += lineIn.Length + terminatorSize;

                        lineIn = lineIn.TrimEnd();

                        if (currentLine == 1)
                        {
                            // Header line; skip it
                            continue;
                        }

                        if (lineIn.Length == 0)
                        {
                            continue;
                        }

                        // Split the line
                        var splitLine = lineIn.Split('\t');

                        if (splitLine.Length < 4)
                        {
                            continue;
                        }

                        if (proteinMapInfoCount >= proteinMapInfo.Length)
                        {
                            var oldProteinMapInfo = proteinMapInfo;
                            proteinMapInfo = new udtProteinIDMapInfoType[(proteinMapInfo.Length * 2)];
                            Array.Copy(oldProteinMapInfo, proteinMapInfo, Math.Min(proteinMapInfo.Length * 2, oldProteinMapInfo.Length));
                        }

                        if (currentProtein.Length == 0 || (currentProtein ?? "") != (splitLine[0] ?? ""))
                        {
                            // Determine the Protein ID for this protein

                            currentProtein = splitLine[0];

                            if (!proteinList.TryGetValue(currentProtein, out currentProteinID))
                            {
                                // New protein; add it, assigning it index proteinList.Count
                                currentProteinID = proteinList.Count;
                                proteinList.Add(currentProtein, currentProteinID);
                            }
                        }

                        {
                            var info = proteinMapInfo[proteinMapInfoCount];
                            info.ProteinID = currentProteinID;
                            info.Peptide = splitLine[1];
                            info.ResidueStart = int.Parse(splitLine[2]);
                            info.ResidueEnd = int.Parse(splitLine[3]);
                            // C# struct in collection - make sure we store the updated values
                            proteinMapInfo[proteinMapInfoCount] = info;
                        }

                        proteinMapInfoCount += 1;
                        if (currentLine % 1000 == 0)
                        {
                            UpdateProgress(PERCENT_COMPLETE_POSTPROCESSING +
                                Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100) * (PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100);
                        }
                    }
                }

                // Populate proteins() and proteinIDPointerArray() using proteinList
                proteins = new string[proteinList.Count];
                proteinIDPointerArray = new int[proteinList.Count];

                // Note: the Keys and Values are not necessarily sorted, but will be copied in the identical order
                proteinList.Keys.CopyTo(proteins, 0);
                proteinList.Values.CopyTo(proteinIDPointerArray, 0);

                // Shrink proteinMapInfo to the appropriate length
                var oldProteinMapInfo1 = proteinMapInfo;
                proteinMapInfo = new udtProteinIDMapInfoType[proteinMapInfoCount];
                Array.Copy(oldProteinMapInfo1, proteinMapInfo, Math.Min(proteinMapInfoCount, oldProteinMapInfo1.Length));

                return true;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error reading the newly created protein to peptide mapping file (" + Path.GetFileName(proteinToPepMapFilePath) + ")";
                HandleException(mStatusMessage, ex);
            }

            return false;
        }

        protected string PreProcessInspectResultsFile(string inputFilePath,
            string outputDirectoryPath,
            string inspectParamFilePath)
        {
            // Read inspectParamFilePath to extract the mod names
            if (!ExtractModInfoFromInspectParamFile(inspectParamFilePath, ref mInspectModNameList))
            {
                if (mInspectModNameList.Count == 0)
                {
                    mInspectModNameList.Add("phos");
                }
            }

            return PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, ePeptideInputFileFormatConstants.InspectResultsFile);
        }

        protected string PreProcessPSMResultsFile(string inputFilePath,
                                                  string outputDirectoryPath,
                                                  ePeptideInputFileFormatConstants eFileType)
        {
            int terminatorSize;

            var sepChars = new char[] { '\t' };

            int peptideSequenceColIndex;
            int scanColIndex;
            string toolDescription;

            if (eFileType == ePeptideInputFileFormatConstants.InspectResultsFile)
            {
                // Assume inspect results file line terminators are only a single byte (it doesn't matter if the terminators are actually two bytes)
                terminatorSize = 1;

                // The 3rd column in the Inspect results file should have the peptide sequence
                peptideSequenceColIndex = 2;
                scanColIndex = 1;
                toolDescription = "Inspect";
            }
            else if (eFileType == ePeptideInputFileFormatConstants.MSGFPlusResultsFile)
            {
                terminatorSize = 2;
                peptideSequenceColIndex = -1;
                scanColIndex = -1;
                toolDescription = "MS-GF+";
            }
            else
            {
                mStatusMessage = "Unrecognized file type: " + eFileType.ToString() + "; will look for column header 'Peptide'";

                terminatorSize = 2;
                peptideSequenceColIndex = -1;
                scanColIndex = -1;
                toolDescription = "Generic PSM result file";
            }

            try
            {
                if (!File.Exists(inputFilePath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    mStatusMessage = "File not found: " + inputFilePath;

                    ShowErrorMessage(mStatusMessage);
                    return string.Empty;
                }

                ShowMessage("Pre-processing the " + toolDescription + " results file: " + Path.GetFileName(inputFilePath));

                // Initialize the peptide list
                if (mUniquePeptideList == null)
                {
                    mUniquePeptideList = new SortedList<string, SortedSet<int>>();
                }
                else
                {
                    mUniquePeptideList.Clear();
                }

                // Open the PSM results file and construct a unique list of peptides in the file (including any modification symbols)
                // Keep track of PSM counts
                using (var reader = new StreamReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    int currentLine = 1;
                    long bytesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        if (AbortProcessing)
                            break;
                        string lineIn = reader.ReadLine();
                        if (lineIn == null)
                            continue;
                        bytesRead += lineIn.Length + terminatorSize;

                        lineIn = lineIn.TrimEnd();

                        if (currentLine == 1 && (peptideSequenceColIndex < 0 || lineIn.StartsWith("#")))
                        {
                            // Header line
                            if (peptideSequenceColIndex < 0)
                            {
                                // Split the header line to look for the "Peptide" and Scan columns
                                var splitLine = lineIn.Split(sepChars);
                                for (int index = 0; index < splitLine.Length; index++)
                                {
                                    if (peptideSequenceColIndex < 0 && (splitLine[index].ToLower() ?? "") == "peptide")
                                    {
                                        peptideSequenceColIndex = index;
                                    }

                                    if (scanColIndex < 0 && splitLine[index].ToLower().StartsWith("scan"))
                                    {
                                        scanColIndex = index;
                                    }
                                }

                                if (peptideSequenceColIndex < 0)
                                {
                                    SetBaseClassErrorCode(ProcessFilesErrorCodes.LocalizedError);
                                    mStatusMessage = "Peptide column not found; unable to continue";

                                    ShowErrorMessage(mStatusMessage);
                                    return string.Empty;
                                }
                            }
                        }
                        else if (lineIn.Length > 0)
                        {
                            var splitLine = lineIn.Split(sepChars);

                            if (splitLine.Length > peptideSequenceColIndex)
                            {
                                var scanNumber = default(int);
                                if (scanColIndex >= 0)
                                {
                                    if (!int.TryParse(splitLine[scanColIndex], out scanNumber))
                                    {
                                        scanNumber = 0;
                                    }
                                }

                                UpdateUniquePeptideList(splitLine[peptideSequenceColIndex], scanNumber);
                            }
                        }

                        if (currentLine % 1000 == 0)
                        {
                            UpdateProgress(PERCENT_COMPLETE_PREPROCESSING +
                                Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100) * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100);
                        }

                        currentLine += 1;
                    }
                }

                string peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath);
                return peptideListFilePath;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error reading " + toolDescription + " input file in PreProcessPSMResultsFile";
                HandleException(mStatusMessage, ex);
            }

            return string.Empty;
        }

        protected string PreProcessPHRPDataFile(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    mStatusMessage = "File not found: " + inputFilePath;

                    ShowErrorMessage(mStatusMessage);
                    return string.Empty;
                }

                Console.WriteLine();
                ShowMessage("Pre-processing PHRP data file: " + Path.GetFileName(inputFilePath));

                // Initialize the peptide list
                if (mUniquePeptideList == null)
                {
                    mUniquePeptideList = new SortedList<string, SortedSet<int>>();
                }
                else
                {
                    mUniquePeptideList.Clear();
                }

                // Initialize the PHRP startup options
                var startupOptions = new clsPHRPStartupOptions()
                {
                    LoadModsAndSeqInfo = false,
                    LoadMSGFResults = false,
                    LoadScanStatsData = false,
                    MaxProteinsPerPSM = 1
                };

                // Open the PHRP data file and construct a unique list of peptides in the file (including any modification symbols).
                // MSPathFinder synopsis files do not have mod symbols in the peptides.
                // This is OK since the peptides in mUniquePeptideList will have mod symbols removed in PreProcessDataWriteOutPeptides
                // when finding proteins that contain the peptides.
                using (var reader = new clsPHRPReader(inputFilePath, clsPHRPReader.ePeptideHitResultType.Unknown, startupOptions))
                {
                    reader.EchoMessagesToConsole = true;
                    reader.SkipDuplicatePSMs = false;

                    foreach (string errorMessage in reader.ErrorMessages)
                        ShowErrorMessage(errorMessage);

                    foreach (string warningMessage in reader.WarningMessages)
                        ShowMessage("Warning: " + warningMessage);

                    reader.ClearErrors();
                    reader.ClearWarnings();

                    RegisterEvents(reader);

                    while (reader.MoveNext())
                    {
                        if (AbortProcessing)
                            break;
                        UpdateUniquePeptideList(reader.CurrentPSM.Peptide, reader.CurrentPSM.ScanNumber);
                        if (mUniquePeptideList.Count % 1000 == 0)
                        {
                            UpdateProgress(PERCENT_COMPLETE_PREPROCESSING +
                            reader.PercentComplete * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100);
                        }
                    }
                }

                string peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath);
                return peptideListFilePath;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error reading PSM input file in PreProcessPHRPDataFile";
                HandleException(mStatusMessage, ex);
            }

            return string.Empty;
        }

        protected string PreProcessDataWriteOutPeptides(string inputFilePath, string outputDirectoryPath)
        {
            try
            {

                // Now write out the unique list of peptides to peptideListFilePath
                string peptideListFileName = Path.GetFileNameWithoutExtension(inputFilePath) + FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES + ".txt";
                string peptideListFilePath;

                if (!string.IsNullOrEmpty(outputDirectoryPath))
                {
                    peptideListFilePath = Path.Combine(outputDirectoryPath, peptideListFileName);
                }
                else
                {
                    var inputFileInfo = new FileInfo(inputFilePath);

                    peptideListFilePath = Path.Combine(inputFileInfo.DirectoryName, peptideListFileName);
                }

                LogMessage("Creating " + Path.GetFileName(peptideListFileName));

                // Open the output file
                using (var writer = new StreamWriter(new FileStream(peptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    // Write out the peptides, removing any mod symbols that might be present
                    foreach (var peptideEntry in mUniquePeptideList)
                    {
                        string peptide;

                        if (mInspectModNameList.Count > 0)
                        {
                            peptide = RemoveInspectMods(peptideEntry.Key, ref mInspectModNameList);
                        }
                        else
                        {
                            peptide = peptideEntry.Key;
                        }

                        if (peptideEntry.Value.Count == 0)
                        {
                            writer.WriteLine(peptide + "\t" + "0");
                        }
                        else
                        {
                            foreach (var scanNumber in peptideEntry.Value)
                                writer.WriteLine(peptide + "\t" + scanNumber);
                        }
                    }
                }

                return peptideListFilePath;
            }
            catch (Exception ex)
            {
                mStatusMessage = "Error writing the Unique Peptides file in PreProcessDataWriteOutPeptides";
                HandleException(mStatusMessage, ex);
                return string.Empty;
            }
        }

        public override bool ProcessFile(string inputFilePath, string outputDirectoryPath, string parameterFilePath, bool resetErrorCode)
        {
            if (resetErrorCode)
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
            }

            try
            {
                if (inputFilePath == null || inputFilePath.Length == 0)
                {
                    ShowMessage("Input file name is empty");
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                bool success = false;

                // Note that CleanupFilePaths() will update mOutputDirectoryPath, which is used by LogMessage()
                if (!CleanupFilePaths(ref inputFilePath, ref outputDirectoryPath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError);
                }
                else
                {
                    LogMessage("Processing " + Path.GetFileName(inputFilePath));
                    ePeptideInputFileFormatConstants eInputFileFormat;
                    if (PeptideInputFileFormat == ePeptideInputFileFormatConstants.AutoDetermine)
                    {
                        eInputFileFormat = DetermineResultsFileFormat(inputFilePath);
                    }
                    else
                    {
                        eInputFileFormat = PeptideInputFileFormat;
                    }

                    if (eInputFileFormat == ePeptideInputFileFormatConstants.Unknown)
                    {
                        ShowMessage("Input file type not recognized");
                        return false;
                    }

                    UpdateProgress("Preprocessing input file", PERCENT_COMPLETE_PREPROCESSING);
                    mInspectModNameList.Clear();

                    string inputFilePathWork;
                    string outputFileBaseName;

                    switch (eInputFileFormat)
                    {
                        case ePeptideInputFileFormatConstants.InspectResultsFile:
                            // Inspect search results file; need to pre-process it
                            inputFilePathWork = PreProcessInspectResultsFile(inputFilePath, outputDirectoryPath, mInspectParameterFilePath);
                            outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                            mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly;
                            mProteinCoverageSummarizer.PeptideFileSkipFirstLine = false;
                            mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = false;
                            break;

                        case ePeptideInputFileFormatConstants.MSGFPlusResultsFile:
                            // MS-GF+ search results file; need to pre-process it
                            // Make sure RemoveSymbolCharacters is true
                            RemoveSymbolCharacters = true;

                            inputFilePathWork = PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, eInputFileFormat);
                            outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                            mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly;
                            mProteinCoverageSummarizer.PeptideFileSkipFirstLine = false;
                            mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = false;
                            break;

                        case ePeptideInputFileFormatConstants.PHRPFile:
                            // Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to pre-process it
                            // Make sure RemoveSymbolCharacters is true
                            RemoveSymbolCharacters = true;

                            // Open the PHRP data files and construct a unique list of peptides in the file (including any modification symbols)
                            // Write the unique peptide list to _syn_peptides.txt
                            inputFilePathWork = PreProcessPHRPDataFile(inputFilePath, outputDirectoryPath);
                            outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                            mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly;
                            mProteinCoverageSummarizer.PeptideFileSkipFirstLine = false;
                            mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = false;
                            break;

                        default:
                            // Pre-process the file to check for a header line
                            inputFilePathWork = string.Copy(inputFilePath);
                            outputFileBaseName = string.Empty;

                            if (eInputFileFormat == ePeptideInputFileFormatConstants.ProteinAndPeptideFile)
                            {
                                mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence;
                            }
                            else
                            {
                                mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly;
                            }

                            if (IsHeaderLinePresent(inputFilePath, eInputFileFormat))
                            {
                                mProteinCoverageSummarizer.PeptideFileSkipFirstLine = true;
                            }

                            break;
                    }

                    if (string.IsNullOrWhiteSpace(inputFilePathWork))
                    {
                        return false;
                    }

                    UpdateProgress("Running protein coverage summarizer", PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER);

                    string proteinToPepMapFilePath = null;

                    // Call mProteinCoverageSummarizer.ProcessFile to perform the work
                    success = mProteinCoverageSummarizer.ProcessFile(inputFilePathWork, outputDirectoryPath,
                                                                     parameterFilePath, true,
                                                                     out proteinToPepMapFilePath, outputFileBaseName);
                    if (!success)
                    {
                        mStatusMessage = "Error running ProteinCoverageSummarizer: " + mProteinCoverageSummarizer.ErrorMessage;
                    }

                    if (success && proteinToPepMapFilePath.Length > 0)
                    {
                        UpdateProgress("Postprocessing", PERCENT_COMPLETE_POSTPROCESSING);

                        switch (eInputFileFormat)
                        {
                            case ePeptideInputFileFormatConstants.PeptideListFile:
                            case ePeptideInputFileFormatConstants.ProteinAndPeptideFile:
                                // No post-processing is required
                                break;

                            default:
                                // Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to post-process the results file
                                success = PostProcessPSMResultsFile(inputFilePathWork, proteinToPepMapFilePath, DeleteTempFiles);
                                break;
                        }
                    }

                    if (success)
                    {
                        LogMessage("Processing successful");
                        OperationComplete();
                    }
                    else
                    {
                        LogMessage("Processing not successful");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                HandleException("Error in ProcessFile", ex);
                return false;
            }
        }

        protected string RemoveInspectMods(string peptide, ref List<string> inspectModNames)
        {
            string prefix = string.Empty;
            string suffix = string.Empty;

            if (peptide.Length >= 4)
            {
                if (peptide[1] == '.' &&
                    peptide[peptide.Length - 2] == '.')
                {
                    prefix = peptide.Substring(0, 2);
                    suffix = peptide.Substring(peptide.Length - 2, 2);

                    peptide = peptide.Substring(2, peptide.Length - 4);
                }
            }

            foreach (string modName in inspectModNames)
                peptide = peptide.Replace(modName, string.Empty);

            return prefix + peptide + suffix;
        }

        /// <summary>
        /// Add peptideSequence to mUniquePeptideList if not defined, including tracking the scanNumber
        /// Otherwise, update the scan list for the peptide
        /// </summary>
        /// <param name="peptideSequence"></param>
        /// <param name="scanNumber"></param>
        private void UpdateUniquePeptideList(string peptideSequence, int scanNumber)
        {
            SortedSet<int> scanList = null;
            if (mUniquePeptideList.TryGetValue(peptideSequence, out scanList))
            {
                if (!scanList.Contains(scanNumber))
                {
                    scanList.Add(scanNumber);
                }
            }
            else
            {
                scanList = new SortedSet<int>() { scanNumber };
                mUniquePeptideList.Add(peptideSequence, scanList);
            }
        }

        #region "Protein Coverage Summarizer Event Handlers"
        private void ProteinCoverageSummarizer_ProgressChanged(string taskDescription, float percentComplete)
        {
            float percentCompleteEffective =
                PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER +
                percentComplete * Convert.ToSingle((PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100.0);

            UpdateProgress(taskDescription, percentCompleteEffective);
        }

        private void ProteinCoverageSummarizer_ProgressReset()
        {
            ResetProgress(mProteinCoverageSummarizer.ProgressStepDescription);
        }
        #endregion

        #region "IComparer Classes"
        protected class ProteinIDMapInfoComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var xData = (udtProteinIDMapInfoType)x;
                var yData = (udtProteinIDMapInfoType)y;

                var pepCompare = xData.Peptide.CompareTo(yData.Peptide);
                if (pepCompare != 0)
                {
                    return pepCompare;
                }

                return xData.ProteinID.CompareTo(yData.ProteinID);
            }
        }

        protected class ProteinIDMapInfoPeptideSearchComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var xData = (udtProteinIDMapInfoType)x;
                string peptide = Convert.ToString(y);

                return xData.Peptide.CompareTo(peptide);
            }
        }

        protected class PepToProteinMappingComparer : IComparer<udtPepToProteinMappingType>
        {
            public int Compare(udtPepToProteinMappingType x, udtPepToProteinMappingType y)
            {
                var pepCompare = x.Peptide.CompareTo(y.Peptide);
                if (pepCompare != 0)
                {
                    return pepCompare;
                }

                return x.Protein.CompareTo(y.Protein);
            }
        }

        #endregion
    }
}