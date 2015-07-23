Option Strict On

' This program uses clsProteinCoverageSummarizer to read in a file with protein sequences along with
' an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
'
' Example command Line
' /I:PeptideInputFilePath /R:ProteinInputFilePath /O:OutputFolderPath /P:ParameterFilePath

' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Program started June 14, 2005
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

Public Module modMain

    Public Const PROGRAM_DATE As String = "July 22, 2015"

	Private mPeptideInputFilePath As String
	Private mProteinInputFilePath As String
	Private mOutputFolderPath As String
	Private mParameterFilePath As String

	Private mIgnoreILDifferences As Boolean
	Private mOutputProteinSequence As Boolean
	Private mSaveProteinToPeptideMappingFile As Boolean
	Private mSkipCoverageComputationSteps As Boolean

	Private mQuietMode As Boolean

	Private WithEvents mProteinCoverageRunner As clsProteinCoverageSummarizerRunner
	Private mLastProgressReportTime As System.DateTime
	Private mLastProgressReportValue As Integer


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

		mIgnoreILDifferences = False
		mOutputProteinSequence = True
		mSaveProteinToPeptideMappingFile = False
		mSkipCoverageComputationSteps = False

		Try
			blnProceed = False
			If objParseCommandLine.ParseCommandLine Then
				If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then blnProceed = True
			End If

			If objParseCommandLine.ParameterCount = 0 And Not objParseCommandLine.NeedToShowHelp Then
				ShowGUI()
			ElseIf Not blnProceed OrElse objParseCommandLine.NeedToShowHelp OrElse objParseCommandLine.ParameterCount = 0 OrElse mPeptideInputFilePath.Length = 0 Then
				ShowProgramHelp()
				intReturnCode = -1
			Else
				Try
					mProteinCoverageRunner = New clsProteinCoverageSummarizerRunner

					With mProteinCoverageRunner
						.ProteinInputFilePath = mProteinInputFilePath
						.ShowMessages = Not mQuietMode
						.CallingAppHandlesEvents = False

						.IgnoreILDifferences = mIgnoreILDifferences
						.OutputProteinSequence = mOutputProteinSequence
						.SaveProteinToPeptideMappingFile = mSaveProteinToPeptideMappingFile
						.SearchAllProteinsSkipCoverageComputationSteps = mSkipCoverageComputationSteps
					End With

					blnSuccess = mProteinCoverageRunner.ProcessFilesWildcard(mPeptideInputFilePath, mOutputFolderPath, mParameterFilePath)

				Catch ex As Exception
					blnSuccess = False
					MsgBox("Error initializing Protein File Parser General Options " & ex.Message)
				End Try

			End If

		Catch ex As Exception
			ShowErrorMessage("Error occurred in modMain->Main: " & System.Environment.NewLine & ex.Message)
			intReturnCode = -1
		End Try

		Return intReturnCode

	End Function

    Private Sub DisplayProgressPercent(intPercentComplete As Integer, blnAddCarriageReturn As Boolean)
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
        If intPercentComplete > 100 Then intPercentComplete = 100
        Console.Write("Processing: " & intPercentComplete.ToString() & "% ")
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
    End Sub

    Private Function GetAppVersion() As String
        Return clsProcessFilesBaseClass.GetAppVersion(PROGRAM_DATE)
    End Function

    ''Private Function GetFilePath(mInputFolderOrFilePath As String) As String
    ''    Dim ioFileInfo As System.IO.FileInfo
    ''    Dim ioFolderInfo As System.IO.DirectoryInfo
    ''    Dim strInputFolderPath As String
    ''    Dim ioPath As System.IO.Path
    ''    Dim strCleanPath As String
    ''    Dim WILDCARD_CHARS As Char() = {"*"c, "?"c}
    ''    Dim strInputFolderOrFilePath As String

    ''    strInputFolderOrFilePath = String.Copy(mInputFolderOrFilePath)

    ''    'check for wild cards
    ''    If (mInputFolderOrFilePath.IndexOf("*") >= 0 Or mInputFolderOrFilePath.IndexOf("?") >= 0) Then
    ''        'if mInputFolderOrFilePath contains a wildcard
    ''        ' Copy the path into strCleanPath and replace any * or ? characters with _
    ''        strCleanPath = mInputFolderOrFilePath.Replace("*", "_")
    ''        strCleanPath = strCleanPath.Replace("?", "_")

    ''        ioFileInfo = New System.IO.FileInfo(strCleanPath)
    ''        If ioFileInfo.Directory.Exists Then
    ''            strInputFolderPath = ioFileInfo.DirectoryName
    ''        Else
    ''            ' Use the current working directory
    ''            strInputFolderPath = ioPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    ''        End If

    ''        strInputFolderOrFilePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(strInputFolderPath), strInputFolderOrFilePath)

    ''    Else
    ''        'user did not provide wildcards at the command line for the input folder
    ''        ioFileInfo = New System.IO.FileInfo(mInputFolderOrFilePath)
    ''        ioFolderInfo = New System.IO.DirectoryInfo(mInputFolderOrFilePath)

    ''        If ioFolderInfo.Exists Then
    ''            'case that we are provided with directory name/path only
    ''            strInputFolderPath = ioFolderInfo.Name
    ''            strInputFolderPath = ioFileInfo.DirectoryName & strInputFolderPath

    ''        ElseIf ioFileInfo.Directory.Exists Then
    ''            'case that we are provided with directory path and a file name
    ''            strInputFolderPath = ioFileInfo.DirectoryName

    ''        Else
    ''            ' Use the current working directory
    ''            strInputFolderPath = ioPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    ''        End If

    ''        strInputFolderOrFilePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(strInputFolderPath), strInputFolderOrFilePath)


    ''        If System.IO.Directory.Exists(mInputFolderOrFilePath) Then
    ''            strInputFolderOrFilePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(mInputFolderOrFilePath), strInputFolderOrFilePath)
    ''        ElseIf mInputFolderOrFilePath.IndexOfAny(WILDCARD_CHARS) >= 0 Then
    ''            ' Wildcards are present; just log in the application folder path
    ''            strInputFolderOrFilePath = System.IO.Path.Combine(ioPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), strInputFolderOrFilePath)
    ''        Else
    ''            ' Path must be to a file
    ''            Dim InputFolderPath As String
    ''            ioFileInfo = New System.IO.FileInfo(mInputFolderOrFilePath)
    ''            strInputFolderOrFilePath = System.IO.Path.Combine(ioFileInfo.DirectoryName, strInputFolderOrFilePath)
    ''        End If

    ''    End If

    ''    Return strInputFolderOrFilePath

    ''End Function


    Private Function SetOptionsUsingCommandLineParameters(objParseCommandLine As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false
        ' /I:PeptideInputFilePath /R: ProteinInputFilePath /O:OutputFolderPath /P:ParameterFilePath

        Dim strValue As String = String.Empty
        Dim lstValidParameters As Generic.List(Of String) = New Generic.List(Of String) From {"I", "O", "R", "P", "G", "H", "M", "K", "Q"}

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
                    If .RetrieveValueForParameter("G", strValue) Then mIgnoreILDifferences = True
                    If .RetrieveValueForParameter("H", strValue) Then mOutputProteinSequence = False
                    If .RetrieveValueForParameter("M", strValue) Then mSaveProteinToPeptideMappingFile = True
                    If .RetrieveValueForParameter("K", strValue) Then mSkipCoverageComputationSteps = True

                    If .RetrieveValueForParameter("Q", strValue) Then mQuietMode = True
                End With

                Return True
            End If

        Catch ex As Exception
            ShowErrorMessage("Error parsing the command line parameters: " & System.Environment.NewLine & ex.Message)
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

    Private Sub ShowGUI()
        Dim objFormMain As GUI

        System.Windows.Forms.Application.EnableVisualStyles()
        System.Windows.Forms.Application.DoEvents()

        Try
            objFormMain = New GUI

            objFormMain.ShowDialog()
        Catch ex As Exception
            MsgBox("Error in ShowGUI: " & ex.Message, MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Error")
        Finally
            objFormMain = Nothing
        End Try

    End Sub

    Private Sub ShowProgramHelp()

        Dim strSyntax As String

        Try
            strSyntax = String.Empty
            strSyntax &= Environment.NewLine & "This program reads in a .fasta or .txt file containing protein names and sequences (and optionally descriptions)"
            strSyntax &= Environment.NewLine & "The program also reads in a .txt file containing peptide sequences and protein names (though protein name is optional) then uses this information to compute the sequence coverage percent for each protein."
            strSyntax &= Environment.NewLine
            strSyntax &= Environment.NewLine & "Program syntax:" & System.Environment.NewLine & IO.Path.GetFileName(clsProcessFilesBaseClass.GetAppPath())
            strSyntax &= Environment.NewLine & " /I:PeptideInputFilePath /R:ProteinInputFilePath [/O:OutputFolderName] [/P:ParameterFilePath] [/G] [/H] [/M] [/K] [/Q]"
            strSyntax &= Environment.NewLine
            strSyntax &= Environment.NewLine & "The input file path can contain the wildcard character *.  If a wildcard is present, then the same protein input file path will be used for each of the peptide input files matched."
            strSyntax &= Environment.NewLine & "The output folder name is optional.  If omitted, the output files will be created in the same folder as the input file.  If included, then a subfolder is created with the name OutputFolderName."
            strSyntax &= Environment.NewLine
            strSyntax &= Environment.NewLine & "The parameter file path is optional.  If included, it should point to a valid XML parameter file."
            strSyntax &= Environment.NewLine
            strSyntax &= Environment.NewLine & "Use /G to ignore I/L differences when finding peptides in proteins or computing coverage."
            strSyntax &= Environment.NewLine & "Use /H to suppress (hide) the protein sequence in the _coverage.txt file."
            strSyntax &= Environment.NewLine & "Use /M to enable the creation of a protein to peptide mapping file."
            strSyntax &= Environment.NewLine

            strSyntax &= Environment.NewLine & "Program written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA) in 2005"
            strSyntax &= Environment.NewLine & "Version: " & GetAppVersion()
            strSyntax &= Environment.NewLine

            strSyntax &= Environment.NewLine & "E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com"
            strSyntax &= Environment.NewLine & "Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/"
            strSyntax &= Environment.NewLine

            If mQuietMode Then
                Console.WriteLine(strSyntax)
            Else
                System.Windows.Forms.MessageBox.Show(strSyntax, "Syntax", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub

    Private Sub WriteToErrorStream(strErrorMessage As String)
        Try
            Using swErrorStream As System.IO.StreamWriter = New System.IO.StreamWriter(Console.OpenStandardError())
                swErrorStream.WriteLine(strErrorMessage)
            End Using
        Catch ex As Exception
            ' Ignore errors here
        End Try
    End Sub

    Private Sub mProteinCoverageRunner_ProgressChanged(taskDescription As String, percentComplete As Single) Handles mProteinCoverageRunner.ProgressChanged
        Const PERCENT_REPORT_INTERVAL As Integer = 25
        Const PROGRESS_DOT_INTERVAL_MSEC As Integer = 250

        If percentComplete >= mLastProgressReportValue Then
            If mLastProgressReportValue > 0 Then
                Console.WriteLine()
            End If
            DisplayProgressPercent(mLastProgressReportValue, False)
            mLastProgressReportValue += PERCENT_REPORT_INTERVAL
            mLastProgressReportTime = DateTime.UtcNow
        Else
            If DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC Then
                mLastProgressReportTime = DateTime.UtcNow
                Console.Write(".")
            End If
        End If
    End Sub

    Private Sub mProteinCoverageRunner_ProgressReset() Handles mProteinCoverageRunner.ProgressReset
        mLastProgressReportTime = DateTime.UtcNow
        mLastProgressReportValue = 0
    End Sub
End Module
