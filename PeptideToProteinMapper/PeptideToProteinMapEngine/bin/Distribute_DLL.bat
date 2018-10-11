f:
pushd "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\AnyCPU"

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\" /Y

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\Release\" /Y

copy PeptideToProteinMapEngine.dll "C:\DMS_Programs\PHRP\" /Y
copy PeptideToProteinMapEngine.pdb "C:\DMS_Programs\PHRP\" /Y
pause

@echo off
echo.
echo.
@echo on

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y

copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y

copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\JoshAldrich\AScore\AScore_DLL\lib\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\JoshAldrich\AScore\AScore_DLL\bin\AnyCPU\Debug\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Debug\" /Y

popd

@echo off
echo.
echo Also distribute ProteinCoverageSummarizer.dll
echo.
pause

@echo on
call ..\..\..\ProteinCoverageSummarizer\bin\Distribute_DLL.bat

