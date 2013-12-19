@echo on
Del PRISM_Class_Library_Source*.zip
Del ProteinFileReader_Source*.zip
Del SharedVBNetRoutines_Source*.zip

@echo off
echo;

Copy "F:\My Documents\Projects\DataMining\DMS_Managers\PRISM_Class_Library\SourceCode\PRISM_Class_Library_Source*.zip" .
Copy "F:\My Documents\Projects\DataMining\ProteinFileReaderDLL\ProteinFileReader_SourceCode\*.zip" .
Copy "F:\My Documents\Projects\DataMining\SharedVBNetRoutines\SharedVBNetRoutines_SourceCode\*.zip" .

