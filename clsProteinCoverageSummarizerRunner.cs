Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Program started June 14, 2005
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

Imports ProteinCoverageSummarizer
Imports ProteinFileReader
'

''' <summary>
''' This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
''' an accompanying file with peptide sequences to then compute the percent coverage of each of the proteins
''' </summary>
Public Class clsProteinCoverageSummarizerRunner
    Inherits PRISM.FileProcessor.ProcessFilesBase

    Public Sub New()
        InitializeVariables()
    End Sub

#Region "Classwide variables"
    Private mProteinCoverageSummarizer As clsProteinCoverageSummarizer

    Private mStatusMessage As String

#End Region

#Region "Properties"

    Public Property CallingAppHandlesEvents As Boolean

    Public Property IgnoreILDifferences As Boolean
        Get
            Return mProteinCoverageSummarizer.IgnoreILDifferences
        End Get
        Set
            mProteinCoverageSummarizer.IgnoreILDifferences = Value
        End Set
    End Property

    ''' <summary>
    ''' When this is True, the SQLite Database will not be deleted after processing finishes
    ''' </summary>
    Public Property KeepDB As Boolean
        Get
            Return mProteinCoverageSummarizer.KeepDB
        End Get
        Set
            mProteinCoverageSummarizer.KeepDB = Value
        End Set
    End Property

    Public Property MatchPeptidePrefixAndSuffixToProtein As Boolean
        Get
            Return mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein
        End Get
        Set
            mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = Value
        End Set
    End Property

    Public Property OutputProteinSequence As Boolean
        Get
            Return mProteinCoverageSummarizer.OutputProteinSequence
        End Get
        Set
            mProteinCoverageSummarizer.OutputProteinSequence = Value
        End Set
    End Property

    Public Property PeptideFileFormatCode As clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode
        Get
            Return mProteinCoverageSummarizer.PeptideFileFormatCode
        End Get
        Set
            mProteinCoverageSummarizer.PeptideFileFormatCode = Value
        End Set
    End Property

    Public Property PeptideFileSkipFirstLine As Boolean
        Get
            Return mProteinCoverageSummarizer.PeptideFileSkipFirstLine
        End Get
        Set
            mProteinCoverageSummarizer.PeptideFileSkipFirstLine = Value
        End Set
    End Property

    Public Property PeptideInputFileDelimiter As Char
        Get
            Return mProteinCoverageSummarizer.PeptideInputFileDelimiter
        End Get
        Set
            mProteinCoverageSummarizer.PeptideInputFileDelimiter = Value
        End Set
    End Property

    Public Property ProteinDataDelimitedFileDelimiter As Char
        Get
            Return mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileDelimiter
        End Get
        Set
            mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileDelimiter = Value
        End Set
    End Property

    Public Property ProteinDataDelimitedFileFormatCode As DelimitedFileReader.eDelimitedFileFormatCode
        Get
            Return mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileFormatCode
        End Get
        Set
            mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileFormatCode = Value
        End Set
    End Property

    Public Property ProteinDataDelimitedFileSkipFirstLine As Boolean
        Get
            Return mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileSkipFirstLine
        End Get
        Set
            mProteinCoverageSummarizer.ProteinDataCache.DelimitedFileSkipFirstLine = Value
        End Set
    End Property

    Public Property ProteinDataRemoveSymbolCharacters As Boolean
        Get
            Return mProteinCoverageSummarizer.ProteinDataCache.RemoveSymbolCharacters
        End Get
        Set
            mProteinCoverageSummarizer.ProteinDataCache.RemoveSymbolCharacters = Value
        End Set
    End Property

    Public Property ProteinDataIgnoreILDifferences As Boolean
        Get
            Return mProteinCoverageSummarizer.ProteinDataCache.IgnoreILDifferences
        End Get
        Set
            mProteinCoverageSummarizer.ProteinDataCache.IgnoreILDifferences = Value
        End Set
    End Property

    Public Property ProteinInputFilePath As String
        Get
            Return mProteinCoverageSummarizer.ProteinInputFilePath
        End Get
        Set
            mProteinCoverageSummarizer.ProteinInputFilePath = Value
        End Set
    End Property

    Public ReadOnly Property ProteinToPeptideMappingFilePath As String
        Get
            Return mProteinCoverageSummarizer.ProteinToPeptideMappingFilePath
        End Get
    End Property

    Public Property RemoveSymbolCharacters As Boolean
        Get
            Return mProteinCoverageSummarizer.RemoveSymbolCharacters
        End Get
        Set
            mProteinCoverageSummarizer.RemoveSymbolCharacters = Value
        End Set
    End Property

    Public ReadOnly Property ResultsFilePath As String
        Get
            Return mProteinCoverageSummarizer.ResultsFilePath
        End Get
    End Property

    Public Property SaveProteinToPeptideMappingFile As Boolean
        Get
            Return mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile
        End Get
        Set
            mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile = Value
        End Set
    End Property

    Public Property SearchAllProteinsForPeptideSequence As Boolean
        Get
            Return mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence
        End Get
        Set
            mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence = Value
        End Set
    End Property

    Public Property UseLeaderSequenceHashTable As Boolean
        Get
            Return mProteinCoverageSummarizer.UseLeaderSequenceHashTable
        End Get
        Set
            mProteinCoverageSummarizer.UseLeaderSequenceHashTable = Value
        End Set
    End Property

    Public Property SearchAllProteinsSkipCoverageComputationSteps As Boolean
        Get
            Return mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps
        End Get
        Set
            mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps = Value
        End Set
    End Property

    Public ReadOnly Property StatusMessage As String
        Get
            Return mStatusMessage
        End Get
    End Property

    Public Property TrackPeptideCounts As Boolean
        Get
            Return mProteinCoverageSummarizer.TrackPeptideCounts
        End Get
        Set
            mProteinCoverageSummarizer.TrackPeptideCounts = Value
        End Set
    End Property

#End Region

    Public Overrides Sub AbortProcessingNow()
        MyBase.AbortProcessingNow()
        If Not mProteinCoverageSummarizer Is Nothing Then
            mProteinCoverageSummarizer.AbortProcessingNow()
        End If
    End Sub

    Public Overrides Function GetErrorMessage() As String
        Return MyBase.GetBaseClassErrorMessage
    End Function

    Private Sub InitializeVariables()
        Me.CallingAppHandlesEvents = False

        AbortProcessing = False
        mStatusMessage = String.Empty

        mProteinCoverageSummarizer = New clsProteinCoverageSummarizer()
        RegisterEvents(mProteinCoverageSummarizer)

        AddHandler mProteinCoverageSummarizer.ProgressChanged, AddressOf ProteinCoverageSummarizer_ProgressChanged

        AddHandler mProteinCoverageSummarizer.ProgressReset, AddressOf ProteinCoverageSummarizer_ProgressReset

    End Sub

    Public Function LoadParameterFileSettings(strParameterFilePath As String) As Boolean
        Return mProteinCoverageSummarizer.LoadParameterFileSettings(strParameterFilePath)
    End Function

    Public Overloads Overrides Function ProcessFile(strInputFilePath As String, strOutputFolderPath As String, strParameterFilePath As String, blnResetErrorCode As Boolean) As Boolean

        Dim blnSuccess As Boolean

        If blnResetErrorCode Then
            MyBase.SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError)
        End If

        Try
            ' Show the progress form
            If Not CallingAppHandlesEvents Then
                Console.WriteLine(MyBase.ProgressStepDescription)
            End If

            ' Call mProteinCoverageSummarizer.ProcessFile to perform the work
            mProteinCoverageSummarizer.KeepDB = KeepDB
            blnSuccess = mProteinCoverageSummarizer.ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, True)

            mProteinCoverageSummarizer.ProteinDataCache.DeleteSQLiteDBFile("clsProteinCoverageSummarizerRunner.ProcessFile_Complete")

        Catch ex As Exception
            mStatusMessage = "Error in ProcessFile:" & ControlChars.NewLine & ex.Message
            OnErrorEvent(mStatusMessage, ex)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Sub ProteinCoverageSummarizer_ProgressChanged(taskDescription As String, percentComplete As Single)
        UpdateProgress(taskDescription, percentComplete)

        ''If mUseProgressForm AndAlso Not mProgressForm Is Nothing Then
        ''    mProgressForm.UpdateCurrentTask(taskDescription)
        ''    mProgressForm.UpdateProgressBar(percentComplete)
        ''    Windows.Forms.Application.DoEvents()
        ''End If
    End Sub

    Private Sub ProteinCoverageSummarizer_ProgressReset()
        ResetProgress(mProteinCoverageSummarizer.ProgressStepDescription)

        ''If mUseProgressForm AndAlso Not mProgressForm Is Nothing Then
        ''    mProgressForm.UpdateProgressBar(0, True)
        ''    mProgressForm.UpdateCurrentTask(mProteinCoverageSummarizer.ProgressStepDescription)
        ''End If

    End Sub

End Class
