Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class MomentumReversalStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private ReadOnly _dummyRSIConsumer As RSIConsumer

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
                {New RSIConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, MomentumReversalUserInputs).RSIPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyRSIConsumer = New RSIConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, MomentumReversalUserInputs).RSIPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Function MonitorAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task
        Throw New NotImplementedException()
    End Function
    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim MRUserSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
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
                'Exit Order block start
                Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                End If
                'Exit Order block end
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
    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim rsiConsumer As RSIConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyRSIConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                (Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint) Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, Idle Time Start:{2}, Idle Time End:{3}, RunningCandlePayloadSnapshotDateTime:{4}, PayloadGeneratedBy:{5}, IsHistoricalCompleted:{6}, Is Active Instrument:{7}, {8}, Current Time:{9}, Current LTP:{10}, TradingSymbol:{11}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            userSettings.IdleTimeStart.ToString,
                            userSettings.IdleTimeEnd.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            IsActiveInstrument(),
                            rsiConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso Not IsActiveInstrument() AndAlso
            Not Me.StrategyExitAllTriggerd Then
            'Not Me.StrategyExitAllTriggerd AndAlso Not IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime) Then
            If currentTime < userSettings.IdleTimeStart OrElse currentTime > userSettings.IdleTimeEnd Then
                Dim signal As Tuple(Of Boolean, Decimal) = GetSignalCandle(runningCandlePayload, currentTick, forcePrint)
                If signal IsNot Nothing AndAlso signal.Item1 Then
                    Dim triggerPrice As Decimal = signal.Item2 + userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).Buffer
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoploss As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).SL
                    Dim target As Decimal = ConvertFloorCeling(triggerPrice * 10 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)

                    If triggerPrice < 300 AndAlso currentTick.LastPrice < triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = Me.TradableInstrument.LotSize * userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).NumberOfLots}
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
    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    Dim entryPrice As Decimal = bussinessOrder.ParentOrder.AveragePrice
                    Dim potentialSL As Decimal = ConvertFloorCeling(entryPrice - userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).SL, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim firstTarget As Decimal = entryPrice + userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).FirstMovementLTP
                    Dim firstSL As Decimal = ConvertFloorCeling(potentialSL + userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).FirstMovementSL, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            Dim reason As String = Nothing
                            If slOrder.TriggerPrice >= firstSL Then
                                Dim gain As Decimal = currentTick.LastPrice - firstTarget
                                Dim multiplier As Integer = Math.Floor(gain / userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).OnwardMovementLTP)
                                If multiplier > 0 Then
                                    triggerPrice = firstSL + ConvertFloorCeling(userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).OnwardMovementSL * multiplier, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                    reason = String.Format("Onward movement {0}", multiplier)
                                End If
                            Else
                                If currentTick.LastPrice >= firstTarget Then
                                    triggerPrice = firstSL
                                    reason = "First movement"
                                End If
                            End If
                            If triggerPrice <> Decimal.MinValue AndAlso slOrder.TriggerPrice < triggerPrice Then
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
    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
            Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                              Return x.ParentOrderIdentifier Is Nothing AndAlso
                                                                              (x.Status = IOrder.TypeOfStatus.TriggerPending OrElse
                                                                              x.Status = IOrder.TypeOfStatus.Open)
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    If parentOrder.Status = IOrder.TypeOfStatus.TriggerPending OrElse
                        parentOrder.Status = IOrder.TypeOfStatus.Open Then
                        If Now() >= parentOrder.TimeStamp.AddMinutes(userSettings.TradeOpenTime) Then
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
                            ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "Trade not triggerd in max open time"))
                        End If
                    End If
                Next
            End If
        End If
        If forcePrint AndAlso ret IsNot Nothing AndAlso ret.Count > 0 Then
            For Each runningOrder In ret
                logger.Debug("***** Exit Order ***** Order ID:{0}, Reason:{1}, {2}", runningOrder.Item2.OrderIdentifier, runningOrder.Item3, Me.TradableInstrument.TradingSymbol)
            Next
        End If
        Return ret
    End Function
    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function
    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, ByVal reason As String) As Task
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

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick, ByVal executeCommand As Boolean) As Tuple(Of Boolean, Decimal)
        Dim ret As Tuple(Of Boolean, Decimal) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing Then
            Dim userSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
            Dim rsiConsumer As RSIConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyRSIConsumer)
            If rsiConsumer.ConsumerPayloads IsNot Nothing AndAlso rsiConsumer.ConsumerPayloads.Count > 0 AndAlso
                rsiConsumer.ConsumerPayloads.ContainsKey(candle.SnapshotDateTime) Then
                If CType(rsiConsumer.ConsumerPayloads(candle.SnapshotDateTime), RSIConsumer.RSIPayload).RSI.Value > userSettings.RSIOverSold AndAlso
                    CType(rsiConsumer.ConsumerPayloads(candle.SnapshotDateTime), RSIConsumer.RSIPayload).RSI.Value < userSettings.RSIOverBought Then
                    If Utilities.Time.IsDateTimeEqualTillMinutes(candle.SnapshotDateTime, userSettings.TradeStartTime) Then
                        ret = New Tuple(Of Boolean, Decimal)(True, candle.PreviousPayload.HighPrice.Value)
                    Else
                        If candle.PreviousPayload IsNot Nothing AndAlso
                            candle.PreviousPayload.PreviousPayload IsNot Nothing AndAlso
                            candle.PreviousPayload.PreviousPayload.PreviousPayload IsNot Nothing AndAlso
                            candle.PreviousPayload.PreviousPayload.PreviousPayload.PreviousPayload IsNot Nothing AndAlso
                            candle.PreviousPayload.PreviousPayload.PreviousPayload.PreviousPayload.SnapshotDateTime.Date = Now.Date Then
                            Dim entryPrice As Decimal = Math.Max(Math.Max(Math.Max(candle.PreviousPayload.HighPrice.Value, candle.PreviousPayload.PreviousPayload.HighPrice.Value), candle.PreviousPayload.PreviousPayload.PreviousPayload.HighPrice.Value), candle.PreviousPayload.PreviousPayload.PreviousPayload.PreviousPayload.HighPrice.Value)
                            ret = New Tuple(Of Boolean, Decimal)(True, entryPrice)
                        End If
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetLastOrderExitTime() As Date
        Dim ret As Date = Date.MinValue
        Dim lastExecutedOrder As IBusinessOrder = GetLastExecutedOrder()
        If lastExecutedOrder IsNot Nothing Then
            If lastExecutedOrder.AllOrder IsNot Nothing AndAlso lastExecutedOrder.AllOrder.Count > 0 Then
                For Each order In lastExecutedOrder.AllOrder
                    If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss Then
                        ret = If(order.TimeStamp > ret, order.TimeStamp, ret)
                    End If
                Next
            End If
        End If
        Return ret
    End Function

    Private Function IsLastTradeExitedAtCurrentCandle(ByVal currentCandleTime As Date) As Boolean
        Dim ret As Boolean = False
        Dim lastTradeExitTime As Date = GetLastOrderExitTime()
        If lastTradeExitTime <> Date.MinValue Then
            Dim blockDateInThisTimeframe As Date = Date.MinValue
            Dim timeframe As Integer = Me.ParentStrategy.UserSettings.SignalTimeFrame
            If Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Minute Mod timeframe = 0 Then
                blockDateInThisTimeframe = New Date(lastTradeExitTime.Year,
                                                    lastTradeExitTime.Month,
                                                    lastTradeExitTime.Day,
                                                    lastTradeExitTime.Hour,
                                                    Math.Floor(lastTradeExitTime.Minute / timeframe) * timeframe, 0)
            Else
                Dim exchangeStartTime As Date = New Date(lastTradeExitTime.Year, lastTradeExitTime.Month, lastTradeExitTime.Day, Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Hour, Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Minute, 0)
                Dim currentTime As Date = New Date(lastTradeExitTime.Year, lastTradeExitTime.Month, lastTradeExitTime.Day, lastTradeExitTime.Hour, lastTradeExitTime.Minute, 0)
                Dim timeDifference As Double = currentTime.Subtract(exchangeStartTime).TotalMinutes
                Dim adjustedTimeDifference As Integer = Math.Floor(timeDifference / timeframe) * timeframe
                Dim currentMinute As Date = exchangeStartTime.AddMinutes(adjustedTimeDifference)
                blockDateInThisTimeframe = New Date(lastTradeExitTime.Year, lastTradeExitTime.Month, lastTradeExitTime.Day, currentMinute.Hour, currentMinute.Minute, 0)
            End If
            If blockDateInThisTimeframe <> Date.MinValue Then
                ret = Utilities.Time.IsDateTimeEqualTillMinutes(blockDateInThisTimeframe, currentCandleTime)
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
                If _APIAdapter IsNot Nothing Then
                    RemoveHandler _APIAdapter.Heartbeat, AddressOf OnHeartbeat
                    RemoveHandler _APIAdapter.WaitingFor, AddressOf OnWaitingFor
                    RemoveHandler _APIAdapter.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                    RemoveHandler _APIAdapter.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                End If
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