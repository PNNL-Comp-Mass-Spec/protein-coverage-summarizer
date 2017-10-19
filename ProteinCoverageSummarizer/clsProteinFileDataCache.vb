Option Strict On

Imports System.Data.SQLite
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PRISM
Imports ProteinFileReader

' This class will read a protein fasta file or delimited protein info file and
' store the proteins in memory
'
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
'
' Started June 2005

<CLSCompliant(True)>
Public Class clsProteinFileDataCache
    Inherits clsEventNotifier

    Public Sub New()
        mFileDate = "October 15, 2017"
        InitializeLocalVariables()
    End Sub

#Region "Constants and Enums"

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
    Private mStatusMessage As String

    Private mDelimitedInputFileDelimiter As Char                              ' Only used for delimited protein input files, not for fasta files

    Public FastaFileOptions As FastaFileOptionsClass

    Private mProteinCount As Integer
    Private mParsedFileIsFastaFile As Boolean

    ' Sql Lite Connection String and filepath
    Private mSqlLiteDBConnectionString As String = String.Empty
    Private mSqlLiteDBFilePath As String = SQL_LITE_PROTEIN_CACHE_FILENAME

    Private mSqlLitePersistentConnection As SQLiteConnection

    Public Event ProteinCachingStart()
    Public Event ProteinCached(intProteinsCached As Integer)
    Public Event ProteinCachedWithProgress(intProteinsCached As Integer, sngPercentFileProcessed As Single)
    Public Event ProteinCachingComplete()

#End Region

#Region "Processing Options Interface Functions"

    ''' <summary>
    ''' When True, assume the input file is a tab-delimited text file
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Ignored if AssumeFastaFile is True</remarks>
    Public Property AssumeDelimitedFile As Boolean

    ''' <summary>
    ''' When True, assume the input file is a FASTA text file
    ''' </summary>
    ''' <returns></returns>
    Public Property AssumeFastaFile As Boolean

    Public Property ChangeProteinSequencesToLowercase As Boolean

    Public Property ChangeProteinSequencesToUppercase As Boolean

    Public Property DelimitedFileFormatCode As DelimitedFileReader.eDelimitedFileFormatCode

    Public Property DelimitedFileDelimiter As Char
        Get
            Return mDelimitedInputFileDelimiter
        End Get
        Set
            If Not Value = Nothing Then
                mDelimitedInputFileDelimiter = Value
            End If
        End Set
    End Property

    Public Property DelimitedFileSkipFirstLine As Boolean

    Public Property IgnoreILDifferences As Boolean

    Public ReadOnly Property ParsedFileIsFastaFile As Boolean
        Get
            Return mParsedFileIsFastaFile
        End Get
    End Property

    Public Property RemoveSymbolCharacters As Boolean

    Public ReadOnly Property StatusMessage As String
        Get
            Return mStatusMessage
        End Get
    End Property

