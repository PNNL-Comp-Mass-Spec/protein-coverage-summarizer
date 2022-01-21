@echo off
echo Be sure to build the program in Debug mode
pause

@echo on
"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\README.md
"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\RevisionHistory.txt
"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\Debug\*.exe
"c:\Program Files\7-Zip\7z.exe" a -r PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\Debug\*.dll

"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer.zip ..\..\README.md
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer.zip ..\..\RevisionHistory.txt
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer.zip ..\..\bin\Debug\*.exe
"c:\Program Files\7-Zip\7z.exe" a -r ProteinCoverageSummarizer.zip ..\..\bin\Debug\*.dll

"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\..\README.md
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\..\RevisionHistory.txt
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\Output\ProteinCoverageSummarizer_Installer.exe

pause
