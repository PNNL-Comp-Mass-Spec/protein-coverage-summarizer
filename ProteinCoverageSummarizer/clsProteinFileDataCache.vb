Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Started June 2005
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

Imports System.Data.SQLite
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PRISM
Imports ProteinFileReader

''' <summary>
''' This class will read a protein FASTA file or delimited protein info file and
''' store the proteins in memory
''' </summary>
<CLSCompliant(True)>
Public Class clsProteinFileDataCache
    Inherits clsEventNotifier

    Public Sub New()
        mFileDate = "September 14, 2018"
        InitializeLocalVariables()
    End Sub

#Region "Constants and Enums"

    Protected Const SQL_LITE_PROTEIN_CACHE_FILENAME As String = "tmpProteinInfoCache.db3"

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

    ''' <summary>
    ''' When this is True, the SQLite Database will not be deleted after processing finishes
    ''' </summary>
    Public Property KeepDB As Boolean

    Public Property RemoveSymbolCharacters As Boolean

    Public ReadOnly Property StatusMessage As String
        Get
            Return mStatusMessage
        End Get
    End Property

#End Region

    Public Function ConnectToSqlLiteDB(blnDisableJournaling As Boolean) As SQLiteConnection

        If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
            OnDebugEvent("ConnectToSqlLiteDB: Unable to open the SQLite database because mSqlLiteDBConnectionString is empty")
            Return Nothing
        End If

        OnDebugEvent("Connecting to SQLite DB: " + mSqlLiteDBConnectionString)

        Dim sqlConnection = New SQLiteConnection(mSqlLiteDBConnectionString, True)
        sqlConnection.Open()

        If blnDisableJournaling Then
            OnDebugEvent("Disabling Journaling and setting Synchronous mode to 0 (improves update speed)")

            Using cmd As SQLiteCommand = sqlConnection.CreateCommand
                cmd.CommandText = "PRAGMA journal_mode = OFF"
                cmd.ExecuteNonQuery()
                cmd.CommandText = "PRAGMA synchronous = 0"
                cmd.ExecuteNonQuery()
            End Using
        End If

        Return sqlConnection

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
            OnDebugEvent("Checking for write permission by creating file " + strFilePath)

            Using writer = New StreamWriter(New FileStream(strFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                writer.WriteLine("Test")
            End Using

            blnSuccess = True

        Catch ex As Exception
            ' Error creating file; user likely doesn't have write-access
            OnDebugEvent(" ... unable to create the file: " + ex.Message)
            blnSuccess = False
        End Try

        If Not blnSuccess Then
            Try
                ' Create a randomly named file in the user's temp folder
                strFilePath = Path.GetTempFileName
                OnDebugEvent("Creating file in user's temp directory: " + strFilePath)

                strFolderPath = Path.GetDirectoryName(strFilePath)
                blnSuccess = True

            Catch ex As Exception
                ' Error creating temp file; user likely doesn't have write-access anywhere on the disk
                OnDebugEvent(" ... unable to create the file: " + ex.Message)
                blnSuccess = False
            End Try
        End If

        If blnSuccess Then
            Try
                ' Delete the temporary file
                OnDebugEvent("Deleting " + strFilePath)
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

        OnDebugEvent(" SQLite DB Path defined: " + strDBPath)

        Return strDBPath

    End Function

    ''' <summary>
    ''' Delete the SQLite database file
    ''' </summary>
    ''' <param name="callingMethod">Calling method name</param>
    ''' <param name="forceDelete">Force deletion (ignore KeepDB)</param>
    Public Sub DeleteSQLiteDBFile(callingMethod As String, Optional forceDelete As Boolean = False)
        Const MAX_RETRY_ATTEMPT_COUNT = 3

        Try
            If Not mSqlLitePersistentConnection Is Nothing Then
                OnDebugEvent("Closing persistent SQLite connection; calling method: " + callingMethod)
                mSqlLitePersistentConnection.Close()
            End If
        Catch ex As Exception
            ' Ignore errors here
            OnDebugEvent(" ... exception: " + ex.Message)
        End Try

        Try

            If String.IsNullOrEmpty(mSqlLiteDBFilePath) Then
                OnDebugEvent("DeleteSQLiteDBFile: SqlLiteDBFilePath is not defined or is empty; nothing to do; calling method: " + callingMethod)
                Exit Sub
            ElseIf Not File.Exists(mSqlLiteDBFilePath) Then
                OnDebugEvent(String.Format("DeleteSQLiteDBFile: File doesn't exist; nothing to do ({0}); calling method: {1}",
                                           mSqlLiteDBFilePath, callingMethod))
                Exit Sub
            End If

            ' Call the garbage collector to dispose of the SQLite objects
            GC.Collect()
            Thread.Sleep(500)

        Catch ex As Exception
            ' Ignore errors here
        End Try

        If KeepDB And Not forceDelete Then
            OnDebugEvent("DeleteSQLiteDBFile: KeepDB is true; not deleting " + mSqlLiteDBFilePath)
            Exit Sub
        End If

        For retryIndex = 0 To MAX_RETRY_ATTEMPT_COUNT - 1
            Dim retryHoldOffSeconds = (retryIndex + 1)

            Try
                If Not String.IsNullOrEmpty(mSqlLiteDBFilePath) Then
                    If File.Exists(mSqlLiteDBFilePath) Then
                        OnDebugEvent("DeleteSQLiteDBFile: Deleting " + mSqlLiteDBFilePath + "; calling method: " + callingMethod)
                        File.Delete(mSqlLiteDBFilePath)
                    End If
                End If

                If retryIndex > 1 Then
                    OnStatusEvent(" --> File now successfully deleted")
                End If

                ' If we get here, the delete succeeded
                Exit For

            Catch ex As Exception
                If retryIndex > 0 Then
                    OnWarningEvent(String.Format("Error deleting {0} (calling method {1}): {2}", mSqlLiteDBFilePath, callingMethod, ex.Message))
                    OnWarningEvent("  Waiting " & retryHoldOffSeconds & " seconds, then trying again")
                End If
            End Try

            GC.Collect()
            Thread.Sleep(retryHoldOffSeconds * 1000)
        Next

    End Sub

    Public Function GetProteinCountCached() As Integer
        Return mProteinCount
    End Function

    Protected Function GetCachedProteinFromSQLiteDB(intIndex As Integer) As udtProteinInfoType
        Dim udtProteinInfo = New udtProteinInfoType

        If mSqlLitePersistentConnection Is Nothing Then
            mSqlLitePersistentConnection = ConnectToSqlLiteDB(False)
        End If

        Dim cmd As SQLiteCommand
        cmd = mSqlLitePersistentConnection.CreateCommand
        cmd.CommandText = "SELECT * FROM udtProteinInfoType WHERE UniqueSequenceID = " & intIndex.ToString

        OnDebugEvent("GetCachedProteinFromSQLiteDB: running query " + cmd.CommandText)

        Dim reader As SQLiteDataReader
        reader = cmd.ExecuteReader()

        If reader.Read() Then
            ' Column names in table udtProteinInfoType:
            '  Name TEXT,
            '  Description TEXT,
            '  Sequence TEXT,
            '  UniqueSequenceID INTEGER,
            '  PercentCoverage REAL,
            '  NonUniquePeptideCount INTEGER,
            '  UniquePeptideCount INTEGER

            With udtProteinInfo
                .UniqueSequenceID = CInt(reader("UniqueSequenceID"))

                .Name = CStr(reader("Name"))
                .PercentCoverage = CDbl(reader("PercentCoverage"))
                .Description = CStr(reader("Description"))

                .Sequence = CStr(reader("Sequence"))
            End With

        End If

        reader.Close()

        Return udtProteinInfo

    End Function

    Public Function GetSQLiteDataReader(strSQLQuery As String) As SQLiteDataReader

        If mSqlLiteDBConnectionString Is Nothing OrElse mSqlLiteDBConnectionString.Length = 0 Then
            Return Nothing
        End If

        Dim sqlConnection = ConnectToSqlLiteDB(False)

        Dim cmd As SQLiteCommand
        cmd = sqlConnection.CreateCommand
        cmd.CommandText = strSQLQuery

        OnDebugEvent("GetSQLiteDataReader: running query " + cmd.CommandText)

        Dim reader As SQLiteDataReader
        reader = cmd.ExecuteReader()

        Return reader

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
                    OnDebugEvent("InitializeLocalVariables: deleting " + mSqlLiteDBFilePath)
                    File.Delete(mSqlLiteDBFilePath)
                End If

                If Not File.Exists(mSqlLiteDBFilePath) Then
                    blnSuccess = True
                End If

            Catch ex As Exception
                ' Error deleting the file
                OnWarningEvent("Exception in InitializeLocalVariables: " + ex.Message)
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
        Dim sqlConnection = ConnectToSqlLiteDB(True)

        ' SQL query to Create the Table
        Dim cmd = sqlConnection.CreateCommand
        cmd.CommandText = "CREATE TABLE udtProteinInfoType( " &
                                    "Name TEXT, " &
                                    "Description TEXT, " &
                                    "sequence TEXT, " &
                                    "UniquesequenceID INTEGER PRIMARY KEY, " &
                                    "PercentCoverage REAL);" ', NonUniquePeptideCount INTEGER, UniquePeptideCount INTEGER);"

        OnDebugEvent("ParseProteinFile: Creating table with " + cmd.CommandText)

        cmd.ExecuteNonQuery()

        ' Define a RegEx to replace all of the non-letter characters
        Dim reReplaceSymbols = New Regex("[^A-Za-z]", RegexOptions.Compiled)

        Dim objProteinFileReader As ProteinFileReaderBaseClass = Nothing

        Dim blnSuccess As Boolean

        Try

            If strProteinInputFilePath Is Nothing OrElse strProteinInputFilePath.Length = 0 Then
                ReportError("Empty protein input file path")
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
                    ReportError("Protein input file not found: " & strProteinInputFilePath)
                    blnSuccess = False
                    Exit Try
                End If

                ' Attempt to open the input file
                If Not objProteinFileReader.OpenFile(strProteinInputFilePath) Then
                    ReportError("Error opening protein input file: " & strProteinInputFilePath)
                    blnSuccess = False
                    Exit Try
                End If

                blnSuccess = True
            End If

        Catch ex As Exception
            ReportError("Error opening protein input file (" & strProteinInputFilePath & "): " & ex.Message, ex)
            blnSuccess = False
        End Try

        ' Abort processing if we couldn't successfully open the input file
        If Not blnSuccess Then Return False

        Try
            ' Read each protein in the input file and process appropriately
            mProteinCount = 0

            RaiseEvent ProteinCachingStart()

            ' Create a parameterized Insert query
            cmd.CommandText = " INSERT INTO udtProteinInfoType(Name, Description, sequence, UniquesequenceID, PercentCoverage) " &
                                     " VALUES (?, ?, ?, ?, ?)"

            Dim nameFld As SQLiteParameter = cmd.CreateParameter
            Dim descriptionFld As SQLiteParameter = cmd.CreateParameter
            Dim sequenceFld As SQLiteParameter = cmd.CreateParameter
            Dim uniquesequenceIDFld As SQLiteParameter = cmd.CreateParameter
            Dim percentCoverageFld As SQLiteParameter = cmd.CreateParameter
            cmd.Parameters.Add(nameFld)
            cmd.Parameters.Add(descriptionFld)
            cmd.Parameters.Add(sequenceFld)
            cmd.Parameters.Add(uniquesequenceIDFld)
            cmd.Parameters.Add(percentCoverageFld)

            ' Begin a SQL Transaction
            Dim SQLTransaction = sqlConnection.BeginTransaction()

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

                cmd.ExecuteNonQuery()

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
            cmd.CommandText = "PRAGMA synchronous=1"
            cmd.ExecuteNonQuery()

            ' Close the Sql Lite DB
            cmd.Dispose()
            sqlConnection.Close()

            ' Close the protein file
            objProteinFileReader.CloseFile()

            RaiseEvent ProteinCachingComplete()

            If blnSuccess Then
                OnStatusEvent("Done: Processed " & intProteinsProcessed.ToString("###,##0") & " proteins (" & intInputFileLinesRead.ToString("###,###,##0") & " lines)")
            Else
                OnErrorEvent(mStatusMessage)
            End If

        Catch ex As Exception
            ReportError("Error reading protein input file (" & strProteinInputFilePath & "): " & ex.Message, ex)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Sub ReportError(errorMessage As String, Optional ex As Exception = Nothing)
        OnErrorEvent(errorMessage, ex)
        mStatusMessage = errorMessage
    End Sub

    ' Options class
    Public Class FastaFileOptionsClass

        Public Sub New()
            mProteinLineStartChar = ">"c
            mProteinLineAccessionEndChar = " "c

        End Sub

#Region "Classwide Variables"

        Private mProteinLineStartChar As Char
        Private mProteinLineAccessionEndChar As Char

#End Region

#Region "Processing Options Interface Functions"

        Public Property ProteinLineStartChar As Char
            Get
                Return mProteinLineStartChar
            End Get
            Set
                If Not Value = Nothing Then
                    mProteinLineStartChar = Value
                End If
            End Set
        End Property

        Public Property ProteinLineAccessionEndChar As Char
            Get
                Return mProteinLineAccessionEndChar
            End Get
            Set
                If Not Value = Nothing Then
                    mProteinLineAccessionEndChar = Value
                End If
            End Set
        End Property

#End Region

    End Class

End Class
