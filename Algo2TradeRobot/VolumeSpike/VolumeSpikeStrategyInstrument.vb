Imports NLog
Imports System.Threading
Imports Utilities.Numbers
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Entities.Indicators

Public Class VolumeSpikeStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

    Public EligibleToTakeTrade As Boolean
    Public StopStrategyInstrument As Boolean
    Public VolumeChangePercentage As Decimal

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _potentialHighEntryPrice As Decimal = Decimal.MinValue
    Private _potentialLowEntryPrice As Decimal = Decimal.MinValue
    Private _signalCandle As OHLCPayload = Nothing
    Private _signalType As TypeOfSignal = TypeOfSignal.None
    Private _entryChanged As Boolean = False
    Private _targetMultiplier As Decimal = Decimal.MinValue
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
                If Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures OrElse
                    CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).CashInstrument Then
                    chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                    {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).ATRPeriod)}
                End If
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        Me.EligibleToTakeTrade = False
        Me.VolumeChangePercentage = Decimal.MinValue
        Me.StopStrategyInstrument = False
        If Not CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).AutoSelectStock Then
            Me.EligibleToTakeTrade = True
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
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
                'Calculate volume spike start
                If userSettings.AutoSelectStock AndAlso Me.VolumeChangePercentage = Decimal.MinValue AndAlso Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                    Await GetVolumeSpike().ConfigureAwait(False)
                End If
                'Calculate volume spike end
                _cts.Token.ThrowIfCancellationRequested()
                If Me.StopStrategyInstrument Then
                    Exit While
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If Me.EligibleToTakeTrade Then
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
                    ''Modify Order block start
                    'Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                    'If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    '    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    'End If
                    ''Modify Order block end
                    _cts.Token.ThrowIfCancellationRequested()
                    'Exit Order block start
                    Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                    If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                        Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                    End If
                    'Exit Order block end
                    _cts.Token.ThrowIfCancellationRequested()
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
        Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        If Not _entryChanged AndAlso GetSignalCandleATR() <> Decimal.MinValue AndAlso _signalCandle IsNot Nothing Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                Dim firstOrder As IBusinessOrder = OrderDetails.OrderBy(Function(x)
                                                                            Return x.Value.ParentOrder.TimeStamp
                                                                        End Function).FirstOrDefault.Value
                If firstOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                    If firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        Select Case _signalType
                            Case TypeOfSignal.CandleHalf
                                _potentialLowEntryPrice = _potentialHighEntryPrice - 2 * ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            Case TypeOfSignal.PinBar, TypeOfSignal.TweezerPattern
                                _potentialLowEntryPrice = _potentialHighEntryPrice - 2 * ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        End Select
                    ElseIf firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        Select Case _signalType
                            Case TypeOfSignal.CandleHalf
                                _potentialHighEntryPrice = _potentialLowEntryPrice + 2 * ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            Case TypeOfSignal.PinBar, TypeOfSignal.TweezerPattern
                                _potentialHighEntryPrice = _potentialLowEntryPrice + 2 * ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        End Select
                    End If
                    _entryChanged = True
                End If
            End If
        End If

        If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
            _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
            logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
            If _signalCandle IsNot Nothing Then
                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Signal Candle Time:{5}, Signal Candle Range:{6}, Signal Candle Source:{7}, {8}, Is Active Instrument:{9}, Number Of Trade:{10}, OverAll PL:{11}, Is Target Reached:{12}, Buy Entry:{13}, Sell Entry:{14}, Signal Type:{15}, Current Time:{16}, Current LTP:{17}, TradingSymbol:{18}",
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
                            Me.GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            IsAnyTradeTargetReached(),
                            _potentialHighEntryPrice,
                            _potentialLowEntryPrice,
                            _signalCandle.ToString,
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
            Else
                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Current Candle Time:{5}, Current Candle Range:{6}, Current Candle Source:{7}, {8}, Is Active Instrument:{9}, Number Of Trade:{10}, OverAll PL:{11}, Is Target Reached:{12}, Current Time:{13}, Current LTP:{14}, TradingSymbol:{15}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToShortTimeString,
                            runningCandlePayload.PreviousPayload.CandleRange,
                            runningCandlePayload.PreviousPayload.PayloadGeneratedBy.ToString,
                            atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                            IsActiveInstrument(),
                            Me.GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            IsAnyTradeTargetReached(),
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
            End If
        End If

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not IsAnyTradeTargetReached() AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() > Math.Abs(userSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso Not Me.StrategyExitAllTriggerd Then
            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                Dim buffer As Decimal = CalculateBuffer(signal.Item2, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)
                Dim quantity As Integer = 0
                If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                    Dim triggerPrice As Decimal = signal.Item2 + buffer
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3
                    If _signalType = TypeOfSignal.CandleHalf Then stoplossPrice = stoplossPrice - buffer
                    Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * _targetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        quantity = CalculateQuantityFromInvestment(triggerPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, True)
                        _firstTradeQuantity = quantity
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 AndAlso currentTick.LastPrice < triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = quantity}
                    End If
                ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                    Dim triggerPrice As Decimal = signal.Item2 - buffer
                    Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3
                    If _signalType = TypeOfSignal.CandleHalf Then stoplossPrice = stoplossPrice + buffer
                    Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * _targetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        quantity = CalculateQuantityFromInvestment(triggerPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, True)
                        _firstTradeQuantity = quantity
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 AndAlso currentTick.LastPrice > triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = quantity}

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

    Protected Overrides Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
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
                                                                              x.Status = IOrder.TypeOfStatus.TriggerPending
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    Dim runningCandle As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                    If runningCandle IsNot Nothing AndAlso runningCandle.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick Then
                        Dim orderCancelled As Boolean = False
                        If Not _entryChanged AndAlso _signalType = TypeOfSignal.PinBar Then
                            Dim tradePlacementTime As Date = New Date(Now.Year, Now.Month, Now.Day, parentOrder.TimeStamp.Hour, parentOrder.TimeStamp.Minute, 0)
                            If runningCandle.SnapshotDateTime >= tradePlacementTime.AddMinutes(Me.ParentStrategy.UserSettings.SignalTimeFrame) Then
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        Continue For
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "Pin Bar not triggered"))
                                orderCancelled = True
                                _signalCandle = Nothing
                                _potentialHighEntryPrice = Decimal.MinValue
                                _potentialLowEntryPrice = Decimal.MinValue
                                '_signalType = TypeOfSignal.None
                            End If
                        End If
                        If Not orderCancelled AndAlso _signalCandle IsNot Nothing Then
                            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(_signalCandle, Me.TradableInstrument.LastTick)
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

    Private Async Function GetVolumeSpike() As Task
        Await Task.Delay(0).ConfigureAwait(False)
        If Me.VolumeChangePercentage = Decimal.MinValue AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            If Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                Me.TradableInstrument.FetchHistorical = False
            End If
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
            Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
                Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                             If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                                 Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                             Else
                                                                                                                 Return Nothing
                                                                                                             End If
                                                                                                         End Function)

                If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                    Dim currentDayVolumeSum As Long = 0
                    Dim previousDaysVolumeSum As Long = 0
                    Dim counter As Integer = 0
                    For Each runningPayload In XMinutePayloadConsumer.ConsumerPayloads.Keys.OrderByDescending(Function(x)
                                                                                                                  Return x
                                                                                                              End Function)
                        Dim firstCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, 9, 15, 0)
                        Dim secondCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, 9, 16, 0)
                        If runningPayload.Date = Now.Date Then
                            If runningPayload = firstCandle OrElse runningPayload = secondCandle Then
                                currentDayVolumeSum += CType(XMinutePayloadConsumer.ConsumerPayloads(runningPayload), OHLCPayload).Volume.Value
                            End If
                        ElseIf runningPayload.Date < Now.Date Then
                            If runningPayload = firstCandle OrElse runningPayload = secondCandle Then
                                previousDaysVolumeSum += CType(XMinutePayloadConsumer.ConsumerPayloads(runningPayload), OHLCPayload).Volume.Value
                                counter += 1
                                If counter = 10 Then Exit For
                            End If
                        End If
                    Next
                    If currentDayVolumeSum <> 0 AndAlso previousDaysVolumeSum <> 0 Then
                        Me.VolumeChangePercentage = ((currentDayVolumeSum / (previousDaysVolumeSum / 5)) - 1) * 100
                    End If
                End If
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
                        If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
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

    Private Function GetSignalCandleATR() As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If _signalCandle IsNot Nothing Then
            Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
            If atrConsumer IsNot Nothing AndAlso atrConsumer.ConsumerPayloads IsNot Nothing AndAlso atrConsumer.ConsumerPayloads.Count > 0 AndAlso
                atrConsumer.ConsumerPayloads.ContainsKey(_signalCandle.SnapshotDateTime) Then
                ret = Math.Round(CType(atrConsumer.ConsumerPayloads(_signalCandle.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)
            End If
        End If
        Return ret
    End Function

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing AndAlso
            Not candle.DeadCandle AndAlso Not candle.PreviousPayload.DeadCandle Then
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
            If _potentialHighEntryPrice = Decimal.MinValue AndAlso _potentialLowEntryPrice = Decimal.MinValue Then
                If IsCandleHalf(candle) AndAlso candle.CandleRange > CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.CandleHalf
                    _targetMultiplier = userSettings.TargetMultiplier
                ElseIf IsPinBar(candle) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.PinBar
                    _targetMultiplier = Math.Floor(userSettings.TargetMultiplier - userSettings.TargetMultiplier * 25 / 100)
                ElseIf IsTweezerPattern(candle) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.TweezerPattern
                    _targetMultiplier = Math.Floor(userSettings.TargetMultiplier - userSettings.TargetMultiplier * 25 / 100)
                End If
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
                    Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.LastPrice >= middlePoint + range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Buy
                    ElseIf currentTick.LastPrice <= middlePoint - range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Sell
                    End If
                    Select Case _signalType
                        Case TypeOfSignal.TweezerPattern, TypeOfSignal.PinBar
                            If _signalType = TypeOfSignal.PinBar Then tradeDirection = GetPinBarEntryDirection(_signalCandle)
                            If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialHighEntryPrice - ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing), IOrder.TypeOfTransaction.Buy)
                            ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialLowEntryPrice + ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing), IOrder.TypeOfTransaction.Sell)
                            End If
                        Case TypeOfSignal.CandleHalf
                            If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialLowEntryPrice, IOrder.TypeOfTransaction.Buy)
                            ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialHighEntryPrice, IOrder.TypeOfTransaction.Sell)
                            End If
                    End Select
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsTweezerPattern(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        If candle.PreviousPayload.CandleColor = Color.Red Then
            If candle.PreviousPayload.CandleWicks.Top <= candle.PreviousPayload.CandleRange * 50 / 100 AndAlso
                candle.PreviousPayload.CandleWicks.Bottom <= candle.PreviousPayload.CandleRange * 25 / 100 Then
                Dim buffer As Decimal = CalculateBuffer(candle.PreviousPayload.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                If candle.CandleColor = Color.Green AndAlso candle.HighPrice.Value >= candle.PreviousPayload.HighPrice.Value - buffer Then
                    If candle.CandleWicks.Top <= candle.CandleRange * 25 / 100 AndAlso
                        candle.CandleWicks.Bottom <= candle.CandleRange * 50 / 100 Then
                        ret = True
                    End If
                End If
            End If
        ElseIf candle.PreviousPayload.CandleColor = Color.Green Then
            If candle.PreviousPayload.CandleWicks.Top <= candle.PreviousPayload.CandleRange * 25 / 100 AndAlso
                candle.PreviousPayload.CandleWicks.Bottom <= candle.PreviousPayload.CandleRange * 50 / 100 Then
                Dim buffer As Decimal = CalculateBuffer(candle.PreviousPayload.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                If candle.CandleColor = Color.Red AndAlso candle.LowPrice.Value <= candle.PreviousPayload.LowPrice.Value + buffer Then
                    If candle.CandleWicks.Top <= candle.CandleRange * 50 / 100 AndAlso
                        candle.CandleWicks.Bottom <= candle.CandleRange * 25 / 100 Then
                        ret = True
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsCandleHalf(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        Dim middlePoint As Decimal = (candle.PreviousPayload.HighPrice.Value + candle.PreviousPayload.LowPrice.Value) / 2
        If candle.SnapshotDateTime >= Me.ParentStrategy.UserSettings.TradeStartTime AndAlso
            candle.CandleRange <= candle.PreviousPayload.CandleRange / 2 AndAlso
            (candle.HighPrice.Value <= ConvertFloorCeling(middlePoint, Me.TradableInstrument.TickSize, RoundOfType.Floor) OrElse
            candle.LowPrice.Value >= ConvertFloorCeling(middlePoint, Me.TradableInstrument.TickSize, RoundOfType.Celing)) Then
            ret = True
        End If
        Return ret
    End Function

    Private Function IsPinBar(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        Dim candleHighBuffer As Decimal = CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
        Dim candleLowBuffer As Decimal = CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
        If candle.CandleWicks.Bottom >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) AndAlso
            candle.Volume.Value >= candle.PreviousPayload.Volume.Value AndAlso
            candle.LowPrice.Value < candle.PreviousPayload.LowPrice.Value Then
            Dim dayLow As Decimal = GetDayLow(candle)
            If dayLow <> Decimal.MinValue AndAlso candle.LowPrice.Value <= dayLow + CalculateBuffer(dayLow, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                ret = True
            End If
        ElseIf candle.CandleWicks.Top >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) AndAlso
            candle.Volume.Value >= candle.PreviousPayload.Volume.Value AndAlso
            candle.HighPrice.Value > candle.PreviousPayload.HighPrice.Value Then
            Dim dayHigh As Decimal = GetDayHigh(candle)
            If dayHigh <> Decimal.MinValue AndAlso candle.HighPrice.Value >= dayHigh - CalculateBuffer(dayHigh, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                ret = True
            End If
        End If
        Return ret
    End Function

    Private Function GetDayHigh(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
            Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                         If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                             Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                         Else
                                                                                                             Return Nothing
                                                                                                         End If
                                                                                                     End Function)

            If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                ret = XMinutePayloadConsumer.ConsumerPayloads.Values.Max(Function(x)
                                                                             Dim y As OHLCPayload = x
                                                                             If y.SnapshotDateTime.Date = Now.Date AndAlso
                                                                              y.SnapshotDateTime <= candle.SnapshotDateTime Then
                                                                                 Return y.HighPrice.Value
                                                                             Else
                                                                                 Return Decimal.MinValue
                                                                             End If
                                                                         End Function)
            End If
        End If
        Return ret
    End Function

    Private Function GetDayLow(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
            Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                         If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                             Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                         Else
                                                                                                             Return Nothing
                                                                                                         End If
                                                                                                     End Function)

            If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                ret = XMinutePayloadConsumer.ConsumerPayloads.Values.Min(Function(x)
                                                                             Dim y As OHLCPayload = x
                                                                             If y.SnapshotDateTime.Date = Now.Date AndAlso
                                                                              y.SnapshotDateTime <= candle.SnapshotDateTime Then
                                                                                 Return y.LowPrice.Value
                                                                             Else
                                                                                 Return Decimal.MaxValue
                                                                             End If
                                                                         End Function)
            End If
        End If
        Return ret
    End Function

    Private Function GetPinBarEntryDirection(ByVal candle As OHLCPayload) As IOrder.TypeOfTransaction
        Dim ret As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
        If _signalCandle IsNot Nothing Then
            Dim candleHighBuffer As Decimal = CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
            Dim candleLowBuffer As Decimal = CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
            If candle.CandleWicks.Bottom >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) Then
                ret = IOrder.TypeOfTransaction.Buy
            ElseIf candle.CandleWicks.Top >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) Then
                ret = IOrder.TypeOfTransaction.Sell
            End If
        End If
        Return ret
    End Function

    Enum TypeOfSignal
        CandleHalf = 1
        PinBar
        TweezerPattern
        None
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
