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

    Public EligibleToTakeTrade As Boolean
    Public StopStrategyInstrument As Boolean
    Public FilledPreviousClose As Boolean
    Public ProcessingDone As Boolean
    Public Slab As Decimal = Decimal.MinValue

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _strategyInstrumentExit As Boolean = False
    Private _takeSingleTargetTrade As Boolean = False
    Private _firstTradedQuantity As Integer = Integer.MinValue
    Private _slMovedOnCandle As Concurrent.ConcurrentBag(Of String) = Nothing

    Private ReadOnly _dummyVWAPConsumer As VWAPConsumer
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
                {New VWAPConsumer(chartConsumer)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyVWAPConsumer = New VWAPConsumer(chartConsumer)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        Me.EligibleToTakeTrade = False
        Me.FilledPreviousClose = False
        Me.StopStrategyInstrument = False
        If Not CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).AutoSelectStock Then
            Me.EligibleToTakeTrade = True
        End If
    End Sub

    Public Overrides Function HandleTickTriggerToUIETCAsync() As Task
        If Me.ParentStrategy.GetTotalPLAfterBrokerage <= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxLossPerDay Then
            Me.StrategyExitAllTriggerd = True
            ForceExitAllTradesAsync("Strategy Max Loss reached")
        ElseIf Me.ParentStrategy.GetTotalPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxProfitPerDay Then
            Me.StrategyExitAllTriggerd = True
            ForceExitAllTradesAsync("Strategy Max Profit reached")
        ElseIf Me.GetOverallPLAfterBrokerage <= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).StockMaxLossPerDay Then
            _strategyInstrumentExit = True
            ForceExitAllTradesAsync("Instrument Max Loss reached")
        ElseIf Me.GetOverallPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).StockMaxProfitPerDay Then
            _strategyInstrumentExit = True
            ForceExitAllTradesAsync("Instrument Max Profit reached")
        End If
        Return MyBase.HandleTickTriggerToUIETCAsync()
    End Function

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                If Me._RMSException IsNot Nothing AndAlso
                    _RMSException.ExceptionType = Algo2TradeCore.Exceptions.AdapterBusinessException.TypeOfException.RMSError Then
                    OnHeartbeat(String.Format("{0}:Will not take no more action in this instrument as RMS Error occured. Error-{1}", Me.TradableInstrument.TradingSymbol, _RMSException.Message))
                    Throw Me._RMSException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                'Get Stock Data Start
                If userSettings.AutoSelectStock AndAlso Not Me.FilledPreviousClose Then
                    Await GetStockData().ConfigureAwait(False)
                End If
                'Get Stock Data end
                _cts.Token.ThrowIfCancellationRequested()
                If Me.StopStrategyInstrument Then
                    Exit While
                End If
                If Me.EligibleToTakeTrade Then
                    _cts.Token.ThrowIfCancellationRequested()
                    'Place Order block start
                    Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                    If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.PlaceBOLimitMISOrder, Nothing).ConfigureAwait(False)
                    End If
                    'Place Order block end
                    _cts.Token.ThrowIfCancellationRequested()
                    'Modify Order block start
                    Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                    If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    End If
                    'Modify Order block end
                    _cts.Token.ThrowIfCancellationRequested()
                    'Exit Order block start
                    Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                    If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                    End If
                    'Exit Order block end
                End If
                _cts.Token.ThrowIfCancellationRequested()
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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim vwapConsumer As VWAPConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyVWAPConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()
        Dim lastExecutedOrder As IBusinessOrder = GetLastExecutedOrder()

        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime Then
            If Me.Slab = Decimal.MinValue Then
                If userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab <> Decimal.MinValue Then
                    Me.Slab = userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab
                Else
                    Me.Slab = CalculateSlab(runningCandlePayload.OpenPrice.Value)
                End If
            End If
            If _firstTradedQuantity = Integer.MinValue Then
                Dim buffer As Decimal = CalculateBuffer(runningCandlePayload.OpenPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                _firstTradedQuantity = CalculateQuantityFromStoploss(runningCandlePayload.OpenPrice.Value, runningCandlePayload.OpenPrice.Value - Me.Slab - buffer, userSettings.MaxLossPerTrade)
            End If
        End If

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                (Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint) Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Previous Candle:{5}, Is Active Instrument:{6}, Number Of Trade:{7}, OverAll PL:{8}, Stock PL:{9}, Strategy Exit All Triggerd:{10}, Strategy Instrument Exit:{11}, {12}, Last Trade Exited at SL Level:{13}, Last Trade Direction:{14}, Current Time:{15}, Current LTP:{16}, TradingSymbol:{17}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToString,
                            IsActiveInstrument(),
                            GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            Me.GetOverallPLAfterBrokerage(),
                            Me.StrategyExitAllTriggerd,
                            _strategyInstrumentExit,
                            vwapConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                            IsTradeExitAtOppositeLevel(lastExecutedOrder),
                            If(lastExecutedOrder IsNot Nothing, lastExecutedOrder.ParentOrder.RawTransactionType, "Nothing"),
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameter As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Me.GetOverallPLAfterBrokerage() > userSettings.StockMaxLossPerDay AndAlso Me.GetOverallPLAfterBrokerage() < userSettings.StockMaxProfitPerDay AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() > userSettings.MaxLossPerDay AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso
            Not Me.StrategyExitAllTriggerd AndAlso Not _strategyInstrumentExit Then
            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                Dim eligilbleToTakeTrade As Boolean = True
                If lastExecutedOrder IsNot Nothing AndAlso IsTradeExitAtOppositeLevel(lastExecutedOrder) Then
                    If lastExecutedOrder.ParentOrder.TransactionType <> signal.Item4 Then
                        Dim lastorderExitTime As Date = GetOrderExitTime(lastExecutedOrder)
                        If lastorderExitTime <> Date.MinValue AndAlso Utilities.Time.IsDateTimeEqualTillMinutes(lastorderExitTime, runningCandlePayload.SnapshotDateTime) Then
                            'If lastExecutedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            '    Dim oppositeLevel As Decimal = lastExecutedOrder.ParentOrder.Price - Me.Slab
                            '    If signal.Item2 = oppositeLevel Then eligilbleToTakeTrade = False
                            'ElseIf lastExecutedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            '    Dim oppositeLevel As Decimal = lastExecutedOrder.ParentOrder.Price + Me.Slab
                            '    If signal.Item2 = oppositeLevel Then eligilbleToTakeTrade = False
                            'End If
                            eligilbleToTakeTrade = False
                        End If
                    End If
                End If
                If eligilbleToTakeTrade Then
                    Dim buffer As Decimal = CalculateBuffer(signal.Item2, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim targetPL As Decimal = userSettings.StockMaxProfitPerDay - Me.GetOverallPLAfterBrokerage()
                    If Me.GetOverallPLAfterBrokerage() <= 3 * userSettings.MaxLossPerTrade Then
                        _takeSingleTargetTrade = True
                    ElseIf Me.GetOverallPLAfterBrokerage() > userSettings.MaxLossPerTrade AndAlso _takeSingleTargetTrade Then
                        _takeSingleTargetTrade = False
                    End If
                    Dim targetPrice As Decimal = CalculateTargetFromPL(signal.Item2, _firstTradedQuantity, targetPL)
                    Dim target As Decimal = targetPrice - signal.Item2
                    If _takeSingleTargetTrade Then
                        target = Me.Slab
                    End If
                    If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                        Dim price As Decimal = signal.Item2
                        Dim stoploss As Decimal = Me.Slab + buffer
                        If currentTick.LastPrice > price AndAlso currentTick.LastPrice < price + Me.Slab Then
                            parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
                        End If
                    ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                        Dim price As Decimal = signal.Item2
                        Dim stoploss As Decimal = Me.Slab + buffer
                        If currentTick.LastPrice < price AndAlso currentTick.LastPrice > price - Me.Slab Then
                            parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
                        End If
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameter IsNot Nothing Then
            Try
                If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameter.ToString, Me.TradableInstrument.TradingSymbol)
            Catch ex As Exception
                logger.Error(ex.ToString)
            End Try

            Dim allSignalActivities As IEnumerable(Of KeyValuePair(Of String, ActivityDashboard)) = Me.ParentStrategy.SignalManager.GetAllSignalActivitiesForInstrument(Me.TradableInstrument.InstrumentIdentifier)
            If allSignalActivities IsNot Nothing AndAlso allSignalActivities.Count > 0 Then
                Dim placedActivities As List(Of ActivityDashboard) = Nothing
                For Each runningActivity In allSignalActivities
                    If placedActivities Is Nothing Then placedActivities = New List(Of ActivityDashboard)
                    placedActivities.Add(runningActivity.Value)
                Next
                If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                    Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                               Return x.EntryActivity.RequestTime
                                                                                           End Function).LastOrDefault
                    If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                        lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                        lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        If currentTime >= lastPlacedActivity.EntryActivity.RequestTime.AddSeconds(Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay) Then
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                        Else
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                        End If
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        'If currentTime >= lastPlacedActivity.EntryActivity.RequestTime.AddSeconds(Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay) Then
                        '    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                        'Else
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                        'End If
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                End If
            Else
                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
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
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso Me.Slab <> Decimal.MinValue Then
            Dim vwapConsumer As VWAPConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyVWAPConsumer)
            Dim vwap As VWAPConsumer.VWAPPayload = Nothing
            If vwapConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) Then
                vwap = vwapConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime)
            End If
            If vwap IsNot Nothing Then
                For Each runningOrderID In OrderDetails.Keys
                    Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                    If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                        Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.AveragePrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                        For Each slOrder In bussinessOrder.SLOrder
                            If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                                Dim triggerPrice As Decimal = Decimal.MinValue
                                Dim reason As String = Nothing
                                If runningCandlePayload.SnapshotDateTime > GetBlockDateTime(bussinessOrder.ParentOrder.TimeStamp, userSettings.SignalTimeFrame) Then
                                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        Dim slabLvl As Decimal = GetSlabBasedLevel(vwap.VWAP.Value, IOrder.TypeOfTransaction.Sell)
                                        Dim moved As Boolean = False
                                        If runningCandlePayload.PreviousPayload.ClosePrice.Value < vwap.VWAP.Value Then
                                            If currentTick.LastPrice > runningCandlePayload.PreviousPayload.LowPrice.Value AndAlso
                                                runningCandlePayload.PreviousPayload.LowPrice.Value > slOrder.TriggerPrice Then
                                                If _slMovedOnCandle Is Nothing OrElse Not _slMovedOnCandle.Contains(slOrder.OrderIdentifier) Then
                                                    triggerPrice = runningCandlePayload.PreviousPayload.LowPrice.Value - buffer
                                                    reason = "Candle Low Below VWAP"
                                                    moved = True
                                                    If _slMovedOnCandle Is Nothing Then _slMovedOnCandle = New Concurrent.ConcurrentBag(Of String)
                                                    If forcePrint Then _slMovedOnCandle.Add(slOrder.OrderIdentifier)
                                                End If
                                            End If
                                        End If
                                        If Not moved Then
                                            If currentTick.LastPrice > slabLvl AndAlso
                                                slabLvl > slOrder.TriggerPrice Then
                                                triggerPrice = slabLvl - buffer
                                                reason = "Slab Below VWAP"
                                            End If
                                        End If
                                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        Dim slabLvl As Decimal = GetSlabBasedLevel(vwap.VWAP.Value, IOrder.TypeOfTransaction.Buy)
                                        Dim moved As Boolean = False
                                        If runningCandlePayload.PreviousPayload.ClosePrice.Value > vwap.VWAP.Value Then
                                            If currentTick.LastPrice < runningCandlePayload.PreviousPayload.HighPrice.Value AndAlso
                                                runningCandlePayload.PreviousPayload.HighPrice.Value < slOrder.TriggerPrice Then
                                                If _slMovedOnCandle Is Nothing OrElse Not _slMovedOnCandle.Contains(slOrder.OrderIdentifier) Then
                                                    triggerPrice = runningCandlePayload.PreviousPayload.HighPrice.Value + buffer
                                                    reason = "Candle High Above VWAP"
                                                    moved = True
                                                    If _slMovedOnCandle Is Nothing Then _slMovedOnCandle = New Concurrent.ConcurrentBag(Of String)
                                                    If forcePrint Then _slMovedOnCandle.Add(slOrder.OrderIdentifier)
                                                End If
                                            End If
                                        End If
                                        If Not moved Then
                                            If currentTick.LastPrice < slabLvl AndAlso
                                                slabLvl < slOrder.TriggerPrice Then
                                                triggerPrice = slabLvl + buffer
                                                reason = "Slab Above VWAP"
                                            End If
                                        End If
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
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, reason))
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        End If
        If forcePrint Then
            Try
                If ret IsNot Nothing AndAlso ret.Count > 0 Then
                    For Each runningAction In ret
                        logger.Debug("Modify Order: Order ID:{0}, Trigger Price:{1}, Reason:{2}, Trading Symbol:{3}",
                                     runningAction.Item2.OrderIdentifier, runningAction.Item3, runningAction.Item4, Me.TradableInstrument.TradingSymbol)
                    Next
                End If
            Catch ex As Exception
                logger.Error(ex.ToString)
            End Try
        End If
        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
            Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                              Return x.ParentOrderIdentifier Is Nothing AndAlso
                                                                              x.Status = IOrder.TypeOfStatus.Open
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                Dim currentTick As ITick = Me.TradableInstrument.LastTick
                Dim runningCandle As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    If Not parentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                       Not parentOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                       Not parentOrder.Status = IOrder.TypeOfStatus.Rejected Then
                        If runningCandle IsNot Nothing AndAlso runningCandle.PreviousPayload IsNot Nothing Then
                            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandle.PreviousPayload, currentTick)
                            If signal IsNot Nothing Then
                                Dim exitTrade As Boolean = False
                                Dim reason As String = Nothing
                                If signal.Item4 <> parentOrder.TransactionType Then
                                    exitTrade = True
                                    reason = "Opposite Direction trade"
                                ElseIf signal.Item2 <> parentOrder.Price Then
                                    exitTrade = True
                                    reason = "Trade Level Changed"
                                ElseIf signal.Item4 = parentOrder.TransactionType Then
                                    If parentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        If currentTick.LastPrice > parentOrder.Price + Me.Slab Then
                                            exitTrade = True
                                            reason = "Capital Preservation"
                                        End If
                                    ElseIf parentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        If currentTick.LastPrice < parentOrder.Price - Me.Slab Then
                                            exitTrade = True
                                            reason = "Capital Preservation"
                                        End If
                                    End If
                                End If
                                If exitTrade Then
                                    'Below portion have to be done in every cancel order trigger
                                    Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                                    If currentSignalActivities IsNot Nothing Then
                                        If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                            currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                            currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                            Continue For
                                        End If
                                    End If
                                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, reason))
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        End If
        Return ret
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

            Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function

    Private Async Function GetStockData() As Task
        Await Task.Delay(0).ConfigureAwait(False)
        If Me.FilledPreviousClose = False AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            Me.TradableInstrument.FetchHistorical = False
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                             If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                                 Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                             Else
                                                                                                                 Return Nothing
                                                                                                             End If
                                                                                                         End Function)

                If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                    Dim currentDayFirstCandle As OHLCPayload = Nothing
                    For Each runningPayload In XMinutePayloadConsumer.ConsumerPayloads.Keys.OrderByDescending(Function(x)
                                                                                                                  Return x
                                                                                                              End Function)
                        Dim firstCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, 9, 15, 0)
                        If runningPayload.Date = Now.Date Then
                            If runningPayload = firstCandle Then
                                currentDayFirstCandle = XMinutePayloadConsumer.ConsumerPayloads(runningPayload)
                            End If
                        End If
                    Next
                    If currentDayFirstCandle IsNot Nothing AndAlso currentDayFirstCandle.PreviousPayload IsNot Nothing Then
                        Dim buffer As Decimal = CalculateBuffer(currentDayFirstCandle.OpenPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                        If currentDayFirstCandle.OpenPrice.Value < currentDayFirstCandle.PreviousPayload.ClosePrice.Value Then
                            If currentDayFirstCandle.HighPrice.Value + buffer >= currentDayFirstCandle.PreviousPayload.LowPrice.Value Then
                                Me.FilledPreviousClose = True
                                Me.ProcessingDone = True
                            End If
                        ElseIf currentDayFirstCandle.OpenPrice.Value >= currentDayFirstCandle.PreviousPayload.ClosePrice.Value Then
                            If currentDayFirstCandle.LowPrice.Value - buffer <= currentDayFirstCandle.PreviousPayload.HighPrice.Value Then
                                Me.FilledPreviousClose = True
                                Me.ProcessingDone = True
                            End If
                        End If
                    End If
                    If Now >= userSettings.TradeStartTime Then
                        Me.ProcessingDone = True
                    End If
                End If
            End If
        End If
    End Function

    Private Function GetSlabBasedLevel(ByVal price As Decimal, ByVal direction As IOrder.TypeOfTransaction) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If direction = IOrder.TypeOfTransaction.Buy Then
            ret = Math.Ceiling(price / Me.Slab) * Me.Slab
        ElseIf direction = IOrder.TypeOfTransaction.Sell Then
            ret = Math.Floor(price / Me.Slab) * Me.Slab
        End If
        Return ret
    End Function

    Private Function CalculateSlab(ByVal price As Decimal) As Decimal
        Dim ret As Decimal = 0.5
        Dim slabList As List(Of Decimal) = New List(Of Decimal) From {0.5, 1, 2.5, 5, 10, 15}
        Dim atrPer As Decimal = CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).InstrumentsData(Me.TradableInstrument.TradingSymbol).ATRPercentage
        Dim atr As Decimal = (atrPer / 100) * price
        Dim supportedSlabList As List(Of Decimal) = slabList.FindAll(Function(x)
                                                                         Return x <= atr / 8
                                                                     End Function)
        If supportedSlabList IsNot Nothing AndAlso supportedSlabList.Count > 0 Then
            ret = supportedSlabList.Max
            If price * 1 / 100 < ret Then
                Dim newSupportedSlabList As List(Of Decimal) = supportedSlabList.FindAll(Function(x)
                                                                                             Return x <= price * 1 / 100
                                                                                         End Function)
                If newSupportedSlabList IsNot Nothing AndAlso newSupportedSlabList.Count > 0 Then
                    ret = newSupportedSlabList.Max
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso Not candle.DeadCandle Then
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            Dim vwapConsumer As VWAPConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyVWAPConsumer)
            Dim vwap As VWAPConsumer.VWAPPayload = Nothing
            If vwapConsumer.ConsumerPayloads.ContainsKey(candle.SnapshotDateTime) Then
                vwap = vwapConsumer.ConsumerPayloads(candle.SnapshotDateTime)
            End If
            If vwap IsNot Nothing Then
                Dim potentialHighEntryPrice As Decimal = GetSlabBasedLevel(vwap.VWAP.Value, IOrder.TypeOfTransaction.Buy)
                Dim potentialLowEntryPrice As Decimal = GetSlabBasedLevel(vwap.VWAP.Value, IOrder.TypeOfTransaction.Sell)
                If potentialHighEntryPrice <> Decimal.MinValue AndAlso potentialLowEntryPrice <> Decimal.MinValue Then
                    Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                    Dim middlePoint As Decimal = (potentialHighEntryPrice + potentialLowEntryPrice) / 2
                    Dim range As Decimal = potentialHighEntryPrice - middlePoint
                    If currentTick.LastPrice >= middlePoint + range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Buy
                    ElseIf currentTick.LastPrice <= middlePoint - range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Sell
                    End If
                    If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, potentialHighEntryPrice, potentialHighEntryPrice - Me.Slab, IOrder.TypeOfTransaction.Buy)
                    ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, potentialLowEntryPrice, potentialLowEntryPrice + Me.Slab, IOrder.TypeOfTransaction.Sell)
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsTradeExitAtOppositeLevel(ByVal order As IBusinessOrder) As Boolean
        Dim ret As Boolean = False
        If order IsNot Nothing Then
            If order.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                If order.AllOrder IsNot Nothing AndAlso order.AllOrder.Count > 0 Then
                    Dim buffer As Decimal = CalculateBuffer(order.ParentOrder.AveragePrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    For Each ruuningOrder In order.AllOrder
                        If ruuningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss AndAlso
                            ruuningOrder.Status = IOrder.TypeOfStatus.Complete Then
                            If order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                If order.ParentOrder.Price - ruuningOrder.TriggerPrice >= Me.Slab + buffer Then
                                    ret = True
                                End If
                            ElseIf order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                If ruuningOrder.TriggerPrice - order.ParentOrder.Price >= Me.Slab + buffer Then
                                    ret = True
                                End If
                            End If
                        End If
                    Next
                End If
            End If
        End If
        Return ret
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
