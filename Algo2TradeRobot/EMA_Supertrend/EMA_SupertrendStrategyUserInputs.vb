Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.DAL

<Serializable>
Public Class EMA_SupertrendStrategyUserInputs
    Inherits StrategyUserInputs
    Public Property FastEMAPeriod As Integer
    Public Property SlowEMAPeriod As Integer
    Public Property SupertrendPeriod As Integer
    Public Property SupertrendMultiplier As Decimal
    Public Property InstrumentDetailsFilePath As String
    Public Property InstrumentsData As Dictionary(Of String, InstrumentDetails)
    <Serializable>
    Public Class InstrumentDetails
        Public Property InstrumentName As String
        Public Property Quantity As Integer
        Public Property StoplossPercentage As Decimal
        Public Property TargetPercentage As Decimal
        Public Property IntemediateTargetPercentage As Decimal

        Private _LowVolatilityStartTime As Date
        Public Property LowVolatilityStartTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _LowVolatilityStartTime.Hour, _LowVolatilityStartTime.Minute, _LowVolatilityStartTime.Second)
            End Get
            Set(value As Date)
                _LowVolatilityStartTime = value
            End Set
        End Property

        Private _LowVolatilityExitTime As Date
        Public Property LowVolatilityExitTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _LowVolatilityExitTime.Hour, _LowVolatilityExitTime.Minute, _LowVolatilityExitTime.Second)
            End Get
            Set(value As Date)
                _LowVolatilityExitTime = value
            End Set
        End Property
        Public Property LowVolatilityTargetPercentage As Decimal
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
                        Dim excelColumnList As New List(Of String) From {"INSTRUMENT NAME", "NUMBER OF LOTS", "STOPLOSS %", "TARGET %", "INTERMEDIATE TARGET %", "LOW VOLATILITY START TIME", "LOW VOLATILITY EXIT TIME", "LOW VOLATILITY TARGET %"}

                        For colCtr = 0 To 7
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
                            Dim stoplossPercentage As Decimal = Decimal.MinValue
                            Dim targetPercentage As Decimal = Decimal.MinValue
                            Dim intemediateTargetPercentage As Decimal = Decimal.MinValue
                            Dim lowVolatilityStartTime As Date = Nothing
                            Dim lowVolatilityExitTime As Date = Nothing
                            Dim lowVolatilityTargetPercentage As Decimal = Decimal.MinValue
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
                                            Throw New ApplicationException(String.Format("Number Of Lots cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 2 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            stoplossPercentage = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Stoploss % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 3 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            targetPercentage = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Target % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 4 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            intemediateTargetPercentage = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Intermediate Target % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                ElseIf columnCtr = 5 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        'If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                        lowVolatilityStartTime = Date.Parse(instrumentDetails(rowCtr, columnCtr))
                                        'Else
                                        '    Throw New ApplicationException(String.Format("Stoploss % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        'End If
                                    End If
                                ElseIf columnCtr = 6 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        'If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                        lowVolatilityExitTime = Date.Parse(instrumentDetails(rowCtr, columnCtr))
                                        'Else
                                        '    Throw New ApplicationException(String.Format("Stoploss % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        'End If
                                    End If
                                ElseIf columnCtr = 7 Then
                                    If instrumentDetails(rowCtr, columnCtr) IsNot Nothing AndAlso
                                        Not Trim(instrumentDetails(rowCtr, columnCtr).ToString) = "" Then
                                        If IsNumeric(instrumentDetails(rowCtr, columnCtr)) Then
                                            lowVolatilityTargetPercentage = instrumentDetails(rowCtr, columnCtr)
                                        Else
                                            Throw New ApplicationException(String.Format("Low Volatility Target % cannot be of type {0} for {1}", instrumentDetails(rowCtr, columnCtr).GetType, instrumentName))
                                        End If
                                    End If
                                End If
                            Next
                            If instrumentName IsNot Nothing Then
                                Dim instrumentData As New InstrumentDetails
                                instrumentData.InstrumentName = instrumentName.ToUpper
                                instrumentData.Quantity = quantity
                                instrumentData.StoplossPercentage = stoplossPercentage
                                instrumentData.TargetPercentage = targetPercentage
                                instrumentData.IntemediateTargetPercentage = intemediateTargetPercentage
                                instrumentData.LowVolatilityStartTime = lowVolatilityStartTime
                                instrumentData.LowVolatilityExitTime = lowVolatilityExitTime
                                instrumentData.LowVolatilityTargetPercentage = lowVolatilityTargetPercentage
                                If Me.InstrumentsData Is Nothing Then Me.InstrumentsData = New Dictionary(Of String, InstrumentDetails)
                                If Me.InstrumentsData.ContainsKey(instrumentData.InstrumentName) Then
                                    Throw New ApplicationException(String.Format("Duplicate Instrument Name {0}", instrumentData.InstrumentName))
                                End If
                                Me.InstrumentsData.Add(instrumentData.InstrumentName, instrumentData)
                            End If
                        Next
                        If Me.InstrumentsData.Count > 10 Then
                            Throw New ApplicationException("More than 10 instrument is not allowed")
                        End If
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
