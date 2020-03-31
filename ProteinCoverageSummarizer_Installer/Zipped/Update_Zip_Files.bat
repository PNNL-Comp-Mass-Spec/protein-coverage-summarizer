"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\README.md
"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\RevisionHistory.txt
"c:\Program Files\7-Zip\7z.exe" a PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\AnyCPU\*.exe
"c:\Program Files\7-Zip\7z.exe" a -r PeptideToProteinMapper.zip ..\..\PeptideToProteinMapper\bin\AnyCPU\*.dll

"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\..\README.md
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\..\RevisionHistory.txt
"c:\Program Files\7-Zip\7z.exe" a ProteinCoverageSummarizer_Installer.zip ..\Output\ProteinCoverageSummarizer_Installer.exe

pause
