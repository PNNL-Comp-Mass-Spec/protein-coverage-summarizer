; This is an Inno Setup configuration file
; https://jrsoftware.org/isinfo.php
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
Source: ..\bin\Release\ProteinCoverageSummarizerGUI.exe            ; DestDir: {app}
Source: ..\bin\Release\ProteinCoverageSummarizerGUI.exe.config     ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Bcl.AsyncInterfaces.dll           ; DestDir: {app}
Source: ..\bin\Release\Npgsql.dll                                  ; DestDir: {app}
Source: ..\bin\Release\Ookii.Dialogs.dll                           ; DestDir: {app}
Source: ..\bin\Release\PRISM.dll                                   ; DestDir: {app}
Source: ..\bin\Release\PRISMDatabaseUtils.dll                      ; DestDir: {app}
Source: ..\bin\Release\PRISMWin.dll                                ; DestDir: {app}
Source: ..\bin\Release\ProteinCoverageSummarizer.dll               ; DestDir: {app}
Source: ..\bin\Release\ProteinFileReader.dll                       ; DestDir: {app}
Source: ..\bin\Release\System.Buffers.dll                          ; DestDir: {app}
Source: ..\bin\Release\System.Data.SQLite.dll                      ; DestDir: {app}
Source: ..\bin\Release\System.Memory.dll                           ; DestDir: {app}
Source: ..\bin\Release\System.Numerics.Vectors.dll                 ; DestDir: {app}
Source: ..\bin\Release\System.Runtime.CompilerServices.Unsafe.dll  ; DestDir: {app}
Source: ..\bin\Release\System.Text.Encodings.Web.dll               ; DestDir: {app}
Source: ..\bin\Release\System.Text.Json.dll                        ; DestDir: {app}
Source: ..\bin\Release\System.Threading.Tasks.Extensions.dll       ; DestDir: {app}
Source: ..\bin\Release\System.ValueTuple.dll                       ; DestDir: {app}
Source: ..\bin\ProteinCoverageSummarizerSettings.xml               ; DestDir: {app}
Source: ..\bin\Release\x64\SQLite.Interop.dll                      ; DestDir: {app}\x64
Source: ..\bin\Release\x86\SQLite.Interop.dll                      ; DestDir: {app}\x86

Source: ..\PeptideToProteinMapper\bin\Release\PeptideToProteinMapper.exe    ; DestDir: {app}
Source: ..\PeptideToProteinMapper\bin\Release\PeptideToProteinMapEngine.dll ; DestDir: {app}
Source: ..\PeptideToProteinMapper\bin\Release\PHRPReader.dll                ; DestDir: {app}

Source: ..\bin\BSA_P171_QID1638_TestProteins.fasta                        ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides.txt                          ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_coverage.txt                 ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_ProteinToPeptideMapping.txt  ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_SequenceOnly.txt                         ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_SequenceOnly_coverage.txt                ; DestDir: {app}
Source: ..\bin\BSA_P171_QID1638_TestPeptides_SequenceOnly_ProteinToPeptideMapping.txt ; DestDir: {app}

Source: ..\bin\BSA_P171_QID1638_TestProteins.txt                          ; DestDir: {app}

Source: Images\delete_16x.ico                ; DestDir: {app}
Source: ..\License.txt                       ; DestDir: {app}
Source: ..\Disclaimer.txt                    ; DestDir: {app}
Source: ..\Readme.md                         ; DestDir: {app}
Source: ..\RevisionHistory.txt               ; DestDir: {app}

[Dirs]
Name: {commonappdata}\ProteinCoverageSummarizer; Flags: uninsalwaysuninstall

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
; Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Icons]
Name: {commondesktop}\Protein Coverage Summarizer; Filename: {app}\ProteinCoverageSummarizerGUI.exe; Tasks: desktopicon; Comment: Protein Coverage SummarizerGUI
Name: {group}\Protein Coverage Summarizer; Filename: {app}\ProteinCoverageSummarizerGUI.exe; Comment: Protein Coverage SummarizerGUI

[Setup]
AppName=Protein Coverage Summarizer
AppVersion={#ApplicationVersion}
;AppVerName=ProteinCoverageSummarizer
AppID=ProteinCoverageSummarizerId
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=https://omics.pnl.gov/software
AppSupportURL=https://omics.pnl.gov/software
AppUpdatesURL=https://github.com/PNNL-Comp-Mass-Spec/protein-coverage-summarizer
ArchitecturesAllowed=x64 x86
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\ProteinCoverageSummarizer
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
;LicenseFile=.\License.rtf
PrivilegesRequired=admin
OutputBaseFilename=ProteinCoverageSummarizer_Installer
;VersionInfoVersion=1.57
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=Protein Coverage Summarizer
VersionInfoCopyright=PNNL
DisableFinishedPage=yes
DisableWelcomePage=no
ShowLanguageDialog=no
ChangesAssociations=no
WizardStyle=modern
EnableDirDoesntExistWarning=no
AlwaysShowDirOnReadyPage=yes
UninstallDisplayIcon={app}\delete_16x.ico
ShowTasksTreeLines=yes
OutputDir=.\Output

[Registry]
;Root: HKCR; Subkey: MyAppFile; ValueType: string; ValueName: ; ValueDataMyApp File; Flags: uninsdeletekey
;Root: HKCR; Subkey: MyAppSetting\DefaultIcon; ValueType: string; ValueData: {app}\wand.ico,0; Flags: uninsdeletevalue

[UninstallDelete]
Name: {app}; Type: filesandordirs
