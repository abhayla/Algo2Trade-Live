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
                'chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                '    {New EMAConsumer(chartConsumer, 5, TypeOfField.Close),
                '     New EMAConsumer(chartConsumer, 20, TypeOfField.Close),
                '     New SupertrendConsumer(chartConsumer, 7, 3)}
                RawPayloadDependentConsumers.Add(chartConsumer)
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
            'Dim slDelayCtr As Integer = 0
            Dim MRUserSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
            Dim instrumentName As String = Nothing
            If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
                instrumentName = Me.TradableInstrument.TradingSymbol.Remove(Me.TradableInstrument.TradingSymbol.Count - 8)
            Else
                instrumentName = Me.TradableInstrument.TradingSymbol
            End If
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If Me.GetOverallPL() <= Math.Abs(MRUserSettings.InstrumentsData(instrumentName).MaxLossPerStock) * -1 OrElse
                    Me.GetOverallPL() >= Math.Abs(MRUserSettings.InstrumentsData(instrumentName).MaxProfitPerStock) Then
                    Debug.WriteLine("Force Cancel for stock pl")
                    Await ForceExitAllTradesAsync("Force Cancel for stock pl").ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderDetails As Object = Nothing
                'Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                'If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                '    placeOrderDetails = Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                '    ''To store signal candle in order collection
                '    'Dim businessOrder As IBusinessOrder = New BusinessOrder With {
                '    '        .ParentOrderIdentifier = placeOrderDetails("data")("order_id"),
                '    '        .SignalCandle = placeOrderTrigger.Item2.SignalCandle
                '    '    }
                '    'businessOrder = Me.OrderDetails.GetOrAdd(businessOrder.ParentOrderIdentifier, businessOrder)
                '    'businessOrder.SignalCandle = placeOrderTrigger.Item2.SignalCandle
                '    'Me.OrderDetails.AddOrUpdate(businessOrder.ParentOrderIdentifier, businessOrder, Function(key, value) businessOrder)
                'End If
                _cts.Token.ThrowIfCancellationRequested()
                'If slDelayCtr = 3 Then
                '    slDelayCtr = 0
                Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                End If
                'End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                'slDelayCtr += 1
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
        Dim MRUserSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(MRUserSettings.SignalTimeFrame)
        Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(Me.TradableInstrument.ExchangeDetails.ExchangeType)

        Dim instrumentName As String = Nothing
        If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
            instrumentName = Me.TradableInstrument.TradingSymbol.Remove(Me.TradableInstrument.TradingSymbol.Count - 8)
        Else
            instrumentName = Me.TradableInstrument.TradingSymbol
        End If

        Dim parameters As PlaceOrderParameters = Nothing
        If Now < MRUserSettings.LastTradeEntryTime AndAlso runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= MRUserSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            Not IsActiveInstrument() AndAlso Me.GetTotalExecutedOrders() < MRUserSettings.InstrumentsData(instrumentName.ToUpper).NumberOfTrade AndAlso
            Not IsAnyTradeExitedInCurrentTimeframeCandle(MRUserSettings.SignalTimeFrame, runningCandlePayload.SnapshotDateTime) AndAlso
            Me.GetOverallPL() > Math.Abs(MRUserSettings.InstrumentsData(instrumentName).MaxLossPerStock) * -1 AndAlso
            Me.GetOverallPL() < Math.Abs(MRUserSettings.InstrumentsData(instrumentName).MaxProfitPerStock) AndAlso
            Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(MRUserSettings.MaxLossPercentagePerDay) * -1 / 100 AndAlso
            Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(MRUserSettings.MaxProfitPercentagePerDay) / 100 Then

            Dim MRTradePrice As Decimal = Nothing
            Dim price As Decimal = Nothing
            Dim triggerPrice As Decimal = Nothing
            Dim stoplossPrice As Decimal = Nothing
            Dim target As Decimal = Nothing
            Dim stoploss As Decimal = Nothing
            Dim quantity As Integer = Nothing
            If Me.TradableInstrument.RawInstrumentType.ToUpper = "FUT" Then
                quantity = Me.TradableInstrument.LotSize * MRUserSettings.InstrumentsData(instrumentName).Quantity
            Else
                If MRUserSettings.InstrumentsData(instrumentName).Capital <> Decimal.MinValue AndAlso
                    MRUserSettings.InstrumentsData(instrumentName).Capital > 0 Then
                    quantity = Math.Floor(MRUserSettings.InstrumentsData(instrumentName).Capital / (Math.Floor(Me.TradableInstrument.LastTick.LastPrice / 13)))
                Else
                    quantity = MRUserSettings.InstrumentsData(instrumentName).Quantity
                End If
            End If

            Dim benchmarkWicksSize As Double = runningCandlePayload.PreviousPayload.CandleRange * MRUserSettings.CandleWickSizePercentage / 100
            If runningCandlePayload.PreviousPayload.CandleRangePercentage > MRUserSettings.MinCandleRangePercentage Then
                'Which wick is bigger
                Dim differenceInBothWicks As Decimal = runningCandlePayload.PreviousPayload.CandleWicks.Top - runningCandlePayload.PreviousPayload.CandleWicks.Bottom

                If (differenceInBothWicks > 0 OrElse
                    (differenceInBothWicks = 0 AndAlso runningCandlePayload.PreviousPayload.CandleColor = Color.Green) OrElse
                    (differenceInBothWicks = 0 AndAlso runningCandlePayload.PreviousPayload.CandleColor = Color.White)) AndAlso
                    runningCandlePayload.PreviousPayload.CandleWicks.Top > benchmarkWicksSize Then

                    MRTradePrice = runningCandlePayload.PreviousPayload.HighPrice.Value
                    price = MRTradePrice + Math.Round(ConvertFloorCeling(MRTradePrice * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    triggerPrice = MRTradePrice + CalculateBuffer(MRTradePrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    stoplossPrice = runningCandlePayload.PreviousPayload.LowPrice.Value - CalculateBuffer(runningCandlePayload.PreviousPayload.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    target = Math.Round(ConvertFloorCeling((triggerPrice - stoplossPrice) * MRUserSettings.TargetMultiplier, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    If MRUserSettings.InstrumentsData(instrumentName).MaxTargetPercentagePerTrade <> Decimal.MinValue Then
                        target = Math.Min(target, MRTradePrice * MRUserSettings.InstrumentsData(instrumentName).MaxTargetPercentagePerTrade / 100)
                    End If
                    stoploss = GetModifiedStoplossAsync(triggerPrice, stoplossPrice, quantity)

                    If Me.TradableInstrument.LastTick.LastPrice < triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                       .Quantity = quantity,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss}
                    End If
                ElseIf (differenceInBothWicks < 0 OrElse
                    (differenceInBothWicks = 0 AndAlso runningCandlePayload.PreviousPayload.CandleColor = Color.Red)) AndAlso
                    runningCandlePayload.PreviousPayload.CandleWicks.Bottom > benchmarkWicksSize Then

                    MRTradePrice = runningCandlePayload.PreviousPayload.LowPrice.Value
                    price = MRTradePrice - Math.Round(ConvertFloorCeling(MRTradePrice * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    triggerPrice = MRTradePrice - CalculateBuffer(MRTradePrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    stoplossPrice = runningCandlePayload.PreviousPayload.HighPrice.Value + CalculateBuffer(runningCandlePayload.PreviousPayload.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    target = Math.Round(ConvertFloorCeling((stoplossPrice - triggerPrice) * MRUserSettings.TargetMultiplier, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    If MRUserSettings.InstrumentsData(instrumentName).MaxTargetPercentagePerTrade <> Decimal.MinValue Then
                        target = Math.Min(target, MRTradePrice * MRUserSettings.InstrumentsData(instrumentName).MaxTargetPercentagePerTrade / 100)
                    End If
                    stoploss = GetModifiedStoplossAsync(stoplossPrice, triggerPrice, quantity)

                    If Me.TradableInstrument.LastTick.LastPrice > triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                       .Quantity = quantity,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss}
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        'If parameters IsNot Nothing Then
        '    Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
        '    If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
        '        If currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
        '            currentSignalActivities.FirstOrDefault.EntryActivity.LastException IsNot Nothing AndAlso
        '            currentSignalActivities.FirstOrDefault.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
        '            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "")
        '        ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
        '            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
        '            'ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
        '            '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
        '        Else
        '            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        '        End If
        '    Else
        '        ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
        '    End If
        'Else
        '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        'End If
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso
                    parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    Dim parentOrderPrice As Decimal = parentBusinessOrder.ParentOrder.AveragePrice
                    Dim potentialSLPrice As Decimal = Nothing
                    Dim triggerPrice As Decimal = Nothing
                    Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(parentOrderId, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                    If signalCandle IsNot Nothing Then
                        If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            potentialSLPrice = signalCandle.LowPrice.Value - CalculateBuffer(signalCandle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            triggerPrice = GetModifiedStoplossAsync(parentOrderPrice, potentialSLPrice, parentBusinessOrder.ParentOrder.Quantity)
                            triggerPrice = Math.Round(ConvertFloorCeling(parentOrderPrice - triggerPrice, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                        ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            potentialSLPrice = signalCandle.HighPrice.Value + CalculateBuffer(signalCandle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            triggerPrice = GetModifiedStoplossAsync(potentialSLPrice, parentOrderPrice, parentBusinessOrder.ParentOrder.Quantity)
                            triggerPrice = Math.Round(ConvertFloorCeling(parentOrderPrice + triggerPrice, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                        End If

                        Dim potentialStoplossPrice As Decimal = Nothing
                        For Each slOrder In parentBusinessOrder.SLOrder
                            If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                                Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                                Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                                If slOrder.TriggerPrice <> triggerPrice Then
                                    'Below portion have to be done in every modify stoploss order trigger
                                    Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(slOrder.Tag)
                                    If currentSignalActivities IsNot Nothing Then
                                        If currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                            Continue For
                                        End If
                                    End If
                                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, ""))
                                    'Else
                                    '    Debug.WriteLine(String.Format("Stoploss modified {0} Quantity:{1}, ID:{2}", Me.GenerateTag(), slOrder.Quantity, slOrder.OrderIdentifier))
                                End If
                            End If
                        Next
                    End If
                End If
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
    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function
    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
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
                        Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(parentOrder.OrderIdentifier, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                        If signalCandle IsNot Nothing AndAlso runningCandle.SnapshotDateTime >= signalCandle.SnapshotDateTime.AddMinutes(Me.ParentStrategy.UserSettings.SignalTimeFrame * 2) Then
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
                            ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, ""))
                        End If
                    End If
                Next
            End If
        End If
        Return ret
    End Function
    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, ByVal reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }
            Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function
    Private Function GetModifiedStoplossAsync(ByVal entryPrice As Decimal, ByVal stoplossPrice As Decimal, ByVal quantity As Integer) As Decimal
        Dim ret As Decimal = Nothing
        Dim MRUserSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        Dim capitalRequiredWithMargin As Decimal = (entryPrice * quantity / 30)
        'Dim pl As Decimal = Await Me._APIAdapter.CalculatePLWithBrokerageAsync(Me.TradableInstrument.TradingSymbol, entryPrice, stoplossPrice, quantity, Me.TradableInstrument.Exchange).ConfigureAwait(False)
        Dim pl As Decimal = (stoplossPrice - entryPrice) * quantity
        If Math.Abs(pl) > capitalRequiredWithMargin * MRUserSettings.MaxCapitalProtectionPercentage / 100 Then
            ret = capitalRequiredWithMargin * MRUserSettings.MaxCapitalProtectionPercentage / 100 / quantity
        Else
            ret = Math.Abs(entryPrice - stoplossPrice)
        End If
        ret = Math.Round(ConvertFloorCeling(ret, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
        Return ret
        'Dim ret As Decimal = Nothing
        'Dim MRUserSettings As MomentumReversalUserInputs = Me.ParentStrategy.UserSettings
        'Dim instrumentName As String = Nothing
        'If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
        '    instrumentName = Me.TradableInstrument.TradingSymbol.Remove(Me.TradableInstrument.TradingSymbol.Count - 8)
        'Else
        '    instrumentName = Me.TradableInstrument.TradingSymbol
        'End If
        'Dim capitalRequiredWithMargin As Decimal = (entryPrice * quantity / 30)
        'Dim pl As Decimal = Await Me._APIAdapter.CalculatePLWithBrokerageAsync(Me.TradableInstrument.TradingSymbol, entryPrice, stoplossPrice, quantity, Me.TradableInstrument.Exchange).ConfigureAwait(False)
        'Dim stoplossTobeCalculateFrom As Decimal = Math.Min(Math.Abs(pl), Math.Min(capitalRequiredWithMargin * MRUserSettings.MaxCapitalProtectionPercentage / 100, Math.Abs(MRUserSettings.InstrumentsData(instrumentName).MaxLossPerTrade)))
        'If Math.Abs(pl) <> stoplossTobeCalculateFrom Then
        '    ret = stoplossTobeCalculateFrom / quantity
        'Else
        '    ret = Math.Abs(entryPrice - stoplossPrice)
        'End If
        'ret = Math.Round(ConvertFloorCeling(ret, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
        'Return ret
    End Function
    Private Function IsAnyTradeExitedInCurrentTimeframeCandle(ByVal timeFrame As Integer, ByVal currentTimeframeCandleTime As Date) As Boolean
        Dim ret As Boolean = False
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder IsNot Nothing Then
                    'If parentBusinessOrder.ParentOrder.Status = "COMPLETE" OrElse parentBusinessOrder.ParentOrder.Status = "OPEN" Then
                    If Not parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Rejected Then
                        If parentBusinessOrder.AllOrder IsNot Nothing AndAlso parentBusinessOrder.AllOrder.Count > 0 Then
                            For Each slOrder In parentBusinessOrder.AllOrder
                                If slOrder.Status = IOrder.TypeOfStatus.Complete OrElse slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                    Dim orderExitBlockTime As Date = New Date(slOrder.TimeStamp.Year,
                                                                            slOrder.TimeStamp.Month,
                                                                            slOrder.TimeStamp.Day,
                                                                            slOrder.TimeStamp.Hour,
                                                                            Math.Floor(slOrder.TimeStamp.Minute / timeFrame) * timeFrame, 0)

                                    If orderExitBlockTime = currentTimeframeCandleTime Then
                                        ret = True
                                        Exit For
                                    End If
                                End If
                            Next
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