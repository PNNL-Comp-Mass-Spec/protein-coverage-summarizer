Option Strict On

' This program uses PeptideToProteinMapEngine.dll to read in a file with peptide sequences, then
' searches for the given peptides in a protein sequence file (.Fasta or tab-delimited text)
' using ProteinCoverageSummarizer.dll
'
' This program is similar to the ProteinCoverageSummarizer, but it is a console-only application
' In addition, this program supports reading Inspect output files
'
' Example command Line
' /I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputFolderPath /P:ParameterFilePath

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started September 27, 2008
'
' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://omics.pnl.gov/ or http://www.sysbio.org/resources/staff/ or http://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute, 
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
' Department of Energy (DOE).  All rights in the computer software are reserved 
' by DOE on behalf of the United States Government and the Contractor as 
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
' SOFTWARE.  This notice including this sentence must appear on any copies of 
' this computer software.

Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports PeptideToProteinMapEngine
Imports PeptideToProteinMapEngine.clsPeptideToProteinMapEngine

Public Module modMain

    Public Const PROGRAM_DATE As String = "July 27, 2016"

    Private mPeptideInputFilePath As String
    Private mProteinInputFilePath As String
    Private mOutputFolderPath As String
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
    Private mLogFolderPath As String = String.Empty

    Private mQuietMode As Boolean

    Private mVerboseLogging As Boolean
    Private mVerboseLogFile As StreamWriter
    Private mVerboseLoggingMostRecentMessage As String = String.Empty

    Private WithEvents mPeptideToProteinMapEngine As clsPeptideToProteinMapEngine
    Private mLastProgressReportTime As DateTime
    Private mLastPercentDisplayed As DateTime

    Private Sub CreateVerboseLogFile()
        Dim strLogFilePath As String
        Dim blnOpeningExistingFile As Boolean

        Try
            strLogFilePath = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)
            strLogFilePath &= "_VerboseLog_" & DateTime.Now.ToString("yyyy-MM-dd") & ".txt"

            blnOpeningExistingFile = File.Exists(strLogFilePath)

            mVerboseLogFile = New StreamWriter(New FileStream(strLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            mVerboseLogFile.AutoFlush = True

            If Not blnOpeningExistingFile Then
                mVerboseLogFile.WriteLine("Date" & ControlChars.Tab & "Percent Complete" & ControlChars.Tab & "Message")
            End If

            mVerboseLoggingMostRecentMessage = String.Empty

        Catch ex As Exception
            Console.WriteLine()
            Console.WriteLine("----------------------------------------------------")
            Console.WriteLine("Error creating verbose log file: " & ex.Message)
            Console.WriteLine("----------------------------------------------------")
        End Try

    End Sub

    Public Function Main() As Integer
        ' Returns 0 if no error, error code if an error
        Dim intReturnCode As Integer
        Dim objParseCommandLine As New clsParseCommandLine
        Dim blnProceed As Boolean
        Dim blnSuccess As Boolean

        intReturnCode = 0
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

        mQuietMode = False
        mLogMessagesToFile = False
        mLogFilePath = String.Empty
        mLogFolderPath = String.Empty

        Try
            blnProceed = False
            If objParseCommandLine.ParseCommandLine Then
                If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then blnProceed = True
            End If

            If Not blnProceed OrElse
               objParseCommandLine.NeedToShowHelp OrElse
               objParseCommandLine.ParameterCount + objParseCommandLine.NonSwitchParameterCount = 0 Then
                ShowProgramHelp()
                intReturnCode = -1
            Else
                Try
                    If mVerboseLogging Then
                        CreateVerboseLogFile()
                    End If

                    If String.IsNullOrWhiteSpace(mPeptideInputFilePath) Then
                        ShowErrorMessage("Peptide input file must be defined via /I (or by listing the filename just after the .exe)")
                        intReturnCode = -1
                        Exit Try
                    ElseIf String.IsNullOrWhiteSpace(mProteinInputFilePath) Then
                        ShowErrorMessage("Protein input file must be defined via /R")
                        intReturnCode = -1
                        Exit Try
                    End If

                    mPeptideToProteinMapEngine = New clsPeptideToProteinMapEngine

                    With mPeptideToProteinMapEngine
                        .ProteinInputFilePath = mProteinInputFilePath
                        .ShowMessages = Not mQuietMode
                        .LogMessagesToFile = mLogMessagesToFile
                        .LogFilePath = mLogFilePath
                        .LogFolderPath = mLogFolderPath

                        .PeptideInputFileFormat = mInputFileFormatCode
                        .InspectParameterFilePath = mInspectParameterFilePath

                        .IgnoreILDifferences = mIgnoreILDifferences
                        .OutputProteinSequence = mOutputProteinSequence
                        .SaveProteinToPeptideMappingFile = mSaveProteinToPeptideMappingFile
                        .SaveSourceDataPlusProteinsFile = mSaveSourceDataPlusProteinsFile

                        .SearchAllProteinsSkipCoverageComputationSteps = mSkipCoverageComputationSteps
                    End With

                    blnSuccess = mPeptideToProteinMapEngine.ProcessFilesWildcard(mPeptideInputFilePath, mOutputFolderPath, mParameterFilePath)

                    If Not mVerboseLogFile Is Nothing Then
                        mVerboseLogFile.Close()
                    End If

                Catch ex As Exception
                    blnSuccess = False
                    ShowErrorMessage("Error initializing the Peptide to Protein Mapper Options " & ex.Message)
                End Try

            End If

        Catch ex As Exception
            ShowErrorMessage("Error occurred in modMain->Main: " & Environment.NewLine & ex.Message)
            intReturnCode = -1
        End Try

        Return intReturnCode

    End Function

    Private Sub DisplayProgressPercent(taskDescription As String, intPercentComplete As Integer, blnAddCarriageReturn As Boolean)
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
        If intPercentComplete > 100 Then intPercentComplete = 100
        If String.IsNullOrEmpty(taskDescription) Then taskDescription = "Processing"

        Console.Write(taskDescription & ": " & intPercentComplete.ToString() & "% ")
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
    End Sub

    Private Function GetAppVersion() As String
        Return clsProcessFilesBaseClass.GetAppVersion(PROGRAM_DATE)
    End Function

    Private Function SetOptionsUsingCommandLineParameters(objParseCommandLine As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false
        ' /I:PeptideInputFilePath /R: ProteinInputFilePath /O:OutputFolderPath /P:ParameterFilePath

        Dim strValue As String = String.Empty
        Dim lstValidParameters As List(Of String) = New List(Of String) From {"I", "O", "R", "P", "F", "N", "G", "H", "K", "A", "L", "LogFolder", "Q", "VerboseLog"}
        Dim intValue As Integer

        Try
            ' Make sure no invalid parameters are present 
            If objParseCommandLine.InvalidParametersPresent(lstValidParameters) Then
                ShowErrorMessage("Invalid commmand line parameters",
                  (From item In objParseCommandLine.InvalidParameters(lstValidParameters) Select "/" + item).ToList())
                Return False
            Else
                With objParseCommandLine
                    ' Query objParseCommandLine to see if various parameters are present
                    If .RetrieveValueForParameter("I", strValue) Then
                        mPeptideInputFilePath = strValue
                    ElseIf .NonSwitchParameterCount > 0 Then
                        mPeptideInputFilePath = .RetrieveNonSwitchParameter(0)
                    End If


                    If .RetrieveValueForParameter("O", strValue) Then mOutputFolderPath = strValue
                    If .RetrieveValueForParameter("R", strValue) Then mProteinInputFilePath = strValue
                    If .RetrieveValueForParameter("P", strValue) Then mParameterFilePath = strValue

                    If .RetrieveValueForParameter("F", strValue) Then
                        If Integer.TryParse(strValue, intValue) Then
                            Try
                                mInputFileFormatCode = CType(intValue, ePeptideInputFileFormatConstants)
                            Catch ex As Exception
                                ' Conversion failed; leave mInputFileFormatCode unchanged
                            End Try
                        End If
                    End If

                    If .RetrieveValueForParameter("N", strValue) Then mInspectParameterFilePath = strValue

                    If .RetrieveValueForParameter("G", strValue) Then mIgnoreILDifferences = True
                    If .RetrieveValueForParameter("H", strValue) Then mOutputProteinSequence = False
                    If .RetrieveValueForParameter("K", strValue) Then mSkipCoverageComputationSteps = True

                    If .RetrieveValueForParameter("A", strValue) Then mSaveSourceDataPlusProteinsFile = True

                    If .RetrieveValueForParameter("L", strValue) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(strValue) Then
                            mLogFilePath = strValue
                        End If
                    End If

                    If .RetrieveValueForParameter("LogFolder", strValue) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(strValue) Then
                            mLogFolderPath = strValue
                        End If
                    End If
                    If .RetrieveValueForParameter("Q", strValue) Then mQuietMode = True

                    If .RetrieveValueForParameter("VerboseLog", strValue) Then mVerboseLogging = True
                End With

                Return True
            End If

        Catch ex As Exception
            ShowErrorMessage("Error parsing the command line parameters: " & Environment.NewLine & ex.Message)
        End Try

        Return False

    End Function

    Private Sub ShowErrorMessage(strMessage As String)
        Dim strSeparator As String = "------------------------------------------------------------------------------"

        Console.WriteLine()
        Console.WriteLine(strSeparator)
        Console.WriteLine(strMessage)
        Console.WriteLine(strSeparator)
        Console.WriteLine()

        WriteToErrorStream(strMessage)
    End Sub

    Private Sub ShowErrorMessage(strTitle As String, items As List(Of String))
        Dim strSeparator As String = "------------------------------------------------------------------------------"
        Dim strMessage As String

        Console.WriteLine()
        Console.WriteLine(strSeparator)
        Console.WriteLine(strTitle)
        strMessage = strTitle & ":"

        For Each item As String In items
            Console.WriteLine("   " + item)
            strMessage &= " " & item
        Next
        Console.WriteLine(strSeparator)
        Console.WriteLine()

        WriteToErrorStream(strMessage)
    End Sub

    Private Sub ShowProgramHelp()

        Try
            Console.WriteLine("This program reads in a text file containing peptide sequences.  It then searches the specified .fasta or text file containing protein names and sequences (and optionally descriptions)")
            Console.WriteLine("to find the proteins that contain each peptide.  It will also compute the sequence coverage percent for each protein (disable using /K).")
            Console.WriteLine()
            Console.WriteLine("Program syntax:" & ControlChars.NewLine & Path.GetFileName(Assembly.GetExecutingAssembly().Location) &
             " /I:PeptideInputFilePath /R:ProteinInputFilePath")
            Console.WriteLine(" [/O:OutputFolderName] [/P:ParameterFilePath] [/F:FileFormatCode] ")
            Console.WriteLine(" [/N:InspectParameterFilePath] [/G] [/H] [/K] [/A]")
            Console.WriteLine(" [/L[:LogFilePath]] [/LogFolder:LogFolderPath] [/VerboseLog] [/Q]")
            Console.WriteLine()
            Console.WriteLine("The input file path can contain the wildcard character *.  If a wildcard is present, then the same protein input file path will be used for each of the peptide input files matched.")
            Console.WriteLine("The output folder name is optional.  If omitted, the output files will be created in the same folder as the input file.  If included, then a subfolder is created with the name OutputFolderName.")
            Console.WriteLine("The parameter file path is optional.  If included, it should point to a valid XML parameter file.")
            Console.WriteLine()

            Console.WriteLine("Use /F to specify the peptide input file format code.  Options are:")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.AutoDetermine & "=Auto Determine: Treated as /F:1 unless name ends in _inspect.txt, then /F:3")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.PeptideListFile & "=Peptide sequence in the 1st column (subsequent columns are ignored)")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.ProteinAndPeptideFile & "=Protein name in 1st column and peptide sequence 2nd column")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.InspectResultsFile & "=Inspect search results file (peptide sequence in the 3rd column)")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.MSGFDBResultsFile & "=MS-GF+ search results file (peptide sequence in the column titled 'Peptide'; optionally scan number in the column titled 'Scan')")
            Console.WriteLine("   " & ePeptideInputFileFormatConstants.PHRPFile & "=Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file")
            Console.WriteLine()

            Console.WriteLine("When processing an Inspect search results file, use /N to specify the Inspect parameter file used (required for determining the mod names embedded in the identified peptides).")
            Console.WriteLine()

            Console.WriteLine("Use /G to ignore I/L differences when finding peptides in proteins or computing coverage")
            Console.WriteLine("Use /H to suppress (hide) the protein sequence in the _coverage.txt file")
            Console.WriteLine("Use /K to skip the protein coverage computation steps (enabling faster processing)")
            Console.WriteLine("Use /A to create a copy of the source file, but with a new column listing the mapped protein for each peptide.  If a peptide maps to multiple proteins, then multiple lines will be listed")

            Console.WriteLine("Use /L to create a log file, optionally specifying the file name")
            Console.WriteLine("Use /LogFolder to define the folder in which the log file should be created")
            Console.WriteLine("Use /VerboseLog to create a detailed log file")
            Console.WriteLine("Use /Q to suppress any error messages")
            Console.WriteLine()

            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2008")
            Console.WriteLine("Version: " & GetAppVersion())
            Console.WriteLine()

            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com")
            Console.WriteLine("Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/")
            Console.WriteLine()

            ' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            Thread.Sleep(750)

        Catch ex As Exception
            ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub

    Private Sub WriteToErrorStream(strErrorMessage As String)
        Try
            Using swErrorStream As StreamWriter = New StreamWriter(Console.OpenStandardError())
                swErrorStream.WriteLine(strErrorMessage)
            End Using
        Catch ex As Exception
            ' Ignore errors here
        End Try
    End Sub

    Private Sub mPeptideToProteinMapEngine_ProgressChanged(taskDescription As String, percentComplete As Single) Handles mPeptideToProteinMapEngine.ProgressChanged
        Const PROGRESS_DOT_INTERVAL_MSEC As Integer = 250

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

	Private Sub mPeptideToProteinMapEngine_ProgressReset() Handles mPeptideToProteinMapEngine.ProgressReset
		mLastProgressReportTime = DateTime.UtcNow
		mLastPercentDisplayed = DateTime.UtcNow
	End Sub
End Module
