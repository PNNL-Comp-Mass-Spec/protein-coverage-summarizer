namespace ProteinCoverageSummarizer
{
    public class ProteinCoverageSummarizerOptions
    {
        public enum PeptideFileColumnOrderingCode
        {
            SequenceOnly = 0,
            ProteinName_PeptideSequence = 1,
            UseHeaderNames = 2
        }

        public bool DebugMode { get; set; }

        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB { get; set; }

        public bool MatchPeptidePrefixAndSuffixToProtein { get; set; }

        public string OutputDirectoryPath { get; set; }

        public bool OutputProteinSequence { get; set; }

        public PeptideFileColumnOrderingCode PeptideFileFormatCode { get; set; }

        public bool PeptideFileSkipFirstLine { get; set; }

        public char PeptideInputFileDelimiter { get; set; }

        public string PeptideInputFilePath { get; set; }

        public string ProteinInputFilePath { get; set; }

        public ProteinDataCacheOptions ProteinDataOptions { get; }

        public bool RemoveSymbolCharacters { get; set; }

        public bool SaveProteinToPeptideMappingFile { get; set; }

        public bool SaveSourceDataPlusProteinsFile { get; set; }

        public bool SearchAllProteinsForPeptideSequence { get; set; }

        public bool SearchAllProteinsSkipCoverageComputationSteps { get; set; }

        public bool TrackPeptideCounts { get; set; }

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
            PeptideFileFormatCode = PeptideFileColumnOrderingCode.ProteinName_PeptideSequence;

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
