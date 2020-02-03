Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.DAL

<Serializable>
Public Class PetDGandhiUserInputs
    Inherits StrategyUserInputs

    Public Property NumberOfTradePerStock As Integer
    Public Property MaxProfitPerDay As Decimal
    Public Property MaxLossPerDay As Decimal
    Public Property InstrumentDetailsFilePath As String
    Public Property InstrumentsData As Dictionary(Of String, InstrumentDetails)

    Public Property TelegramAPIKey As String
    Public Property TelegramChatID As String
    Public Property TelegramPLChatID As String

    <Serializable>
    Public Class InstrumentDetails
        Public Property TradingSymbol As String
        Public Property Quantity As Integer
        Public Property Direction As IOrder.TypeOfTransaction
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
                        Dim excelColumnList As New List(Of String) From {"INSTRUMENT NAME", "QUANTITY", "DIRECTION"}

                        For colCtr = 0 To 2
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
                            Dim direction As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
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
                                            Math.Round(Val(instrumentDetails(rowCtr, columnCtr)), 2) = Val(instrumentDetails(rowCtr, columnCtr)) Then
                                            quantity = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Quantity cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Quantity cannot be null for {0}", instrumentName))
                                    End If
                                ElseIf columnCtr = 2 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "BUY" Then
                                            direction = IOrder.TypeOfTransaction.Buy
                                        ElseIf instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "SELL" Then
                                            direction = IOrder.TypeOfTransaction.Sell
                                        Else
                                            Throw New ApplicationException(String.Format("Direction cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Buffer cannot be null for {0}", instrumentName))
                                    End If
                                End If
                            Next
                            If instrumentName IsNot Nothing Then
                                Dim instrumentData As New InstrumentDetails
                                instrumentData.TradingSymbol = instrumentName.ToUpper
                                instrumentData.Quantity = quantity
                                instrumentData.Direction = direction
                                If Me.InstrumentsData Is Nothing Then Me.InstrumentsData = New Dictionary(Of String, InstrumentDetails)
                                If Me.InstrumentsData.ContainsKey(instrumentData.TradingSymbol) Then
                                    Throw New ApplicationException(String.Format("Duplicate Instrument Name {0}", instrumentData.TradingSymbol))
                                End If
                                Me.InstrumentsData.Add(instrumentData.TradingSymbol, instrumentData)
                            End If
                        Next
                    Else
                        Throw New ApplicationException("No valid input in the file")
                    End If
                Else
                    Throw New ApplicationException("File Type not supported. Application only support .csv file.")
                End If
            Else
                Throw New ApplicationException("Input File does not exists. Please select valid file")
            End If
        Else
            Throw New ApplicationException("No valid input file path exists")
        End If
    End Sub

End Class
