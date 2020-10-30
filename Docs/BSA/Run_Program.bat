if not exist ProteinComparison (mkdir ProteinComparison)

..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides.txt              /R:BSA_P171_QID1638_TestProteins.fasta /D
..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides.txt              /R:BSA_P171_QID1638_TestProteins.txt   /D /O:ProteinComparison\
..\..\bin\Debug\ProteinCoverageSummarizerGUI.exe BSA_P171_QID1638_TestPeptides_SequenceOnly.txt /R:BSA_P171_QID1638_TestProteins.fasta /D

pause
