Option Strict On

Imports System.IO

' This class tracks the first n letters of each peptide sent to it, while also
' tracking the peptides and the location of those peptides in the leader sequence hash table
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Class started August 24, 2007
'
' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://panomics.pnl.gov/ or https://omics.pnl.gov
' -------------------------------------------------------------------------------
'
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' http://www.apache.org/licenses/LICENSE-2.0
'

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
        Public ProteinName As String                    ' The protein name is optional
        Public PeptideSequence As String                ' Note that residues are stored as uppercase letters
        Public Prefix As Char
        Public Suffix As Char
        Public PeptideSequenceLtoI As String            ' Only used if mIgnoreILDifferences=True
        Public PrefixLtoI As Char              ' Only used if mIgnoreILDifferences=True
        Public SuffixLtoI As Char              ' Only used if mIgnoreILDifferences=True
    End Structure
#End Region

#Region "Classwide variables"
    Private mLeaderSequenceMinimumLength As Integer
    Private mLeaderSequences As Dictionary(Of String, Integer)

    Public mCachedPeptideCount As Integer
    Public mCachedPeptideSeqInfo() As udtPeptideSequenceInfoType
    Private mCachedPeptideToHashIndexPointer() As Integer               ' Parallel to mCachedPeptideSeqInfo
    Private mIndicesSorted As Boolean

    Private mErrorMessage As String
    Private mAbortProcessing As Boolean

    Private mIgnoreILDifferences As Boolean

    Public Event ProgressReset()
    Public Event ProgressChanged(taskDescription As String, percentComplete As Single)     ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
    Public Event ProgressComplete()

    Protected mProgressStepDescription As String
    Protected mProgressPercentComplete As Single        ' Ranges from 0 to 100, but can contain decimal percentage values

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
        Get
            Return mIgnoreILDifferences
        End Get
        Set
            mIgnoreILDifferences = Value
        End Set
    End Property

    Public Property LeaderSequenceMinimumLength As Integer
        Get
            Return mLeaderSequenceMinimumLength
        End Get
        Set
            mLeaderSequenceMinimumLength = Value
        End Set
    End Property

    Public ReadOnly Property ProgressStepDescription As String
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

