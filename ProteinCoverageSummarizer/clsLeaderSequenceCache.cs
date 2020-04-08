Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Started August 2007
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
Imports System.Text.RegularExpressions

''' <summary>
''' This class tracks the first n letters of each peptide sent to it, while also
''' tracking the peptides and the location of those peptides in the leader sequence hash table
''' </summary>
Public Class clsLeaderSequenceCache

    Public Sub New()
        InitializeVariables()
    End Sub

#Region "Constants and Enums"
    Public Const DEFAULT_LEADER_SEQUENCE_LENGTH As Integer = 5
    Public Const MINIMUM_LEADER_SEQUENCE_LENGTH As Integer = 5

    Private Const INITIAL_LEADER_SEQUENCE_COUNT_TO_RESERVE As Integer = 10000
    Public Const MAX_LEADER_SEQUENCE_COUNT As Integer = 500000
#End Region

#Region "Structures"
    Public Structure udtPeptideSequenceInfoType
        ''' <summary>
        ''' Protein name (optional)
        ''' </summary>
        Public ProteinName As String

        ''' <summary>
        ''' Peptide amino acids (stored as uppercase letters)
        ''' </summary>
        Public PeptideSequence As String

        ''' <summary>
        ''' Prefix residue
        ''' </summary>
        Public Prefix As Char

        ''' <summary>
        ''' Suffix residue
        ''' </summary>
        Public Suffix As Char

        ''' <summary>
        ''' Peptide sequence where leucines have been changed to isoleucine
        ''' </summary>
        ''' <remarks>Only used if mIgnoreILDifferences is True</remarks>
        Public PeptideSequenceLtoI As String

        ''' <summary>
        ''' Prefix residue; if leucine, changed to isoleucine
        ''' </summary>
        ''' <remarks>Only used if mIgnoreILDifferences is True</remarks>
        Public PrefixLtoI As Char

        ''' <summary>
        ''' Suffix residue; if leucine, changed to isoleucine
        ''' </summary>
        ''' <remarks>Only used if mIgnoreILDifferences is True</remarks>
        Public SuffixLtoI As Char

        ''' <summary>
        ''' Show the peptide sequence, including prefix and suffix
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            If String.IsNullOrWhiteSpace(Prefix) Then
                Return PeptideSequence
            End If

            Return Prefix & "." & PeptideSequence & "." & Suffix
        End Function
    End Structure

#End Region

#Region "Classwide variables"
    Private mLeaderSequences As Dictionary(Of String, Integer)

    Public mCachedPeptideCount As Integer
    Public mCachedPeptideSeqInfo() As udtPeptideSequenceInfoType

#Disable Warning IDE0044 ' Add readonly modifier
    ''' <summary>
    ''' Parallel to mCachedPeptideSeqInfo
    ''' </summary>
    Private mCachedPeptideToHashIndexPointer() As Integer
#Enable Warning IDE0044

    Private mIndicesSorted As Boolean

    Private mErrorMessage As String
    Private mAbortProcessing As Boolean

    Public Event ProgressReset()

    ''' <summary>
    ''' Progress changed event
    ''' </summary>
    ''' <param name="taskDescription"></param>
    ''' <param name="percentComplete">Value between 0 and 100, but can contain decimal percentage values</param>
    Public Event ProgressChanged(taskDescription As String, percentComplete As Single)

    Public Event ProgressComplete()

    Protected mProgressStepDescription As String

    ''' <summary>
    ''' Percent complete
    ''' </summary>
    ''' <remarks>
    ''' Value between 0 and 100, but can contain decimal percentage values
    ''' </remarks>
    Protected mProgressPercentComplete As Single

#End Region

