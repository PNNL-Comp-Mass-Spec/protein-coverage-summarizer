# PeptideToProteinMapper

The PeptideToProteinMapper reads in a text file containing peptide sequences then 
searches the specified .fasta or text file containing protein names and sequences 
to find the proteins that contain each peptide.  The program will also compute 
the sequence coverage percent for each protein (though this can be disable using /K).

## Program Syntax

```
PeptideToProteinMapper.exe /I:PeptideInputFilePath /R:ProteinInputFilePath
 [/O:OutputFolderName] [/P:ParameterFilePath] [/F:FileFormatCode]
 [/N:InspectParameterFilePath] [/G] [/H] [/K] [/A]
 [/L[:LogFilePath]] [/LogFolder:LogFolderPath] [/VerboseLog] [/Q]
```

The input file path can contain the wildcard character *.  If a wildcard is present, then the same 
protein input file path will be used for each of the peptide input files matched.

The output folder name is optional.  If omitted, the output files will be created in the same folder 
as the input file.  If included, then a subfolder is created with the name OutputFolderName.

The parameter file path is optional.  If included, it should point to a valid XML parameter file.

Use /F to specify the peptide input file format code.  Options are:
* 0=Auto Determine: Treated as /F:1 unless name ends in _inspect.txt, then /F:3
* 1=Peptide sequence in the 1st column (subsequent columns are ignored)
* 2=Protein name in 1st column and peptide sequence 2nd column
* 3=Inspect search results file (peptide sequence in the 3rd column)
* 4=MS-GF+ search results file (peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan')
* 5=Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file

When processing an Inspect search results file, use /N to specify the Inspect parameter file used 
(required for determining the mod names embedded in the identified peptides).

Use /G to ignore I/L differences when finding peptides in proteins or computing coverage

Use /H to suppress (hide) the protein sequence in the _coverage.txt file

Use /K to skip the protein coverage computation steps (enabling faster processing)

Use /A to create a copy of the source file, but with a new column listing the mapped protein for each peptide.
If a peptide maps to multiple proteins, then multiple lines will be listed.

Use /L to create a log file, optionally specifying the file name

Use /LogFolder to define the folder in which the log file should be created

Use /VerboseLog to create a detailed log file

Use /Q to suppress any error messages

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://panomics.pnl.gov/ or https://omics.pnl.gov

## License

The Protein Coverage Summarizer is licensed under the 2-Clause BSD License; 
you may not use this file except in compliance with the License.  You may obtain 
a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
