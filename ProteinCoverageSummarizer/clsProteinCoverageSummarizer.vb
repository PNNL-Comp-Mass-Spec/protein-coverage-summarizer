Option Strict On

Imports System.Data.SQLite
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports ProteinFileReader

' This class will read in a protein fasta file or delimited protein info file along with
' an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Program started June 14, 2005
'
' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://omics.pnl.gov/ or http://www.sysbio.org/resources/staff/
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

' Last updated October 22, 2012

<CLSCompliant(True)>
Public Class clsProteinCoverageSummarizer

    Public Sub New()
        InitializeVariables()
    End Sub

#Region "Constants and Enums"
    Public Const XML_SECTION_PROCESSING_OPTIONS As String = "ProcessingOptions"

    Public Const OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER As Integer = 3
    Public Const OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER As Integer = 7

    Public Const NEW_PROTEINS_CACHE_MEMORY_RESERVE_COUNT As Integer = 500

    Public Const FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING As String = "_ProteinToPeptideMapping.txt"
    Public Const FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS As String = "_AllProteins.txt"

    Protected Const PROTEIN_CHUNK_COUNT As Integer = 50000

    Public Enum DelimiterCharConstants
        Space = 0
        Tab = 1
        Comma = 2
        Other = 3
    End Enum

    Public Enum ePeptideFileColumnOrderingCode As Integer
        SequenceOnly = 0
        ProteinName_PeptideSequence = 1
    End Enum

    Public Enum eProteinCoverageErrorCodes
        NoError = 0
        InvalidInputFilePath = 1
        ErrorReadingParameterFile = 2
        FilePathError = 16
        UnspecifiedError = -1
    End Enum

    ' Note: if you add/remove any steps, then update PERCENT_COMPLETE_LEVEL_COUNT and update the population of mPercentCompleteStartLevels()
    Enum eProteinCoverageProcessingSteps
        Starting = 0
        CacheProteins = 1
        DetermineShortestPeptideLength = 2
        CachePeptides = 3
        SearchProteinsUsingLeaderSequences = 4
        SearchProteinsAgainstShortPeptides = 5
        ComputePercentCoverage = 6
        WriteProteinCoverageFile = 7
        SaveAllProteinsVersionOfInputFile = 8
    End Enum

#End Region

#Region "Structures"
    Protected Structure udtPeptideCountStatsType
        Public UniquePeptideCount As Integer
        Public NonUniquePeptideCount As Integer
    End Structure

    Protected Structure udtSequence
        Public KeyRecord As Integer
        Public NewSequenceValue As String
    End Structure
#End Region

#Region "Classwide variables"
    Public WithEvents mProteinDataCache As clsProteinFileDataCache
    Private WithEvents mLeaderSequenceCache As clsLeaderSequenceCache
    Private mNewProteinsCache() As udtSequence

    ' This dictionary contains entries of the form 1234::K.ABCDEFR.A
    '  where the number is the protein ID and the peptide is the peptide sequence
    ' The value for each entry is the number of times the peptide is present in the given protein
    ' This dictionary is only populated if mTrackPeptideCounts is true
    Private mProteinPeptideStats As Dictionary(Of String, Integer)

    Private mProteinInputFilePath As String
    Private mResultsFilePath As String              ' This value is populated by function ProcessFile()

    Private mOutputProteinSequence As Boolean

    Private mPeptideFileSkipFirstLine As Boolean
    Private mPeptideInputFileDelimiter As Char
    Private mPeptideFileColumnOrdering As ePeptideFileColumnOrderingCode

    Private mRemoveSymbolCharacters As Boolean
    Private mMatchPeptidePrefixAndSuffixToProtein As Boolean
    Private mIgnoreILDifferences As Boolean

    Private mSearchAllProteinsForPeptideSequence As Boolean
    Private mSearchAllProteinsSkipCoverageComputationSteps As Boolean

    ' If this is disabled, then a brute-force search is applied,
    ' The brute-force search is nearly always much slower than with mUseLeaderSequenceHashTable enabled
    Private mUseLeaderSequenceHashTable As Boolean

    Private mSaveProteinToPeptideMappingFile As Boolean
    Private mProteinToPeptideMappingFilePath As String
    Private mProteinToPeptideMappingOutputFile As StreamWriter

    Private mSaveSourceDataPlusProteinsFile As Boolean

    Private mTrackPeptideCounts As Boolean

    Private mErrorCode As eProteinCoverageErrorCodes
    Private mErrorMessage As String

    Private mAbortProcessing As Boolean

    Private mCachedProteinInfoStartIndex As Integer = -1
    Private mCachedProteinInfoCount As Integer
    Private mCachedProteinInfo() As clsProteinFileDataCache.udtProteinInfoType

    Private mPeptideToProteinMapResults As Dictionary(Of String, List(Of String))

    ' mPercentCompleteStartLevels is an array that lists the percent complete value to report
    '  at the start of each of the various processing steps performed in this procedure
    ' The percent complete values range from 0 to 100
    Const PERCENT_COMPLETE_LEVEL_COUNT As Integer = 9
    Protected mPercentCompleteStartLevels() As Single

#End Region

#Region "Progress Events and Variables"
    Public Event ProgressReset()
    Public Event ProgressChanged(taskDescription As String, percentComplete As Single)     ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
    Public Event ProgressComplete()

    ' Note: These events are no longer used
    ''Public Event SubtaskProgressChanged(taskDescription As String, percentComplete As Single)     ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
    ''Public Event SubtaskProgressComplete()

    Protected mCurrentProcessingStep As eProteinCoverageProcessingSteps = eProteinCoverageProcessingSteps.Starting
    Protected mProgressStepDescription As String = String.Empty
    Protected mProgressPercentComplete As Single        ' Ranges from 0 to 100, but can contain decimal percentage values

    ''Protected mSubtaskStepDescription As String = String.Empty
    ''Protected mSubtaskPercentComplete As Single        ' Ranges from 0 to 100, but can contain decimal percentage values
#End Region

#Region "Properties"
    Public ReadOnly Property ErrorCode As eProteinCoverageErrorCodes
        Get
            Return mErrorCode
        End Get
    End Property

    Public ReadOnly Property ErrorMessage As String
        Get
            Return GetErrorMessage()
        End Get
    End Property

    Public Property IgnoreILDifferences As Boolean
        Get
            Return mIgnoreILDifferences
        End Get
        Set
            mIgnoreILDifferences = Value
        End Set
    End Property

    Public Property MatchPeptidePrefixAndSuffixToProtein As Boolean
        Get
            Return mMatchPeptidePrefixAndSuffixToProtein
        End Get
        Set
            mMatchPeptidePrefixAndSuffixToProtein = Value
        End Set
    End Property

    Public Property OutputProteinSequence As Boolean
        Get
            Return mOutputProteinSequence
        End Get
        Set
            mOutputProteinSequence = Value
        End Set
    End Property

    Public Property PeptideFileFormatCode As ePeptideFileColumnOrderingCode
        Get
            Return mPeptideFileColumnOrdering
        End Get
        Set
            mPeptideFileColumnOrdering = Value
        End Set
    End Property

    Public Property PeptideFileSkipFirstLine As Boolean
        Get
            Return mPeptideFileSkipFirstLine
        End Get
        Set
            mPeptideFileSkipFirstLine = Value
        End Set
    End Property

    Public Property PeptideInputFileDelimiter As Char
        Get
            Return mPeptideInputFileDelimiter
        End Get
        Set
            mPeptideInputFileDelimiter = Value
        End Set
    End Property

    Public Overridable ReadOnly Property ProgressStepDescription As String
        Get
            Return mProgressStepDescription
        End Get
    End Property

    ' ProgressPercentComplete ranges from 0 to 100, but can contain decimal percentage values
    Public ReadOnly Property ProgressPercentComplete As Single
        Get
            Return CType(Math.Round(mProgressPercentComplete, 2), Single)
        End Get
    End Property

    Public Property ProteinInputFilePath As String
        Get
            Return mProteinInputFilePath
        End Get
        Set
            mProteinInputFilePath = Value
        End Set
    End Property

    Public ReadOnly Property ProteinToPeptideMappingFilePath As String
        Get
            Return mProteinToPeptideMappingFilePath
        End Get
    End Property

    Public Property RemoveSymbolCharacters As Boolean
        Get
            Return mRemoveSymbolCharacters
        End Get
        Set
            mRemoveSymbolCharacters = Value
        End Set
    End Property

    Public ReadOnly Property ResultsFilePath As String
        Get
            Return mResultsFilePath
        End Get
    End Property

    Public Property SaveProteinToPeptideMappingFile As Boolean
        Get
            Return mSaveProteinToPeptideMappingFile
        End Get
        Set
            mSaveProteinToPeptideMappingFile = Value
        End Set
    End Property

    Public Property SaveSourceDataPlusProteinsFile As Boolean
        Get
            Return mSaveSourceDataPlusProteinsFile
        End Get
        Set
            mSaveSourceDataPlusProteinsFile = Value
        End Set
    End Property

    Public Property SearchAllProteinsForPeptideSequence As Boolean
        Get
            Return mSearchAllProteinsForPeptideSequence
        End Get
        Set
            mSearchAllProteinsForPeptideSequence = Value
        End Set
    End Property

    Public Property UseLeaderSequenceHashTable As Boolean
        Get
            Return mUseLeaderSequenceHashTable
        End Get
        Set
            mUseLeaderSequenceHashTable = Value
        End Set
    End Property

    Public Property SearchAllProteinsSkipCoverageComputationSteps As Boolean
        Get
            Return mSearchAllProteinsSkipCoverageComputationSteps
        End Get
        Set
            mSearchAllProteinsSkipCoverageComputationSteps = Value
        End Set
    End Property

    Public ReadOnly Property StatusMessage As String
        Get
            Return mErrorMessage
        End Get
    End Property

    ''Public ReadOnly Property SubtaskStepDescription() As String
    ''    Get
    ''        Return mSubtaskStepDescription
    ''    End Get
    ''End Property

    ''Public ReadOnly Property SubtaskPercentComplete() As Single
    ''    Get
    ''        Return mSubtaskPercentComplete
    ''    End Get
    ''End Property

    Public Property TrackPeptideCounts As Boolean
        Get
            Return mTrackPeptideCounts
        End Get
        Set
            mTrackPeptideCounts = Value
        End Set
    End Property

