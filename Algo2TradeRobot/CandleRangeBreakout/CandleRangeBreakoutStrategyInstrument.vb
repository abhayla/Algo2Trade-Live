Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class CandleRangeBreakoutStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _lastPrevPayloadInnerPlaceOrder As String = ""
    Private _lastPlacedOrder As IBusinessOrder = Nothing
    Public Sub New(ByVal associatedInstrument As IInstrument,
                   ByVal associatedParentStrategy As Strategy,
                   ByVal isPairInstrumnet As Boolean,
                   ByVal canceller As CancellationTokenSource)
        MyBase.New(associatedInstrument, associatedParentStrategy, isPairInstrumnet, canceller)
        Select Case Me.ParentStrategy.ParentController.BrokerSource
            Case APISource.Zerodha
                _APIAdapter = New ZerodhaAdapter(ParentStrategy.ParentController, _cts)
            Case APISource.Upstox
                Throw New NotImplementedException
            Case APISource.None
                Throw New NotImplementedException
        End Select
        AddHandler _APIAdapter.Heartbeat, AddressOf OnHeartbeat
        AddHandler _APIAdapter.WaitingFor, AddressOf OnWaitingFor
        AddHandler _APIAdapter.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        AddHandler _APIAdapter.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
        RawPayloadDependentConsumers = New List(Of IPayloadConsumer)
        If Me.ParentStrategy.IsStrategyCandleStickBased Then
            If Me.ParentStrategy.UserSettings.SignalTimeFrame > 0 Then
                Dim chartConsumer As PayloadToChartConsumer = New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                RawPayloadDependentConsumers.Add(chartConsumer)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As CandleRangeBreakoutUserInputs = Me.ParentStrategy.UserSettings
            Dim minTargetPoint As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MinTargetPoint
            Dim maxStoplossPoint As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStoplossPoint
            Dim maxStockProfit As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStockProfit
            Dim maxStockLoss As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStockLoss
            Dim reverseTrade As Boolean = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).ReverseTrade

            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                'MTM exit block start
                If Me.GetOverallPLAfterBrokerage() >= maxStockProfit Then
                    Await ForceExitAllTradesAsync("Stock Max Profit reached").ConfigureAwait(False)
                ElseIf Me.GetOverallPLAfterBrokerage() <= Math.Abs(maxStockLoss) * -1 Then
                    Await ForceExitAllTradesAsync("Stock Max Loss reached").ConfigureAwait(False)
                End If
                'MTM exit block end

                _cts.Token.ThrowIfCancellationRequested()
                Dim activeOrder As IBusinessOrder = Me.GetActiveOrder(IOrder.TypeOfTransaction.None)
                'Check Target block start
                If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 Then
                    If activeOrder.SLOrder.Count > 1 Then
                        Throw New ApplicationException("Why sl order greater than 1")
                    Else
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            Dim target As Decimal = activeOrder.ParentOrder.AveragePrice
                            Dim targetPoint As Decimal = (activeOrder.ParentOrder.AveragePrice - activeOrder.SLOrder.FirstOrDefault.TriggerPrice) * 2
                            If targetPoint > minTargetPoint AndAlso targetPoint < maxStoplossPoint * 2 Then
                                target += targetPoint
                            ElseIf targetPoint > maxStoplossPoint * 2 Then
                                target += maxStoplossPoint * 2
                            Else
                                target += minTargetPoint
                            End If

                            If Me.TradableInstrument.LastTick.LastPrice >= target Then
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            Dim target As Decimal = activeOrder.ParentOrder.AveragePrice
                            Dim targetPoint As Decimal = (activeOrder.SLOrder.FirstOrDefault.TriggerPrice - activeOrder.ParentOrder.AveragePrice) * 2
                            If targetPoint > minTargetPoint AndAlso targetPoint < maxStoplossPoint * 2 Then
                                target -= targetPoint
                            ElseIf targetPoint > maxStoplossPoint * 2 Then
                                target -= maxStoplossPoint * 2
                            Else
                                target -= minTargetPoint
                            End If

                            If Me.TradableInstrument.LastTick.LastPrice <= target Then
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                            End If
                        End If
                    End If
                End If
                'Check Target block start

                _cts.Token.ThrowIfCancellationRequested()
                'Check Stoploss block start -------------- Only for paper trade
                If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 Then
                    If activeOrder.SLOrder.Count > 1 Then
                        Throw New ApplicationException("Why sl order greater than 1")
                    Else
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            If Me.TradableInstrument.LastTick.LastPrice <= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If Me.TradableInstrument.LastTick.LastPrice >= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        End If
                    End If
                End If
                'Check Stoploss block start

                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    If reverseTrade AndAlso _lastPlacedOrder IsNot Nothing AndAlso
                        _lastPlacedOrder.ParentOrder.TransactionType <> placeOrderTrigger.Item2.EntryDirection AndAlso
                        _lastPlacedOrder.SLOrder IsNot Nothing AndAlso _lastPlacedOrder.SLOrder.Count > 0 Then
                        Await ForceExitSpecificTradeAsync(_lastPlacedOrder.SLOrder.FirstOrDefault, "Force exit order for reverse entry").ConfigureAwait(False)
                    End If
                    Dim modifiedPlaceOrderTrigger As Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String) = New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(placeOrderTrigger.Item1, Me, placeOrderTrigger.Item2, placeOrderTrigger.Item3)
                    Dim placeOrderResponse As IBusinessOrder = Await TakePaperTradeAsync(modifiedPlaceOrderTrigger).ConfigureAwait(False)
                    _lastPlacedOrder = placeOrderResponse
                    If placeOrderResponse IsNot Nothing Then
                        Dim target As Decimal = placeOrderResponse.ParentOrder.AveragePrice
                        If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            Dim targetPoint As Decimal = (placeOrderResponse.ParentOrder.AveragePrice - placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice) * 2
                            If targetPoint > minTargetPoint AndAlso targetPoint < maxStoplossPoint * 2 Then
                                target += targetPoint
                            ElseIf targetPoint >= maxStoplossPoint * 2 Then
                                target += maxStoplossPoint * 2
                            Else
                                target += minTargetPoint
                            End If
                        ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            Dim targetPoint As Decimal = (placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice - placeOrderResponse.ParentOrder.AveragePrice) * 2
                            If targetPoint > minTargetPoint AndAlso targetPoint < maxStoplossPoint * 2 Then
                                target -= targetPoint
                            ElseIf targetPoint >= maxStoplossPoint * 2 Then
                                target -= maxStoplossPoint * 2
                            Else
                                target -= minTargetPoint
                            End If
                        End If
                        Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Direction:{1}, Signal Candle Time:{2}, Top Wick %:{3}, Bottom Wick %:{4}, Entry Price:{5}, Quantity:{6}, Stoploss Price:{7}, Target Price:{8}, Timestamp:{9}",
                                                              Me.TradableInstrument.TradingSymbol,
                                                              placeOrderResponse.ParentOrder.TransactionType.ToString,
                                                              placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToShortTimeString,
                                                              placeOrderTrigger.Item2.Supporting(0),
                                                              placeOrderTrigger.Item2.Supporting(1),
                                                              placeOrderResponse.ParentOrder.AveragePrice,
                                                              placeOrderResponse.ParentOrder.Quantity,
                                                              placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice,
                                                              target,
                                                              Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                    'Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularMarketCNCOrder, Nothing).ConfigureAwait(False)
                    'If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                    '    placeOrderResponse("data").ContainsKey("order_id") Then

                    'End If
                End If
                'Place Order block end

                Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
            End While
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function

    Public Overrides Function MonitorAsync(command As ExecuteCommands, data As Object) As Task
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As CandleRangeBreakoutUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTime As Date = Now()
        Dim minTargetPoint As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MinTargetPoint
        Dim maxStoplossPoint As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStoplossPoint
        Dim maxStockProfit As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStockProfit
        Dim maxStockLoss As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStockLoss
        Dim reverseTrade As Boolean = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).ReverseTrade
        Dim currentLTP As Decimal = Me.TradableInstrument.LastTick.LastPrice

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
                    _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Max Stock Profit:{5}, Max Stock Loss:{6}, Stock PL:{7}, Max Profit Per Day:{8}, Max Loss Per Day:{9}, Overall PL:{10}, Is Active Instrument:{11}, Reverse Trade:{12}, Top Wick %:{13}, Bottom Wick %:{14}, Current Time:{15}, Current LTP:{16}, TradingSymbol:{17}",
                    userSettings.TradeStartTime.ToString,
                    userSettings.LastTradeEntryTime.ToString,
                    runningCandlePayload.SnapshotDateTime.ToString,
                    runningCandlePayload.PayloadGeneratedBy.ToString,
                    Me.TradableInstrument.IsHistoricalCompleted,
                    maxStockProfit,
                    maxStockLoss,
                    Me.GetOverallPLAfterBrokerage(),
                    userSettings.MaxProfitPerDay,
                    userSettings.MaxLossPerDay,
                    Me.ParentStrategy.GetTotalPLAfterBrokerage,
                    IsActiveInstrument(),
                    reverseTrade,
                    (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                    (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                    currentTime.ToString,
                    currentLTP,
                    Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Me.GetOverallPLAfterBrokerage() < maxStockProfit AndAlso Me.GetOverallPLAfterBrokerage() > Math.Abs(maxStockLoss) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage < userSettings.MaxProfitPerDay AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage > Math.Abs(userSettings.MaxLossPerDay) * -1 Then
            If reverseTrade OrElse Not IsActiveInstrument() Then
                Dim higherTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).HigherTailPercentage
                Dim lowerTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowerTailPercentage
                If (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
                    (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then
                    Dim potentialEntryPrice As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value
                    potentialEntryPrice += CalculateBuffer(potentialEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                    Dim stoploss As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value
                    stoploss -= CalculateBuffer(stoploss, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                    If (potentialEntryPrice - stoploss) <= maxStoplossPoint Then
                        If currentLTP >= potentialEntryPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
                                     .TriggerPrice = stoploss,
                                     .Supporting = New List(Of Object)}

                            parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                            parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                        End If
                    Else
                        Try
                            If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadInnerPlaceOrder Then
                                _lastPrevPayloadInnerPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                                logger.Debug("Place Order-> Stoploss is greater than max stoploss point. Direction:{0}, Entry Price:{1}, Stoploss:{2}, SLPoint:{3}, Max Stoploss Point:{4}",
                                             "Buy", potentialEntryPrice, stoploss, potentialEntryPrice - stoploss, maxStoplossPoint)
                            End If
                        Catch ex As Exception
                            logger.Error(ex)
                        End Try
                    End If
                ElseIf (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
                    (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then
                    Dim potentialEntryPrice As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value
                    potentialEntryPrice -= CalculateBuffer(potentialEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                    Dim stoploss As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value
                    stoploss += CalculateBuffer(stoploss, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                    If (stoploss - potentialEntryPrice) <= maxStoplossPoint Then
                        If currentLTP <= potentialEntryPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
                                     .TriggerPrice = stoploss,
                                     .Supporting = New List(Of Object)}

                            parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                            parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                        End If
                    Else
                        Try
                            If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadInnerPlaceOrder Then
                                _lastPrevPayloadInnerPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                                logger.Debug("Place Order-> Stoploss is greater than max stoploss point. Direction:{0}, Entry Price:{1}, Stoploss:{2}, SLPoint:{3}, Max Stoploss Point:{4}",
                                             "Sell", potentialEntryPrice, stoploss, stoploss - potentialEntryPrice, maxStoplossPoint)
                            End If
                        Catch ex As Exception
                            logger.Error(ex)
                        End Try
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                If forcePrint Then
                    logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    If Me.TradableInstrument.IsHistoricalCompleted Then
                        logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                        logger.Debug("PlaceOrder-> Rest all parameters: 
                                    Trade Start Time:{0}, Last Trade Entry Time:{1}, 
                                    RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, 
                                    Max Stock Profit:{5}, Max Stock Loss:{6}, Stock PL:{7}, 
                                    Max Profit Per Day:{8}, Max Loss Per Day:{9}, Overall PL:{10}, 
                                    Is Active Instrument:{11}, Reverse Trade:{12}, 
                                    Top Wick %:{13}, Bottom Wick %:{14}, 
                                    Current Time:{15}, Current LTP:{16}, 
                                    TradingSymbol:{17}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    maxStockProfit,
                                    maxStockLoss,
                                    Me.GetOverallPLAfterBrokerage(),
                                    userSettings.MaxProfitPerDay,
                                    userSettings.MaxLossPerDay,
                                    Me.ParentStrategy.GetTotalPLAfterBrokerage,
                                    IsActiveInstrument(),
                                    reverseTrade,
                                    (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                                    (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                                    currentTime.ToString,
                                    currentLTP,
                                    Me.TradableInstrument.TradingSymbol)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex)
            End Try

            Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
            If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                If currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                    currentSignalActivities.FirstOrDefault.EntryActivity.LastException IsNot Nothing AndAlso
                    currentSignalActivities.FirstOrDefault.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "Condition Satisfied")
                ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
                    'ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                    '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
                Else
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "Condition Satisfied")
                End If
            Else
                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
            End If
        Else
            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        End If
        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete AndAlso
            Not order.Status = IOrder.TypeOfStatus.Cancelled AndAlso
            Not order.Status = IOrder.TypeOfStatus.Rejected Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }

            Dim exitOrderResponse As IBusinessOrder = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)

            If exitOrderResponse IsNot Nothing AndAlso exitOrderResponse.AllOrder IsNot Nothing AndAlso exitOrderResponse.AllOrder.Count > 0 Then
                For Each runningSLOrder In exitOrderResponse.AllOrder
                    Dim message As String = String.Format("{0}. Trading Symbol:{1}, Direction:{2}, Entry Price:{3}, Quantity:{4}, Exit Price:{5}, Order PL:{6}, Total Stock PL:{7}, Overall PL:{8}, Timestamp:{9}",
                                                              reason,
                                                              Me.TradableInstrument.TradingSymbol,
                                                              exitOrderResponse.ParentOrder.TransactionType.ToString,
                                                              exitOrderResponse.ParentOrder.AveragePrice,
                                                              exitOrderResponse.ParentOrder.Quantity,
                                                              exitOrderResponse.AllOrder.FirstOrDefault.AveragePrice,
                                                              Me.GetTotalPLOfAnOrderAfterBrokerage(exitOrderResponse.ParentOrderIdentifier),
                                                              Me.GetOverallPLAfterBrokerage(),
                                                              Me.ParentStrategy.GetTotalPLAfterBrokerage,
                                                              Now)
                    GenerateTelegramMessageAsync(message)
                Next
            End If
        End If
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As CandleRangeBreakoutUserInputs = Me.ParentStrategy.UserSettings
        If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
            userInputs.TelegramChatID IsNot Nothing AndAlso Not userInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
    End Function

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
