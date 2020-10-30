if not exist ProteinComparison (mkdir ProteinComparison)
if not exist FileMode1 (mkdir FileMode1)

..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides.txt                  /R:BSA_P171_QID1638_TestProteins.fasta /D
..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides.txt      /SkipHeader /R:BSA_P171_QID1638_TestProteins.txt   /D /O:ProteinComparison\
..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides.txt /F:1 /SkipHeader /R:BSA_P171_QID1638_TestProteins.fasta /D /O:FileMode1\

..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides_SequenceOnly.txt /R:..\BSA\BSA_P171_QID1638_TestProteins.fasta /D /M /SkipHeader

pause
