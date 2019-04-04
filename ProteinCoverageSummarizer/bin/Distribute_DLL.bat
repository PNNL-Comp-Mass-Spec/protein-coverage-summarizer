@echo off:
pushd "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\ProteinCoverageSummarizer\bin\AnyCPU"

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InSpecT_PlugIn\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\" /Y

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\" /Y

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y

pause

@echo off
echo.
echo.
@echo on

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y
copy ..\x64\ProteinCoverageSummarizer.dll "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x64\" /Y

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\x86\" /Y
copy ..\x64\ProteinCoverageSummarizer.dll "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\x64\" /Y

copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\bin\x86\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\x86\" /Y
copy ProteinCoverageSummarizer.pdb        "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\JoshAldrich\AScore\AScore_DLL\lib\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\JoshAldrich\AScore\AScore_DLL\bin\AnyCPU\Debug\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Debug\" /Y

popd

rem PeptideToProteinMapEngine\bin\Distribute_DLL.bat passes "NoCall" to this batch file
rem to indicate that this batch file should not call ..\..\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\Distribute_DLL.bat 
If "%1"=="NoCall" Goto Done

@echo off
echo.
echo.
echo.
echo Press any key to also distribute PeptideToProteinMapEngine.dll
pause
@echo on

call ..\..\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\Distribute_DLL.bat NoCall

:Done
