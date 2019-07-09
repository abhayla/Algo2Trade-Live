Imports System.IO
Imports System.Net
Imports System.Threading
Imports Utilities.Strings
Imports Algo2TradeCore.Entities

Public Class TwoThirdFillInstrumentDetails
    Implements IDisposable

#Region "Events/Event handlers"
    Public Event DocumentDownloadComplete()
    Public Event DocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
    Public Event Heartbeat(ByVal msg As String)
    Public Event WaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
    'The below functions are needed to allow the derived classes to raise the above two events
    Protected Overridable Sub OnDocumentDownloadComplete()
        RaiseEvent DocumentDownloadComplete()
    End Sub
    Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
        RaiseEvent DocumentRetryStatus(currentTry, totalTries)
    End Sub
    Protected Overridable Sub OnHeartbeat(ByVal msg As String)
        RaiseEvent Heartbeat(msg)
    End Sub
    Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
        RaiseEvent WaitingFor(elapsedSecs, totalSecs, msg)
    End Sub
#End Region

    Private cts As CancellationTokenSource
    Private ReadOnly ZerodhaHistoricalURL = "https://kitecharts-aws.zerodha.com/api/chart/{0}/day?api_key=kitefront&access_token=K&from={1}&to={2}"
    Public Sub New(ByVal canceller As CancellationTokenSource)
        cts = canceller
    End Sub

    Private Async Function GetHistoricalCandleStickAsync(ByVal instrumentToken As String, ByVal fromDate As Date, ByVal toDate As Date) As Task(Of Dictionary(Of String, Object))
        Try
            cts.Token.ThrowIfCancellationRequested()
            Dim historicalDataURL As String = String.Format(ZerodhaHistoricalURL, instrumentToken, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"))
            OnHeartbeat(String.Format("Fetching historical Data: {0}", historicalDataURL))
            Using sr As New StreamReader(HttpWebRequest.Create(historicalDataURL).GetResponseAsync().Result.GetResponseStream)
                Dim jsonString = Await sr.ReadToEndAsync.ConfigureAwait(False)
                Dim retDictionary As Dictionary(Of String, Object) = StringManipulation.JsonDeserialize(jsonString)
                Return retDictionary
            End Using
        Catch ex As Exception
            Throw ex
        End Try
    End Function
    Public Async Function GetInstrumentData(ByVal allInstruments As IEnumerable(Of IInstrument), ByVal bannedStock As List(Of String)) As Task
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            Dim nfoInstruments As IEnumerable(Of IInstrument) = allInstruments.Where(Function(x)
                                                                                         Return x.Segment = "NFO-FUT"
                                                                                     End Function)
            If nfoInstruments IsNot Nothing AndAlso nfoInstruments.Count > 0 Then
                Dim highATRStocks As Dictionary(Of String, Decimal) = Nothing
                For Each runningInstrument In nfoInstruments
                    If runningInstrument.RawExchange.ToUpper = "NFO" AndAlso (bannedStock Is Nothing OrElse
                    bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(runningInstrument.RawInstrumentName)) Then
                        If highATRStocks Is Nothing OrElse (highATRStocks IsNot Nothing AndAlso Not highATRStocks.ContainsKey(runningInstrument.RawInstrumentName)) Then
                            Dim rawCashInstrument As IInstrument = allInstruments.ToList.Find(Function(x)
                                                                                                  Return x.TradingSymbol = runningInstrument.RawInstrumentName
                                                                                              End Function)
                            If rawCashInstrument IsNot Nothing Then
                                Dim instrumentData As KeyValuePair(Of Integer, String) = New KeyValuePair(Of Integer, String)(rawCashInstrument.InstrumentIdentifier, rawCashInstrument.TradingSymbol)
                                Dim historicalCandlesJSONDict As Dictionary(Of String, Object) = Await GetHistoricalCandleStickAsync(instrumentData.Key, Now.AddDays(-300), Now.AddDays(-1)).ConfigureAwait(False)
                                If historicalCandlesJSONDict IsNot Nothing AndAlso historicalCandlesJSONDict.Count > 0 Then
                                    Dim eodHistoricalData As Dictionary(Of Date, OHLCPayload) = Await GetChartFromHistoricalAsync(historicalCandlesJSONDict, instrumentData.Value).ConfigureAwait(False)
                                    If eodHistoricalData IsNot Nothing AndAlso eodHistoricalData.Count > 0 Then
                                        Dim ATRPayload As Dictionary(Of Date, Decimal) = Nothing
                                        CalculateATR(14, eodHistoricalData, ATRPayload)
                                        Dim lastDayClosePrice As Decimal = eodHistoricalData.LastOrDefault.Value.ClosePrice.Value
                                        If lastDayClosePrice >= 80 AndAlso lastDayClosePrice <= 1500 Then
                                            Dim atrPercentage As Decimal = (ATRPayload(eodHistoricalData.LastOrDefault.Key) / lastDayClosePrice) * 100
                                            If atrPercentage >= 3 Then
                                                Dim volumePayload As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = eodHistoricalData.OrderByDescending(Function(x)
                                                                                                                                                                  Return x.Key
                                                                                                                                                              End Function).Take(30)
                                                If volumePayload IsNot Nothing AndAlso volumePayload.Count > 0 Then
                                                    Dim avgVolume As Decimal = volumePayload.Average(Function(x)
                                                                                                         Return CType(x.Value.Volume.Value, Long)
                                                                                                     End Function)
                                                    If avgVolume >= (300000 / 100) * lastDayClosePrice Then
                                                        If highATRStocks Is Nothing Then highATRStocks = New Dictionary(Of String, Decimal)
                                                        highATRStocks.Add(instrumentData.Value, atrPercentage)
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
                If highATRStocks IsNot Nothing AndAlso highATRStocks.Count > 0 Then

                End If
            End If
        End If
    End Function
    Private Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object),
                                                       ByVal tradingSymbol As String) As Task(Of Dictionary(Of Date, OHLCPayload))
        Await Task.Delay(0, cts.Token).ConfigureAwait(False)
        Dim ret As Dictionary(Of Date, OHLCPayload) = Nothing
        If historicalCandlesJSONDict.ContainsKey("data") Then
            Dim historicalCandlesDict As Dictionary(Of String, Object) = historicalCandlesJSONDict("data")
            If historicalCandlesDict.ContainsKey("candles") AndAlso historicalCandlesDict("candles").count > 0 Then
                Dim historicalCandles As ArrayList = historicalCandlesDict("candles")
                If ret Is Nothing Then ret = New Dictionary(Of Date, OHLCPayload)
                OnHeartbeat(String.Format("Generating Payload for {0}", tradingSymbol))
                Dim previousPayload As OHLCPayload = Nothing
                For Each historicalCandle In historicalCandles
                    cts.Token.ThrowIfCancellationRequested()
                    Dim runningSnapshotTime As Date = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))

                    Dim runningPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.Historical)
                    With runningPayload
                        .SnapshotDateTime = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))
                        .TradingSymbol = tradingSymbol
                        .OpenPrice.Value = historicalCandle(1)
                        .HighPrice.Value = historicalCandle(2)
                        .LowPrice.Value = historicalCandle(3)
                        .ClosePrice.Value = historicalCandle(4)
                        .Volume.Value = historicalCandle(5)
                        .PreviousPayload = previousPayload
                    End With
                    previousPayload = runningPayload
                    ret.Add(runningSnapshotTime, runningPayload)
                Next
            End If
        End If
        Return ret
    End Function

    Private Sub CalculateATR(ByVal ATRPeriod As Integer, ByVal inputPayload As Dictionary(Of Date, OHLCPayload), ByRef outputPayload As Dictionary(Of Date, Decimal))
        'Using WILDER Formula
        If inputPayload IsNot Nothing AndAlso inputPayload.Count > 0 Then
            If inputPayload.Count < 100 Then
                Throw New ApplicationException("Can't Calculate ATR")
            End If
            Dim firstPayload As Boolean = True
            Dim highLow As Double = Nothing
            Dim highClose As Double = Nothing
            Dim lowClose As Double = Nothing
            Dim TR As Double = Nothing
            Dim SumTR As Double = 0.00
            Dim AvgTR As Double = 0.00
            Dim counter As Integer = 0
            outputPayload = New Dictionary(Of Date, Decimal)
            For Each runningInputPayload In inputPayload
                counter += 1
                highLow = runningInputPayload.Value.HighPrice.Value - runningInputPayload.Value.LowPrice.Value
                If firstPayload = True Then
                    TR = highLow
                    firstPayload = False
                Else
                    highClose = Math.Abs(runningInputPayload.Value.HighPrice.Value - runningInputPayload.Value.PreviousPayload.ClosePrice.Value)
                    lowClose = Math.Abs(runningInputPayload.Value.LowPrice.Value - runningInputPayload.Value.PreviousPayload.ClosePrice.Value)
                    TR = Math.Max(highLow, Math.Max(highClose, lowClose))
                End If
                SumTR = SumTR + TR
                If counter = ATRPeriod Then
                    AvgTR = SumTR / ATRPeriod
                    outputPayload.Add(runningInputPayload.Value.SnapshotDateTime, AvgTR)
                ElseIf counter > ATRPeriod Then
                    AvgTR = (outputPayload(runningInputPayload.Value.PreviousPayload.SnapshotDateTime) * (ATRPeriod - 1) + TR) / ATRPeriod
                    outputPayload.Add(runningInputPayload.Value.SnapshotDateTime, AvgTR)
                Else
                    AvgTR = SumTR / counter
                    outputPayload.Add(runningInputPayload.Value.SnapshotDateTime, AvgTR)
                End If
            Next
        End If
    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class
