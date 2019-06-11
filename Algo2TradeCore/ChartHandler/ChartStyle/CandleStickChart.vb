Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Controller
Imports NLog
Imports Algo2TradeCore.Strategies

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

#Region "Test"
        'Public Overrides Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object)) As Task
        '    'Exit Function
        '    'logger.Debug("{0}->GetChartFromHistoricalAsync, parameters:{1}", Me.ToString, Utilities.Strings.JsonSerialize(historicalCandlesJSONDict))
        '    Try
        '        While 1 = Interlocked.Exchange(_historicalLock, 1)
        '            Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
        '        End While
        '        'Interlocked.Increment(_historicalLock)
        '        'Debug.WriteLine(String.Format("Process Historical before. Time:{0}, Lock:{1}", Now, _lock))
        '        If historicalCandlesJSONDict.ContainsKey("data") Then
        '            Dim historicalCandlesDict As Dictionary(Of String, Object) = historicalCandlesJSONDict("data")
        '            If historicalCandlesDict.ContainsKey("candles") AndAlso historicalCandlesDict("candles").count > 0 Then
        '                Dim historicalCandles As ArrayList = historicalCandlesDict("candles")
        '                Dim previousCandlePayload As OHLCPayload = Nothing
        '                If _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
        '                    Dim previousCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) =
        '                   _parentInstrument.RawPayloads.Where(Function(y)
        '                                                           Return y.Key < Utilities.Time.GetDateTimeTillMinutes(historicalCandles(0)(0))
        '                                                       End Function)

        '                    If previousCandles IsNot Nothing AndAlso previousCandles.Count > 0 Then
        '                        previousCandlePayload = previousCandles.OrderByDescending(Function(x)
        '                                                                                      Return x.Key
        '                                                                                  End Function).FirstOrDefault.Value
        '                    End If
        '                End If
        '                Dim s As Stopwatch = New Stopwatch
        '                s.Start()
        '                'If Me._subscribedStrategyInstruments IsNot Nothing AndAlso Me._subscribedStrategyInstruments.Count > 0 Then
        '                '    For Each runningStrategyInstrument In _subscribedStrategyInstruments
        '                '        If runningStrategyInstrument.IsPairInstrument And _parentInstrument.ContractType = IInstrument.TypeOfContract.Far Then
        '                '            If runningStrategyInstrument.DependendStratrgyInstruments IsNot Nothing AndAlso
        '                '                runningStrategyInstrument.DependendStratrgyInstruments.Count > 0 Then
        '                '                For Each runningDependendStrategyInstrument In runningStrategyInstrument.DependendStratrgyInstruments
        '                '                    While Not runningDependendStrategyInstrument.TradableInstrument.IsHistoricalCompleted
        '                '                        Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
        '                '                    End While
        '                '                Next
        '                '            End If
        '                '        End If
        '                '    Next
        '                'End If

        '                Dim onwardCandleUpdate As Boolean = False
        '                For Each historicalCandle In historicalCandles
        '                    Dim runningSnapshotTime As Date = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))
        '                    Dim dummy As OHLCPayload = Nothing
        '                    Dim existingOrAddedPayload As OHLCPayload = Nothing
        '                    Dim freshCandleAdded As Boolean = False
        '                    If _parentInstrument.RawPayloads IsNot Nothing Then
        '                        If Not _parentInstrument.RawPayloads.ContainsKey(runningSnapshotTime) Then
        '                            freshCandleAdded = True
        '                            'Create new candle and add it
        '                            Dim runningPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.Historical)
        '                            With runningPayload
        '                                .SnapshotDateTime = runningSnapshotTime
        '                                .TradingSymbol = _parentInstrument.TradingSymbol
        '                                .OpenPrice.Value = historicalCandle(1)
        '                                .HighPrice.Value = historicalCandle(2)
        '                                .LowPrice.Value = historicalCandle(3)
        '                                .ClosePrice.Value = historicalCandle(4)
        '                                .Volume.Value = historicalCandle(5)
        '                                If previousCandlePayload IsNot Nothing AndAlso
        '                                    .SnapshotDateTime.Date = previousCandlePayload.SnapshotDateTime.Date Then
        '                                    .DailyVolume = .Volume.Value + previousCandlePayload.DailyVolume
        '                                Else
        '                                    .DailyVolume = .Volume.Value
        '                                End If
        '                                .PreviousPayload = previousCandlePayload
        '                            End With
        '                            'Now that the candle is created, add it to the collection
        '                            existingOrAddedPayload = _parentInstrument.RawPayloads.GetOrAdd(runningSnapshotTime, runningPayload)
        '                        Else
        '                            'Get the existing already present candle
        '                            existingOrAddedPayload = _parentInstrument.RawPayloads.GetOrAdd(runningSnapshotTime, dummy)
        '                        End If
        '                    End If
        '                    Dim candleNeedsUpdate As Boolean = False
        '                    If existingOrAddedPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick Then
        '                        candleNeedsUpdate = True
        '                        UpdateHistoricalCandleStick(runningSnapshotTime,
        '                                                    historicalCandle(1),
        '                                                    historicalCandle(2),
        '                                                    historicalCandle(3),
        '                                                    historicalCandle(4),
        '                                                    historicalCandle(5),
        '                                                    previousCandlePayload)
        '                    ElseIf onwardCandleUpdate OrElse Not existingOrAddedPayload.Equals(historicalCandle(1),
        '                                                                                        historicalCandle(2),
        '                                                                                        historicalCandle(3),
        '                                                                                        historicalCandle(4),
        '                                                                                        historicalCandle(5),
        '                                                                                        runningSnapshotTime) Then
        '                        candleNeedsUpdate = True
        '                        onwardCandleUpdate = True
        '                    End If
        '                    If candleNeedsUpdate OrElse freshCandleAdded Then
        '                        ''existingOrAddedPayload = runningPayload
        '                        UpdateHistoricalCandleStick(runningSnapshotTime,
        '                                                    historicalCandle(1),
        '                                                    historicalCandle(2),
        '                                                    historicalCandle(3),
        '                                                    historicalCandle(4),
        '                                                    historicalCandle(5),
        '                                                    previousCandlePayload)

        '                        'Since this candle is updated or added fresh, we need to mark this candle dirty
        '                        'Marking dirty would mean chart consumer and subsequently indicator consumers should fire

        '                        _parentInstrument.RawPayloads(runningSnapshotTime).IsDirtyCandle = True

        '                        'Candlestickchart is a one-to-one mapping with instrument
        '                        'One instrument can have several startegyinstruments
        '                        'Now since the historical data is received for the instrument, so all the stratgy instruments need
        '                        'to have their charts created and corresponding indicators fired via the below lines

        '                        Dim consumersToBeCalculated As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = Nothing
        '                        If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
        '                            For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
        '                                'If the runningstrategyinstrument is from a pair trading
        '                                'and is the far contract, then we need to find the near contract candle changes, ie.
        '                                'find the dirty candles of the near contract. This is required as the indicators
        '                                'associated to a pair startgey will depend on changes of the far and near contract
        '                                'While the changes of the far contract can bet detected normally and the indicators fired,
        '                                'but the changes of the near contract should also be detected the corresponding far contracts
        '                                'indicator should be fired as there is a change in the near contract

        '                                ''Check first if we are in far contract
        '                                Dim consumerToCalculateFrom As Date = Date.MaxValue
        '                                'If runningSubscribedStrategyInstrument.IsPairInstrument AndAlso
        '                                '    runningSubscribedStrategyInstrument.DependendStratrgyInstruments IsNot Nothing AndAlso
        '                                '    runningSubscribedStrategyInstrument.DependendStratrgyInstruments.Count > 0 AndAlso
        '                                '    runningSubscribedStrategyInstrument.TradableInstrument.ContractType = IInstrument.TypeOfContract.Far Then

        '                                '    'Get the dirty flag payloads from the near contract or any other dependent strategy instruements of the far contract
        '                                '    For Each runningDependendStrategyInstrument In runningSubscribedStrategyInstrument.DependendStratrgyInstruments
        '                                '        If runningDependendStrategyInstrument.RawPayloadDependentConsumers IsNot Nothing AndAlso
        '                                '            runningDependendStrategyInstrument.RawPayloadDependentConsumers.Count > 0 Then
        '                                '            For Each runningRawPayloadConsumer In runningDependendStrategyInstrument.RawPayloadDependentConsumers
        '                                '                If runningRawPayloadConsumer.TypeOfConsumer = IPayloadConsumer.ConsumerType.Chart Then
        '                                '                    'Get all the dates which are dirty from the dependent stragey instrument/s charts
        '                                '                    consumersToBeCalculated = runningDependendStrategyInstrument.TradableInstrument.RawPayloads.Where(Function(x)
        '                                '                                                                                                                          Return x.Value.IsDirtyCandle = True
        '                                '                                                                                                                      End Function)
        '                                '                    If consumersToBeCalculated IsNot Nothing AndAlso consumersToBeCalculated.Count > 0 Then
        '                                '                        'Select the minimum date as the consumer updates of the far contract needs to start right from the minimum
        '                                '                        consumerToCalculateFrom = consumersToBeCalculated.Min(Function(y)
        '                                '                                                                                  Return y.Key
        '                                '                                                                              End Function)

        '                                '                    End If
        '                                '                End If
        '                                '            Next
        '                                '        End If
        '                                '    Next
        '                                'End If
        '                                consumerToCalculateFrom = New Date(Math.Min(consumerToCalculateFrom.Ticks, runningSnapshotTime.Ticks))
        '                                Dim consumersToBeCalculatedPayloads As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = Nothing
        '                                consumersToBeCalculatedPayloads = _parentInstrument.RawPayloads.Where(Function(x)
        '                                                                                                          Return x.Key <= consumerToCalculateFrom
        '                                                                                                      End Function)
        '                                Dim consumersToBeCalculatedPayload As KeyValuePair(Of Date, OHLCPayload) = Nothing
        '                                If consumersToBeCalculatedPayloads IsNot Nothing AndAlso consumersToBeCalculatedPayloads.Count > 0 Then
        '                                    consumersToBeCalculatedPayload = consumersToBeCalculatedPayloads.OrderBy(Function(x)
        '                                                                                                                 Return x.Key
        '                                                                                                             End Function).LastOrDefault

        '                                    Await runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsAsync(Me, consumersToBeCalculatedPayload.Value).ConfigureAwait(False)
        '                                End If
        '                                consumersToBeCalculated = Utilities.Collections.ConcatSingle(consumersToBeCalculated, New KeyValuePair(Of Date, OHLCPayload)(runningSnapshotTime, _parentInstrument.RawPayloads(runningSnapshotTime)))
        '                            Next
        '                        End If

        '                        'After the consumers have run, we need to reset the flag to false under two conditions
        '                        '1) if the stratgey instrument is not a pair instrument
        '                        '2) if its a pair stratgey and is the far contract (for the other dependent contracts, the far contract takes
        '                        'the responsibility of changing the dirt flag as done above
        '                        'If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
        '                        '    For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
        '                        '        If (Not runningSubscribedStrategyInstrument.IsPairInstrument) OrElse
        '                        '            (runningSubscribedStrategyInstrument.IsPairInstrument AndAlso
        '                        '             runningSubscribedStrategyInstrument.TradableInstrument.ContractType = IInstrument.TypeOfContract.Far) Then
        '                        '            consumersToBeCalculated.All(Function(z)
        '                        '                                            z.Value.IsDirtyCandle = False
        '                        '                                            Return True
        '                        '                                        End Function)
        '                        '        End If
        '                        '    Next
        '                        'End If
        '                    End If
        '                    previousCandlePayload = _parentInstrument.RawPayloads(runningSnapshotTime)
        '                Next
        '                s.Stop()
        '                Debug.WriteLine(String.Format("{0}, Time:{1}", _parentInstrument.TradingSymbol, s.ElapsedMilliseconds))
        '                _parentInstrument.IsHistoricalCompleted = True
        '                ''TODO: Below loop is for checking purpose
        '                'For Each payload In _parentInstrument.RawPayloads.OrderBy(Function(x)
        '                '                                                              Return x.Key
        '                '                                                          End Function)
        '                '    Debug.WriteLine(payload.Value.ToString())
        '                'Next
        '                'Try
        '                '    Dim outputConsumer As PayloadToChartConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault
        '                '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
        '                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
        '                '                                                                        Return x.Key
        '                '                                                                    End Function)
        '                '            If CType(payload.Value, OHLCPayload).PreviousPayload IsNot Nothing Then
        '                '                Debug.WriteLine(payload.Value.ToString())
        '                '            End If
        '                '        Next
        '                '    End If
        '                'Catch ex As Exception
        '                '    Throw ex
        '                'End Try
        '                'Try
        '                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
        '                '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
        '                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
        '                '                                                                        Return x.Key
        '                '                                                                    End Function)
        '                '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.EMAConsumer.EMAPayload).EMA.Value.ToString())
        '                '        Next
        '                '    End If
        '                'Catch ex As Exception
        '                '    Throw ex
        '                'End Try
        '                'Try
        '                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.LastOrDefault
        '                '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
        '                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
        '                '                                                                        Return x.Key
        '                '                                                                    End Function)
        '                '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.SupertrendConsumer.SupertrendPayload).Supertrend.Value.ToString())
        '                '        Next
        '                '    End If
        '                'Catch ex As Exception
        '                '    Throw ex
        '                'End Try
        '                'Try
        '                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.LastOrDefault
        '                '    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
        '                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
        '                '                                                                        Return x.Key
        '                '                                                                    End Function)
        '                '            Debug.WriteLine(payload.Key.ToString + "   " + CType(payload.Value, Indicators.BollingerConsumer.BollingerPayload).HighBollinger.Value.ToString() + "   " + CType(payload.Value, Indicators.BollingerConsumer.BollingerPayload).SMABollinger.Value.ToString() + "   " + CType(payload.Value, Indicators.BollingerConsumer.BollingerPayload).LowBollinger.Value.ToString())
        '                '        Next
        '                '    End If
        '                'Catch ex As Exception
        '                '    Throw ex
        '                'End Try
        '            End If
        '        End If
        '    Catch ex As Exception
        '        logger.Error("GetChartFromHistoricalAsync:{0}, error:{1}", Me.ToString, ex.ToString)
        '        Me.ParentController.OrphanException = ex
        '    Finally
        '        Interlocked.Exchange(_historicalLock, 0)
        '        'Debug.WriteLine(String.Format("Process Historical after. Time:{0}, Lock:{1}", Now, _lock))
        '    End Try
        'End Function
