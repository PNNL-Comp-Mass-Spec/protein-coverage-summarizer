Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started in 2003

' E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
' Website: http://ncrr.pnl.gov/ or http://www.sysbio.org/resources/staff/
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

'
' Last Modified August 12, 2005

Public Class ADONetRoutines

    Public Const DEFAULT_CONNECTION_STRING_NO_PROVIDER As String = "Data Source=pogo;Initial Catalog=MTS_Master;User ID=mtuser;Password=mt4fun"

    Public Shared Function AppendColumnToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, ByVal objColumnType As System.Type, ByVal dblDefaultValue As Double, ByVal blnReadOnly As Boolean, ByVal blnUnique As Boolean) As Boolean
        Dim objNewCol As DataColumn

        Try
            objNewCol = dtDataTable.Columns.Add(strColumnName)
            With objNewCol
                .DataType = objColumnType
                .ColumnName = strColumnName
                .DefaultValue = dblDefaultValue
                .AutoIncrement = False
                .ReadOnly = blnReadOnly
                .Unique = blnUnique
            End With
            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Shared Sub AppendColumnDateToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, ByVal dtDefaultDate As DateTime, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False)

        Dim blnSuccess As Boolean

        blnSuccess = AppendColumnToTable(dtDataTable, strColumnName, System.Type.GetType("System.DateTime"), 0, blnReadOnly, blnUnique)

        If blnSuccess Then
            With dtDataTable.Columns(strColumnName)
                .DefaultValue = dtDefaultDate
            End With
        End If

    End Sub

    Public Shared Function AppendColumnDoubleToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, Optional ByVal dblDefaultValue As Double = 0, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False) As Boolean
        Return AppendColumnToTable(dtDataTable, strColumnName, System.Type.GetType("System.Double"), dblDefaultValue, blnReadOnly, blnUnique)
    End Function

    Public Shared Function AppendColumnSingleToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, Optional ByVal sngDefaultValue As Single = 0, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False) As Boolean
        Return AppendColumnToTable(dtDataTable, strColumnName, System.Type.GetType("System.Double"), sngDefaultValue, blnReadOnly, blnUnique)
    End Function

    Public Shared Sub AppendColumnIntegerToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, Optional ByVal intDefaultValue As Integer = 0, Optional ByVal blnAutoIncrement As Boolean = False, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False)

        Dim blnSuccess As Boolean

        blnSuccess = AppendColumnToTable(dtDataTable, strColumnName, System.Type.GetType("System.Int32"), intDefaultValue, blnReadOnly, blnUnique)

        If blnSuccess And blnAutoIncrement Then
            With dtDataTable.Columns(strColumnName)
                .DefaultValue = Nothing
                .AutoIncrement = True
                .Unique = True
            End With
        End If

    End Sub

    Public Shared Sub AppendColumnLongToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, Optional ByVal lngDefaultValue As Long = 0, Optional ByVal blnAutoIncrement As Boolean = False, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False)

        Dim blnSuccess As Boolean

        blnSuccess = AppendColumnToTable(dtDataTable, strColumnName, System.Type.GetType("System.Int64"), lngDefaultValue, blnReadOnly, blnUnique)

        If blnSuccess And blnAutoIncrement Then
            With dtDataTable.Columns(strColumnName)
                .DefaultValue = Nothing
                .AutoIncrement = True
                .Unique = True
            End With
        End If

    End Sub

    Public Shared Sub AppendColumnStringToTable(ByRef dtDataTable As DataTable, ByVal strColumnName As String, Optional ByVal strDefaultValue As String = Nothing, Optional ByVal blnReadOnly As Boolean = False, Optional ByVal blnUnique As Boolean = False)

        Dim myDataColumn As New DataColumn

        With myDataColumn
            .DataType = System.Type.GetType("System.String")
            .ColumnName = strColumnName
            If Not strDefaultValue Is Nothing Then
                .DefaultValue = strDefaultValue
            End If
            .AutoIncrement = False
            .ReadOnly = blnReadOnly
            .Unique = blnUnique
        End With
        dtDataTable.Columns.Add(myDataColumn)

    End Sub


    Public Shared Sub AppendColumnToTableStyle(ByRef tsTableStyle As System.Windows.Forms.DataGridTableStyle, ByVal strMappingName As String, ByVal strHeaderText As String, Optional ByVal intWidth As Integer = 75, Optional ByVal blnIsReadOnly As Boolean = False, Optional ByVal blnIsDateTime As Boolean = False, Optional ByVal intDecimalPlaces As Integer = -1)
        ' If intDecimalPlaces is >=0, then a format string is constructed to show the specified number of decimal places
        Dim TextCol As New Windows.Forms.DataGridTextBoxColumn
        Dim i As Integer

        With TextCol
            .MappingName = strMappingName
            .HeaderText = strHeaderText
            .Width = intWidth
            .ReadOnly = blnIsReadOnly
            If blnIsDateTime Then
                .Format = "g"
            ElseIf intDecimalPlaces >= 0 Then
                .Format = "0."
                For i = 0 To intDecimalPlaces - 1
                    .Format &= "0"
                Next i
            End If
        End With

        tsTableStyle.GridColumnStyles.Add(TextCol)

    End Sub

    Public Shared Sub AppendBoolColumnToTableStyle(ByRef tsTableStyle As System.Windows.Forms.DataGridTableStyle, ByVal strMappingName As String, ByVal strHeaderText As String, Optional ByVal intWidth As Integer = 75, Optional ByVal blnIsReadOnly As Boolean = False, Optional ByVal blnSourceIsTrueFalse As Boolean = True)
        ' If intDecimalPlaces is >=0, then a format string is constructed to show the specified number of decimal places
        Dim BoolCol As New Windows.Forms.DataGridBoolColumn

        With BoolCol
            .MappingName = strMappingName
            .HeaderText = strHeaderText
            .Width = intWidth
            .ReadOnly = blnIsReadOnly
            If blnSourceIsTrueFalse Then
                .FalseValue = False
                .TrueValue = True
            Else
                .FalseValue = 0
                .TrueValue = 1
            End If
            .AllowNull = False
            .NullValue = Convert.DBNull
        End With

        tsTableStyle.GridColumnStyles.Add(BoolCol)

    End Sub

    Public Shared Function ConstructConnectionStringForSqlClient(ByVal strServerName As String, ByVal strDBName As String, ByVal strUserName As String, ByVal strPassword As String) As String
        Return ConstructConnectionString(strServerName, strDBName, strUserName, strPassword, DEFAULT_CONNECTION_STRING_NO_PROVIDER)
    End Function

    Public Shared Function ConstructConnectionStringForSqlClientIntegratedSecurity(ByVal strServerName As String, ByVal strDBName As String) As String
        Dim strNewConnectionString As String

        strNewConnectionString = ConstructConnectionString(strServerName, strDBName, "dummyuser", "dummypw", DEFAULT_CONNECTION_STRING_NO_PROVIDER)
        strNewConnectionString &= ";Integrated Security=SSPI"

        Return strNewConnectionString

    End Function

    Public Shared Function ConstructConnectionString(ByVal strServerName As String, ByVal strDBName As String, ByVal strUserName As String, ByVal strPassword As String, ByVal strModelConnectionString As String) As String

        ' Typical ADODB connection string format:
        '  Provider=sqloledb;Data Source=pogo;Initial Catalog=MT_Deinococcus_P20;User ID=mtuser;Password=mt4fun

        ' Typical .NET connection string format:
        '  Server=pogo;database=MT_Main;uid=mtuser;Password=mt4fun

        Dim strConnStrParts() As String
        Dim strParameterName As String
        Dim strNewConnStr As String

        Dim intIndex As Integer
        Dim intCharIndex As Integer

        strConnStrParts = strModelConnectionString.Split(";"c)
        strNewConnStr = String.Empty

        For intIndex = 0 To strConnStrParts.Length - 1
            intCharIndex = strConnStrParts(intIndex).IndexOf("=")
            If intCharIndex > 0 Then
                strParameterName = strConnStrParts(intIndex).Substring(0, intCharIndex)
                Select Case strParameterName.ToLower.Trim
                    Case "data source", "server"
                        ' Server name
                        strConnStrParts(intIndex) = strParameterName & "=" & strServerName
                    Case "initial catalog", "database"
                        ' DB name
                        strConnStrParts(intIndex) = strParameterName & "=" & strDBName
                    Case "user id", "uid"
                        strConnStrParts(intIndex) = strParameterName & "=" & strUserName
                    Case "password"
                        strConnStrParts(intIndex) = strParameterName & "=" & strPassword
                    Case Else
                        ' Ignore this entry
                End Select
            End If

            If strNewConnStr.Length > 0 Then
                strNewConnStr &= ";"
            End If
            strNewConnStr &= strConnStrParts(intIndex)

        Next intIndex

        Return strNewConnStr

    End Function

    Public Shared Sub FixDataGridScrollBarBug(ByRef objThisDataGrid As Windows.Forms.DataGrid)

        With objThisDataGrid
            'Resize DataGrid to prevent a bug with unusable scroll bars that occurs when the previous
            'database did not have any quantitations and the current one does.  Without this, the scroll bars
            'appear on screen, but are sometimes grayed out and unusable.
            .Width -= 1
            .Width += 1
            .Height -= 1
            .Height += 1
        End With

    End Sub

    Public Shared Function ConnectionStringRemoveProvider(ByVal strConnectionString As String) As String

        Dim strConnStrParts() As String
        Dim strNewConnStr As String
        Dim intIndex As Integer

        If strConnectionString.ToLower.IndexOf("provider") >= 0 Then
            ' Remove the provider portion from strOleDBConnectionString

            strConnStrParts = strConnectionString.Split(";"c)
            strNewConnStr = String.Empty

            For intIndex = 0 To strConnStrParts.Length - 1
                If strConnStrParts(intIndex).ToLower.StartsWith("provider") Then
                    ' Skip this part
                Else
                    If strNewConnStr.Length > 0 Then
                        strNewConnStr &= ";"
                    End If
                    strNewConnStr &= strConnStrParts(intIndex)
                End If
            Next intIndex
        Else
            strNewConnStr = String.Copy(strConnectionString)
        End If

        Return strNewConnStr

    End Function

End Class
