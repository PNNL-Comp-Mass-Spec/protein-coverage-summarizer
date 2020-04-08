Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Started September 2008
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
Imports PHRPReader
Imports ProteinCoverageSummarizer
Imports ProteinFileReader

''' <summary>
''' This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
''' an accompanying file with peptide sequences to find the proteins that contain each peptide
''' It will also optionally compute the percent coverage of each of the proteins
''' </summary>
Public Class clsPeptideToProteinMapEngine
    Inherits PRISM.FileProcessor.ProcessFilesBase

    Public Sub New()
        InitializeVariables()
    End Sub

#Region "Constants and Enums"

    Public Const FILENAME_SUFFIX_INSPECT_RESULTS_FILE As String = "_inspect.txt"
    Public Const FILENAME_SUFFIX_MSGFDB_RESULTS_FILE As String = "_msgfdb.txt"
    Public Const FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE As String = "_msgfplus.txt"

    Public Const FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING As String = "_PepToProtMap.txt"

    Protected Const FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES As String = "_peptides"

    ' The following are the initial % complete value displayed during each of these stages
    Protected Const PERCENT_COMPLETE_PREPROCESSING As Single = 0
    Protected Const PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER As Single = 5
    Protected Const PERCENT_COMPLETE_POSTPROCESSING As Single = 95

    Public Enum ePeptideInputFileFormatConstants
        Unknown = -1
        AutoDetermine = 0
        PeptideListFile = 1             ' First column is peptide sequence
        ProteinAndPeptideFile = 2       ' First column is protein name, second column is peptide sequence
        InspectResultsFile = 3          ' Inspect results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
        <Obsolete("Old Name")>
        MSGFDBResultsFile = 4
        MSGFPlusResultsFile = 4         ' MS-GF+ results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
        PHRPFile = 5                    ' Sequest, Inspect, X!Tandem, or MS-GF+ synopsis or first-hits file created by PHRP; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
    End Enum

#End Region

#Region "Structures"

    Protected Structure udtProteinIDMapInfoType
        Public ProteinID As Integer
        Public Peptide As String
        Public ResidueStart As Integer
        Public ResidueEnd As Integer

        ''' <summary>
        ''' Show the peptide sequence
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Return Peptide & ", Protein ID " & ProteinID
        End Function
    End Structure

    Protected Structure udtPepToProteinMappingType
        Public Peptide As String
        Public Protein As String
        Public ResidueStart As Integer
        Public ResidueEnd As Integer

        ''' <summary>
        ''' Show the peptide sequence
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Return Peptide & ", Protein " & Protein
        End Function
    End Structure
#End Region

#Region "Classwide variables"
    Protected WithEvents mProteinCoverageSummarizer As clsProteinCoverageSummarizer

    ' When processing an inspect search result file, if you provide the inspect parameter file name,
    '  then this program will read the parameter file and look for the "mod," lines.  The user-assigned mod
    '  names will be extracted and used when "cleaning" the peptides prior to looking for matching proteins
    Private mInspectParameterFilePath As String

    Private mStatusMessage As String

    ' The following is used when the input file is Sequest, X!Tandem, Inspect, or MS-GF+ results file
    ' Keys are peptide sequences; values are Lists of scan numbers that each peptide was observed in
    ' Keys may have mod symbols in them; those symbols will be removed in PreProcessDataWriteOutPeptides
    Private mUniquePeptideList As SortedList(Of String, SortedSet(Of Integer))

    ' Mod names must be lower case, and 4 characters in length (or shorter)
    ' Only used with Inspect since mods in MS-GF+ are simply numbers, e.g. R.DNFM+15.995SATQAVEYGLVDAVM+15.995TK.R
    '  while mods in Sequest and XTandem are symbols (*, #, @)
    Private mInspectModNameList As List(Of String)

#End Region

#Region "Properties"

    ' ReSharper disable UnusedMember.Global

    ''' <summary>
    ''' Legacy property; superseded by DeleteTempFiles
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property DeleteInspectTempFiles As Boolean
        Get
            Return Me.DeleteTempFiles
        End Get
        Set
            Me.DeleteTempFiles = Value
        End Set
    End Property

    Public Property DeleteTempFiles As Boolean

    Public Property IgnoreILDifferences As Boolean
        Get
            Return mProteinCoverageSummarizer.IgnoreILDifferences
        End Get
        Set
            mProteinCoverageSummarizer.IgnoreILDifferences = Value
        End Set
    End Property

    Public Property InspectParameterFilePath As String
        Get
            Return mInspectParameterFilePath
        End Get
        Set
            If Value Is Nothing Then Value = String.Empty
            mInspectParameterFilePath = Value
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

    Public Property PeptideInputFileFormat As ePeptideInputFileFormatConstants

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
            If Value Is Nothing Then Value = String.Empty
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

    Public Property SaveSourceDataPlusProteinsFile As Boolean
        Get
            Return mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile
        End Get
        Set
            mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile = Value
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

    ' ReSharper restore UnusedMember.Global

#End Region

    Public Overrides Sub AbortProcessingNow()
        MyBase.AbortProcessingNow()
        If Not mProteinCoverageSummarizer Is Nothing Then
            mProteinCoverageSummarizer.AbortProcessingNow()
        End If
    End Sub

    Public Function DetermineResultsFileFormat(filePath As String) As ePeptideInputFileFormatConstants
        ' Examine the filePath to determine the file format

        If Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_INSPECT_RESULTS_FILE.ToLower()) Then
            Return ePeptideInputFileFormatConstants.InspectResultsFile

        ElseIf Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_MSGFDB_RESULTS_FILE.ToLower()) Then
            Return ePeptideInputFileFormatConstants.MSGFPlusResultsFile

        ElseIf Path.GetFileName(filePath).ToLower().EndsWith(FILENAME_SUFFIX_MSGFPLUS_RESULTS_FILE.ToLower()) Then
            Return ePeptideInputFileFormatConstants.MSGFPlusResultsFile

        ElseIf PeptideInputFileFormat <> ePeptideInputFileFormatConstants.AutoDetermine And PeptideInputFileFormat <> ePeptideInputFileFormatConstants.Unknown Then
            Return PeptideInputFileFormat
        End If

        Dim baseNameLCase As String = Path.GetFileNameWithoutExtension(filePath)
        If baseNameLCase.EndsWith("_MSGFDB", StringComparison.OrdinalIgnoreCase) OrElse
           baseNameLCase.EndsWith("_MSGFPlus", StringComparison.OrdinalIgnoreCase) Then
            Return ePeptideInputFileFormatConstants.MSGFPlusResultsFile
        End If

        Dim eResultType = clsPHRPReader.AutoDetermineResultType(filePath)
        If eResultType <> clsPHRPReader.ePeptideHitResultType.Unknown Then
            Return ePeptideInputFileFormatConstants.PHRPFile
        End If

        ShowMessage("Unable to determine the format of the input file based on the filename suffix; will assume the first column contains peptide sequence")
        Return ePeptideInputFileFormatConstants.PeptideListFile

    End Function

    Public Function ExtractModInfoFromInspectParamFile(inspectParamFilePath As String, ByRef inspectModNames As List(Of String)) As Boolean

        Try

            If inspectModNames Is Nothing Then
                inspectModNames = New List(Of String)
            Else
                inspectModNames.Clear()
            End If

            If inspectParamFilePath Is Nothing OrElse inspectParamFilePath.Length = 0 Then
                Return False
            End If

            ShowMessage("Looking for mod definitions in the Inspect param file: " & Path.GetFileName(inspectParamFilePath))

            ' Read the contents of inspectParamFilePath
            Using reader = New StreamReader(New FileStream(inspectParamFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                Do While Not reader.EndOfStream
                    Dim lineIn = reader.ReadLine()
                    If lineIn Is Nothing Then Continue Do

                    lineIn = lineIn.TrimEnd()

                    If lineIn.Length > 0 Then

                        If lineIn.Chars(0) = "#"c Then
                            ' Comment line; skip it
                        ElseIf lineIn.ToLower().StartsWith("mod") Then
                            ' Modification definition line

                            ' Split the line on commas
                            Dim splitLine = lineIn.Split(","c)

                            If splitLine.Length >= 5 AndAlso splitLine(0).ToLower().Trim() = "mod" Then

                                Dim modName As String
                                modName = splitLine(4).ToLower()

                                If modName.Length > 4 Then
                                    ' Only keep the first 4 characters of the modification name
                                    modName = modName.Substring(0, 4)
                                End If

                                inspectModNames.Add(modName)
                                ShowMessage("Found modification: " & lineIn & "   -->   Mod Symbol """ & modName & """")

                            End If
                        End If
                    End If

                Loop

            End Using

            Console.WriteLine()

            Return True

        Catch ex As Exception
            mStatusMessage = "Error reading the Inspect parameter file (" & Path.GetFileName(inspectParamFilePath) & ")"
            HandleException(mStatusMessage, ex)
        End Try

        Return False

    End Function

    Public Overrides Function GetErrorMessage() As String
        Return MyBase.GetBaseClassErrorMessage
    End Function

    Private Sub InitializeVariables()

        PeptideInputFileFormat = ePeptideInputFileFormatConstants.AutoDetermine
        DeleteTempFiles = True

        mInspectParameterFilePath = String.Empty

        AbortProcessing = False
        mStatusMessage = String.Empty

        mProteinCoverageSummarizer = New clsProteinCoverageSummarizer()
        RegisterEvents(mProteinCoverageSummarizer)

        mInspectModNameList = New List(Of String)

        mUniquePeptideList = New SortedList(Of String, SortedSet(Of Integer))
    End Sub

    ''' <summary>
    ''' Open the file and read the first line
    ''' Examine it to determine if it looks like a header line
    ''' </summary>
    ''' <returns></returns>
    Private Function IsHeaderLinePresent(filePath As String, eInputFileFormat As ePeptideInputFileFormatConstants) As Boolean

        Dim sepChars = New Char() {ControlChars.Tab}

        Try
            Dim headerFound = False

            ' Read the contents of filePath
            Using reader = New StreamReader(New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                If Not reader.EndOfStream Then
                    Dim dataLine = reader.ReadLine()

                    If Not String.IsNullOrEmpty(dataLine) Then
                        Dim dataCols = dataLine.Split(sepChars)

                        If eInputFileFormat = ePeptideInputFileFormatConstants.ProteinAndPeptideFile Then
                            If dataCols.Length > 1 AndAlso dataCols(1).StartsWith("peptide", StringComparison.InvariantCultureIgnoreCase) Then
                                headerFound = True
                            End If
                        ElseIf eInputFileFormat = ePeptideInputFileFormatConstants.PeptideListFile Then
                            If dataCols(0).StartsWith("peptide", StringComparison.InvariantCultureIgnoreCase) Then
                                headerFound = True
                            End If
                        Else
                            If dataCols.Any(Function(dataColumn) dataColumn.ToLower().StartsWith("peptide")) Then
                                headerFound = True
                            End If
                        End If
                    End If
                End If

            End Using

            Return headerFound

        Catch ex As Exception
            mStatusMessage = "Error looking for a header line in " & Path.GetFileName(filePath)
            HandleException(mStatusMessage, ex)
            Return False
        End Try

    End Function

    ' ReSharper disable once UnusedMember.Global
    Public Function LoadParameterFileSettings(parameterFilePath As String) As Boolean
        Return mProteinCoverageSummarizer.LoadParameterFileSettings(parameterFilePath)
    End Function

    Protected Function PostProcessPSMResultsFile(peptideListFilePath As String,
                                                 proteinToPepMapFilePath As String,
                                                 deleteWorkingFiles As Boolean) As Boolean

        Const UNKNOWN_PROTEIN_NAME = "__NoMatch__"

        Dim proteins() As String
        Dim proteinIDPointerArray() As Integer

        Dim proteinMapInfo() As udtProteinIDMapInfoType

        Try
            Console.WriteLine()

            ShowMessage("Post-processing the results files")

            If mUniquePeptideList Is Nothing OrElse mUniquePeptideList.Count = 0 Then
                mStatusMessage = "Error in PostProcessPSMResultsFile: mUniquePeptideList is empty; this is unexpected; unable to continue"

                HandleException(mStatusMessage, New Exception("Empty Array"))

                Return False
            End If

            ReDim proteins(0)
            ReDim proteinIDPointerArray(0)
            ReDim proteinMapInfo(0)

            PostProcessPSMResultsFileReadMapFile(proteinToPepMapFilePath, proteins, proteinIDPointerArray, proteinMapInfo)

            ' Sort proteinMapInfo on peptide, then on protein
            Array.Sort(proteinMapInfo, New ProteinIDMapInfoComparer)

            Dim peptideToProteinMappingFilePath As String

            ' Create the final result file
            If proteinToPepMapFilePath.Contains(FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES & clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING) Then
                ' This was an old name format that is no longer used
                ' This code block should, therefore, never be reached
                peptideToProteinMappingFilePath = proteinToPepMapFilePath.Replace(
                    FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES & clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                    FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING)
            Else
                peptideToProteinMappingFilePath = proteinToPepMapFilePath.Replace(
                    clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING,
                    FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING)

                If String.Equals(proteinToPepMapFilePath, peptideToProteinMappingFilePath) Then
                    ' The filename was not in the exacted format
                    peptideToProteinMappingFilePath = clsProteinCoverageSummarizer.ConstructOutputFilePath(
                        proteinToPepMapFilePath, FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING,
                        Path.GetDirectoryName(proteinToPepMapFilePath), "")
                End If
            End If

            LogMessage("Creating " & Path.GetFileName(peptideToProteinMappingFilePath))

            Using writer = New StreamWriter(New FileStream(peptideToProteinMappingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

                ' Write the headers
                writer.WriteLine("Peptide" & ControlChars.Tab &
                  "Protein" & ControlChars.Tab &
                  "Residue_Start" & ControlChars.Tab &
                  "Residue_End")

                ' Initialize the Binary Search comparer
                Dim proteinMapPeptideComparer = New ProteinIDMapInfoPeptideSearchComparer()


                ' Assure that proteinIDPointerArray and proteins are sorted in parallel
                Array.Sort(proteinIDPointerArray, proteins)

                ' Initialize cachedData
                Dim cachedData = New List(Of udtPepToProteinMappingType)

                ' Initialize cachedDataComparer
                Dim cachedDataComparer = New PepToProteinMappingComparer()

                For Each peptideEntry In mUniquePeptideList

                    Dim prefixResidue As Char
                    Dim suffixResidue As Char

                    ' Construct the clean sequence for this peptide
                    Dim cleanSequence = clsProteinCoverageSummarizer.GetCleanPeptideSequence(
                       peptideEntry.Key,
                       prefixResidue,
                       suffixResidue,
                       mProteinCoverageSummarizer.RemoveSymbolCharacters)

                    If mInspectModNameList.Count > 0 Then
                        cleanSequence = RemoveInspectMods(cleanSequence, mInspectModNameList)
                    End If

                    ' Look for cleanSequence in proteinMapInfo
                    Dim matchIndex = Array.BinarySearch(proteinMapInfo, cleanSequence, proteinMapPeptideComparer)

                    If matchIndex < 0 Then
                        ' Match not found; this is unexpected
                        ' However, this code will be reached if the peptide is not present in any of the proteins in the protein data file
                        writer.WriteLine(
                            peptideEntry.Key & ControlChars.Tab &
                            UNKNOWN_PROTEIN_NAME & ControlChars.Tab &
                            0.ToString() & ControlChars.Tab &
                            0.ToString())

                    Else
                        ' Decrement matchIndex until the first match in proteinMapInfo is found
                        Do While matchIndex > 0 AndAlso proteinMapInfo(matchIndex - 1).Peptide = cleanSequence
                            matchIndex -= 1
                        Loop

                        ' Now write out each of the proteins for this peptide
                        ' We're caching results to cachedData so that we can sort by protein name
                        cachedData.Clear()

                        Do
                            ' Find the Protein for ID proteinMapInfo(matchIndex).ProteinID
                            Dim proteinIDMatchIndex = Array.BinarySearch(proteinIDPointerArray, proteinMapInfo(matchIndex).ProteinID)
                            Dim protein As String

                            If proteinIDMatchIndex >= 0 Then
                                protein = proteins(proteinIDMatchIndex)
                            Else
                                protein = UNKNOWN_PROTEIN_NAME
                            End If

                            Try
                                If protein <> proteins(proteinMapInfo(matchIndex).ProteinID) Then
                                    ' This is unexpected
                                    ShowMessage("Warning: Unexpected protein ID lookup array mismatch for ID " & proteinMapInfo(matchIndex).ProteinID.ToString)
                                End If
                            Catch ex As Exception
                                ' This code shouldn't be reached
                                ' Ignore errors occur
                            End Try

                            Dim cachedDataEntry = New udtPepToProteinMappingType With {
                                .Peptide = String.Copy(peptideEntry.Key),
                                .Protein = String.Copy(protein),
                                .ResidueStart = proteinMapInfo(matchIndex).ResidueStart,
                                .ResidueEnd = proteinMapInfo(matchIndex).ResidueEnd
                            }

                            cachedData.Add(cachedDataEntry)

                            matchIndex += 1
                        Loop While matchIndex < proteinMapInfo.Length AndAlso proteinMapInfo(matchIndex).Peptide = cleanSequence

                        If cachedData.Count > 1 Then
                            cachedData.Sort(cachedDataComparer)
                        End If

                        For cacheIndex = 0 To cachedData.Count - 1
                            With cachedData(cacheIndex)
                                writer.WriteLine(.Peptide & ControlChars.Tab &
                                  .Protein & ControlChars.Tab &
                                  .ResidueStart.ToString() & ControlChars.Tab &
                                  .ResidueEnd.ToString())

                            End With

                        Next
                    End If

                Next

            End Using

            If deleteWorkingFiles Then
                Try
                    LogMessage("Deleting " & Path.GetFileName(peptideListFilePath))
                    File.Delete(peptideListFilePath)
                Catch ex As Exception
                End Try

                Try
                    LogMessage("Deleting " & Path.GetFileName(proteinToPepMapFilePath))
                    File.Delete(proteinToPepMapFilePath)
                Catch ex As Exception
                End Try

            End If

            Return True

        Catch ex As Exception
            mStatusMessage = "Error writing the Inspect or MS-GF+ peptide to protein map file in PostProcessPSMResultsFile"
            HandleException(mStatusMessage, ex)
        End Try

        Return False

    End Function

    Protected Function PostProcessPSMResultsFileReadMapFile(proteinToPepMapFilePath As String,
      ByRef proteins() As String,
      ByRef proteinIDPointerArray() As Integer,
      ByRef proteinMapInfo() As udtProteinIDMapInfoType) As Boolean

        Dim terminatorSize = 2

        Try

            ' Initialize the protein list dictionary
            Dim proteinList = New Dictionary(Of String, Integer)

            Dim proteinMapInfoCount = 0

            ' Initialize the protein to peptide mapping array
            ' We know the length will be at least as long as mUniquePeptideList, and easily twice that length
            ReDim proteinMapInfo(mUniquePeptideList.Count * 2 - 1)

            LogMessage("Reading " & Path.GetFileName(proteinToPepMapFilePath))

            ' Read the contents of proteinToPepMapFilePath
            Using reader = New StreamReader(New FileStream(proteinToPepMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                Dim currentProtein = String.Empty

                Dim currentLine = 0
                Dim bytesRead As Long = 0

                Dim currentProteinID = 0

                Do While Not reader.EndOfStream
                    currentLine += 1

                    If AbortProcessing Then Exit Do

                    Dim lineIn = reader.ReadLine()
                    If lineIn Is Nothing Then Continue Do

                    bytesRead += lineIn.Length + terminatorSize

                    lineIn = lineIn.TrimEnd()

                    If currentLine = 1 Then
                        ' Header line; skip it
                        Continue Do
                    End If

                    If lineIn.Length = 0 Then
                        Continue Do
                    End If

                    ' Split the line
                    Dim splitLine = lineIn.Split(ControlChars.Tab)

                    If splitLine.Length < 4 Then
                        Continue Do
                    End If

                    If proteinMapInfoCount >= proteinMapInfo.Length Then
                        ReDim Preserve proteinMapInfo(proteinMapInfo.Length * 2 - 1)
                    End If

                    If currentProtein.Length = 0 OrElse currentProtein <> splitLine(0) Then
                        ' Determine the Protein ID for this protein

                        currentProtein = splitLine(0)

                        If Not proteinList.TryGetValue(currentProtein, currentProteinID) Then
                            ' New protein; add it, assigning it index proteinList.Count
                            currentProteinID = proteinList.Count
                            proteinList.Add(currentProtein, currentProteinID)
                        End If

                    End If

                    With proteinMapInfo(proteinMapInfoCount)
                        .ProteinID = currentProteinID
                        .Peptide = splitLine(1)
                        .ResidueStart = Integer.Parse(splitLine(2))
                        .ResidueEnd = Integer.Parse(splitLine(3))
                    End With

                    proteinMapInfoCount += 1

                    If currentLine Mod 1000 = 0 Then
                        UpdateProgress(PERCENT_COMPLETE_POSTPROCESSING +
                           CSng((bytesRead / reader.BaseStream.Length) * 100) * (PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100)
                    End If

                Loop

            End Using

            ' Populate proteins() and proteinIDPointerArray() using proteinList
            ReDim proteins(proteinList.Count - 1)
            ReDim proteinIDPointerArray(proteinList.Count - 1)

            ' Note: the Keys and Values are not necessarily sorted, but will be copied in the identical order
            proteinList.Keys.CopyTo(proteins, 0)
            proteinList.Values.CopyTo(proteinIDPointerArray, 0)

            ' Shrink proteinMapInfo to the appropriate length
            ReDim Preserve proteinMapInfo(proteinMapInfoCount - 1)

            Return True

        Catch ex As Exception
            mStatusMessage = "Error reading the newly created protein to peptide mapping file (" & Path.GetFileName(proteinToPepMapFilePath) & ")"
            HandleException(mStatusMessage, ex)
        End Try

        Return False

    End Function

    Protected Function PreProcessInspectResultsFile(inputFilePath As String,
       outputDirectoryPath As String,
       inspectParamFilePath As String) As String

        ' Read inspectParamFilePath to extract the mod names
        If Not ExtractModInfoFromInspectParamFile(inspectParamFilePath, mInspectModNameList) Then
            If mInspectModNameList.Count = 0 Then
                mInspectModNameList.Add("phos")
            End If
        End If

        Return PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, ePeptideInputFileFormatConstants.InspectResultsFile)

    End Function

    Protected Function PreProcessPSMResultsFile(inputFilePath As String,
                                                outputDirectoryPath As String,
                                                eFileType As ePeptideInputFileFormatConstants) As String

        Dim terminatorSize As Integer

        Dim sepChars = New Char() {ControlChars.Tab}

        Dim peptideSequenceColIndex As Integer
        Dim scanColIndex As Integer
        Dim toolDescription As String

        If eFileType = ePeptideInputFileFormatConstants.InspectResultsFile Then
            ' Assume inspect results file line terminators are only a single byte (it doesn't matter if the terminators are actually two bytes)
            terminatorSize = 1

            ' The 3rd column in the Inspect results file should have the peptide sequence
            peptideSequenceColIndex = 2
            scanColIndex = 1
            toolDescription = "Inspect"

        ElseIf eFileType = ePeptideInputFileFormatConstants.MSGFPlusResultsFile Then
            terminatorSize = 2
            peptideSequenceColIndex = -1
            scanColIndex = -1
            toolDescription = "MS-GF+"

        Else
            mStatusMessage = "Unrecognized file type: " & eFileType.ToString() & "; will look for column header 'Peptide'"

            terminatorSize = 2
            peptideSequenceColIndex = -1
            scanColIndex = -1
            toolDescription = "Generic PSM result file"
        End If

        Try
            If Not File.Exists(inputFilePath) Then
                SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath)
                mStatusMessage = "File not found: " & inputFilePath

                ShowErrorMessage(mStatusMessage)
                Exit Try

            End If

            ShowMessage("Pre-processing the " & toolDescription & " results file: " & Path.GetFileName(inputFilePath))

            ' Initialize the peptide list
            If mUniquePeptideList Is Nothing Then
                mUniquePeptideList = New SortedList(Of String, SortedSet(Of Integer))
            Else
                mUniquePeptideList.Clear()
            End If

            ' Open the PSM results file and construct a unique list of peptides in the file (including any modification symbols)
            ' Keep track of PSM counts
            Using reader = New StreamReader(New FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))

                Dim currentLine = 1
                Dim bytesRead As Long = 0

                Do While Not reader.EndOfStream
                    If AbortProcessing Then Exit Do

                    Dim lineIn = reader.ReadLine()
                    If lineIn Is Nothing Then Continue Do

                    bytesRead += lineIn.Length + terminatorSize

                    lineIn = lineIn.TrimEnd()

                    If currentLine = 1 AndAlso (peptideSequenceColIndex < 0 OrElse lineIn.StartsWith("#")) Then

                        ' Header line
                        If peptideSequenceColIndex < 0 Then
                            ' Split the header line to look for the "Peptide" and Scan columns
                            Dim splitLine = lineIn.Split(sepChars)
                            For index = 0 To splitLine.Length - 1
                                If peptideSequenceColIndex < 0 AndAlso splitLine(index).ToLower() = "peptide" Then
                                    peptideSequenceColIndex = index
                                End If

                                If scanColIndex < 0 AndAlso splitLine(index).ToLower().StartsWith("scan") Then
                                    scanColIndex = index
                                End If
                            Next

                            If peptideSequenceColIndex < 0 Then
                                SetBaseClassErrorCode(ProcessFilesErrorCodes.LocalizedError)
                                mStatusMessage = "Peptide column not found; unable to continue"

                                ShowErrorMessage(mStatusMessage)
                                Return String.Empty

                            End If

                        End If

                    ElseIf lineIn.Length > 0 Then

                        Dim splitLine = lineIn.Split(sepChars)

                        If splitLine.Length > peptideSequenceColIndex Then
                            Dim scanNumber As Integer
                            If scanColIndex >= 0 Then
                                If Not Integer.TryParse(splitLine(scanColIndex), scanNumber) Then
                                    scanNumber = 0
                                End If
                            End If

                            UpdateUniquePeptideList(splitLine(peptideSequenceColIndex), scanNumber)
                        End If

                    End If

                    If currentLine Mod 1000 = 0 Then
                        UpdateProgress(PERCENT_COMPLETE_PREPROCESSING +
                           CSng((bytesRead / reader.BaseStream.Length) * 100) * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100)
                    End If

                    currentLine += 1

                Loop

            End Using

            Dim peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath)
            Return peptideListFilePath

        Catch ex As Exception
            mStatusMessage = "Error reading " & toolDescription & " input file in PreProcessPSMResultsFile"
            HandleException(mStatusMessage, ex)
        End Try

        Return String.Empty

    End Function

    Protected Function PreProcessPHRPDataFile(inputFilePath As String, outputDirectoryPath As String) As String

        Try
            If Not File.Exists(inputFilePath) Then
                SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath)
                mStatusMessage = "File not found: " & inputFilePath

                ShowErrorMessage(mStatusMessage)
                Exit Try

            End If

            Console.WriteLine()
            ShowMessage("Pre-processing PHRP data file: " & Path.GetFileName(inputFilePath))

            ' Initialize the peptide list
            If mUniquePeptideList Is Nothing Then
                mUniquePeptideList = New SortedList(Of String, SortedSet(Of Integer))
            Else
                mUniquePeptideList.Clear()
            End If

            ' Initialize the PHRP startup options
            Dim startupOptions = New clsPHRPStartupOptions() With {
                .LoadModsAndSeqInfo = False,
                .LoadMSGFResults = False,
                .LoadScanStatsData = False,
                .MaxProteinsPerPSM = 1
            }

            ' Open the PHRP data file and construct a unique list of peptides in the file (including any modification symbols).
            ' MSPathFinder synopsis files do not have mod symbols in the peptides.
            ' This is OK since the peptides in mUniquePeptideList will have mod symbols removed in PreProcessDataWriteOutPeptides
            ' when finding proteins that contain the peptides.
            Using reader As New clsPHRPReader(inputFilePath, clsPHRPReader.ePeptideHitResultType.Unknown, startupOptions)
                reader.EchoMessagesToConsole = True
                reader.SkipDuplicatePSMs = False

                For Each errorMessage As String In reader.ErrorMessages
                    ShowErrorMessage(errorMessage)
                Next

                For Each warningMessage As String In reader.WarningMessages
                    ShowMessage("Warning: " & warningMessage)
                Next

                reader.ClearErrors()
                reader.ClearWarnings()

                RegisterEvents(reader)

                Do While reader.MoveNext()
                    If AbortProcessing Then Exit Do

                    UpdateUniquePeptideList(reader.CurrentPSM.Peptide, reader.CurrentPSM.ScanNumber)

                    If mUniquePeptideList.Count Mod 1000 = 0 Then
                        UpdateProgress(PERCENT_COMPLETE_PREPROCESSING +
                           reader.PercentComplete * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100)
                    End If
                Loop
            End Using

            Dim peptideListFilePath = PreProcessDataWriteOutPeptides(inputFilePath, outputDirectoryPath)
            Return peptideListFilePath

        Catch ex As Exception
            mStatusMessage = "Error reading PSM input file in PreProcessPHRPDataFile"
            HandleException(mStatusMessage, ex)
        End Try

        Return String.Empty

    End Function

    Protected Function PreProcessDataWriteOutPeptides(inputFilePath As String, outputDirectoryPath As String) As String

        Try

            ' Now write out the unique list of peptides to peptideListFilePath
            Dim peptideListFileName = Path.GetFileNameWithoutExtension(inputFilePath) & FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES & ".txt"
            Dim peptideListFilePath As String

            If Not String.IsNullOrEmpty(outputDirectoryPath) Then
                peptideListFilePath = Path.Combine(outputDirectoryPath, peptideListFileName)
            Else
                Dim inputFileInfo = New FileInfo(inputFilePath)

                peptideListFilePath = Path.Combine(inputFileInfo.DirectoryName, peptideListFileName)
            End If

            LogMessage("Creating " & Path.GetFileName(peptideListFileName))

            ' Open the output file
            Using writer = New StreamWriter(New FileStream(peptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

                ' Write out the peptides, removing any mod symbols that might be present
                For Each peptideEntry In mUniquePeptideList
                    Dim peptide As String

                    If mInspectModNameList.Count > 0 Then
                        peptide = RemoveInspectMods(peptideEntry.Key, mInspectModNameList)
                    Else
                        peptide = peptideEntry.Key
                    End If

                    If peptideEntry.Value.Count = 0 Then
                        writer.WriteLine(peptide & ControlChars.Tab & "0")
                    Else
                        For Each scanNumber In peptideEntry.Value
                            writer.WriteLine(peptide & ControlChars.Tab & scanNumber)
                        Next
                    End If

                Next
            End Using

            Return peptideListFilePath

        Catch ex As Exception
            mStatusMessage = "Error writing the Unique Peptides file in PreProcessDataWriteOutPeptides"
            HandleException(mStatusMessage, ex)
            Return String.Empty
        End Try

    End Function

    Public Overloads Overrides Function ProcessFile(inputFilePath As String, outputDirectoryPath As String, parameterFilePath As String, resetErrorCode As Boolean) As Boolean

        If resetErrorCode Then
            MyBase.SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError)
        End If

        Try
            If inputFilePath Is Nothing OrElse inputFilePath.Length = 0 Then
                ShowMessage("Input file name is empty")
                MyBase.SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath)
                Return False
            End If
            Dim success = False

            ' Note that CleanupFilePaths() will update mOutputDirectoryPath, which is used by LogMessage()
            If Not CleanupFilePaths(inputFilePath, outputDirectoryPath) Then
                MyBase.SetBaseClassErrorCode(ProcessFilesErrorCodes.FilePathError)
            Else

                LogMessage("Processing " & Path.GetFileName(inputFilePath))
                Dim eInputFileFormat As ePeptideInputFileFormatConstants

                If PeptideInputFileFormat = ePeptideInputFileFormatConstants.AutoDetermine Then
                    eInputFileFormat = DetermineResultsFileFormat(inputFilePath)
                Else
                    eInputFileFormat = PeptideInputFileFormat
                End If

                If eInputFileFormat = ePeptideInputFileFormatConstants.Unknown Then
                    ShowMessage("Input file type not recognized")
                    Return False
                End If

                UpdateProgress("Preprocessing input file", PERCENT_COMPLETE_PREPROCESSING)
                mInspectModNameList.Clear()

                Dim inputFilePathWork As String
                Dim outputFileBaseName As String

                Select Case eInputFileFormat
                    Case ePeptideInputFileFormatConstants.InspectResultsFile
                        ' Inspect search results file; need to pre-process it
                        inputFilePathWork = PreProcessInspectResultsFile(inputFilePath, outputDirectoryPath, mInspectParameterFilePath)
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath)

                        mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
                        mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
                        mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

                    Case ePeptideInputFileFormatConstants.MSGFPlusResultsFile
                        ' MS-GF+ search results file; need to pre-process it
                        ' Make sure RemoveSymbolCharacters is true
                        Me.RemoveSymbolCharacters = True

                        inputFilePathWork = PreProcessPSMResultsFile(inputFilePath, outputDirectoryPath, eInputFileFormat)
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath)

                        mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
                        mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
                        mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

                    Case ePeptideInputFileFormatConstants.PHRPFile
                        ' Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to pre-process it
                        ' Make sure RemoveSymbolCharacters is true
                        Me.RemoveSymbolCharacters = True

                        ' Open the PHRP data files and construct a unique list of peptides in the file (including any modification symbols)
                        ' Write the unique peptide list to _syn_peptides.txt
                        inputFilePathWork = PreProcessPHRPDataFile(inputFilePath, outputDirectoryPath)
                        outputFileBaseName = Path.GetFileNameWithoutExtension(inputFilePath)

                        mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
                        mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
                        mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

                    Case Else
                        ' Pre-process the file to check for a header line
                        inputFilePathWork = String.Copy(inputFilePath)
                        outputFileBaseName = String.Empty

                        If eInputFileFormat = ePeptideInputFileFormatConstants.ProteinAndPeptideFile Then
                            mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
                        Else
                            mProteinCoverageSummarizer.PeptideFileFormatCode = clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
                        End If

                        If IsHeaderLinePresent(inputFilePath, eInputFileFormat) Then
                            mProteinCoverageSummarizer.PeptideFileSkipFirstLine = True
                        End If

                End Select

                If String.IsNullOrWhiteSpace(inputFilePathWork) Then
                    Return False
                End If

                UpdateProgress("Running protein coverage summarizer", PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER)

                Dim proteinToPepMapFilePath As String = Nothing

                ' Call mProteinCoverageSummarizer.ProcessFile to perform the work
                success = mProteinCoverageSummarizer.ProcessFile(inputFilePathWork, outputDirectoryPath,
                                                                 parameterFilePath, True,
                                                                 proteinToPepMapFilePath, outputFileBaseName)
                If Not success Then
                    mStatusMessage = "Error running ProteinCoverageSummarizer: " & mProteinCoverageSummarizer.ErrorMessage
                End If

                If success AndAlso proteinToPepMapFilePath.Length > 0 Then
                    UpdateProgress("Postprocessing", PERCENT_COMPLETE_POSTPROCESSING)

                    Select Case eInputFileFormat
                        Case ePeptideInputFileFormatConstants.PeptideListFile, ePeptideInputFileFormatConstants.ProteinAndPeptideFile
                            ' No post-processing is required

                        Case Else
                            ' Sequest, X!Tandem, Inspect, or MS-GF+ PHRP data file; need to post-process the results file
                            success = PostProcessPSMResultsFile(inputFilePathWork, proteinToPepMapFilePath, DeleteTempFiles)

                    End Select
                End If

                If success Then
                    LogMessage("Processing successful")
                    OperationComplete()
                Else
                    LogMessage("Processing not successful")
                End If
            End If

            Return success

        Catch ex As Exception
            HandleException("Error in ProcessFile", ex)
            Return False
        End Try

    End Function

    Protected Function RemoveInspectMods(peptide As String, ByRef inspectModNames As List(Of String)) As String

        Dim prefix As String = String.Empty
        Dim suffix As String = String.Empty

        If peptide.Length >= 4 Then
            If peptide.Chars(1) = "."c AndAlso
               peptide.Chars(peptide.Length - 2) = "."c Then
                prefix = peptide.Substring(0, 2)
                suffix = peptide.Substring(peptide.Length - 2, 2)

                peptide = peptide.Substring(2, peptide.Length - 4)
            End If
        End If

        For Each modName As String In inspectModNames
            peptide = peptide.Replace(modName, String.Empty)
        Next

        Return prefix & peptide & suffix

    End Function

    ''' <summary>
    ''' Add peptideSequence to mUniquePeptideList if not defined, including tracking the scanNumber
    ''' Otherwise, update the scan list for the peptide
    ''' </summary>
    ''' <param name="peptideSequence"></param>
    ''' <param name="scanNumber"></param>
    Private Sub UpdateUniquePeptideList(peptideSequence As String, scanNumber As Integer)

        Dim scanList As SortedSet(Of Integer) = Nothing
        If mUniquePeptideList.TryGetValue(peptideSequence, scanList) Then
            If Not scanList.Contains(scanNumber) Then
                scanList.Add(scanNumber)
            End If
        Else
            scanList = New SortedSet(Of Integer) From {
                scanNumber
            }
            mUniquePeptideList.Add(peptideSequence, scanList)
        End If

    End Sub