#End Region

    Public Function ConnectToSqlLiteDB(blnDisableJournaling As Boolean) As SQLiteConnection

        If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
            Return Nothing
        End If

        Dim SQLconnect = New SQLiteConnection(mSqlLiteDBConnectionString, True)
        SQLconnect.Open()

        ' Turn off Journaling and set Synchronous mode to 0
        ' These changes are required to improve the update speed
        If blnDisableJournaling Then

            Using SQLcommand As SQLiteCommand = SQLconnect.CreateCommand
                SQLcommand.CommandText = "PRAGMA journal_mode = OFF"
                SQLcommand.ExecuteNonQuery()
                SQLcommand.CommandText = "PRAGMA synchronous = 0"
                SQLcommand.ExecuteNonQuery()
            End Using
        End If

        Return SQLconnect

    End Function

    Private Function DefineSqlLiteDBPath(strSqlLiteDBFileName As String) As String
        Dim strDBPath As String
        Dim strFolderPath As String = String.Empty
        Dim strFilePath As String = String.Empty

        Dim blnSuccess As Boolean

        Try
            ' See if we can create files in the folder that contains this .Dll
            strFolderPath = clsProteinCoverageSummarizer.GetAppFolderPath()

            strFilePath = Path.Combine(strFolderPath, "TempFileToTestFileIOPermissions.tmp")

            Using swTestFile = New StreamWriter(New FileStream(strFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                swTestFile.WriteLine("Test")
            End Using

            blnSuccess = True

        Catch ex As Exception
            ' Error creating file; user likely doesn't have write-access
            blnSuccess = False
        End Try

        If Not blnSuccess Then
            Try
                ' Create a randomly named file in the user's temp folder
                strFilePath = Path.GetTempFileName
                strFolderPath = Path.GetDirectoryName(strFilePath)
                blnSuccess = True

            Catch ex As Exception
                ' Error creating temp file; user likely doesn't have write-access anywhere on the disk
                blnSuccess = False
            End Try
        End If

        If blnSuccess Then
            Try
                ' Delete the temporary file
                File.Delete(strFilePath)
            Catch ex As Exception
                ' Ignore errors here
            End Try
        End If

        If blnSuccess Then
            strDBPath = Path.Combine(strFolderPath, strSqlLiteDBFileName)
        Else
            strDBPath = strSqlLiteDBFileName
        End If

        Return strDBPath

    End Function

    Public Sub DeleteSQLiteDBFile()
        Const MAX_RETRY_ATTEMPT_COUNT = 3

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
            ElseIf Not File.Exists(mSqlLiteDBFilePath) Then
                ' File doesn't exist; nothing to do
                Exit Sub
            End If

            ' Call the garbage collector to dispose of the SQLite objects
            GC.Collect()
            Thread.Sleep(500)

        Catch ex As Exception
            ' Ignore errors here
        End Try

        For intRetryIndex = 0 To MAX_RETRY_ATTEMPT_COUNT - 1
            Dim intRetryHoldoffSeconds = (intRetryIndex + 1)

            Try
                If Not String.IsNullOrEmpty(mSqlLiteDBFilePath) Then
                    If File.Exists(mSqlLiteDBFilePath) Then
                        File.Delete(mSqlLiteDBFilePath)
                    End If
                End If

                If intRetryIndex > 1 Then
                    OnStatusEvent(" --> File now successfully deleted")
                End If

                ' If we get here, the delete succeeded
                Exit For

            Catch ex As Exception
                If intRetryIndex > 0 Then
                    OnWarningEvent("Error deleting " & mSqlLiteDBFilePath & ": " & ControlChars.NewLine & ex.Message)
                    OnWarningEvent("  Waiting " & intRetryHoldoffSeconds & " seconds, then trying again")
                End If
            End Try

            GC.Collect()
            Thread.Sleep(intRetryHoldoffSeconds * 1000)
        Next

    End Sub

    Public Function GetProteinCountCached() As Integer
        Return mProteinCount
    End Function

    Public Function GetCachedProtein(intIndex As Integer) As udtProteinInfoType
        If intIndex >= 0 And intIndex < mProteinCount Then
            Return GetCachedProteinFromSQLiteDB(intIndex)
        Else
            Return New udtProteinInfoType
        End If
    End Function

    Protected Function GetCachedProteinFromSQLiteDB(intIndex As Integer) As udtProteinInfoType
        Dim udtProteinInfo = New udtProteinInfoType

        If mSqlLitePersistentConnection Is Nothing Then
            mSqlLitePersistentConnection = ConnectToSqlLiteDB(False)
        End If

        Dim SQLcommand As SQLiteCommand
        SQLcommand = mSqlLitePersistentConnection.CreateCommand
        SQLcommand.CommandText = "SELECT * FROM udtProteinInfoType WHERE UniqueSequenceID = " & intIndex.ToString

        Dim SQLreader As SQLiteDataReader
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

    Public Function GetSQLiteDataReader(strSQLQuery As String) As SQLiteDataReader

        If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
            Return Nothing
        End If

        Dim SQLconnect = ConnectToSqlLiteDB(False)

        Dim SQLcommand As SQLiteCommand
        SQLcommand = SQLconnect.CreateCommand
        SQLcommand.CommandText = strSQLQuery

        Dim SQLreader As SQLiteDataReader
        SQLreader = SQLcommand.ExecuteReader()

        Return SQLreader

    End Function

    Private Sub InitializeLocalVariables()
        Const MAX_FILE_CREATE_ATTEMPTS = 10

        AssumeDelimitedFile = False
        AssumeFastaFile = False

        mDelimitedInputFileDelimiter = ControlChars.Tab
        DelimitedFileFormatCode = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence
        DelimitedFileSkipFirstLine = False

        FastaFileOptions = New FastaFileOptionsClass

        mProteinCount = 0

        RemoveSymbolCharacters = True

        ChangeProteinSequencesToLowercase = False
        ChangeProteinSequencesToUppercase = False

        IgnoreILDifferences = False

        Dim intFileAttemptCount = 0
        Dim blnSuccess = False
        Do While Not blnSuccess AndAlso intFileAttemptCount < MAX_FILE_CREATE_ATTEMPTS

            ' Define the path to the Sql Lite database
            If intFileAttemptCount = 0 Then
                mSqlLiteDBFilePath = DefineSqlLiteDBPath(SQL_LITE_PROTEIN_CACHE_FILENAME)
            Else
                mSqlLiteDBFilePath = DefineSqlLiteDBPath(Path.GetFileNameWithoutExtension(SQL_LITE_PROTEIN_CACHE_FILENAME) &
                                                         intFileAttemptCount.ToString &
                                                         Path.GetExtension(SQL_LITE_PROTEIN_CACHE_FILENAME))
            End If

            Try
                ' If the file exists, we need to delete it
                If File.Exists(mSqlLiteDBFilePath) Then
                    File.Delete(mSqlLiteDBFilePath)
                End If

                If Not File.Exists(mSqlLiteDBFilePath) Then
                    blnSuccess = True
                End If

            Catch ex As Exception
                ' Error deleting the file
            End Try

            intFileAttemptCount += 1
        Loop

        mSqlLiteDBConnectionString = "Data Source=" & mSqlLiteDBFilePath & ";"

    End Sub

    ''' <summary>
    ''' Examines the file's extension and true if it ends in .fasta or .fsa or .faa
    ''' </summary>
    ''' <param name="strFilePath"></param>
    ''' <returns></returns>
    Public Shared Function IsFastaFile(strFilePath As String) As Boolean

        Dim proteinFileExtension = Path.GetExtension(strFilePath).ToLower()

        If proteinFileExtension = ".fasta" OrElse proteinFileExtension = ".fsa" OrElse proteinFileExtension = ".faa" Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ParseProteinFile(strProteinInputFilePath As String) As Boolean
        ' If strOutputFileNameBaseOverride is defined, then uses that name for the protein output filename rather than auto-defining the name

        ' Create the SQL Lite DB
        Dim SQLconnect = ConnectToSqlLiteDB(True)

        ' SQL query to Create the Table
        Dim SQLcommand = SQLconnect.CreateCommand
        SQLcommand.CommandText = "CREATE TABLE udtProteinInfoType( " &
                                    "Name TEXT, " &
                                    "Description TEXT, " &
                                    "sequence TEXT, " &
                                    "UniquesequenceID INTEGER PRIMARY KEY, " &
                                    "PercentCoverage REAL);" ', NonUniquePeptideCount INTEGER, UniquePeptideCount INTEGER);"
        SQLcommand.ExecuteNonQuery()

        ' Define a RegEx to replace all of the non-letter characters
        Dim reReplaceSymbols = New Regex("[^A-Za-z]", RegexOptions.Compiled)

        Dim objProteinFileReader As ProteinFileReaderBaseClass = Nothing

        Dim blnSuccess As Boolean

        Try

            If strProteinInputFilePath Is Nothing OrElse strProteinInputFilePath.Length = 0 Then
                mStatusMessage = "Empty protein input file path"
                blnSuccess = False
            Else

                If AssumeFastaFile OrElse IsFastaFile(strProteinInputFilePath) Then
                    mParsedFileIsFastaFile = True
                Else
                    If AssumeDelimitedFile Then
                        mParsedFileIsFastaFile = False
                    Else
                        mParsedFileIsFastaFile = True
                    End If
                End If

                If mParsedFileIsFastaFile Then
                    objProteinFileReader = New FastaFileReader() With {
                        .ProteinLineStartChar = FastaFileOptions.ProteinLineStartChar,
                        .ProteinLineAccessionEndChar = FastaFileOptions.ProteinLineAccessionEndChar
                    }
                Else
                    objProteinFileReader = New DelimitedFileReader With {
                        .Delimiter = mDelimitedInputFileDelimiter,
                        .DelimitedFileFormatCode = DelimitedFileFormatCode,
                        .SkipFirstLine = DelimitedFileSkipFirstLine
                    }
                End If

                ' Verify that the input file exists
                If Not File.Exists(strProteinInputFilePath) Then
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
            SQLcommand.CommandText = " INSERT INTO udtProteinInfoType(Name, Description, sequence, UniquesequenceID, PercentCoverage) " &
                                     " VALUES (?, ?, ?, ?, ?)"

            Dim nameFld As SQLiteParameter = SQLcommand.CreateParameter
            Dim descriptionFld As SQLiteParameter = SQLcommand.CreateParameter
            Dim sequenceFld As SQLiteParameter = SQLcommand.CreateParameter
            Dim uniquesequenceIDFld As SQLiteParameter = SQLcommand.CreateParameter
            Dim percentCoverageFld As SQLiteParameter = SQLcommand.CreateParameter
            SQLcommand.Parameters.Add(nameFld)
            SQLcommand.Parameters.Add(descriptionFld)
            SQLcommand.Parameters.Add(sequenceFld)
            SQLcommand.Parameters.Add(uniquesequenceIDFld)
            SQLcommand.Parameters.Add(percentCoverageFld)

            ' Begin a SQL Transaction
            Dim SQLTransaction = SQLconnect.BeginTransaction()

            Dim intProteinsProcessed = 0
            Dim intInputFileLinesRead = 0

            Do
                Dim blnInputProteinFound = objProteinFileReader.ReadNextProteinEntry()

                If Not blnInputProteinFound Then
                    Exit Do
                End If

                intProteinsProcessed += 1
                intInputFileLinesRead = objProteinFileReader.LinesRead

                Dim name = objProteinFileReader.ProteinName
                Dim description = objProteinFileReader.ProteinDescription
                Dim sequence As String

                If RemoveSymbolCharacters Then
                    sequence = reReplaceSymbols.Replace(objProteinFileReader.ProteinSequence, String.Empty)
                Else
                    sequence = objProteinFileReader.ProteinSequence
                End If

                If ChangeProteinSequencesToLowercase Then
                    If IgnoreILDifferences Then
                        ' Replace all L characters with I
                        sequence = sequence.ToLower().Replace("l"c, "i"c)
                    Else
                        sequence = sequence.ToLower()
                    End If
                ElseIf ChangeProteinSequencesToUppercase Then
                    If IgnoreILDifferences Then
                        ' Replace all L characters with I
                        sequence = sequence.ToUpper.Replace("L"c, "I"c)
                    Else
                        sequence = sequence.ToUpper
                    End If
                Else
                    If IgnoreILDifferences Then
                        ' Replace all L characters with I
                        sequence = sequence.Replace("L"c, "I"c).Replace("l"c, "i"c)
                    End If
                End If

                ' Store this protein in the Sql Lite DB
                nameFld.Value = name
                descriptionFld.Value = description
                sequenceFld.Value = sequence

                ' Use mProteinCount to assign UniquesequenceID values
                uniquesequenceIDFld.Value = mProteinCount

                percentCoverageFld.Value = 0

                SQLcommand.ExecuteNonQuery()

                mProteinCount += 1

                RaiseEvent ProteinCached(mProteinCount)

                If mProteinCount Mod 100 = 0 Then
                    RaiseEvent ProteinCachedWithProgress(mProteinCount, objProteinFileReader.PercentFileProcessed)
                End If

                blnSuccess = True
            Loop

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
                OnStatusEvent("Done: Processed " & intProteinsProcessed.ToString("###,##0") & " proteins (" & intInputFileLinesRead.ToString("###,###,##0") & " lines)")
            Else
                OnErrorEvent(mStatusMessage)
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
        Public Property ReadonlyClass As Boolean
            Get
                Return mReadonlyClass
            End Get
            Set
                If Not mReadonlyClass Then
                    mReadonlyClass = Value
                End If
            End Set
        End Property

        Public Property ProteinLineStartChar As Char
            Get
                Return mProteinLineStartChar
            End Get
            Set
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mProteinLineStartChar = Value
                End If
            End Set
        End Property

        Public Property ProteinLineAccessionEndChar As Char
            Get
                Return mProteinLineAccessionEndChar
            End Get
            Set
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
