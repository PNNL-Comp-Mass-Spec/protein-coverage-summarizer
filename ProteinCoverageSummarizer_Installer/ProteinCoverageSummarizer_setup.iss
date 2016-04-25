; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php
;
; This file uses Protein Coverage Summarizer files from bin\AnyCPU
; Those come from the Debug AnyCPU build of ProteinCoverageSummarizerGUI.sln

#define ApplicationVersion GetFileVersion('..\bin\AnyCPU\ProteinCoverageSummarizer.dll')

[CustomMessages]
AppName=Protein Coverage Summarizer
[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.
; Example with multiple lines:
; WelcomeLabel2=Welcome message%n%nAdditional sentence
[Files]
Source: ..\bin\AnyCPU\ProteinCoverageSummarizerGUI.exe         ; DestDir: {app}
Source: ..\bin\AnyCPU\ProteinCoverageSummarizerGUI.exe.config  ; DestDir: {app}
Source: ..\bin\AnyCPU\PRISM.dll                                ; DestDir: {app}
Source: ..\bin\AnyCPU\ProteinCoverageSummarizer.dll            ; DestDir: {app}
Source: ..\bin\AnyCPU\ProteinFileReader.dll                    ; DestDir: {app}
Source: ..\bin\AnyCPU\SharedVBNetRoutines.dll                  ; DestDir: {app}
Source: ..\bin\AnyCPU\System.Data.SQLite.dll                   ; DestDir: {app}
Source: ..\bin\AnyCPU\x64\SQLite.Interop.dll                   ; DestDir: {app}\x64
Source: ..\bin\AnyCPU\x86\SQLite.Interop.dll                   ; DestDir: {app}\x86

Source: ..\bin\BSA_P171_QID1638_TestProteins.fasta                        ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides.txt                          ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_coverage.txt                 ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_ProteinToPeptideMapping.txt  ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_SequenceOnly.txt             ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestProteins.txt                          ; DestDir: {app}

Source: Images\textdoc.ico                   ; DestDir: {app}
Source: Images\delete_16x.ico                ; DestDir: {app}
Source: ..\Readme.txt                        ; DestDir: {app}
Source: ..\RevisionHistory.txt               ; DestDir: {app}

[Dirs]
Name: {commonappdata}\ProteinCoverageSummarizer; Flags: uninsalwaysuninstall

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
; Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Icons]
Name: {commondesktop}\Protein Coverage Summarizer; Filename: {app}\ProteinCoverageSummarizerGUI.exe; Tasks: desktopicon; Comment: Protein Coverage SummarizerGUI
Name: {group}\Protein Coverage Summarizer; Filename: {app}\ProteinCoverageSummarizerGUI.exe; Comment: Protein Coverage SummarizerGUI
Name: {group}\ReadMe File; Filename: {app}\readme.txt; IconFilename: {app}\textdoc.ico; IconIndex: 0; Comment: Protein Coverage Summarizer ReadMe

[Setup]
AppName=Protein Coverage Summarizer
AppVersion={#ApplicationVersion}
;AppVerName=ProteinCoverageSummarizer
AppID=ProteinCoverageSummarizerId
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=http://omics.pnl.gov/software
AppSupportURL=http://omics.pnl.gov/software
AppUpdatesURL=http://omics.pnl.gov/software
DefaultDirName={pf}\ProteinCoverageSummarizer
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
;LicenseFile=.\License.rtf
PrivilegesRequired=poweruser
OutputBaseFilename=ProteinCoverageSummarizer_Installer
;VersionInfoVersion=1.57
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=Protein Coverage Summarizer
VersionInfoCopyright=PNNL
DisableFinishedPage=true
ShowLanguageDialog=no
ChangesAssociations=false
EnableDirDoesntExistWarning=false
AlwaysShowDirOnReadyPage=true
UninstallDisplayIcon={app}\delete_16x.ico
ShowTasksTreeLines=true
OutputDir=.\Output
[Registry]
;Root: HKCR; Subkey: MyAppFile; ValueType: string; ValueName: ; ValueDataMyApp File; Flags: uninsdeletekey
;Root: HKCR; Subkey: MyAppSetting\DefaultIcon; ValueType: string; ValueData: {app}\wand.ico,0; Flags: uninsdeletevalue
[UninstallDelete]
Name: {app}; Type: filesandordirs
