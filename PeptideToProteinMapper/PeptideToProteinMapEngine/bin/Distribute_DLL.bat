f:
pushd "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\Debug"

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\" /Y

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\bin\Debug\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\Release\" /Y

copy PeptideToProteinMapEngine.dll "C:\DMS_Programs\PHRP\" /Y
copy PeptideToProteinMapEngine.pdb "C:\DMS_Programs\PHRP\" /Y
If not "%1"=="NoPause" If not "%2"=="NoPause" pause

@echo off
echo.
echo.
@echo on

copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\Debug" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\Release" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\Debug\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\Release\" /Y

copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\bin\Debug\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\bin\Release\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\Debug\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\Release\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\Debug\" /Y
copy PeptideToProteinMapEngine.pdb "F:\Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\Release\" /Y

copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\Josh_Aldrich\AScore\AScore_DLL\lib\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\Josh_Aldrich\AScore\AScore_DLL\bin\AnyCPU\Debug\" /Y
copy PeptideToProteinMapEngine.dll "F:\Documents\Projects\Josh_Aldrich\AScore\AScore_Console\bin\Debug\" /Y

popd

rem ProteinCoverageSummarizer\bin\Distribute_DLL.bat passes "NoCall" to this batch file
rem to indicate that this batch file should not call ..\..\..\ProteinCoverageSummarizer\bin\Distribute_DLL.bat
If "%1"=="NoCall" Goto Done

@echo off
echo.
echo Also distribute ProteinCoverageSummarizer.dll
echo.
If not "%1"=="NoPause" If not "%2"=="NoPause" pause

@echo on
call ..\..\..\ProteinCoverageSummarizer\bin\Distribute_DLL.bat NoCall %2

:Done

If not "%1"=="NoPause" If not "%2"=="NoPause" pause