#Region "Properties"
    Public ReadOnly Property CachedPeptideCount As Integer
        Get
            Return mCachedPeptideCount
        End Get
    End Property

    Public ReadOnly Property ErrorMessage As String
        Get
            Return mErrorMessage
        End Get
    End Property

    Public Property IgnoreILDifferences As Boolean

    Public Property LeaderSequenceMinimumLength As Integer

    Public ReadOnly Property ProgressStepDescription As String
        Get
            Return mProgressStepDescription
        End Get
    End Property

    ''' <summary>
    ''' Percent complete
    ''' </summary>
    ''' <remarks>
    ''' Value between 0 and 100, but can contain decimal percentage values
    ''' </remarks>
    Public ReadOnly Property ProgressPercentComplete As Single
        Get
            Return CType(Math.Round(mProgressPercentComplete, 2), Single)
        End Get
    End Property

#End Region

    Public Sub AbortProcessingNow()
        mAbortProcessing = True
    End Sub

    ''' <summary>
    ''' Caches the peptide and updates mLeaderSequences
    ''' </summary>
    ''' <param name="peptideSequence">Peptide sequence</param>
    ''' <param name="proteinName">Protein name</param>
    ''' <param name="prefixResidue">Prefix residue</param>
    ''' <param name="suffixResidue">Suffix residue</param>
    ''' <returns></returns>
    Public Function CachePeptide(peptideSequence As String, proteinName As String, prefixResidue As Char, suffixResidue As Char) As Boolean

        Try
            If peptideSequence Is Nothing OrElse peptideSequence.Length < LeaderSequenceMinimumLength Then
                ' Peptide is too short; cannot process it
                mErrorMessage = "Peptide length is shorter than " & LeaderSequenceMinimumLength.ToString & "; unable to cache the peptide"
                Return False
            Else
                mErrorMessage = String.Empty
            End If

            ' Make sure the residues are capitalized
            peptideSequence = peptideSequence.ToUpper
            If Char.IsLetter(prefixResidue) Then prefixResidue = Char.ToUpper(prefixResidue)
            If Char.IsLetter(suffixResidue) Then suffixResidue = Char.ToUpper(suffixResidue)

            Dim leaderSequence = peptideSequence.Substring(0, LeaderSequenceMinimumLength)
            Dim prefixResidueLtoI = prefixResidue
            Dim suffixResidueLtoI = suffixResidue

            If IgnoreILDifferences Then
                ' Replace all L characters with I
                leaderSequence = leaderSequence.Replace("L"c, "I"c)

                If prefixResidueLtoI = "L"c Then prefixResidueLtoI = "I"c
                If suffixResidueLtoI = "L"c Then suffixResidueLtoI = "I"c
            End If

            Dim hashIndexPointer As Integer

            ' Look for leaderSequence in mLeaderSequences
            If Not mLeaderSequences.TryGetValue(leaderSequence, hashIndexPointer) Then
                ' leaderSequence was not found; add it and initialize intHashIndexPointer
                hashIndexPointer = mLeaderSequences.Count
                mLeaderSequences.Add(leaderSequence, hashIndexPointer)
            End If

            ' Expand mCachedPeptideSeqInfo if needed
            If mCachedPeptideCount >= mCachedPeptideSeqInfo.Length AndAlso mCachedPeptideCount < MAX_LEADER_SEQUENCE_COUNT Then
                ReDim Preserve mCachedPeptideSeqInfo(mCachedPeptideSeqInfo.Length * 2 - 1)
                ReDim Preserve mCachedPeptideToHashIndexPointer(mCachedPeptideSeqInfo.Length - 1)
            End If

            ' Add peptideSequence to mCachedPeptideSeqInfo
            With mCachedPeptideSeqInfo(mCachedPeptideCount)
                .ProteinName = String.Copy(proteinName)
                .PeptideSequence = String.Copy(peptideSequence)
                .Prefix = prefixResidue
                .Suffix = suffixResidue
                .PrefixLtoI = prefixResidueLtoI
                .SuffixLtoI = suffixResidueLtoI
                If IgnoreILDifferences Then
                    .PeptideSequenceLtoI = peptideSequence.Replace("L"c, "I"c)
                End If
            End With

            ' Update the peptide to Hash Index pointer array
            mCachedPeptideToHashIndexPointer(mCachedPeptideCount) = hashIndexPointer
            mCachedPeptideCount += 1
            mIndicesSorted = False

            Return True

        Catch ex As Exception
            Throw New Exception("Error in CachePeptide", ex)
        End Try

    End Function

    Public Function DetermineShortestPeptideLengthInFile(
        inputFilePath As String, terminatorSize As Integer,
        peptideFileSkipFirstLine As Boolean, peptideInputFileDelimiter As Char,
        columnNumWithPeptideSequence As Integer) As Boolean

        ' Parses inputFilePath examining column columnNumWithPeptideSequence to determine the minimum peptide sequence length present
        ' Updates mLeaderSequenceMinimumLength if successful, though the minimum length is not allowed to be less than MINIMUM_LEADER_SEQUENCE_LENGTH

        ' columnNumWithPeptideSequence should be 1 if the peptide sequence is in the first column, 2 if in the second, etc.

        ' Define a RegEx to replace all of the non-letter characters
        Dim reReplaceSymbols = New Regex("[^A-Za-z]", RegexOptions.Compiled)

        Try
            Dim validPeptideCount = 0
            Dim leaderSeqMinimumLength = 0

            ' Open the file and read in the lines
            Using reader = New StreamReader(New FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                Dim linesRead = 1
                Dim bytesRead As Long = 0

                Do While Not reader.EndOfStream
                    If mAbortProcessing Then Exit Do

                    Dim dataLine = reader.ReadLine
                    If dataLine Is Nothing Then Continue Do

                    bytesRead += dataLine.Length + terminatorSize

                    dataLine = dataLine.TrimEnd()

                    If linesRead Mod 100 = 1 Then
                        UpdateProgress("Scanning input file to determine minimum peptide length: " & linesRead.ToString,
                                       bytesRead / CSng(reader.BaseStream.Length) * 100)
                    End If

                    If linesRead = 1 AndAlso peptideFileSkipFirstLine Then
                        ' Do nothing, skip the first line
                    ElseIf dataLine.Length > 0 Then

                        Dim validLine As Boolean
                        Dim peptideSequence = ""

                        Try
                            Dim dataCols = dataLine.Split(peptideInputFileDelimiter)

                            If columnNumWithPeptideSequence >= 1 And columnNumWithPeptideSequence < dataCols.Length - 1 Then
                                peptideSequence = dataCols(columnNumWithPeptideSequence - 1)
                            Else
                                peptideSequence = dataCols(0)
                            End If
                            validLine = True
                        Catch ex As Exception
                            validLine = False
                        End Try

                        If validLine Then
                            If peptideSequence.Length >= 4 Then
                                ' Check for, and remove any prefix or suffix residues
                                If peptideSequence.Chars(1) = "."c AndAlso peptideSequence.Chars(peptideSequence.Length - 2) = "."c Then
                                    peptideSequence = peptideSequence.Substring(2, peptideSequence.Length - 4)
                                End If
                            End If

                            ' Remove any non-letter characters
                            peptideSequence = reReplaceSymbols.Replace(peptideSequence, String.Empty)

                            If peptideSequence.Length >= MINIMUM_LEADER_SEQUENCE_LENGTH Then
                                If validPeptideCount = 0 Then
                                    leaderSeqMinimumLength = peptideSequence.Length
                                Else
                                    If peptideSequence.Length < leaderSeqMinimumLength Then
                                        leaderSeqMinimumLength = peptideSequence.Length
                                    End If
                                End If
                                validPeptideCount += 1
                            End If
                        End If

                    End If
                    linesRead += 1
                Loop

            End Using

            Dim success As Boolean

            If validPeptideCount = 0 Then
                ' No valid peptides were found; either no peptides are in the file or they're all shorter than MINIMUM_LEADER_SEQUENCE_LENGTH
                LeaderSequenceMinimumLength = MINIMUM_LEADER_SEQUENCE_LENGTH
                success = False
            Else
                LeaderSequenceMinimumLength = leaderSeqMinimumLength
                success = True
            End If

            OperationComplete()
            Return success

        Catch ex As Exception
            Throw New Exception("Error in DetermineShortestPeptideLengthInFile", ex)
        End Try

    End Function

    Public Function GetFirstPeptideIndexForLeaderSequence(leaderSequenceToFind As String) As Integer
        ' Looks up the first index value in mCachedPeptideSeqInfo that matches strLeaderSequenceToFind
        ' Returns the index value if found, or -1 if not found
        ' Calls SortIndices if mIndicesSorted = False

        Dim targetHashIndex As Integer

        If Not mLeaderSequences.TryGetValue(leaderSequenceToFind, targetHashIndex) Then
            Return -1
        End If

        ' Item found in mLeaderSequences
        ' Return the first peptide index value mapped to the leader sequence

        If Not mIndicesSorted Then
            SortIndices()
        End If

        Dim cachedPeptideMatchIndex = Array.BinarySearch(mCachedPeptideToHashIndexPointer, 0, mCachedPeptideCount, targetHashIndex)

        Do While cachedPeptideMatchIndex > 0 AndAlso mCachedPeptideToHashIndexPointer(cachedPeptideMatchIndex - 1) = targetHashIndex
            cachedPeptideMatchIndex -= 1
        Loop

        Return cachedPeptideMatchIndex

    End Function

    Public Function GetNextPeptideWithLeaderSequence(intCachedPeptideMatchIndexCurrent As Integer) As Integer
        If intCachedPeptideMatchIndexCurrent < mCachedPeptideCount - 1 Then
            If mCachedPeptideToHashIndexPointer(intCachedPeptideMatchIndexCurrent + 1) = mCachedPeptideToHashIndexPointer(intCachedPeptideMatchIndexCurrent) Then
                Return intCachedPeptideMatchIndexCurrent + 1
            Else
                Return -1
            End If
        Else
            Return -1
        End If
    End Function


    Public Sub InitializeCachedPeptides()
        mCachedPeptideCount = 0
        ReDim mCachedPeptideSeqInfo(INITIAL_LEADER_SEQUENCE_COUNT_TO_RESERVE - 1)
        ReDim mCachedPeptideToHashIndexPointer(mCachedPeptideSeqInfo.Length - 1)

        mIndicesSorted = False

        If mLeaderSequences Is Nothing Then
            mLeaderSequences = New Dictionary(Of String, Integer)
        Else
            mLeaderSequences.Clear()
        End If
    End Sub

    Public Sub InitializeVariables()

        LeaderSequenceMinimumLength = DEFAULT_LEADER_SEQUENCE_LENGTH
        mErrorMessage = String.Empty
        mAbortProcessing = False

        IgnoreILDifferences = False

        InitializeCachedPeptides()
    End Sub

    Private Sub SortIndices()
        Array.Sort(mCachedPeptideToHashIndexPointer, mCachedPeptideSeqInfo, 0, mCachedPeptideCount)
        mIndicesSorted = True
    End Sub

    Protected Sub ResetProgress()
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub ResetProgress(strProgressStepDescription As String)
        UpdateProgress(strProgressStepDescription, 0)
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub UpdateProgress(strProgressStepDescription As String)
        UpdateProgress(strProgressStepDescription, mProgressPercentComplete)
    End Sub

    Protected Sub UpdateProgress(sngPercentComplete As Single)
        UpdateProgress(Me.ProgressStepDescription, sngPercentComplete)
    End Sub

    Protected Sub UpdateProgress(strProgressStepDescription As String, sngPercentComplete As Single)
        mProgressStepDescription = String.Copy(strProgressStepDescription)
        If sngPercentComplete < 0 Then
            sngPercentComplete = 0
        ElseIf sngPercentComplete > 100 Then
            sngPercentComplete = 100
        End If
        mProgressPercentComplete = sngPercentComplete

        RaiseEvent ProgressChanged(Me.ProgressStepDescription, Me.ProgressPercentComplete)
    End Sub

    Protected Sub OperationComplete()
        RaiseEvent ProgressComplete()
    End Sub

End Class
