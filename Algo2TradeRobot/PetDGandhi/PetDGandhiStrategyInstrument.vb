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

    Private lastPrevPayloadPlaceOrder As String = ""
    Private ReadOnly _dummyEMAConsumer As EMAConsumer
    Private ReadOnly _dummyPivotHighLowConsumer As PivotHighLowConsumer
    Private _signalLevel As SignalLevels
    Private _usedSignalLevel As SignalLevels

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
                {New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).EMAPeriod, TypeOfField.Close),
                New PivotHighLowConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).PivotHighLowStrict)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyEMAConsumer = New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).EMAPeriod, TypeOfField.Close)
                _dummyPivotHighLowConsumer = New PivotHighLowConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).PivotHighLowStrict)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        _signalLevel = New SignalLevels
        _usedSignalLevel = New SignalLevels
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If

                _cts.Token.ThrowIfCancellationRequested()
                If Me.GetOverallPL() <= Math.Abs(petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxLossPerStock) * -1 OrElse
                    Me.GetOverallPL() >= Math.Abs(petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxProfitPerStock) Then
                    Await ForceExitAllTradesAsync("Force Cancel for stock PL").ConfigureAwait(False)
                End If

                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                'If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                '    Dim placeOrderResponse As Object = Nothing
                '    If placeOrderTrigger.Item2.OrderType = IOrder.TypeOfOrder.SL Then
                '        placeOrderResponse = Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                '    ElseIf placeOrderTrigger.Item2.OrderType = IOrder.TypeOfOrder.Limit Then
                '        placeOrderResponse = Await ExecuteCommandAsync(ExecuteCommands.PlaceBOLimitMISOrder, Nothing).ConfigureAwait(False)
                '    End If
                'End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim exitOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrdersTrigger IsNot Nothing AndAlso exitOrdersTrigger.Count > 0 Then
                    Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
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
        Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(petDGandhiUserSettings.SignalTimeFrame)
        Dim emaConsumer As EMAConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyEMAConsumer)
        Dim pivotHighLowConsumer As PivotHighLowConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyPivotHighLowConsumer)
        Dim ltp As Decimal = Me.TradableInstrument.LastTick.LastPrice

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadPlaceOrder OrElse forcePrint Then
                    lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    Dim signalData As SignalLevels = GetBuySellLevel(runningCandlePayload, emaConsumer, pivotHighLowConsumer, True)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, LastTradeEntryTime:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsActiveInstrument:{4}, IsHistoricalCompleted:{5}, MTM Loss: {6}, MTM Profit: {7}, TotalStrategyPL:{8}, EMA:{9}, Pivot High:{10}, Pivot Low:{11}, LTP:{12}, TradingSymbol:{13}",
                    petDGandhiUserSettings.TradeStartTime,
                    petDGandhiUserSettings.LastTradeEntryTime,
                    runningCandlePayload.SnapshotDateTime.ToString,
                    runningCandlePayload.PayloadGeneratedBy.ToString,
                    IsActiveInstrument(),
                    Me.TradableInstrument.IsHistoricalCompleted,
                    Math.Abs(petDGandhiUserSettings.MaxLossPerDay) * -1,
                    Math.Abs(petDGandhiUserSettings.MaxProfitPerDay),
                    Me.ParentStrategy.GetTotalPL,
                    CType(emaConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), EMAConsumer.EMAPayload).EMA.Value,
                    If(signalData.BuyLevel = Decimal.MinValue, 0, signalData.BuyLevel),
                    If(signalData.SellLevel = Decimal.MinValue, 0, signalData.SellLevel),
                    ltp,
                    Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        Dim buyLine As Decimal = 0
        Dim sellLine As Decimal = 0
        Dim signal As SignalLevels = Nothing
        If Now >= petDGandhiUserSettings.TradeStartTime AndAlso Now <= petDGandhiUserSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= petDGandhiUserSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Not IsActiveInstrument() AndAlso
            Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Me.ParentStrategy.GetTotalPL() > Math.Abs(petDGandhiUserSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPL() < Math.Abs(petDGandhiUserSettings.MaxProfitPerDay) Then
            signal = GetBuySellLevel(runningCandlePayload, emaConsumer, pivotHighLowConsumer, False)
            If signal IsNot Nothing Then
                Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                If signal.BuyLevel <> Decimal.MinValue AndAlso signal.SellLevel <> Decimal.MinValue Then
                    buyLine = signal.BuyLevel - Math.Abs(signal.BuyLevel - signal.SellLevel) * 30 / 100
                    sellLine = signal.SellLevel + Math.Abs(signal.BuyLevel - signal.SellLevel) * 30 / 100
                    If ltp > buyLine Then
                        tradeDirection = IOrder.TypeOfTransaction.Buy
                    ElseIf ltp < sellLine Then
                        tradeDirection = IOrder.TypeOfTransaction.Sell
                    End If
                ElseIf signal.BuyLevel <> Decimal.MinValue AndAlso signal.SellLevel = Decimal.MinValue Then
                    tradeDirection = IOrder.TypeOfTransaction.Buy
                ElseIf signal.BuyLevel = Decimal.MinValue AndAlso signal.SellLevel <> Decimal.MinValue Then
                    tradeDirection = IOrder.TypeOfTransaction.Sell
                End If

                Dim quantity As Integer = 1
                If Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                    quantity = petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity
                ElseIf Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures Then
                    quantity = Me.TradableInstrument.LotSize * petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity
                End If
                Dim triggerPrice As Decimal = Decimal.MinValue
                Dim price As Decimal = Decimal.MinValue
                Dim target As Decimal = Decimal.MinValue
                Dim stoploss As Decimal = Decimal.MinValue
                Dim buyEntry As Boolean = True
                Dim sellEntry As Boolean = True

                Dim businessOrder As IBusinessOrder = GetLastExecutedOrder()
                If businessOrder IsNot Nothing AndAlso businessOrder.AllOrder IsNot Nothing AndAlso businessOrder.AllOrder.Count > 0 Then
                    Dim targetReached As Boolean = False
                    For Each runningTargetOrder In businessOrder.AllOrder
                        If runningTargetOrder.Status = IOrder.TypeOfStatus.Complete Then
                            Select Case businessOrder.ParentOrder.TransactionType
                                Case IOrder.TypeOfTransaction.Buy
                                    If runningTargetOrder.AveragePrice >= businessOrder.ParentOrder.AveragePrice Then
                                        targetReached = True
                                        Exit For
                                    End If
                                Case IOrder.TypeOfTransaction.Sell
                                    If runningTargetOrder.AveragePrice <= businessOrder.ParentOrder.AveragePrice Then
                                        targetReached = True
                                        Exit For
                                    End If
                            End Select
                        End If
                    Next

                    If forcePrint Then
                        logger.Debug("Place Order-> Order ID:{0}, Direction:{1}, Target Reached:{2}", businessOrder.ParentOrderIdentifier, businessOrder.ParentOrder.TransactionType.ToString, targetReached)
                    End If

                    If targetReached Then
                        If Not petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).SimilarDirectionTradeAfterTarget Then
                            If businessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                buyEntry = False
                            ElseIf businessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                sellEntry = False
                            End If
                        End If
                    End If
                End If

                If tradeDirection = IOrder.TypeOfTransaction.Buy AndAlso buyEntry Then
                    triggerPrice = signal.BuyLevel + CalculateBuffer(signal.BuyLevel, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    price = signal.BuyLevel + Math.Round(ConvertFloorCeling(triggerPrice * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    target = Math.Round(ConvertFloorCeling((triggerPrice * petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxTargetPercentagePerTrade / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor), 2)
                    stoploss = Math.Round(ConvertFloorCeling((triggerPrice * petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStoplossPercentagePerTrade / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor), 2)
                    If Me.TradableInstrument.LastTick.LastPrice < triggerPrice Then
                        parameters = New PlaceOrderParameters(signal.BuySignalCandle) With
                        {
                            .EntryDirection = IOrder.TypeOfTransaction.Buy,
                            .Price = price,
                            .TriggerPrice = triggerPrice,
                            .Quantity = quantity,
                            .SquareOffValue = target,
                            .StoplossValue = stoploss,
                            .OrderType = IOrder.TypeOfOrder.SL
                        }
                    ElseIf Me.GetTotalExecutedOrders() > 0 Then
                        price = ltp + Math.Round(ConvertFloorCeling(ltp * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                        parameters = New PlaceOrderParameters(signal.BuySignalCandle) With
                        {
                            .EntryDirection = IOrder.TypeOfTransaction.Buy,
                            .Price = price,
                            .Quantity = quantity,
                            .SquareOffValue = target,
                            .StoplossValue = stoploss,
                            .OrderType = IOrder.TypeOfOrder.Limit
                        }
                    End If
                ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell AndAlso sellEntry Then
                    triggerPrice = signal.SellLevel - CalculateBuffer(signal.SellLevel, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    price = signal.SellLevel - Math.Round(ConvertFloorCeling(triggerPrice * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                    target = Math.Round(ConvertFloorCeling((triggerPrice * petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxTargetPercentagePerTrade / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor), 2)
                    stoploss = Math.Round(ConvertFloorCeling((triggerPrice * petDGandhiUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).MaxStoplossPercentagePerTrade / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor), 2)
                    If Me.TradableInstrument.LastTick.LastPrice > triggerPrice Then
                        parameters = New PlaceOrderParameters(signal.SellSignalCandle) With
                        {
                            .EntryDirection = IOrder.TypeOfTransaction.Sell,
                            .Price = price,
                            .TriggerPrice = triggerPrice,
                            .Quantity = quantity,
                            .SquareOffValue = target,
                            .StoplossValue = stoploss,
                            .OrderType = IOrder.TypeOfOrder.SL
                        }
                    ElseIf Me.GetTotalExecutedOrders() > 0 Then
                        price = ltp - Math.Round(ConvertFloorCeling(ltp * 0.3 / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                        parameters = New PlaceOrderParameters(signal.SellSignalCandle) With
                        {
                            .EntryDirection = IOrder.TypeOfTransaction.Sell,
                            .Price = price,
                            .Quantity = quantity,
                            .SquareOffValue = target,
                            .StoplossValue = stoploss,
                            .OrderType = IOrder.TypeOfOrder.Limit
                        }
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        'If parameters IsNot Nothing Then
        '    Try
        '        If forcePrint Then
        '            logger.Debug("Place Order parametres-> Entry Direcrion:{0}, Price:{1}, Quantity:{2}, Target:{3}, Stoploss:{4}, Signal Candle:{5}, Buy Level:{6}, Sell Level:{7}, Buy Line:{8}, Sell Line:{9}, LTP:{10}, Order Type:{11}",
        '                     parameters.EntryDirection.ToString, parameters.Price,
        '                     parameters.Quantity,
        '                     parameters.SquareOffValue,
        '                     parameters.StoplossValue,
        '                     parameters.SignalCandle.SnapshotDateTime.ToString,
        '                     If(signal.BuyLevel = Decimal.MinValue, 0, signal.BuyLevel),
        '                     If(signal.SellLevel = Decimal.MinValue, 0, signal.SellLevel),
        '                     buyLine, sellLine, ltp, parameters.OrderType.ToString)
        '        End If
        '    Catch ex As Exception
        '        logger.Error(ex.ToString)
        '    End Try
        '    Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
        '    If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
        '        For Each currentSignalActivity In currentSignalActivities
        '            Dim businessOrder As IBusinessOrder = Nothing
        '            If Me.OrderDetails IsNot Nothing AndAlso Me.OrderDetails.Count > 0 AndAlso Me.OrderDetails.ContainsKey(currentSignalActivity.ParentOrderID) Then
        '                businessOrder = OrderDetails(currentSignalActivity.ParentOrderID)
        '            End If
        '            If businessOrder IsNot Nothing AndAlso businessOrder.ParentOrder IsNot Nothing Then
        '                If businessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Cancelled Then
        '                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
        '                End If
        '            ElseIf currentSignalActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
        '                    currentSignalActivity.EntryActivity.LastException IsNot Nothing AndAlso
        '                    currentSignalActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
        '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "Condition Satisfied")
        '            ElseIf currentSignalActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
        '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
        '                'ElseIf currentSignalActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
        '                '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
        '            Else
        '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "Condition Satisfied")
        '            End If
        '        Next
        '        Dim handledActivity As IEnumerable(Of ActivityDashboard) =
        '            currentSignalActivities.Where(Function(x)
        '                                              Return x.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
        '                                              x.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated
        '                                          End Function)
        '        If handledActivity IsNot Nothing AndAlso handledActivity.Count > 0 Then
        '            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        '        End If
        '    Else
        '        ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
        '    End If
        'Else
        '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        'End If
        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso
                    parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    For Each slOrder In parentBusinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
                            Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(petDGandhiUserSettings.SignalTimeFrame)
                            Dim emaConsumer As EMAConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyEMAConsumer)
                            Dim pivotHighLowConsumer As PivotHighLowConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyPivotHighLowConsumer)
                            Dim signal As SignalLevels = GetBuySellLevel(runningCandlePayload, emaConsumer, pivotHighLowConsumer, False)
                            If signal IsNot Nothing Then
                                Dim slPrice As Decimal = 0
                                If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy AndAlso signal.SellLevel <> Decimal.MinValue Then
                                    slPrice = signal.SellLevel - CalculateBuffer(signal.SellLevel, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                    If slPrice > slOrder.TriggerPrice Then
                                        triggerPrice = slPrice
                                    End If
                                ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell AndAlso signal.BuyLevel <> Decimal.MinValue Then
                                    slPrice = signal.BuyLevel + CalculateBuffer(signal.BuyLevel, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                    If slPrice < slOrder.TriggerPrice Then
                                        triggerPrice = slPrice
                                    End If
                                End If
                            End If
                            If triggerPrice <> Decimal.MinValue AndAlso slOrder.TriggerPrice <> triggerPrice Then
                                'Below portion have to be done in every modify stoploss order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(slOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                    currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                                        Continue For
                                    End If
                                End If

                                If forcePrint Then
                                    Try
                                        logger.Debug("Stoploss modification: Parent Order ID:{0}, Parent Order Direction:{1}, Trigger Price:{2}",
                                                 parentBusinessOrder.ParentOrder.OrderIdentifier, parentBusinessOrder.ParentOrder.TransactionType.ToString, triggerPrice)
                                    Catch ex As Exception
                                        logger.Error(ex.ToString)
                                    End Try
                                End If

                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, "Opposite direction trade entry price"))
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
                    Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    Dim petDGandhiUserSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
                    Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(petDGandhiUserSettings.SignalTimeFrame)
                    Dim emaConsumer As EMAConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyEMAConsumer)
                    Dim pivotHighLowConsumer As PivotHighLowConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyPivotHighLowConsumer)
                    Dim signal As SignalLevels = GetBuySellLevel(runningCandlePayload, emaConsumer, pivotHighLowConsumer, False)

                    If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick Then
                        If signal IsNot Nothing Then
                            Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                            Dim buyLine As Decimal = 0
                            Dim sellLine As Decimal = 0
                            Dim ltp As Decimal = Me.TradableInstrument.LastTick.LastPrice
                            If signal.BuyLevel <> Decimal.MinValue AndAlso signal.SellLevel <> Decimal.MinValue Then
                                buyLine = signal.BuyLevel - Math.Abs(signal.BuyLevel - signal.SellLevel) * 30 / 100
                                sellLine = signal.SellLevel + Math.Abs(signal.BuyLevel - signal.SellLevel) * 30 / 100
                                If ltp > buyLine Then
                                    tradeDirection = IOrder.TypeOfTransaction.Buy
                                ElseIf ltp < sellLine Then
                                    tradeDirection = IOrder.TypeOfTransaction.Sell
                                End If
                            ElseIf signal.BuyLevel <> Decimal.MinValue AndAlso signal.SellLevel = Decimal.MinValue Then
                                tradeDirection = IOrder.TypeOfTransaction.Buy
                            ElseIf signal.BuyLevel = Decimal.MinValue AndAlso signal.SellLevel <> Decimal.MinValue Then
                                tradeDirection = IOrder.TypeOfTransaction.Sell
                            End If

                            Dim orderNeedsToBeCancelled As Boolean = False
                            Dim reason As String = Nothing
                            If tradeDirection <> IOrder.TypeOfTransaction.None AndAlso tradeDirection <> parentBusinessOrder.ParentOrder.TransactionType Then
                                'Below portion have to be done in every cancel order trigger
                                If parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.TriggerPending Then
                                    orderNeedsToBeCancelled = True
                                    reason = "LTP is near to opposite direction"
                                End If
                            ElseIf tradeDirection <> IOrder.TypeOfTransaction.None AndAlso tradeDirection = parentBusinessOrder.ParentOrder.TransactionType Then
                                If parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.TriggerPending Then
                                    Dim orderSignalCandle As OHLCPayload = GetSignalCandleOfAnOrder(parentBusinessOrder.ParentOrderIdentifier, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                                    If orderSignalCandle IsNot Nothing Then
                                        If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                                            If signal.BuySignalCandle IsNot Nothing AndAlso
                                                signal.BuySignalCandle.SnapshotDateTime <> orderSignalCandle.SnapshotDateTime Then
                                                orderNeedsToBeCancelled = True
                                                reason = "New favourable Buy Signal received"
                                            End If
                                        ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                                            If signal.SellSignalCandle IsNot Nothing AndAlso
                                                signal.SellSignalCandle.SnapshotDateTime <> orderSignalCandle.SnapshotDateTime Then
                                                orderNeedsToBeCancelled = True
                                                reason = "New favourable Sell Signal received"
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                            If orderNeedsToBeCancelled Then
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                    currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                    currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        Continue For
                                    End If
                                End If

                                If forcePrint Then
                                    Try
                                        logger.Debug("Exit Order: Parent Order ID:{0}, Parent Order Direction:{1}, Buy Line:{2}, Sell Line:{3}, LTP:{4}, Reason:{5}",
                                                    parentBusinessOrder.ParentOrder.OrderIdentifier,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString, buyLine, sellLine,
                                                    ltp, reason)
                                    Catch ex As Exception
                                        logger.Error(ex.ToString)
                                    End Try
                                End If

                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBusinessOrder.ParentOrder, reason))
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
            Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
            'If exitOrderResponse IsNot Nothing Then
            '    GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, reason))
            'End If
        End If
    End Function

    Public Overrides Function ProcessOrderAsync(orderData As IBusinessOrder) As Task
        If orderData.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
            Dim signalCandleTime As Date = Me.ParentStrategy.SignalManager.GetSignalActivities(orderData.ParentOrder.Tag).SignalGeneratedTime
            Try
                If orderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy AndAlso _signalLevel.BuySignalCandle IsNot Nothing AndAlso
                    _signalLevel.BuySignalCandle.SnapshotDateTime = signalCandleTime Then
                    logger.Debug("Removing Buy Signal as it is used. Buy Price:{0}, BuySignalCandle:{1}", _signalLevel.BuyLevel, _signalLevel.BuySignalCandle.SnapshotDateTime)
                    _usedSignalLevel.BuyLevel = _signalLevel.BuyLevel
                    _usedSignalLevel.BuySignalCandle = _signalLevel.BuySignalCandle
                    _signalLevel.BuyLevel = Decimal.MinValue
                    _signalLevel.BuySignalCandle = Nothing
                ElseIf orderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell AndAlso _signalLevel.SellSignalCandle IsNot Nothing AndAlso
                    _signalLevel.SellSignalCandle.SnapshotDateTime = signalCandleTime Then
                    logger.Debug("Removing Sell Signal as it is used. Sell Price:{0}, SellSignalCandle:{1}", _signalLevel.SellLevel, _signalLevel.SellSignalCandle.SnapshotDateTime)
                    _usedSignalLevel.SellLevel = _signalLevel.SellLevel
                    _usedSignalLevel.SellSignalCandle = _signalLevel.SellSignalCandle
                    _signalLevel.SellLevel = Decimal.MinValue
                    _signalLevel.SellSignalCandle = Nothing
                End If
            Catch ex As Exception
                logger.Debug(ex.ToString)
                Throw ex
            End Try
        End If
        Return MyBase.ProcessOrderAsync(orderData)
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim hedgingUserInputs As NearFarHedgingUserInputs = Me.ParentStrategy.UserSettings
        If hedgingUserInputs.TelegramAPIKey IsNot Nothing AndAlso Not hedgingUserInputs.TelegramAPIKey.Trim = "" AndAlso
            hedgingUserInputs.TelegramChatID IsNot Nothing AndAlso Not hedgingUserInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(hedgingUserInputs.TelegramAPIKey.Trim, hedgingUserInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                'logger.Debug("Encoded String:{0}", encodedString)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
    End Function

    Private Function GetBuySellLevel(ByVal currentPayload As OHLCPayload,
                                     ByVal emaConsumer As EMAConsumer,
                                     ByVal pivotHighLowConsumer As PivotHighLowConsumer,
                                     ByVal forcePrint As Boolean) As SignalLevels
        If currentPayload IsNot Nothing AndAlso currentPayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
        currentPayload.PreviousPayload IsNot Nothing AndAlso currentPayload.PreviousPayload.PreviousPayload IsNot Nothing AndAlso
        currentPayload.PreviousPayload.PreviousPayload.PreviousPayload IsNot Nothing Then
            If emaConsumer IsNot Nothing AndAlso pivotHighLowConsumer IsNot Nothing Then
                If emaConsumer.ConsumerPayloads IsNot Nothing AndAlso emaConsumer.ConsumerPayloads.Count > 0 AndAlso
               pivotHighLowConsumer.ConsumerPayloads IsNot Nothing AndAlso pivotHighLowConsumer.ConsumerPayloads.Count > 0 Then
                    If pivotHighLowConsumer.ConsumerPayloads.ContainsKey(currentPayload.PreviousPayload.SnapshotDateTime) AndAlso
                    pivotHighLowConsumer.ConsumerPayloads.ContainsKey(currentPayload.PreviousPayload.PreviousPayload.SnapshotDateTime) Then
                        Dim lastPivotHighLow As PivotHighLowConsumer.PivotHighLowPayload = pivotHighLowConsumer.ConsumerPayloads(currentPayload.PreviousPayload.SnapshotDateTime)
                        Dim lastPivotHighEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotHighSignalCandle.SnapshotDateTime)
                        Dim prelastPivotHighEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotHighSignalCandle.PreviousPayload.SnapshotDateTime)
                        Dim prePrelastPivotHighEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotHighSignalCandle.PreviousPayload.PreviousPayload.SnapshotDateTime)
                        If lastPivotHighLow.PivotHighSignalCandle.HighPrice.Value > lastPivotHighEMA.EMA.Value OrElse
                            lastPivotHighLow.PivotHighSignalCandle.PreviousPayload.HighPrice.Value > prelastPivotHighEMA.EMA.Value OrElse
                            lastPivotHighLow.PivotHighSignalCandle.PreviousPayload.PreviousPayload.HighPrice.Value > prePrelastPivotHighEMA.EMA.Value Then
                            If _signalLevel.BuyLevel <> lastPivotHighLow.PivotHigh.Value OrElse
                                _signalLevel.BuySignalCandle.SnapshotDateTime <> lastPivotHighLow.PivotHighSignalCandle.SnapshotDateTime Then
                                If _usedSignalLevel IsNot Nothing AndAlso
                                    (_usedSignalLevel.BuyLevel <> lastPivotHighLow.PivotHigh.Value OrElse
                                    _usedSignalLevel.BuySignalCandle.SnapshotDateTime <> lastPivotHighLow.PivotHighSignalCandle.SnapshotDateTime) Then
                                    _signalLevel.BuyLevel = lastPivotHighLow.PivotHigh.Value
                                    _signalLevel.BuySignalCandle = lastPivotHighLow.PivotHighSignalCandle
                                    logger.Debug("*** New Buy signal received. Buy Level:{0}, SignalCandleTime:{1}", _signalLevel.BuyLevel, _signalLevel.BuySignalCandle.SnapshotDateTime)
                                ElseIf _usedSignalLevel Is Nothing OrElse _usedSignalLevel.BuyLevel = Decimal.MinValue Then
                                    _signalLevel.BuyLevel = lastPivotHighLow.PivotHigh.Value
                                    _signalLevel.BuySignalCandle = lastPivotHighLow.PivotHighSignalCandle
                                    logger.Debug("*** New Buy signal received. Buy Level:{0}, SignalCandleTime:{1}", _signalLevel.BuyLevel, _signalLevel.BuySignalCandle.SnapshotDateTime)
                                End If
                            End If
                        End If

                        Dim lastPivotLowEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotLowSignalCandle.SnapshotDateTime)
                        Dim prelastPivotLowEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotLowSignalCandle.PreviousPayload.SnapshotDateTime)
                        Dim prePrelastPivotLowEMA As EMAConsumer.EMAPayload = emaConsumer.ConsumerPayloads(lastPivotHighLow.PivotLowSignalCandle.PreviousPayload.PreviousPayload.SnapshotDateTime)
                        If lastPivotHighLow.PivotLowSignalCandle.LowPrice.Value < lastPivotLowEMA.EMA.Value OrElse
                            lastPivotHighLow.PivotLowSignalCandle.PreviousPayload.LowPrice.Value < prelastPivotLowEMA.EMA.Value OrElse
                            lastPivotHighLow.PivotLowSignalCandle.PreviousPayload.PreviousPayload.LowPrice.Value < prePrelastPivotLowEMA.EMA.Value Then
                            If _signalLevel.SellLevel <> lastPivotHighLow.PivotLow.Value OrElse
                                _signalLevel.SellSignalCandle.SnapshotDateTime <> lastPivotHighLow.PivotLowSignalCandle.SnapshotDateTime Then
                                If _usedSignalLevel IsNot Nothing AndAlso
                                    (_usedSignalLevel.SellLevel <> lastPivotHighLow.PivotLow.Value OrElse
                                    _usedSignalLevel.SellSignalCandle.SnapshotDateTime <> lastPivotHighLow.PivotLowSignalCandle.SnapshotDateTime) Then
                                    _signalLevel.SellLevel = lastPivotHighLow.PivotLow.Value
                                    _signalLevel.SellSignalCandle = lastPivotHighLow.PivotLowSignalCandle
                                    logger.Debug("*** New Sell signal received. Sell Level:{0}, SignalCandleTime:{1}", _signalLevel.SellLevel, _signalLevel.SellSignalCandle.SnapshotDateTime)
                                ElseIf _usedSignalLevel Is Nothing OrElse _usedSignalLevel.SellLevel = Decimal.MinValue Then
                                    _signalLevel.SellLevel = lastPivotHighLow.PivotLow.Value
                                    _signalLevel.SellSignalCandle = lastPivotHighLow.PivotLowSignalCandle
                                    logger.Debug("*** New Sell signal received. Sell Level:{0}, SignalCandleTime:{1}", _signalLevel.SellLevel, _signalLevel.SellSignalCandle.SnapshotDateTime)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If
        If forcePrint Then
            logger.Debug("Buy Level:{0}, BuySignalCandle:{1}, Sell Level:{2}, SellSignalCandle:{3}",
                     If(_signalLevel.BuyLevel = Decimal.MinValue, 0, _signalLevel.BuyLevel),
                     If(_signalLevel.BuySignalCandle Is Nothing, "Nothing", _signalLevel.BuySignalCandle.SnapshotDateTime),
                     If(_signalLevel.SellLevel = Decimal.MinValue, 0, _signalLevel.SellLevel),
                     If(_signalLevel.SellSignalCandle Is Nothing, "Nothing", _signalLevel.SellSignalCandle.SnapshotDateTime))
        End If
        Return _signalLevel
    End Function

    Private Class SignalLevels
        Public BuyLevel As Decimal = Decimal.MinValue
        Public BuySignalCandle As OHLCPayload = Nothing
        Public SellLevel As Decimal = Decimal.MinValue
        Public SellSignalCandle As OHLCPayload = Nothing
    End Class
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
