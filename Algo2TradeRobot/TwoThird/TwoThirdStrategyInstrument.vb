Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators
Imports Utilities.Numbers

Public Class TwoThirdStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _signalCandle As OHLCPayload = Nothing
    Private _firstTradeQuantity As Integer = Integer.MinValue
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
                {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, TwoThirdUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, TwoThirdUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
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
                'Force Cancel block strat
                If IsAnyTradeTargetReached() Then
                    Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                End If
                'Force Cancel block end
                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
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
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()
        Dim longBreakevenPoints As Decimal = Decimal.MaxValue
        Dim shortBreakevenPoints As Decimal = Decimal.MaxValue

        Dim longActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Buy)
        Dim shortActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Sell)
        Dim lastExecutedTrade As IBusinessOrder = GetLastExecutedOrder()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If _signalCandle Is Nothing Then
                    Dim potentialLongEntryPrice As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value
                    potentialLongEntryPrice += CalculateBuffer(potentialLongEntryPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim potentialShortEntryPrice As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value
                    potentialShortEntryPrice -= CalculateBuffer(potentialShortEntryPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim potentialLongEntryQuantity As Integer = CalculateQuantityFromInvestment(potentialLongEntryPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, userSettings.AllowToIncreaseCapital)
                    Dim potentialShortEntryQuantity As Integer = CalculateQuantityFromInvestment(potentialShortEntryPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, userSettings.AllowToIncreaseCapital)
                    longBreakevenPoints = GetBreakevenPoint(potentialLongEntryPrice, potentialLongEntryQuantity, IOrder.TypeOfTransaction.Buy)
                    shortBreakevenPoints = GetBreakevenPoint(potentialShortEntryPrice, potentialLongEntryQuantity, IOrder.TypeOfTransaction.Sell)
                End If
                If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
                    _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    If _signalCandle IsNot Nothing Then
                        logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Signal Candle Time:{5}, Signal Candle Range:{6}, Signal Candle Source:{7}, {8}, Is Active Instrument:{9}, Reverse Trade:{10}, Number Of Trade:{11}, Stoploss Movement To Breakeven:{12}, Count Trades With Breakeven Movement:{13}, OverAll PL:{14}, Is Target Reached:{15}, Long Active Trades:{16}, Short Active Trades:{17}, Last Executed Trade Direction:{18}, Current Time:{19}, Current LTP:{20}, TradingSymbol:{21}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    _signalCandle.SnapshotDateTime.ToShortTimeString,
                                    _signalCandle.CandleRange,
                                    _signalCandle.PayloadGeneratedBy.ToString,
                                    atrConsumer.ConsumerPayloads(_signalCandle.SnapshotDateTime).ToString,
                                    IsActiveInstrument(),
                                    userSettings.ReverseTrade,
                                    GetTotalLogicalExecutedOrders(),
                                    userSettings.StoplossMovementToBreakeven,
                                    userSettings.CountTradesWithBreakevenMovement,
                                    Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                    IsAnyTradeTargetReached(),
                                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                                    If(lastExecutedTrade Is Nothing, "Nothing", lastExecutedTrade.ParentOrder.TransactionType.ToString),
                                    currentTime.ToString,
                                    currentTick.LastPrice,
                                    Me.TradableInstrument.TradingSymbol)
                    Else
                        logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Candle Range:{5}, Breakevenpoints:{6}, {7}, Is Active Instrument:{8}, Reverse Trade:{9}, Number Of Trade:{10}, Stoploss Movement To Breakeven:{11}, Count Trades With Breakeven Movement:{12}, OverAll PL:{13}, Is Target Reached:{14}, Long Active Trades:{15}, Short Active Trades:{16}, Current Time:{17}, Current LTP:{18}, TradingSymbol:{19}",
                                   userSettings.TradeStartTime.ToString,
                                   userSettings.LastTradeEntryTime.ToString,
                                   runningCandlePayload.SnapshotDateTime.ToString,
                                   runningCandlePayload.PayloadGeneratedBy.ToString,
                                   Me.TradableInstrument.IsHistoricalCompleted,
                                   runningCandlePayload.PreviousPayload.CandleRange,
                                   Math.Max(longBreakevenPoints, shortBreakevenPoints),
                                   atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                   IsActiveInstrument(),
                                   userSettings.ReverseTrade,
                                   GetTotalLogicalExecutedOrders(),
                                   userSettings.StoplossMovementToBreakeven,
                                   userSettings.CountTradesWithBreakevenMovement,
                                   Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                   IsAnyTradeTargetReached(),
                                   If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                                   If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                                   currentTime.ToString,
                                   currentTick.LastPrice,
                                   Me.TradableInstrument.TradingSymbol)
                    End If
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters1 As PlaceOrderParameters = Nothing
        Dim parameters2 As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            (Not IsActiveInstrument() OrElse userSettings.ReverseTrade) AndAlso GetTotalLogicalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not IsAnyTradeTargetReached() AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() > Math.Abs(userSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso Not Me.StrategyExitAllTriggerd Then

            If _signalCandle Is Nothing OrElse (_signalCandle IsNot Nothing AndAlso _signalCandle.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick) Then
                Dim atr As Decimal = Math.Round(CType(atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)
                If runningCandlePayload.PreviousPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick Then
                    If runningCandlePayload.PreviousPayload.CandleRange <> 0 AndAlso
                        runningCandlePayload.PreviousPayload.CandleRangePercentage < 1 AndAlso
                        runningCandlePayload.PreviousPayload.CandleRange < atr * 90 / 100 AndAlso
                        runningCandlePayload.PreviousPayload.CandleRange >= Math.Max(longBreakevenPoints, shortBreakevenPoints) Then
                        _signalCandle = runningCandlePayload.PreviousPayload
                    End If
                ElseIf runningCandlePayload.PreviousPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedHistorical Then
                    If runningCandlePayload.PreviousPayload.CandleRange <> 0 AndAlso
                        runningCandlePayload.PreviousPayload.CandleRangePercentage < 1 AndAlso
                        runningCandlePayload.PreviousPayload.CandleRange < atr AndAlso
                        runningCandlePayload.PreviousPayload.CandleRange >= Math.Max(longBreakevenPoints, shortBreakevenPoints) Then
                        _signalCandle = runningCandlePayload.PreviousPayload
                    Else
                        _signalCandle = Nothing
                    End If
                End If
            Else
                Dim potentialLongEntryPrice As Decimal = _signalCandle.HighPrice.Value
                Dim longEntryBuffer As Decimal = CalculateBuffer(potentialLongEntryPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)

                Dim potentialShortEntryPrice As Decimal = _signalCandle.LowPrice.Value
                Dim shortEntryBuffer As Decimal = CalculateBuffer(potentialShortEntryPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)

                Dim quantity As Integer = 0
                If (longActiveTrades Is Nothing OrElse longActiveTrades.Count = 0) AndAlso
                    (lastExecutedTrade Is Nothing OrElse lastExecutedTrade.ParentOrder.TransactionType <> IOrder.TypeOfTransaction.Buy) Then
                    Dim triggerPrice As Decimal = potentialLongEntryPrice + longEntryBuffer
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoploss As Decimal = ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        quantity = CalculateQuantityFromInvestment(triggerPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, userSettings.AllowToIncreaseCapital)
                        _firstTradeQuantity = quantity
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 AndAlso currentTick.LastPrice < triggerPrice Then
                        parameters1 = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                 .TriggerPrice = triggerPrice,
                                 .Price = price,
                                 .StoplossValue = stoploss + 2 * longEntryBuffer,
                                 .SquareOffValue = target,
                                 .Quantity = quantity}
                    End If
                End If
                If (shortActiveTrades Is Nothing OrElse shortActiveTrades.Count = 0) AndAlso
                    (lastExecutedTrade Is Nothing OrElse lastExecutedTrade.ParentOrder.TransactionType <> IOrder.TypeOfTransaction.Sell) Then
                    Dim triggerPrice As Decimal = potentialShortEntryPrice - shortEntryBuffer
                    Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoploss As Decimal = ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        quantity = CalculateQuantityFromInvestment(triggerPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, userSettings.AllowToIncreaseCapital)
                        _firstTradeQuantity = quantity
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 AndAlso currentTick.LastPrice > triggerPrice Then
                        parameters2 = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                 .TriggerPrice = triggerPrice,
                                 .Price = price,
                                 .StoplossValue = stoploss + 2 * shortEntryBuffer,
                                 .SquareOffValue = target,
                                 .Quantity = quantity}

                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters1 IsNot Nothing Then
            Try
                If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters1.ToString, Me.TradableInstrument.TradingSymbol)
            Catch ex As Exception
                logger.Error(ex.ToString)
            End Try

            Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters1.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
            If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                Dim placedActivities As IEnumerable(Of ActivityDashboard) = currentSignalActivities.Where(Function(x)
                                                                                                              Return x.EntryActivity.RequestRemarks = parameters1.ToString
                                                                                                          End Function)
                If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                    Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                               Return x.EntryActivity.RequestTime
                                                                                           End Function).LastOrDefault
                    If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                            lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                            lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters1, parameters1.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
                End If
            Else
                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
            End If
        End If
        If parameters2 IsNot Nothing Then
            Try
                If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters2.ToString, Me.TradableInstrument.TradingSymbol)
            Catch ex As Exception
                logger.Error(ex.ToString)
            End Try

            Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters2.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
            If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                Dim placedActivities As IEnumerable(Of ActivityDashboard) = currentSignalActivities.Where(Function(x)
                                                                                                              Return x.EntryActivity.RequestRemarks = parameters2.ToString
                                                                                                          End Function)
                If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                    Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                               Return x.EntryActivity.RequestTime
                                                                                           End Function).LastOrDefault
                    If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                            lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                            lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters2, parameters2.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                    ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
                End If
            Else
                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
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
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                Dim targetReachedForBreakevenMovement As Boolean = False
                If bussinessOrder.TargetOrder IsNot Nothing AndAlso bussinessOrder.TargetOrder.Count > 0 AndAlso userSettings.StoplossMovementToBreakeven Then
                    For Each runningTragetOrder In bussinessOrder.TargetOrder
                        If runningTragetOrder.Status = IOrder.TypeOfStatus.Open Then
                            Dim target As Decimal = 0
                            Dim potentialTargetPrice As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = runningTragetOrder.Price - bussinessOrder.ParentOrder.AveragePrice
                                potentialTargetPrice = bussinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling(target * 2 / 3, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                If currentTick.LastPrice >= potentialTargetPrice Then targetReachedForBreakevenMovement = True
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - runningTragetOrder.Price
                                potentialTargetPrice = bussinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling(target * 2 / 3, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                If currentTick.LastPrice <= potentialTargetPrice Then targetReachedForBreakevenMovement = True
                            End If
                        End If
                    Next
                End If
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    Dim triggerPrice As Decimal = 0
                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        If targetReachedForBreakevenMovement Then
                            triggerPrice = ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice + GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, IOrder.TypeOfTransaction.Buy), Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        Else
                            triggerPrice = _signalCandle.LowPrice.Value - CalculateBuffer(_signalCandle.LowPrice.Value, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)
                        End If
                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        If targetReachedForBreakevenMovement Then
                            triggerPrice = ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice - GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, IOrder.TypeOfTransaction.Sell), Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        Else
                            triggerPrice = _signalCandle.HighPrice.Value + CalculateBuffer(_signalCandle.HighPrice.Value, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)
                        End If
                    End If
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then

                            Dim moveToBreakeven As Boolean = False
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                If slOrder.TriggerPrice - bussinessOrder.ParentOrder.AveragePrice > 0 Then
                                    moveToBreakeven = True
                                End If
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                If bussinessOrder.ParentOrder.AveragePrice - slOrder.TriggerPrice > 0 Then
                                    moveToBreakeven = True
                                End If
                            End If

                            If slOrder.TriggerPrice <> triggerPrice AndAlso Not moveToBreakeven Then
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
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, If(targetReachedForBreakevenMovement, "Move to breakeven", "SL movement to signal candle high/low")))
                            End If
                        End If
                    Next
                End If
            Next
        End If
        If forcePrint AndAlso ret IsNot Nothing AndAlso ret.Count > 0 Then
            For Each runningOrder In ret
                logger.Debug("***** Modify Stoploss ***** Order ID:{0}, Reason:{1}, {2}", runningOrder.Item2.OrderIdentifier, runningOrder.Item4, Me.TradableInstrument.TradingSymbol)
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

            Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function

    Private Function GetTotalLogicalExecutedOrders() As Integer
        Dim ret As Integer = Me.GetTotalExecutedOrders()
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        If ret > 0 Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso Not userSettings.CountTradesWithBreakevenMovement Then
                For Each runningParentOrderID In OrderDetails.Keys
                    Dim bussinessOrder As IBusinessOrder = OrderDetails(runningParentOrderID)
                    If bussinessOrder.AllOrder IsNot Nothing AndAlso bussinessOrder.AllOrder.Count > 0 Then
                        For Each runningOrder In bussinessOrder.AllOrder
                            If runningOrder.Status = IOrder.TypeOfStatus.Complete AndAlso runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss Then
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    If runningOrder.AveragePrice - bussinessOrder.ParentOrder.AveragePrice > 0 Then
                                        ret -= 1
                                    End If
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    If bussinessOrder.ParentOrder.AveragePrice - runningOrder.AveragePrice > 0 Then
                                        ret -= 1
                                    End If
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        End If
        Return ret
    End Function

    Private Function IsAnyTradeTargetReached() As Boolean
        Dim ret As Boolean = False
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso _signalCandle IsNot Nothing Then
            For Each parentOrder In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(parentOrder)
                If bussinessOrder.AllOrder IsNot Nothing AndAlso bussinessOrder.AllOrder.Count > 0 Then
                    For Each order In bussinessOrder.AllOrder
                        If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
                            Dim target As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = order.AveragePrice - bussinessOrder.ParentOrder.AveragePrice
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - order.AveragePrice
                            End If
                            If target >= ConvertFloorCeling(_signalCandle.CandleRange * (Me.ParentStrategy.UserSettings.TargetMultiplier - 1), Me.TradableInstrument.TickSize, RoundOfType.Celing) Then
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