#Region "Protein Coverage Summarizer Event Handlers"
    Private Sub ProteinCoverageSummarizer_ProgressChanged(taskDescription As String, percentComplete As Single) Handles mProteinCoverageSummarizer.ProgressChanged

        Dim percentCompleteEffective As Single =
           PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER +
           percentComplete * CSng((PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100.0)

        UpdateProgress(taskDescription, percentCompleteEffective)
    End Sub

    Private Sub ProteinCoverageSummarizer_ProgressReset() Handles mProteinCoverageSummarizer.ProgressReset
        ResetProgress(mProteinCoverageSummarizer.ProgressStepDescription)
    End Sub
#End Region

#Region "IComparer Classes"
    Protected Class ProteinIDMapInfoComparer
        Implements IComparer

        Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
            Dim xData = CType(x, udtProteinIDMapInfoType)
            Dim yData = CType(y, udtProteinIDMapInfoType)

            If xData.Peptide > yData.Peptide Then
                Return 1
            ElseIf xData.Peptide < yData.Peptide Then
                Return -1
            Else
                If xData.ProteinID > yData.ProteinID Then
                    Return 1
                ElseIf xData.ProteinID < yData.ProteinID Then
                    Return -1
                Else
                    Return 0
                End If
            End If

        End Function
    End Class

    Protected Class ProteinIDMapInfoPeptideSearchComparer
        Implements IComparer

        Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
            Dim xData = CType(x, udtProteinIDMapInfoType)
            Dim peptide = CType(y, String)

            If xData.Peptide > peptide Then
                Return 1
            ElseIf xData.Peptide < peptide Then
                Return -1
            Else
                Return 0
            End If

        End Function
    End Class

    Protected Class PepToProteinMappingComparer
        Implements IComparer(Of udtPepToProteinMappingType)

        Public Function Compare(x As udtPepToProteinMappingType, y As udtPepToProteinMappingType) As Integer Implements IComparer(Of udtPepToProteinMappingType).Compare

            If x.Peptide > y.Peptide Then
                Return 1
            ElseIf x.Peptide < y.Peptide Then
                Return -1
            Else
                If x.Protein > y.Protein Then
                    Return 1
                ElseIf x.Protein < y.Protein Then
                    Return -1
                Else
                    Return 0
                End If
            End If

        End Function

    End Class
#End Region

End Class
