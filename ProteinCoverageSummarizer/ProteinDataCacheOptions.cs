using ProteinFileReader;

namespace ProteinCoverageSummarizer
{
    /// <summary>
    /// Protein data cache options
    /// </summary>
    public class ProteinDataCacheOptions
    {
        /// <summary>
        /// Delimiter character for delimited protein files
        /// </summary>
        /// <remarks>Only used for delimited protein input files, not for FASTA files</remarks>
        private char mDelimitedInputFileDelimiter;

        /// <summary>
        /// When True, assume the input file is a tab-delimited text file
        /// </summary>
        /// <remarks>Ignored if AssumeFastaFile is True</remarks>
        public bool AssumeDelimitedFile { get; set; }

        /// <summary>
        /// When True, assume the input file is a FASTA text file
        /// </summary>
        public bool AssumeFastaFile { get; set; }

        /// <summary>
        /// When true, change protein sequences to lowercase
        /// </summary>
        public bool ChangeProteinSequencesToLowercase { get; set; }

        /// <summary>
        /// When true, change protein sequences to uppercase
        /// </summary>
        public bool ChangeProteinSequencesToUppercase { get; set; }

        /// <summary>
        /// Delimiter character for delimited protein files
        /// </summary>
        public char DelimitedInputFileDelimiter
        {
            get => mDelimitedInputFileDelimiter;
            set
            {
                if (value != default)
                {
                    mDelimitedInputFileDelimiter = value;
                }
            }
        }

        /// <summary>
        /// Delimited file format
        /// </summary>
        public DelimitedProteinFileReader.ProteinFileFormatCode DelimitedFileFormatCode { get; set; }

        /// <summary>
        /// When true, skip the first line of a delimited protein file
        /// </summary>
        public bool DelimitedFileSkipFirstLine { get; set; }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB { get; set; }

        /// <summary>
        /// When true, remove symbol characters from proteins
        /// </summary>
        public bool RemoveSymbolCharacters { get; set; }

        /// <summary>
        /// When true, treat I and L residues equally
        /// </summary>
        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProteinDataCacheOptions()
        {
            DelimitedInputFileDelimiter = '\t';
            DelimitedFileFormatCode = DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Description_Sequence;

            RemoveSymbolCharacters = true;

            ChangeProteinSequencesToLowercase = false;
            ChangeProteinSequencesToUppercase = false;

            IgnoreILDifferences = false;
        }
    }
}
