f:
pushd "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\AnyCPU"

copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_IMS_Plugin\bin\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\" /Y

copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\Release\" /Y

copy PeptideToProteinMapEngine.dll "C:\DMS_Programs\PHRP\" /Y
copy PeptideToProteinMapEngine.pdb "C:\DMS_Programs\PHRP\" /Y
pause

@echo off
echo.
echo.
@echo on

copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.dll        "F:\My Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y

copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y
popd

@echo off
echo.
echo Also distribute ProteinCoverageSummarizer.dll
echo.
pause

@echo on
call ..\..\..\ProteinCoverageSummarizer\bin\Distribute_DLL.bat