#End Region

        Public Overrides Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object)) As Task
            'Exit Function
            'logger.Debug("{0}->GetChartFromHistoricalAsync, parameters:{1}", Me.ToString, Utilities.Strings.JsonSerialize(historicalCandlesJSONDict))
            Try
                While 1 = Interlocked.Exchange(_historicalLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

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
                            End If
                        End If
                        Dim s As Stopwatch = New Stopwatch
                        s.Start()
                        Dim onwardCandleUpdate As Boolean = False
                        Dim consumerToCallFromThisTime As Date = Date.MaxValue
                        For Each historicalCandle In historicalCandles
                            Dim runningSnapshotTime As Date = Utilities.Time.GetDateTimeTillMinutes(historicalCandle(0))
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
                                UpdateHistoricalCandleStick(runningSnapshotTime,
                                                            historicalCandle(1),
                                                            historicalCandle(2),
                                                            historicalCandle(3),
                                                            historicalCandle(4),
                                                            historicalCandle(5),
                                                            previousCandlePayload)

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
                                UpdateHistoricalCandleStick(runningSnapshotTime,
                                                            historicalCandle(1),
                                                            historicalCandle(2),
                                                            historicalCandle(3),
                                                            historicalCandle(4),
                                                            historicalCandle(5),
                                                            previousCandlePayload)

                                consumerToCallFromThisTime = If(runningSnapshotTime < consumerToCallFromThisTime, runningSnapshotTime, consumerToCallFromThisTime)

                            End If
                            previousCandlePayload = _parentInstrument.RawPayloads(runningSnapshotTime)
                        Next

                        If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 AndAlso consumerToCallFromThisTime <> Date.MaxValue Then
                            For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
                                'Console.WriteLine(String.Format("Historical: Consumer to calculate from: {0}", consumerToCallFromThisTime.ToString))
                                Await runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsAsync(Me, _parentInstrument.RawPayloads(consumerToCallFromThisTime)).ConfigureAwait(False)
                            Next
                        End If
                        _parentInstrument.IsHistoricalCompleted = True
                        s.Stop()
                        Debug.WriteLine(String.Format("{0}, Time:{1}", _parentInstrument.TradingSymbol, s.ElapsedMilliseconds))

                        'If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                        '    For Each runningStrategyInstrument In _subscribedStrategyInstruments
                        '        If runningStrategyInstrument.RawPayloadDependentConsumers IsNot Nothing AndAlso
                        '            runningStrategyInstrument.RawPayloadDependentConsumers.Count > 0 Then
                        '            For Each runningRawPayloadDependentConsumers In runningStrategyInstrument.RawPayloadDependentConsumers
                        '                If runningRawPayloadDependentConsumers.TypeOfConsumer = IPayloadConsumer.ConsumerType.Chart Then
                        '                    Dim outputConsumer As PayloadToChartConsumer = runningRawPayloadDependentConsumers
                        '                    If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                        '                        'For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                        '                        '                                                                Return x.Key
                        '                        '                                                            End Function)
                        '                        '    If CType(payload.Value, OHLCPayload).PreviousPayload IsNot Nothing Then
                        '                        '        Debug.WriteLine(payload.Value.ToString())
                        '                        '    End If
                        '                        'Next
                        '                        Dim dataToPrint As Date = CType(outputConsumer.ConsumerPayloads(outputConsumer.ConsumerPayloads.Keys.Max), OHLCPayload).PreviousPayload.SnapshotDateTime
                        '                        Debug.WriteLine(outputConsumer.ConsumerPayloads(dataToPrint).ToString)
                        '                        If outputConsumer.OnwardLevelConsumers IsNot Nothing AndAlso outputConsumer.OnwardLevelConsumers.Count > 0 Then
                        '                            For Each runningOnwardLevelConsumer In outputConsumer.OnwardLevelConsumers
                        '                                If runningOnwardLevelConsumer.ConsumerPayloads IsNot Nothing AndAlso
                        '                                    runningOnwardLevelConsumer.ConsumerPayloads.Count > 0 AndAlso
                        '                                    runningOnwardLevelConsumer.ConsumerPayloads.ContainsKey(dataToPrint) Then
                        '                                    Debug.WriteLine(runningOnwardLevelConsumer.ConsumerPayloads(dataToPrint).ToString)
                        '                                End If
                        '                            Next
                        '                        End If
                        '                    End If
                        '                End If
                        '            Next
                        '        End If
                        '    Next
                        'End If

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
            If tickData.Timestamp.Value < Me._parentInstrument.ExchangeDetails.ExchangeStartTime OrElse
                tickData.Timestamp.Value > Me._parentInstrument.ExchangeDetails.ExchangeEndTime Then
                Exit Function
            End If

            Try
                While 1 = Interlocked.Exchange(_tickLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                ''Add data to Tick Payload
                'If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                '    For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
                '        If runningSubscribedStrategyInstrument.ParentStrategy.IsTickPopulationNeeded Then
                '            runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsFromTick(Me, tickData.Timestamp.Value)
                '        End If
                '    Next
                'End If

                'Processing candle from tick
                Dim lastExistingPayload As OHLCPayload = Nothing
                If _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
                    Dim lastExistingPayloads As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) =
                        _parentInstrument.RawPayloads.Where(Function(y)
                                                                Return Utilities.Time.IsDateTimeEqualTillMinutes(y.Key, tickData.Timestamp.Value)
                                                            End Function)
                    If lastExistingPayloads IsNot Nothing AndAlso lastExistingPayloads.Count > 0 Then lastExistingPayload = lastExistingPayloads.LastOrDefault.Value
                End If

                Dim runningPayload As OHLCPayload = Nothing
                Dim tickWasProcessed As Boolean = False
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
                    End If

                    runningPayload = New OHLCPayload(OHLCPayload.PayloadSource.Tick)
                    With runningPayload
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
                    tickWasProcessed = True
                End If
                If tickWasProcessed Then 'If not processed would mean that the tick was for a historical candle that was already processed and not for a live candle
                    If runningPayload IsNot Nothing Then
                        _parentInstrument.RawPayloads.GetOrAdd(runningPayload.SnapshotDateTime, runningPayload)
                    Else
                        runningPayload = lastExistingPayload
                    End If
                    If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                        For Each runningSubscribedStrategyInstrument In _subscribedStrategyInstruments
                            'Console.WriteLine(String.Format("Tick: Consumer to calculate from: {0}", runningPayload.SnapshotDateTime.ToString))
                            Await runningSubscribedStrategyInstrument.PopulateChartAndIndicatorsAsync(Me, runningPayload).ConfigureAwait(False)
                        Next
                    End If
                End If

                'If freshCandle Then
                '    For Each payload In _parentInstrument.RawPayloads.OrderBy(Function(x)
                '                                                                  Return x.Key
                '                                                              End Function)
                '        Debug.WriteLine(payload.Value.ToString())
                '    Next
                'End If

                ''TODO: Below loop is for checking purpose
                'Try
                '    Dim outputConsumer As PayloadToChartConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine(payload.Value.ToString())
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                'TODO: Below loop is for checking purpose
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine("20" & payload.Key.ToString & vbTab & payload.Value.ToString())
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
                '            Debug.WriteLine("50" & payload.Key.ToString & vbTab & payload.Value.ToString())
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.DependendStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine(payload.Key.ToString & vbTab & CType(payload.Value, Indicators.SpreadRatioConsumer.SpreadRatioPayload).ToString)
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.DependendStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault.OnwardLevelConsumers.FirstOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Dim currentPayloadValue As Indicators.BollingerConsumer.BollingerPayload = payload.Value
                '            Debug.WriteLine(String.Format("{0}{1}{2}", payload.Key.ToString, vbTab, currentPayloadValue.ToString))
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
                'Try
                '    Dim outputConsumer As PayloadToIndicatorConsumer = _subscribedStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault.OnwardLevelConsumers.LastOrDefault
                '    If freshCandle AndAlso outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
                '        For Each payload In outputConsumer.ConsumerPayloads.OrderBy(Function(x)
                '                                                                        Return x.Key
                '                                                                    End Function)
                '            Debug.WriteLine(String.Format("{0}{1}{2}", payload.Key.ToString, ",", payload.Value.ToString))
                '        Next
                '    End If
                'Catch ex As Exception
                '    Throw ex
                'End Try
                'Try
                '    If freshCandle AndAlso _subscribedStrategyInstruments.FirstOrDefault.DependendStrategyInstruments.FirstOrDefault.IsPairInstrument Then
                '        Dim outputConsumer As PayloadToPairConsumer = _subscribedStrategyInstruments.FirstOrDefault.DependendStrategyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault
                '        If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 AndAlso
                '            outputConsumer.ConsumerPayloads.ContainsKey(Utilities.Time.GetDateTimeTillMinutes(tickData.Timestamp.Value)) Then
                '            Dim currentCandleTime As Date = Utilities.Time.GetDateTimeTillMinutes(tickData.Timestamp.Value)
                '            Debug.WriteLine(String.Format("{0}_{1}", Now.ToLongTimeString, outputConsumer.ConsumerPayloads(currentCandleTime).ToString))
                '            Dim previousCandleTime As Date = currentCandleTime.AddMinutes(-1)
                '            If outputConsumer.ConsumerPayloads.ContainsKey(previousCandleTime) Then
                '                Debug.WriteLine(String.Format("{0}_{1}", Now.ToLongTimeString, outputConsumer.ConsumerPayloads(previousCandleTime).ToString))
                '            End If
                '        End If
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

        'Public Overrides Function ConvertTimeframe(ByVal timeframe As Integer, ByVal currentPayload As OHLCPayload, ByVal outputConsumer As PayloadToChartConsumer) As Date
        '    'logger.Debug("{0}->ConvertTimeframeAsync, parameters:{1},{2},{3}", Me.ToString, timeframe, currentPayload.ToString, outputConsumer.ToString)
        '    Dim ret As Date = Date.MaxValue
        '    'If currentPayload IsNot Nothing AndAlso _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
        '    '    Dim requiredDataSet As IEnumerable(Of Date) = _parentInstrument.RawPayloads.Keys.Where(Function(x)
        '    '                                                                                               Return x >= currentPayload.SnapshotDateTime
        '    '                                                                                           End Function)

        '    '    For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
        '    '                                                             Return x
        '    '                                                         End Function)

        '    '        currentPayload = _parentInstrument.RawPayloads(runningInputDate)

        '    Dim blockDateInThisTimeframe As New Date(currentPayload.SnapshotDateTime.Year,
        '                                            currentPayload.SnapshotDateTime.Month,
        '                                            currentPayload.SnapshotDateTime.Day,
        '                                            currentPayload.SnapshotDateTime.Hour,
        '                                            Math.Floor(currentPayload.SnapshotDateTime.Minute / timeframe) * timeframe, 0)

        '    Dim payloadSource As OHLCPayload.PayloadSource = OHLCPayload.PayloadSource.None
        '    If currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Tick Then
        '        payloadSource = OHLCPayload.PayloadSource.CalculatedTick
        '    ElseIf currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.Historical Then
        '        payloadSource = OHLCPayload.PayloadSource.CalculatedHistorical
        '    End If
        '    If outputConsumer.ConsumerPayloads Is Nothing Then
        '        outputConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
        '        Dim runninPayload As New OHLCPayload(payloadSource)
        '        With runninPayload
        '            .OpenPrice.Value = currentPayload.OpenPrice.Value
        '            .HighPrice.Value = currentPayload.HighPrice.Value
        '            .LowPrice.Value = currentPayload.LowPrice.Value
        '            .ClosePrice.Value = currentPayload.ClosePrice.Value
        '            .DailyVolume = currentPayload.DailyVolume
        '            .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
        '            .PreviousPayload = Nothing
        '            .SnapshotDateTime = blockDateInThisTimeframe
        '            .TradingSymbol = currentPayload.TradingSymbol
        '            .Volume.Value = currentPayload.Volume.Value
        '        End With
        '        outputConsumer.ConsumerPayloads.GetOrAdd(blockDateInThisTimeframe, runninPayload)
        '        ret = Date.MaxValue
        '    Else
        '        Dim lastExistingPayload As OHLCPayload = Nothing
        '        If outputConsumer.ConsumerPayloads IsNot Nothing AndAlso outputConsumer.ConsumerPayloads.Count > 0 Then
        '            Dim lastExistingPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
        '            outputConsumer.ConsumerPayloads.Where(Function(x)
        '                                                      Return x.Key = blockDateInThisTimeframe
        '                                                  End Function)
        '            If lastExistingPayloads IsNot Nothing AndAlso lastExistingPayloads.Count > 0 Then lastExistingPayload = lastExistingPayloads.LastOrDefault.Value
        '        End If

        '        If lastExistingPayload IsNot Nothing Then
        '            Dim previousPayload As OHLCPayload = Nothing
        '            Dim previousPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
        '                    outputConsumer.ConsumerPayloads.Where(Function(y)
        '                                                              Return y.Key < blockDateInThisTimeframe
        '                                                          End Function)

        '            If previousPayloads IsNot Nothing AndAlso previousPayloads.Count > 0 Then
        '                Dim potentialPreviousPayload As OHLCPayload = previousPayloads.OrderByDescending(Function(x)
        '                                                                                                     Return x.Key
        '                                                                                                 End Function).FirstOrDefault.Value

        '                Select Case currentPayload.PayloadGeneratedBy
        '                    Case OHLCPayload.PayloadSource.Historical
        '                        previousPayload = potentialPreviousPayload
        '                    Case OHLCPayload.PayloadSource.Tick
        '                        If _parentInstrument.IsHistoricalCompleted Then
        '                            previousPayload = potentialPreviousPayload
        '                        Else
        '                            If Utilities.Time.IsDateTimeEqualTillMinutes(potentialPreviousPayload.SnapshotDateTime, lastExistingPayload.SnapshotDateTime.AddMinutes(-timeframe)) Then
        '                                previousPayload = potentialPreviousPayload
        '                            Else
        '                                Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
        '                            End If
        '                        End If
        '                End Select
        '            End If

        '            If previousPayload IsNot Nothing Then
        '                ret = If(blockDateInThisTimeframe < ret, blockDateInThisTimeframe, ret)
        '            End If

        '            Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = _parentInstrument.RawPayloads.Where(Function(x)
        '                                                                                                                                 Return x.Key >= blockDateInThisTimeframe AndAlso
        '                                                                                                                                 x.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
        '                                                                                                                             End Function).OrderBy(Function(y)
        '                                                                                                                                                       Return y.Key
        '                                                                                                                                                   End Function)

        '            With lastExistingPayload
        '                '.OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
        '                '.HighPrice.Value = timeframeCandles.Max(Function(x) x.Value.HighPrice.Value)
        '                '.LowPrice.Value = timeframeCandles.Min(Function(x) x.Value.LowPrice.Value)
        '                '.ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
        '                '.PreviousPayload = previousPayload
        '                '.Volume.Value = timeframeCandles.Sum(Function(x) x.Value.Volume.Value)
        '                '.DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
        '                '.PayloadGeneratedBy = payloadSource

        '                .OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
        '                .HighPrice.Value = timeframeCandles.Max(Function(x)
        '                                                            Return CType(x.Value.HighPrice.Value, Decimal)
        '                                                        End Function)
        '                .LowPrice.Value = timeframeCandles.Min(Function(x)
        '                                                           Return CType(x.Value.LowPrice.Value, Decimal)
        '                                                       End Function)
        '                .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
        '                .PreviousPayload = previousPayload
        '                .Volume.Value = timeframeCandles.Sum(Function(x)
        '                                                         Return CType(x.Value.Volume.Value, Long)
        '                                                     End Function)
        '                .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
        '                .PayloadGeneratedBy = payloadSource
        '            End With
        '        Else
        '            Dim previousPayload As OHLCPayload = Nothing
        '            Dim previousPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
        '                outputConsumer.ConsumerPayloads.Where(Function(y)
        '                                                          Return y.Key < blockDateInThisTimeframe
        '                                                      End Function)
        '            If previousPayloads IsNot Nothing AndAlso previousPayloads.Count > 0 Then
        '                Dim potentialPreviousPayload As OHLCPayload = previousPayloads.OrderByDescending(Function(x)
        '                                                                                                     Return x.Key
        '                                                                                                 End Function).FirstOrDefault.Value

        '                Select Case currentPayload.PayloadGeneratedBy
        '                    Case OHLCPayload.PayloadSource.Historical
        '                        previousPayload = potentialPreviousPayload
        '                    Case OHLCPayload.PayloadSource.Tick
        '                        If _parentInstrument.IsHistoricalCompleted Then
        '                            previousPayload = potentialPreviousPayload
        '                        Else
        '                            If Utilities.Time.IsDateTimeEqualTillMinutes(potentialPreviousPayload.SnapshotDateTime, blockDateInThisTimeframe.AddMinutes(-timeframe)) Then
        '                                previousPayload = potentialPreviousPayload
        '                            Else
        '                                Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
        '                            End If
        '                        End If
        '                End Select
        '            End If

        '            If previousPayload IsNot Nothing Then
        '                ret = If(blockDateInThisTimeframe < ret, blockDateInThisTimeframe, ret)
        '            End If

        '            Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = _parentInstrument.RawPayloads.Where(Function(x)
        '                                                                                                                                 Return x.Key >= blockDateInThisTimeframe AndAlso
        '                                                                                                                                 x.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
        '                                                                                                                             End Function).OrderBy(Function(y)
        '                                                                                                                                                       Return y.Key
        '                                                                                                                                                   End Function)

        '            Dim runningPayload As New OHLCPayload(payloadSource)
        '            With runningPayload
        '                '.SnapshotDateTime = blockDateInThisTimeframe
        '                '.OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
        '                '.HighPrice.Value = timeframeCandles.Max(Function(x) x.Value.HighPrice.Value)
        '                '.LowPrice.Value = timeframeCandles.Min(Function(x) x.Value.LowPrice.Value)
        '                '.ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
        '                '.PreviousPayload = previousPayload
        '                '.Volume.Value = timeframeCandles.Sum(Function(x) x.Value.Volume.Value)
        '                '.DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
        '                '.NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
        '                '.TradingSymbol = currentPayload.TradingSymbol

        '                .SnapshotDateTime = blockDateInThisTimeframe
        '                .OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
        '                .HighPrice.Value = timeframeCandles.Max(Function(x)
        '                                                            Return CType(x.Value.HighPrice.Value, Decimal)
        '                                                        End Function)
        '                .LowPrice.Value = timeframeCandles.Min(Function(x)
        '                                                           Return CType(x.Value.LowPrice.Value, Decimal)
        '                                                       End Function)
        '                .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
        '                .PreviousPayload = previousPayload
        '                .Volume.Value = timeframeCandles.Sum(Function(x)
        '                                                         Return CType(x.Value.Volume.Value, Long)
        '                                                     End Function)
        '                .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
        '                .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
        '                .TradingSymbol = currentPayload.TradingSymbol
        '            End With

        '            outputConsumer.ConsumerPayloads.GetOrAdd(runningPayload.SnapshotDateTime, runningPayload)
        '        End If
        '    End If
        '    '    Next
        '    'End If
        '    'Console.WriteLine(String.Format("Convert Time frame return: {0}", ret.ToString))
        '    Return ret
        'End Function

        Public Overrides Function ConvertTimeframe(ByVal timeframe As Integer, ByVal currentPayload As OHLCPayload, ByVal outputConsumer As PayloadToChartConsumer) As Date
            'logger.Debug("{0}->ConvertTimeframeAsync, parameters:{1},{2},{3}", Me.ToString, timeframe, currentPayload.ToString, outputConsumer.ToString)
            Dim ret As Date = Date.MaxValue
            If currentPayload IsNot Nothing AndAlso _parentInstrument.RawPayloads IsNot Nothing AndAlso _parentInstrument.RawPayloads.Count > 0 Then
                Dim requiredDataSet As IEnumerable(Of Date) = _parentInstrument.RawPayloads.Keys.Where(Function(x)
                                                                                                           Return x >= currentPayload.SnapshotDateTime
                                                                                                       End Function)

                For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                         Return x
                                                                     End Function)

                    currentPayload = _parentInstrument.RawPayloads(runningInputDate)

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
                        ret = Date.MaxValue
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
                                                                      Return y.Key < blockDateInThisTimeframe
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
                                                'Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
                                            End If
                                        End If
                                End Select
                            End If

                            If previousPayload IsNot Nothing Then
                                ret = If(blockDateInThisTimeframe < ret, blockDateInThisTimeframe, ret)
                            End If

                            Dim timeframeCandles As IEnumerable(Of KeyValuePair(Of Date, OHLCPayload)) = _parentInstrument.RawPayloads.Where(Function(x)
                                                                                                                                                 Return x.Key >= blockDateInThisTimeframe AndAlso
                                                                                                                                         x.Key < blockDateInThisTimeframe.AddMinutes(timeframe)
                                                                                                                                             End Function).OrderBy(Function(y)
                                                                                                                                                                       Return y.Key
                                                                                                                                                                   End Function)

                            With lastExistingPayload
                                .OpenPrice.Value = timeframeCandles.FirstOrDefault.Value.OpenPrice.Value
                                .HighPrice.Value = timeframeCandles.Max(Function(x)
                                                                            Return CType(x.Value.HighPrice.Value, Decimal)
                                                                        End Function)
                                .LowPrice.Value = timeframeCandles.Min(Function(x)
                                                                           Return CType(x.Value.LowPrice.Value, Decimal)
                                                                       End Function)
                                .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
                                .PreviousPayload = previousPayload
                                .Volume.Value = timeframeCandles.Sum(Function(x)
                                                                         Return CType(x.Value.Volume.Value, Long)
                                                                     End Function)
                                .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
                                .PayloadGeneratedBy = payloadSource
                            End With
                        Else
                            Dim previousPayload As OHLCPayload = Nothing
                            Dim previousPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                        outputConsumer.ConsumerPayloads.Where(Function(y)
                                                                  Return y.Key < blockDateInThisTimeframe
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
                                                'Debug.WriteLine(String.Format("******************************************* candle not processed. {0}", currentPayload.PayloadGeneratedBy.ToString))
                                            End If
                                        End If
                                End Select
                            End If

                            If previousPayload IsNot Nothing Then
                                ret = If(blockDateInThisTimeframe < ret, blockDateInThisTimeframe, ret)
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
                                .HighPrice.Value = timeframeCandles.Max(Function(x)
                                                                            Return CType(x.Value.HighPrice.Value, Decimal)
                                                                        End Function)
                                .LowPrice.Value = timeframeCandles.Min(Function(x)
                                                                           Return CType(x.Value.LowPrice.Value, Decimal)
                                                                       End Function)
                                .ClosePrice.Value = timeframeCandles.LastOrDefault.Value.ClosePrice.Value
                                .PreviousPayload = previousPayload
                                .Volume.Value = timeframeCandles.Sum(Function(x)
                                                                         Return CType(x.Value.Volume.Value, Long)
                                                                     End Function)
                                .DailyVolume = timeframeCandles.LastOrDefault.Value.DailyVolume
                                .NumberOfTicks = 0 ' Cannot caluclated as histrical will not have the value
                                .TradingSymbol = currentPayload.TradingSymbol
                            End With

                            outputConsumer.ConsumerPayloads.GetOrAdd(runningPayload.SnapshotDateTime, runningPayload)
                        End If
                    End If
                Next
            End If
            'Console.WriteLine(String.Format("Convert Time frame return: {0}", ret.ToString))
            Return ret
        End Function

        Public Function UpdateHistoricalCandleStick(ByVal runningCandleTime As Date,
                                                    ByVal open As Decimal,
                                                    ByVal high As Decimal,
                                                    ByVal low As Decimal,
                                                    ByVal close As Decimal,
                                                    ByVal volume As Long,
                                                    ByVal previousCandlePayload As OHLCPayload) As OHLCPayload
            If runningCandleTime <> Date.MinValue Then
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
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetType.ToString
        End Function
    End Class
End Namespace