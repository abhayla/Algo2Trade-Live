Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.DAL
Imports Algo2TradeCore.Entities

<Serializable>
Public Class MomentumReversalUserInputs
    Inherits StrategyUserInputs

    Private _IdleTimeStart As Date
    Public Property IdleTimeStart As Date
        Get
            Return New Date(Now.Year, Now.Month, Now.Day, _IdleTimeStart.Hour, _IdleTimeStart.Minute, _IdleTimeStart.Second)
        End Get
        Set(value As Date)
            _IdleTimeStart = value
        End Set
    End Property

    Private _IdleTimeEnd As Date
    Public Property IdleTimeEnd As Date
        Get
            Return New Date(Now.Year, Now.Month, Now.Day, _IdleTimeEnd.Hour, _IdleTimeEnd.Minute, _IdleTimeEnd.Second)
        End Get
        Set(value As Date)
            _IdleTimeEnd = value
        End Set
    End Property

    Public Property MaxLossPerDay As Decimal
    Public Property MaxProfitPerDay As Decimal
    Public Property InstrumentDetailsFilePath As String
    Public Property InstrumentsData As Dictionary(Of String, InstrumentDetails)

    <Serializable>
    Public Class InstrumentDetails
        Public Property TradingSymbol As String
        Public Property Quantity As Integer
        Public Property Buffer As Decimal
        Public Property Direction As IOrder.TypeOfTransaction
        Public Property Distance As Decimal
        Public Property SL As Decimal
        Public Property FirstMovementLTP As Decimal
        Public Property FirstMovementSL As Decimal
        Public Property OnwardMovementLTP As Decimal
        Public Property OnwardMovementSL As Decimal
        Public Property Percentage As Boolean
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
                        Dim excelColumnList As New List(Of String) From {"TRADING SYMBOL", "QUANTITY", "BUFFER", "DIRECTION", "DISTANCE", "SL", "FIRST MOVEMENT LTP", "FIRST MOVEMENT SL", "ONWARD MOVEMENT LTP", "ONWARD MOVEMENT SL", "PERCENTAGE"}

                        For colCtr = 0 To 10
                            If instrumentDetails(0, colCtr) Is Nothing OrElse Trim(instrumentDetails(0, colCtr).ToString) = "" Then
                                Throw New ApplicationException(String.Format("Invalid format."))
                            Else
                                If Not excelColumnList.Contains(Trim(instrumentDetails(0, colCtr).ToString.ToUpper)) Then
                                    Throw New ApplicationException(String.Format("Invalid format or invalid column at ColumnNumber: {0}", colCtr))
                                End If
                            End If
                        Next
                        For rowCtr = 1 To instrumentDetails.GetLength(0) - 1
                            Dim trdngSymbl As String = Nothing
                            Dim qnty As Integer = Integer.MinValue
                            Dim bfr As Decimal = Decimal.MinValue
                            Dim drctn As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                            Dim dstnc As Decimal = Decimal.MinValue
                            Dim stoploss As Decimal = Decimal.MinValue
                            Dim firstLTP As Decimal = Decimal.MinValue
                            Dim firstSL As Decimal = Decimal.MinValue
                            Dim onwardLTP As Decimal = Decimal.MinValue
                            Dim onwardSL As Decimal = Decimal.MinValue
                            Dim percentage As Boolean = False

                            For columnCtr = 0 To instrumentDetails.GetLength(1)
                                If columnCtr = 0 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        trdngSymbl = instrumentDetails(rowCtr, columnCtr)
                                    Else
                                        If Not rowCtr = instrumentDetails.GetLength(0) Then
                                            Throw New ApplicationException(String.Format("Trading Symbol Missing or Blank Row. RowNumber: {0}", rowCtr))
                                        End If
                                    End If
                                ElseIf columnCtr = 1 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) AndAlso
                                            Math.Round(Val(instrumentDetails(rowCtr, columnCtr)), 0) = Val(instrumentDetails(rowCtr, columnCtr)) Then
                                            qnty = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Quantity cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Quantity cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 2 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            bfr = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Buffer cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Buffer cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 3 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "BUY" Then
                                            drctn = IOrder.TypeOfTransaction.Buy
                                        ElseIf instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "SELL" Then
                                            drctn = IOrder.TypeOfTransaction.Sell
                                        Else
                                            Throw New ApplicationException(String.Format("Only 'BUY' or 'SELL' is allowed as Direction for {0}", trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Direction cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 4 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            dstnc = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Distance cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Distance cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 5 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            stoploss = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Stoploss cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Stoploss cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 6 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            firstLTP = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("First Movement LTP cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("First Movement LTP cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 7 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            firstSL = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("First Movement SL cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("First Movement SL cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 8 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            onwardLTP = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Onward Movement LTP cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Onward Movement LTP cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 9 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            onwardSL = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Onward Movement SL cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Onward Movement SL cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                ElseIf columnCtr = 10 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If instrumentDetails(rowCtr, columnCtr).ToString.ToUpper = "TRUE" Then
                                            percentage = True
                                        End If
                                    Else
                                        Throw New ApplicationException(String.Format("Percentage cannot be null for {0}", instrumentDetails(rowCtr, columnCtr).GetType, trdngSymbl))
                                    End If
                                End If
                            Next
                            If trdngSymbl IsNot Nothing Then
                                Dim instrumentData As New InstrumentDetails With {
                                    .TradingSymbol = trdngSymbl.ToUpper,
                                    .Quantity = qnty,
                                    .Buffer = bfr,
                                    .Direction = drctn,
                                    .Distance = dstnc,
                                    .SL = stoploss,
                                    .FirstMovementLTP = firstLTP,
                                    .FirstMovementSL = firstSL,
                                    .OnwardMovementLTP = onwardLTP,
                                    .OnwardMovementSL = onwardSL,
                                    .Percentage = percentage
                                }
                                If Me.InstrumentsData Is Nothing Then Me.InstrumentsData = New Dictionary(Of String, InstrumentDetails)
                                If Me.InstrumentsData.ContainsKey(instrumentData.TradingSymbol) Then
                                    Throw New ApplicationException(String.Format("Duplicate Trading Symbol {0}", instrumentData.TradingSymbol))
                                End If
                                Me.InstrumentsData.Add(instrumentData.TradingSymbol, instrumentData)
                                If Me.InstrumentsData IsNot Nothing AndAlso Me.InstrumentsData.Count > 7 Then
                                    Throw New ApplicationException(String.Format("Only seven instrument can be added"))
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
