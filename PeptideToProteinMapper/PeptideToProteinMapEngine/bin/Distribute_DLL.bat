f:
pushd "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\AnyCPU"

copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Common\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_IMS_Plugin\bin\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\PeptideToProteinMapEngine.dll" /Y
copy PeptideToProteinMapEngine.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\Release\PeptideToProteinMapEngine.dll" /Y

copy ..\x64\PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\x64\PeptideToProteinMapEngine.dll" /Y
copy ..\x64\PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\x64\PeptideToProteinMapEngine.pdb" /Y

copy ..\x64\PeptideToProteinMapEngine.dll "C:\DMS_Programs\PHRP\PeptideToProteinMapEngine.dll" /Y
copy ..\x64\PeptideToProteinMapEngine.pdb "C:\DMS_Programs\PHRP\PeptideToProteinMapEngine.pdb" /Y


copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\PeptideToProteinMapEngine.dll" /Y
copy ..\x64\PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\x64\PeptideToProteinMapEngine.dll" /Y

copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\PeptideToProteinMapEngine.dll" /Y
copy ..\x64\PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\x64\PeptideToProteinMapEngine.dll" /Y

copy PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\PeptideToProteinMapEngine.dll" /Y
copy ..\x64\PeptideToProteinMapEngine.dll "F:\My Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x64\PeptideToProteinMapEngine.dll" /Y

copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\bin\PeptideToProteinMapEngine.pdb" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\PeptideToProteinMapEngine.pdb" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\Lib\PeptideToProteinMapEngine.pdb" /Y
copy PeptideToProteinMapEngine.pdb "F:\My Documents\projects\dataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\PeptideToProteinMapEngine.pdb" /Y
pause
popd

rem Also distribute ProteinCoverageSummarizer.dll
rem Also distribute ProteinCoverageSummarizer.dll
call ..\..\..\ProteinCoverageSummarizer\bin\Distribute_DLL.bat