#End Region

    Public Sub AbortProcessingNow()
        mAbortProcessing = True
    End Sub

    Public Function CachePeptide(strPeptideSequence As String, chPrefixResidue As Char, chSuffixResidue As Char) As Boolean
        Return CachePeptide(strPeptideSequence, Nothing, chPrefixResidue, chSuffixResidue)
    End Function

    Public Function CachePeptide(strPeptideSequence As String, strProteinName As String, chPrefixResidue As Char, chSuffixResidue As Char) As Boolean
        ' Caches the peptide and updates mLeaderSequenceHashTable

        Dim blnSuccess As Boolean

        Dim objItem As Object
        Dim strLeaderSequence As String
        Dim chPrefixResidueLtoI As Char
        Dim chSuffixResidueLtoI As Char

        Dim intHashIndexPointer As Integer

        Try
            If strPeptideSequence Is Nothing OrElse strPeptideSequence.Length < mLeaderSequenceMinimumLength Then
                ' Peptide is too short; cannot process it
                mErrorMessage = "Peptide length is shorter than " & mLeaderSequenceMinimumLength.ToString & "; unable to cache the peptide"
                Return False
            Else
                mErrorMessage = String.Empty
            End If

            ' Make sure the residues are capitalized
            strPeptideSequence = strPeptideSequence.ToUpper
            If Char.IsLetter(chPrefixResidue) Then chPrefixResidue = Char.ToUpper(chPrefixResidue)
            If Char.IsLetter(chSuffixResidue) Then chSuffixResidue = Char.ToUpper(chSuffixResidue)

            strLeaderSequence = strPeptideSequence.Substring(0, mLeaderSequenceMinimumLength)
            chPrefixResidueLtoI = chPrefixResidue
            chSuffixResidueLtoI = chSuffixResidue

            If mIgnoreILDifferences Then
                ' Replace all L characters with I
                strLeaderSequence = strLeaderSequence.Replace("L"c, "I"c)

                If chPrefixResidueLtoI = "L"c Then chPrefixResidueLtoI = "I"c
                If chSuffixResidueLtoI = "L"c Then chSuffixResidueLtoI = "I"c
            End If

            Dim hashIndexPointer As Integer

            ' Look for strLeaderSequence in mLeaderSequences
            If Not mLeaderSequences.TryGetValue(strLeaderSequence, hashIndexPointer) Then
                ' strLeaderSequence was not found; add it and initialize intHashIndexPointer
                hashIndexPointer = mLeaderSequences.Count
                mLeaderSequences.Add(strLeaderSequence, hashIndexPointer)
            End If

            ' Expand mCachedPeptideSeqInfo if needed
            If mCachedPeptideCount >= mCachedPeptideSeqInfo.Length AndAlso mCachedPeptideCount < MAX_LEADER_SEQUENCE_COUNT Then
                ReDim Preserve mCachedPeptideSeqInfo(mCachedPeptideSeqInfo.Length * 2 - 1)
                ReDim Preserve mCachedPeptideToHashIndexPointer(mCachedPeptideSeqInfo.Length - 1)
            End If

            ' Add strPeptideSequence to mCachedPeptideSeqInfo
            With mCachedPeptideSeqInfo(mCachedPeptideCount)
                .ProteinName = String.Copy(strProteinName)
                .PeptideSequence = String.Copy(strPeptideSequence)
                .Prefix = chPrefixResidue
                .Suffix = chSuffixResidue
                .PrefixLtoI = chPrefixResidueLtoI
                .SuffixLtoI = chSuffixResidueLtoI
                If mIgnoreILDifferences Then
                    .PeptideSequenceLtoI = strPeptideSequence.Replace("L"c, "I"c)
                End If
            End With

            ' Update the peptide to Hash Index pointer array
            mCachedPeptideToHashIndexPointer(mCachedPeptideCount) = intHashIndexPointer
            mCachedPeptideCount += 1
            mIndicesSorted = False

            blnSuccess = True

        Catch ex As Exception
            Throw New System.Exception("Error in CachePeptide", ex)
            Return False
        End Try

        Return blnSuccess

    End Function

    Public Function DetermineShortestPeptideLengthInFile(strInputFilePath As String, intTerminatorSize As Integer,
                            blnPeptideFileSkipFirstLine As Boolean, chPeptideInputFileDelimiter As Char,
                            intColumnNumWithPeptideSequence As Integer) As Boolean

        ' Parses strInputFilePath examining column intColumnNumWithPeptideSequence to determine the minimum peptide sequence length present
        ' Updates mLeaderSequenceMinimumLength if successful, though the minimum length is not allowed to be less than MINIMUM_LEADER_SEQUENCE_LENGTH

        ' intColumnNumWithPeptideSequence should be 1 if the peptide sequence is in the first column, 2 if in the second, etc.

        Dim srInFile As StreamReader

        Dim strLineIn As String
        Dim strSplitLine() As String
        Dim strPeptideSequence As String = String.Empty

        Dim bytesRead As Long = 0
        Dim intCurrentLine As Integer
        Dim blnValidLine As Boolean

        Dim intValidPeptideCount As Integer
        Dim intLeaderSequenceMinimumLength As Integer

        Dim blnSuccess As Boolean

        ' Define a RegEx to replace all of the non-letter characters
        Dim reReplaceSymbols As System.Text.RegularExpressions.Regex
        reReplaceSymbols = New System.Text.RegularExpressions.Regex("[^A-Za-z]", System.Text.RegularExpressions.RegexOptions.Compiled)

        Try
            blnSuccess = False

            ' Open the file and read in the lines
            srInFile = New StreamReader(New FileStream(strInputFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

            intValidPeptideCount = 0
            intLeaderSequenceMinimumLength = 0
            intCurrentLine = 1
            Do While Not srInFile.EndOfStream
                If mAbortProcessing Then Exit Do

                strLineIn = srInFile.ReadLine
                bytesRead += strLineIn.Length + intTerminatorSize

                strLineIn = strLineIn.Trim

                If intCurrentLine Mod 100 = 1 Then
                    UpdateProgress("Scanning input file to determine minimum peptide length: " & intCurrentLine.ToString, CSng((bytesRead / srInFile.BaseStream.Length) * 100))
                End If

                If intCurrentLine = 1 AndAlso blnPeptideFileSkipFirstLine Then
                    ' Do nothing, skip the first line
                ElseIf strLineIn.Length > 0 Then

                    Try
                        blnValidLine = False

                        strSplitLine = strLineIn.Split(chPeptideInputFileDelimiter)

                        If intColumnNumWithPeptideSequence >= 1 And intColumnNumWithPeptideSequence < strSplitLine.Length - 1 Then
                            strPeptideSequence = strSplitLine(intColumnNumWithPeptideSequence - 1)
                        Else
                            strPeptideSequence = strSplitLine(0)
                        End If
                        blnValidLine = True
                    Catch ex As Exception
                        blnValidLine = False
                    End Try

                    If blnValidLine Then
                        If strPeptideSequence.Length >= 4 Then
                            ' Check for, and remove any prefix or suffix residues
                            If strPeptideSequence.Chars(1) = "."c AndAlso strPeptideSequence.Chars(strPeptideSequence.Length - 2) = "."c Then
                                strPeptideSequence = strPeptideSequence.Substring(2, strPeptideSequence.Length - 4)
                            End If
                        End If

                        ' Remove any non-letter characters
                        strPeptideSequence = reReplaceSymbols.Replace(strPeptideSequence, String.Empty)

                        If strPeptideSequence.Length >= MINIMUM_LEADER_SEQUENCE_LENGTH Then
                            If intValidPeptideCount = 0 Then
                                intLeaderSequenceMinimumLength = strPeptideSequence.Length
                            Else
                                If strPeptideSequence.Length < intLeaderSequenceMinimumLength Then
                                    intLeaderSequenceMinimumLength = strPeptideSequence.Length
                                End If
                            End If
                            intValidPeptideCount += 1
                        End If
                    End If

                End If
                intCurrentLine += 1
            Loop

            ' Close the input file(s)
            srInFile.Close()

            If intValidPeptideCount = 0 Then
                ' No valid peptides were found; either no peptides are in the file or they're all shorter than MINIMUM_LEADER_SEQUENCE_LENGTH
                mLeaderSequenceMinimumLength = MINIMUM_LEADER_SEQUENCE_LENGTH
                blnSuccess = False
            Else
                mLeaderSequenceMinimumLength = intLeaderSequenceMinimumLength
                blnSuccess = True
            End If

            OperationComplete()

        Catch ex As Exception
            Throw New System.Exception("Error in DetermineShortestPeptideLengthInFile", ex)
            Return False
        End Try

        Return blnSuccess

    End Function

    Public Function GetFirstPeptideIndexForLeaderSequence(strLeaderSequenceToFind As String) As Integer
        ' Looks up the first index value in mCachedPeptideSeqInfo that matches strLeaderSequenceToFind
        ' Returns the index value if found, or -1 if not found
        ' Calls SortIndices if mIndicesSorted = False

        Dim targetHashIndex As Integer

        If Not mLeaderSequences.TryGetValue(strLeaderSequenceToFind, targetHashIndex) Then
            Return -1
        End If

            intTargetHashIndex = CInt(objItem)

            If Not mIndicesSorted Then
                SortIndices()
            End If

            intCachedPeptideMatchIndex = Array.BinarySearch(mCachedPeptideToHashIndexPointer, 0, mCachedPeptideCount, intTargetHashIndex)

            Do While intCachedPeptideMatchIndex > 0 AndAlso mCachedPeptideToHashIndexPointer(intCachedPeptideMatchIndex - 1) = intTargetHashIndex
                intCachedPeptideMatchIndex -= 1
            Loop

            Return intCachedPeptideMatchIndex
        End If

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

        mLeaderSequenceMinimumLength = DEFAULT_LEADER_SEQUENCE_LENGTH
        mErrorMessage = String.Empty
        mAbortProcessing = False

        mIgnoreILDifferences = False

        InitializeCachedPeptides()
    End Sub

    Private Sub SortIndices()
        'Array.Sort(mCachedPeptideToHashIndexPointer, mCachedPeptideSeqInfo, mCachedPeptideCount, New CachedPeptidesSeqInfoComparerClass)
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
