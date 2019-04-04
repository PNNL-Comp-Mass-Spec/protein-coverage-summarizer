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
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports PRISM
Imports ProteinFileReader

''' <summary>
''' This class will read in a protein FASTA file or delimited protein info file along with
''' an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
''' </summary>
<CLSCompliant(True)>
Public Class clsProteinCoverageSummarizer
    Inherits EventNotifier

    Public Sub New()
        InitializeVariables()
    End Sub

#Region "Constants and Enums"
    Public Const XML_SECTION_PROCESSING_OPTIONS As String = "ProcessingOptions"

    Public Const OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER As Integer = 3
    Public Const OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER As Integer = 7

    Public Const FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING As String = "_ProteinToPeptideMapping.txt"
    Public Const FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS As String = "_AllProteins.txt"

    Protected Const PROTEIN_CHUNK_COUNT As Integer = 50000

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

#End Region

#Region "Classwide variables"
    Public WithEvents ProteinDataCache As clsProteinFileDataCache
    Private WithEvents mLeaderSequenceCache As clsLeaderSequenceCache

    ' This dictionary contains entries of the form 1234::K.ABCDEFR.A
    '  where the number is the protein ID and the peptide is the peptide sequence
    ' The value for each entry is the number of times the peptide is present in the given protein
    ' This dictionary is only populated if mTrackPeptideCounts is true
    Private mProteinPeptideStats As Dictionary(Of String, Integer)
    Private mResultsFilePath As String              ' This value is populated by function ProcessFile()

    Private mProteinToPeptideMappingFilePath As String
    Private mProteinToPeptideMappingOutputFile As StreamWriter

    Private mErrorCode As eProteinCoverageErrorCodes
    Private mErrorMessage As String

    Private mAbortProcessing As Boolean

    Private mCachedProteinInfoStartIndex As Integer = -1
    Private mCachedProteinInfoCount As Integer

#Disable Warning IDE0044 ' Add readonly modifier
    Private mCachedProteinInfo() As clsProteinFileDataCache.udtProteinInfoType