#End Region

    Public Sub AbortProcessingNow()
        If Not mLeaderSequenceCache Is Nothing Then
            mLeaderSequenceCache.AbortProcessingNow()
        End If
    End Sub

    Private Function BooleanArrayContainsTrueEntries(blnArrayToCheck As IList(Of Boolean), intArrayLength As Integer) As Boolean

        Dim blnContainsTrueEntries = False

        For intIndex = 0 To intArrayLength - 1
            If blnArrayToCheck(intIndex) Then
                blnContainsTrueEntries = True
                Exit For
            End If
        Next

        Return blnContainsTrueEntries

    End Function

    Private Function CapitalizeMatchingProteinSequenceLetters(
      strProteinSequence As String,
      strPeptideSequence As String,
      proteinPeptideKey As String,
      chPrefixResidue As Char,
      chSuffixResidue As Char,
      <Out> ByRef blnMatchFound As Boolean,
      <Out> ByRef blnMatchIsNew As Boolean,
      <Out> ByRef intStartResidue As Integer,
      <Out> ByRef intEndResidue As Integer) As String

        ' Note: this function assumes strPeptideSequence, chPrefix, and chSuffix have all uppercase letters
        ' chPrefix and chSuffix are only used if mMatchPeptidePrefixAndSuffixToProtein = true

        ' Note: This is a count of the number of times the peptide is present in the protein sequence (typically 1); this value is not stored anywhere
        Dim intPeptideCount = 0

        Dim blnCurrentMatchValid As Boolean

        blnMatchFound = False
        blnMatchIsNew = False

        intStartResidue = 0
        intEndResidue = 0

        Dim intCharIndex As Integer

        If mSearchAllProteinsSkipCoverageComputationSteps Then
            ' No need to capitalize strProteinSequence since it's already capitalized
            intCharIndex = strProteinSequence.IndexOf(strPeptideSequence, StringComparison.Ordinal)
        Else
            ' Need to change strProteinSequence to all caps when searching for strPeptideSequence
            intCharIndex = strProteinSequence.ToUpper().IndexOf(strPeptideSequence, StringComparison.Ordinal)
        End If

        If intCharIndex >= 0 Then
            intStartResidue = intCharIndex + 1
            intEndResidue = intStartResidue + strPeptideSequence.Length - 1

            blnMatchFound = True

            If mMatchPeptidePrefixAndSuffixToProtein Then
                blnCurrentMatchValid = ValidatePrefixAndSuffix(strProteinSequence, chPrefixResidue, chSuffixResidue, intCharIndex, intEndResidue - 1)
            Else
                blnCurrentMatchValid = True
            End If

            If blnCurrentMatchValid Then
                intPeptideCount += 1
            Else
                intStartResidue = 0
                intEndResidue = 0
            End If
        Else
            blnCurrentMatchValid = False
        End If

        If blnMatchFound AndAlso Not mSearchAllProteinsSkipCoverageComputationSteps Then
            Do While intCharIndex >= 0

                If blnCurrentMatchValid Then
                    Dim intNextStartIndex = intCharIndex + strPeptideSequence.Length

                    Dim strNewProteinSequence = String.Empty
                    If intCharIndex > 0 Then
                        strNewProteinSequence = strProteinSequence.Substring(0, intCharIndex)
                    End If
                    strNewProteinSequence &= strProteinSequence.Substring(intCharIndex, intNextStartIndex - intCharIndex).ToUpper
                    strNewProteinSequence &= strProteinSequence.Substring(intNextStartIndex)
                    strProteinSequence = String.Copy(strNewProteinSequence)
                End If

                ' Look for another occurrence of strPeptideSequence in this protein
                intCharIndex = strProteinSequence.ToUpper().IndexOf(strPeptideSequence, intCharIndex + 1, StringComparison.Ordinal)

                If intCharIndex >= 0 Then
                    If mMatchPeptidePrefixAndSuffixToProtein Then
                        blnCurrentMatchValid = ValidatePrefixAndSuffix(strProteinSequence, chPrefixResidue, chSuffixResidue, intCharIndex, intCharIndex + strPeptideSequence.Length - 1)
                    Else
                        blnCurrentMatchValid = True
                    End If

                    If blnCurrentMatchValid Then
                        intPeptideCount += 1

                        If intStartResidue = 0 Then
                            intStartResidue = intCharIndex + 1
                            intEndResidue = intStartResidue + strPeptideSequence.Length - 1
                        End If
                    End If
                End If
            Loop
        End If


        If blnMatchFound Then
            If intPeptideCount = 0 Then
                ' The protein contained strPeptideSequence, but mMatchPeptidePrefixAndSuffixToProtein = true and either chPrefixResidue or chSuffixResidue doesn't match
                blnMatchFound = False
            ElseIf mTrackPeptideCounts Then
                blnMatchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey)
            Else
                ' Must always assume the match is new since not tracking peptide counts
                blnMatchIsNew = True
            End If
        End If

        Return strProteinSequence

    End Function

    ''' <summary>
    ''' Construct the output file path
    ''' The output file is based on outputFileBaseName if defined, otherwise is based on inputFilePath with the suffix removed
    ''' In either case, suffixToAppend is appended
    ''' The Output fodler is based on outputFolderPath if defined, otherwise it is the folder where inputFilePath resides
    ''' </summary>
    ''' <param name="inputFilePath"></param>
    ''' <param name="suffixToAppend"></param>
    ''' <param name="outputFolderPath"></param>
    ''' <param name="outputFileBaseName"></param>
    ''' <returns></returns>
    Public Shared Function ConstructOutputFilePath(
      inputFilePath As String,
      suffixToAppend As String,
      outputFolderPath As String,
      outputFileBaseName As String) As String

        Dim outputFileName As String

        If String.IsNullOrEmpty(outputFileBaseName) Then
            outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) & suffixToAppend
        Else
            outputFileName = outputFileBaseName & suffixToAppend
        End If

        Dim outputFilePath = Path.Combine(GetOutputFolderPath(outputFolderPath, inputFilePath), outputFileName)

        Return outputFilePath

    End Function

    Private Function ConstructPeptideSequenceForKey(strPeptideSequence As String, chPrefixResidue As Char, chSuffixResidue As Char) As String
        Dim strPeptideSequenceForKey As String

        If Convert.ToInt32(chPrefixResidue) = 0 AndAlso Convert.ToInt32(chSuffixResidue) = 0 Then
            strPeptideSequenceForKey = String.Copy(strPeptideSequence)
        Else
            If Char.IsLetter(chPrefixResidue) Then
                chPrefixResidue = Char.ToUpper(chPrefixResidue)
                strPeptideSequenceForKey = chPrefixResidue & "."c & strPeptideSequence
            Else
                strPeptideSequenceForKey = "-." & strPeptideSequence
            End If

            If Char.IsLetter(chSuffixResidue) Then
                chSuffixResidue = Char.ToUpper(chSuffixResidue)
                strPeptideSequenceForKey &= "."c & chSuffixResidue
            Else
                strPeptideSequenceForKey &= ".-"
            End If
        End If

        Return strPeptideSequenceForKey
    End Function

    Private Sub CreateProteinCoverageFile(strPeptideInputFilePath As String, strOutputFolderPath As String, outputFileBaseName As String)
        Const INITIAL_PROTEIN_COUNT_RESERVE = 5000

        ' The data in mProteinPeptideStats is copied into array udtPeptideStats for fast lookup
        ' This is necessary since use of the enumerator returned by mProteinPeptideStats.GetEnumerator
        '  for every protein in mProteinDataCache.mProteins leads to very slow program performance
        Dim intPeptideStatsCount = 0
        Dim udtPeptideStats() As udtPeptideCountStatsType

        If mResultsFilePath = Nothing OrElse mResultsFilePath.Length = 0 Then
            If strPeptideInputFilePath.Length > 0 Then
                mResultsFilePath = ConstructOutputFilePath(strPeptideInputFilePath, "_coverage.txt", strOutputFolderPath, outputFileBaseName)
            Else
                mResultsFilePath = Path.Combine(GetOutputFolderPath(strOutputFolderPath, String.Empty), "Peptide_coverage.txt")
            End If
        End If

        UpdateProgress("Creating the protein coverage file: " & Path.GetFileName(mResultsFilePath), 0,
           eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

        Using swOutputFile = New StreamWriter(New FileStream(mResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

            ' Note: If the column ordering is changed, be sure to update OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER and OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER
            Dim strLineOut = "Protein Name" & ControlChars.Tab &
             "Percent Coverage" & ControlChars.Tab &
             "Protein Description" & ControlChars.Tab &
             "Non Unique Peptide Count" & ControlChars.Tab &
             "Unique Peptide Count" & ControlChars.Tab &
             "Protein Residue Count"

            If mOutputProteinSequence Then
                strLineOut &= ControlChars.Tab & "Protein Sequence"
            End If
            swOutputFile.WriteLine(strLineOut)

            ' Contains pointers to entries in udtPeptideStats()
            Dim proteinIDLookup = New Dictionary(Of Integer, Integer)

            ' Populate udtPeptideStats() using dictionary mProteinPeptideStats
            If mTrackPeptideCounts Then

                ' Initially reserve space for INITIAL_PROTEIN_COUNT_RESERVE proteins
                ReDim udtPeptideStats(INITIAL_PROTEIN_COUNT_RESERVE - 1)

                Dim myEnumerator = mProteinPeptideStats.GetEnumerator
                While myEnumerator.MoveNext()
                    Dim proteinPeptideKey = myEnumerator.Current.Key

                    ' strKey will be of the form 1234::K.ABCDEFR.A
                    ' Look for the first colon
                    Dim intColonIndex = proteinPeptideKey.IndexOf(":"c)

                    If intColonIndex > 0 Then
                        Dim intProteinID = CInt(proteinPeptideKey.Substring(0, intColonIndex))
                        Dim intTargetIndex As Integer

                        If Not proteinIDLookup.TryGetValue(intProteinID, intTargetIndex) Then
                            ' ID not found; so add it

                            intTargetIndex = intPeptideStatsCount
                            intPeptideStatsCount += 1

                            proteinIDLookup.Add(intProteinID, intTargetIndex)

                            If intTargetIndex >= udtPeptideStats.Length Then
                                ' Reserve more space in the arrays
                                ReDim Preserve udtPeptideStats(udtPeptideStats.Length * 2 - 1)
                            End If
                        End If


                        ' Update the protein counts at intTargetIndex
                        udtPeptideStats(intTargetIndex).UniquePeptideCount += 1
                        udtPeptideStats(intTargetIndex).NonUniquePeptideCount += myEnumerator.Current.Value

                    End If
                End While

                ' Shrink udtPeptideStats
                If intPeptideStatsCount < udtPeptideStats.Length Then
                    ReDim Preserve udtPeptideStats(intPeptideStatsCount - 1)
                End If
            Else
                ReDim udtPeptideStats(-1)
            End If

            ' Query the SqlLite DB to extract the protein information
            Dim SQLreader = mProteinDataCache.GetSQLiteDataReader("SELECT * FROM udtProteinInfoType")

            Dim proteinIndex = 0
            While SQLreader.Read()
                ' Column names in table udtProteinInfoType:
                '  Name TEXT,
                '  Description TEXT,
                '  Sequence TEXT,
                '  UniqueSequenceID INTEGER,
                '  PercentCoverage REAL,
                '  NonUniquePeptideCount INTEGER,
                '  UniquePeptideCount INTEGER

                Dim proteinID = CInt(SQLreader("UniqueSequenceID"))

                Dim uniquePeptideCount = 0
                Dim nonUniquePeptideCount = 0

                If mTrackPeptideCounts Then
                    Dim targetIndex As Integer
                    If proteinIDLookup.TryGetValue(proteinID, targetIndex) Then
                        uniquePeptideCount = udtPeptideStats(targetIndex).UniquePeptideCount
                        nonUniquePeptideCount = udtPeptideStats(targetIndex).NonUniquePeptideCount
                    End If
                End If

                strLineOut = CStr(SQLreader("Name")) & ControlChars.Tab &
                     Math.Round(CDbl(SQLreader("PercentCoverage")) * 100, 3) & ControlChars.Tab &
                     CStr(SQLreader("Description")) & ControlChars.Tab &
                     nonUniquePeptideCount & ControlChars.Tab &
                     uniquePeptideCount & ControlChars.Tab &
                     CStr(SQLreader("Sequence")).Length

                If mOutputProteinSequence Then
                    strLineOut &= ControlChars.Tab & CStr(SQLreader("Sequence"))
                End If
                swOutputFile.WriteLine(strLineOut)

                If proteinIndex Mod 25 = 0 Then
                    UpdateProgress(proteinIndex / CSng(mProteinDataCache.GetProteinCountCached()) * 100, eProteinCoverageProcessingSteps.WriteProteinCoverageFile)
                End If

                If mAbortProcessing Then Exit While
                proteinIndex += 1
            End While

            ' Close the SQL Reader
            SQLreader.Close()

        End Using

    End Sub

    Private Function DetermineLineTerminatorSize(strInputFilePath As String) As Integer

        Dim intTerminatorSize = 2

        Try
            ' Open the input file and look for the first carriage return or line feed
            Using fsInFile = New FileStream(strInputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

                Do While fsInFile.Position < fsInFile.Length AndAlso fsInFile.Position < 100000

                    Dim intByte = fsInFile.ReadByte()

                    If intByte = 10 Then
                        ' Found linefeed
                        If fsInFile.Position < fsInFile.Length Then
                            intByte = fsInFile.ReadByte()
                            If intByte = 13 Then
                                ' LfCr
                                intTerminatorSize = 2
                            Else
                                ' Lf only
                                intTerminatorSize = 1
                            End If
                        Else
                            intTerminatorSize = 1
                        End If
                        Exit Do
                    ElseIf intByte = 13 Then
                        ' Found carriage return
                        If fsInFile.Position < fsInFile.Length Then
                            intByte = fsInFile.ReadByte()
                            If intByte = 10 Then
                                ' CrLf
                                intTerminatorSize = 2
                            Else
                                ' Cr only
                                intTerminatorSize = 1
                            End If
                        Else
                            intTerminatorSize = 1
                        End If
                        Exit Do
                    End If

                Loop
            End Using

        Catch ex As Exception
            SetErrorMessage("Error in DetermineLineTerminatorSize: " & ex.Message)
        End Try

        Return intTerminatorSize

    End Function

    ''' <summary>
    ''' Searches for proteins that contain the peptides in peptideList
    ''' If strProteinNameForPeptide is blank or mSearchAllProteinsForPeptideSequence=True then searches all proteins
    ''' Otherwise, only searches protein strProteinNameForPeptide
    ''' </summary>
    ''' <param name="peptideList">Dictionary containing the peptides to search; peptides must be in the format Prefix.Peptide.Suffix where Prefix and Suffix are single characters; peptides are assumed to only contain letters (no symbols)</param>
    ''' <param name="strProteinNameForPeptides">The protein to search; only used if mSearchAllProteinsForPeptideSequence=False</param>
    ''' <remarks></remarks>
    Private Sub FindSequenceMatchForPeptideList(peptideList As IDictionary(Of String, Integer),
      strProteinNameForPeptides As String)

        Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        Try
            ' Make sure strProteinNameForPeptide is a valid string
            If strProteinNameForPeptides Is Nothing Then
                strProteinNameForPeptides = String.Empty
            End If

            Dim intExpectedPeptideIterations = CInt(Math.Ceiling(mProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * peptideList.Count
            If intExpectedPeptideIterations < 1 Then intExpectedPeptideIterations = 1

            UpdateProgress("Finding matching proteins for peptide list", 0,
               eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

            Dim intStartIndex = 0
            Do
                ' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQL Lite database
                ' Store the information in the four local arrays
                Dim intProteinCount = ReadProteinInfoChunk(intStartIndex, blnProteinUpdated, False)

                Dim intPeptideIterationsComplete = 0

                ' Iterate through the peptides in peptideList
                Dim myEnumerator = peptideList.GetEnumerator

                Do While myEnumerator.MoveNext

                    Dim chPrefixResidue As Char
                    Dim chSuffixResidue As Char

                    ' Retrieve the next peptide from peptideList
                    ' Use GetCleanPeptideSequence() to extract out the sequence, prefix, and suffix letters (we're setting blnRemoveSymbolCharacters to False since that should have been done before the peptides were stored in peptideList)
                    ' Make sure the peptide sequence has uppercase letters
                    Dim strPeptideSequenceClean = GetCleanPeptideSequence(myEnumerator.Current.Key, chPrefixResidue, chSuffixResidue, False).ToUpper

                    Dim strPeptideSequenceForKeySource As String
                    Dim strPeptideSequenceForKey As String
                    Dim strPeptideSequenceToSearchOn As String

                    If mMatchPeptidePrefixAndSuffixToProtein Then
                        strPeptideSequenceForKeySource = ConstructPeptideSequenceForKey(strPeptideSequenceClean, chPrefixResidue, chSuffixResidue)
                    Else
                        strPeptideSequenceForKeySource = String.Copy(strPeptideSequenceClean)
                    End If

                    If mIgnoreILDifferences Then
                        ' Replace all L characters with I
                        strPeptideSequenceForKey = strPeptideSequenceForKeySource.Replace("L"c, "I"c)

                        strPeptideSequenceToSearchOn = strPeptideSequenceClean.Replace("L"c, "I"c)

                        If chPrefixResidue = "L"c Then chPrefixResidue = "I"c
                        If chSuffixResidue = "L"c Then chSuffixResidue = "I"c
                    Else
                        strPeptideSequenceToSearchOn = String.Copy(strPeptideSequenceClean)

                        ' I'm purposely not using String.Copy() here in order to obtain increased speed
                        strPeptideSequenceForKey = strPeptideSequenceForKeySource
                    End If

                    ' Search for strPeptideSequence in the protein sequences
                    For intProteinIndex = 0 To intProteinCount - 1
                        Dim blnMatchFound = False
                        Dim blnMatchIsNew As Boolean
                        Dim intStartResidue As Integer
                        Dim intEndResidue As Integer

                        If mSearchAllProteinsForPeptideSequence OrElse strProteinNameForPeptides.Length = 0 Then
                            ' Search through all Protein sequences and capitalize matches for Peptide Sequence

                            Dim strKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey
                            mCachedProteinInfo(intProteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(
                                mCachedProteinInfo(intProteinIndex).Sequence, strPeptideSequenceToSearchOn,
                                strKey, chPrefixResidue, chSuffixResidue,
                                blnMatchFound, blnMatchIsNew,
                                intStartResidue, intEndResidue)
                        Else
                            ' Only search protein strProteinNameForPeptide
                            If mCachedProteinInfo(intProteinIndex).Name = strProteinNameForPeptides Then

                                ' Define the peptide match key using the Unique Sequence ID, two colons, and the peptide sequence
                                Dim strKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey

                                ' Capitalize matching residues in sequence
                                mCachedProteinInfo(intProteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(
                                    mCachedProteinInfo(intProteinIndex).Sequence, strPeptideSequenceToSearchOn,
                                    strKey, chPrefixResidue, chSuffixResidue,
                                    blnMatchFound, blnMatchIsNew,
                                    intStartResidue, intEndResidue)
                            End If
                        End If

                        If blnMatchFound Then
                            If Not mSearchAllProteinsSkipCoverageComputationSteps Then
                                blnProteinUpdated(intProteinIndex) = True
                            End If

                            If blnMatchIsNew Then
                                If mSaveProteinToPeptideMappingFile Then
                                    WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(intProteinIndex).Name, strPeptideSequenceForKeySource, intStartResidue, intEndResidue)
                                End If

                                If mSaveSourceDataPlusProteinsFile Then
                                    StorePeptideToProteinMatch(strPeptideSequenceClean, mCachedProteinInfo(intProteinIndex).Name)
                                End If

                            End If
                        End If

                    Next intProteinIndex

                    intPeptideIterationsComplete += 1

                    If intPeptideIterationsComplete Mod 10 = 0 Then
                        UpdateProgress(CSng((intPeptideIterationsComplete / intExpectedPeptideIterations) * 100),
                           eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

                    End If
                Loop

                ' Store the updated protein sequence information in the database
                UpdateSequenceDbDataValues(blnProteinUpdated, intProteinCount)

                ' Increment intStartIndex to obtain the next chunk of proteins
                intStartIndex += PROTEIN_CHUNK_COUNT

            Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

        Catch ex As Exception
            SetErrorMessage("Error in FindSequenceMatch:" & ControlChars.NewLine & ex.Message)
        End Try

    End Sub

    Private Sub UpdateSequenceDbDataValues(blnProteinUpdated As IList(Of Boolean), intProteinCount As Integer)
        Try
            If Not BooleanArrayContainsTrueEntries(blnProteinUpdated, intProteinCount) Then
                ' All of the entries in blnProteinUpdated() are False; nothing to update
                Exit Sub
            End If

            ' Store the updated protein sequences in the Sql Lite database
            Dim SQLconnect = mProteinDataCache.ConnectToSqlLiteDB(True)

            Using dbTrans As SQLiteTransaction = SQLconnect.BeginTransaction()
                Using cmd As SQLiteCommand = SQLconnect.CreateCommand()

                    ' Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set Sequence = ? Where UniqueSequenceID = ?"

                    Dim SequenceFld As SQLiteParameter = cmd.CreateParameter
                    Dim UniqueSequenceIDFld As SQLiteParameter = cmd.CreateParameter
                    cmd.Parameters.Add(SequenceFld)
                    cmd.Parameters.Add(UniqueSequenceIDFld)

                    ' Update each protein that has blnProteinUpdated(intProteinIndex) = True
                    For intProteinIndex = 0 To intProteinCount - 1
                        If blnProteinUpdated(intProteinIndex) Then
                            UniqueSequenceIDFld.Value = mCachedProteinInfo(intProteinIndex).UniqueSequenceID
                            SequenceFld.Value = mCachedProteinInfo(intProteinIndex).Sequence
                            cmd.ExecuteNonQuery()
                        End If
                    Next
                End Using
                dbTrans.Commit()
            End Using

            ' Close the Sql Reader
            SQLconnect.Close()
            SQLconnect.Dispose()

        Catch ex As Exception
            SetErrorMessage("Error in UpdateSequenceDbDataValues: " & ex.Message)
        End Try

    End Sub

    Public Shared Function GetAppFolderPath() As String
        ' Could use Application.StartupPath, but .GetExecutingAssembly is better
        Return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    End Function

    Public Shared Function GetCleanPeptideSequence(strPeptideSequence As String,
      <Out> ByRef chPrefixResidue As Char,
      <Out> ByRef chSuffixResidue As Char,
      blnRemoveSymbolCharacters As Boolean) As String

        Static reReplaceSymbols As Regex = New Regex("[^A-Za-z]", RegexOptions.Compiled)

        chPrefixResidue = Nothing
        chSuffixResidue = Nothing

        If strPeptideSequence.Length >= 4 Then
            If strPeptideSequence.Chars(1) = "."c AndAlso strPeptideSequence.Chars(strPeptideSequence.Length - 2) = "."c Then
                chPrefixResidue = strPeptideSequence.Chars(0)
                chSuffixResidue = strPeptideSequence.Chars(strPeptideSequence.Length - 1)
                strPeptideSequence = strPeptideSequence.Substring(2, strPeptideSequence.Length - 4)
            End If
        End If

        If blnRemoveSymbolCharacters Then
            strPeptideSequence = reReplaceSymbols.Replace(strPeptideSequence, String.Empty)
        End If

        Return strPeptideSequence

    End Function

    Public Function GetErrorMessage() As String
        ' Returns String.Empty if no error

        Dim strMessage As String

        Select Case Me.ErrorCode
            Case eProteinCoverageErrorCodes.NoError
                strMessage = String.Empty
            Case eProteinCoverageErrorCodes.InvalidInputFilePath
                strMessage = "Invalid input file path"
                ''Case eProteinCoverageErrorCodes.InvalidOutputFolderPath
                ''    strMessage = "Invalid output folder path"
                ''Case eProteinCoverageErrorCodes.ParameterFileNotFound
                ''    strMessage = "Parameter file not found"

                ''Case eProteinCoverageErrorCodes.ErrorReadingInputFile
                ''    strMessage = "Error reading input file"
                ''Case eProteinCoverageErrorCodes.ErrorCreatingOutputFiles
                ''    strMessage = "Error creating output files"

            Case eProteinCoverageErrorCodes.ErrorReadingParameterFile
                strMessage = "Invalid parameter file"

            Case eProteinCoverageErrorCodes.FilePathError
                strMessage = "General file path error"
            Case eProteinCoverageErrorCodes.UnspecifiedError
                strMessage = "Unspecified error"
            Case Else
                ' This shouldn't happen
                strMessage = "Unknown error state"
        End Select

        If mErrorMessage.Length > 0 Then
            If strMessage.Length > 0 Then
                strMessage &= "; "
            End If
            strMessage &= mErrorMessage
        End If

        Return strMessage
    End Function

    ''' <summary>
    ''' Determine the output folder path
    ''' Uses strOutputFolderPath if defined
    ''' Otherwise uses the folder where strOutputFilePath resides
    ''' </summary>
    ''' <param name="strOutputFolderPath"></param>
    ''' <param name="strOutputFilePath"></param>
    ''' <returns></returns>
    ''' <remarks>If an error, or unable to determine a folder, returns the folder with the application files</remarks>
    Public Shared Function GetOutputFolderPath(strOutputFolderPath As String, strOutputFilePath As String) As String

        Try
            If Not String.IsNullOrWhiteSpace(strOutputFolderPath) Then
                strOutputFolderPath = Path.GetFullPath(strOutputFolderPath)
            Else
                strOutputFolderPath = Path.GetDirectoryName(strOutputFilePath)
            End If

            If Not Directory.Exists(strOutputFolderPath) Then
                Directory.CreateDirectory(strOutputFolderPath)
            End If

        Catch ex As Exception
            strOutputFolderPath = GetAppFolderPath()
        End Try

        Return strOutputFolderPath

    End Function

    Private Sub GetPercentCoverage()

        Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        UpdateProgress("Computing percent coverage", 0,
           eProteinCoverageProcessingSteps.ComputePercentCoverage)

        Dim intStartIndex = 0
        Dim intIndex = 0
        Do
            ' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
            ' Store the information in the four local arrays
            Dim intProteinCount = ReadProteinInfoChunk(intStartIndex, blnProteinUpdated, False)

            For intProteinIndex = 0 To intProteinCount - 1

                If Not mCachedProteinInfo(intProteinIndex).Sequence Is Nothing Then
                    Dim charArray = mCachedProteinInfo(intProteinIndex).Sequence.ToCharArray()
                    Dim intCapitalLetterCount = 0
                    For Each character In charArray
                        If Char.IsUpper(character) Then intCapitalLetterCount += 1
                    Next

                    mCachedProteinInfo(intProteinIndex).PercentCoverage = intCapitalLetterCount / mCachedProteinInfo(intProteinIndex).Sequence.Length
                    If mCachedProteinInfo(intProteinIndex).PercentCoverage > 0 Then
                        blnProteinUpdated(intProteinIndex) = True
                    End If
                End If

                If intIndex Mod 100 = 0 Then
                    UpdateProgress(intIndex / CSng(mProteinDataCache.GetProteinCountCached()) * 100,
                       eProteinCoverageProcessingSteps.ComputePercentCoverage)
                End If

                intIndex += 1
            Next

            UpdatePercentCoveragesDbDataValues(blnProteinUpdated, intProteinCount)

            ' Increment intStartIndex to obtain the next chunk of proteins
            intStartIndex += PROTEIN_CHUNK_COUNT

        Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

    End Sub

    Private Sub UpdatePercentCoveragesDbDataValues(blnProteinUpdated() As Boolean, intProteinCount As Integer)
        Try
            If Not BooleanArrayContainsTrueEntries(blnProteinUpdated, intProteinCount) Then
                ' All of the entries in blnProteinUpdated() are False; nothing to update
                Exit Sub
            End If

            ' Store the updated protein coverage values in the Sql Lite database
            Dim SQLconnect = mProteinDataCache.ConnectToSqlLiteDB(True)

            Using dbTrans As SQLiteTransaction = SQLconnect.BeginTransaction()
                Using cmd As SQLiteCommand = SQLconnect.CreateCommand()

                    ' Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set PercentCoverage = ? Where UniqueSequenceID = ?"

                    Dim PercentCoverageFld As SQLiteParameter = cmd.CreateParameter
                    Dim UniqueSequenceIDFld As SQLiteParameter = cmd.CreateParameter
                    cmd.Parameters.Add(PercentCoverageFld)
                    cmd.Parameters.Add(UniqueSequenceIDFld)

                    ' Update each protein that has blnProteinUpdated(intProteinIndex) = True
                    For intProteinIndex = 0 To intProteinCount - 1
                        If blnProteinUpdated(intProteinIndex) Then
                            UniqueSequenceIDFld.Value = mCachedProteinInfo(intProteinIndex).UniqueSequenceID
                            PercentCoverageFld.Value = mCachedProteinInfo(intProteinIndex).PercentCoverage
                            cmd.ExecuteNonQuery()
                        End If
                    Next
                End Using
                dbTrans.Commit()
            End Using

            ' Close the Sql Reader
            SQLconnect.Close()
            SQLconnect.Dispose()

        Catch ex As Exception
            SetErrorMessage("Error in UpdatePercentCoveragesDbDataValues: " & ex.Message)
            If Not mShowMessages Then Throw New Exception("Error in UpdatePercentCoveragesDbDataValues", ex)
        End Try

    End Sub

    ''' <summary>
    ''' Increment the observation count for the given key in the given dictionary
    ''' If the key is not defined, add it
    ''' </summary>
    ''' <param name="oDictionary"></param>
    ''' <param name="key"></param>
    ''' <returns>True if the protein is new and was added tomProteinPeptideStats </returns>
    Private Function IncrementCountByKey(oDictionary As IDictionary(Of String, Integer), key As String) As Boolean
        Dim value As Integer

        If oDictionary.TryGetValue(key, value) Then
            oDictionary(key) = value + 1
            Return False
        Else
            oDictionary.Add(key, 1)
            Return True
        End If
    End Function

    Private Sub InitializeVariables()
        mAbortProcessing = False
        mShowMessages = True
        mErrorMessage = String.Empty

        mProteinInputFilePath = String.Empty
        mResultsFilePath = String.Empty

        mProteinDataCache = New clsProteinFileDataCache()
        RegisterEvents(mProteinDataCache)

        mCachedProteinInfoStartIndex = -1

        mPeptideFileSkipFirstLine = False
        mPeptideInputFileDelimiter = ControlChars.Tab
        mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence

        mOutputProteinSequence = True
        mSearchAllProteinsForPeptideSequence = True
        mSearchAllProteinsSkipCoverageComputationSteps = False
        mUseLeaderSequenceHashTable = True

        mSaveProteinToPeptideMappingFile = False
        mProteinToPeptideMappingFilePath = String.Empty

        mSaveSourceDataPlusProteinsFile = False

        mTrackPeptideCounts = True
        mRemoveSymbolCharacters = True
        mMatchPeptidePrefixAndSuffixToProtein = False
        mIgnoreILDifferences = False

        ' Define the percent complete values to use for the start of each processing step

        ReDim mPercentCompleteStartLevels(PERCENT_COMPLETE_LEVEL_COUNT)

        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.Starting) = 0
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.CacheProteins) = 1
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.DetermineShortestPeptideLength) = 45
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.CachePeptides) = 50
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences) = 55
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides) = 90
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.ComputePercentCoverage) = 95
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.WriteProteinCoverageFile) = 97
        mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile) = 98
        mPercentCompleteStartLevels(PERCENT_COMPLETE_LEVEL_COUNT) = 100

    End Sub

    Public Function LoadParameterFileSettings(strParameterFilePath As String) As Boolean

        Try

            If String.IsNullOrWhiteSpace(strParameterFilePath) Then
                ' No parameter file specified; default settings will be used
                Return True
            End If

            If Not File.Exists(strParameterFilePath) Then
                ' See if strParameterFilePath points to a file in the same directory as the application
                Dim strAlternateFilePath = Path.Combine(GetAppFolderPath(), Path.GetFileName(strParameterFilePath))
                If Not File.Exists(strAlternateFilePath) Then
                    ' Parameter file still not found
                    SetErrorMessage("Parameter file not found: " & strParameterFilePath)
                    Return False
                Else
                    strParameterFilePath = String.Copy(strAlternateFilePath)
                End If
            End If

            Dim objSettingsFile = New XmlSettingsFileAccessor

            If objSettingsFile.LoadSettings(strParameterFilePath) Then

                If Not objSettingsFile.SectionPresent(XML_SECTION_PROCESSING_OPTIONS) Then
                    OnWarningEvent("The node '<section name=""" & XML_SECTION_PROCESSING_OPTIONS & """> was not found in the parameter file: " & strParameterFilePath)
                Else
                    OutputProteinSequence = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", Me.OutputProteinSequence)
                    SearchAllProteinsForPeptideSequence = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", Me.SearchAllProteinsForPeptideSequence)
                    SaveProteinToPeptideMappingFile = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", Me.SaveProteinToPeptideMappingFile)
                    SaveSourceDataPlusProteinsFile = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "mSaveSourceDataPlusProteinsFile", Me.SaveSourceDataPlusProteinsFile)

                    TrackPeptideCounts = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", Me.TrackPeptideCounts)
                    RemoveSymbolCharacters = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", Me.RemoveSymbolCharacters)
                    MatchPeptidePrefixAndSuffixToProtein = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", Me.MatchPeptidePrefixAndSuffixToProtein)
                    IgnoreILDifferences = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", Me.IgnoreILDifferences)

                    PeptideFileSkipFirstLine = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", Me.PeptideFileSkipFirstLine)
                    PeptideInputFileDelimiter = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", Me.PeptideInputFileDelimiter).Chars(0)
                    PeptideFileFormatCode = CType(objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", CInt(Me.PeptideFileFormatCode)), ePeptideFileColumnOrderingCode)

                    mProteinDataCache.DelimitedFileSkipFirstLine = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", mProteinDataCache.DelimitedFileSkipFirstLine)
                    mProteinDataCache.DelimitedFileDelimiter = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", mProteinDataCache.DelimitedFileDelimiter).Chars(0)
                    mProteinDataCache.DelimitedFileFormatCode = CType(objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", CInt(mProteinDataCache.DelimitedFileFormatCode)), DelimitedFileReader.eDelimitedFileFormatCode)

                End If

            Else
                SetErrorMessage("Error calling objSettingsFile.LoadSettings for " & strParameterFilePath)
                Return False
            End If

        Catch ex As Exception
            SetErrorMessage("Error in LoadParameterFileSettings:" & ex.Message)
            SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
            If Not mShowMessages Then Throw New Exception("Error in LoadParameterFileSettings", ex)
            Return False
        End Try

        Return True

    End Function

    Private Function ParsePeptideInputFile(
      strPeptideInputFilePath As String,
      strOutputFolderPath As String,
      outputFileBaseName As String,
      <Out> ByRef strProteinToPeptideMappingFilePath As String) As Boolean

        Const MAX_SHORT_PEPTIDES_TO_CACHE = 1000000

        strProteinToPeptideMappingFilePath = String.Empty

        Try
            ' Initialize chSepChars
            Dim chSepChars = New Char() {mPeptideInputFileDelimiter}

            ' Initialize some dictionaries

            Dim shortPeptideCache = New Dictionary(Of String, Integer)

            If mProteinPeptideStats Is Nothing Then
                mProteinPeptideStats = New Dictionary(Of String, Integer)
            Else
                mProteinPeptideStats.Clear()
            End If

            If mPeptideToProteinMapResults Is Nothing Then
                mPeptideToProteinMapResults = New Dictionary(Of String, List(Of String))
            Else
                mPeptideToProteinMapResults.Clear()
            End If

            If Not File.Exists(strPeptideInputFilePath) Then
                SetErrorMessage("File not found: " & strPeptideInputFilePath)
                Return False
            End If

            Dim strProgressMessageBase = "Reading peptides from " & Path.GetFileName(strPeptideInputFilePath)
            If mUseLeaderSequenceHashTable Then
                strProgressMessageBase &= " and finding leader sequences"
            Else
                If Not mSearchAllProteinsSkipCoverageComputationSteps Then
                    strProgressMessageBase &= " and computing coverage"
                End If
            End If

            mProgressStepDescription = String.Copy(strProgressMessageBase)
            Console.WriteLine()
            Console.WriteLine()
            Console.WriteLine("Parsing " & Path.GetFileName(strPeptideInputFilePath))
            Console.WriteLine(mProgressStepDescription)

            UpdateProgress(mProgressStepDescription, 0,
               eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

            ' Open the file and read, at most, the first 100,000 characters to see if it contains CrLf or just Lf
            Dim intTerminatorSize = DetermineLineTerminatorSize(strPeptideInputFilePath)

            ' Possibly open the file and read the first few line to make sure the number of columns is appropriate
            Dim blnSuccess = ValidateColumnCountInInputFile(strPeptideInputFilePath)
            If Not blnSuccess Then
                Return False
            End If

            If mUseLeaderSequenceHashTable Then
                ' Determine the shortest peptide present in the input file
                ' This is a fast process that involves checking the length of each sequence in the input file

                UpdateProgress("Determining the shortest peptide in the input file", 0,
                   eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

                If mLeaderSequenceCache Is Nothing Then
                    mLeaderSequenceCache = New clsLeaderSequenceCache
                Else
                    mLeaderSequenceCache.InitializeVariables()
                End If
                mLeaderSequenceCache.IgnoreILDifferences = mIgnoreILDifferences

                Dim intColumnNumWithPeptideSequence As Integer
                Select Case mPeptideFileColumnOrdering
                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                        intColumnNumWithPeptideSequence = 2
                    Case Else
                        ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                        intColumnNumWithPeptideSequence = 1
                End Select

                mLeaderSequenceCache.DetermineShortestPeptideLengthInFile(strPeptideInputFilePath, intTerminatorSize, mPeptideFileSkipFirstLine, mPeptideInputFileDelimiter, intColumnNumWithPeptideSequence)

                If mAbortProcessing Then
                    Return False
                Else
                    strProgressMessageBase &= " (leader seq length = " & mLeaderSequenceCache.LeaderSequenceMinimumLength.ToString & ")"

                    UpdateProgress(strProgressMessageBase)
                End If
            End If

            Dim intInvalidLineCount = 0

            ' Open the peptide file and read in the lines
            Using srInFile = New StreamReader(New FileStream(strPeptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                ' Create the protein to peptide match details file
                mProteinToPeptideMappingFilePath = ConstructOutputFilePath(strPeptideInputFilePath, FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                                                                           strOutputFolderPath, outputFileBaseName)

                If mSaveProteinToPeptideMappingFile Then
                    strProteinToPeptideMappingFilePath = String.Copy(mProteinToPeptideMappingFilePath)

                    UpdateProgress("Creating the protein to peptide mapping file: " & Path.GetFileName(mProteinToPeptideMappingFilePath))

                    mProteinToPeptideMappingOutputFile = New StreamWriter(New FileStream(mProteinToPeptideMappingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) With {
                        .AutoFlush = True
                    }

                    mProteinToPeptideMappingOutputFile.WriteLine("Protein Name" & ControlChars.Tab & "Peptide Sequence" & ControlChars.Tab & "Residue Start" & ControlChars.Tab & "Residue End")
                End If

                Dim intCurrentLine = 1
                Dim bytesRead As Long = 0

                Do While Not srInFile.EndOfStream
                    If mAbortProcessing Then Exit Do

                    Dim strLineIn = srInFile.ReadLine()
                    If strLineIn Is Nothing Then Continue Do

                    bytesRead += strLineIn.Length + intTerminatorSize

                    strLineIn = strLineIn.Trim

                    If intCurrentLine Mod 500 = 0 Then
                        UpdateProgress("Reading peptide input file", CSng((bytesRead / srInFile.BaseStream.Length) * 100),
                           eProteinCoverageProcessingSteps.CachePeptides)
                    End If

                    If intCurrentLine = 1 AndAlso mPeptideFileSkipFirstLine Then
                        ' do nothing, skip the first line
                    ElseIf strLineIn.Length > 0 Then

                        Dim blnValidLine = False
                        Dim strProteinName = ""
                        Dim strPeptideSequence = ""

                        Try

                            ' Split the line, but for efficiency purposes, only parse out the first 3 columns
                            Dim strSplitLine = strLineIn.Split(chSepChars, 3)

                            Select Case mPeptideFileColumnOrdering
                                Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                                    strProteinName = strSplitLine(0)

                                    If strSplitLine.Length > 1 AndAlso Not String.IsNullOrWhiteSpace(strSplitLine(1)) Then
                                        strPeptideSequence = strSplitLine(1)
                                        blnValidLine = True
                                    End If
                                Case Else
                                    ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                    strPeptideSequence = strSplitLine(0)
                                    strProteinName = String.Empty
                                    blnValidLine = True
                            End Select

                        Catch ex As Exception
                            blnValidLine = False
                        End Try

                        If blnValidLine Then
                            ' Check for and remove prefix and suffix letters
                            ' Also possibly remove symbol characters

                            Dim chPrefixResidue As Char
                            Dim chSuffixResidue As Char
                            strPeptideSequence = GetCleanPeptideSequence(strPeptideSequence, chPrefixResidue, chSuffixResidue, mRemoveSymbolCharacters)

                            If mUseLeaderSequenceHashTable AndAlso
                             strPeptideSequence.Length >= mLeaderSequenceCache.LeaderSequenceMinimumLength Then

                                If mLeaderSequenceCache.CachedPeptideCount >= clsLeaderSequenceCache.MAX_LEADER_SEQUENCE_COUNT Then
                                    ' Need to step through the proteins and match them to the data in mLeaderSequenceCache
                                    SearchProteinsUsingLeaderSequences()
                                    mLeaderSequenceCache.InitializeCachedPeptides()
                                End If

                                mLeaderSequenceCache.CachePeptide(strPeptideSequence, strProteinName, chPrefixResidue, chSuffixResidue)
                            Else
                                ' Either mUseLeaderSequenceHashTable is false, or the peptide sequence is less than MINIMUM_LEADER_SEQUENCE_LENGTH residues long
                                ' We must search all proteins for the given peptide

                                ' Cache the short peptides in shortPeptideCache
                                If shortPeptideCache.Count >= MAX_SHORT_PEPTIDES_TO_CACHE Then
                                    ' Step through the proteins and match them to the data in shortPeptideCache
                                    SearchProteinsUsingCachedPeptides(shortPeptideCache)
                                    shortPeptideCache.Clear()
                                End If

                                Dim strPeptideSequenceToCache = chPrefixResidue & "." & strPeptideSequence & "." & chSuffixResidue

                                IncrementCountByKey(shortPeptideCache, strPeptideSequenceToCache)
                            End If

                        Else
                            intInvalidLineCount += 1
                        End If

                    End If
                    intCurrentLine += 1

                Loop

            End Using

            If mUseLeaderSequenceHashTable Then
                ' Step through the proteins and match them to the data in mLeaderSequenceCache
                If mLeaderSequenceCache.CachedPeptideCount > 0 Then
                    SearchProteinsUsingLeaderSequences()
                End If
            End If

            ' Step through the proteins and match them to the data in shortPeptideCache
            SearchProteinsUsingCachedPeptides(shortPeptideCache)

            If Not mAbortProcessing And Not mSearchAllProteinsSkipCoverageComputationSteps Then
                ' Compute the residue coverage percent for each protein
                GetPercentCoverage()
            End If

            If Not mProteinToPeptideMappingOutputFile Is Nothing Then
                mProteinToPeptideMappingOutputFile.Close()
                mProteinToPeptideMappingOutputFile = Nothing
            End If

            If mSaveSourceDataPlusProteinsFile Then
                ' Create a new version of the input file, but with all of the proteins listed
                SaveDataPlusAllProteinsFile(strPeptideInputFilePath, strOutputFolderPath, outputFileBaseName, chSepChars, intTerminatorSize)

            End If

            If intInvalidLineCount > 0 Then
                Select Case mPeptideFileColumnOrdering
                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                        OnWarningEvent("Found " & intInvalidLineCount & " lines that did not have two columns (Protein Name and Peptide Sequence).  Those line(s) have been skipped.")
                    Case Else
                        OnWarningEvent("Found " & intInvalidLineCount & " lines that did not contain a peptide sequence.  Those line(s) have been skipped.")
                End Select
            End If

        Catch ex As Exception
            SetErrorMessage("Error in ParsePeptideInputFile: " & ex.Message)
        End Try

        Return Not mAbortProcessing

    End Function

    Private Function ParseProteinInputFile() As Boolean
        Dim blnSuccess = False

        Try
            mProgressStepDescription = "Reading protein input file"

            With mProteinDataCache

                ' Protein file options
                If clsProteinFileDataCache.IsFastaFile(mProteinInputFilePath) Then
                    ' .fasta or .fsa file
                    mProteinDataCache.AssumeFastaFile = True
                ElseIf Path.GetExtension(mProteinInputFilePath).ToLower() = ".txt" Then
                    mProteinDataCache.AssumeDelimitedFile = True
                Else
                    mProteinDataCache.AssumeFastaFile = False
                End If

                If mSearchAllProteinsSkipCoverageComputationSteps Then
                    ' Make sure all of the protein sequences are uppercase
                    .ChangeProteinSequencesToLowercase = False
                    .ChangeProteinSequencesToUppercase = True
                Else
                    ' Make sure all of the protein sequences are lowercase
                    .ChangeProteinSequencesToLowercase = True
                    .ChangeProteinSequencesToUppercase = False
                End If

                blnSuccess = .ParseProteinFile(mProteinInputFilePath)

                If Not blnSuccess Then
                    SetErrorMessage("Error parsing protein file: " & .StatusMessage)
                Else
                    If .GetProteinCountCached = 0 Then
                        blnSuccess = False
                        SetErrorMessage("Error parsing protein file: no protein entries were found in the file.  Please verify that the column order defined for the proteins file is correct.")
                    End If
                End If
            End With

        Catch ex As Exception
            SetErrorMessage("Error in ParseProteinInputFile: " & ex.Message)
        End Try

        Return blnSuccess
    End Function

    Public Function ProcessFile(strInputFilePath As String,
      strOutputFolderPath As String,
      strParameterFilePath As String,
      blnResetErrorCode As Boolean) As Boolean

        Dim strProteinToPeptideMappingFilePath As String = String.Empty

        Return ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, blnResetErrorCode, strProteinToPeptideMappingFilePath)
    End Function

    Public Function ProcessFile(
      strInputFilePath As String,
      strOutputFolderPath As String,
      strParameterFilePath As String,
      blnResetErrorCode As Boolean,
      <Out> ByRef strProteinToPeptideMappingFilePath As String,
      Optional outputFileBaseName As String = "") As Boolean

        Dim blnSuccess As Boolean

        If blnResetErrorCode Then
            SetErrorCode(eProteinCoverageErrorCodes.NoError)
        End If

        Console.WriteLine("Initializing")
        strProteinToPeptideMappingFilePath = String.Empty

        If Not LoadParameterFileSettings(strParameterFilePath) Then
            SetErrorMessage("Parameter file load error: " & strParameterFilePath)

            If mErrorCode = eProteinCoverageErrorCodes.NoError Then
                SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
            End If

            Return False
        End If

        Try
            mCachedProteinInfoStartIndex = -1
            With mProteinDataCache
                .ShowMessages = mShowMessages
                .RemoveSymbolCharacters = Me.RemoveSymbolCharacters
                .IgnoreILDifferences = Me.IgnoreILDifferences
            End With

            If String.IsNullOrWhiteSpace(strInputFilePath) Then
                Console.WriteLine("Input file name is empty")
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            End If

            ' Note that the results file path will be auto-defined in CreateProteinCoverageFile
            mResultsFilePath = String.Empty

            If String.IsNullOrWhiteSpace(mProteinInputFilePath) Then
                SetErrorMessage("Protein file name is empty")
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            ElseIf Not File.Exists(mProteinInputFilePath) Then
                SetErrorMessage("Protein input file not found: " & mProteinInputFilePath)
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            End If

            mProteinDataCache.DeleteSQLiteDBFile()

            ' First read the protein input file
            mProgressStepDescription = "Reading protein input file: " & Path.GetFileName(mProteinInputFilePath)
            Console.WriteLine(mProgressStepDescription)
            UpdateProgress(mProgressStepDescription, 0, eProteinCoverageProcessingSteps.CacheProteins)

            blnSuccess = ParseProteinInputFile()

            If blnSuccess Then
                Console.WriteLine()
                mProgressStepDescription = "Complete reading protein input file: " & Path.GetFileName(mProteinInputFilePath)
                Console.WriteLine(mProgressStepDescription)
                UpdateProgress(mProgressStepDescription, 100, eProteinCoverageProcessingSteps.CacheProteins)

                ' Now read the peptide input file
                blnSuccess = ParsePeptideInputFile(strInputFilePath, strOutputFolderPath, outputFileBaseName, strProteinToPeptideMappingFilePath)

                If blnSuccess And Not mSearchAllProteinsSkipCoverageComputationSteps Then
                    CreateProteinCoverageFile(strInputFilePath, strOutputFolderPath, outputFileBaseName)
                End If

                UpdateProgress("Processing complete; deleting the temporary SqlLite database", 100,
                   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

                'All done; delete the temporary SqlLite database
                mProteinDataCache.DeleteSQLiteDBFile()

                UpdateProgress("Done")

                mProteinPeptideStats = Nothing
            End If

        Catch ex As Exception
            SetErrorMessage("Error in ProcessFile:" & ControlChars.NewLine & ex.Message)
            If Not mShowMessages Then Throw New Exception("Error in ProcessFile", ex)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    ''' <summary>
    ''' Read the next chunk of proteins from the database (SequenceID, ProteinName, ProteinSequence)
    ''' </summary>
    ''' <returns>The number of records read</returns>
    ''' <remarks></remarks>
    Private Function ReadProteinInfoChunk(intStartIndex As Integer, blnProteinUpdated() As Boolean, blnForceReload As Boolean) As Integer

        ' We use a SQLLite database to store the protein sequences (to avoid running out of memory when parsing large protein lists)
        ' However, we will store the most recently loaded peptides in mCachedProteinInfoCount() and
        ' will only reload them if intStartIndex is different than mCachedProteinInfoStartIndex

        ' Reset the values in blnProteinUpdated()
        Array.Clear(blnProteinUpdated, 0, blnProteinUpdated.Length)

        If Not blnForceReload AndAlso
           mCachedProteinInfoStartIndex >= 0 AndAlso
           mCachedProteinInfoStartIndex = intStartIndex AndAlso
           Not mCachedProteinInfo Is Nothing Then

            ' The data loaded in memory is already valid; no need to reload
            Return mCachedProteinInfoCount
        End If

        ' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
        ' Store the information in the four local arrays

        Dim strSqlCommand As String
        strSqlCommand = " SELECT UniqueSequenceID, Name, Description, Sequence, PercentCoverage" &
         " FROM udtProteinInfoType" &
         " WHERE UniqueSequenceID BETWEEN " & CStr(intStartIndex) & " AND " & CStr(intStartIndex + PROTEIN_CHUNK_COUNT - 1)

        Dim SQLreader As SQLiteDataReader
        SQLreader = mProteinDataCache.GetSQLiteDataReader(strSqlCommand)

        mCachedProteinInfoStartIndex = intStartIndex
        mCachedProteinInfoCount = 0
        If mCachedProteinInfo Is Nothing Then
            ReDim mCachedProteinInfo(PROTEIN_CHUNK_COUNT - 1)
        End If

        While SQLreader.Read
            With mCachedProteinInfo(mCachedProteinInfoCount)
                .UniqueSequenceID = CInt(SQLreader("UniqueSequenceID"))
                .Description = CStr(SQLreader("Description"))
                .Name = CStr(SQLreader("Name"))
                .Sequence = CStr(SQLreader("Sequence"))
                .PercentCoverage = CDbl(SQLreader("PercentCoverage"))
            End With

            mCachedProteinInfoCount += 1
        End While

        ' Close the Sql Reader
        SQLreader.Close()

        Return mCachedProteinInfoCount

    End Function

    Private Sub SaveDataPlusAllProteinsFile(
      strPeptideInputFilePath As String,
      strOutputFolderPath As String,
      outputFileBaseName As String,
      chSepChars() As Char,
      intTerminatorSize As Integer)

        Try
            Dim strDataPlusAllProteinsFile = ConstructOutputFilePath(strPeptideInputFilePath, FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS,
                                                                 strOutputFolderPath, outputFileBaseName)

            UpdateProgress("Creating the data plus all-proteins output file: " & Path.GetFileName(strDataPlusAllProteinsFile))

            Using swDataPlusAllProteinsFile = New StreamWriter(New FileStream(strDataPlusAllProteinsFile, FileMode.Create, FileAccess.Write, FileShare.Read))

                Dim intCurrentLine = 1
                Dim bytesRead As Long = 0

                Using srInFile = New StreamReader(New FileStream(strPeptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Do While Not srInFile.EndOfStream
                        Dim strLineIn = srInFile.ReadLine()
                        If strLineIn Is Nothing Then Continue Do

                        bytesRead += strLineIn.Length + intTerminatorSize
                        strLineIn = strLineIn.Trim()

                        If intCurrentLine Mod 500 = 0 Then
                            UpdateProgress("Creating the data plus all-proteins output file", CSng((bytesRead / srInFile.BaseStream.Length) * 100), eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile)
                        End If

                        If intCurrentLine = 1 AndAlso mPeptideFileSkipFirstLine Then
                            ' Print out the first line, but append a new column name
                            swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "Protein_Name")

                        ElseIf strLineIn.Length > 0 Then

                            Dim blnValidLine = False
                            Dim strPeptideSequence = ""

                            Try

                                ' Split the line, but for efficiency purposes, only parse out the first 3 columns
                                Dim strSplitLine = strLineIn.Split(chSepChars, 3)

                                Select Case mPeptideFileColumnOrdering
                                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                                        ' strProteinName = strSplitLine(0)

                                        If strSplitLine.Length > 1 AndAlso Not String.IsNullOrWhiteSpace(strSplitLine(1)) Then
                                            strPeptideSequence = strSplitLine(1)
                                            blnValidLine = True
                                        End If
                                    Case Else
                                        ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                        strPeptideSequence = strSplitLine(0)
                                        ' strProteinName = String.Empty
                                        blnValidLine = True
                                End Select

                            Catch ex As Exception
                                blnValidLine = False
                            End Try

                            If Not blnValidLine Then
                                swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "?")
                            Else
                                Dim chPrefixResidue As Char
                                Dim chSuffixResidue As Char
                                strPeptideSequence = GetCleanPeptideSequence(strPeptideSequence, chPrefixResidue, chSuffixResidue, mRemoveSymbolCharacters)

                                Dim lstProteins As List(Of String) = Nothing
                                If mPeptideToProteinMapResults.TryGetValue(strPeptideSequence, lstProteins) Then

                                    For Each strProtein As String In lstProteins
                                        swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & strProtein)
                                    Next
                                Else
                                    If intCurrentLine = 1 Then
                                        ' This is likely a header line
                                        swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "Protein_Name")
                                    Else
                                        swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "?")
                                    End If
                                End If
                            End If
                        Else
                            swDataPlusAllProteinsFile.WriteLine()
                        End If
                    Loop


                End Using

            End Using

        Catch ex As Exception
            SetErrorMessage("Error in SaveDataPlusAllProteinsFile: " & ex.Message)
            If Not mShowMessages Then Throw New Exception("Error in SaveDataPlusAllProteinsFile", ex)
        End Try
    End Sub

    Private Sub SearchProteinsUsingLeaderSequences()

        Dim intLeaderSequenceMinimumLength As Integer = mLeaderSequenceCache.LeaderSequenceMinimumLength

        Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        ' Step through the proteins in memory and compare the residues for each to mLeaderSequenceHashTable
        ' If mSearchAllProteinsForPeptideSequence = False, then require that the protein name in the peptide input file matches the protein being examined

        Try
            Dim strProgressMessageBase = "Comparing proteins to peptide leader sequences"
            OnStatusEvent(strProgressMessageBase)

            Dim intProteinProcessIterations = 0
            Dim intProteinProcessIterationsExpected = CInt(Math.Ceiling(mProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * PROTEIN_CHUNK_COUNT
            If intProteinProcessIterationsExpected < 1 Then intProteinProcessIterationsExpected = 1

            UpdateProgress(strProgressMessageBase, 0,
               eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)

            Dim intStartIndex = 0
            Do
                ' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
                ' Store the information in the four local arrays
                Dim intProteinCount = ReadProteinInfoChunk(intStartIndex, blnProteinUpdated, False)

                For intProteinIndex = 0 To intProteinCount - 1

                    Dim strProteinSequence = String.Copy(mCachedProteinInfo(intProteinIndex).Sequence)
                    Dim blnProteinSequenceUpdated = False

                    For intProteinSeqCharIndex = 0 To strProteinSequence.Length - intLeaderSequenceMinimumLength

                        Dim intCachedPeptideMatchIndex As Integer

                        ' Call .GetFirstPeptideIndexForLeaderSequence to see if the sequence cache contains the intLeaderSequenceMinimumLength residues starting at intProteinSeqCharIndex
                        If mSearchAllProteinsSkipCoverageComputationSteps Then
                            ' No need to capitalize strProteinSequence since it's already capitalized
                            intCachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength))
                        Else
                            ' Need to change strProteinSequence to all caps when calling GetFirstPeptideIndexForLeaderSequence
                            intCachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength).ToUpper)
                        End If


                        If intCachedPeptideMatchIndex >= 0 Then
                            ' mLeaderSequenceCache contains 1 or more peptides that start with strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength)
                            ' Test each of the peptides against this protein

                            Do
                                Dim blnTestPeptide As Boolean

                                If mSearchAllProteinsForPeptideSequence Then
                                    blnTestPeptide = True
                                Else
                                    ' Make sure that the protein for intCachedPeptideMatchIndex matches this protein name
                                    If mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).ProteinName.ToLower = mCachedProteinInfo(intProteinIndex).Name.ToLower Then
                                        blnTestPeptide = True
                                    Else
                                        blnTestPeptide = False
                                    End If
                                End If

                                ' Cache the peptide length in intPeptideLength
                                Dim intPeptideLength = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence.Length

                                ' Only compare the full sequence to the protein if:
                                '  a) the protein name matches (or mSearchAllProteinsForPeptideSequence = True) and
                                '  b) the peptide sequence doesn't pass the end of the protein
                                If blnTestPeptide AndAlso intProteinSeqCharIndex + intPeptideLength <= strProteinSequence.Length Then

                                    ' See if the full sequence matches the protein
                                    Dim blnMatchFound = False
                                    If mSearchAllProteinsSkipCoverageComputationSteps Then
                                        ' No need to capitalize strProteinSequence since it's already capitalized
                                        If mIgnoreILDifferences Then
                                            If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequenceLtoI Then
                                                blnMatchFound = True
                                            End If
                                        Else
                                            If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence Then
                                                blnMatchFound = True
                                            End If
                                        End If
                                    Else
                                        ' Need to change strProteinSequence to all caps when comparing to .PeptideSequence
                                        If mIgnoreILDifferences Then
                                            If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequenceLtoI Then
                                                blnMatchFound = True
                                            End If
                                        Else
                                            If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence Then
                                                blnMatchFound = True
                                            End If
                                        End If

                                    End If

                                    If blnMatchFound Then
                                        Dim intEndIndex = intProteinSeqCharIndex + intPeptideLength - 1
                                        If mMatchPeptidePrefixAndSuffixToProtein Then
                                            blnMatchFound = ValidatePrefixAndSuffix(strProteinSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PrefixLtoI, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).SuffixLtoI, intProteinSeqCharIndex, intEndIndex)
                                        End If

                                        If blnMatchFound Then
                                            Dim strPeptideSequenceForKeySource As String
                                            Dim strPeptideSequenceForKey As String

                                            If mMatchPeptidePrefixAndSuffixToProtein Then
                                                strPeptideSequenceForKeySource = ConstructPeptideSequenceForKey(mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).Prefix, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).Suffix)
                                            Else
                                                ' I'm purposely not using String.Copy() here in order to obtain increased speed
                                                strPeptideSequenceForKeySource = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence
                                            End If

                                            If mIgnoreILDifferences Then
                                                ' Replace all L characters with I
                                                strPeptideSequenceForKey = strPeptideSequenceForKeySource.Replace("L"c, "I"c)
                                            Else
                                                ' I'm purposely not using String.Copy() here in order to obtain increased speed
                                                strPeptideSequenceForKey = strPeptideSequenceForKeySource
                                            End If

                                            If Not mSearchAllProteinsSkipCoverageComputationSteps Then
                                                ' Capitalize the protein sequence letters where this peptide matched
                                                Dim intNextStartIndex = intEndIndex + 1

                                                Dim strNewProteinSequence = String.Empty
                                                If intProteinSeqCharIndex > 0 Then
                                                    strNewProteinSequence = strProteinSequence.Substring(0, intProteinSeqCharIndex)
                                                End If
                                                strNewProteinSequence &= strProteinSequence.Substring(intProteinSeqCharIndex, intNextStartIndex - intProteinSeqCharIndex).ToUpper
                                                strNewProteinSequence &= strProteinSequence.Substring(intNextStartIndex)
                                                strProteinSequence = String.Copy(strNewProteinSequence)

                                                blnProteinSequenceUpdated = True
                                            End If

                                            Dim blnMatchIsNew As Boolean

                                            If mTrackPeptideCounts Then
                                                Dim proteinPeptideKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey

                                                blnMatchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey)
                                            Else
                                                ' Must always assume the match is new since not tracking peptide counts
                                                blnMatchIsNew = True
                                            End If

                                            If blnMatchIsNew Then
                                                If mSaveProteinToPeptideMappingFile Then
                                                    WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(intProteinIndex).Name, strPeptideSequenceForKeySource, intProteinSeqCharIndex + 1, intEndIndex + 1)
                                                End If

                                                If mSaveSourceDataPlusProteinsFile Then
                                                    StorePeptideToProteinMatch(mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence, mCachedProteinInfo(intProteinIndex).Name)
                                                End If

                                            End If
                                        End If
                                    End If
                                End If

                                intCachedPeptideMatchIndex = mLeaderSequenceCache.GetNextPeptideWithLeaderSequence(intCachedPeptideMatchIndex)
                            Loop While intCachedPeptideMatchIndex >= 0
                        End If
                    Next intProteinSeqCharIndex

                    If blnProteinSequenceUpdated Then
                        mCachedProteinInfo(intProteinIndex).Sequence = String.Copy(strProteinSequence)
                        blnProteinUpdated(intProteinIndex) = True
                    End If

                    intProteinProcessIterations += 1
                    If intProteinProcessIterations Mod 100 = 0 Then
                        UpdateProgress(CSng(intProteinProcessIterations / intProteinProcessIterationsExpected * 100),
                           eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)
                    End If

                    If mAbortProcessing Then Exit For

                Next

                ' Store the updated protein sequence information in the SQL Lite DB
                UpdateSequenceDbDataValues(blnProteinUpdated, intProteinCount)

                ' Increment intStartIndex to obtain the next chunk of proteins
                intStartIndex += PROTEIN_CHUNK_COUNT

            Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

        Catch ex As Exception
            SetErrorMessage("Error in SearchProteinsUsingLeaderSequences: " & ex.Message)
            If Not mShowMessages Then Throw New Exception("Error in SearchProteinsUsingLeaderSequences", ex)
        End Try

    End Sub

    Private Sub SearchProteinsUsingCachedPeptides(shortPeptideCache As IDictionary(Of String, Integer))

        Dim strProgressMessageBase As String

        If shortPeptideCache.Count > 0 Then
            Console.WriteLine()
            Console.WriteLine()
            strProgressMessageBase = "Comparing proteins to short peptide sequences"
            Console.WriteLine(strProgressMessageBase)

            UpdateProgress(strProgressMessageBase)

            ' Need to step through the proteins and match them to the data in shortPeptideCache
            FindSequenceMatchForPeptideList(shortPeptideCache, String.Empty)
        End If

    End Sub

    Private Sub StorePeptideToProteinMatch(strCleanPeptideSequence As String, strProteinName As String)

        ' Store the mapping between peptide sequence and protein name
        Dim lstProteins As List(Of String) = Nothing
        If mPeptideToProteinMapResults.TryGetValue(strCleanPeptideSequence, lstProteins) Then
            lstProteins.Add(strProteinName)
        Else
            lstProteins = New List(Of String)
            lstProteins.Add(strProteinName)
            mPeptideToProteinMapResults.Add(strCleanPeptideSequence, lstProteins)
        End If

    End Sub

    Private Function ValidateColumnCountInInputFile(strPeptideInputFilePath As String) As Boolean

        Dim blnSuccess As Boolean

        If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly Then
            ' Simply return true; don't even pre-read the file
            ' However, auto-switch mSearchAllProteinsForPeptideSequence to true if not true
            If Not mSearchAllProteinsForPeptideSequence Then
                mSearchAllProteinsForPeptideSequence = True
            End If
            Return True
        End If

        blnSuccess = ValidateColumnCountInInputFile(strPeptideInputFilePath, mPeptideFileColumnOrdering, mPeptideFileSkipFirstLine, mPeptideInputFileDelimiter)

        If blnSuccess Then
            If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly Then
                ' Need to auto-switch to search all proteins
                mSearchAllProteinsForPeptideSequence = True
            End If
        End If

        Return blnSuccess
    End Function

    ''' <summary>
    ''' Read the first two lines to check whether the data file actually has only one column when the user has
    ''' specified mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
    ''' If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly, the file isn't even opened
    ''' </summary>
    ''' <param name="strPeptideInputFilePath"></param>
    ''' <param name="ePeptideFileColumnOrdering">Input / Output parameter</param>
    ''' <param name="blnSkipFirstLine"></param>
    ''' <param name="chColumnDelimiter"></param>
    ''' <returns>True if no problems; False if the user chooses to abort</returns>
    Public Shared Function ValidateColumnCountInInputFile(
      strPeptideInputFilePath As String,
      ByRef ePeptideFileColumnOrdering As ePeptideFileColumnOrderingCode,
      blnSkipFirstLine As Boolean,
      chColumnDelimiter As Char) As Boolean
        ' Open the file and read in the lines
        Using srInFile = New StreamReader(New FileStream(strPeptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

            Dim intCurrentLine = 1
            Do While Not srInFile.EndOfStream AndAlso intCurrentLine < 3
                Dim strLineIn = srInFile.ReadLine.Trim

                If intCurrentLine = 1 AndAlso blnSkipFirstLine Then
                    ' do nothing, skip the first line
                ElseIf strLineIn.Length > 0 Then
                    Try
                        Dim strSplitLine = strLineIn.Split(chColumnDelimiter)

                        If (Not blnSkipFirstLine AndAlso intCurrentLine = 1) OrElse
                           (blnSkipFirstLine AndAlso intCurrentLine = 2) Then
                            If strSplitLine.Length = 1 AndAlso ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence Then
                                ' Auto switch to ePeptideFileColumnOrderingCode.SequenceOnly
                                ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly
                            End If
                        End If

                    Catch ex As Exception
                        ' Ignore the error
                    End Try
                End If
                intCurrentLine += 1
            Loop

        End Using

        Return True

    End Function

    Private Function ValidatePrefixAndSuffix(strProteinSequence As String, chPrefixResidue As Char, chSuffixResidue As Char, intStartIndex As Integer, intEndIndex As Integer) As Boolean

        Dim blnMatchValid = True

        If Char.IsLetter(chPrefixResidue) Then
            If intStartIndex >= 1 Then
                If Char.ToUpper(strProteinSequence.Chars(intStartIndex - 1)) <> chPrefixResidue Then
                    blnMatchValid = False
                End If
            End If
        ElseIf chPrefixResidue = "-"c AndAlso intStartIndex <> 0 Then
            blnMatchValid = False
        End If

        If blnMatchValid Then
            If Char.IsLetter(chSuffixResidue) Then
                If intEndIndex < strProteinSequence.Length - 1 Then
                    If Char.ToUpper(strProteinSequence.Chars(intEndIndex + 1)) <> chSuffixResidue Then
                        blnMatchValid = False
                    End If
                Else
                    blnMatchValid = False
                End If
            ElseIf chSuffixResidue = "-"c AndAlso intEndIndex < strProteinSequence.Length - 1 Then
                blnMatchValid = False
            End If
        End If

        Return blnMatchValid

    End Function

    Private Sub WriteEntryToProteinToPeptideMappingFile(strProteinName As String, strPeptideSequenceForKey As String, intStartResidue As Integer, intEndResidue As Integer)
        If mSaveProteinToPeptideMappingFile AndAlso Not mProteinToPeptideMappingOutputFile Is Nothing Then
            mProteinToPeptideMappingOutputFile.WriteLine(strProteinName & ControlChars.Tab & strPeptideSequenceForKey & ControlChars.Tab & intStartResidue & ControlChars.Tab & intEndResidue)
        End If
    End Sub

    Protected Sub ResetProgress()
        ResetProgress(String.Empty)
    End Sub

    Protected Sub ResetProgress(strProgressStepDescription As String)
        mProgressStepDescription = String.Copy(strProgressStepDescription)
        mProgressPercentComplete = 0
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub SetErrorCode(eNewErrorCode As eProteinCoverageErrorCodes)
        SetErrorCode(eNewErrorCode, False)
    End Sub

    Protected Sub SetErrorCode(eNewErrorCode As eProteinCoverageErrorCodes, blnLeaveExistingErrorCodeUnchanged As Boolean)
        If blnLeaveExistingErrorCodeUnchanged AndAlso mErrorCode <> eProteinCoverageErrorCodes.NoError Then
            ' An error code is already defined; do not change it
        Else
            mErrorCode = eNewErrorCode
        End If
    End Sub

    Protected Sub SetErrorMessage(strMessage As String)
        If strMessage Is Nothing Then
            mErrorMessage = String.Empty
        Else
            mErrorMessage = String.Copy(strMessage)
        End If

        If mErrorMessage.Length > 0 Then
            OnErrorEvent(mErrorMessage)
            UpdateProgress(mErrorMessage)
        End If
    End Sub

    Protected Sub UpdateProgress(strProgressStepDescription As String)
        mProgressStepDescription = String.Copy(strProgressStepDescription)
        RaiseEvent ProgressChanged(Me.ProgressStepDescription, Me.ProgressPercentComplete)
    End Sub

    Protected Sub UpdateProgress(sngPercentComplete As Single, eCurrentProcessingStep As eProteinCoverageProcessingSteps)
        UpdateProgress(Me.ProgressStepDescription, sngPercentComplete, eCurrentProcessingStep)
    End Sub

    Protected Sub UpdateProgress(strProgressStepDescription As String, sngPercentComplete As Single, eCurrentProcessingStep As eProteinCoverageProcessingSteps)

        mProgressStepDescription = String.Copy(strProgressStepDescription)
        mCurrentProcessingStep = eCurrentProcessingStep

        If sngPercentComplete < 0 Then
            sngPercentComplete = 0
        ElseIf sngPercentComplete > 100 Then
            sngPercentComplete = 100
        End If

        Dim sngStartPercent = mPercentCompleteStartLevels(eCurrentProcessingStep)
        Dim sngEndPercent = mPercentCompleteStartLevels(eCurrentProcessingStep + 1)

        ' Use the start and end percent complete values for the specified processing step to convert sngPercentComplete to an overall percent complete value
        mProgressPercentComplete = sngStartPercent + CSng(sngPercentComplete / 100.0 * (sngEndPercent - sngStartPercent))

        RaiseEvent ProgressChanged(Me.ProgressStepDescription, Me.ProgressPercentComplete)
    End Sub

    ''Protected Sub UpdateSubtaskProgress(sngPercentComplete As Single)
    ''    UpdateSubtaskProgress(mSubtaskStepDescription, sngPercentComplete)
    ''End Sub

    ''Protected Sub UpdateSubtaskProgress(strSubtaskStepDescription As String, sngPercentComplete As Single)
    ''    mSubtaskStepDescription = String.Copy(strSubtaskStepDescription)
    ''    If sngPercentComplete < 0 Then
    ''        sngPercentComplete = 0
    ''    ElseIf sngPercentComplete > 100 Then
    ''        sngPercentComplete = 100
    ''    End If
    ''    mSubtaskPercentComplete = sngPercentComplete

    ''    RaiseEvent SubtaskProgressChanged(Me.SubtaskStepDescription, Me.SubtaskPercentComplete)
    ''End Sub

    Private Sub mLeaderSequenceCache_ProgressChanged(taskDescription As String, percentComplete As Single) Handles mLeaderSequenceCache.ProgressChanged
        UpdateProgress(percentComplete, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
    End Sub

    Private Sub mLeaderSequenceCache_ProgressComplete() Handles mLeaderSequenceCache.ProgressComplete
        UpdateProgress(100, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
    End Sub

    Private Sub mProteinDataCache_ProteinCachedWithProgress(intProteinsCached As Integer, sngPercentFileProcessed As Single) Handles mProteinDataCache.ProteinCachedWithProgress
        Const CONSOLE_UPDATE_INTERVAL_SECONDS = 3

        Static dtLastUpdate As DateTime = DateTime.UtcNow

        If DateTime.UtcNow.Subtract(dtLastUpdate).TotalSeconds >= CONSOLE_UPDATE_INTERVAL_SECONDS Then
            dtLastUpdate = DateTime.UtcNow
            Console.Write(".")
        End If

        UpdateProgress(sngPercentFileProcessed, eProteinCoverageProcessingSteps.CacheProteins)

    End Sub

    Private Sub mProteinDataCache_ProteinCachingComplete() Handles mProteinDataCache.ProteinCachingComplete
        UpdateProgress(100, eProteinCoverageProcessingSteps.CacheProteins)
    End Sub
End Class
