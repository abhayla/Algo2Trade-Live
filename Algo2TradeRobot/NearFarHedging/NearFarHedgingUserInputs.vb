Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.DAL

<Serializable>
Public Class NearFarHedgingUserInputs
    Inherits StrategyUserInputs
    Public Property BollingerPeriod As Integer
    Public Property BollingerMultiplier As Integer
    Public Property UseBothSignal As Boolean
    Public Property TelegramAPIKey As String
    Public Property TelegramChatID As String
    Public Property TelegramPLChatID As String
    Public Property InstrumentDetailsFilePath As String
    Public Property InstrumentsData As Dictionary(Of String, InstrumentDetails)
    <Serializable>
    Public Class InstrumentDetails
        Public Property VirtualInstrumentName As String
        Public Property Pair1TradingSymbol As String
        Public Property Pair1Quantity As Integer
        Public Property Pair2TradingSymbol As String
        Public Property Pair2Quantity As Integer
        Public Property PLOffSet As Decimal
        Public Property ReverseSignalExit As Boolean
        Public Property ReverseSignalEntry As Boolean
        Public Property MaxPairLoss As Decimal
        Public Property MaxPairGain As Decimal
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
                        Dim excelColumnList As New List(Of String) From {"PAIR 1 TRADING SYMBOL", "PAIR 1 QUANTITY MULTIPLIER", "PAIR 2 TRADING SYMBOL", "PAIR 2 QUANTITY MULTIPLIER", "PL OFFSET", "REVERSE SIGNAL EXIT", "REVERSE SIGNAL ENTRY", "MAX PAIR LOSS", "MAX PAIR GAIN"}

                        For colCtr = 0 To 8
                            If instrumentDetails(0, colCtr) Is Nothing OrElse Trim(instrumentDetails(0, colCtr).ToString) = "" Then
                                Throw New ApplicationException(String.Format("Invalid format."))
                            Else
                                If Not excelColumnList.Contains(Trim(instrumentDetails(0, colCtr).ToString.ToUpper)) Then
                                    Throw New ApplicationException(String.Format("Invalid format or invalid column at ColumnNumber: {0}", colCtr))
                                End If
                            End If
                        Next

                        For rowCtr = 1 To instrumentDetails.GetLength(0) - 1
                            Dim controllerInstrumentName As String = Nothing
                            Dim pair1TradingSymbol As String = Nothing
                            Dim pair1Quantity As Integer = Integer.MinValue
                            Dim pair2TradingSymbol As String = Nothing
                            Dim pair2Quantity As Integer = Integer.MinValue
                            Dim plOffset As Decimal = Decimal.MinValue
                            Dim reverseSignalExit As Boolean = False
                            Dim reverseSignalEntry As Boolean = False
                            Dim maxPairLoss As Decimal = Decimal.MinValue
                            Dim maxPairGain As Decimal = Decimal.MinValue

                            For columnCtr = 0 To instrumentDetails.GetLength(1)
                                If columnCtr = 0 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        pair1TradingSymbol = instrumentDetails(rowCtr, columnCtr)
                                    Else
                                        If Not rowCtr = instrumentDetails.GetLength(0) Then
                                            Throw New ApplicationException(String.Format("Pair 1 Trading Symbol Missing or Blank Row. RowNumber: {0}", rowCtr))
                                        End If
                                    End If
                                ElseIf columnCtr = 1 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) AndAlso
                                        Math.Round(Val(instrumentDetails(rowCtr, columnCtr)), 0) = Val(instrumentDetails(rowCtr, columnCtr)) Then
                                            pair1Quantity = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Pair 1 Quantity cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, pair1TradingSymbol))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Pair 1 Quantity cannot be blank for {0}", pair1Quantity))
                                    End If
                                ElseIf columnCtr = 2 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        pair2TradingSymbol = instrumentDetails(rowCtr, columnCtr)
                                    Else
                                        If Not rowCtr = instrumentDetails.GetLength(0) Then
                                            Throw New ApplicationException(String.Format("Pair 2 Trading Symbol Missing or Blank Row. RowNumber: {0}", rowCtr))
                                        End If
                                    End If
                                ElseIf columnCtr = 3 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) AndAlso
                                        Math.Round(Val(instrumentDetails(rowCtr, columnCtr)), 0) = Val(instrumentDetails(rowCtr, columnCtr)) Then
                                            pair2Quantity = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Pair 2 Quantity cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, pair2TradingSymbol))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Pair 2 Quantity cannot be blank for {0}", pair2Quantity))
                                    End If
                                ElseIf columnCtr = 4 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            plOffset = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("PL Offset cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, pair1TradingSymbol))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("PL Offset cannot be blank for {0}", pair1TradingSymbol))
                                    End If
                                ElseIf columnCtr = 5 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "TRUE" Then
                                            reverseSignalExit = True
                                        ElseIf instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "FALSE" Then
                                            reverseSignalExit = False
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Reverse Signal Exit cannot be blank for {0}", pair1TradingSymbol))
                                    End If
                                ElseIf columnCtr = 6 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                       Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "TRUE" Then
                                            reverseSignalEntry = True
                                        ElseIf instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "FALSE" Then
                                            reverseSignalEntry = False
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Reverse Signal Entry cannot be blank for {0}", pair1TradingSymbol))
                                    End If
                                ElseIf columnCtr = 7 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                      Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            maxPairLoss = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Max Pair Loss cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, pair1TradingSymbol))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Max Pair Loss cannot be blank for {0}", pair1TradingSymbol))
                                    End If
                                ElseIf columnCtr = 8 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                      Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            maxPairGain = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Max Pair Gain cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, pair1TradingSymbol))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Max Pair Gain cannot be blank for {0}", pair1TradingSymbol))
                                    End If
                                End If
                            Next
                            If pair1TradingSymbol IsNot Nothing AndAlso pair2TradingSymbol IsNot Nothing Then
                                If pair1TradingSymbol <> pair2TradingSymbol Then
                                    controllerInstrumentName = String.Format("{0}_{1}", pair1TradingSymbol, pair2TradingSymbol)
                                    Dim instrumentData As New InstrumentDetails With
                                    {
                                        .VirtualInstrumentName = controllerInstrumentName,
                                        .Pair1TradingSymbol = pair1TradingSymbol,
                                        .Pair1Quantity = pair1Quantity,
                                        .Pair2TradingSymbol = pair2TradingSymbol,
                                        .Pair2Quantity = pair2Quantity,
                                        .PLOffSet = plOffset,
                                        .ReverseSignalExit = reverseSignalExit,
                                        .ReverseSignalEntry = reverseSignalEntry,
                                        .MaxPairLoss = maxPairLoss,
                                        .MaxPairGain = maxPairGain
                                    }

                                    If Me.InstrumentsData Is Nothing Then Me.InstrumentsData = New Dictionary(Of String, InstrumentDetails)
                                    If Me.InstrumentsData.ContainsKey(instrumentData.VirtualInstrumentName) Then
                                        Throw New ApplicationException(String.Format("Duplicate Instrument Name {0}", instrumentData.VirtualInstrumentName))
                                    End If
                                    Me.InstrumentsData.Add(instrumentData.VirtualInstrumentName, instrumentData)
                                End If
                            End If
                        Next
                        'If Me.InstrumentsData.Count > 10 Then
                        '    Throw New ApplicationException("More than 10 instrument is not allowed")
                        'End If
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
