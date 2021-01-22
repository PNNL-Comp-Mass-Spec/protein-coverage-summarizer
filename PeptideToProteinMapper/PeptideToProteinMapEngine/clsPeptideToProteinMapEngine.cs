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
using PHRPReader;
using PRISM;
using ProteinCoverageSummarizer;

namespace PeptideToProteinMapEngine
{
    /// <summary>
    /// This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
    /// an accompanying file with peptide sequences to find the proteins that contain each peptide
    /// It will also optionally compute the percent coverage of each of the proteins
    /// </summary>
    public class clsPeptideToProteinMapEngine : PRISM.FileProcessor.ProcessFilesBase
    {
        // Ignore Spelling: phos, struct

        #region "Constants and Enums"

        /// <summary>
        /// Inspect results filename suffix
        /// </summary>
        public const string FILENAME_SUFFIX_INSPECT_RESULTS_FILE = "_inspect.txt";

        /// <summary>
        /// MS-GF DB  results filename suffix
        /// </summary>
        public const string FILENAME_SUFFIX_MSGFDB_RESULTS_FILE = "_msgfdb.txt";

        /// <summary>
        /// MS-GF+ results filename suffix
        /// </summary>
        public const string FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE = "_msgfplus.txt";

        /// <summary>
        /// PHRP peptide to protein map file suffix
        /// </summary>
        public const string FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING = "_PepToProtMap.txt";

        /// <summary>
        /// Unique peptides filename suffix
        /// </summary>
        private const string FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES = "_peptides";

        /// <summary>
        /// Initial % complete value displayed at the start of pre-processing
        /// </summary>
        private const float PERCENT_COMPLETE_PREPROCESSING = 0;

        /// <summary>
        /// Initial % complete value displayed when the coverage computation starts
        /// </summary>
        private const float PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER = 5;

        /// <summary>
        /// Initial % complete value displayed at the start of post-processing
        /// </summary>
        private const float PERCENT_COMPLETE_POSTPROCESSING = 95;

        /// <summary>
        /// Peptide input file formats
        /// </summary>
        public enum PeptideInputFileFormatConstants
        {
            /// <summary>
            /// Unknown file type
            /// </summary>
            Unknown = -1,
            /// <summary>
            /// Auto-determine the file type
            /// </summary>
            AutoDetermine = 0,
            /// <summary>
            ///  First column is peptide sequence
            /// </summary>
            PeptideListFile = 1,
            /// <summary>
            /// First column is protein name, second column is peptide sequence
            /// </summary>
            ProteinAndPeptideFile = 2,
            /// <summary>
            /// Inspect results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
            /// </summary>
            InspectResultsFile = 3,
            /// <summary>
            /// MS-GF+ results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
            /// </summary>
            MSGFPlusResultsFile = 4,
            /// <summary>
            /// SEQUEST, Inspect, X!Tandem, or MS-GF+ synopsis or first-hits file created by Peptide Hit Results Processor (PHRP); pre-processes the file to determine the peptides present, then determine the proteins that contain the given peptides
            /// </summary>
            PHRPFile = 5,
            /// <summary>
            /// Generic tab-delimited text file; will look for column names that start with Peptide, Protein, and Scan
            /// </summary>
            TabDelimitedText = 6
        }

        #endregion

        #region "Structures"

        private struct udtProteinIDMapInfoType
        {
            public int ProteinID;
            public string Peptide;
            public int ResidueStart;
            public int ResidueEnd;

            /// <summary>
            /// Show the peptide sequence
            /// </summary>
            public override string ToString()
            {
                return Peptide + ", Protein ID " + ProteinID;
            }
        }

        private struct udtPepToProteinMappingType
        {
            public string Peptide;
            public string Protein;
            public int ResidueStart;
            public int ResidueEnd;

            /// <summary>
            /// Show the peptide sequence
            /// </summary>
            public override string ToString()
            {
                return Peptide + ", Protein " + Protein;
            }
        }
        #endregion

        #region "Class wide variables"

        private bool mOptionsShown;

        private clsProteinCoverageSummarizer mProteinCoverageSummarizer;

        // When processing an inspect search result file, if you provide the inspect parameter file name,
        // then this program will read the parameter file and look for the "mod," lines.  The user-assigned mod
        // names will be extracted and used when "cleaning" the peptides prior to looking for matching proteins
        private string mInspectParameterFilePath;

