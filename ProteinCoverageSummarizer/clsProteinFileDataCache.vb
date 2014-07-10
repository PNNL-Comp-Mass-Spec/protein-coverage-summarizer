Option Strict On

' This class will read a protein fasta file or delimited protein info file and 
' store the proteins in memory
'
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
'
' Started June 2005

<CLSCompliant(True)>
Public Class clsProteinFileDataCache

    Public Sub New()
		mFileDate = "January 29, 2014"
        InitializeLocalVariables()
    End Sub

#Region "Constants and Enums"

    Private Const OPTIONS_SECTION As String = "ProteinFileParsingOptions"
    Private Const XML_SECTION_PROTEIN_FILE_OPTIONS As String = "ProteinFileOptions"
    Private Const XML_SECTION_PROCESSING_OPTIONS As String = "ProcessingOptions"
    Private Const XML_SECTION_GUI_OPTIONS As String = "GUIOptions"

    Private Const PROTEIN_CACHE_MEMORY_RESERVE_COUNT As Integer = 500

    Private Const SCRAMBLING_CACHE_LENGTH As Integer = 4000
    Private Const PROTEIN_PREFIX_SCRAMBLED As String = "Scrambled_"
    Private Const PROTEIN_PREFIX_REVERSED As String = "Reversed_"

    Private Const MAXIMUM_PROTEIN_NAME_LENGTH As Integer = 34

    Private Const MAX_ABBREVIATED_FILENAME_LENGTH As Integer = 15

    Protected Const SQL_LITE_PROTEIN_CACHE_FILENAME As String = "tmpProteinInfoCache.db3"

    Public Enum DelimiterCharConstants
        Space = 0
        Tab = 1
        Comma = 2
        Other = 3
    End Enum

#End Region

#Region "Structures"
    Public Structure udtProteinInfoType
        Public Name As String
        Public Description As String
        Public Sequence As String
        Public UniqueSequenceID As Integer              ' Index number applied to the proteins stored in the SQL Lite DB; the first protein has UniqueSequenceID = 0
        Public PercentCoverage As Double
    End Structure

#End Region

#Region "Classwide Variables"
    Protected mFileDate As String
    Protected mShowMessages As Boolean
    Private mStatusMessage As String

    Private mAssumeDelimitedFile As Boolean
    Private mAssumeFastaFile As Boolean
    Private mDelimitedInputFileDelimiter As Char                              ' Only used for delimited protein input files, not for fasta files
    Private mDelimitedInputFileFormatCode As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode
    Private mDelimitedFileSkipFirstLine As Boolean

    Public FastaFileOptions As FastaFileOptionsClass
    Private mObjectVariablesLoaded As Boolean

    Private mProteinCount As Integer
    Private mParsedFileIsFastaFile As Boolean

    Private mRemoveSymbolCharacters As Boolean
    Private mChangeProteinSequencesToLowercase As Boolean
    Private mChangeProteinSequencesToUppercase As Boolean
    Private mIgnoreILDifferences As Boolean

    ' Sql Lite Connection String and filepath
    Private mSqlLiteDBConnectionString As String = String.Empty
    Private mSqlLiteDBFilePath As String = SQL_LITE_PROTEIN_CACHE_FILENAME

	Private mSqlLitePersistentConnection As SQLite.SQLiteConnection

	Public Event ProteinCachingStart()
	Public Event ProteinCached(ByVal intProteinsCached As Integer)
	Public Event ProteinCachedWithProgress(ByVal intProteinsCached As Integer, ByVal sngPercentFileProcessed As Single)
	Public Event ProteinCachingComplete()

#End Region