#Enable Warning IDE0044 ' Add readonly modifier

    Private mKeepDB As Boolean

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

    Protected mCurrentProcessingStep As eProteinCoverageProcessingSteps = eProteinCoverageProcessingSteps.Starting
    Protected mProgressStepDescription As String = String.Empty

    ''' <summary>
    ''' Percent complete
    ''' </summary>
    ''' <remarks>
    ''' Ranges from 0 to 100, but can contain decimal percentage values
    ''' </remarks>
    Protected mProgressPercentComplete As Single

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

    ''' <summary>
    ''' When this is True, the SQLite Database will not be deleted after processing finishes
    ''' </summary>
    Public Property KeepDB As Boolean
        Get
            Return mKeepDB
        End Get
        Set(value As Boolean)
            mKeepDB = value
            If Not ProteinDataCache Is Nothing Then
                ProteinDataCache.KeepDB = mKeepDB
            End If

        End Set
    End Property

    Public Property MatchPeptidePrefixAndSuffixToProtein As Boolean

    Public Property OutputProteinSequence As Boolean

    Public Property PeptideFileFormatCode As ePeptideFileColumnOrderingCode

    Public Property PeptideFileSkipFirstLine As Boolean

    Public Property PeptideInputFileDelimiter As Char

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

    Public ReadOnly Property ProteinToPeptideMappingFilePath As String
        Get
            Return mProteinToPeptideMappingFilePath
        End Get
    End Property

    Public Property RemoveSymbolCharacters As Boolean

    Public ReadOnly Property ResultsFilePath As String
        Get
            Return mResultsFilePath
        End Get
    End Property

    Public Property SaveProteinToPeptideMappingFile As Boolean

    Public Property SaveSourceDataPlusProteinsFile As Boolean

    Public Property SearchAllProteinsForPeptideSequence As Boolean

    Public Property UseLeaderSequenceHashTable As Boolean

    Public Property SearchAllProteinsSkipCoverageComputationSteps As Boolean

    Public ReadOnly Property StatusMessage As String
        Get
            Return mErrorMessage
        End Get
    End Property

    Public Property TrackPeptideCounts As Boolean

#End Region

    Public Sub AbortProcessingNow()
        If Not mLeaderSequenceCache Is Nothing Then
            mLeaderSequenceCache.AbortProcessingNow()
        End If
    End Sub

    Private Function BooleanArrayContainsTrueEntries(arrayToCheck As IList(Of Boolean), arrayLength As Integer) As Boolean

        Dim containsTrueEntries = False

        For index = 0 To arrayLength - 1
            If arrayToCheck(index) Then
                containsTrueEntries = True
                Exit For
            End If
        Next

        Return containsTrueEntries

    End Function

    Private Function CapitalizeMatchingProteinSequenceLetters(
      proteinSequence As String,
      peptideSequence As String,
      proteinPeptideKey As String,
      prefixResidue As Char,
      suffixResidue As Char,
      <Out> ByRef matchFound As Boolean,
      <Out> ByRef matchIsNew As Boolean,
      <Out> ByRef startResidue As Integer,
      <Out> ByRef endResidue As Integer) As String

        ' Note: this function assumes peptideSequence, prefix, and suffix have all uppercase letters
        ' prefix and suffix are only used if mMatchPeptidePrefixAndSuffixToProtein = true

        ' Note: This is a count of the number of times the peptide is present in the protein sequence (typically 1); this value is not stored anywhere
        Dim peptideCount = 0

        Dim currentMatchValid As Boolean

        matchFound = False
        matchIsNew = False

        startResidue = 0
        endResidue = 0

        Dim charIndex As Integer

        If SearchAllProteinsSkipCoverageComputationSteps Then
            ' No need to capitalize proteinSequence since it's already capitalized
            charIndex = proteinSequence.IndexOf(peptideSequence, StringComparison.Ordinal)
        Else
            ' Need to change proteinSequence to all caps when searching for peptideSequence
            charIndex = proteinSequence.ToUpper().IndexOf(peptideSequence, StringComparison.Ordinal)
        End If

        If charIndex >= 0 Then
            startResidue = charIndex + 1
            endResidue = startResidue + peptideSequence.Length - 1

            matchFound = True

            If MatchPeptidePrefixAndSuffixToProtein Then
                currentMatchValid = ValidatePrefixAndSuffix(proteinSequence, prefixResidue, suffixResidue, charIndex, endResidue - 1)
            Else
                currentMatchValid = True
            End If

            If currentMatchValid Then
                peptideCount += 1
            Else
                startResidue = 0
                endResidue = 0
            End If
        Else
            currentMatchValid = False
        End If

        If matchFound AndAlso Not SearchAllProteinsSkipCoverageComputationSteps Then
            Do While charIndex >= 0

                If currentMatchValid Then
                    Dim nextStartIndex = charIndex + peptideSequence.Length

                    Dim newProteinSequence = String.Empty
                    If charIndex > 0 Then
                        newProteinSequence = proteinSequence.Substring(0, charIndex)
                    End If
                    newProteinSequence &= proteinSequence.Substring(charIndex, nextStartIndex - charIndex).ToUpper
                    newProteinSequence &= proteinSequence.Substring(nextStartIndex)
                    proteinSequence = String.Copy(newProteinSequence)
                End If

                ' Look for another occurrence of peptideSequence in this protein
                charIndex = proteinSequence.ToUpper().IndexOf(peptideSequence, charIndex + 1, StringComparison.Ordinal)

                If charIndex >= 0 Then
                    If MatchPeptidePrefixAndSuffixToProtein Then
                        currentMatchValid = ValidatePrefixAndSuffix(proteinSequence, prefixResidue, suffixResidue, charIndex, charIndex + peptideSequence.Length - 1)
                    Else
                        currentMatchValid = True
                    End If

                    If currentMatchValid Then
                        peptideCount += 1

                        If startResidue = 0 Then
                            startResidue = charIndex + 1
                            endResidue = startResidue + peptideSequence.Length - 1
                        End If
                    End If
                End If
            Loop
        End If


        If matchFound Then
            If peptideCount = 0 Then
                ' The protein contained peptideSequence, but mMatchPeptidePrefixAndSuffixToProtein = true and either prefixResidue or suffixResidue doesn't match
                matchFound = False
            ElseIf TrackPeptideCounts Then
                matchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey)
            Else
                ' Must always assume the match is new since not tracking peptide counts
                matchIsNew = True
            End If
        End If

        Return proteinSequence

    End Function

    ''' <summary>
    ''' Construct the output file path
    ''' The output file is based on outputFileBaseName if defined, otherwise is based on inputFilePath with the suffix removed
    ''' In either case, suffixToAppend is appended
    ''' The Output directory is based on outputDirectoryPath if defined, otherwise it is the directory where inputFilePath resides
    ''' </summary>
    ''' <param name="inputFilePath"></param>
    ''' <param name="suffixToAppend"></param>
    ''' <param name="outputDirectoryPath"></param>
    ''' <param name="outputFileBaseName"></param>
    ''' <returns></returns>
    Public Shared Function ConstructOutputFilePath(
      inputFilePath As String,
      suffixToAppend As String,
      outputDirectoryPath As String,
      outputFileBaseName As String) As String

        Dim outputFileName As String

        If String.IsNullOrEmpty(outputFileBaseName) Then
            outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) & suffixToAppend
        Else
            outputFileName = outputFileBaseName & suffixToAppend
        End If

        Dim outputFilePath = Path.Combine(GetOutputDirectoryPath(outputDirectoryPath, inputFilePath), outputFileName)

        Return outputFilePath

    End Function

    Private Function ConstructPeptideSequenceForKey(peptideSequence As String, prefixResidue As Char, suffixResidue As Char) As String
        Dim peptideSequenceForKey As String

        If Convert.ToInt32(prefixResidue) = 0 AndAlso Convert.ToInt32(suffixResidue) = 0 Then
            peptideSequenceForKey = String.Copy(peptideSequence)
        Else
            If Char.IsLetter(prefixResidue) Then
                prefixResidue = Char.ToUpper(prefixResidue)
                peptideSequenceForKey = prefixResidue & "."c & peptideSequence
            Else
                peptideSequenceForKey = "-." & peptideSequence
            End If

            If Char.IsLetter(suffixResidue) Then
                suffixResidue = Char.ToUpper(suffixResidue)
                peptideSequenceForKey &= "."c & suffixResidue
            Else
                peptideSequenceForKey &= ".-"
            End If
        End If

        Return peptideSequenceForKey
    End Function

    Private Sub CreateProteinCoverageFile(peptideInputFilePath As String, outputDirectoryPath As String, outputFileBaseName As String)
        Const INITIAL_PROTEIN_COUNT_RESERVE = 5000

        ' The data in mProteinPeptideStats is copied into array peptideStats for fast lookup
        ' This is necessary since use of the enumerator returned by mProteinPeptideStats.GetEnumerator
        '  for every protein in ProteinDataCache.mProteins leads to very slow program performance
        Dim peptideStatsCount = 0
        Dim udtPeptideStats() As udtPeptideCountStatsType

        If mResultsFilePath = Nothing OrElse mResultsFilePath.Length = 0 Then
            If peptideInputFilePath.Length > 0 Then
                mResultsFilePath = ConstructOutputFilePath(peptideInputFilePath, "_coverage.txt", outputDirectoryPath, outputFileBaseName)
            Else
                mResultsFilePath = Path.Combine(GetOutputDirectoryPath(outputDirectoryPath, String.Empty), "Peptide_coverage.txt")
            End If
        End If

        UpdateProgress("Creating the protein coverage file: " & Path.GetFileName(mResultsFilePath), 0,
           eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

        Using writer = New StreamWriter(New FileStream(mResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

            ' Note: If the column ordering is changed, be sure to update OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER and OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER
            Dim dataLine = "Protein Name" & ControlChars.Tab &
             "Percent Coverage" & ControlChars.Tab &
             "Protein Description" & ControlChars.Tab &
             "Non Unique Peptide Count" & ControlChars.Tab &
             "Unique Peptide Count" & ControlChars.Tab &
             "Protein Residue Count"

            If OutputProteinSequence Then
                dataLine &= ControlChars.Tab & "Protein Sequence"
            End If
            writer.WriteLine(dataLine)

            ' Contains pointers to entries in udtPeptideStats()
            Dim proteinIDLookup = New Dictionary(Of Integer, Integer)

            ' Populate udtPeptideStats() using dictionary mProteinPeptideStats
            If TrackPeptideCounts Then

                ' Initially reserve space for INITIAL_PROTEIN_COUNT_RESERVE proteins
                ReDim udtPeptideStats(INITIAL_PROTEIN_COUNT_RESERVE - 1)

                Dim myEnumerator = mProteinPeptideStats.GetEnumerator
                While myEnumerator.MoveNext()
                    Dim proteinPeptideKey = myEnumerator.Current.Key

                    ' proteinPeptideKey will be of the form 1234::K.ABCDEFR.A
                    ' Look for the first colon
                    Dim colonIndex = proteinPeptideKey.IndexOf(":"c)

                    If colonIndex > 0 Then
                        Dim proteinID = CInt(proteinPeptideKey.Substring(0, colonIndex))
                        Dim targetIndex As Integer

                        If Not proteinIDLookup.TryGetValue(proteinID, targetIndex) Then
                            ' ID not found; so add it

                            targetIndex = peptideStatsCount
                            peptideStatsCount += 1

                            proteinIDLookup.Add(proteinID, targetIndex)

                            If targetIndex >= udtPeptideStats.Length Then
                                ' Reserve more space in the arrays
                                ReDim Preserve udtPeptideStats(udtPeptideStats.Length * 2 - 1)
                            End If
                        End If


                        ' Update the protein counts at targetIndex
                        udtPeptideStats(targetIndex).UniquePeptideCount += 1
                        udtPeptideStats(targetIndex).NonUniquePeptideCount += myEnumerator.Current.Value

                    End If
                End While

                ' Shrink udtPeptideStats
                If peptideStatsCount < udtPeptideStats.Length Then
                    ReDim Preserve udtPeptideStats(peptideStatsCount - 1)
                End If
            Else
                ReDim udtPeptideStats(-1)
            End If

            ' Query the SQLite DB to extract the protein information
            Dim proteinsProcessed = 0
            For Each udtProtein In ProteinDataCache.GetCachedProteins()

                Dim uniquePeptideCount = 0
                Dim nonUniquePeptideCount = 0

                If TrackPeptideCounts Then
                    Dim targetIndex As Integer
                    If proteinIDLookup.TryGetValue(udtProtein.UniqueSequenceID, targetIndex) Then
                        uniquePeptideCount = udtPeptideStats(targetIndex).UniquePeptideCount
                        nonUniquePeptideCount = udtPeptideStats(targetIndex).NonUniquePeptideCount
                    End If
                End If

                dataLine = udtProtein.Name & ControlChars.Tab &
                     Math.Round(udtProtein.PercentCoverage * 100, 3) & ControlChars.Tab &
                     udtProtein.Description & ControlChars.Tab &
                     nonUniquePeptideCount & ControlChars.Tab &
                     uniquePeptideCount & ControlChars.Tab &
                     udtProtein.Sequence.Length

                If OutputProteinSequence Then
                    dataLine &= ControlChars.Tab & CStr(udtProtein.Sequence)
                End If
                writer.WriteLine(dataLine)

                If proteinsProcessed Mod 25 = 0 Then
                    UpdateProgress(proteinsProcessed / CSng(ProteinDataCache.GetProteinCountCached()) * 100,
                                   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)
                End If

                If mAbortProcessing Then Exit For
                proteinsProcessed += 1
            Next

        End Using

    End Sub

    Private Function DetermineLineTerminatorSize(inputFilePath As String) As Integer

        Dim terminatorSize = 2

        Try
            ' Open the input file and look for the first carriage return or line feed
            Using fsInFile = New FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

                Do While fsInFile.Position < fsInFile.Length AndAlso fsInFile.Position < 100000

                    Dim intByte = fsInFile.ReadByte()

                    If intByte = 10 Then
                        ' Found linefeed
                        If fsInFile.Position < fsInFile.Length Then
                            intByte = fsInFile.ReadByte()
                            If intByte = 13 Then
                                ' LfCr
                                terminatorSize = 2
                            Else
                                ' Lf only
                                terminatorSize = 1
                            End If
                        Else
                            terminatorSize = 1
                        End If
                        Exit Do
                    ElseIf intByte = 13 Then
                        ' Found carriage return
                        If fsInFile.Position < fsInFile.Length Then
                            intByte = fsInFile.ReadByte()
                            If intByte = 10 Then
                                ' CrLf
                                terminatorSize = 2
                            Else
                                ' Cr only
                                terminatorSize = 1
                            End If
                        Else
                            terminatorSize = 1
                        End If
                        Exit Do
                    End If

                Loop
            End Using

        Catch ex As Exception
            SetErrorMessage("Error in DetermineLineTerminatorSize: " & ex.Message, ex)
        End Try

        Return terminatorSize

    End Function

    ''' <summary>
    ''' Searches for proteins that contain the peptides in peptideList
    ''' If proteinNameForPeptide is blank or mSearchAllProteinsForPeptideSequence=True then searches all proteins
    ''' Otherwise, only searches protein proteinNameForPeptide
    ''' </summary>
    ''' <param name="peptideList">Dictionary containing the peptides to search; peptides must be in the format Prefix.Peptide.Suffix where Prefix and Suffix are single characters; peptides are assumed to only contain letters (no symbols)</param>
    ''' <param name="proteinNameForPeptides">The protein to search; only used if mSearchAllProteinsForPeptideSequence=False</param>
    ''' <remarks></remarks>
    Private Sub FindSequenceMatchForPeptideList(peptideList As IDictionary(Of String, Integer),
      proteinNameForPeptides As String)

        Dim proteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        Try
            ' Make sure proteinNameForPeptide is a valid string
            If proteinNameForPeptides Is Nothing Then
                proteinNameForPeptides = String.Empty
            End If

            Dim expectedPeptideIterations = CInt(Math.Ceiling(ProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * peptideList.Count
            If expectedPeptideIterations < 1 Then expectedPeptideIterations = 1

            UpdateProgress("Finding matching proteins for peptide list", 0,
               eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

            Dim startIndex = 0
            Do
                ' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
                ' Store the information in the four local arrays
                Dim proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, False)

                Dim peptideIterationsComplete = 0

                ' Iterate through the peptides in peptideList
                Dim myEnumerator = peptideList.GetEnumerator

                Do While myEnumerator.MoveNext

                    Dim prefixResidue As Char
                    Dim suffixResidue As Char

                    ' Retrieve the next peptide from peptideList
                    ' Use GetCleanPeptideSequence() to extract out the sequence, prefix, and suffix letters (we're setting removeSymbolCharacters to False since that should have been done before the peptides were stored in peptideList)
                    ' Make sure the peptide sequence has uppercase letters
                    Dim peptideSequenceClean = GetCleanPeptideSequence(myEnumerator.Current.Key, prefixResidue, suffixResidue, False).ToUpper

                    Dim peptideSequenceForKeySource As String
                    Dim peptideSequenceForKey As String
                    Dim peptideSequenceToSearchOn As String

                    If MatchPeptidePrefixAndSuffixToProtein Then
                        peptideSequenceForKeySource = ConstructPeptideSequenceForKey(peptideSequenceClean, prefixResidue, suffixResidue)
                    Else
                        peptideSequenceForKeySource = String.Copy(peptideSequenceClean)
                    End If

                    If IgnoreILDifferences Then
                        ' Replace all L characters with I
                        peptideSequenceForKey = peptideSequenceForKeySource.Replace("L"c, "I"c)

                        peptideSequenceToSearchOn = peptideSequenceClean.Replace("L"c, "I"c)

                        If prefixResidue = "L"c Then prefixResidue = "I"c
                        If suffixResidue = "L"c Then suffixResidue = "I"c
                    Else
                        peptideSequenceToSearchOn = String.Copy(peptideSequenceClean)

                        ' I'm purposely not using String.Copy() here in order to obtain increased speed
                        peptideSequenceForKey = peptideSequenceForKeySource
                    End If

                    ' Search for peptideSequence in the protein sequences
                    For proteinIndex = 0 To proteinCount - 1
                        Dim matchFound = False
                        Dim matchIsNew As Boolean
                        Dim startResidue As Integer
                        Dim endResidue As Integer

                        If SearchAllProteinsForPeptideSequence OrElse proteinNameForPeptides.Length = 0 Then
                            ' Search through all Protein sequences and capitalize matches for Peptide Sequence

                            Dim proteinPeptideKey = CStr(mCachedProteinInfo(proteinIndex).UniqueSequenceID) & "::" & peptideSequenceForKey
                            mCachedProteinInfo(proteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(
                                mCachedProteinInfo(proteinIndex).Sequence, peptideSequenceToSearchOn,
                                proteinPeptideKey, prefixResidue, suffixResidue,
                                matchFound, matchIsNew,
                                startResidue, endResidue)
                        Else
                            ' Only search protein proteinNameForPeptide
                            If mCachedProteinInfo(proteinIndex).Name = proteinNameForPeptides Then

                                ' Define the peptide match key using the Unique Sequence ID, two colons, and the peptide sequence
                                Dim proteinPeptideKey = CStr(mCachedProteinInfo(proteinIndex).UniqueSequenceID) & "::" & peptideSequenceForKey

                                ' Capitalize matching residues in sequence
                                mCachedProteinInfo(proteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(
                                    mCachedProteinInfo(proteinIndex).Sequence, peptideSequenceToSearchOn,
                                    proteinPeptideKey, prefixResidue, suffixResidue,
                                    matchFound, matchIsNew,
                                    startResidue, endResidue)
                            End If
                        End If

                        If matchFound Then
                            If Not SearchAllProteinsSkipCoverageComputationSteps Then
                                proteinUpdated(proteinIndex) = True
                            End If

                            If matchIsNew Then
                                If SaveProteinToPeptideMappingFile Then
                                    WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(proteinIndex).Name, peptideSequenceForKeySource, startResidue, endResidue)
                                End If

                                If SaveSourceDataPlusProteinsFile Then
                                    StorePeptideToProteinMatch(peptideSequenceClean, mCachedProteinInfo(proteinIndex).Name)
                                End If

                            End If
                        End If

                    Next proteinIndex

                    peptideIterationsComplete += 1

                    If peptideIterationsComplete Mod 10 = 0 Then
                        UpdateProgress(CSng((peptideIterationsComplete / expectedPeptideIterations) * 100),
                           eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

                    End If
                Loop

                ' Store the updated protein sequence information in the database
                UpdateSequenceDbDataValues(proteinUpdated, proteinCount)

                ' Increment startIndex to obtain the next chunk of proteins
                startIndex += PROTEIN_CHUNK_COUNT

            Loop While startIndex < ProteinDataCache.GetProteinCountCached()

        Catch ex As Exception
            SetErrorMessage("Error in FindSequenceMatch:" & ControlChars.NewLine & ex.Message, ex)
        End Try

    End Sub

    Private Sub UpdateSequenceDbDataValues(proteinUpdated As IList(Of Boolean), proteinCount As Integer)
        Try
            If Not BooleanArrayContainsTrueEntries(proteinUpdated, proteinCount) Then
                ' All of the entries in proteinUpdated() are False; nothing to update
                Exit Sub
            End If

            ' Store the updated protein sequences in the SQLite database
            Dim sqlConnection = ProteinDataCache.ConnectToSQLiteDB(True)

            Using dbTrans As SQLiteTransaction = sqlConnection.BeginTransaction()
                Using cmd As SQLiteCommand = sqlConnection.CreateCommand()

                    ' Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set Sequence = ? Where UniqueSequenceID = ?"

                    Dim SequenceFld As SQLiteParameter = cmd.CreateParameter
                    Dim UniqueSequenceIDFld As SQLiteParameter = cmd.CreateParameter
                    cmd.Parameters.Add(SequenceFld)
                    cmd.Parameters.Add(UniqueSequenceIDFld)

                    ' Update each protein that has proteinUpdated(proteinIndex) = True
                    For proteinIndex = 0 To proteinCount - 1
                        If proteinUpdated(proteinIndex) Then
                            UniqueSequenceIDFld.Value = mCachedProteinInfo(proteinIndex).UniqueSequenceID
                            SequenceFld.Value = mCachedProteinInfo(proteinIndex).Sequence
                            cmd.ExecuteNonQuery()
                        End If
                    Next
                End Using
                dbTrans.Commit()
            End Using

            ' Close the Sql Reader
            sqlConnection.Close()
            sqlConnection.Dispose()

        Catch ex As Exception
            SetErrorMessage("Error in UpdateSequenceDbDataValues: " & ex.Message, ex)
        End Try

    End Sub

    Public Shared Function GetAppDirectoryPath() As String
        ' Could use Application.StartupPath, but .GetExecutingAssembly is better
        Return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    End Function

    Public Shared Function GetCleanPeptideSequence(peptideSequence As String,
      <Out> ByRef prefixResidue As Char,
      <Out> ByRef suffixResidue As Char,
      removeSymbolCharacters As Boolean) As String

        Static reReplaceSymbols As Regex = New Regex("[^A-Za-z]", RegexOptions.Compiled)

        prefixResidue = Nothing
        suffixResidue = Nothing

        If peptideSequence.Length >= 4 Then
            If peptideSequence.Chars(1) = "."c AndAlso peptideSequence.Chars(peptideSequence.Length - 2) = "."c Then
                prefixResidue = peptideSequence.Chars(0)
                suffixResidue = peptideSequence.Chars(peptideSequence.Length - 1)
                peptideSequence = peptideSequence.Substring(2, peptideSequence.Length - 4)
            End If
        End If

        If removeSymbolCharacters Then
            peptideSequence = reReplaceSymbols.Replace(peptideSequence, String.Empty)
        End If

        Return peptideSequence

    End Function

    Public Function GetErrorMessage() As String
        ' Returns String.Empty if no error

        Dim message As String

        Select Case ErrorCode
            Case eProteinCoverageErrorCodes.NoError
                message = String.Empty
            Case eProteinCoverageErrorCodes.InvalidInputFilePath
                message = "Invalid input file path"
                ''Case eProteinCoverageErrorCodes.InvalidOutputDirectoryPath
                ''    message = "Invalid output directory path"
                ''Case eProteinCoverageErrorCodes.ParameterFileNotFound
                ''    message = "Parameter file not found"

                ''Case eProteinCoverageErrorCodes.ErrorReadingInputFile
                ''    message = "Error reading input file"
                ''Case eProteinCoverageErrorCodes.ErrorCreatingOutputFiles
                ''    message = "Error creating output files"

            Case eProteinCoverageErrorCodes.ErrorReadingParameterFile
                message = "Invalid parameter file"

            Case eProteinCoverageErrorCodes.FilePathError
                message = "General file path error"
            Case eProteinCoverageErrorCodes.UnspecifiedError
                message = "Unspecified error"
            Case Else
                ' This shouldn't happen
                message = "Unknown error state"
        End Select

        If mErrorMessage.Length > 0 Then
            If message.Length > 0 Then
                message &= "; "
            End If
            message &= mErrorMessage
        End If

        Return message
    End Function

    <Obsolete("Use GetOutputDirectoryPath")>
    Public Shared Function GetOutputFolderPath(outputFolderPath As String, outputFilePath As String) As String
        Return GetOutputDirectoryPath(outputFolderPath, outputFilePath)
    End Function

    ''' <summary>
    ''' Determine the output directory path
    ''' Uses outputDirectoryPath if defined
    ''' Otherwise uses the directory where outputFilePath resides
    ''' </summary>
    ''' <param name="outputDirectoryPath"></param>
    ''' <param name="outputFilePath"></param>
    ''' <returns></returns>
    ''' <remarks>If an error, or unable to determine a directory, returns the directory with the application files</remarks>
    Public Shared Function GetOutputDirectoryPath(outputDirectoryPath As String, outputFilePath As String) As String

        Try
            If Not String.IsNullOrWhiteSpace(outputDirectoryPath) Then
                outputDirectoryPath = Path.GetFullPath(outputDirectoryPath)
            Else
                outputDirectoryPath = Path.GetDirectoryName(outputFilePath)
            End If

            If Not Directory.Exists(outputDirectoryPath) Then
                Directory.CreateDirectory(outputDirectoryPath)
            End If

        Catch ex As Exception
            outputDirectoryPath = GetAppDirectoryPath()
        End Try

        Return outputDirectoryPath

    End Function

    Private Sub GetPercentCoverage()

        Dim proteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        UpdateProgress("Computing percent coverage", 0,
           eProteinCoverageProcessingSteps.ComputePercentCoverage)

        Dim startIndex = 0
        Dim index = 0
        Do
            ' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
            ' Store the information in the four local arrays
            Dim proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, False)

            For proteinIndex = 0 To proteinCount - 1

                If Not mCachedProteinInfo(proteinIndex).Sequence Is Nothing Then
                    Dim charArray = mCachedProteinInfo(proteinIndex).Sequence.ToCharArray()
                    Dim capitalLetterCount = 0
                    For Each character In charArray
                        If Char.IsUpper(character) Then capitalLetterCount += 1
                    Next

                    mCachedProteinInfo(proteinIndex).PercentCoverage = capitalLetterCount / mCachedProteinInfo(proteinIndex).Sequence.Length
                    If mCachedProteinInfo(proteinIndex).PercentCoverage > 0 Then
                        proteinUpdated(proteinIndex) = True
                    End If
                End If

                If index Mod 100 = 0 Then
                    UpdateProgress(index / CSng(ProteinDataCache.GetProteinCountCached()) * 100,
                       eProteinCoverageProcessingSteps.ComputePercentCoverage)
                End If

                index += 1
            Next

            UpdatePercentCoveragesDbDataValues(proteinUpdated, proteinCount)

            ' Increment startIndex to obtain the next chunk of proteins
            startIndex += PROTEIN_CHUNK_COUNT

        Loop While startIndex < ProteinDataCache.GetProteinCountCached()

    End Sub

    Private Sub UpdatePercentCoveragesDbDataValues(proteinUpdated As IList(Of Boolean), proteinCount As Integer)
        Try
            If Not BooleanArrayContainsTrueEntries(proteinUpdated, proteinCount) Then
                ' All of the entries in proteinUpdated() are False; nothing to update
                Exit Sub
            End If

            ' Store the updated protein coverage values in the SQLite database
            Dim sqlConnection = ProteinDataCache.ConnectToSQLiteDB(True)

            Using dbTrans As SQLiteTransaction = sqlConnection.BeginTransaction()
                Using cmd As SQLiteCommand = sqlConnection.CreateCommand()

                    ' Create a parameterized Update query
                    cmd.CommandText = "UPDATE udtProteinInfoType Set PercentCoverage = ? Where UniqueSequenceID = ?"

                    Dim PercentCoverageFld As SQLiteParameter = cmd.CreateParameter
                    Dim UniqueSequenceIDFld As SQLiteParameter = cmd.CreateParameter
                    cmd.Parameters.Add(PercentCoverageFld)
                    cmd.Parameters.Add(UniqueSequenceIDFld)

                    ' Update each protein that has proteinUpdated(proteinIndex) = True
                    For proteinIndex = 0 To proteinCount - 1
                        If proteinUpdated(proteinIndex) Then
                            UniqueSequenceIDFld.Value = mCachedProteinInfo(proteinIndex).UniqueSequenceID
                            PercentCoverageFld.Value = mCachedProteinInfo(proteinIndex).PercentCoverage
                            cmd.ExecuteNonQuery()
                        End If
                    Next
                End Using
                dbTrans.Commit()
            End Using

            ' Close the Sql Reader
            sqlConnection.Close()
            sqlConnection.Dispose()

        Catch ex As Exception
            SetErrorMessage("Error in UpdatePercentCoveragesDbDataValues: " & ex.Message, ex)
        End Try

    End Sub

    ''' <summary>
    ''' Increment the observation count for the given key in the given dictionary
    ''' If the key is not defined, add it
    ''' </summary>
    ''' <param name="dictionaryToUpdate"></param>
    ''' <param name="keyName"></param>
    ''' <returns>True if the protein is new and was added tomProteinPeptideStats </returns>
    Private Function IncrementCountByKey(dictionaryToUpdate As IDictionary(Of String, Integer), keyName As String) As Boolean
        Dim value As Integer

        If dictionaryToUpdate.TryGetValue(keyName, value) Then
            dictionaryToUpdate(keyName) = value + 1
            Return False
        Else
            dictionaryToUpdate.Add(keyName, 1)
            Return True
        End If
    End Function

    Private Sub InitializeVariables()
        mAbortProcessing = False
        mErrorMessage = String.Empty

        ProteinInputFilePath = String.Empty
        mResultsFilePath = String.Empty

        ProteinDataCache = New clsProteinFileDataCache With {
            .KeepDB = KeepDB
        }

        RegisterEvents(ProteinDataCache)

        mCachedProteinInfoStartIndex = -1

        PeptideFileSkipFirstLine = False
        PeptideInputFileDelimiter = ControlChars.Tab
        PeptideFileFormatCode = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence

        OutputProteinSequence = True
        SearchAllProteinsForPeptideSequence = True
        SearchAllProteinsSkipCoverageComputationSteps = False
        UseLeaderSequenceHashTable = True

        SaveProteinToPeptideMappingFile = False
        mProteinToPeptideMappingFilePath = String.Empty

        SaveSourceDataPlusProteinsFile = False

        TrackPeptideCounts = True
        RemoveSymbolCharacters = True
        MatchPeptidePrefixAndSuffixToProtein = False
        IgnoreILDifferences = False

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

    Public Function LoadParameterFileSettings(parameterFilePath As String) As Boolean

        Try

            If String.IsNullOrWhiteSpace(parameterFilePath) Then
                ' No parameter file specified; default settings will be used
                Return True
            End If

            If Not File.Exists(parameterFilePath) Then
                ' See if parameterFilePath points to a file in the same directory as the application
                Dim alternateFilePath = Path.Combine(GetAppDirectoryPath(), Path.GetFileName(parameterFilePath))
                If Not File.Exists(alternateFilePath) Then
                    ' Parameter file still not found
                    SetErrorMessage("Parameter file not found: " & parameterFilePath)
                    Return False
                Else
                    parameterFilePath = String.Copy(alternateFilePath)
                End If
            End If

            Dim settingsFileReader = New XmlSettingsFileAccessor()

            If settingsFileReader.LoadSettings(parameterFilePath) Then

                If Not settingsFileReader.SectionPresent(XML_SECTION_PROCESSING_OPTIONS) Then
                    OnWarningEvent("The node '<section name=""" & XML_SECTION_PROCESSING_OPTIONS & """> was not found in the parameter file: " & parameterFilePath)
                Else
                    OutputProteinSequence = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", OutputProteinSequence)
                    SearchAllProteinsForPeptideSequence = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", SearchAllProteinsForPeptideSequence)
                    SaveProteinToPeptideMappingFile = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", SaveProteinToPeptideMappingFile)
                    SaveSourceDataPlusProteinsFile = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveSourceDataPlusProteinsFile", SaveSourceDataPlusProteinsFile)

                    TrackPeptideCounts = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", TrackPeptideCounts)
                    RemoveSymbolCharacters = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", RemoveSymbolCharacters)
                    MatchPeptidePrefixAndSuffixToProtein = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", MatchPeptidePrefixAndSuffixToProtein)
                    IgnoreILDifferences = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", IgnoreILDifferences)

                    PeptideFileSkipFirstLine = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", PeptideFileSkipFirstLine)
                    PeptideInputFileDelimiter = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", PeptideInputFileDelimiter).Chars(0)
                    PeptideFileFormatCode = CType(settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", PeptideFileFormatCode), ePeptideFileColumnOrderingCode)

                    ProteinDataCache.DelimitedFileSkipFirstLine = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", ProteinDataCache.DelimitedFileSkipFirstLine)
                    ProteinDataCache.DelimitedFileDelimiter = settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", ProteinDataCache.DelimitedFileDelimiter).Chars(0)
                    ProteinDataCache.DelimitedFileFormatCode = CType(settingsFileReader.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", ProteinDataCache.DelimitedFileFormatCode), DelimitedFileReader.eDelimitedFileFormatCode)

                End If

            Else
                SetErrorMessage("Error calling settingsFileReader.LoadSettings for " & parameterFilePath)
                Return False
            End If

        Catch ex As Exception
            SetErrorMessage("Error in LoadParameterFileSettings:" & ex.Message, ex)
            SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
            Return False
        End Try

        Return True

    End Function

    Private Function ParsePeptideInputFile(
      peptideInputFilePath As String,
      outputDirectoryPath As String,
      outputFileBaseName As String,
      <Out> ByRef proteinToPepMapFilePath As String) As Boolean

        Const MAX_SHORT_PEPTIDES_TO_CACHE = 1000000

        proteinToPepMapFilePath = String.Empty

        Try
            ' Initialize sepChars
            Dim sepChars = New Char() {PeptideInputFileDelimiter}

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

            If Not File.Exists(peptideInputFilePath) Then
                SetErrorMessage("File not found: " & peptideInputFilePath)
                Return False
            End If

            Dim progressMessageBase = "Reading peptides from " & Path.GetFileName(peptideInputFilePath)
            If UseLeaderSequenceHashTable Then
                progressMessageBase &= " and finding leader sequences"
            Else
                If Not SearchAllProteinsSkipCoverageComputationSteps Then
                    progressMessageBase &= " and computing coverage"
                End If
            End If

            mProgressStepDescription = String.Copy(progressMessageBase)
            Console.WriteLine()
            OnStatusEvent("Parsing " & Path.GetFileName(peptideInputFilePath))

            UpdateProgress(mProgressStepDescription, 0,
               eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

            ' Open the file and read, at most, the first 100,000 characters to see if it contains CrLf or just Lf
            Dim terminatorSize = DetermineLineTerminatorSize(peptideInputFilePath)

            ' Possibly open the file and read the first few line to make sure the number of columns is appropriate
            Dim success = ValidateColumnCountInInputFile(peptideInputFilePath)
            If Not success Then
                Return False
            End If

            If UseLeaderSequenceHashTable Then
                ' Determine the shortest peptide present in the input file
                ' This is a fast process that involves checking the length of each sequence in the input file

                UpdateProgress("Determining the shortest peptide in the input file", 0,
                   eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

                If mLeaderSequenceCache Is Nothing Then
                    mLeaderSequenceCache = New clsLeaderSequenceCache
                Else
                    mLeaderSequenceCache.InitializeVariables()
                End If
                mLeaderSequenceCache.IgnoreILDifferences = IgnoreILDifferences

                Dim columnNumWithPeptideSequence As Integer
                Select Case PeptideFileFormatCode
                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                        columnNumWithPeptideSequence = 2
                    Case Else
                        ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                        columnNumWithPeptideSequence = 1
                End Select

                mLeaderSequenceCache.DetermineShortestPeptideLengthInFile(peptideInputFilePath, terminatorSize, PeptideFileSkipFirstLine, PeptideInputFileDelimiter, columnNumWithPeptideSequence)

                If mAbortProcessing Then
                    Return False
                Else
                    progressMessageBase &= " (leader seq length = " & mLeaderSequenceCache.LeaderSequenceMinimumLength.ToString & ")"

                    UpdateProgress(progressMessageBase)
                End If
            End If

            Dim invalidLineCount = 0

            ' Open the peptide file and read in the lines
            Using reader = New StreamReader(New FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                ' Create the protein to peptide match details file
                mProteinToPeptideMappingFilePath = ConstructOutputFilePath(peptideInputFilePath, FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                                                                           outputDirectoryPath, outputFileBaseName)

                If SaveProteinToPeptideMappingFile Then
                    proteinToPepMapFilePath = String.Copy(mProteinToPeptideMappingFilePath)

                    UpdateProgress("Creating the protein to peptide mapping file: " & Path.GetFileName(mProteinToPeptideMappingFilePath))

                    mProteinToPeptideMappingOutputFile = New StreamWriter(New FileStream(mProteinToPeptideMappingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) With {
                        .AutoFlush = True
                    }

                    mProteinToPeptideMappingOutputFile.WriteLine("Protein Name" & ControlChars.Tab & "Peptide Sequence" & ControlChars.Tab & "Residue Start" & ControlChars.Tab & "Residue End")
                End If

                Dim currentLine = 1
                Dim bytesRead As Long = 0

                Do While Not reader.EndOfStream
                    If mAbortProcessing Then Exit Do

                    Dim dataLine = reader.ReadLine()
                    If dataLine Is Nothing Then Continue Do

                    bytesRead += dataLine.Length + terminatorSize

                    dataLine = dataLine.TrimEnd()

                    If currentLine Mod 500 = 0 Then
                        UpdateProgress("Reading peptide input file", CSng((bytesRead / reader.BaseStream.Length) * 100),
                           eProteinCoverageProcessingSteps.CachePeptides)
                    End If

                    If currentLine = 1 AndAlso PeptideFileSkipFirstLine Then
                        ' do nothing, skip the first line
                    ElseIf dataLine.Length > 0 Then

                        Dim validLine = False
                        Dim proteinName = ""
                        Dim peptideSequence = ""

                        Try

                            ' Split the line, but for efficiency purposes, only parse out the first 3 columns
                            Dim dataCols = dataLine.Split(sepChars, 3)

                            Select Case PeptideFileFormatCode
                                Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                                    proteinName = dataCols(0)

                                    If dataCols.Length > 1 AndAlso Not String.IsNullOrWhiteSpace(dataCols(1)) Then
                                        peptideSequence = dataCols(1)
                                        validLine = True
                                    End If
                                Case Else
                                    ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                    peptideSequence = dataCols(0)
                                    proteinName = String.Empty
                                    validLine = True
                            End Select

                        Catch ex As Exception
                            validLine = False
                        End Try

                        If validLine Then
                            ' Check for and remove prefix and suffix letters
                            ' Also possibly remove symbol characters

                            Dim prefixResidue As Char
                            Dim suffixResidue As Char
                            peptideSequence = GetCleanPeptideSequence(peptideSequence, prefixResidue, suffixResidue, RemoveSymbolCharacters)

                            If UseLeaderSequenceHashTable AndAlso
                             peptideSequence.Length >= mLeaderSequenceCache.LeaderSequenceMinimumLength Then

                                If mLeaderSequenceCache.CachedPeptideCount >= clsLeaderSequenceCache.MAX_LEADER_SEQUENCE_COUNT Then
                                    ' Need to step through the proteins and match them to the data in mLeaderSequenceCache
                                    SearchProteinsUsingLeaderSequences()
                                    mLeaderSequenceCache.InitializeCachedPeptides()
                                End If

                                mLeaderSequenceCache.CachePeptide(peptideSequence, proteinName, prefixResidue, suffixResidue)
                            Else
                                ' Either mUseLeaderSequenceHashTable is false, or the peptide sequence is less than MINIMUM_LEADER_SEQUENCE_LENGTH residues long
                                ' We must search all proteins for the given peptide

                                ' Cache the short peptides in shortPeptideCache
                                If shortPeptideCache.Count >= MAX_SHORT_PEPTIDES_TO_CACHE Then
                                    ' Step through the proteins and match them to the data in shortPeptideCache
                                    SearchProteinsUsingCachedPeptides(shortPeptideCache)
                                    shortPeptideCache.Clear()
                                End If

                                Dim peptideSequenceToCache = prefixResidue & "." & peptideSequence & "." & suffixResidue

                                IncrementCountByKey(shortPeptideCache, peptideSequenceToCache)
                            End If

                        Else
                            invalidLineCount += 1
                        End If

                    End If
                    currentLine += 1

                Loop

            End Using

            If UseLeaderSequenceHashTable Then
                ' Step through the proteins and match them to the data in mLeaderSequenceCache
                If mLeaderSequenceCache.CachedPeptideCount > 0 Then
                    SearchProteinsUsingLeaderSequences()
                End If
            End If

            ' Step through the proteins and match them to the data in shortPeptideCache
            SearchProteinsUsingCachedPeptides(shortPeptideCache)

            If Not mAbortProcessing And Not SearchAllProteinsSkipCoverageComputationSteps Then
                ' Compute the residue coverage percent for each protein
                GetPercentCoverage()
            End If

            If Not mProteinToPeptideMappingOutputFile Is Nothing Then
                mProteinToPeptideMappingOutputFile.Close()
                mProteinToPeptideMappingOutputFile = Nothing
            End If

            If SaveSourceDataPlusProteinsFile Then
                ' Create a new version of the input file, but with all of the proteins listed
                SaveDataPlusAllProteinsFile(peptideInputFilePath, outputDirectoryPath, outputFileBaseName, sepChars, terminatorSize)

            End If

            If invalidLineCount > 0 Then
                Select Case PeptideFileFormatCode
                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                        OnWarningEvent("Found " & invalidLineCount & " lines that did not have two columns (Protein Name and Peptide Sequence).  Those line(s) have been skipped.")
                    Case Else
                        OnWarningEvent("Found " & invalidLineCount & " lines that did not contain a peptide sequence.  Those line(s) have been skipped.")
                End Select
            End If

        Catch ex As Exception
            SetErrorMessage("Error in ParsePeptideInputFile: " & ex.Message, ex)
        End Try

        Return Not mAbortProcessing

    End Function

    Private Function ParseProteinInputFile() As Boolean
        Dim success = False

        Try
            mProgressStepDescription = "Reading protein input file"

            With ProteinDataCache

                ' Protein file options
                If clsProteinFileDataCache.IsFastaFile(ProteinInputFilePath) Then
                    ' .fasta or .fsa file
                    ProteinDataCache.AssumeFastaFile = True
                ElseIf Path.GetExtension(ProteinInputFilePath).ToLower() = ".txt" Then
                    ProteinDataCache.AssumeDelimitedFile = True
                Else
                    ProteinDataCache.AssumeFastaFile = False
                End If

                If SearchAllProteinsSkipCoverageComputationSteps Then
                    ' Make sure all of the protein sequences are uppercase
                    .ChangeProteinSequencesToLowercase = False
                    .ChangeProteinSequencesToUppercase = True
                Else
                    ' Make sure all of the protein sequences are lowercase
                    .ChangeProteinSequencesToLowercase = True
                    .ChangeProteinSequencesToUppercase = False
                End If

                success = .ParseProteinFile(ProteinInputFilePath)

                If Not success Then
                    SetErrorMessage("Error parsing protein file: " & .StatusMessage)
                Else
                    If .GetProteinCountCached = 0 Then
                        success = False
                        SetErrorMessage("Error parsing protein file: no protein entries were found in the file.  Please verify that the column order defined for the proteins file is correct.")
                    End If
                End If
            End With

        Catch ex As Exception
            SetErrorMessage("Error in ParseProteinInputFile: " & ex.Message, ex)
        End Try

        Return success
    End Function

    Public Function ProcessFile(inputFilePath As String,
      outputDirectoryPath As String,
      parameterFilePath As String,
      resetErrorCode As Boolean) As Boolean

        Dim proteinToPepMapFilePath As String = String.Empty

        Return ProcessFile(inputFilePath, outputDirectoryPath, parameterFilePath, resetErrorCode, proteinToPepMapFilePath)
    End Function

    Public Function ProcessFile(
      inputFilePath As String,
      outputDirectoryPath As String,
      parameterFilePath As String,
      resetErrorCode As Boolean,
      <Out> ByRef proteinToPepMapFilePath As String,
      Optional outputFileBaseName As String = "") As Boolean

        Dim success As Boolean

        If resetErrorCode Then
            SetErrorCode(eProteinCoverageErrorCodes.NoError)
        End If

        OnStatusEvent("Initializing")
        proteinToPepMapFilePath = String.Empty

        If Not LoadParameterFileSettings(parameterFilePath) Then
            SetErrorMessage("Parameter file load error: " & parameterFilePath)

            If mErrorCode = eProteinCoverageErrorCodes.NoError Then
                SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
            End If

            Return False
        End If

        Try
            mCachedProteinInfoStartIndex = -1
            With ProteinDataCache
                .RemoveSymbolCharacters = RemoveSymbolCharacters
                .IgnoreILDifferences = IgnoreILDifferences
            End With

            If String.IsNullOrWhiteSpace(inputFilePath) Then
                OnErrorEvent("Input file name is empty")
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            End If

            ' Note that the results file path will be auto-defined in CreateProteinCoverageFile
            mResultsFilePath = String.Empty

            If String.IsNullOrWhiteSpace(ProteinInputFilePath) Then
                SetErrorMessage("Protein file name is empty")
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            ElseIf Not File.Exists(ProteinInputFilePath) Then
                SetErrorMessage("Protein input file not found: " & ProteinInputFilePath)
                SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
                Return False
            End If

            ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizer.ProcessFile_Start", True)

            ' First read the protein input file
            mProgressStepDescription = "Reading protein input file: " & Path.GetFileName(ProteinInputFilePath)
            UpdateProgress(mProgressStepDescription, 0, eProteinCoverageProcessingSteps.CacheProteins)

            success = ParseProteinInputFile()

            If success Then
                mProgressStepDescription = "Complete reading protein input file: " & Path.GetFileName(ProteinInputFilePath)
                UpdateProgress(mProgressStepDescription, 100, eProteinCoverageProcessingSteps.CacheProteins)

                ' Now read the peptide input file
                success = ParsePeptideInputFile(inputFilePath, outputDirectoryPath, outputFileBaseName, proteinToPepMapFilePath)

                If success And Not SearchAllProteinsSkipCoverageComputationSteps Then
                    CreateProteinCoverageFile(inputFilePath, outputDirectoryPath, outputFileBaseName)
                End If

                UpdateProgress("Processing complete; deleting the temporary SQLite database", 100,
                   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

                'All done; delete the temporary SQLite database
                ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizer.ProcessFile_Complete")

                UpdateProgress("Done")

                mProteinPeptideStats = Nothing
            End If

        Catch ex As Exception
            SetErrorMessage("Error in ProcessFile:" & ControlChars.NewLine & ex.Message, ex)
            success = False
        End Try

        Return success

    End Function

    ''' <summary>
    ''' Read the next chunk of proteins from the database (SequenceID, ProteinName, ProteinSequence)
    ''' </summary>
    ''' <returns>The number of records read</returns>
    ''' <remarks></remarks>
    Private Function ReadProteinInfoChunk(startIndex As Integer, proteinUpdated() As Boolean, forceReload As Boolean) As Integer

        ' We use a SQLite database to store the protein sequences (to avoid running out of memory when parsing large protein lists)
        ' However, we will store the most recently loaded peptides in mCachedProteinInfoCount() and
        ' will only reload them if startIndex is different than mCachedProteinInfoStartIndex

        ' Reset the values in proteinUpdated()
        Array.Clear(proteinUpdated, 0, proteinUpdated.Length)

        If Not forceReload AndAlso
           mCachedProteinInfoStartIndex >= 0 AndAlso
           mCachedProteinInfoStartIndex = startIndex AndAlso
           Not mCachedProteinInfo Is Nothing Then

            ' The data loaded in memory is already valid; no need to reload
            Return mCachedProteinInfoCount
        End If

        ' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
        ' Store the information in the four local arrays

        Dim endIndex = startIndex + PROTEIN_CHUNK_COUNT - 1

        mCachedProteinInfoStartIndex = startIndex
        mCachedProteinInfoCount = 0
        If mCachedProteinInfo Is Nothing Then
            ReDim mCachedProteinInfo(PROTEIN_CHUNK_COUNT - 1)
        End If

        For Each udtProtein In ProteinDataCache.GetCachedProteins(startIndex, endIndex)
            With mCachedProteinInfo(mCachedProteinInfoCount)
                .UniqueSequenceID = udtProtein.UniqueSequenceID
                .Description = udtProtein.Description
                .Name = udtProtein.Name
                .Sequence = udtProtein.Sequence
                .PercentCoverage = udtProtein.PercentCoverage
            End With

            mCachedProteinInfoCount += 1
        Next

        Return mCachedProteinInfoCount

    End Function

    Private Sub SaveDataPlusAllProteinsFile(
      peptideInputFilePath As String,
      outputDirectoryPath As String,
      outputFileBaseName As String,
      sepChars() As Char,
      terminatorSize As Integer)

        Try
            Dim dataPlusAllProteinsFile = ConstructOutputFilePath(peptideInputFilePath, FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS,
                                                                 outputDirectoryPath, outputFileBaseName)

            UpdateProgress("Creating the data plus all-proteins output file: " & Path.GetFileName(dataPlusAllProteinsFile))

            Using dataPlusProteinsWriter = New StreamWriter(New FileStream(dataPlusAllProteinsFile, FileMode.Create, FileAccess.Write, FileShare.Read))

                Dim currentLine = 1
                Dim bytesRead As Long = 0

                Using reader = New StreamReader(New FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Do While Not reader.EndOfStream
                        Dim dataLine = reader.ReadLine()
                        If dataLine Is Nothing Then Continue Do

                        bytesRead += dataLine.Length + terminatorSize
                        dataLine = dataLine.TrimEnd()

                        If currentLine Mod 500 = 0 Then
                            UpdateProgress("Creating the data plus all-proteins output file", CSng((bytesRead / reader.BaseStream.Length) * 100), eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile)
                        End If

                        If currentLine = 1 AndAlso PeptideFileSkipFirstLine Then
                            ' Print out the first line, but append a new column name
                            dataPlusProteinsWriter.WriteLine(dataLine & ControlChars.Tab & "Protein_Name")

                        ElseIf dataLine.Length > 0 Then

                            Dim validLine = False
                            Dim peptideSequence = ""

                            Try

                                ' Split the line, but for efficiency purposes, only parse out the first 3 columns
                                Dim dataCols = dataLine.Split(sepChars, 3)

                                Select Case PeptideFileFormatCode
                                    Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                                        ' proteinName = dataCols(0)

                                        If dataCols.Length > 1 AndAlso Not String.IsNullOrWhiteSpace(dataCols(1)) Then
                                            peptideSequence = dataCols(1)
                                            validLine = True
                                        End If
                                    Case Else
                                        ' Includes ePeptideFileColumnOrderingCode.SequenceOnly
                                        peptideSequence = dataCols(0)
                                        ' proteinName = String.Empty
                                        validLine = True
                                End Select

                            Catch ex As Exception
                                validLine = False
                            End Try

                            If Not validLine Then
                                dataPlusProteinsWriter.WriteLine(dataLine & ControlChars.Tab & "?")
                            Else
                                Dim prefixResidue As Char
                                Dim suffixResidue As Char
                                peptideSequence = GetCleanPeptideSequence(peptideSequence, prefixResidue, suffixResidue, RemoveSymbolCharacters)

                                Dim proteins As List(Of String) = Nothing
                                If mPeptideToProteinMapResults.TryGetValue(peptideSequence, proteins) Then

                                    For Each protein As String In proteins
                                        dataPlusProteinsWriter.WriteLine(dataLine & ControlChars.Tab & protein)
                                    Next
                                Else
                                    If currentLine = 1 Then
                                        ' This is likely a header line
                                        dataPlusProteinsWriter.WriteLine(dataLine & ControlChars.Tab & "Protein_Name")
                                    Else
                                        dataPlusProteinsWriter.WriteLine(dataLine & ControlChars.Tab & "?")
                                    End If
                                End If
                            End If
                        Else
                            dataPlusProteinsWriter.WriteLine()
                        End If

                        currentLine += 1
                    Loop


                End Using

            End Using

        Catch ex As Exception
            SetErrorMessage("Error in SaveDataPlusAllProteinsFile: " & ex.Message, ex)
        End Try
    End Sub

    Private Sub SearchProteinsUsingLeaderSequences()

        Dim leaderSequenceMinimumLength As Integer = mLeaderSequenceCache.LeaderSequenceMinimumLength

        Dim proteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

        ' Step through the proteins in memory and compare the residues for each to mLeaderSequenceHashTable
        ' If mSearchAllProteinsForPeptideSequence = False, then require that the protein name in the peptide input file matches the protein being examined

        Try
            Dim progressMessageBase = "Comparing proteins to peptide leader sequences"
            OnStatusEvent(progressMessageBase)

            Dim proteinProcessIterations = 0
            Dim proteinProcessIterationsExpected = CInt(Math.Ceiling(ProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * PROTEIN_CHUNK_COUNT
            If proteinProcessIterationsExpected < 1 Then proteinProcessIterationsExpected = 1

            UpdateProgress(progressMessageBase, 0,
               eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)

            Dim startIndex = 0
            Do
                ' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQLite database
                ' Store the information in the four local arrays
                Dim proteinCount = ReadProteinInfoChunk(startIndex, proteinUpdated, False)

                For proteinIndex = 0 To proteinCount - 1

                    Dim proteinSequence = String.Copy(mCachedProteinInfo(proteinIndex).Sequence)
                    Dim proteinSequenceUpdated = False

                    For proteinSeqCharIndex = 0 To proteinSequence.Length - leaderSequenceMinimumLength

                        Dim cachedPeptideMatchIndex As Integer

                        ' Call .GetFirstPeptideIndexForLeaderSequence to see if the sequence cache contains the leaderSequenceMinimumLength residues starting at proteinSeqCharIndex
                        If SearchAllProteinsSkipCoverageComputationSteps Then
                            ' No need to capitalize proteinSequence since it's already capitalized
                            cachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength))
                        Else
                            ' Need to change proteinSequence to all caps when calling GetFirstPeptideIndexForLeaderSequence
                            cachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength).ToUpper)
                        End If


                        If cachedPeptideMatchIndex >= 0 Then
                            ' mLeaderSequenceCache contains 1 or more peptides that start with proteinSequence.Substring(proteinSeqCharIndex, leaderSequenceMinimumLength)
                            ' Test each of the peptides against this protein

                            Do
                                Dim testPeptide As Boolean

                                If SearchAllProteinsForPeptideSequence Then
                                    testPeptide = True
                                Else
                                    ' Make sure that the protein for cachedPeptideMatchIndex matches this protein name
                                    If String.Equals(mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).ProteinName,
                                                     mCachedProteinInfo(proteinIndex).Name, StringComparison.CurrentCultureIgnoreCase) Then
                                        testPeptide = True
                                    Else
                                        testPeptide = False
                                    End If
                                End If

                                ' Cache the peptide length in peptideLength
                                Dim peptideLength = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence.Length

                                ' Only compare the full sequence to the protein if:
                                '  a) the protein name matches (or mSearchAllProteinsForPeptideSequence = True) and
                                '  b) the peptide sequence doesn't pass the end of the protein
                                If testPeptide AndAlso proteinSeqCharIndex + peptideLength <= proteinSequence.Length Then

                                    ' See if the full sequence matches the protein
                                    Dim matchFound = False
                                    If SearchAllProteinsSkipCoverageComputationSteps Then
                                        ' No need to capitalize proteinSequence since it's already capitalized
                                        If IgnoreILDifferences Then
                                            If proteinSequence.Substring(proteinSeqCharIndex, peptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequenceLtoI Then
                                                matchFound = True
                                            End If
                                        Else
                                            If proteinSequence.Substring(proteinSeqCharIndex, peptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence Then
                                                matchFound = True
                                            End If
                                        End If
                                    Else
                                        ' Need to change proteinSequence to all caps when comparing to .PeptideSequence
                                        If IgnoreILDifferences Then
                                            If proteinSequence.Substring(proteinSeqCharIndex, peptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequenceLtoI Then
                                                matchFound = True
                                            End If
                                        Else
                                            If proteinSequence.Substring(proteinSeqCharIndex, peptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence Then
                                                matchFound = True
                                            End If
                                        End If

                                    End If

                                    If matchFound Then
                                        Dim endIndex = proteinSeqCharIndex + peptideLength - 1
                                        If MatchPeptidePrefixAndSuffixToProtein Then
                                            matchFound = ValidatePrefixAndSuffix(proteinSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PrefixLtoI, mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).SuffixLtoI, proteinSeqCharIndex, endIndex)
                                        End If

                                        If matchFound Then
                                            Dim peptideSequenceForKeySource As String
                                            Dim peptideSequenceForKey As String

                                            If MatchPeptidePrefixAndSuffixToProtein Then
                                                peptideSequenceForKeySource = ConstructPeptideSequenceForKey(mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).Prefix, mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).Suffix)
                                            Else
                                                ' I'm purposely not using String.Copy() here in order to obtain increased speed
                                                peptideSequenceForKeySource = mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence
                                            End If

                                            If IgnoreILDifferences Then
                                                ' Replace all L characters with I
                                                peptideSequenceForKey = peptideSequenceForKeySource.Replace("L"c, "I"c)
                                            Else
                                                ' I'm purposely not using String.Copy() here in order to obtain increased speed
                                                peptideSequenceForKey = peptideSequenceForKeySource
                                            End If

                                            If Not SearchAllProteinsSkipCoverageComputationSteps Then
                                                ' Capitalize the protein sequence letters where this peptide matched
                                                Dim nextStartIndex = endIndex + 1

                                                Dim newProteinSequence = String.Empty
                                                If proteinSeqCharIndex > 0 Then
                                                    newProteinSequence = proteinSequence.Substring(0, proteinSeqCharIndex)
                                                End If
                                                newProteinSequence &= proteinSequence.Substring(proteinSeqCharIndex, nextStartIndex - proteinSeqCharIndex).ToUpper
                                                newProteinSequence &= proteinSequence.Substring(nextStartIndex)
                                                proteinSequence = String.Copy(newProteinSequence)

                                                proteinSequenceUpdated = True
                                            End If

                                            Dim matchIsNew As Boolean

                                            If TrackPeptideCounts Then
                                                Dim proteinPeptideKey = CStr(mCachedProteinInfo(proteinIndex).UniqueSequenceID) & "::" & peptideSequenceForKey

                                                matchIsNew = IncrementCountByKey(mProteinPeptideStats, proteinPeptideKey)
                                            Else
                                                ' Must always assume the match is new since not tracking peptide counts
                                                matchIsNew = True
                                            End If

                                            If matchIsNew Then
                                                If SaveProteinToPeptideMappingFile Then
                                                    WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(proteinIndex).Name, peptideSequenceForKeySource, proteinSeqCharIndex + 1, endIndex + 1)
                                                End If

                                                If SaveSourceDataPlusProteinsFile Then
                                                    StorePeptideToProteinMatch(mLeaderSequenceCache.mCachedPeptideSeqInfo(cachedPeptideMatchIndex).PeptideSequence, mCachedProteinInfo(proteinIndex).Name)
                                                End If

                                            End If
                                        End If
                                    End If
                                End If

                                cachedPeptideMatchIndex = mLeaderSequenceCache.GetNextPeptideWithLeaderSequence(cachedPeptideMatchIndex)
                            Loop While cachedPeptideMatchIndex >= 0
                        End If
                    Next proteinSeqCharIndex

                    If proteinSequenceUpdated Then
                        mCachedProteinInfo(proteinIndex).Sequence = String.Copy(proteinSequence)
                        proteinUpdated(proteinIndex) = True
                    End If

                    proteinProcessIterations += 1
                    If proteinProcessIterations Mod 100 = 0 Then
                        UpdateProgress(CSng(proteinProcessIterations / proteinProcessIterationsExpected * 100),
                           eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)
                    End If

                    If mAbortProcessing Then Exit For

                Next

                ' Store the updated protein sequence information in the SQLite DB
                UpdateSequenceDbDataValues(proteinUpdated, proteinCount)

                ' Increment startIndex to obtain the next chunk of proteins
                startIndex += PROTEIN_CHUNK_COUNT

            Loop While startIndex < ProteinDataCache.GetProteinCountCached()

        Catch ex As Exception
            SetErrorMessage("Error in SearchProteinsUsingLeaderSequences: " & ex.Message, ex)
        End Try

    End Sub

    Private Sub SearchProteinsUsingCachedPeptides(shortPeptideCache As IDictionary(Of String, Integer))

        If shortPeptideCache.Count > 0 Then
            UpdateProgress("Comparing proteins to short peptide sequences")

            ' Need to step through the proteins and match them to the data in shortPeptideCache
            FindSequenceMatchForPeptideList(shortPeptideCache, String.Empty)
        End If

    End Sub

    Private Sub StorePeptideToProteinMatch(cleanPeptideSequence As String, proteinName As String)

        ' Store the mapping between peptide sequence and protein name
        Dim proteins As List(Of String) = Nothing
        If mPeptideToProteinMapResults.TryGetValue(cleanPeptideSequence, proteins) Then
            proteins.Add(proteinName)
        Else
            proteins = New List(Of String) From {
                proteinName
            }
            mPeptideToProteinMapResults.Add(cleanPeptideSequence, proteins)
        End If

    End Sub

    Private Function ValidateColumnCountInInputFile(peptideInputFilePath As String) As Boolean

        Dim success As Boolean

        If PeptideFileFormatCode = ePeptideFileColumnOrderingCode.SequenceOnly Then
            ' Simply return true; don't even pre-read the file
            ' However, auto-switch mSearchAllProteinsForPeptideSequence to true if not true
            If Not SearchAllProteinsForPeptideSequence Then
                SearchAllProteinsForPeptideSequence = True
            End If
            Return True
        End If

        success = ValidateColumnCountInInputFile(peptideInputFilePath, PeptideFileFormatCode, PeptideFileSkipFirstLine, PeptideInputFileDelimiter)

        If success Then
            If PeptideFileFormatCode = ePeptideFileColumnOrderingCode.SequenceOnly Then
                ' Need to auto-switch to search all proteins
                SearchAllProteinsForPeptideSequence = True
            End If
        End If

        Return success
    End Function

    ''' <summary>
    ''' Read the first two lines to check whether the data file actually has only one column when the user has
    ''' specified mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
    ''' If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly, the file isn't even opened
    ''' </summary>
    ''' <param name="peptideInputFilePath"></param>
    ''' <param name="ePeptideFileColumnOrdering">Input / Output parameter</param>
    ''' <param name="skipFirstLine"></param>
    ''' <param name="columnDelimiter"></param>
    ''' <returns>True if no problems; False if the user chooses to abort</returns>
    Public Shared Function ValidateColumnCountInInputFile(
      peptideInputFilePath As String,
      ByRef ePeptideFileColumnOrdering As ePeptideFileColumnOrderingCode,
      skipFirstLine As Boolean,
      columnDelimiter As Char) As Boolean

        ' Open the file and read in the lines
        Using reader = New StreamReader(New FileStream(peptideInputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

            Dim currentLine = 1
            Do While Not reader.EndOfStream AndAlso currentLine < 3
                Dim lineIn = reader.ReadLine
                If lineIn Is Nothing Then Continue Do

                Dim dataLine = lineIn.TrimEnd()

                If currentLine = 1 AndAlso skipFirstLine Then
                    ' do nothing, skip the first line
                ElseIf dataLine.Length > 0 Then
                    Try
                        Dim dataCols = dataLine.Split(columnDelimiter)

                        If (Not skipFirstLine AndAlso currentLine = 1) OrElse
                           (skipFirstLine AndAlso currentLine = 2) Then
                            If dataCols.Length = 1 AndAlso ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence Then
                                ' Auto switch to ePeptideFileColumnOrderingCode.SequenceOnly
                                ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly
                            End If
                        End If

                    Catch ex As Exception
                        ' Ignore the error
                    End Try
                End If
                currentLine += 1
            Loop

        End Using

        Return True

    End Function

    Private Function ValidatePrefixAndSuffix(proteinSequence As String, prefixResidue As Char, suffixResidue As Char, startIndex As Integer, endIndex As Integer) As Boolean

        Dim matchValid = True

        If Char.IsLetter(prefixResidue) Then
            If startIndex >= 1 Then
                If Char.ToUpper(proteinSequence.Chars(startIndex - 1)) <> prefixResidue Then
                    matchValid = False
                End If
            End If
        ElseIf prefixResidue = "-"c AndAlso startIndex <> 0 Then
            matchValid = False
        End If

        If matchValid Then
            If Char.IsLetter(suffixResidue) Then
                If endIndex < proteinSequence.Length - 1 Then
                    If Char.ToUpper(proteinSequence.Chars(endIndex + 1)) <> suffixResidue Then
                        matchValid = False
                    End If
                Else
                    matchValid = False
                End If
            ElseIf suffixResidue = "-"c AndAlso endIndex < proteinSequence.Length - 1 Then
                matchValid = False
            End If
        End If

        Return matchValid

    End Function

    Private Sub WriteEntryToProteinToPeptideMappingFile(proteinName As String, peptideSequenceForKey As String, startResidue As Integer, endResidue As Integer)
        If SaveProteinToPeptideMappingFile AndAlso Not mProteinToPeptideMappingOutputFile Is Nothing Then
            mProteinToPeptideMappingOutputFile.WriteLine(proteinName & ControlChars.Tab & peptideSequenceForKey & ControlChars.Tab & startResidue & ControlChars.Tab & endResidue)
        End If
    End Sub

    Protected Sub ResetProgress(stepDescription As String)
        mProgressStepDescription = String.Copy(stepDescription)
        mProgressPercentComplete = 0
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub SetErrorCode(eNewErrorCode As eProteinCoverageErrorCodes)
        SetErrorCode(eNewErrorCode, False)
    End Sub

    Protected Sub SetErrorCode(eNewErrorCode As eProteinCoverageErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        If leaveExistingErrorCodeUnchanged AndAlso mErrorCode <> eProteinCoverageErrorCodes.NoError Then
            ' An error code is already defined; do not change it
        Else
            mErrorCode = eNewErrorCode
        End If
    End Sub

    Protected Sub SetErrorMessage(message As String, Optional ex As Exception = Nothing)
        If message Is Nothing Then
            mErrorMessage = String.Empty
        Else
            mErrorMessage = message
        End If

        If mErrorMessage.Length > 0 Then
            OnErrorEvent(mErrorMessage, ex)
            UpdateProgress(mErrorMessage)
        End If
    End Sub

    Protected Sub UpdateProgress(stepDescription As String)
        mProgressStepDescription = String.Copy(stepDescription)
        RaiseEvent ProgressChanged(ProgressStepDescription, ProgressPercentComplete)
    End Sub

    Protected Sub UpdateProgress(percentComplete As Single, eCurrentProcessingStep As eProteinCoverageProcessingSteps)
        UpdateProgress(ProgressStepDescription, percentComplete, eCurrentProcessingStep)
    End Sub

    Protected Sub UpdateProgress(stepDescription As String, percentComplete As Single, eCurrentProcessingStep As eProteinCoverageProcessingSteps)

        mProgressStepDescription = String.Copy(stepDescription)
        mCurrentProcessingStep = eCurrentProcessingStep

        If percentComplete < 0 Then
            percentComplete = 0
        ElseIf percentComplete > 100 Then
            percentComplete = 100
        End If

        Dim startPercent = mPercentCompleteStartLevels(eCurrentProcessingStep)
        Dim endPercent = mPercentCompleteStartLevels(eCurrentProcessingStep + 1)

        ' Use the start and end percent complete values for the specified processing step to convert percentComplete to an overall percent complete value
        mProgressPercentComplete = startPercent + CSng(percentComplete / 100.0 * (endPercent - startPercent))

        RaiseEvent ProgressChanged(ProgressStepDescription, ProgressPercentComplete)
    End Sub

    Private Sub LeaderSequenceCache_ProgressChanged(taskDescription As String, percentComplete As Single) Handles mLeaderSequenceCache.ProgressChanged
        UpdateProgress(percentComplete, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
    End Sub

    Private Sub LeaderSequenceCache_ProgressComplete() Handles mLeaderSequenceCache.ProgressComplete
        UpdateProgress(100, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
    End Sub

    Private Sub ProteinDataCache_ProteinCachedWithProgress(proteinsCached As Integer, percentFileProcessed As Single) Handles ProteinDataCache.ProteinCachedWithProgress
        Const CONSOLE_UPDATE_INTERVAL_SECONDS = 3

        Static lastUpdate As DateTime = DateTime.UtcNow

        If DateTime.UtcNow.Subtract(lastUpdate).TotalSeconds >= CONSOLE_UPDATE_INTERVAL_SECONDS Then
            lastUpdate = DateTime.UtcNow
            Console.Write(".")
        End If

        UpdateProgress(percentFileProcessed, eProteinCoverageProcessingSteps.CacheProteins)

    End Sub

    Private Sub ProteinDataCache_ProteinCachingComplete() Handles ProteinDataCache.ProteinCachingComplete
        UpdateProgress(100, eProteinCoverageProcessingSteps.CacheProteins)
    End Sub
End Class
