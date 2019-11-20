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
    Private _firstTradedQuantity As Integer = Integer.MinValue

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
            Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
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
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        Dim parameter As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            Me.TradableInstrument.IsHistoricalCompleted AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not Me.StrategyExitAllTriggerd Then
            If Me.Slab = Decimal.MinValue Then
                If userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab <> Decimal.MinValue Then
                    Me.Slab = userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Slab
                Else
                    Me.Slab = CalculateSlab(runningCandlePayload.OpenPrice.Value)
                End If
            End If
            If _firstTradedQuantity = Integer.MinValue Then
                If userSettings.CashInstrument Then
                    _firstTradedQuantity = CalculateQuantityFromStoploss(runningCandlePayload.OpenPrice.Value, runningCandlePayload.OpenPrice.Value - Me.Slab, -50)
                ElseIf userSettings.FutureInstrument Then
                    _firstTradedQuantity = CalculateQuantityFromInvestment(runningCandlePayload.OpenPrice.Value, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapitalPerStock, userSettings.AllowToIncreaseQuantity)
                End If
            End If

            Dim longActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Buy)
            Dim shortActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Sell)
            Dim buyPrice As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
            Dim sellPrice As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)

            If longActiveOrders IsNot Nothing AndAlso longActiveOrders.Count > 0 Then
                For Each runningActiveTrade In longActiveOrders
                    If runningActiveTrade.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent Then
                        'If runningActiveTrade.Status = IOrder.TypeOfStatus.Complete Then
                        sellPrice = runningActiveTrade.TriggerPrice - Me.Slab
                        'Else
                        '    sellPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
                        'End If
                        Exit For
                    End If
                Next
            End If
            If shortActiveOrders IsNot Nothing AndAlso shortActiveOrders.Count > 0 Then
                For Each runningActiveTrade In shortActiveOrders
                    If runningActiveTrade.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent Then
                        'If runningActiveTrade.Status = IOrder.TypeOfStatus.Complete Then
                        buyPrice = runningActiveTrade.TriggerPrice + Me.Slab
                        'Else
                        '    buyPrice = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                        'End If
                        Exit For
                    End If
                Next
            End If

            Dim entryDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
            If (longActiveOrders Is Nothing OrElse longActiveOrders.Count = 0) AndAlso
                (shortActiveOrders Is Nothing OrElse shortActiveOrders.Count = 0) Then
                Dim middlePrice As Decimal = (buyPrice + sellPrice) / 2
                If currentTick.LastPrice > middlePrice Then
                    entryDirection = IOrder.TypeOfTransaction.Buy
                ElseIf currentTick.LastPrice < middlePrice Then
                    entryDirection = IOrder.TypeOfTransaction.Sell
                End If
            ElseIf longActiveOrders Is Nothing OrElse longActiveOrders.Count = 0 Then
                entryDirection = IOrder.TypeOfTransaction.Buy
            ElseIf shortActiveOrders Is Nothing OrElse shortActiveOrders.Count = 0 Then
                entryDirection = IOrder.TypeOfTransaction.Sell
            End If

            If entryDirection = IOrder.TypeOfTransaction.Buy Then
                Dim triggerPrice As Decimal = buyPrice
                Dim price As Decimal = triggerPrice + ConvertFloorCeling(Me.Slab / 2, TradableInstrument.TickSize, RoundOfType.Celing)
                Dim stoplossPrice As Decimal = triggerPrice - Me.Slab
                Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(Me.Slab * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                If currentTick.LastPrice < triggerPrice Then
                    parameter = New PlaceOrderParameters(runningCandlePayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
                End If
            ElseIf entryDirection = IOrder.TypeOfTransaction.Sell Then
                Dim triggerPrice As Decimal = sellPrice
                Dim price As Decimal = triggerPrice - ConvertFloorCeling(Me.Slab / 2, TradableInstrument.TickSize, RoundOfType.Celing)
                Dim stoplossPrice As Decimal = triggerPrice + Me.Slab
                Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(Me.Slab * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                If currentTick.LastPrice > triggerPrice Then
                    parameter = New PlaceOrderParameters(runningCandlePayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = _firstTradedQuantity}
                End If
            End If

            If forcePrint Then
                Try
                    logger.Debug("Place Order Details: Long Active Trades:{0}, Short Active Trades:{1}, LTP:{2}, Slab:{3}, Trading Symbol:{4}",
                                 If(longActiveOrders Is Nothing, "Nothing", longActiveOrders.Count),
                                 If(shortActiveOrders Is Nothing, "Nothing", shortActiveOrders.Count),
                                 currentTick.LastPrice,
                                 Me.Slab,
                                 Me.TradableInstrument.TradingSymbol)
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
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
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameter, parameter.ToString))
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
                            If Not slOrder.SupportingFlag Then
                                If bussinessOrder.ParentOrder.AveragePrice <> bussinessOrder.ParentOrder.TriggerPrice Then
                                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        triggerPrice = bussinessOrder.ParentOrder.TriggerPrice - Me.Slab
                                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        triggerPrice = bussinessOrder.ParentOrder.TriggerPrice + Me.Slab
                                    End If
                                    reason = "Slippage"
                                    If forcePrint Then slOrder.SupportingFlag = True
                                Else
                                    slOrder.SupportingFlag = True
                                End If
                            Else
                                If slOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy) + Me.Slab
                                    If price < slOrder.TriggerPrice Then
                                        triggerPrice = price
                                        reason = "SL order LTP based Movement"
                                    End If
                                ElseIf slOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell) - Me.Slab
                                    If price > slOrder.TriggerPrice Then
                                        triggerPrice = price
                                        reason = "SL order LTP based Movement"
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
                                            If currentSignalActivities.StoplossModifyActivity.RequestRemarks.Contains("SL order") Then
                                                Continue For
                                            End If
                                        End If
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, reason))
                            End If
                        End If
                    Next
                End If
                If bussinessOrder.ParentOrder IsNot Nothing AndAlso bussinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.TriggerPending Then
                    Dim triggerPrice As Decimal = Decimal.MinValue
                    Dim reason As String = Nothing
                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Buy)
                        Dim runningOrder As List(Of IOrder) = GetRunningOrders(IOrder.TypeOfTransaction.Sell)
                        If runningOrder IsNot Nothing AndAlso runningOrder.Count > 0 Then price = price + Me.Slab
                        If price < bussinessOrder.ParentOrder.TriggerPrice Then
                            triggerPrice = price
                            reason = "Parent order LTP based Movement"
                        End If
                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        Dim price As Decimal = GetSlabBasedLevel(currentTick.LastPrice, IOrder.TypeOfTransaction.Sell)
                        Dim runningOrder As List(Of IOrder) = GetRunningOrders(IOrder.TypeOfTransaction.Buy)
                        If runningOrder IsNot Nothing AndAlso runningOrder.Count > 0 Then price = price - Me.Slab
                        If price > bussinessOrder.ParentOrder.TriggerPrice Then
                            triggerPrice = price
                            reason = "Parent order LTP based Movement"
                        End If
                    End If
                    If triggerPrice <> Decimal.MinValue AndAlso bussinessOrder.ParentOrder.TriggerPrice <> triggerPrice Then
                        'Below portion have to be done in every modify stoploss order trigger
                        Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(bussinessOrder.ParentOrder.Tag)
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
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, bussinessOrder.ParentOrder, triggerPrice, reason))
                    End If
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
        Dim supportedLowerSlab As Decimal = price * 0.2 / 100
        Dim supportedUpperSlab As Decimal = price * 0.5 / 100
        Dim supportedSlabList As List(Of Decimal) = slabList.FindAll(Function(x)
                                                                         Return x >= supportedLowerSlab AndAlso
                                                                         x <= supportedUpperSlab
                                                                     End Function)
        If supportedSlabList IsNot Nothing AndAlso supportedSlabList.Count > 0 Then
            ret = supportedSlabList.Min
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
