using ProteinFileReader;

namespace ProteinCoverageSummarizer
{
    public class ProteinDataCacheOptions
    {
        private char mDelimitedInputFileDelimiter;                              // Only used for delimited protein input files, not for fasta files

        /// <summary>
        /// When True, assume the input file is a tab-delimited text file
        /// </summary>
        /// <remarks>Ignored if AssumeFastaFile is True</remarks>
        public bool AssumeDelimitedFile { get; set; }

        /// <summary>
        /// When True, assume the input file is a FASTA text file
        /// </summary>
        public bool AssumeFastaFile { get; set; }

        public bool ChangeProteinSequencesToLowercase { get; set; }

        public bool ChangeProteinSequencesToUppercase { get; set; }

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

        public clsProteinFileDataCache.FastaFileOptionsClass FastaFileOptions;

        public DelimitedProteinFileReader.ProteinFileFormatCode DelimitedFileFormatCode { get; set; }

        public bool DelimitedFileSkipFirstLine { get; set; }

        /// <summary>
        /// When this is True, the SQLite Database will not be deleted after processing finishes
        /// </summary>
        public bool KeepDB { get; set; }

        public bool RemoveSymbolCharacters { get; set; }

        public bool IgnoreILDifferences { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProteinDataCacheOptions()
        {
            DelimitedInputFileDelimiter = '\t';
            DelimitedFileFormatCode = DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Description_Sequence;
            FastaFileOptions = new clsProteinFileDataCache.FastaFileOptionsClass();

            RemoveSymbolCharacters = true;

            ChangeProteinSequencesToLowercase = false;
            ChangeProteinSequencesToUppercase = false;

            IgnoreILDifferences = false;
        }
    }
}
