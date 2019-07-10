Imports System.IO
Imports System.Net
Imports System.Threading
Imports Utilities.Strings
Imports Utilities.DAL
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

    Private _cts As CancellationTokenSource
    Private ReadOnly _parentStrategy As TwoThirdStrategy
    Private ReadOnly ZerodhaEODHistoricalURL = "https://kitecharts-aws.zerodha.com/api/chart/{0}/day?api_key=kitefront&access_token=K&from={1}&to={2}"
    Private ReadOnly ZerodhaIntradayHistoricalURL = "https://kitecharts-aws.zerodha.com/api/chart/{0}/minute?api_key=kitefront&access_token=K&from={1}&to={2}"
    Private ReadOnly tradingDay As Date = Date.MinValue
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal parentStrategy As TwoThirdStrategy)
        _cts = canceller
        _parentStrategy = parentStrategy
        tradingDay = Now
    End Sub

    Private Async Function GetHistoricalCandleStickAsync(ByVal instrumentToken As String, ByVal fromDate As Date, ByVal toDate As Date, ByVal historicalDataType As TypeOfData) As Task(Of Dictionary(Of String, Object))
        Try
            _cts.Token.ThrowIfCancellationRequested()
            Dim historicalDataURL As String = Nothing
            Select Case historicalDataType
                Case TypeOfData.Intraday
                    historicalDataURL = String.Format(ZerodhaIntradayHistoricalURL, instrumentToken, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"))
                Case TypeOfData.EOD
                    historicalDataURL = String.Format(ZerodhaEODHistoricalURL, instrumentToken, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"))
            End Select
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
                Dim highATRStocks As Dictionary(Of String, Decimal()) = Nothing
                Dim lastTradingDay As Date = Date.MinValue
                For Each runningInstrument In nfoInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If runningInstrument.RawExchange.ToUpper = "NFO" AndAlso (bannedStock Is Nothing OrElse
                        bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(runningInstrument.RawInstrumentName)) Then
                        If highATRStocks Is Nothing OrElse (highATRStocks IsNot Nothing AndAlso Not highATRStocks.ContainsKey(runningInstrument.RawInstrumentName)) Then
                            Dim rawCashInstrument As IInstrument = allInstruments.ToList.Find(Function(x)
                                                                                                  Return x.TradingSymbol = runningInstrument.RawInstrumentName
                                                                                              End Function)
                            If rawCashInstrument IsNot Nothing Then
                                Dim instrumentData As KeyValuePair(Of Integer, String) = New KeyValuePair(Of Integer, String)(rawCashInstrument.InstrumentIdentifier, rawCashInstrument.TradingSymbol)
                                _cts.Token.ThrowIfCancellationRequested()
                                Dim historicalCandlesJSONDict As Dictionary(Of String, Object) = Await GetHistoricalCandleStickAsync(instrumentData.Key, tradingDay.AddDays(-300), tradingDay.AddDays(-1), TypeOfData.EOD).ConfigureAwait(False)
                                _cts.Token.ThrowIfCancellationRequested()
                                If historicalCandlesJSONDict IsNot Nothing AndAlso historicalCandlesJSONDict.Count > 0 Then
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Dim eodHistoricalData As Dictionary(Of Date, OHLCPayload) = Await GetChartFromHistoricalAsync(historicalCandlesJSONDict, instrumentData.Value).ConfigureAwait(False)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    If eodHistoricalData IsNot Nothing AndAlso eodHistoricalData.Count > 0 Then
                                        _cts.Token.ThrowIfCancellationRequested()
                                        Dim ATRPayload As Dictionary(Of Date, Decimal) = Nothing
                                        CalculateATR(14, eodHistoricalData, ATRPayload)
                                        _cts.Token.ThrowIfCancellationRequested()
                                        Dim lastDayClosePrice As Decimal = eodHistoricalData.LastOrDefault.Value.ClosePrice.Value
                                        lastTradingDay = eodHistoricalData.LastOrDefault.Key
                                        If lastDayClosePrice >= 80 AndAlso lastDayClosePrice <= 1500 Then
                                            Dim atrPercentage As Decimal = (ATRPayload(eodHistoricalData.LastOrDefault.Key) / lastDayClosePrice) * 100
                                            If atrPercentage >= 3 Then
                                                _cts.Token.ThrowIfCancellationRequested()
                                                Dim volumePayload As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = eodHistoricalData.OrderByDescending(Function(x)
                                                                                                                                                                  Return x.Key
                                                                                                                                                              End Function).Take(5)
                                                _cts.Token.ThrowIfCancellationRequested()
                                                If volumePayload IsNot Nothing AndAlso volumePayload.Count > 0 Then
                                                    _cts.Token.ThrowIfCancellationRequested()
                                                    Dim avgVolume As Decimal = volumePayload.Average(Function(x)
                                                                                                         Return CType(x.Value.Volume.Value, Long)
                                                                                                     End Function)
                                                    _cts.Token.ThrowIfCancellationRequested()
                                                    If avgVolume >= (300000 / 100) * lastDayClosePrice Then
                                                        If highATRStocks Is Nothing Then highATRStocks = New Dictionary(Of String, Decimal())
                                                        highATRStocks.Add(instrumentData.Value, {atrPercentage, avgVolume * 100 / ((300000 / 100) * lastDayClosePrice)})
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
                    Dim capableStocks As Dictionary(Of String, InstrumentDetails) = Nothing
                    For Each stock In highATRStocks.OrderByDescending(Function(x)
                                                                          Return x.Value(0)
                                                                      End Function)
                        _cts.Token.ThrowIfCancellationRequested()
                        Dim futureStocks As List(Of IInstrument) = nfoInstruments.ToList.FindAll(Function(x)
                                                                                                     Return x.RawInstrumentName = stock.Key
                                                                                                 End Function)
                        If futureStocks IsNot Nothing AndAlso futureStocks.Count > 0 Then
                            Dim minexpiry As Date = futureStocks.Min(Function(y)
                                                                         Return y.Expiry
                                                                     End Function)
                            Dim tradingStock As IInstrument = Nothing
                            Dim volumeCheckingStock As IInstrument = Nothing
                            _cts.Token.ThrowIfCancellationRequested()
                            If minexpiry.Date = Now.Date Then
                                volumeCheckingStock = futureStocks.Find(Function(x)
                                                                            Return x.Expiry = minexpiry
                                                                        End Function)
                                Dim nextMinexpiry As Date = futureStocks.Min(Function(y)
                                                                                 If Not y.Expiry.Value.Date = Now.Date Then
                                                                                     Return y.Expiry.Value
                                                                                 Else
                                                                                     Return Date.MaxValue
                                                                                 End If
                                                                             End Function)
                                tradingStock = futureStocks.Find(Function(z)
                                                                     Return z.Expiry = nextMinexpiry
                                                                 End Function)
                            Else
                                tradingStock = futureStocks.Find(Function(x)
                                                                     Return x.Expiry = minexpiry
                                                                 End Function)
                                volumeCheckingStock = tradingStock
                            End If
                            _cts.Token.ThrowIfCancellationRequested()
                            If tradingStock IsNot Nothing AndAlso volumeCheckingStock IsNot Nothing Then
                                _cts.Token.ThrowIfCancellationRequested()
                                Dim historicalIntradayCandlesJSONDict As Dictionary(Of String, Object) = Await GetHistoricalCandleStickAsync(volumeCheckingStock.InstrumentIdentifier, lastTradingDay, lastTradingDay, TypeOfData.Intraday).ConfigureAwait(False)
                                _cts.Token.ThrowIfCancellationRequested()
                                If historicalIntradayCandlesJSONDict IsNot Nothing AndAlso historicalIntradayCandlesJSONDict.Count > 0 Then
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Dim intradayHistoricalData As Dictionary(Of Date, OHLCPayload) = Await GetChartFromHistoricalAsync(historicalIntradayCandlesJSONDict, volumeCheckingStock.TradingSymbol).ConfigureAwait(False)
                                    If intradayHistoricalData IsNot Nothing AndAlso intradayHistoricalData.Count > 0 Then
                                        Dim blankCandlePercentage As Decimal = CalculateBlankVolumePercentage(intradayHistoricalData)
                                        Dim instrumentData As New InstrumentDetails With
                                            {.TradingSymbol = tradingStock.TradingSymbol,
                                             .ATR = stock.Value(0),
                                             .BlankCandlePercentage = blankCandlePercentage}
                                        If capableStocks Is Nothing Then capableStocks = New Dictionary(Of String, InstrumentDetails)
                                        capableStocks.Add(tradingStock.TradingSymbol, instrumentData)
                                    End If
                                End If
                            End If
                        End If
                    Next
                    If capableStocks IsNot Nothing AndAlso capableStocks.Count > 0 Then
                        Dim todayStockList As List(Of String) = Nothing
                        Dim stocksLessThanMaxBlankCandlePercentage As IEnumerable(Of KeyValuePair(Of String, InstrumentDetails)) =
                                    capableStocks.Where(Function(x)
                                                            Return x.Value.BlankCandlePercentage <> Decimal.MinValue AndAlso
                                                                  x.Value.BlankCandlePercentage <= 8
                                                        End Function)
                        If stocksLessThanMaxBlankCandlePercentage IsNot Nothing AndAlso stocksLessThanMaxBlankCandlePercentage.Count > 0 Then
                            Dim stockCounter As Integer = 0
                            For Each stockData In stocksLessThanMaxBlankCandlePercentage.OrderByDescending(Function(x)
                                                                                                               Return x.Value.ATR
                                                                                                           End Function)
                                _cts.Token.ThrowIfCancellationRequested()
                                If todayStockList Is Nothing Then todayStockList = New List(Of String)
                                todayStockList.Add(stockData.Key)
                                stockCounter += 1
                                If stockCounter = 5 Then Exit For
                            Next
                            If stockCounter < 5 Then
                                Dim stocksLessThanHigherLimitOfMaxBlankCandlePercentage As IEnumerable(Of KeyValuePair(Of String, InstrumentDetails)) =
                                    capableStocks.Where(Function(x)
                                                            Return x.Value.BlankCandlePercentage > 8 AndAlso
                                                                  x.Value.BlankCandlePercentage <= 20
                                                        End Function)
                                If stocksLessThanHigherLimitOfMaxBlankCandlePercentage IsNot Nothing AndAlso stocksLessThanHigherLimitOfMaxBlankCandlePercentage.Count > 0 Then
                                    For Each stockData In stocksLessThanHigherLimitOfMaxBlankCandlePercentage.OrderBy(Function(y)
                                                                                                                          Return y.Value.BlankCandlePercentage
                                                                                                                      End Function)
                                        _cts.Token.ThrowIfCancellationRequested()
                                        If todayStockList Is Nothing Then todayStockList = New List(Of String)
                                        todayStockList.Add(stockData.Key)
                                        stockCounter += 1
                                        If stockCounter = 5 Then Exit For
                                    Next
                                End If
                            End If
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If todayStockList IsNot Nothing AndAlso todayStockList.Count > 0 Then
                            Dim allStockData As DataTable = Nothing
                            If CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentDetailsFilePath IsNot Nothing AndAlso
                                File.Exists(CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentDetailsFilePath) Then
                                Using csv As New CSVHelper(CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentDetailsFilePath, ",", _cts)
                                    allStockData = csv.GetDataTableFromCSV(1)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    If allStockData IsNot Nothing AndAlso allStockData.Rows.Count > 0 Then
                                        For i = 0 To allStockData.Rows.Count - 1
                                            allStockData.Rows(i)(0) = todayStockList(i)
                                        Next
                                        File.Delete(CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentDetailsFilePath)
                                        csv.GetCSVFromDataTable(allStockData)
                                    End If
                                End Using
                                If CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentsData IsNot Nothing Then
                                    CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentsData.Clear()
                                    CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentsData = Nothing
                                    CType(_parentStrategy.UserSettings, TwoThirdUserInputs).FillInstrumentDetails(CType(_parentStrategy.UserSettings, TwoThirdUserInputs).InstrumentDetailsFilePath, _cts)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Function
    Private Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object),
                                                       ByVal tradingSymbol As String) As Task(Of Dictionary(Of Date, OHLCPayload))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim ret As Dictionary(Of Date, OHLCPayload) = Nothing
        If historicalCandlesJSONDict.ContainsKey("data") Then
            Dim historicalCandlesDict As Dictionary(Of String, Object) = historicalCandlesJSONDict("data")
            If historicalCandlesDict.ContainsKey("candles") AndAlso historicalCandlesDict("candles").count > 0 Then
                Dim historicalCandles As ArrayList = historicalCandlesDict("candles")
                If ret Is Nothing Then ret = New Dictionary(Of Date, OHLCPayload)
                OnHeartbeat(String.Format("Generating Payload for {0}", tradingSymbol))
                Dim previousPayload As OHLCPayload = Nothing
                For Each historicalCandle In historicalCandles
                    _cts.Token.ThrowIfCancellationRequested()
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

    Private Function CalculateBlankVolumePercentage(ByVal inputPayload As Dictionary(Of Date, OHLCPayload)) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If inputPayload IsNot Nothing AndAlso inputPayload.Count > 0 Then
            Dim blankCandlePayload As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = inputPayload.Where(Function(x)
                                                                                                                  Return x.Value.OpenPrice.Value = x.Value.LowPrice.Value AndAlso
                                                                                                                  x.Value.LowPrice.Value = x.Value.HighPrice.Value AndAlso
                                                                                                                  x.Value.HighPrice.Value = x.Value.ClosePrice.Value
                                                                                                              End Function)
            If blankCandlePayload IsNot Nothing AndAlso blankCandlePayload.Count > 0 Then
                ret = Math.Round((blankCandlePayload.Count / inputPayload.Count) * 100, 2)
            Else
                ret = 0
            End If
        End If
        Return ret
    End Function

    Private Class InstrumentDetails
        Public TradingSymbol As String
        Public ATR As Decimal
        Public BlankCandlePercentage As Decimal
    End Class

    Enum TypeOfData
        Intraday = 1
        EOD
    End Enum

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
