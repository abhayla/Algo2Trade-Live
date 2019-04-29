Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Controller
Imports NLog
Imports Algo2TradeCore.Strategies
Imports System.Text

Namespace ChartHandler.ChartStyle
    Public Class CandleStickChart
        Inherits Chart

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal assoicatedParentInstrument As IInstrument,
                       ByVal associatedStrategyInstruments As IEnumerable(Of StrategyInstrument),
                       ByVal canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, assoicatedParentInstrument, associatedStrategyInstruments, canceller)
        End Sub
        Public Overrides Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object)) As Task
            'Exit Function
            'logger.Debug("{0}->GetChartFromHistoricalAsync, parameters:{1}", Me.ToString, Utilities.Strings.JsonSerialize(historicalCandlesJSONDict))
            Try
                While 1 = Interlocked.Exchange(_historicalLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                'Interlocked.Increment(_historicalLock)
                'Debug.WriteLine(String.Format("Process Historical before. Time:{0}, Lock:{1}", Now, _lock))
                If historicalCandlesJSONDict.ContainsKey("data") Then
                    Dim historicalCandlesDict As Dictionary(Of String, Object) = historicalCandlesJSONDict("data")
                    If historicalCandlesDict.ContainsKey("candles") AndAlso historicalCandlesDict("candles").count > 0 Then
                        Dim historicalCandles As ArrayList = historicalCandlesDict("candles")
                        Dim previousCandlePayload As OHLCPayload = Nothing
                        If _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
                            Dim previousCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) =
                           _parentInstrument.RawPayloads.Where(Function(y)
                                                                   Return y.Key < Utilities.Time.GetDateTimeTillMinutes(historicalCandles(0)(0))
                                                               End Function)

                            If previousCandles IsNot Nothing AndAlso previousCandles.Count > 0 Then
                                previousCandlePayload = previousCandles.OrderByDescending(Function(x)
                                                                                              Return x.Key
                                                                                          End Function).FirstOrDefault.Value
                                'Print previous candle
                                'Debug.WriteLine(previousCandle.PreviousPayload.ToString)
                                'Debug.WriteLine(previousCandle.ToString)
                            End If
                        End If
                        Dim s As Stopwatch = New Stopwatch
                        s.Start()
                        Dim onwardCandleUpdate As Boolean = False
                        For Each historicalCandle In historicalCandles
                            Dim runningSnapshotTime As Date = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))

                            'Dim runningPayload As OHLCPayload = New OHLCPayload(IPayload.PayloadSource.Historical)
                            'With runningPayload
                            '    .SnapshotDateTime = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))
                            '    .TradingSymbol = _parentInstrument.TradingSymbol
                            '    .OpenPrice = New Field(TypeOfField.Open, historicalCandle(1))
                            '    .HighPrice = New Field(TypeOfField.High, historicalCandle(2))
                            '    .LowPrice = New Field(TypeOfField.Low, historicalCandle(3))
                            '    .ClosePrice = New Field(TypeOfField.Close, historicalCandle(4))
                            '    .Volume = New Field(TypeOfField.Volume, historicalCandle(5))
                            '    If previousCandlePayload IsNot Nothing AndAlso
                            '        .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
                            '        .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
                            '    Else
                            '        .DailyVolume = .Volume.Value
                            '    End If
                            '    .PreviousPayload = previousCandlePayload
                            'End With
                            'previousCandlePayload = runningPayload

                            'If runningPayload IsNot Nothing Then
                            Dim dummy As OHLCPayload = Nothing
                            Dim existingOrAddedPayload As OHLCPayload = Nothing
                            Dim freshCandleAdded As Boolean = False
                            If _parentInstrument.RawPayloads IsNot Nothing Then
                                If Not _parentInstrument.RawPayloads.ContainsKey(runningSnapshotTime) Then
                                    freshCandleAdded = True
                                    'Create new candle and add it
                                    Dim runningPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.Historical)
                                    With runningPayload
                                        .SnapshotDateTime = runningSnapshotTime
                                        .TradingSymbol = _parentInstrument.TradingSymbol
                                        .OpenPrice.Value = historicalCandle(1)
                                        .HighPrice.Value = historicalCandle(2)
                                        .LowPrice.Value = historicalCandle(3)
                                        .ClosePrice.Value = historicalCandle(4)
                                        .Volume.Value = historicalCandle(5)
                                        If previousCandlePayload IsNot Nothing AndAlso
                                            .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
                                            .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
                                        Else
                                            .DailyVolume = .Volume.Value
                                        End If
                                        .PreviousPayload = previousCandlePayload
                                    End With
                                    ''''previousCandlePayload = runningPayload
                                    'Now that the candle is created, add it to the collection
                                    existingOrAddedPayload = _parentInstrument.RawPayloads.GetOrAdd(runningSnapshotTime, runningPayload)
                                Else
                                    'Get the existing already present candle
                                    existingOrAddedPayload = _parentInstrument.RawPayloads.GetOrAdd(runningSnapshotTime, dummy)
                                End If
                            End If
                            Dim candleNeedsUpdate As Boolean = False
                            If existingOrAddedPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick Then
                                candleNeedsUpdate = True
                                'candleNeedsUpdate = Not existingOrAddedPayload.Equals(historicalCandle(1),
                                '                                                    historicalCandle(2),
                                '                                                    historicalCandle(3),
                                '                                                    historicalCandle(4),
                                '                                                    historicalCandle(5),
                                '                                                    runningSnapshotTime)

                                UpdateHistoricalCandleStick(runningSnapshotTime,
                                                            historicalCandle(1),
                                                            historicalCandle(2),
                                                            historicalCandle(3),
                                                            historicalCandle(4),
                                                            historicalCandle(5),
                                                            previousCandlePayload)
                                'With _parentInstrument.RawPayloads(runningSnapshotTime)
                                '    .PayloadGeneratedBy = IPayload.PayloadSource.Historical
                                '    .SnapshotDateTime = runningSnapshotTime
                                '    .TradingSymbol = _parentInstrument.TradingSymbol
                                '    .OpenPrice.Value = historicalCandle(1)
                                '    .HighPrice.Value = historicalCandle(2)
                                '    .LowPrice.Value = historicalCandle(3)
                                '    .ClosePrice.Value = historicalCandle(4)
                                '    .Volume.Value = historicalCandle(5)
                                '    If previousCandlePayload IsNot Nothing AndAlso
                                '        .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
                                '        .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
                                '    Else
                                '        .DailyVolume = .Volume.Value
                                '    End If
                                '    .PreviousPayload = previousCandlePayload
                                'End With
                                ''''If Not candleNeedsUpdate Then previousCandlePayload = _parentInstrument.RawPayloads(runningSnapshotTime)
                                '_parentInstrument.RawPayloads(runningSnapshotTime) = runningPayload
                            ElseIf onwardCandleUpdate OrElse Not existingOrAddedPayload.Equals(historicalCandle(1),
                                                                                                historicalCandle(2),
                                                                                                historicalCandle(3),
                                                                                                historicalCandle(4),
                                                                                                historicalCandle(5),
                                                                                                runningSnapshotTime) Then
                                candleNeedsUpdate = True
                                onwardCandleUpdate = True
                            End If
                            If candleNeedsUpdate OrElse freshCandleAdded Then
                                ''existingOrAddedPayload = runningPayload
                                UpdateHistoricalCandleStick(runningSnapshotTime,
                                                            historicalCandle(1),
                                                            historicalCandle(2),
                                                            historicalCandle(3),
                                                            historicalCandle(4),
                                                            historicalCandle(5),
                                                            previousCandlePayload)
                                'With _parentInstrument.RawPayloads(runningSnapshotTime)
                                '    .SnapshotDateTime = runningSnapshotTime
                                '    .TradingSymbol = _parentInstrument.TradingSymbol
                                '    .OpenPrice.Value = historicalCandle(1)
                                '    .HighPrice.Value = historicalCandle(2)
                                '    .LowPrice.Value = historicalCandle(3)
                                '    .ClosePrice.Value = historicalCandle(4)
                                '    .Volume.Value = historicalCandle(5)
                                '    If previousCandlePayload IsNot Nothing AndAlso
                                '        .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
                                '        .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
                                '    Else
                                '        .DailyVolume = .Volume.Value
                                '    End If
                                '    .PreviousPayload = previousCandlePayload
                                'End With
                                ''''previousCandlePayload = _parentInstrument.RawPayloads(runningSnapshotTime)

                                '_parentInstrument.RawPayloads(runningSnapshotTime) = runningPayload
                                If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                                    For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
                                        Await runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsAsync(Me, _parentInstrument.RawPayloads(runningSnapshotTime)).ConfigureAwait(False)
                                    Next
                                End If
                            End If
                            'End If
                            previousCandlePayload = _parentInstrument.RawPayloads(runningSnapshotTime)
                        Next
                        _parentInstrument.IsHistoricalCompleted = True

                        s.Stop()
                        Debug.WriteLine(String.Format("{0}, Time:{1}", _parentInstrument.TradingSymbol, s.ElapsedMilliseconds))
                        ''TODO: Below loop is for checking purpose
                        'For Each payload In _parentInstrument.RawPayloads.OrderBy(Function(x)
                        '                                                              Return x.Key
                        '                                                          End Function)
                        '    Debug.WriteLine(payload.Value.ToString())
                        'Next
                        'Try
                        '    Dim outputConsumer As PayloadToChartConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault
                        '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                        '                                                                        Return x.Key
                        '                                                                    End Function)
                        '            If CType(payload.Value, OHLCPayload).PreviousPayload IsNot Nothing Then
                        '                Debug.WriteLine(payload.Value.ToString())
                        '            End If
                        '        Next
                        '    End If
                        'Catch ex As Exception
                        '    Throw ex
                        'End Try
                        'Try
                        '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
                        '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                        '                                                                        Return x.Key
                        '                                                                    End Function)
                        '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.EMAConsumer.EMAPayload).EMA.Value.ToString())
                        '        Next
                        '    End If
                        'Catch ex As Exception
                        '    Throw ex
                        'End Try
                        'Try
                        '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.LastOrDefault
                        '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                        '                                                                        Return x.Key
                        '                                                                    End Function)
                        '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.SupertrendConsumer.SupertrendPayload).Supertrend.Value.ToString())
                        '        Next
                        '    End If
                        'Catch ex As Exception
                        '    Throw ex
                        'End Try
                    End If
                End If
            Catch ex As Exception
                logger.Error("GetChartFromHistoricalAsync:{0}, error:{1}", Me.ToString, ex.ToString)
                Me.ParentController.OrphanException = ex
            Finally
                Interlocked.Exchange(_historicalLock, 0)
                'Debug.WriteLine(String.Format("Process Historical after. Time:{0}, Lock:{1}", Now, _lock))
            End Try
        End Function

        Public Overrides Async Function GetChartFromTickAsync(ByVal tickData As ITick) As Task
            'logger.Debug("{0}->GetChartFromTickAsync, parameters:{1}", Me.ToString, Utilities.Strings.JsonSerialize(tickData))
            'Exit Function
            If tickData Is Nothing OrElse tickData.Timestamp Is Nothing OrElse tickData.Timestamp.Value = Date.MinValue OrElse tickData.Timestamp.Value = New Date(1970, 1, 1, 5, 30, 0) Then
                Exit Function
            End If
            If tickData.Timestamp.Value < Me._parentInstrument.ExchangeDetails.ExchangeStartTime Then
                Exit Function
            End If

            Try
                While 1 = Interlocked.Exchange(_tickLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                'Interlocked.Increment(_tickLock)

                Dim lastExistingPayload As OHLCPayload = Nothing
                If _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
                    Dim lastExistingPayloads As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) =
                        _parentInstrument.RawPayloads.Where(Function(y)
                                                                Return Utilities.Time.IsDateTimeEqualTillMinutes(y.Key, tickData.Timestamp.Value)
                                                            End Function)
                    If lastExistingPayloads IsNot Nothing AndAlso lastExistingPayloads.Count > 0 Then lastExistingPayload = lastExistingPayloads.LastOrDefault.Value
                End If
                Dim runningPayloads As List(Of OHLCPayload) = New List(Of OHLCPayload)
                Dim tickWasProcessed As Boolean = False
                'Do not touch the payload if it was already processed by historical
                Dim freshCandle As Boolean = False
                If lastExistingPayload IsNot Nothing Then
                    With lastExistingPayload
                        .HighPrice.Value = Math.Max(lastExistingPayload.HighPrice.Value, tickData.LastPrice)
                        .LowPrice.Value = Math.Min(lastExistingPayload.LowPrice.Value, tickData.LastPrice)
                        .ClosePrice.Value = tickData.LastPrice
                        If .PreviousPayload IsNot Nothing Then
                            If .PreviousPayload.SnapshotDateTime.Date = tickData.Timestamp.Value.Date Then
                                .Volume.Value = tickData.Volume - .PreviousPayload.DailyVolume
                            Else
                                .Volume.Value = tickData.Volume
                            End If
                        Else
                            .Volume.Value = tickData.Volume
                        End If
                        .DailyVolume = tickData.Volume
                        .NumberOfTicks += 1
                        .PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick
                    End With
                    tickWasProcessed = True
                ElseIf lastExistingPayload Is Nothing Then
                    'Fresh candle needs to be created
                    freshCandle = True
                    Dim previousCandle As OHLCPayload = Nothing
                    Dim previousCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) =
                        _parentInstrument.RawPayloads.Where(Function(y)
                                                                Return y.Key < tickData.Timestamp.Value AndAlso
                                                                (y.Value.PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick OrElse
                                                                (_parentInstrument.IsHistoricalCompleted AndAlso
                                                                y.Value.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical))
                                                            End Function)

                    If previousCandles IsNot Nothing AndAlso previousCandles.Count > 0 Then
                        previousCandle = previousCandles.OrderByDescending(Function(x)
                                                                               Return x.Key
                                                                           End Function).FirstOrDefault.Value
                        'Print previous candle
                        'Debug.WriteLine(previousCandle.PreviousPayload.ToString)
                        'Debug.WriteLine(previousCandle.ToString)
                    End If

                    'Fill 0 volume Candles
                    'If previousCandle IsNot Nothing AndAlso
                    '    Utilities.Time.GetDateTimeTillMinutes(Now) <= Utilities.Time.GetDateTimeTillMinutes(Me._parentInstrument.ExchangeDetails.ExchangeEndTime) AndAlso
                    '    Not Utilities.Time.IsDateTimeEqualTillMinutes(tickData.Timestamp.Value, previousCandle.SnapshotDateTime.AddMinutes(1)) Then
                    '    Dim timeToCalculateFrom As Date = Date.MinValue
                    '    If previousCandle.SnapshotDateTime < Me._parentInstrument.ExchangeDetails.ExchangeStartTime Then
                    '        timeToCalculateFrom = Me._parentInstrument.ExchangeDetails.ExchangeStartTime
                    '    Else
                    '        timeToCalculateFrom = previousCandle.SnapshotDateTime.AddMinutes(1)
                    '    End If
                    '    Dim timeGap As Integer = DateDiff(DateInterval.Minute, timeToCalculateFrom, tickData.Timestamp.Value)
                    '    Dim fillPayload As OHLCPayload = Nothing
                    '    For time As Integer = 0 To timeGap - 1
                    '        fillPayload = New OHLCPayload(OHLCPayload.PayloadSource.Tick)
                    '        With fillPayload
                    '            .TradingSymbol = _parentInstrument.TradingSymbol
                    '            .OpenPrice.Value = previousCandle.ClosePrice.Value
                    '            .HighPrice.Value = previousCandle.ClosePrice.Value
                    '            .LowPrice.Value = previousCandle.ClosePrice.Value
                    '            .ClosePrice.Value = previousCandle.ClosePrice.Value
                    '            .Volume.Value = 0
                    '            .DailyVolume = tickData.Volume
                    '            .SnapshotDateTime = timeToCalculateFrom.AddMinutes(time)
                    '            .PreviousPayload = previousCandle
                    '            .NumberOfTicks = 1
                    '        End With
                    '        previousCandle = fillPayload
                    '        runningPayloads.Add(fillPayload)
                    '    Next
                    'End If

                    Dim currentPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.Tick)
                    With currentPayload
                        .TradingSymbol = _parentInstrument.TradingSymbol
                        .OpenPrice.Value = tickData.LastPrice
                        .HighPrice.Value = tickData.LastPrice
                        .LowPrice.Value = tickData.LastPrice
                        .ClosePrice.Value = tickData.LastPrice
                        .Volume.Value = tickData.Volume
                        .DailyVolume = tickData.Volume
                        .SnapshotDateTime = Utilities.Time.GetDateTimeTillMinutes(tickData.Timestamp.Value)
                        .PreviousPayload = previousCandle
                        .NumberOfTicks = 1
                    End With
                    runningPayloads.Add(currentPayload)
                    tickWasProcessed = True
                    'Debug.WriteLine(currentPayload.ToString())
                    ''TODO: Below loop is for checking purpose
                    'For Each payload In _parentInstrument.RawPayloads.OrderBy(Function(x)
                    '                                                              Return x.Key
                    '                                                          End Function)
                    '    If payload.Value.PreviousPayload IsNot Nothing Then
                    '        Debug.WriteLine(payload.Value.ToString())
                    '    End If
                    'Next
                    'Debug.WriteLine(currentPayload.ToString)
                End If
                If tickWasProcessed Then 'If not processed would mean that the tick was for a historical candle that was already processed and not for a live candle
                    If runningPayloads IsNot Nothing AndAlso runningPayloads.Count > 0 Then
                        For Each currentRunningPayload In runningPayloads
                            _parentInstrument.RawPayloads.GetOrAdd(currentRunningPayload.SnapshotDateTime, currentRunningPayload)
                        Next
                    Else
                        runningPayloads.Add(lastExistingPayload)
                    End If
                    If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                        For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
                            Await runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsAsync(Me, runningPayloads).ConfigureAwait(False)
                        Next
                    End If
                End If


                'If freshCandle Then
                '    For Each payload In _parentInstrument.RawPayloads.OrderBy(Function(x)
                '                                                                  Return x.Key
                '                                                              End Function)
                '        If payload.Value.PreviousPayload IsNot Nothing Then
                '            Debug.WriteLine(payload.Value.ToString())
                '        End If
                '    Next
                'End If

                'TODO: Below loop is for checking purpose
                'Try
                '    Dim outputConsumer As PayloadToChartConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            If CType(payload.Value, OHLCPayload).PreviousPayload IsNot Nothing Then
                '                Debug.WriteLine(payload.Value.ToString())
                '            End If
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                ''TODO: Below loop is for checking purpose
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.EMAConsumer.EMAPayload).EMA.Value.ToString())
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.LastOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.SupertrendConsumer.SupertrendPayload).Supertrend.Value.ToString())
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
            Catch ex As Exception
                logger.Error("GetChartFromTickAsync:{0}, error:{1}", Me.ToString, ex.ToString)
                Me.ParentController.OrphanException = ex
            Finally
                Interlocked.Exchange(_tickLock, 0)
                'If _tickLock <> 0 Then Throw New ApplicationException("Check why lock is not released")
                'Debug.WriteLine(String.Format("Process Historical after. Time:{0}, Lock:{1}", Now, _lock))
            End Try
        End Function

        Public Overrides Async Function ConvertTimeframeAsync(ByVal timeframe As Integer, ByVal currentPayload As OHLCPayload, ByVal outputConsumer As PayloadToChartConsumer) As Task(Of Date)
            'logger.Debug("{0}->ConvertTimeframeAsync, parameters:{1},{2},{3}", Me.ToString, timeframe, currentPayload.ToString, outputConsumer.ToString)
            Dim ret As Date = Date.MinValue
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim blockDateInThisTimeframe As New Date(currentPayload.SnapshotDateTime.Year,
                                                    currentPayload.SnapshotDateTime.Month,
                                                    currentPayload.SnapshotDateTime.Day,
                                                    currentPayload.SnapshotDateTime.Hour,
                                                    Math.Floor(currentPayload.SnapshotDateTime.Minute / timeframe) * timeframe, 0)

            Dim payloadSource As OHLCPayload.PayloadSource = OHLCPayload.PayloadSource.None
            If currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick Then
                payloadSource = OHLCPayload.PayloadSource.CalculatedTick
            ElseIf currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical Then
                payloadSource = OHLCPayload.PayloadSource.CalculatedHistorical
            End If
            If outputConsumer.ConsumerPayloads Is Nothing Then
                outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
                Dim runninPayload As New OHLCPayload(payloadSource)
                With runninPayload
                    .OpenPrice.Value = currentPayload.OpenPrice.Value
                    .HighPrice.Value = currentPayload.HighPrice.Value
                    .LowPrice.Value = currentPayload.LowPrice.Value
                    .ClosePrice.Value = currentPayload.ClosePrice.Value
                    .DailyVolume = currentPayload.DailyVolume
                    .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
                    .PreviousPayload = Nothing
                    .SnapshotDateTime = blockDateInThisTimeframe
                    .TradingSymbol = currentPayload.TradingSymbol
                    .Volume.Value = currentPayload.Volume.Value
                End With
                outputConsumer.ConsumerPayloads.GetOrAdd(blockDateInThisTimeframe, runninPayload)
                ret = blockDateInThisTimeframe
            Else
                Dim lastExistingPayload As OHLCPayload = Nothing
                If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                    Dim lastExistingPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                    outputConsumer.ConsumerPayloads.Where(Function(x)
                                                              Return x.Key = blockDateInThisTimeframe
                                                          End Function)
                    If lastExistingPayloads IsNot Nothing AndAlso lastExistingPayloads.Count > 0 Then lastExistingPayload = lastExistingPayloads.LastOrDefault.Value
                End If

                If lastExistingPayload IsNot Nothing Then
                    Dim previousPayload As OHLCPayload = Nothing
                    Dim previousPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                        outputConsumer.ConsumerPayloads.Where(Function(y)
                                                                  Return y.Key < blockDateInThisTimeframe 'AndAlso
                                                                  '(CType(y.Value, OHLCPayload).PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick OrElse
                                                                  'Interlocked.Read(_historicalLock) = 0 OrElse
                                                                  '(Interlocked.Read(_historicalLock) = 1 AndAlso
                                                                  'currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical))
                                                              End Function)

                    If previousPayloads IsNot Nothing AndAlso previousPayloads.Count > 0 Then
                        Dim potentialPreviousPayload As OHLCPayload = previousPayloads.OrderByDescending(Function(x)
                                                                                                             Return x.Key
                                                                                                         End Function).FirstOrDefault.Value

                        Select Case currentPayload.PayloadGeneratedBy
                            Case OHLCPayload.PayloadSource.Historical
                                previousPayload = potentialPreviousPayload
                            Case OHLCPayload.PayloadSource.Tick
                                If _parentInstrument.IsHistoricalCompleted Then
                                    previousPayload = potentialPreviousPayload
                                Else
                                    If Utilities.Time.IsDateTimeEqualTillMinutes(potentialPreviousPayload.SnapshotDateTime, lastExistingPayload.SnapshotDateTime.AddMinutes(-timeframe)) Then
                                        previousPayload = potentialPreviousPayload
                                    Else
                                        Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
                                    End If
                                End If
                        End Select
                    End If

                    If previousPayload IsNot Nothing Then
                        ret = blockDateInThisTimeframe
                    End If

                    'Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = From candle In _parentInstrument.RawPayloads
                    '                                                                             Where candle.Key >= blockDateInThisTimeframe AndAlso candle.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
                    '                                                                             Order By candle.Key
                    '                                                                             Select candle

                    Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = _parentInstrument.RawPayloads.Where(Function(x)
                                                                                                                                         Return x.Key >= blockDateInThisTimeframe AndAlso
                                                                                                                                         x.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
                                                                                                                                     End Function).OrderBy(Function(y)
                                                                                                                                                               Return y.Key
                                                                                                                                                           End Function)

                    With lastExistingPayload
                        .OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
                        .HighPrice.Value = timeframeCandles.Max(Function(x) x.Value.HighPrice.Value)
                        .LowPrice.Value = timeframeCandles.Min(Function(x) x.Value.LowPrice.Value)
                        .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
                        .PreviousPayload = previousPayload
                        .Volume.Value = timeframeCandles.Sum(Function(x) x.Value.Volume.Value)
                        .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
                        .PayloadGeneratedBy = payloadSource
                    End With

                    'If currentPayload.SnapshotDateTime = blockDateInThisTimeframe AndAlso currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical Then
                    '    lastExistingPayload.OpenPrice.Value = currentPayload.OpenPrice.Value
                    'End If
                    'With lastExistingPayload
                    '    .HighPrice.Value = Math.Max(.HighPrice.Value, currentPayload.HighPrice.Value)
                    '    .LowPrice.Value = Math.Min(.LowPrice.Value, currentPayload.LowPrice.Value)
                    '    .ClosePrice.Value = currentPayload.ClosePrice.Value
                    '    .PreviousPayload = previousPayload
                    '    If .PreviousPayload IsNot Nothing AndAlso .SnapshotDateTime.Date = .PreviousPayload.SnapshotDateTime.Date Then
                    '        .Volume.Value = currentPayload.DailyVolume - .PreviousPayload.DailyVolume
                    '    Else
                    '        .Volume.Value = currentPayload.DailyVolume
                    '    End If
                    '    .DailyVolume = currentPayload.DailyVolume
                    '    .PayloadGeneratedBy = payloadSource
                    'End With
                Else
                    Dim previousPayload As OHLCPayload = Nothing
                    Dim previousPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                        outputConsumer.ConsumerPayloads.Where(Function(y)
                                                                  Return y.Key < blockDateInThisTimeframe 'AndAlso
                                                                  '(CType(y.Value, OHLCPayload).PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick OrElse
                                                                  'Interlocked.Read(_historicalLock) = 0 OrElse
                                                                  '(Interlocked.Read(_historicalLock) = 1 AndAlso
                                                                  'currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical))
                                                              End Function)
                    If previousPayloads IsNot Nothing AndAlso previousPayloads.Count > 0 Then
                        Dim potentialPreviousPayload As OHLCPayload = previousPayloads.OrderByDescending(Function(x)
                                                                                                             Return x.Key
                                                                                                         End Function).FirstOrDefault.Value

                        Select Case currentPayload.PayloadGeneratedBy
                            Case OHLCPayload.PayloadSource.Historical
                                previousPayload = potentialPreviousPayload
                            Case OHLCPayload.PayloadSource.Tick
                                If _parentInstrument.IsHistoricalCompleted Then
                                    previousPayload = potentialPreviousPayload
                                Else
                                    If Utilities.Time.IsDateTimeEqualTillMinutes(potentialPreviousPayload.SnapshotDateTime, blockDateInThisTimeframe.AddMinutes(-timeframe)) Then
                                        previousPayload = potentialPreviousPayload
                                    Else
                                        Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
                                    End If
                                End If
                        End Select
                    End If

                    If previousPayload IsNot Nothing Then
                        ret = blockDateInThisTimeframe
                    End If

                    Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = _parentInstrument.RawPayloads.Where(Function(x)
                                                                                                                                         Return x.Key >= blockDateInThisTimeframe AndAlso
                                                                                                                                         x.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
                                                                                                                                     End Function).OrderBy(Function(y)
                                                                                                                                                               Return y.Key
                                                                                                                                                           End Function)

                    Dim runningPayload As New OHLCPayload(payloadSource)
                    With runningPayload
                        .SnapshotDateTime = blockDateInThisTimeframe
                        .OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
                        .HighPrice.Value = timeframeCandles.Max(Function(x) x.Value.HighPrice.Value)
                        .LowPrice.Value = timeframeCandles.Min(Function(x) x.Value.LowPrice.Value)
                        .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
                        .PreviousPayload = previousPayload
                        .Volume.Value = timeframeCandles.Sum(Function(x) x.Value.Volume.Value)
                        .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
                        .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
                        .TradingSymbol = currentPayload.TradingSymbol
                    End With

                    'Dim runningPayload As New OHLCPayload(payloadSource)
                    'With runningPayload
                    '    .OpenPrice.Value = currentPayload.OpenPrice.Value
                    '    .HighPrice.Value = currentPayload.HighPrice.Value
                    '    .LowPrice.Value = currentPayload.LowPrice.Value
                    '    .ClosePrice.Value = currentPayload.ClosePrice.Value
                    '    .DailyVolume = currentPayload.DailyVolume
                    '    .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
                    '    .PreviousPayload = previousPayload
                    '    .SnapshotDateTime = blockDateInThisTimeframe
                    '    .TradingSymbol = currentPayload.TradingSymbol
                    '    If .PreviousPayload IsNot Nothing AndAlso
                    '        .SnapshotDateTime.Date = .PreviousPayload.SnapshotDateTime.Date Then
                    '        .Volume.Value = currentPayload.DailyVolume - .PreviousPayload.DailyVolume
                    '    Else
                    '        .Volume.Value = currentPayload.DailyVolume
                    '    End If
                    'End With
                    outputConsumer.ConsumerPayloads.GetOrAdd(runningPayload.SnapshotDateTime, runningPayload)

                    ''TODO: Below loop is for checking purpose
                    'For Each payload In outputConsumer.ChartPayloads.OrderBy(Function(x)
                    '                                                             Return x.Key
                    '                                                         End Function)
                    '    If payload.Value.PreviousPayload IsNot Nothing Then
                    '        Debug.WriteLine(payload.Value.ToString())
                    '    End If
                    'Next

                    'Debug.WriteLine(runninPayload)

                    'Debug.WriteLine(runninPayload.PreviousPayload.ToString)
                End If
            End If
            Return ret
        End Function
        Public Function UpdateHistoricalCandleStick(ByVal runningCandleTime As Date,
                                                    ByVal open As Decimal,
                                                    ByVal high As Decimal,
                                                    ByVal low As Decimal,
                                                    ByVal close As Decimal,
                                                    ByVal volume As Long,
                                                    ByVal previousCandlePayload As OHLCPayload) As OHLCPayload
            With _parentInstrument.RawPayloads(runningCandleTime)
                .PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical
                .SnapshotDateTime = runningCandleTime
                .TradingSymbol = _parentInstrument.TradingSymbol
                .OpenPrice.Value = open
                .HighPrice.Value = high
                .LowPrice.Value = low
                .ClosePrice.Value = close
                .Volume.Value = volume
                If previousCandlePayload IsNot Nothing AndAlso
                    .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
                    .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
                Else
                    .DailyVolume = .Volume.Value
                End If
                .PreviousPayload = previousCandlePayload
            End With
            Return _parentInstrument.RawPayloads(runningCandleTime)
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetType.ToString
        End Function
    End Class
End Namespace