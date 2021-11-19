@echo off

set ExePath=ProteinCoverageSummarizerGUI.exe

if exist %ExePath% goto DoWork
if exist ..\%ExePath% set ExePath=..\%ExePath% && goto DoWork
if exist ..\Bin\%ExePath% set ExePath=..\Bin\%ExePath% && goto DoWork
if exist ..\Bin\Debug\%ExePath% set ExePath=..\Bin\Debug\%ExePath% && goto DoWork

echo Executable not found: %ExePath%
goto Done

:DoWork
echo.
echo Processing with %ExePath%
echo.


%ExePath% QC_Mam_19_01_a_12Oct20_Pippin_Rep-20-08-08_msgfplus.tsv /R:M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /D
%ExePath% ProteinAndPeptide.txt /R:M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /D
%ExePath% PeptideOnly.txt       /R:M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /D

:Done

pause


pause
