Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class PetDGandhiStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable


#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _potentialHighEntryPrice As Decimal = Decimal.MinValue
    Private _potentialLowEntryPrice As Decimal = Decimal.MinValue
    Private _candleRange As Decimal = Decimal.MinValue
    Private _signalCandle As OHLCPayload = Nothing
    Private _lastTick As ITick = Nothing
    Private _eligibleToTakeTrade As Boolean = True

    Private ReadOnly _initialSLPercentage As Decimal = 10
    Private ReadOnly _dummyATRConsumer As ATRConsumer

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
                chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                _lastTick = Me.TradableInstrument.LastTick
                Dim currentTick As ITick = _lastTick
                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 AndAlso placeOrderTriggers.FirstOrDefault.Item1 = ExecuteCommandAction.Take Then
                    Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = placeOrderTriggers.FirstOrDefault
                    Dim modifiedPlaceOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = New List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String))
                    modifiedPlaceOrderTrigger.Add(New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(placeOrderTrigger.Item1, Me, placeOrderTrigger.Item2, placeOrderTrigger.Item3))
                    Dim placeOrderResponses As List(Of IBusinessOrder) = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger, True, _lastTick).ConfigureAwait(False)
                    Dim placeOrderResponse As IBusinessOrder = placeOrderResponses.FirstOrDefault
                    If placeOrderResponse IsNot Nothing Then
                        Dim potentialTargetPL As Decimal = 0
                        Dim potentialStoplossPL As Decimal = 0
                        Dim potentialEntry As Decimal = 0
                        Dim slipage As Decimal = 0
                        If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialEntry = _potentialHighEntryPrice
                            slipage = potentialEntry - placeOrderResponse.ParentOrder.AveragePrice
                        ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialEntry = _potentialLowEntryPrice
                            slipage = placeOrderResponse.ParentOrder.AveragePrice - potentialEntry
                        End If
                        Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Signal Candle Time:{1}, Candle Range:{2}, ATR:{3}, Quantity:{4}, {5}Direction:{6}, {7}Potential Entry:{8}, Entry Price:{9}({10}), {11}Stoploss Price:{12}, Potential Stoploss PL:₹{13}, {14}Target Price:{15}, Potential Target PL:₹{16}, {17}Total Stock PL:₹{18}, {19}LTP:{20}, Tick Timestamp:{21}, {22}Timestamp:{23}",
                                                                Me.TradableInstrument.TradingSymbol,
                                                                _signalCandle.SnapshotDateTime.ToShortTimeString,
                                                                _candleRange,
                                                                GetHighestATROfTheDay(_signalCandle),
                                                                placeOrderResponse.ParentOrder.Quantity,
                                                                vbNewLine,
                                                                placeOrderResponse.ParentOrder.TransactionType.ToString,
                                                                vbNewLine,
                                                                potentialEntry,
                                                                placeOrderResponse.ParentOrder.AveragePrice,
                                                                slipage,
                                                                vbNewLine,
                                                                placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice,
                                                                Math.Round(potentialStoplossPL, 2),
                                                                vbNewLine,
                                                                placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice,
                                                                Math.Round(potentialTargetPL, 2),
                                                                vbNewLine,
                                                                Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                                vbNewLine,
                                                                _lastTick.LastPrice,
                                                                _lastTick.Timestamp,
                                                                vbNewLine,
                                                                Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                End If
                'Place Order block end

                _cts.Token.ThrowIfCancellationRequested()
                'Modify Order block start
                Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    Dim modifyOrderResponses As List(Of IBusinessOrder) = Await ModifySLPaperTradeAsync(modifyStoplossOrderTrigger).ConfigureAwait(False)
                    Dim modifyOrderResponse As IBusinessOrder = Nothing
                    If modifyOrderResponses IsNot Nothing AndAlso modifyOrderResponses.Count > 0 Then
                        modifyOrderResponse = modifyOrderResponses.FirstOrDefault
                    End If
                    If modifyOrderResponse IsNot Nothing Then
                        Dim potentialTargetPL As Decimal = 0
                        Dim potentialStoplossPL As Decimal = 0
                        If modifyOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, modifyOrderResponse.ParentOrder.Quantity)
                        ElseIf modifyOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                        End If
                        Dim message As String = String.Format("Order Modified. Trading Symbol:{0}, Signal Candle Time:{1}, Candle Range:{2}, ATR:{3}, Quantity:{4}, {5}Direction:{6}, {7}, {8}Entry Price:{9}, {10}Stoploss Price:{11}, Potential Stoploss PL:₹{12}, {13}Target Price:{14}, Potential Target PL:₹{15}, {16}Reason:{17}, {18}Total Stock PL:₹{19}, Timestamp:{20}",
                                                                Me.TradableInstrument.TradingSymbol,
                                                                _signalCandle.SnapshotDateTime.ToShortTimeString,
                                                                _candleRange,
                                                                GetHighestATROfTheDay(_signalCandle),
                                                                modifyOrderResponse.ParentOrder.Quantity,
                                                                vbNewLine,
                                                                modifyOrderResponse.ParentOrder.TransactionType.ToString,
                                                                "",
                                                                vbNewLine,
                                                                modifyOrderResponse.ParentOrder.AveragePrice,
                                                                vbNewLine,
                                                                modifyOrderResponse.SLOrder.FirstOrDefault.TriggerPrice,
                                                                Math.Round(potentialStoplossPL, 2),
                                                                vbNewLine,
                                                                modifyOrderResponse.TargetOrder.FirstOrDefault.AveragePrice,
                                                                Math.Round(potentialTargetPL, 2),
                                                                vbNewLine,
                                                                modifyStoplossOrderTrigger.LastOrDefault.Item4,
                                                                vbNewLine,
                                                                Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                                Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                End If
                'Modify Order block end

                _cts.Token.ThrowIfCancellationRequested()
                Dim activeOrder As IBusinessOrder = Me.GetActiveOrder(IOrder.TypeOfTransaction.None)
                'Check Target block start
                If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 AndAlso
                    activeOrder.TargetOrder IsNot Nothing AndAlso activeOrder.TargetOrder.Count > 0 Then
                    If activeOrder.TargetOrder.Count > 1 Then
                        Throw New ApplicationException("Why target order greater than 1")
                    Else
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice >= target Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice <= target Then
                                _lastTick = currentTick
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
                            If currentTick.LastPrice <= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If currentTick.LastPrice >= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        End If
                    End If
                End If
                'Check Stoploss block end
                _cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = _lastTick
        Dim currentTime As Date = Now()
        Dim lastExecutedOrder As IBusinessOrder = GetLastExecutedOrder()

        If Not _eligibleToTakeTrade Then
            Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
            If currentTick.LastPrice < middlePoint + userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer OrElse
                currentTick.LastPrice > middlePoint - userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer Then
                _eligibleToTakeTrade = True
                logger.Debug("LTP is inside entry range. {0}", Me.TradableInstrument.TradingSymbol)
            End If
        End If

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                (Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint) Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                If _signalCandle IsNot Nothing Then
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Signal Candle Time:{5}, Signal Candle Range:{6}, Signal Candle Source:{7}, Highest ATR:{8}, Target Point:{9}, Eligible to take trade:{10}, Is Active Instrument:{11}, Number Of Trade:{12}, OverAll PL:{13}, Stock PL:{14}, Is Target Reached:{15}, Buy Entry:{16}, Sell Entry:{17}, Last Trade Direction:{18}, Last Trade Entry Time:{19}, Current Time:{20}, Current LTP:{21}, TradingSymbol:{22}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    _signalCandle.SnapshotDateTime.ToShortTimeString,
                                    _candleRange,
                                    _signalCandle.PayloadGeneratedBy.ToString,
                                    GetHighestATROfTheDay(_signalCandle),
                                    GetHighestATROfTheDay(_signalCandle) * userSettings.TargetMultiplier,
                                    _eligibleToTakeTrade,
                                    IsActiveInstrument(),
                                    Me.GetTotalExecutedOrders(),
                                    Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                    Me.GetOverallPLAfterBrokerage(),
                                    IsAnyTradeTargetReached(),
                                    _potentialHighEntryPrice,
                                    _potentialLowEntryPrice,
                                    If(lastExecutedOrder IsNot Nothing, lastExecutedOrder.ParentOrder.TransactionType, "Nothing"),
                                    If(lastExecutedOrder IsNot Nothing, lastExecutedOrder.ParentOrder.TimeStamp, "Nothing"),
                                    currentTime.ToString,
                                    currentTick.LastPrice,
                                    Me.TradableInstrument.TradingSymbol)
                Else
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Current Candle Time:{5}, Current Candle Range:{6}, Current Candle Source:{7}, {8}, Highest ATR:{9}, Is Active Instrument:{10}, Number Of Trade:{11}, OverAll PL:{12}, Stock PL:{13}, Is Target Reached:{14}, Current Time:{15}, Current LTP:{16}, TradingSymbol:{17}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    runningCandlePayload.PreviousPayload.SnapshotDateTime.ToShortTimeString,
                                    runningCandlePayload.PreviousPayload.CandleRange,
                                    runningCandlePayload.PreviousPayload.PayloadGeneratedBy.ToString,
                                    "",
                                    GetHighestATROfTheDay(runningCandlePayload.PreviousPayload),
                                    IsActiveInstrument(),
                                    Me.GetTotalExecutedOrders(),
                                    Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                    Me.GetOverallPLAfterBrokerage(),
                                    IsAnyTradeTargetReached(),
                                    currentTime.ToString,
                                    currentTick.LastPrice,
                                    Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If _eligibleToTakeTrade AndAlso currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not IsAnyTradeTargetReached() AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() > Math.Abs(userSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso Not Me.StrategyExitAllTriggerd Then
            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                Dim takeTrade As Boolean = True
                If lastExecutedOrder IsNot Nothing AndAlso Utilities.Time.IsDateTimeEqualTillMinutes(lastExecutedOrder.ParentOrder.TimeStamp, runningCandlePayload.SnapshotDateTime) Then
                    takeTrade = False
                End If
                If takeTrade Then
                    If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                        Dim triggerPrice As Decimal = signal.Item2
                        Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoplossPrice As Decimal = signal.Item3
                        Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                        Dim target As Decimal = GetHighestATROfTheDay(_signalCandle) * userSettings.TargetMultiplier

                        If currentTick.LastPrice >= triggerPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                        {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                         .TriggerPrice = triggerPrice,
                                         .Price = price,
                                         .StoplossValue = stoploss,
                                         .SquareOffValue = target,
                                         .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity}
                        End If
                    ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                        Dim triggerPrice As Decimal = signal.Item2
                        Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoplossPrice As Decimal = signal.Item3
                        Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                        Dim target As Decimal = GetHighestATROfTheDay(_signalCandle) * userSettings.TargetMultiplier

                        If currentTick.LastPrice <= triggerPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                        {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                         .TriggerPrice = triggerPrice,
                                         .Price = price,
                                         .StoplossValue = stoploss,
                                         .SquareOffValue = target,
                                         .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity}
                        End If
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters.ToString, Me.TradableInstrument.TradingSymbol)
            Catch ex As Exception
                logger.Error(ex.ToString)
            End Try

            Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
            If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                Dim placedActivities As IEnumerable(Of ActivityDashboard) = currentSignalActivities.Where(Function(x)
                                                                                                              Return x.EntryActivity.RequestRemarks = parameters.ToString
                                                                                                          End Function)
                If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                    Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                               Return x.EntryActivity.RequestTime
                                                                                           End Function).LastOrDefault
                    If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                            lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                            lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, parameters.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters, parameters.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters, parameters.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters, parameters.ToString))
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, parameters.ToString))
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, parameters.ToString))
                End If
            Else
                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, parameters.ToString))
            End If
        End If

        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTick As ITick = _lastTick
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    Dim triggerPrice As Decimal = Decimal.MinValue
                    Dim modifyAfterEntryTrigger As Boolean = False
                    Dim buffer As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer
                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        triggerPrice = ConvertFloorCeling((_potentialLowEntryPrice + buffer) + _candleRange * _initialSLPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        If runningCandlePayload.PreviousPayload.LowPrice.Value < middlePoint AndAlso runningCandlePayload.PreviousPayload.LowPrice.Value > triggerPrice Then
                            modifyAfterEntryTrigger = True
                        End If
                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        triggerPrice = ConvertFloorCeling((_potentialHighEntryPrice - buffer) - _candleRange * _initialSLPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                        If runningCandlePayload.PreviousPayload.HighPrice.Value > middlePoint AndAlso runningCandlePayload.PreviousPayload.HighPrice.Value < triggerPrice Then
                            modifyAfterEntryTrigger = True
                        End If
                    End If
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected AndAlso
                            Not slOrder.SupportingFlag Then

                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                If modifyAfterEntryTrigger AndAlso slOrder.TriggerPrice = triggerPrice AndAlso
                                    runningCandlePayload.SnapshotDateTime > bussinessOrder.ParentOrder.TimeStamp Then
                                    triggerPrice = runningCandlePayload.PreviousPayload.LowPrice.Value
                                    slOrder.SupportingFlag = True
                                    'Else
                                    '    If triggerPrice < slOrder.TriggerPrice Then
                                    '        triggerPrice = Decimal.MinValue
                                    '    End If
                                Else
                                    modifyAfterEntryTrigger = False
                                End If
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                If modifyAfterEntryTrigger AndAlso slOrder.TriggerPrice = triggerPrice AndAlso
                                    runningCandlePayload.SnapshotDateTime > bussinessOrder.ParentOrder.TimeStamp Then
                                    triggerPrice = runningCandlePayload.PreviousPayload.HighPrice.Value
                                    slOrder.SupportingFlag = True
                                    'Else
                                    '    If triggerPrice > slOrder.TriggerPrice Then
                                    '        triggerPrice = Decimal.MinValue
                                    '    End If
                                Else
                                    modifyAfterEntryTrigger = False
                                End If
                            End If

                            If triggerPrice <> Decimal.MinValue AndAlso slOrder.TriggerPrice <> triggerPrice Then
                                'Below portion have to be done in every modify stoploss order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(slOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        If Val(currentSignalActivities.StoplossModifyActivity.Supporting) = triggerPrice Then
                                            Continue For
                                        End If
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, If(modifyAfterEntryTrigger, "Aggressive Modify", "Normal Modify")))
                            End If
                        End If
                    Next
                End If
            Next
        End If
        Return ret
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

            Dim potentialExitPrice As Decimal = 0
            Dim exitOrderResponses As List(Of IBusinessOrder) = Nothing
            If reason.ToUpper = "TARGET REACHED" Then
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).TargetOrder.LastOrDefault.AveragePrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
            ElseIf reason.ToUpper = "STOPLOSS REACHED" Then
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.TriggerPrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)

                If exitOrderResponses IsNot Nothing AndAlso exitOrderResponses.Count > 0 Then
                    If exitOrderResponses.FirstOrDefault IsNot Nothing AndAlso exitOrderResponses.FirstOrDefault.AllOrder IsNot Nothing AndAlso
                        exitOrderResponses.FirstOrDefault.AllOrder.Count > 0 Then
                        Dim exitTime As Date = Date.MinValue
                        For Each runningOrder In exitOrderResponses.FirstOrDefault.AllOrder
                            If runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss AndAlso runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                                exitTime = runningOrder.TimeStamp
                                Exit For
                            End If
                        Next
                        If exitTime <> Date.MinValue Then
                            While _lastTick.Timestamp.Value = exitTime
                                _lastTick = Me.TradableInstrument.LastTick
                            End While
                            If _lastTick.LastPrice > _potentialHighEntryPrice OrElse _lastTick.LastPrice < _potentialLowEntryPrice Then
                                logger.Debug("LTP is outside entry price. {0}", Me.TradableInstrument.TradingSymbol)
                                _eligibleToTakeTrade = False
                            End If
                        End If
                    End If
                End If
            Else
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.TriggerPrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)
                OnHeartbeat(String.Format("Force Cancel Order Successful. {0}", reason))
            End If

            Dim exitOrderResponse As IBusinessOrder = Nothing
            If exitOrderResponses IsNot Nothing AndAlso exitOrderResponses.Count > 0 Then
                exitOrderResponse = exitOrderResponses.FirstOrDefault
            End If
            If exitOrderResponse IsNot Nothing AndAlso exitOrderResponse.AllOrder IsNot Nothing AndAlso exitOrderResponse.AllOrder.Count > 0 Then
                Dim exitPrice As Decimal = Decimal.MinValue
                For Each runningOrder In exitOrderResponse.AllOrder
                    If runningOrder.Status = IOrder.TypeOfStatus.Cancelled OrElse runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                        If runningOrder.Quantity <> 0 Then
                            exitPrice = runningOrder.AveragePrice
                            Exit For
                        End If
                    End If
                Next
                Dim potentialExitPL As Decimal = 0
                Dim slipage As Decimal = 0
                Dim plSlipage As Decimal = 0
                Dim orderPL As Decimal = Me.GetTotalPLOfAnOrderAfterBrokerage(exitOrderResponse.ParentOrderIdentifier)
                If exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, exitOrderResponse.ParentOrder.AveragePrice, potentialExitPrice, exitOrderResponse.ParentOrder.Quantity)
                    slipage = exitPrice - potentialExitPrice
                    plSlipage = orderPL - potentialExitPL
                ElseIf exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, potentialExitPrice, exitOrderResponse.ParentOrder.AveragePrice, exitOrderResponse.ParentOrder.Quantity)
                    slipage = potentialExitPrice - exitPrice
                    plSlipage = orderPL - potentialExitPL
                End If
                Dim message As String = String.Format("{0}. Trading Symbol:{1}, Signal Candle Time:{2}, Candle Range:{3}, ATR:{4}, Quantity:{5}, {6}Direction:{7}, {8}Entry Price:{9}, {10}Potential Exit Price:{11}, Exit Price:{12}({13}), {14}Potential Exit PL:₹{15}, Exit PL:₹{16}(₹{17}), {18}Total Stock PL:₹{19}, Number Of Trade:{20}, {21}LTP:{22}, Tick Timestamp:{23}, {24}Timestamp:{25}",
                                                        reason,
                                                        Me.TradableInstrument.TradingSymbol,
                                                        _signalCandle.SnapshotDateTime.ToShortTimeString,
                                                        _candleRange,
                                                        GetHighestATROfTheDay(_signalCandle),
                                                        exitOrderResponse.ParentOrder.Quantity,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.TransactionType.ToString,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.AveragePrice,
                                                        vbNewLine,
                                                        potentialExitPrice,
                                                        exitPrice,
                                                        slipage,
                                                        vbNewLine,
                                                        Math.Round(potentialExitPL, 2),
                                                        Math.Round(orderPL, 2),
                                                        Math.Round(plSlipage, 2),
                                                        vbNewLine,
                                                        Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                        GetTotalExecutedOrders(),
                                                        vbNewLine,
                                                        _lastTick.LastPrice,
                                                        _lastTick.Timestamp,
                                                        vbNewLine,
                                                        Now)
                GenerateTelegramMessageAsync(message)
            End If
        End If
    End Function

    Private Function IsAnyTradeTargetReached() As Boolean
        Dim ret As Boolean = False
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrder In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(parentOrder)
                If bussinessOrder.AllOrder IsNot Nothing AndAlso bussinessOrder.AllOrder.Count > 0 Then
                    For Each order In bussinessOrder.AllOrder
                        'If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
                        If order.Status = IOrder.TypeOfStatus.Complete Then
                            Dim target As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = order.AveragePrice - bussinessOrder.ParentOrder.AveragePrice
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - order.AveragePrice
                            End If
                            If target >= 0 Then
                                ret = True
                                Exit For
                            End If
                        End If
                    Next
                End If
                If ret Then Exit For
            Next
        End If
        Return ret
    End Function

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing AndAlso
            Not candle.DeadCandle AndAlso Not candle.PreviousPayload.DeadCandle Then
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            If _signalCandle Is Nothing Then
                If GetHighestATROfTheDay(candle) <> Decimal.MinValue AndAlso
                    candle.CandleRange <= (GetHighestATROfTheDay(candle) / 2) + (userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer / 2) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value + userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer
                    _potentialLowEntryPrice = candle.LowPrice.Value - userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer
                    _signalCandle = candle
                    _candleRange = _signalCandle.CandleRange
                End If
            End If

            If _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
                Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                If currentTick.LastPrice >= middlePoint + range * 30 / 100 Then
                    tradeDirection = IOrder.TypeOfTransaction.Buy
                ElseIf currentTick.LastPrice <= middlePoint - range * 30 / 100 Then
                    tradeDirection = IOrder.TypeOfTransaction.Sell
                End If
                Dim buffer As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Buffer
                If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                    Dim sl As Decimal = ConvertFloorCeling((_potentialLowEntryPrice + buffer) + _candleRange * _initialSLPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, sl, IOrder.TypeOfTransaction.Buy)
                ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                    Dim sl As Decimal = ConvertFloorCeling((_potentialHighEntryPrice - buffer) - _candleRange * _initialSLPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, sl, IOrder.TypeOfTransaction.Sell)
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetHighestATROfTheDay(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If candle IsNot Nothing Then
            Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
            If atrConsumer IsNot Nothing AndAlso atrConsumer.ConsumerPayloads IsNot Nothing AndAlso atrConsumer.ConsumerPayloads.Count > 0 Then
                For Each runningATR In atrConsumer.ConsumerPayloads
                    If runningATR.Key.Date = Now.Date AndAlso runningATR.Key <= candle.SnapshotDateTime Then
                        ret = Math.Max(ret, CType(runningATR.Value, ATRConsumer.ATRPayload).ATR.Value)
                    End If
                Next
            End If
        End If
        If ret <> Decimal.MinValue Then ret = ConvertFloorCeling(ret, Me.TradableInstrument.TickSize, RoundOfType.Celing)
        Return ret
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
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
