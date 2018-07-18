# Protein Coverage Summarizer

The Protein Coverage Summarizer can be used to determine the percent of the residues 
in each protein sequence that have been identified. 

The program requires two input files: 
* a file with protein names and protein sequences (optionally with protein description).  This can be a tab-delimited text file or a FASTA file
* a file with peptide sequences and optionally also protein names for each peptide

A graphical user interface (GUI) is provided to allow the user to select the input files, set the options, and browse the coverage results. 
The results browser displays the protein sequences, highlighting the residues that were present 
in the peptide input file, and providing sequence coverage stats for each protein.

## Console Version Syntax

```
ProteinCoverageSummarizerGUI.exe
  /I:PeptideInputFilePath /R:ProteinInputFilePath [/O:OutputFolderName] [/P:ParameterFilePath] 
  [/G] [/H] [/M] [/K] [/Q]
```

The input file path can contain the wildcard character *.  If a wildcard is present, the same 
protein input file path will be used for each of the peptide input files matched.

The output folder name is optional. If omitted, the output files will be created in the same folder as the input file.
If included, then a subfolder is created with the name OutputFolderName.

The parameter file path is optional.  If included, it should point to a valid XML parameter file.

Use /G to ignore I/L differences when finding peptides in proteins or computing coverage.

Use /H to suppress (hide) the protein sequence in the _coverage.txt file.

Use /M to enable the creation of a protein to peptide mapping file.

## Contacts

Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA) in 2005 \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://panomics.pnl.gov/ or https://omics.pnl.gov

## License

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
