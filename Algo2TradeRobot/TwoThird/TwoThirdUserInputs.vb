Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.DAL

<Serializable>
Public Class TwoThirdUserInputs
    Inherits StrategyUserInputs

    Public Property ATRPeriod As Integer
    Public Property NumberOfTradePerStock As Integer
    Public Property ReverseTrade As Boolean
    Public Property StoplossMovementToBreakeven As Boolean
    Public Property CountTradesWithBreakevenMovement As Boolean
    Public Property BreakevenPoint As Decimal

    Public Property TelegramAPIKey As String
    Public Property TelegramChatID As String
    Public Property TelegramPLChatID As String
    Public Property InstrumentDetailsFilePath As String
    Public Property InstrumentsData As Dictionary(Of String, InstrumentDetails)

    <Serializable>
    Public Class InstrumentDetails
        Public Property InstrumentName As String
        Public Property Quantity As Integer
        Public Property Capital As Decimal
        Public Property AllowCapitalToIncrease As Boolean
    End Class

    Public Sub FillInstrumentDetails(ByVal filePath As String, ByVal canceller As CancellationTokenSource)
        If filePath IsNot Nothing Then
            If File.Exists(filePath) Then
                Dim extension As String = Path.GetExtension(filePath)
                If extension = ".csv" Then
                    Dim instrumentDetails(,) As Object = Nothing
                    Using csvReader As New CSVHelper(filePath, ",", canceller)
                        instrumentDetails = csvReader.Get2DArrayFromCSV(0)
                    End Using
                    If instrumentDetails IsNot Nothing AndAlso instrumentDetails.Length > 0 Then
                        Dim excelColumnList As New List(Of String) From {"TRADING SYMBOL", "QUANTITY", "CAPITAL", "INCREASE CAPITAL ALLOWED"}
                        For colCtr = 0 To 3
                            If instrumentDetails(0, colCtr) Is Nothing OrElse Trim(instrumentDetails(0, colCtr).ToString) = "" Then
                                Throw New ApplicationException(String.Format("Invalid format."))
                            Else
                                If Not excelColumnList.Contains(Trim(instrumentDetails(0, colCtr).ToString.ToUpper)) Then
                                    Throw New ApplicationException(String.Format("Invalid format or invalid column at ColumnNumber: {0}", colCtr))
                                End If
                            End If
                        Next
                        For rowCtr = 1 To instrumentDetails.GetLength(0) - 1
                            Dim instrumentName As String = Nothing
                            Dim quantity As Integer = Integer.MinValue
                            Dim capital As Decimal = Decimal.MinValue
                            Dim allowCapitalIncrease As Decimal = Decimal.MinValue

                            For columnCtr = 0 To instrumentDetails.GetLength(1)
                                If columnCtr = 0 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        instrumentName = instrumentDetails(rowCtr, columnCtr)
                                    Else
                                        If Not rowCtr = instrumentDetails.GetLength(0) Then
                                            Throw New ApplicationException(String.Format("Instrument Name Missing or Blank Row. RowNumber: {0}", rowCtr))
                                        End If
                                    End If
                                ElseIf columnCtr = 1 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) AndAlso
                                            Math.Round(Val(instrumentDetails(rowCtr, columnCtr)), 0) = Val(instrumentDetails(rowCtr, columnCtr)) Then
                                            quantity = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Quantity cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 2 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            capital = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Capital cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 3 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "TRUE" Then
                                        allowCapitalIncrease = True
                                    End If
                                End If
                            Next
                            If instrumentName IsNot Nothing Then
                                If quantity <> Integer.MinValue OrElse capital <> Decimal.MinValue Then
                                    Dim instrumentData As New InstrumentDetails
                                    With instrumentData
                                        .InstrumentName = instrumentName.ToUpper
                                        .Quantity = quantity
                                        .Capital = capital
                                        .AllowCapitalToIncrease = allowCapitalIncrease
                                    End With
                                    If Me.InstrumentsData Is Nothing Then Me.InstrumentsData = New Dictionary(Of String, InstrumentDetails)
                                    If Me.InstrumentsData.ContainsKey(instrumentData.InstrumentName) Then
                                        Throw New ApplicationException(String.Format("Duplicate Instrument Name {0}", instrumentData.InstrumentName))
                                    End If
                                    Me.InstrumentsData.Add(instrumentData.InstrumentName, instrumentData)
                                Else
                                    Throw New ApplicationException(String.Format("Quantity and Capital both can not be null for {0}", instrumentName))
                                End If
                            End If
                        Next
                    Else
                        Throw New ApplicationException("No valid input in the file")
                    End If
                Else
                    Throw New ApplicationException("File Type not supported. Application only support .csv file.")
                End If
            Else
                Throw New ApplicationException("File does not exists. Please select valid file")
            End If
        Else
            Throw New ApplicationException("No valid file path exists")
        End If
    End Sub
End Class
