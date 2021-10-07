# PeptideToProteinMapper

The PeptideToProteinMapper reads in a text file containing peptide sequences then 
searches the specified .fasta or text file containing protein names and sequences 
to find the proteins that contain each peptide.  The program will also compute 
the sequence coverage percent for each protein (though this can be disable using /K).

## Downloads

Download PeptideToProteinMapper.zip from:
https://github.com/PNNL-Comp-Mass-Spec/protein-coverage-summarizer/releases

## Program Syntax

```
PeptideToProteinMapper.exe
 /I:PeptideInputFilePath /R:ProteinInputFilePath
 [/O:OutputDirectoryName] [/P:ParameterFilePath] [/F:FileFormatCode]
 [/N:InspectParameterFilePath] [/G] [/H] [/K] [/A]
 [/L[:LogFilePath]] [/LogDir:LogDirectoryPath] [/VerboseLog] [/Q]
```

The input file path can contain the wildcard character *. If a wildcard is
present, the same protein input file path will be used for each of the peptide
input files matched.

The output directory name is optional. If omitted, the output files will be
created in the same directory as the input file. If included, then a subdirectory
is created with the name OutputDirectoryName.

The parameter file path is optional. If included, it should point to a valid XML
parameter file.

Use `/F` to specify the peptide input file format code.  Options are:
| Format Code | Type           | Comment                                                         |
|-------------|----------------|-----------------------------------------------------------------|
| 0           | Auto Determine | Treated as `/F:1` unless name ends in _inspect.txt, then `/F:3` |
| 1           | Peptide sequence in the 1st column | Subsequent columns are ignored              |
| 2           | Protein name in 1st column and peptide sequence 2nd column |                     |
| 3           | Inspect search results file               | Peptide sequence in the 3rd column   |
| 4           | MS-GF+ search results file                | Peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan'     |
| 5           | Peptide Hit Results Processor (PHRP) file | PHRP creates tab-delimited text files for MS-GF+, X!Tandem, SEQUEST, or Inspect results                 |
| 6           | Generic tab-delimited text file           | Will look for a column titled 'Peptide'; also looks for Protein and Scan, though these are not required |

When processing an Inspect search results file, use `/N` to specify the Inspect
parameter file used (required for determining the mod names embedded in the
identified peptides).

Use `/G` to ignore I/L differences when finding peptides in proteins or computing coverage

Use `/H` to suppress (hide) the protein sequence in the _coverage.txt file

Use `/K` to skip the protein coverage computation steps (enabling faster processing)

Use `/A` to create the _AllProteins.txt file, listing each of the peptides in the input file,
plus one line per mapped protein for that peptide

Use `/L` to create a log file, optionally specifying the file name

Use `/LogDir` to define the directory in which the log file should be created

Use `/VerboseLog` to create a detailed log file

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics

## License

The Peptide to Protein Mapper is licensed under the 2-Clause BSD License; 
you may not use this program except in compliance with the License.  You may obtain 
a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
