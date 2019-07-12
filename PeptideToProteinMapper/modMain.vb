Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Started September 2008
'
' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause
'
' Copyright 2018 Battelle Memorial Institute

Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports PeptideToProteinMapEngine
Imports PeptideToProteinMapEngine.clsPeptideToProteinMapEngine
Imports PRISM

''' <summary>
''' This program uses PeptideToProteinMapEngine.dll to read in a file with peptide sequences, then
''' searches for the given peptides in a protein sequence file (.Fasta or tab-delimited text)
''' using ProteinCoverageSummarizer.dll
'''
''' This program is similar to the ProteinCoverageSummarizer, but it is a console-only application
''' In addition, this program supports reading Inspect output files
'''
''' Example command Line
''' /I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath
''' </summary>
Public Module modMain

    Public Const PROGRAM_DATE As String = "July 11, 2019"

    Private mPeptideInputFilePath As String
    Private mProteinInputFilePath As String
    Private mOutputDirectoryPath As String
    Private mParameterFilePath As String
    Private mInspectParameterFilePath As String

    Private mIgnoreILDifferences As Boolean
    Private mOutputProteinSequence As Boolean

    Private mSaveProteinToPeptideMappingFile As Boolean
    Private mSaveSourceDataPlusProteinsFile As Boolean

    Private mSkipCoverageComputationSteps As Boolean
    Private mInputFileFormatCode As ePeptideInputFileFormatConstants

    Private mLogMessagesToFile As Boolean
    Private mLogFilePath As String = String.Empty
    Private mLogDirectoryPath As String = String.Empty

    Private mVerboseLogging As Boolean
    Private mVerboseLogFile As StreamWriter
    Private mVerboseLoggingMostRecentMessage As String = String.Empty

    Private mPeptideToProteinMapEngine As clsPeptideToProteinMapEngine
    Private mLastProgressReportTime As DateTime
    Private mLastPercentDisplayed As DateTime

    Private Sub CreateVerboseLogFile()
        Dim logFilePath As String
        Dim openingExistingFile As Boolean

        Try
            logFilePath = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)
            logFilePath &= "_VerboseLog_" & DateTime.Now.ToString("yyyy-MM-dd") & ".txt"

            openingExistingFile = File.Exists(logFilePath)

            mVerboseLogFile = New StreamWriter(
                New FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read)) With {
                .AutoFlush = True
            }

            If Not openingExistingFile Then
                mVerboseLogFile.WriteLine("Date" & ControlChars.Tab & "Percent Complete" & ControlChars.Tab & "Message")
            End If

            mVerboseLoggingMostRecentMessage = String.Empty

        Catch ex As Exception
            ShowErrorMessage("Error creating verbose log file: " & ex.Message)
        End Try

    End Sub

    Public Function Main() As Integer
        ' Returns 0 if no error, error code if an error
        Dim returnCode As Integer
        Dim commandLineParser As New clsParseCommandLine()
        Dim proceed As Boolean

        returnCode = 0
        mPeptideInputFilePath = String.Empty
        mProteinInputFilePath = String.Empty
        mParameterFilePath = String.Empty
        mInspectParameterFilePath = String.Empty

        mIgnoreILDifferences = False
        mOutputProteinSequence = True

        mSaveProteinToPeptideMappingFile = True
        mSaveSourceDataPlusProteinsFile = False

        mSkipCoverageComputationSteps = False
        mInputFileFormatCode = ePeptideInputFileFormatConstants.AutoDetermine

        mLogMessagesToFile = False
        mLogFilePath = String.Empty
        mLogDirectoryPath = String.Empty

        Try
            proceed = False
            If commandLineParser.ParseCommandLine Then
                If SetOptionsUsingCommandLineParameters(commandLineParser) Then proceed = True
            End If

            If Not proceed OrElse
               commandLineParser.NeedToShowHelp OrElse
               commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount = 0 Then
                ShowProgramHelp()
                returnCode = -1
            Else
                Try
                    If mVerboseLogging Then
                        CreateVerboseLogFile()
                    End If

                    If String.IsNullOrWhiteSpace(mPeptideInputFilePath) Then
                        ShowErrorMessage("Peptide input file must be defined via /I (or by listing the filename just after the .exe)")
                        returnCode = -1
                        Exit Try
                    ElseIf String.IsNullOrWhiteSpace(mProteinInputFilePath) Then
                        ShowErrorMessage("Protein input file must be defined via /R")
                        returnCode = -1
                        Exit Try
                    End If

                    mPeptideToProteinMapEngine = New clsPeptideToProteinMapEngine() With {
                        .ProteinInputFilePath = mProteinInputFilePath,
                        .LogMessagesToFile = mLogMessagesToFile,
                        .LogFilePath = mLogFilePath,
                        .LogDirectoryPath = mLogDirectoryPath,
                        .PeptideInputFileFormat = mInputFileFormatCode,
                        .InspectParameterFilePath = mInspectParameterFilePath,
                        .IgnoreILDifferences = mIgnoreILDifferences,
                        .OutputProteinSequence = mOutputProteinSequence,
                        .SaveProteinToPeptideMappingFile = mSaveProteinToPeptideMappingFile,
                        .SaveSourceDataPlusProteinsFile = mSaveSourceDataPlusProteinsFile,
                        .SearchAllProteinsSkipCoverageComputationSteps = mSkipCoverageComputationSteps
                    }

                    AddHandler mPeptideToProteinMapEngine.StatusEvent, AddressOf PeptideToProteinMapEngine_StatusEvent
                    AddHandler mPeptideToProteinMapEngine.ErrorEvent, AddressOf PeptideToProteinMapEngine_ErrorEvent
                    AddHandler mPeptideToProteinMapEngine.WarningEvent, AddressOf PeptideToProteinMapEngine_WarningEvent

                    AddHandler mPeptideToProteinMapEngine.ProgressUpdate, AddressOf PeptideToProteinMapEngine_ProgressChanged
                    AddHandler mPeptideToProteinMapEngine.ProgressReset, AddressOf PeptideToProteinMapEngine_ProgressReset

                    mPeptideToProteinMapEngine.ProcessFilesWildcard(mPeptideInputFilePath, mOutputDirectoryPath, mParameterFilePath)

                    If Not mVerboseLogFile Is Nothing Then
                        mVerboseLogFile.Close()
                    End If

                Catch ex As Exception
                    ShowErrorMessage("Error initializing the Peptide to Protein Mapper Options " & ex.Message)
                End Try

            End If

        Catch ex As Exception
            ShowErrorMessage("Error occurred in modMain->Main: " & Environment.NewLine & ex.Message)
            returnCode = -1
        End Try

        Return returnCode

    End Function

    Private Sub DisplayProgressPercent(taskDescription As String, percentComplete As Integer, addCarriageReturn As Boolean)
        If addCarriageReturn Then
            Console.WriteLine()
        End If
        If percentComplete > 100 Then percentComplete = 100
        If String.IsNullOrEmpty(taskDescription) Then taskDescription = "Processing"

        Console.Write(taskDescription & ": " & percentComplete.ToString() & "% ")
        If addCarriageReturn Then
            Console.WriteLine()
        End If
    End Sub

    Private Function GetAppVersion() As String
        Return FileProcessor.ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE)
    End Function

    Private Function SetOptionsUsingCommandLineParameters(CommandLineParser As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false
        ' /I:PeptideInputFilePath /R: ProteinInputFilePath /O:OutputDirectoryPath /P:ParameterFilePath

        Dim value As String = String.Empty
        Dim validParameters = New List(Of String) From {"I", "O", "R", "P", "F", "N", "G", "H", "K", "A", "L", "LogDir", "LogFolder", "VerboseLog"}
        Dim intValue As Integer

        Try
            ' Make sure no invalid parameters are present
            If CommandLineParser.InvalidParametersPresent(validParameters) Then
                ShowErrorMessage("Invalid command line parameters",
                  (From item In CommandLineParser.InvalidParameters(validParameters) Select "/" + item).ToList())
                Return False
            Else
                With CommandLineParser
                    ' Query commandLineParser to see if various parameters are present
                    If .RetrieveValueForParameter("I", value) Then
                        mPeptideInputFilePath = value
                    ElseIf .NonSwitchParameterCount > 0 Then
                        mPeptideInputFilePath = .RetrieveNonSwitchParameter(0)
                    End If


                    If .RetrieveValueForParameter("O", value) Then mOutputDirectoryPath = value
                    If .RetrieveValueForParameter("R", value) Then mProteinInputFilePath = value
                    If .RetrieveValueForParameter("P", value) Then mParameterFilePath = value

                    If .RetrieveValueForParameter("F", value) Then
                        If Integer.TryParse(value, intValue) Then
                            Try
                                mInputFileFormatCode = CType(intValue, ePeptideInputFileFormatConstants)
                            Catch ex As Exception
                                ' Conversion failed; leave mInputFileFormatCode unchanged
                            End Try
                        End If
                    End If

                    If .RetrieveValueForParameter("N", value) Then mInspectParameterFilePath = value

                    If .RetrieveValueForParameter("G", value) Then mIgnoreILDifferences = True
                    If .RetrieveValueForParameter("H", value) Then mOutputProteinSequence = False
                    If .RetrieveValueForParameter("K", value) Then mSkipCoverageComputationSteps = True

                    If .RetrieveValueForParameter("A", value) Then mSaveSourceDataPlusProteinsFile = True

                    If .RetrieveValueForParameter("L", value) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(value) Then
                            mLogFilePath = value
                        End If
                    End If

                    If .RetrieveValueForParameter("LogDir", value) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(value) Then
                            mLogDirectoryPath = value
                        End If
                    End If

                    If .RetrieveValueForParameter("LogFolder", value) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(value) Then
                            mLogDirectoryPath = value
                        End If
                    End If

                    If .RetrieveValueForParameter("VerboseLog", value) Then mVerboseLogging = True
                End With

                Return True
            End If

        Catch ex As Exception
            ShowErrorMessage("Error parsing the command line parameters: " & Environment.NewLine & ex.Message)
        End Try

        Return False

    End Function

    Private Sub ShowErrorMessage(message As String)
        ConsoleMsgUtils.ShowError(message)
    End Sub

    Private Sub ShowErrorMessage(title As String, errorMessages As List(Of String))
        ConsoleMsgUtils.ShowErrors(title, errorMessages)
    End Sub

    Private Sub ShowProgramHelp()

        Try
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                "This program reads in a text file containing peptide sequences. " &
                "It then searches the specified .fasta or text file containing protein names and sequences " &
                "(and optionally descriptions) to find the proteins that contain each peptide. " &
                "It will also compute the sequence coverage percent for each protein (disable using /K)."))
            Console.WriteLine()
            Console.WriteLine("Program syntax:" & ControlChars.NewLine & Path.GetFileName(Assembly.GetExecutingAssembly().Location))
            Console.WriteLine(" /I:PeptideInputFilePath /R:ProteinInputFilePath")
            Console.WriteLine(" [/O:OutputDirectoryName] [/P:ParameterFilePath] [/F:FileFormatCode] ")
            Console.WriteLine(" [/N:InspectParameterFilePath] [/G] [/H] [/K] [/A]")
            Console.WriteLine(" [/L[:LogFilePath]] [/LogDir:LogDirectoryPath] [/VerboseLog] [/Q]")
            Console.WriteLine()
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                "The input file path can contain the wildcard character *. If a wildcard is present, " &
                "the same protein input file path will be used for each of the peptide input files matched."))
            Console.WriteLine()
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph("The output directory name is optional. " &
                              "If omitted, the output files will be created in the same directory as the input file. " &
                              "If included, then a subdirectory is created with the name OutputDirectoryName."))
            Console.WriteLine()
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph("The parameter file path is optional. " &
                              "If included, it should point to a valid XML parameter file."))
            Console.WriteLine()

            Console.WriteLine("Use /F to specify the peptide input file format code.  Options are:")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.AutoDetermine & "=Auto Determine: Treated as /F:1 unless name ends in _inspect.txt, then /F:3")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.PeptideListFile & "=Peptide sequence in the 1st column (subsequent columns are ignored)")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.ProteinAndPeptideFile & "=Protein name in 1st column and peptide sequence 2nd column")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.InspectResultsFile & "=Inspect search results file (peptide sequence in the 3rd column)")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.MSGFPlusResultsFile & "=MS-GF+ search results file (peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan')")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.PHRPFile & "=SEQUEST, X!Tandem, Inspect, or MS-GF+ PHRP data file")
            Console.WriteLine()

            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                "When processing an Inspect search results file, use /N to specify the Inspect parameter file used " &
                "(required for determining the mod names embedded in the identified peptides)."))
            Console.WriteLine()

            Console.WriteLine("Use /G to ignore I/L differences when finding peptides in proteins or computing coverage")
            Console.WriteLine("Use /H to suppress (hide) the protein sequence in the _coverage.txt file")
            Console.WriteLine("Use /K to skip the protein coverage computation steps (enabling faster processing)")
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                "Use /A to create a copy of the source file, but with a new column listing the mapped protein for each peptide. " &
                "If a peptide maps to multiple proteins, then multiple lines will be listed"))

            Console.WriteLine("Use /L to create a log file, optionally specifying the file name")
            Console.WriteLine("Use /LogDir to define the directory in which the log file should be created")
            Console.WriteLine("Use /VerboseLog to create a detailed log file")
            Console.WriteLine()

            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2008")
            Console.WriteLine("Version: " & GetAppVersion())
            Console.WriteLine()

            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov")
            Console.WriteLine("Website: https://omics.pnl.gov or https://panomics.pnl.gov/")
            Console.WriteLine()

            ' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            Thread.Sleep(750)

        Catch ex As Exception
            ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub

    Private Sub PeptideToProteinMapEngine_StatusEvent(message As String)
        Console.WriteLine(message)
    End Sub

    Private Sub PeptideToProteinMapEngine_WarningEvent(message As String)
        ConsoleMsgUtils.ShowWarning(message)
    End Sub

    Private Sub PeptideToProteinMapEngine_ErrorEvent(message As String, ex As Exception)
        ShowErrorMessage(message)
    End Sub

    Private Sub PeptideToProteinMapEngine_ProgressChanged(taskDescription As String, percentComplete As Single)
        Const PROGRESS_DOT_INTERVAL_MSEC = 250

        If DateTime.UtcNow.Subtract(mLastPercentDisplayed).TotalSeconds >= 15 Then
            Console.WriteLine()

            DisplayProgressPercent(taskDescription, CInt(percentComplete), False)
            mLastPercentDisplayed = DateTime.UtcNow
        Else
            If DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC Then
                mLastProgressReportTime = DateTime.UtcNow
                Console.Write(".")
            End If
        End If

        If Not mVerboseLogFile Is Nothing Then
            If taskDescription Is Nothing Then taskDescription = String.Empty

            If taskDescription = mVerboseLoggingMostRecentMessage Then
                mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab &
                        percentComplete.ToString & ControlChars.Tab &
                        ".")
            Else
                mVerboseLoggingMostRecentMessage = String.Copy(taskDescription)

                mVerboseLogFile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab &
                        percentComplete.ToString & ControlChars.Tab &
                        taskDescription)

            End If
        End If
    End Sub

    Private Sub PeptideToProteinMapEngine_ProgressReset()
        mLastProgressReportTime = DateTime.UtcNow
        mLastPercentDisplayed = DateTime.UtcNow
    End Sub

End Module