        // The following is used when the input file is SEQUEST, X!Tandem, Inspect, or MS-GF+ results file
        // Keys are peptide sequences; values are Lists of scan numbers that each peptide was observed in
        // Keys may have mod symbols in them; those symbols will be removed in PreProcessDataWriteOutPeptides
        private SortedList<string, SortedSet<int>> mUniquePeptideList;

        // Mod names must be lower case, and 4 characters in length (or shorter)
        // Only used with Inspect since mods in MS-GF+ are simply numbers, e.g. R.DNFM+15.995SATQAVEYGLVDAVM+15.995TK.R
        // while mods in SEQUEST and XTandem are symbols (*, #, @)
        private List<string> mInspectModNameList;

        #endregion

        #region "Properties"

        /// <summary>
        /// When true, delete temp files
        /// </summary>
        public bool DeleteTempFiles { get; set; }

        /// <summary>
        /// Inspect parameter file path
        /// </summary>
        public string InspectParameterFilePath
        {
            get => mInspectParameterFilePath;
            set => mInspectParameterFilePath = value ?? string.Empty;
        }

        /// <summary>
        /// Peptide to protein map engine options
        /// </summary>
        public ProteinCoverageSummarizerOptions Options { get; }

        /// <summary>
        /// Peptide input file format
        /// </summary>
        public PeptideInputFileFormatConstants PeptideInputFileFormat { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage { get; private set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        public clsPeptideToProteinMapEngine(ProteinCoverageSummarizerOptions options)
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
        /// Auto-determine the file format
        /// </summary>
        /// <param name="filePath"></param>
        public PeptideInputFileFormatConstants DetermineResultsFileFormat(string filePath)
        {
            Console.WriteLine();

            // First examine the file name to determine the file format

            if (Path.GetFileName(filePath).EndsWith(FILENAME_SUFFIX_INSPECT_RESULTS_FILE, StringComparison.OrdinalIgnoreCase))
            {
                OnStatusEvent(string.Format("Input file type: Inspect results file (matched suffix {0})", FILENAME_SUFFIX_INSPECT_RESULTS_FILE));
                return PeptideInputFileFormatConstants.InspectResultsFile;
            }

            if (Path.GetFileName(filePath).EndsWith(FILENAME_SUFFIX_MSGFDB_RESULTS_FILE, StringComparison.OrdinalIgnoreCase))
            {
                OnStatusEvent(string.Format("Input file type: MS-GF+ results file (matched suffix {0})", FILENAME_SUFFIX_MSGFDB_RESULTS_FILE));
                return PeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }

            if (Path.GetFileName(filePath).EndsWith(FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE, StringComparison.OrdinalIgnoreCase))
            {
                OnStatusEvent(string.Format("Input file type: MS-GF+ results file (matched suffix {0})", FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE));
                return PeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }

            if (PeptideInputFileFormat != PeptideInputFileFormatConstants.AutoDetermine && PeptideInputFileFormat != PeptideInputFileFormatConstants.Unknown)
            {
                return PeptideInputFileFormat;
            }

            var baseNameLCase = Path.GetFileNameWithoutExtension(filePath);
            if (baseNameLCase.EndsWith("_MSGFDB", StringComparison.OrdinalIgnoreCase))
            {
                OnStatusEvent("Input file type: MS-GF+ results file (matched suffix _MSGFDB)");
                return PeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }

            if (baseNameLCase.EndsWith("_MSGFPlus", StringComparison.OrdinalIgnoreCase))
            {
                OnStatusEvent("Input file type: MS-GF+ results file (matched suffix _MSGFPlus)");
                return PeptideInputFileFormatConstants.MSGFPlusResultsFile;
            }

            var resultType = clsPHRPReader.AutoDetermineResultType(filePath);
            if (resultType != clsPHRPReader.PeptideHitResultTypes.Unknown)
            {
                OnStatusEvent("Input file type: PHRPFile (based on column names)");
                return PeptideInputFileFormatConstants.PHRPFile;
            }

            // Could not determine the type from the filename; look for "Peptide" or "Sequence" in the header line
            IsHeaderLinePresent(filePath, PeptideInputFileFormatConstants.Unknown, out var hasPeptideOrSequenceColumn);
            if (hasPeptideOrSequenceColumn)
            {
                OnStatusEvent("Input file type: Tab-delimited text (has column Peptide or Sequence))");
                PeptideInputFileFormat = PeptideInputFileFormatConstants.TabDelimitedText;
                return PeptideInputFileFormat;
            }

            ShowMessage("Unable to determine the format of the input file based on the filename suffix or column names; will assume the first column contains peptide sequence");
            return PeptideInputFileFormatConstants.PeptideListFile;
        }

        /// <summary>
        /// Parse an Inspect parameter file to determine the modification names
        /// </summary>
        /// <param name="inspectParamFilePath"></param>
        /// <param name="inspectModNames"></param>
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

                if (string.IsNullOrEmpty(inspectParamFilePath))
                {
                    return false;
                }

                ShowMessage("Looking for mod definitions in the Inspect param file: " + Path.GetFileName(inspectParamFilePath));

                // Read the contents of inspectParamFilePath
                using (var reader = new StreamReader(new FileStream(inspectParamFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    while (!reader.EndOfStream)
                    {
                        var lineIn = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(lineIn))
                            continue;

                        var dataLine = lineIn.Trim();

                        if (dataLine[0] == '#')
                        {
                            // Comment line; skip it
                            continue;
                        }

                        if (!dataLine.StartsWith("mod", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Modification definition line

                        // Split the line on commas
                        var splitLine = dataLine.Split(',');

                        if (splitLine.Length < 5 || splitLine[0].ToLower().Trim() != "mod")
                            continue;

                        var modName = splitLine[4].ToLower();

                        if (modName.Length > 4)
                        {
                            // Only keep the first 4 characters of the modification name
                            modName = modName.Substring(0, 4);
                        }

                        inspectModNames.Add(modName);
                        ShowMessage("Found modification: " + dataLine + "   -->   Mod Symbol \"" + modName + "\"");
                    }
                }

                Console.WriteLine();

                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error reading the Inspect parameter file (" + Path.GetFileName(inspectParamFilePath) + ")";
                HandleException(StatusMessage, ex);
            }

            return false;
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
            PeptideInputFileFormat = PeptideInputFileFormatConstants.AutoDetermine;
            DeleteTempFiles = true;

            mInspectParameterFilePath = string.Empty;

            AbortProcessing = false;
            StatusMessage = string.Empty;

            mProteinCoverageSummarizer = new clsProteinCoverageSummarizer(Options);
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
        /// <param name="filePath">File path</param>
        /// <param name="inputFileFormat">Input file format</param>
        /// <param name="hasPeptideOrSequenceColumn">
        /// Output: true if the file has a column named Peptide or Sequence
        /// Will be set to True if inputFileFormat is ProteinAndPeptideFile or PeptideListFile</param>
        /// <returns>True if the file has a header line</returns>
        private bool IsHeaderLinePresent(string filePath, PeptideInputFileFormatConstants inputFileFormat, out bool hasPeptideOrSequenceColumn)
        {
            var sepChars = new[] { '\t' };
            hasPeptideOrSequenceColumn = false;

            try
            {
                // Read the contents of filePath
                using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    if (reader.EndOfStream)
                        return false;

                    var dataLine = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(dataLine))
                        return false;

                    var columnNames = dataLine.Split(sepChars);

                    if (inputFileFormat == PeptideInputFileFormatConstants.ProteinAndPeptideFile)
                    {
                        hasPeptideOrSequenceColumn = true;
                        if (columnNames.Length > 1 && (
                            columnNames[1].StartsWith("peptide", StringComparison.OrdinalIgnoreCase) ||
                            columnNames[1].StartsWith("sequence", StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }
                    else if (inputFileFormat == PeptideInputFileFormatConstants.PeptideListFile)
                    {
                        hasPeptideOrSequenceColumn = true;
                        if (columnNames[0].StartsWith("peptide", StringComparison.OrdinalIgnoreCase) ||
                            columnNames[0].StartsWith("sequence", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else if (columnNames.Any(dataColumn => dataColumn.StartsWith("peptide", StringComparison.OrdinalIgnoreCase)) ||
                             columnNames.Any(dataColumn => dataColumn.StartsWith("sequence", StringComparison.OrdinalIgnoreCase)))
                    {
                        hasPeptideOrSequenceColumn = true;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error looking for a header line in " + Path.GetFileName(filePath);
                HandleException(StatusMessage, ex);
                return false;
            }
        }

        /// <summary>
        /// Load settings from an XML-based parameter file
        /// </summary>
        /// <param name="parameterFilePath"></param>
        // ReSharper disable once UnusedMember.Global
        public bool LoadParameterFileSettings(string parameterFilePath)
        {
            return mProteinCoverageSummarizer.LoadParameterFileSettings(parameterFilePath);
        }

        /// <summary>
        /// Post-process a PSM results file
        /// </summary>
        /// <param name="peptideListFilePath"></param>
        /// <param name="proteinToPepMapFilePath"></param>
        /// <param name="deleteWorkingFiles"></param>
        private bool PostProcessPSMResultsFile(string peptideListFilePath,
                                               string proteinToPepMapFilePath,
                                               bool deleteWorkingFiles)
        {
            const string UNKNOWN_PROTEIN_NAME = "__NoMatch__";

            try
            {
                Console.WriteLine();

                ShowMessage("Post-processing the results files");

                if (mUniquePeptideList == null || mUniquePeptideList.Count == 0)
                {
                    StatusMessage = "Error in PostProcessPSMResultsFile: mUniquePeptideList is empty; this is unexpected; unable to continue";

                    HandleException(StatusMessage, new Exception("Empty Array"));

                    return false;
                }

                var success = PostProcessPSMResultsFileReadMapFile(proteinToPepMapFilePath, out var proteins, out var proteinIDPointerArray, out var proteinMapInfo);
                if (!success)
                    return false;

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
                        // Construct the clean sequence for this peptide
                        var cleanSequence = clsProteinCoverageSummarizer.GetCleanPeptideSequence(
                            peptideEntry.Key,
                            out _,
                            out _,
                            mProteinCoverageSummarizer.Options.RemoveSymbolCharacters);

                        if (mInspectModNameList.Count > 0)
                        {
                            cleanSequence = RemoveInspectMods(cleanSequence, ref mInspectModNameList);
                        }

                        // Look for cleanSequence in proteinMapInfo
                        var matchIndex = Array.BinarySearch(proteinMapInfo, cleanSequence, proteinMapPeptideComparer);
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
                                matchIndex--;

                            // Now write out each of the proteins for this peptide
                            // We're caching results to cachedData so that we can sort by protein name
                            cachedData.Clear();

                            do
                            {
                                // Find the Protein for ID proteinMapInfo(matchIndex).ProteinID
                                var proteinIDMatchIndex = Array.BinarySearch(proteinIDPointerArray, proteinMapInfo[matchIndex].ProteinID);
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
                                catch (Exception)
                                {
                                    // This code shouldn't be reached
                                    // Ignore errors occur
                                }

                                var cachedDataEntry = new udtPepToProteinMappingType()
                                {
                                    Peptide = string.Copy(peptideEntry.Key),
                                    Protein = string.Copy(protein ?? string.Empty),
                                    ResidueStart = proteinMapInfo[matchIndex].ResidueStart,
                                    ResidueEnd = proteinMapInfo[matchIndex].ResidueEnd
                                };

                                cachedData.Add(cachedDataEntry);

                                matchIndex++;
                            }
                            while (matchIndex < proteinMapInfo.Length && proteinMapInfo[matchIndex].Peptide == cleanSequence);
                            if (cachedData.Count > 1)
                            {
                                cachedData.Sort(cachedDataComparer);
                            }

                            foreach (var data in cachedData)
                            {
                                writer.WriteLine(data.Peptide + "\t" +
                                                 data.Protein + "\t" +
                                                 data.ResidueStart + "\t" +
                                                 data.ResidueEnd);
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
                    catch (Exception)
                    {
                        // Ignore errors here
                    }

                    try
                    {
                        LogMessage("Deleting " + Path.GetFileName(proteinToPepMapFilePath));
                        File.Delete(proteinToPepMapFilePath);
                    }
                    catch (Exception)
                    {
                        // Ignore errors here
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error writing the Inspect or MS-GF+ peptide to protein map file in PostProcessPSMResultsFile";
                HandleException(StatusMessage, ex);
            }

            return false;
        }

        private bool PostProcessPSMResultsFileReadMapFile(
            string proteinToPepMapFilePath,
            out string[] proteins,
            out int[] proteinIDPointerArray,
            out udtProteinIDMapInfoType[] proteinMapInfo)
        {
            const int terminatorSize = 2;

            bool success;

            var proteinNames = new List<string>();
            var proteinIDPointers = new List<int>();
            var proteinMapping = new List<udtProteinIDMapInfoType>();

            try
            {
                // Initialize the protein list dictionary
                var proteinList = new Dictionary<string, int>();

                // Initialize the protein to peptide mapping array
                // We know the length will be at least as long as mUniquePeptideList, and easily twice that length

                LogMessage("Reading " + Path.GetFileName(proteinToPepMapFilePath));

                // Read the contents of proteinToPepMapFilePath
                using (var reader = new StreamReader(new FileStream(proteinToPepMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var currentProtein = string.Empty;

                    var linesRead = 0;
                    long bytesRead = 0;

                    var currentProteinID = 0;

                    while (!reader.EndOfStream)
                    {
                        if (AbortProcessing)
                            break;

                        var lineIn = reader.ReadLine();

                        if (string.IsNullOrEmpty(lineIn))
                            continue;

                        bytesRead += lineIn.Length + terminatorSize;

                        var dataLine = lineIn.TrimEnd();

                        linesRead++;
                        if (linesRead == 1)
                        {
                            // Header line; skip it
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(dataLine))
                        {
                            continue;
                        }

                        // Split the line
                        var splitLine = dataLine.Split('\t');

                        if (splitLine.Length < 4)
                        {
                            continue;
                        }

                        if (currentProtein.Length == 0 || currentProtein != splitLine[0])
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

                        var cleanSequence = clsProteinCoverageSummarizer.GetCleanPeptideSequence(
                            splitLine[1],
                            out _,
                            out _,
                            mProteinCoverageSummarizer.Options.RemoveSymbolCharacters);

                        var proteinInfo = new udtProteinIDMapInfoType
                        {
                            ProteinID = currentProteinID,
                            Peptide = cleanSequence,
                            ResidueStart = int.Parse(splitLine[2]),
                            ResidueEnd = int.Parse(splitLine[3])
                        };

                        // Update the values in the array. The other option would be to index the array for each of the assignments above.
                        proteinMapping.Add(proteinInfo);

                        if (linesRead % 1000 == 0)
                        {
                            UpdateProgress(PERCENT_COMPLETE_POSTPROCESSING +
                                Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100) * (PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100);
                        }
                    }
                }

                // Populate Lists proteinNames and proteinIDPointers using proteinList
                foreach (var item in proteinList)
                {
                    proteinNames.Add(item.Key);
                    proteinIDPointers.Add(item.Value);
                }

                success = true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error reading the newly created protein to peptide mapping file (" + Path.GetFileName(proteinToPepMapFilePath) + ")";
                HandleException(StatusMessage, ex);
                success = false;
            }

            proteins = proteinNames.ToArray();
            proteinIDPointerArray = proteinIDPointers.ToArray();
            proteinMapInfo = proteinMapping.ToArray();

            return success;
        }

        private string PreProcessInspectResultsFile(string inputFilePath,
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

            return PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, PeptideInputFileFormatConstants.InspectResultsFile);
        }

        private string PreProcessPSMResultsFile(string inputFilePath,
                                                string outputDirectoryPath,
                                                PeptideInputFileFormatConstants inputFileFormat)
        {
            int terminatorSize;

            var sepChars = new[] { '\t' };

            int peptideSequenceColIndex;
            int scanColIndex;
            string toolDescription;

            if (inputFileFormat == PeptideInputFileFormatConstants.InspectResultsFile)
            {
                // Assume inspect results file line terminators are only a single byte (it doesn't matter if the terminators are actually two bytes)
                terminatorSize = 1;

                // The 3rd column in the Inspect results file should have the peptide sequence
                peptideSequenceColIndex = 2;
                scanColIndex = 1;
                toolDescription = "Inspect results";
            }
            else if (inputFileFormat == PeptideInputFileFormatConstants.MSGFPlusResultsFile)
            {
                terminatorSize = 2;
                peptideSequenceColIndex = -1;
                scanColIndex = -1;
                toolDescription = "MS-GF+ results";
            }
            else if (inputFileFormat == PeptideInputFileFormatConstants.TabDelimitedText)
            {
                terminatorSize = 2;
                peptideSequenceColIndex = -1;
                scanColIndex = -1;
                toolDescription = "Tab-delimited text";
            }
            else
            {
                StatusMessage = "Unrecognized file type code: " + inputFileFormat + "; will look for column header 'Peptide'";

                terminatorSize = 2;
                peptideSequenceColIndex = -1;
                scanColIndex = -1;
                toolDescription = "Generic PSM result";
            }

            try
            {
                if (!File.Exists(inputFilePath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    StatusMessage = "File not found (PreProcessPSMResultsFile): " + inputFilePath;

                    ShowErrorMessage(StatusMessage);
                    return string.Empty;
                }

                ShowMessage("Pre-processing the " + toolDescription + " file: " + Path.GetFileName(inputFilePath));

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
                    var linesRead = 0;
                    long bytesRead = 0;

                    while (!reader.EndOfStream)
                    {
                        if (AbortProcessing)
                            break;

                        var lineIn = reader.ReadLine();

                        if (string.IsNullOrEmpty(lineIn))
                            continue;

                        bytesRead += lineIn.Length + terminatorSize;

                        var dataLine = lineIn.TrimEnd();

                        linesRead++;
                        if (linesRead == 1 && (peptideSequenceColIndex < 0 || dataLine.StartsWith("#")))
                        {
                            // Header line
                            if (peptideSequenceColIndex < 0)
                            {
                                // Split the header line to look for the "Peptide" and Scan columns
                                var columnNames = dataLine.Split(sepChars);

                                peptideSequenceColIndex = clsProteinCoverageSummarizer.FindColumnIndex(columnNames, "peptide");
                                if (peptideSequenceColIndex < 0)
                                {
                                    peptideSequenceColIndex = clsProteinCoverageSummarizer.FindColumnIndex(columnNames, "sequence");
                                }

                                scanColIndex = clsProteinCoverageSummarizer.FindColumnIndex(columnNames, "scan");

                                if (peptideSequenceColIndex < 0)
                                {
                                    SetBaseClassErrorCode(ProcessFilesErrorCodes.LocalizedError);
                                    StatusMessage = "Peptide column not found; unable to continue";

                                    ShowErrorMessage(StatusMessage);
                                    return string.Empty;
                                }
                            }

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(dataLine))
                        {
                            continue;
                        }

                        var splitLine = dataLine.Split(sepChars);

                        if (splitLine.Length > peptideSequenceColIndex)
                        {
                            int scanNumber;
                            if (scanColIndex >= 0 && int.TryParse(splitLine[scanColIndex], out var parsedScanNumber))
                            {
                                scanNumber = parsedScanNumber;
                            }
                            else
                            {
                                scanNumber = 0;
                            }

                            UpdateUniquePeptideList(splitLine[peptideSequenceColIndex], scanNumber);
                        }

                        if (linesRead % 1000 == 0)
                        {
                            UpdateProgress(PERCENT_COMPLETE_PREPROCESSING +
                                Convert.ToSingle(bytesRead / (double)reader.BaseStream.Length * 100) * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100);
                        }
                    }
                }

                var peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath);
                return peptideListFilePath;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error reading the " + toolDescription + " file in PreProcessPSMResultsFile";
                HandleException(StatusMessage, ex);
            }

            return string.Empty;
        }

        private string PreProcessPHRPDataFile(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    StatusMessage = "File not found (PreProcessPHRPDataFile): " + inputFilePath;

                    ShowErrorMessage(StatusMessage);
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
                using (var reader = new clsPHRPReader(inputFilePath, clsPHRPReader.PeptideHitResultTypes.Unknown, startupOptions))
                {
                    reader.EchoMessagesToConsole = true;
                    reader.SkipDuplicatePSMs = false;

                    foreach (var errorMessage in reader.ErrorMessages)
                        ShowErrorMessage(errorMessage);

                    foreach (var warningMessage in reader.WarningMessages)
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

                var peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath);
                return peptideListFilePath;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error reading PSM input file in PreProcessPHRPDataFile";
                HandleException(StatusMessage, ex);
            }

            return string.Empty;
        }

        private string PreProcessDataWriteOutPeptides(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                // Now write out the unique list of peptides to peptideListFilePath
                // Example output filename: QC_19_01_a_12Oct20_msgfplus_peptides.txt
                var peptideListFileName = Path.GetFileNameWithoutExtension(inputFilePath) + FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES + ".txt";
                string peptideListFilePath;

                if (!string.IsNullOrEmpty(outputDirectoryPath))
                {
                    peptideListFilePath = Path.Combine(outputDirectoryPath, peptideListFileName);
                }
                else
                {
                    var inputFileInfo = new FileInfo(inputFilePath);

                    peptideListFilePath = Path.Combine(inputFileInfo.DirectoryName ?? string.Empty, peptideListFileName);
                }

                LogMessage("Creating " + Path.GetFileName(peptideListFileName));

                // Open the output file
                using (var writer = new StreamWriter(new FileStream(peptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    writer.WriteLine("Peptide\tScan");

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
                StatusMessage = "Error writing the Unique Peptides file in PreProcessDataWriteOutPeptides";
                HandleException(StatusMessage, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Process the input file to map peptides to proteins in the FASTA file
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="parameterFilePath"></param>
        /// <param name="resetErrorCode"></param>
        /// <returns>True if success, false if an error</returns>
        public override bool ProcessFile(string inputFilePath, string outputDirectoryPath, string parameterFilePath, bool resetErrorCode)
        {
            if (resetErrorCode)
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
            }

            try
            {
                if (string.IsNullOrEmpty(inputFilePath))
                {
                    ShowMessage("Input file name is empty");
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                // Note that CleanupFilePaths() will update mOutputDirectoryPath, which is used by LogMessage()
                if (!CleanupFilePaths(ref inputFilePath, ref outputDirectoryPath))
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError);
                    return false;
                }

                if (!mOptionsShown)
                {
                    mOptionsShown = true;
                    Console.WriteLine("Processing Options");
                    Console.WriteLine();
                    Console.WriteLine("{0,-26} {1}", "Input File:", PRISM.PathUtils.CompactPathString(inputFilePath, 80));
                    Console.WriteLine("{0,-26} {1}", "Output Directory:", PRISM.PathUtils.CompactPathString(outputDirectoryPath, 80));
                    Console.WriteLine("{0,-26} {1}", "Proteins File:", PRISM.PathUtils.CompactPathString(Options.ProteinInputFilePath, 80));

                    if (!string.IsNullOrWhiteSpace(parameterFilePath))
                    {
                        Console.WriteLine("{0,-26} {1}", "Parameter File:", parameterFilePath);
                    }

                    Console.WriteLine("{0,-26} {1} (type {2})", "Input File Format:", PeptideInputFileFormat, (int)PeptideInputFileFormat);

                    if (!string.IsNullOrWhiteSpace(mInspectParameterFilePath))
                    {
                        Console.WriteLine("{0,-26} {1}", "Inspect Param File:", mInspectParameterFilePath);
                    }

                    Console.WriteLine("{0,-26} {1}", "Ignore I/L Differences:", Options.IgnoreILDifferences);
                    Console.WriteLine("{0,-26} {1}", "Match Prefix and Suffix Residues:", Options.MatchPeptidePrefixAndSuffixToProtein);
                    Console.WriteLine("{0,-26} {1}", "Output Protein Sequence:", Options.OutputProteinSequence);
                    Console.WriteLine("{0,-26} {1}", "Skip Coverage Computation:", Options.SearchAllProteinsSkipCoverageComputationSteps);
                    Console.WriteLine("{0,-26} {1}", "Create _AllProteins.txt:", Options.SaveSourceDataPlusProteinsFile);

                    if (Options.KeepDB)
                    {
                        Console.WriteLine("{0,-26} {1}", "Delete SQLite database:", "False");
                    }

                    Console.WriteLine();
                }

                LogMessage("Processing " + Path.GetFileName(inputFilePath));
                PeptideInputFileFormatConstants inputFileFormat;
                if (PeptideInputFileFormat == PeptideInputFileFormatConstants.AutoDetermine)
                {
                    inputFileFormat = DetermineResultsFileFormat(inputFilePath);
                    Console.WriteLine();
                }
                else
                {
                    inputFileFormat = PeptideInputFileFormat;
                }

                if (inputFileFormat == PeptideInputFileFormatConstants.Unknown)
                {
                    ShowMessage("Input file type not recognized");
                    return false;
                }

                UpdateProgress("Preprocessing input file", PERCENT_COMPLETE_PREPROCESSING);
                mInspectModNameList.Clear();

                string inputFilePathWork;
                string outputFileBaseName;

                switch (inputFileFormat)
                {
                    case PeptideInputFileFormatConstants.InspectResultsFile:
                        // Inspect search results file; need to pre-process it
                        inputFilePathWork = PreProcessInspectResultsFile(inputFilePath, outputDirectoryPath, mInspectParameterFilePath);
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                        mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly;
                        mProteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = true;
                        mProteinCoverageSummarizer.Options.MatchPeptidePrefixAndSuffixToProtein = false;
                        break;

                    case PeptideInputFileFormatConstants.MSGFPlusResultsFile:
                        // MS-GF+ search results file; need to pre-process it
                        // Make sure RemoveSymbolCharacters is true
                        Options.RemoveSymbolCharacters = true;

                        inputFilePathWork = PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, inputFileFormat);
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                        mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly;
                        mProteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = true;
                        mProteinCoverageSummarizer.Options.MatchPeptidePrefixAndSuffixToProtein = false;
                        break;

                    case PeptideInputFileFormatConstants.PHRPFile:
                        // SEQUEST, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to pre-process it
                        // Make sure RemoveSymbolCharacters is true
                        Options.RemoveSymbolCharacters = true;

                        // Open the PHRP data files and construct a unique list of peptides in the file (including any modification symbols)
                        // Write the unique peptide list to _syn_peptides.txt
                        inputFilePathWork = PreProcessPHRPDataFile(inputFilePath, outputDirectoryPath);
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath);

                        mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly;
                        mProteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = true;
                        mProteinCoverageSummarizer.Options.MatchPeptidePrefixAndSuffixToProtein = false;
                        break;

                    case PeptideInputFileFormatConstants.TabDelimitedText:
                        Options.RemoveSymbolCharacters = true;
                        mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames;
                        mProteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = true;

                        inputFilePathWork = string.Copy(inputFilePath);
                        outputFileBaseName = string.Empty;
                        break;

                    default:
                        // Pre-process the file to check for a header line
                        inputFilePathWork = string.Copy(inputFilePath);
                        outputFileBaseName = string.Empty;

                        if (inputFileFormat == PeptideInputFileFormatConstants.ProteinAndPeptideFile)
                        {
                            mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.ProteinName_PeptideSequence;
                        }
                        else
                        {
                            mProteinCoverageSummarizer.Options.PeptideFileFormatCode = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly;
                        }

                        if (IsHeaderLinePresent(inputFilePath, inputFileFormat, out _))
                        {
                            mProteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = true;
                        }

                        break;
                }

                if (string.IsNullOrWhiteSpace(inputFilePathWork))
                {
                    return false;
                }

                UpdateProgress("Running protein coverage summarizer", PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER);

                // Call mProteinCoverageSummarizer.ProcessFile to perform the work
                var coverageSuccess = mProteinCoverageSummarizer.ProcessFile(
                    inputFilePathWork, outputDirectoryPath, parameterFilePath, true, out var proteinToPepMapFilePath, outputFileBaseName);

                if (!coverageSuccess)
                {
                    StatusMessage = "Error running ProteinCoverageSummarizer: " + mProteinCoverageSummarizer.ErrorMessage;
                    return false;
                }

                bool success;

                if (proteinToPepMapFilePath.Length > 0)
                {
                    UpdateProgress("Post-processing", PERCENT_COMPLETE_POSTPROCESSING);

                    switch (inputFileFormat)
                    {
                        case PeptideInputFileFormatConstants.PeptideListFile:
                        case PeptideInputFileFormatConstants.ProteinAndPeptideFile:
                            // No post-processing is required
                            success = true;
                            break;

                        default:
                            // SEQUEST, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to post-process the results file
                            success = PostProcessPSMResultsFile(inputFilePathWork, proteinToPepMapFilePath, DeleteTempFiles);
                            break;
                    }
                }
                else
                {
                    success = true;
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

                return success;
            }
            catch (Exception ex)
            {
                HandleException("Error in ProcessFile", ex);
                OnDebugEvent(StackTraceFormatter.GetExceptionStackTraceMultiLine(ex));
                return false;
            }
        }

        private string RemoveInspectMods(string peptide, ref List<string> inspectModNames)
        {
            var prefix = string.Empty;
            var suffix = string.Empty;

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

            foreach (var modName in inspectModNames)
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
            if (mUniquePeptideList.TryGetValue(peptideSequence, out var scanList))
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
            var percentCompleteEffective =
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

        private class ProteinIDMapInfoComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == null || y == null)
                    return 0;

                var xData = (udtProteinIDMapInfoType)x;
                var yData = (udtProteinIDMapInfoType)y;

                var pepCompare = string.CompareOrdinal(xData.Peptide, yData.Peptide);
                if (pepCompare != 0)
                {
                    return pepCompare;
                }

                return xData.ProteinID.CompareTo(yData.ProteinID);
            }
        }

        private class ProteinIDMapInfoPeptideSearchComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == null || y == null)
                    return 0;

                var xData = (udtProteinIDMapInfoType)x;
                var peptide = Convert.ToString(y);

                return String.CompareOrdinal(xData.Peptide, peptide);
            }
        }

        private class PepToProteinMappingComparer : IComparer<udtPepToProteinMappingType>
        {
            public int Compare(udtPepToProteinMappingType x, udtPepToProteinMappingType y)
            {
                var pepCompare = String.CompareOrdinal(x.Peptide, y.Peptide);
                if (pepCompare != 0)
                {
                    return pepCompare;
                }

                return String.CompareOrdinal(x.Protein, y.Protein);
            }
        }

        #endregion
    }
}