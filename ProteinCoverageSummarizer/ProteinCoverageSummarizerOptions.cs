namespace ProteinCoverageSummarizer
{
    /// <summary>
    /// Protein coverage summarizer options
    /// </summary>
    public class ProteinCoverageSummarizerOptions
    {
        /// <summary>
        /// Peptide file column order
        /// </summary>
        public enum PeptideFileColumnOrderingCode
        {
            /// <summary>
            /// The first column is peptide sequence; ignore other columns
            /// </summary>
            SequenceOnly = 0,

            /// <summary>
            /// Protein name in the first column, peptide sequence in the second column
            /// </summary>
            ProteinName_PeptideSequence = 1,

            /// <summary>
            /// Examine header names to find the peptide and protein columns
            /// </summary>
            UseHeaderNames = 2
        }

        /// <summary>
        /// When true, show debug messages
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// When true, treat I and L residues equally
        /// </summary>
        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB { get; set; }

        /// <summary>
        /// When true, require that each peptide's prefix and suffix letters match the protein sequence
        /// </summary>
        public bool MatchPeptidePrefixAndSuffixToProtein { get; set; }

        /// <summary>
        /// Output directory path
        /// </summary>
        /// <remarks>Leave empty to auto-define</remarks>
        public string OutputDirectoryPath { get; set; }

        /// <summary>
        /// When true, include the protein sequence in the output file
        /// </summary>
        public bool OutputProteinSequence { get; set; }

        /// <summary>
        /// Peptide file column order
        /// </summary>
        public PeptideFileColumnOrderingCode PeptideFileFormatCode { get; set; }

        /// <summary>
        /// When true, skip the first line of the peptide file
        /// </summary>
        public bool PeptideFileSkipFirstLine { get; set; }

        /// <summary>
        /// Peptide file column delimiter (default is tab)
        /// </summary>
        public char PeptideInputFileDelimiter { get; set; }

        /// <summary>
        /// Peptide input file path
        /// </summary>
        public string PeptideInputFilePath { get; set; }

        /// <summary>
        /// Protein input file path
        /// </summary>
        public string ProteinInputFilePath { get; set; }

        /// <summary>
        /// Protein data cache options
        /// </summary>
        public ProteinDataCacheOptions ProteinDataOptions { get; }

        /// <summary>
        /// When true, remove symbol characters from peptide sequences (default is true)
        /// </summary>
        public bool RemoveSymbolCharacters { get; set; }

        /// <summary>
        /// Save the protein to peptide map file
        /// </summary>
        public bool SaveProteinToPeptideMappingFile { get; set; }

        /// <summary>
        /// Create a new file that matches the input file, but has protein name appended
        /// </summary>
        public bool SaveSourceDataPlusProteinsFile { get; set; }

        /// <summary>
        /// When true, search all proteins for each peptide, ignoring any protein names listed in the input file
        /// </summary>
        public bool SearchAllProteinsForPeptideSequence { get; set; }

        /// <summary>
        /// When true, skip coverage computation steps (saves memory)
        /// </summary>
        public bool SearchAllProteinsSkipCoverageComputationSteps { get; set; }

        /// <summary>
        /// Track peptide counts
        /// </summary>
        public bool TrackPeptideCounts { get; set; }

        /// <summary>
        /// Use the leader sequence hash table (dramatically speeds up the search)
        /// </summary>
        public bool UseLeaderSequenceHashTable { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProteinCoverageSummarizerOptions()
        {
            ProteinDataOptions = new ProteinDataCacheOptions();

            PeptideInputFilePath = string.Empty;
            ProteinInputFilePath = string.Empty;
            OutputDirectoryPath = string.Empty;

            PeptideFileSkipFirstLine = false;
            PeptideInputFileDelimiter = '\t';
            PeptideFileFormatCode = PeptideFileColumnOrderingCode.UseHeaderNames;

            IgnoreILDifferences = false;
            OutputProteinSequence = true;
            SaveProteinToPeptideMappingFile = false;
            SaveSourceDataPlusProteinsFile = false;
            SearchAllProteinsForPeptideSequence = true;
            SearchAllProteinsSkipCoverageComputationSteps = false;

            TrackPeptideCounts = true;
            RemoveSymbolCharacters = true;
            MatchPeptidePrefixAndSuffixToProtein = false;

            UseLeaderSequenceHashTable = true;

            DebugMode = false;
            KeepDB = false;
        }
    }
}
