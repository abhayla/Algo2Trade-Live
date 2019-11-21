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

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _potentialHighEntryPrice As Decimal = Decimal.MinValue
    Private _potentialLowEntryPrice As Decimal = Decimal.MinValue
    Private _entryChanged As Boolean = False
    Private _entryChangedTime As Date = Date.MinValue
    Private _firstTradedQuantity As Integer = Integer.MinValue
    Private _breakevenMovedOrders As Concurrent.ConcurrentBag(Of String) = Nothing

    Public Property Slab As Decimal = Decimal.MinValue
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
            ForceExitAllTradesAsync("Max Profit reached")
        ElseIf Me.ParentStrategy.GetTotalPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxProfitPerDay Then
            Me.StrategyExitAllTriggerd = True
            ForceExitAllTradesAsync("Max Loss reached")
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
                        Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                    End If
                    'Place Order block end
                    _cts.Token.ThrowIfCancellationRequested()
                    'Modify Order block start
                    Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                    If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    End If
                    Dim modifyTargetOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyTargetOrderAsync(False).ConfigureAwait(False)
                    If modifyTargetOrderTrigger IsNot Nothing AndAlso modifyTargetOrderTrigger.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.ModifyTargetOrder, Nothing).ConfigureAwait(False)
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
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        'If Not _entryChanged AndAlso Me.Slab <> Decimal.MinValue AndAlso
        '    _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
        '    If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
        '        Dim firstOrder As IBusinessOrder = Nothing
        '        For Each runningOrder In OrderDetails.OrderBy(Function(x)
        '                                                          Return x.Value.ParentOrder.TimeStamp
        '                                                      End Function)
        '            If runningOrder.Value.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
        '                firstOrder = runningOrder.Value
        '                Exit For
        '            End If
        '        Next
        '        If firstOrder IsNot Nothing AndAlso firstOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
        '            If firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
        '                _potentialLowEntryPrice = _potentialHighEntryPrice - 2 * Me.Slab
        '            ElseIf firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
        '                _potentialHighEntryPrice = _potentialLowEntryPrice + 2 * Me.Slab
        '            End If
        '            _entryChanged = True
        '        End If
        '    End If
        'End If
        If Not _entryChanged AndAlso Me.Slab <> Decimal.MinValue AndAlso
            _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                Dim lastOrder As IBusinessOrder = GetLastExecutedOrder()
                If lastOrder IsNot Nothing AndAlso lastOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    lastOrder.ParentOrder.TimeStamp > _entryChangedTime Then
                    If lastOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        _potentialLowEntryPrice = _potentialHighEntryPrice - 2 * Me.Slab
                    ElseIf lastOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        _potentialHighEntryPrice = _potentialLowEntryPrice + 2 * Me.Slab
                    End If
                    _entryChanged = True
                End If
            End If
        End If

        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime Then
            If Me.Slab = Decimal.MinValue Then
                If userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab <> Decimal.MinValue Then
                    Me.Slab = userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab
                Else
                    Me.Slab = CalculateSlab(runningCandlePayload.OpenPrice.Value)
                End If
            End If
            If _firstTradedQuantity = Integer.MinValue Then
                If userSettings.CashInstrument Then
                    _firstTradedQuantity = CalculateQuantityFromStoploss(runningCandlePayload.OpenPrice.Value, runningCandlePayload.OpenPrice.Value - Me.Slab, -1)
                ElseIf userSettings.FutureInstrument Then
                    _firstTradedQuantity = CalculateQuantityFromInvestment(runningCandlePayload.OpenPrice.Value, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapitalPerStock, userSettings.AllowToIncreaseQuantity)
                End If
            End If
        End If

        Dim parameter As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() > userSettings.MaxLossPerDay AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso Not Me.StrategyExitAllTriggerd Then

            Dim lastExuctedOrder As IBusinessOrder = GetLastExecutedOrder()
            'If lastExuctedOrder IsNot Nothing AndAlso IsOrderExitedAtBreakeven(lastExuctedOrder) Then
            '    Dim buffer As Decimal = CalculateBuffer(lastExuctedOrder.ParentOrder.TriggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
            '    Dim price As Decimal = Decimal.MinValue
            '    If lastExuctedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
            '        price = lastExuctedOrder.ParentOrder.TriggerPrice - buffer
            '    ElseIf lastExuctedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
            '        price = lastExuctedOrder.ParentOrder.TriggerPrice + buffer
            '    End If
            '    _potentialHighEntryPrice = price + Me.Slab
            '    _potentialLowEntryPrice = price - Me.Slab
            'End If
            If lastExuctedOrder IsNot Nothing AndAlso IsOrderForceExitedForBreakeven(lastExuctedOrder) Then
                _potentialHighEntryPrice = Decimal.MinValue
                _potentialLowEntryPrice = Decimal.MinValue
                _entryChanged = False
                _entryChangedTime = Now
            End If

            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                Dim buffer As Decimal = CalculateBuffer(signal.Item2, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                    Dim triggerPrice As Decimal = signal.Item2 + buffer
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(Me.Slab / 2, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3 - buffer
                    Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(Me.Slab * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing) - buffer
                    If currentTick.LastPrice < triggerPrice Then
                        parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
                    End If
                ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                    Dim triggerPrice As Decimal = signal.Item2 - buffer
                    Dim price As Decimal = triggerPrice - ConvertFloorCeling(Me.Slab / 2, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3 + buffer
                    Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(Me.Slab * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing) - buffer
                    If currentTick.LastPrice > triggerPrice Then
                        parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
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
                        If currentTime >= lastPlacedActivity.EntryActivity.RequestTime.AddSeconds(Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay) Then
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                        Else
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                        End If
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
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            Dim reason As String = Nothing
                            Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.TriggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            If bussinessOrder.ParentOrder.AveragePrice <> bussinessOrder.ParentOrder.TriggerPrice Then
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    If slOrder.TriggerPrice < bussinessOrder.ParentOrder.AveragePrice Then
                                        triggerPrice = bussinessOrder.ParentOrder.TriggerPrice - Me.Slab - 2 * buffer
                                        reason = "Slippage"
                                    End If
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    If slOrder.TriggerPrice > bussinessOrder.ParentOrder.AveragePrice Then
                                        triggerPrice = bussinessOrder.ParentOrder.TriggerPrice + Me.Slab + 2 * buffer
                                        reason = "Slippage"
                                    End If
                                End If
                            End If
                            'If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            '    Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
                            '    If price - (bussinessOrder.ParentOrder.TriggerPrice - buffer) >= Me.Slab Then
                            '        Dim brkevn As Decimal = GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, bussinessOrder.ParentOrder.TransactionType)
                            '        triggerPrice = bussinessOrder.ParentOrder.AveragePrice + brkevn
                            '        reason = "Breakeven Mocement"
                            '    End If
                            'ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            '    Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                            '    If (bussinessOrder.ParentOrder.TriggerPrice + buffer) - price >= Me.Slab Then
                            '        Dim brkevn As Decimal = GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, bussinessOrder.ParentOrder.TransactionType)
                            '        triggerPrice = bussinessOrder.ParentOrder.AveragePrice - brkevn
                            '        reason = "Breakeven Mocement"
                            '    End If
                            'End If
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

    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso Me.Slab <> Decimal.MinValue Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.TargetOrder IsNot Nothing AndAlso bussinessOrder.TargetOrder.Count > 0 Then
                    For Each targetOrder In bussinessOrder.TargetOrder
                        If Not targetOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not targetOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not targetOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim price As Decimal = Decimal.MinValue
                            Dim reason As String = Nothing
                            Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.TriggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            If bussinessOrder.ParentOrder.AveragePrice <> bussinessOrder.ParentOrder.TriggerPrice Then
                                Dim target As Decimal = ConvertFloorCeling(Me.Slab * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    price = (bussinessOrder.ParentOrder.TriggerPrice - buffer) + target
                                    reason = "Slippage"
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    If targetOrder.TriggerPrice > bussinessOrder.ParentOrder.AveragePrice Then
                                        price = (bussinessOrder.ParentOrder.TriggerPrice + buffer) - target
                                        reason = "Slippage"
                                    End If
                                End If
                            End If
                            If price <> Decimal.MinValue AndAlso targetOrder.Price <> price Then
                                'Below portion have to be done in every modify stoploss order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(targetOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        If Val(currentSignalActivities.TargetModifyActivity.Supporting) = price Then
                                            Continue For
                                        End If
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, targetOrder, price, reason))
                            End If
                        End If
                    Next
                End If
            Next
        End If
        Return ret
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
            Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                              Return x.ParentOrderIdentifier Is Nothing AndAlso
                                                                              x.Status = IOrder.TypeOfStatus.TriggerPending
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                Dim currentTick As ITick = Me.TradableInstrument.LastTick
                Dim runningCandle As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    If runningCandle IsNot Nothing AndAlso runningCandle.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick Then
                        Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandle.PreviousPayload, currentTick)
                        If signal IsNot Nothing Then
                            If signal.Item4 <> parentOrder.TransactionType Then
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
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "Opposite Direction trade"))
                            End If
                        End If
                    End If
                Next
            End If

            Dim slOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                          Return x.ParentOrderIdentifier IsNot Nothing AndAlso
                                                                              x.Status = IOrder.TypeOfStatus.TriggerPending
                                                                      End Function)
            If slOrders IsNot Nothing AndAlso slOrders.Count > 0 Then
                Dim currentTick As ITick = Me.TradableInstrument.LastTick
                Dim runningCandle As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                For Each slOrder In slOrders
                    Dim bussinessOrder As IBusinessOrder = GetParentFromChildOrder(slOrder)
                    If bussinessOrder IsNot Nothing AndAlso runningCandle IsNot Nothing AndAlso runningCandle.PreviousPayload IsNot Nothing Then
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.TriggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
                                If price - (bussinessOrder.ParentOrder.TriggerPrice - buffer) >= Me.Slab Then
                                    If _breakevenMovedOrders Is Nothing Then _breakevenMovedOrders = New Concurrent.ConcurrentBag(Of String)
                                    If Not _breakevenMovedOrders.Contains(bussinessOrder.ParentOrderIdentifier) Then _breakevenMovedOrders.Add(bussinessOrder.ParentOrderIdentifier)
                                End If
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                                If (bussinessOrder.ParentOrder.TriggerPrice + buffer) - price >= Me.Slab Then
                                    If _breakevenMovedOrders Is Nothing Then _breakevenMovedOrders = New Concurrent.ConcurrentBag(Of String)
                                    If Not _breakevenMovedOrders.Contains(bussinessOrder.ParentOrderIdentifier) Then _breakevenMovedOrders.Add(bussinessOrder.ParentOrderIdentifier)
                                End If
                            End If
                            If _breakevenMovedOrders IsNot Nothing AndAlso _breakevenMovedOrders.Contains(bussinessOrder.ParentOrderIdentifier) Then
                                Dim exitTrade As Boolean = False
                                If runningCandle.PreviousPayload.SnapshotDateTime >= bussinessOrder.ParentOrder.TimeStamp Then
                                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        If runningCandle.PreviousPayload.ClosePrice.Value < bussinessOrder.ParentOrder.TriggerPrice - 2 * buffer Then
                                            exitTrade = True
                                        End If
                                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        If runningCandle.PreviousPayload.ClosePrice.Value > bussinessOrder.ParentOrder.TriggerPrice + 2 * buffer Then
                                            exitTrade = True
                                        End If
                                    End If
                                End If
                                If exitTrade Then
                                    Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(slOrder.Tag)
                                    If currentSignalActivities IsNot Nothing Then
                                        If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                            Continue For
                                        End If
                                    End If
                                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, slOrder, "Breakeven Exit"))
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
                        If currentDayFirstCandle.OpenPrice.Value < currentDayFirstCandle.PreviousPayload.ClosePrice.Value Then
                            If currentDayFirstCandle.HighPrice.Value >= currentDayFirstCandle.PreviousPayload.LowPrice.Value Then
                                Me.FilledPreviousClose = True
                            End If
                        ElseIf currentDayFirstCandle.OpenPrice.Value >= currentDayFirstCandle.PreviousPayload.ClosePrice.Value Then
                            If currentDayFirstCandle.LowPrice.Value <= currentDayFirstCandle.PreviousPayload.HighPrice.Value Then
                                Me.FilledPreviousClose = True
                            End If
                        End If
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
        'Dim supportedLowerSlab As Decimal = price * 0.2 / 100
        'Dim supportedUpperSlab As Decimal = price * 0.5 / 100
        'Dim supportedSlabList As List(Of Decimal) = slabList.FindAll(Function(x)
        '                                                                 Return x >= supportedLowerSlab AndAlso
        '                                                                 x <= supportedUpperSlab
        '                                                             End Function)
        'If supportedSlabList IsNot Nothing AndAlso supportedSlabList.Count > 0 Then
        '    ret = supportedSlabList.Min
        'End If
        Dim atrPer As Decimal = CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).InstrumentsData(Me.TradableInstrument.TradingSymbol).ATRPercentage
        Dim atr As Decimal = (atrPer / 100) * price
        Dim supportedSlabList As List(Of Decimal) = slabList.FindAll(Function(x)
                                                                         Return x <= atr / 8
                                                                     End Function)
        If supportedSlabList IsNot Nothing AndAlso supportedSlabList.Count > 0 Then
            ret = supportedSlabList.Max
        End If
        If Me.TradableInstrument.TradingSymbol.ToUpper = "IBULHSGFIN" Then
            ret = 1
        End If
        Return ret
    End Function

    Private Function GetRunningOrders(ByVal signalDirection As IOrder.TypeOfTransaction) As List(Of IOrder)
        Dim ret As List(Of IOrder) = Nothing
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder IsNot Nothing Then
                    If signalDirection = IOrder.TypeOfTransaction.None OrElse parentBusinessOrder.ParentOrder.TransactionType = signalDirection Then
                        If Not parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                                For Each slOrder In parentBusinessOrder.SLOrder
                                    If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                        If ret Is Nothing Then ret = New List(Of IOrder)
                                        ret.Add(parentBusinessOrder.ParentOrder)
                                    End If
                                Next
                            End If
                        End If
                    End If
                End If
            Next
        End If
        Return ret
    End Function

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing AndAlso
            Not candle.DeadCandle AndAlso Not candle.PreviousPayload.DeadCandle Then
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            If _potentialHighEntryPrice = Decimal.MinValue AndAlso _potentialLowEntryPrice = Decimal.MinValue Then
                _potentialHighEntryPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                _potentialLowEntryPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
            End If

            If _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
                If _entryChanged Then
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.LastPrice >= middlePoint + range * 60 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, middlePoint, IOrder.TypeOfTransaction.Buy)
                    ElseIf currentTick.LastPrice <= middlePoint - range * 60 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, middlePoint, IOrder.TypeOfTransaction.Sell)
                    End If
                Else
                    If Not IsActiveInstrument() Then
                        _potentialHighEntryPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                        _potentialLowEntryPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
                    End If
                    Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.LastPrice >= middlePoint + range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Buy
                    ElseIf currentTick.LastPrice <= middlePoint - range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Sell
                    End If
                    If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialHighEntryPrice - Me.Slab, IOrder.TypeOfTransaction.Buy)
                    ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialLowEntryPrice + Me.Slab, IOrder.TypeOfTransaction.Sell)
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsOrderExitedAtBreakeven(ByVal order As IBusinessOrder) As Boolean
        Dim ret As Boolean = False
        If order IsNot Nothing Then
            If order.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                If order.AllOrder IsNot Nothing AndAlso order.AllOrder.Count > 0 Then
                    For Each runningOrder In order.AllOrder
                        If runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss AndAlso
                            runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                            If Math.Abs(runningOrder.AveragePrice - order.ParentOrder.AveragePrice) < Me.Slab Then
                                ret = True
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsOrderForceExitedForBreakeven(ByVal order As IBusinessOrder) As Boolean
        Dim ret As Boolean = False
        If order IsNot Nothing Then
            If order.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                If order.AllOrder IsNot Nothing AndAlso order.AllOrder.Count > 0 Then
                    Dim buffer As Decimal = CalculateBuffer(order.ParentOrder.TriggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    For Each runningOrder In order.AllOrder
                        If runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                            If order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                If runningOrder.AveragePrice > order.ParentOrder.TriggerPrice - buffer - Me.Slab AndAlso
                                    runningOrder.AveragePrice < order.ParentOrder.TriggerPrice + Me.Slab Then
                                    ret = True
                                    Exit For
                                End If
                            ElseIf order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                If runningOrder.AveragePrice < order.ParentOrder.TriggerPrice + buffer + Me.Slab AndAlso
                                    runningOrder.AveragePrice > order.ParentOrder.TriggerPrice - Me.Slab Then
                                    ret = True
                                    Exit For
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