#Region "Processing Options Interface Functions"
	Public Property AssumeDelimitedFile() As Boolean
		Get
			Return mAssumeDelimitedFile
		End Get
		Set(ByVal Value As Boolean)
			mAssumeDelimitedFile = Value
		End Set
	End Property

	Public Property AssumeFastaFile() As Boolean
		Get
			Return mAssumeFastaFile
		End Get
		Set(ByVal Value As Boolean)
			mAssumeFastaFile = Value
		End Set
	End Property

	Public Property ChangeProteinSequencesToLowercase() As Boolean
		Get
			Return mChangeProteinSequencesToLowercase
		End Get
		Set(ByVal Value As Boolean)
			mChangeProteinSequencesToLowercase = Value
		End Set
	End Property

	Public Property ChangeProteinSequencesToUppercase() As Boolean
		Get
			Return mChangeProteinSequencesToUppercase
		End Get
		Set(ByVal Value As Boolean)
			mChangeProteinSequencesToUppercase = Value
		End Set
	End Property

	Public Property DelimitedFileFormatCode() As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode
		Get
			Return mDelimitedInputFileFormatCode
		End Get
		Set(ByVal Value As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode)
			mDelimitedInputFileFormatCode = Value
		End Set
	End Property

	Public Property DelimitedFileDelimiter() As Char
		Get
			Return mDelimitedInputFileDelimiter
		End Get
		Set(ByVal Value As Char)
			If Not Value = Nothing Then
				mDelimitedInputFileDelimiter = Value
			End If
		End Set
	End Property

	Public Property DelimitedFileSkipFirstLine() As Boolean
		Get
			Return mDelimitedFileSkipFirstLine
		End Get
		Set(ByVal Value As Boolean)
			mDelimitedFileSkipFirstLine = Value
		End Set
	End Property

	Public Property IgnoreILDifferences() As Boolean
		Get
			Return mIgnoreILDifferences
		End Get
		Set(ByVal Value As Boolean)
			mIgnoreILDifferences = Value
		End Set
	End Property

	Public ReadOnly Property ParsedFileIsFastaFile() As Boolean
		Get
			Return mParsedFileIsFastaFile
		End Get
	End Property

	Public Property RemoveSymbolCharacters() As Boolean
		Get
			Return mRemoveSymbolCharacters
		End Get
		Set(ByVal Value As Boolean)
			mRemoveSymbolCharacters = Value
		End Set
	End Property

	Public Property ShowMessages() As Boolean
		Get
			Return mShowMessages
		End Get
		Set(ByVal Value As Boolean)
			Value = mShowMessages
		End Set
	End Property

	Public ReadOnly Property StatusMessage() As String
		Get
			Return mStatusMessage
		End Get
	End Property

