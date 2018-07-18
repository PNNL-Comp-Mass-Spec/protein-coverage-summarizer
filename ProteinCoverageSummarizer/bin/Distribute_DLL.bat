f:
pushd "F:\Documents\Projects\DataMining\Protein_Coverage_Summarizer\ProteinCoverageSummarizer\bin\AnyCPU"

copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InSpecT_PlugIn\bin\" /Y
copy ProteinCoverageSummarizer.dll        "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_IMS_Plugin\bin\" /Y
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

popd

pause