#End Region

	Public Function ConnectToSqlLiteDB(ByVal blnDisableJournalling As Boolean) As SQLite.SQLiteConnection

		If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
			Return Nothing
		End If

		Dim SQLconnect As SQLite.SQLiteConnection = New SQLite.SQLiteConnection(mSqlLiteDBConnectionString, True)
		SQLconnect.Open()

		' Turn off Journaling and set Synchronous mode to 0
		' These changes are required to improve the update speed
		If blnDisableJournalling Then

			Using SQLcommand As SQLite.SQLiteCommand = SQLconnect.CreateCommand
				SQLcommand.CommandText = "PRAGMA journal_mode = OFF"
				SQLcommand.ExecuteNonQuery()
				SQLcommand.CommandText = "PRAGMA synchronous = 0"
				SQLcommand.ExecuteNonQuery()
			End Using
		End If

		Return SQLconnect

	End Function

	Private Function DefineSqlLiteDBPath(ByVal strSqlLiteDBFileName As String) As String
		Dim strDBPath As String
		Dim strFolderPath As String = String.Empty
		Dim strFilePath As String = String.Empty

		Dim swTestFile As System.IO.StreamWriter
		Dim blnSuccess As Boolean = False

		Try
			' See if we can create files in the folder that contains this .Dll
			strFolderPath = clsProteinCoverageSummarizer.GetAppFolderPath()

			strFilePath = System.IO.Path.Combine(strFolderPath, "TempFileToTestFileIOPermissions.tmp")

			swTestFile = New System.IO.StreamWriter(New System.IO.FileStream(strFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))
			swTestFile.WriteLine("Test")
			swTestFile.Close()
			blnSuccess = True

		Catch ex As Exception
			' Error creating file; user likely doesn't have write-access
			blnSuccess = False
		End Try

		If Not blnSuccess Then
			Try
				' Create a randomly named file in the user's temp folder
				strFilePath = System.IO.Path.GetTempFileName
				strFolderPath = System.IO.Path.GetDirectoryName(strFilePath)
				blnSuccess = True

			Catch ex As Exception
				' Error creating temp file; user likely doesn't have write-access anywhere on the disk
				blnSuccess = False
			End Try
		End If

		If blnSuccess Then
			Try
				' Delete the temporary file
				System.IO.File.Delete(strFilePath)
			Catch ex As Exception
				' Ignore errors here
			End Try
		End If

		If blnSuccess Then
			strDBPath = System.IO.Path.Combine(strFolderPath, strSqlLiteDBFileName)
		Else
			strDBPath = strSqlLiteDBFileName
		End If

		Return strDBPath

	End Function

	Public Sub DeleteSQLiteDBFile()
		Const MAX_RETRY_ATTEMPT_COUNT As Integer = 3

		Dim intRetryIndex As Integer
		Dim intRetryHoldoffSeconds As Integer

		Try
			If Not mSqlLitePersistentConnection Is Nothing Then
				mSqlLitePersistentConnection.Close()
			End If
		Catch ex As Exception
			' Ignore errors here
		End Try

		Try

			If String.IsNullOrEmpty(mSqlLiteDBFilePath) Then
				' SqlLiteDBFilePath is not defined or is empty; nothing to do
				Exit Sub
			ElseIf Not System.IO.File.Exists(mSqlLiteDBFilePath) Then
				' File doesn't exist; nothing to do
				Exit Sub
			End If

			' Call the garbage collector to dispose of the SQLite objects
			GC.Collect()
			System.Threading.Thread.Sleep(500)

		Catch ex As Exception
			' Ignore errors here
		End Try

		For intRetryIndex = 0 To MAX_RETRY_ATTEMPT_COUNT - 1
			intRetryHoldoffSeconds = (intRetryIndex + 1)

			Try
				If Not String.IsNullOrEmpty(mSqlLiteDBFilePath) Then
					If System.IO.File.Exists(mSqlLiteDBFilePath) Then
						System.IO.File.Delete(mSqlLiteDBFilePath)
					End If
				End If

				If intRetryIndex > 1 Then
					Console.WriteLine(" --> File now successfully deleted")
				End If

				' If we get here, the delete succeeded
				Exit For

			Catch ex As Exception
				If intRetryIndex > 0 Then
					Console.WriteLine()
					Console.WriteLine("Error deleting " & mSqlLiteDBFilePath & ": " & ControlChars.NewLine & ex.Message)
					Console.WriteLine()
					Console.WriteLine("  Waiting " & intRetryHoldoffSeconds & " seconds, then trying again")
				End If
			End Try

			GC.Collect()
			System.Threading.Thread.Sleep(intRetryHoldoffSeconds * 1000)
		Next

	End Sub

	Public Function GetProteinCountCached() As Integer
		Return mProteinCount
	End Function

	Public Function GetCachedProtein(ByVal intIndex As Integer) As udtProteinInfoType
		If intIndex >= 0 And intIndex < mProteinCount Then
			Return GetCachedProteinFromSQLiteDB(intIndex)
		Else
			Return New udtProteinInfoType
		End If
	End Function

	Protected Function GetCachedProteinFromSQLiteDB(ByVal intIndex As Integer) As udtProteinInfoType
		Dim udtProteinInfo As udtProteinInfoType = New udtProteinInfoType

		If mSqlLitePersistentConnection Is Nothing Then
			mSqlLitePersistentConnection = ConnectToSqlLiteDB(False)
		End If

		Dim SQLcommand As SQLite.SQLiteCommand
		SQLcommand = mSqlLitePersistentConnection.CreateCommand
		SQLcommand.CommandText = "SELECT * FROM udtProteinInfoType WHERE UniqueSequenceID = " & intIndex.ToString

		Dim SQLreader As SQLite.SQLiteDataReader
		SQLreader = SQLcommand.ExecuteReader()

		If SQLreader.Read() Then
			' Column names in table udtProteinInfoType:
			'  Name TEXT, 
			'  Description TEXT, 
			'  Sequence TEXT, 
			'  UniqueSequenceID INTEGER, 
			'  PercentCoverage REAL, 
			'  NonUniquePeptideCount INTEGER, 
			'  UniquePeptideCount INTEGER

			With udtProteinInfo
				.UniqueSequenceID = CInt(SQLreader("UniqueSequenceID"))

				.Name = CStr(SQLreader("Name"))
				.PercentCoverage = CDbl(SQLreader("PercentCoverage"))
				.Description = CStr(SQLreader("Description"))

				.Sequence = CStr(SQLreader("Sequence"))
			End With

		End If

		SQLreader.Close()

		Return udtProteinInfo

	End Function

	Public Function GetSQLiteDataReader(ByVal strSQLQuery As String) As SQLite.SQLiteDataReader

		If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
			Return Nothing
		End If

		Dim SQLconnect = ConnectToSqlLiteDB(False)

		Dim SQLcommand As SQLite.SQLiteCommand
		SQLcommand = SQLconnect.CreateCommand
		SQLcommand.CommandText = strSQLQuery

		Dim SQLreader As SQLite.SQLiteDataReader
		SQLreader = SQLcommand.ExecuteReader()

		Return SQLreader

	End Function

	Private Sub InitializeLocalVariables()
		Const MAX_FILE_CREATE_ATTEMPTS As Integer = 10

		Dim intFileAttemptCount As Integer
		Dim blnSuccess As Boolean

		mAssumeDelimitedFile = False
		mAssumeFastaFile = False

		mDelimitedInputFileDelimiter = ControlChars.Tab
		mDelimitedInputFileFormatCode = ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence
		mDelimitedFileSkipFirstLine = False

		FastaFileOptions = New FastaFileOptionsClass

		mProteinCount = 0

		mRemoveSymbolCharacters = True

		mChangeProteinSequencesToLowercase = False
		mChangeProteinSequencesToUppercase = False

		mIgnoreILDifferences = False

		intFileAttemptCount = 0
		blnSuccess = False
		Do While Not blnSuccess AndAlso intFileAttemptCount < MAX_FILE_CREATE_ATTEMPTS

			' Define the path to the Sql Lite database
			If intFileAttemptCount = 0 Then
				mSqlLiteDBFilePath = DefineSqlLiteDBPath(SQL_LITE_PROTEIN_CACHE_FILENAME)
			Else
				mSqlLiteDBFilePath = DefineSqlLiteDBPath(System.IO.Path.GetFileNameWithoutExtension(SQL_LITE_PROTEIN_CACHE_FILENAME) & _
														 intFileAttemptCount.ToString & _
														 System.IO.Path.GetExtension(SQL_LITE_PROTEIN_CACHE_FILENAME))
			End If

			Try
				' If the file exists, we need to delete it
				If System.IO.File.Exists(mSqlLiteDBFilePath) Then
					System.IO.File.Delete(mSqlLiteDBFilePath)
				End If

				If Not System.IO.File.Exists(mSqlLiteDBFilePath) Then
					blnSuccess = True
				End If

			Catch ex As Exception
				' Error deleting the file
			End Try

			intFileAttemptCount += 1
		Loop

		mSqlLiteDBConnectionString = "Data Source=" & mSqlLiteDBFilePath & ";"

	End Sub

	Public Shared Function IsFastaFile(ByVal strFilePath As String) As Boolean
		' Examines the file's extension and true if it ends in .fasta

		If System.IO.Path.GetFileName(strFilePath).ToLower.IndexOf(".fasta") > 0 Then
			Return True
		Else
			Return False
		End If

	End Function

	Public Function ParseProteinFile(ByVal strProteinInputFilePath As String) As Boolean
		' If strOutputFileNameBaseOverride is defined, then uses that name for the protein output filename rather than auto-defining the name

		Dim objProteinFileReader As ProteinFileReader.ProteinFileReaderBaseClass = Nothing
		Dim objFastaFileReader As ProteinFileReader.FastaFileReader
		Dim objDelimitedFileReader As ProteinFileReader.DelimitedFileReader
		Dim blnSuccess As Boolean = False
		Dim blnInputProteinFound As Boolean
		Dim intProteinsProcessed As Integer
		Dim intInputFileLinesRead As Integer

		Dim Name As String
		Dim Description As String
		Dim Sequence As String

		Dim SQLcommand As SQLite.SQLiteCommand
		Dim SQLTransaction As SQLite.SQLiteTransaction

		' Create the SQL Lite DB
		Dim SQLconnect = ConnectToSqlLiteDB(True)

		' SQL query to Create the Table
		SQLcommand = SQLconnect.CreateCommand
		SQLcommand.CommandText = "CREATE TABLE udtProteinInfoType( " & _
									"Name TEXT, " & _
									"Description TEXT, " & _
									"Sequence TEXT, " & _
									"UniqueSequenceID INTEGER PRIMARY KEY, " & _
									"PercentCoverage REAL);" ', NonUniquePeptideCount INTEGER, UniquePeptideCount INTEGER);"
		SQLcommand.ExecuteNonQuery()

		Dim reReplaceSymbols As System.Text.RegularExpressions.Regex

		' Define a RegEx to replace all of the non-letter characters
		reReplaceSymbols = New System.Text.RegularExpressions.Regex("[^A-Za-z]", System.Text.RegularExpressions.RegexOptions.Compiled)

		Dim split As String() = Nothing

		Try

			If strProteinInputFilePath Is Nothing OrElse strProteinInputFilePath.Length = 0 Then
				mStatusMessage = "Empty protein input file path"
				blnSuccess = False
			Else

				If mAssumeFastaFile OrElse IsFastaFile(strProteinInputFilePath) Then
					If mAssumeDelimitedFile Then
						mParsedFileIsFastaFile = False
					Else
						mParsedFileIsFastaFile = True
					End If
				Else
					mParsedFileIsFastaFile = False
				End If

				If mParsedFileIsFastaFile Then
					objFastaFileReader = New ProteinFileReader.FastaFileReader
					With objFastaFileReader
						.ProteinLineStartChar = FastaFileOptions.ProteinLineStartChar
						.ProteinLineAccessionEndChar = FastaFileOptions.ProteinLineAccessionEndChar
					End With
					objProteinFileReader = objFastaFileReader
				Else
					objDelimitedFileReader = New ProteinFileReader.DelimitedFileReader
					With objDelimitedFileReader
						.Delimiter = mDelimitedInputFileDelimiter
						.DelimitedFileFormatCode = mDelimitedInputFileFormatCode
						.SkipFirstLine = mDelimitedFileSkipFirstLine
					End With
					objProteinFileReader = objDelimitedFileReader
				End If

				' Verify that the input file exists
				If Not System.IO.File.Exists(strProteinInputFilePath) Then
					mStatusMessage = "Protein input file not found: " & strProteinInputFilePath
					blnSuccess = False
					Exit Try
				End If

				' Attempt to open the input file
				If Not objProteinFileReader.OpenFile(strProteinInputFilePath) Then
					mStatusMessage = "Error opening protein input file: " & strProteinInputFilePath
					blnSuccess = False
					Exit Try
				End If

				blnSuccess = True
			End If

		Catch ex As Exception
			mStatusMessage = "Error opening protein input file (" & strProteinInputFilePath & "): " & ex.Message
			blnSuccess = False
		End Try

		' Abort processing if we couldn't successfully open the input file
		If Not blnSuccess Then Return False

		Try
			' Read each protein in the input file and process appropriately
			mProteinCount = 0

			RaiseEvent ProteinCachingStart()

			' Create a parameterized Insert query
			SQLcommand.CommandText = " INSERT INTO udtProteinInfoType(Name, Description, Sequence, UniqueSequenceID, PercentCoverage) " & _
									 " VALUES (?, ?, ?, ?, ?)"

			Dim NameFld As SQLite.SQLiteParameter = SQLcommand.CreateParameter
			Dim DescriptionFld As SQLite.SQLiteParameter = SQLcommand.CreateParameter
			Dim SequenceFld As SQLite.SQLiteParameter = SQLcommand.CreateParameter
			Dim UniqueSequenceIDFld As SQLite.SQLiteParameter = SQLcommand.CreateParameter
			Dim PercentCoverageFld As SQLite.SQLiteParameter = SQLcommand.CreateParameter
			SQLcommand.Parameters.Add(NameFld)
			SQLcommand.Parameters.Add(DescriptionFld)
			SQLcommand.Parameters.Add(SequenceFld)
			SQLcommand.Parameters.Add(UniqueSequenceIDFld)
			SQLcommand.Parameters.Add(PercentCoverageFld)

			' Begin a SQL Transaction
			SQLTransaction = SQLconnect.BeginTransaction()

			Do
				blnInputProteinFound = objProteinFileReader.ReadNextProteinEntry()

				If blnInputProteinFound Then
					intProteinsProcessed += 1
					intInputFileLinesRead = objProteinFileReader.LinesRead

					Name = objProteinFileReader.ProteinName
					Description = objProteinFileReader.ProteinDescription

					If RemoveSymbolCharacters Then
						Sequence = reReplaceSymbols.Replace(objProteinFileReader.ProteinSequence, String.Empty)
					Else
						Sequence = objProteinFileReader.ProteinSequence
					End If

					If mChangeProteinSequencesToLowercase Then
						If mIgnoreILDifferences Then
							' Replace all L characters with I
							Sequence = Sequence.ToLower.Replace("l"c, "i"c)
						Else
							Sequence = Sequence.ToLower
						End If
					ElseIf mChangeProteinSequencesToUppercase Then
						If mIgnoreILDifferences Then
							' Replace all L characters with I
							Sequence = Sequence.ToUpper.Replace("L"c, "I"c)
						Else
							Sequence = Sequence.ToUpper
						End If
					Else
						If mIgnoreILDifferences Then
							' Replace all L characters with I
							Sequence = Sequence.Replace("L"c, "I"c).Replace("l"c, "i"c)
						End If
					End If

					' Store this protein in the Sql Lite DB
					NameFld.Value = Name
					DescriptionFld.Value = Description
					SequenceFld.Value = Sequence
					UniqueSequenceIDFld.Value = mProteinCount		' Use mProteinCount to assign UniqueSequenceID values
					PercentCoverageFld.Value = 0

					SQLcommand.ExecuteNonQuery()

					mProteinCount += 1

					RaiseEvent ProteinCached(mProteinCount)

					If mProteinCount Mod 100 = 0 Then
						RaiseEvent ProteinCachedWithProgress(mProteinCount, objProteinFileReader.PercentFileProcessed)
					End If
				End If
				blnSuccess = True
			Loop While blnInputProteinFound

			' Finalize the SQL Transaction
			SQLTransaction.Commit()

			' Set Synchronous mode to 1   (this may not be truly necessary)
			SQLcommand.CommandText = "PRAGMA synchronous=1"
			SQLcommand.ExecuteNonQuery()

			' Close the Sql Lite DB
			SQLcommand.Dispose()
			SQLconnect.Close()

			' Close the protein file
			objProteinFileReader.CloseFile()

			RaiseEvent ProteinCachingComplete()

			If blnSuccess Then
				If mShowMessages Then
					MsgBox("Done: Processed " & intProteinsProcessed.ToString("###,##0") & " proteins (" & intInputFileLinesRead.ToString("###,###,##0") & " lines)", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Done")
				End If
			Else
				If mShowMessages Then
					MsgBox(mStatusMessage, MsgBoxStyle.Information Or MsgBoxStyle.OkOnly, "Error")
				End If
			End If

		Catch ex As Exception
			mStatusMessage = "Error reading protein input file (" & strProteinInputFilePath & "): " & ex.Message
			blnSuccess = False
		End Try

		Return blnSuccess

	End Function

    ' Options class
    Public Class FastaFileOptionsClass

        Public Sub New()
            mProteinLineStartChar = ">"c
            mProteinLineAccessionEndChar = " "c

        End Sub

#Region "Classwide Variables"
        Private mReadonlyClass As Boolean

        Private mProteinLineStartChar As Char
        Private mProteinLineAccessionEndChar As Char

#End Region

#Region "Processing Options Interface Functions"
        Public Property ReadonlyClass() As Boolean
            Get
                Return mReadonlyClass
            End Get
            Set(ByVal Value As Boolean)
                If Not mReadonlyClass Then
                    mReadonlyClass = Value
                End If
            End Set
        End Property

        Public Property ProteinLineStartChar() As Char
            Get
                Return mProteinLineStartChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mProteinLineStartChar = Value
                End If
            End Set
        End Property

        Public Property ProteinLineAccessionEndChar() As Char
            Get
                Return mProteinLineAccessionEndChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mProteinLineAccessionEndChar = Value
                End If
            End Set
        End Property

#End Region

    End Class

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        Try
            DeleteSQLiteDBFile()
        Catch ex As Exception
            ' Ignore errors here
        End Try

    End Sub
End Class
