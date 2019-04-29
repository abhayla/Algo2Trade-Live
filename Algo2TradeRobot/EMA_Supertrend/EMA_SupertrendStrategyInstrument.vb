Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class EMA_SupertrendStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private lastPrevPayloadPlaceOrder As String = ""
    Private lastPrevPayloadModifyOrder As String = ""
    Private lastPrevPayloadExitOrder As String = ""
    Private ReadOnly _apiKey As String = "815345137:AAH_34NObix7jbARfZZII5yu-bhFpGXdUFE"
    Private ReadOnly _chaitId As String = "-322631613"
    Private ReadOnly _dummyFastEMAConsumer As EMAConsumer
    Private ReadOnly _dummySlowEMAConsumer As EMAConsumer
    Private ReadOnly _dummySupertrendConsumer As SupertrendConsumer
    Private ReadOnly _useST As Boolean = True
    Private _sendParentOrderDetailsOfOrderId As String = Nothing
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
                {New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).FastEMAPeriod, TypeOfField.Close),
                New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SlowEMAPeriod, TypeOfField.Close),
                New SupertrendConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SupertrendPeriod, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SupertrendMultiplier)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyFastEMAConsumer = New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).FastEMAPeriod, TypeOfField.Close)
                _dummySlowEMAConsumer = New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SlowEMAPeriod, TypeOfField.Close)
                _dummySupertrendConsumer = New SupertrendConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SupertrendPeriod, CType(Me.ParentStrategy.UserSettings, EMA_SupertrendStrategyUserInputs).SupertrendMultiplier)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If

                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceCOMarketMISOrder, Nothing).ConfigureAwait(False)
                    If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                        placeOrderResponse("data").ContainsKey("order_id") Then
                        _sendParentOrderDetailsOfOrderId = placeOrderResponse("data")("order_id")
                        GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Placed, Direction: {2}, Quantity: {3}, Proposed SL Price: {4}", Now, Me.TradableInstrument.TradingSymbol, placeOrderTrigger.Item2.EntryDirection.ToString, placeOrderTrigger.Item2.Quantity, placeOrderTrigger.Item2.TriggerPrice))
                    End If
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim modifyStoplossOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrdersTrigger IsNot Nothing AndAlso modifyStoplossOrdersTrigger.Count > 0 Then
                    Dim modifyOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    If modifyOrderResponse IsNot Nothing Then
                        _sendParentOrderDetailsOfOrderId = Nothing
                        Dim modifyStoplossOrderTrigger As Tuple(Of ExecuteCommandAction, IOrder, Decimal, String) = modifyStoplossOrdersTrigger.LastOrDefault
                        Dim parentBusinessOrder As IBusinessOrder = GetParentFromChildOrder(modifyStoplossOrderTrigger.Item2)
                        If parentBusinessOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder IsNot Nothing Then
                            If modifyStoplossOrderTrigger.Item4 = "Normal SL movement according to SL%" Then
                                GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Executed, Direction: {2}, Quantity: {3}, Actual Entry Price:{4}, Actual SL Price: {5}", Now, Me.TradableInstrument.TradingSymbol, parentBusinessOrder.ParentOrder.TransactionType.ToString, parentBusinessOrder.ParentOrder.Quantity, parentBusinessOrder.ParentOrder.AveragePrice, modifyStoplossOrderTrigger.Item3))
                            Else
                                GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Modified, Direction: {2}, Quantity: {3}, Actual Entry Price:{4}, Actual SL Price: {5}, Remarks:{6}", Now, Me.TradableInstrument.TradingSymbol, parentBusinessOrder.ParentOrder.TransactionType.ToString, parentBusinessOrder.ParentOrder.Quantity, parentBusinessOrder.ParentOrder.AveragePrice, modifyStoplossOrderTrigger.Item3, modifyStoplossOrderTrigger.Item4))
                            End If
                        End If
                    End If
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim exitOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrdersTrigger IsNot Nothing AndAlso exitOrdersTrigger.Count > 0 Then
                    Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.CancelCOOrder, Nothing).ConfigureAwait(False)
                    If exitOrderResponse IsNot Nothing Then
                        GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, exitOrdersTrigger.LastOrDefault.Item3))
                    End If
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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim emaStUserSettings As EMA_SupertrendStrategyUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(emaStUserSettings.SignalTimeFrame)
        Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(Me.TradableInstrument.ExchangeDetails.ExchangeType)
        Dim supertrendConsumer As SupertrendConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySupertrendConsumer)

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadPlaceOrder Then
                    lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, LastTradeEntryTime:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsActiveInstrument:{4}, IsHistoricalCompleted:{5}, Capital at Day Start:{21}, MTM Loss: {6}, MTM Profit: {7}, TotalStrategyPL:{8}, IsAboveOrBelow(above):{9}, IsAboveOrBelow(below):{10}, SupertrendColor:{11}, Supertrend:{12}, Quantity:{13}, Stoploss%:{14}, IT%:{15}, T%:{16}, LVT%:{17}, LVStartTime:{18}, LVEndYime:{19}, Is Transition Achieved:{22}, TradingSymbol:{20}",
                    emaStUserSettings.TradeStartTime,
                    emaStUserSettings.LastTradeEntryTime,
                    runningCandlePayload.SnapshotDateTime.ToString,
                    runningCandlePayload.PayloadGeneratedBy.ToString,
                    IsActiveInstrument(),
                    Me.TradableInstrument.IsHistoricalCompleted,
                    capitalAtDayStart * Math.Abs(emaStUserSettings.MaxLossPercentagePerDay) * -1 / 100,
                    capitalAtDayStart * Math.Abs(emaStUserSettings.MaxProfitPercentagePerDay) / 100,
                    Me.ParentStrategy.GetTotalPL,
                    IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Above, True),
                    IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Below, True),
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor.ToString,
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value,
                    Me.TradableInstrument.LotSize * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime.ToString,
                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime.ToString,
                    Me.TradableInstrument.TradingSymbol,
                    capitalAtDayStart,
                    IsTransitionAchieved(runningCandlePayload, True))
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim marketPrice As Decimal = Me.TradableInstrument.LastTick.LastPrice
        Dim parameters As PlaceOrderParameters = Nothing
        If Now >= emaStUserSettings.TradeStartTime AndAlso Now <= emaStUserSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= emaStUserSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Not IsActiveInstrument() AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(emaStUserSettings.MaxLossPercentagePerDay) * -1 / 100 AndAlso
            Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(emaStUserSettings.MaxProfitPercentagePerDay) / 100 AndAlso
            IsTransitionAchieved(runningCandlePayload, False) Then

            Dim triggerPrice As Decimal = Nothing
            Dim quantity As Integer = Me.TradableInstrument.LotSize * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity

            If IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Above, False) AndAlso
                supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) AndAlso
                (Not _useST Or CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Green) Then

                triggerPrice = marketPrice - ConvertFloorCeling((marketPrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)

                parameters = New PlaceOrderParameters(runningCandlePayload) With
                                   {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                   .Quantity = quantity,
                                   .TriggerPrice = triggerPrice}
            ElseIf IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Below, False) AndAlso
                supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) AndAlso
                (Not _useST Or CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Red) Then

                triggerPrice = marketPrice + ConvertFloorCeling((marketPrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)

                parameters = New PlaceOrderParameters(runningCandlePayload) With
                                   {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                   .Quantity = quantity,
                                   .TriggerPrice = triggerPrice}
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then

            Try
                If forcePrint Then
                    logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, LastTradeEntryTime:{1}, RunningCandlePayloadSnapshotDateTime:{2},
                                PayloadGeneratedBy:{3}, IsActiveInstrument:{4}, IsHistoricalCompleted:{5}, MTM Loss: {6}, MTM Profit: {7}, 
                                TotalStrategyPL:{8}, IsAboveOrBelow(above):{9}, IsAboveOrBelow(below):{10}, SupertrendColor:{11}, Quantity:{12},
                                Stoploss%(SL):{13}, IT%:{14}, T%:{15}, LVT%:{16}, LVStartTime:{17}, LVEndYime:{18}, 
                                MarketPrice(MP):{19}, TriggerPrice[MP{20}MP*SL/100]:{21}, IsTransitionAchieved:{22}, TradingSymbol:{23}",
                                    emaStUserSettings.TradeStartTime,
                                    emaStUserSettings.LastTradeEntryTime,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    IsActiveInstrument(),
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    capitalAtDayStart * Math.Abs(emaStUserSettings.MaxLossPercentagePerDay) * -1 / 100,
                                    capitalAtDayStart * Math.Abs(emaStUserSettings.MaxProfitPercentagePerDay) / 100,
                                    Me.ParentStrategy.GetTotalPL,
                                    IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Above, True),
                                    IsAboveOrBelow(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Position.Below, True),
                                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor.ToString,
                                    Me.TradableInstrument.LotSize * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime.ToString,
                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime.ToString,
                                    marketPrice,
                                    If(parameters.EntryDirection = IOrder.TypeOfTransaction.Buy, "-", "+"),
                                    parameters.TriggerPrice,
                                    IsTransitionAchieved(runningCandlePayload, True),
                                    Me.TradableInstrument.TradingSymbol)
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

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim emaStUserSettings As EMA_SupertrendStrategyUserInputs = Me.ParentStrategy.UserSettings

        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso
                    parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    Dim runningCandlePayload As OHLCPayload = Nothing
                    Dim checkIT As Boolean = False
                    If emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage <> Decimal.MinValue Then
                        runningCandlePayload = GetXMinuteCurrentCandle(emaStUserSettings.SignalTimeFrame)
                        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                            checkIT = True
                        End If
                    End If

                    Dim intermediateTargetReached As Boolean = False
                    Dim triggerPrice As Decimal = Decimal.MinValue
                    If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        triggerPrice = parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                        If checkIT Then
                            Dim it As Decimal = parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            intermediateTargetReached = runningCandlePayload.PreviousPayload.HighPrice.Value >= it

                            Try
                                If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                                    If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadModifyOrder Then
                                        lastPrevPayloadModifyOrder = runningCandlePayload.PreviousPayload.ToString
                                        logger.Debug("ModifyOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                                        logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, IT%:{1}, OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, IntermediateTarget(AP+AP*IT/100):{5}, PreviousCandleHigh:{6}, IntermediateTargetReached:{7}, TradingSymbol:{8}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    it,
                                                    runningCandlePayload.PreviousPayload.HighPrice.Value,
                                                    intermediateTargetReached,
                                                    Me.TradableInstrument.TradingSymbol)
                                    End If
                                End If
                            Catch ex As Exception
                                logger.Error(ex)
                            End Try

                        End If
                    ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        triggerPrice = parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                        If checkIT Then
                            Dim it As Decimal = parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            intermediateTargetReached = runningCandlePayload.PreviousPayload.LowPrice.Value <= it

                            Try
                                If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                                    If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadModifyOrder Then
                                        lastPrevPayloadModifyOrder = runningCandlePayload.PreviousPayload.ToString
                                        logger.Debug("ModifyOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                                        logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, IT%:{1}, OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, IntermediateTarget(AP-AP*IT/100):{5}, PreviousCandleLow:{6}, IntermediateTargetReached:{7}, TradingSymbol:{8}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    it,
                                                    runningCandlePayload.PreviousPayload.LowPrice.Value,
                                                    intermediateTargetReached,
                                                    Me.TradableInstrument.TradingSymbol)
                                    End If
                                End If
                            Catch ex As Exception
                                logger.Error(ex)
                            End Try

                        End If
                    End If

                    For Each slOrder In parentBusinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            If _sendParentOrderDetailsOfOrderId IsNot Nothing AndAlso _sendParentOrderDetailsOfOrderId = parentOrderId AndAlso
                                slOrder.TriggerPrice = triggerPrice Then
                                _sendParentOrderDetailsOfOrderId = Nothing
                                GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Executed, Direction: {2}, Quantity: {3}, Actual Entry Price:{4}, Actual SL Price: {5}", Now, Me.TradableInstrument.TradingSymbol, parentBusinessOrder.ParentOrder.TransactionType.ToString, parentBusinessOrder.ParentOrder.Quantity, parentBusinessOrder.ParentOrder.AveragePrice, slOrder.TriggerPrice))
                            End If
                            If intermediateTargetReached Then
                                logger.Debug("IT reached")
                                triggerPrice = runningCandlePayload.OpenPrice.Value
                            End If
                            If slOrder.TriggerPrice <> triggerPrice Then
                                If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    Try
                                        If forcePrint AndAlso intermediateTargetReached Then
                                            logger.Debug("ModifyOrder-> ****************************************************************************** {0}", Me.TradableInstrument.TradingSymbol)
                                            logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, IT%:{1}, 
                                                    OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, IntermediateTarget(AP+AP*IT/100):{5}, 
                                                    PreviousCandleHigh:{6}, IntermediateTargetReached:{7}, TriggerPrice:{8}, Reason: SL modify for intermediate target reached, TradingSymbol:{9}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor),
                                                    runningCandlePayload.PreviousPayload.HighPrice.Value,
                                                    intermediateTargetReached,
                                                    triggerPrice,
                                                    Me.TradableInstrument.TradingSymbol)
                                        End If
                                    Catch ex As Exception
                                        logger.Error(ex)
                                    End Try
                                ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    Try
                                        If forcePrint AndAlso intermediateTargetReached Then
                                            logger.Debug("ModifyOrder-> ****************************************************************************** {0}", Me.TradableInstrument.TradingSymbol)
                                            logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, IT%:{1}, 
                                                    OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, IntermediateTarget(AP+AP*IT/100):{5}, 
                                                    PreviousCandleHigh:{6}, IntermediateTargetReached:{7}, TriggerPrice:{8}, Reason: SL modify for intermediate target reached TradingSymbol:{9}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).IntemediateTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor),
                                                    runningCandlePayload.PreviousPayload.LowPrice.Value,
                                                    intermediateTargetReached,
                                                    triggerPrice,
                                                    Me.TradableInstrument.TradingSymbol)
                                        End If
                                    Catch ex As Exception
                                        logger.Error(ex)
                                    End Try
                                End If

                                'Below portion have to be done in every modify stoploss order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(slOrder.Tag)
                                If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.StoplossModifyActivity.Supporting = triggerPrice Then
                                    If currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        Continue For
                                    End If
                                End If

                                If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    Try
                                        If forcePrint AndAlso Not intermediateTargetReached Then
                                            logger.Debug("ModifyOrder-> ****************************************************************************** {0}", Me.TradableInstrument.TradingSymbol)
                                            logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, SL%:{1}, 
                                                    OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, Stoploss(AP-AP*SL/100):{5}, 
                                                    TriggerPrice:{6}, Reason: Normal SL modify, TradingSymbol:{7}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor),
                                                    triggerPrice,
                                                    Me.TradableInstrument.TradingSymbol)
                                        End If
                                    Catch ex As Exception
                                        logger.Error(ex)
                                    End Try
                                ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    Try
                                        If forcePrint AndAlso Not intermediateTargetReached Then
                                            logger.Debug("ModifyOrder-> ****************************************************************************** {0}", Me.TradableInstrument.TradingSymbol)
                                            logger.Debug("ModifyOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, SL%:{1}, 
                                                    OrderID:{2}, OrderDirection:{3}, AveragePrice(AP):{4}, Stoploss(AP+AP*SL/100):{5}, 
                                                    TriggerPrice:{6}, Reason: Normal SL modify, TradingSymbol:{7}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage,
                                                    parentOrderId,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    parentBusinessOrder.ParentOrder.AveragePrice,
                                                    parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).StoplossPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor),
                                                    triggerPrice,
                                                    Me.TradableInstrument.TradingSymbol)
                                        End If
                                    Catch ex As Exception
                                        logger.Error(ex)
                                    End Try
                                End If

                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, If(intermediateTargetReached, "SL movement due to Intermediate Target reached", "Normal SL movement according to SL%")))
                            End If
                        End If
                    Next
                End If
            Next
        End If
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function
    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Dim emaStUserSettings As EMA_SupertrendStrategyUserInputs = Me.ParentStrategy.UserSettings
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                              Return x.ParentOrderIdentifier Is Nothing
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                For Each parentOrder In parentOrders
                    Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                        For Each slOrder In parentBusinessOrder.SLOrder
                            Dim tradeWillExit As Boolean = False
                            Dim emaCrossOver As Boolean = False
                            Dim beyondST As Boolean = False
                            Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                            Dim supertrendConsumer As SupertrendConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySupertrendConsumer)
                            If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                                Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                                Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then

                                Try
                                    If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                                        If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadExitOrder Then
                                            lastPrevPayloadExitOrder = runningCandlePayload.PreviousPayload.ToString
                                            logger.Debug("ExitOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                                            logger.Debug("ExitOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, OrderID:{1}, OrderDirection:{2}, IsCrossover(above):{3}, IsCrossover(below):{4}, Supertrend:{5}, PreviousCandleClose:{6}, TradingSymbol:{7}",
                                                        runningCandlePayload.SnapshotDateTime.ToString,
                                                        slOrder.ParentOrderIdentifier,
                                                        parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                        IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Above, True),
                                                        IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Below, True),
                                                        CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value,
                                                        runningCandlePayload.PreviousPayload.ClosePrice.Value,
                                                        Me.TradableInstrument.TradingSymbol)
                                        End If
                                    End If
                                Catch ex As Exception
                                    logger.Error(ex)
                                End Try

                                If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                                    If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        'Exit for T % reach or LT% reach
                                        Dim target As Decimal = parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                        Dim lt As Boolean = False
                                        If emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime <> Now.Date AndAlso
                                            emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime <> Now.Date AndAlso
                                            emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage <> Decimal.MinValue Then
                                            If Now >= emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime AndAlso
                                                Now <= emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime Then
                                                lt = True
                                                target = parentBusinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                            End If
                                        End If
                                        If Me.TradableInstrument.LastTick.LastPrice >= target Then
                                            Try
                                                logger.Debug("ExitOrder-> ********************************************************************")
                                                logger.Debug("ExitOrder-> {0} reached. {1}%:{2}, OrderId:{3}, OrderDirection:{4}, 
                                                             AveragePrice(AP):{5}, LastPrice:{6}, TargetPrice(AP+AP*{7}/100):{8}, TradingSymbol:{9}",
                                                             If(lt, "LT%", "T%"),
                                                             If(lt, "LT%", "T%"),
                                                             If(lt, emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage, emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage),
                                                             slOrder.ParentOrderIdentifier,
                                                             parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                             parentBusinessOrder.ParentOrder.AveragePrice,
                                                             Me.TradableInstrument.LastTick.LastPrice,
                                                             If(lt, "LT", "T"),
                                                             target,
                                                             Me.TradableInstrument.TradingSymbol)
                                            Catch ex As Exception
                                                logger.Error(ex)
                                            End Try
                                            Await ForceExitSpecificTradeAsync(slOrder, If(lt, "Low volatility target% reached", "Target% reached")).ConfigureAwait(False)
                                        End If
                                        'Exit for Opposite direction EMA crossover
                                        If IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Below, False) Then
                                            logger.Debug("Ema crossover below")
                                            emaCrossOver = True
                                            tradeWillExit = True
                                        End If
                                        'Exit for Candle close beyond Supertrend
                                        If _useST AndAlso supertrendConsumer IsNot Nothing AndAlso supertrendConsumer.ConsumerPayloads.Count > 0 AndAlso
                                            supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) AndAlso
                                            runningCandlePayload.PreviousPayload.ClosePrice.Value < CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value Then
                                            logger.Debug("Beyond ST")
                                            beyondST = True
                                            tradeWillExit = True
                                        End If
                                    ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        'Exit for T % reach or LT% reach
                                        Dim target As Decimal = parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                        Dim lt As Boolean = False
                                        If emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime <> Now.Date AndAlso
                                            emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime <> Now.Date AndAlso
                                            emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage <> Decimal.MinValue Then
                                            If Now >= emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityStartTime AndAlso
                                                Now <= emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityExitTime Then
                                                lt = True
                                                target = parentBusinessOrder.ParentOrder.AveragePrice - ConvertFloorCeling((parentBusinessOrder.ParentOrder.AveragePrice * emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage / 100), Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                            End If
                                        End If
                                        If Me.TradableInstrument.LastTick.LastPrice <= target Then
                                            logger.Debug("ExitOrder-> ********************************************************************")
                                            logger.Debug("ExitOrder-> {0} reached. {1}%:{2}, OrderId:{3}, OrderDirection:{4}, 
                                                          AveragePrice(AP):{5}, LastPrice:{6}, TargetPrice(AP-AP*{7}/100):{8}, TradingSymbol:{9}",
                                                             If(lt, "LT%", "T%"),
                                                             If(lt, "LT%", "T%"),
                                                             If(lt, emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowVolatilityTargetPercentage, emaStUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).TargetPercentage),
                                                             slOrder.ParentOrderIdentifier,
                                                             parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                             parentBusinessOrder.ParentOrder.AveragePrice,
                                                             Me.TradableInstrument.LastTick.LastPrice,
                                                             If(lt, "LT", "T"),
                                                             target,
                                                             Me.TradableInstrument.TradingSymbol)
                                            Await ForceExitSpecificTradeAsync(slOrder, If(lt, "Low volatility target% reached", "Target% reached")).ConfigureAwait(False)
                                        End If
                                        'Exit for Opposite direction EMA crossover
                                        If IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Above, False) Then
                                            logger.Debug("Ema crossover above")
                                            emaCrossOver = True
                                            tradeWillExit = True
                                        End If
                                        'Exit for Candle close beyond Supertrend
                                        If _useST AndAlso supertrendConsumer IsNot Nothing AndAlso supertrendConsumer.ConsumerPayloads.Count > 0 AndAlso
                                            supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) AndAlso
                                            runningCandlePayload.PreviousPayload.ClosePrice.Value > CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value Then
                                            logger.Debug("Beyond ST")
                                            beyondST = True
                                            tradeWillExit = True
                                        End If
                                    End If
                                End If
                            End If
                            If tradeWillExit Then
                                'Below portion have to be done in every cancel order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                        currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        Continue For
                                    End If
                                End If

                                Dim exitReason As String = ""
                                If emaCrossOver Then
                                    exitReason = "Opposite direction EMA cross over"
                                ElseIf beyondST Then
                                    exitReason = "Candle closed beyond Supertrend"
                                End If

                                Try
                                    If forcePrint Then
                                        logger.Debug("ExitOrder-> **********************************************************")
                                        logger.Debug("ExitOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, OrderID:{1}, OrderDirection:{2}, 
                                                IsCrossover(above):{3}, IsCrossover(below):{4}, Supertrend:{5}, PreviousCandleClose:{6}, ExitReason:{7}, TradingSymbol:{8}",
                                                    runningCandlePayload.SnapshotDateTime.ToString,
                                                    slOrder.ParentOrderIdentifier,
                                                    parentBusinessOrder.ParentOrder.TransactionType.ToString,
                                                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Above, True),
                                                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Below, True),
                                                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value,
                                                    runningCandlePayload.PreviousPayload.ClosePrice.Value,
                                                    exitReason,
                                                    Me.TradableInstrument.TradingSymbol)
                                    End If
                                Catch ex As Exception
                                    logger.Error(ex)
                                End Try

                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, slOrder, exitReason))
                            End If
                        Next
                    End If
                Next
            End If
        End If
        Return ret
    End Function

    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, ByVal reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete AndAlso
            Not order.Status = IOrder.TypeOfStatus.Cancelled AndAlso
            Not order.Status = IOrder.TypeOfStatus.Rejected Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }
            Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelCOOrder, cancellableOrder).ConfigureAwait(False)
            If exitOrderResponse IsNot Nothing Then
                GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, reason))
            End If
        End If
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        Using tSender As New Utilities.Notification.Telegram(_apiKey, _chaitId, _cts)
            Await tSender.SendMessageGetAsync(Utilities.Strings.EncodeString(message)).ConfigureAwait(False)
        End Using
    End Function

    Private Function IsTransitionAchieved(ByVal runningCandlePayload As OHLCPayload, ByVal printDetails As Boolean) As Boolean
        Dim ret As Boolean = False
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload.PreviousPayload IsNot Nothing Then
            Dim supertrendConsumer As SupertrendConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySupertrendConsumer)
            If supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.SnapshotDateTime) AndAlso
            supertrendConsumer.ConsumerPayloads.ContainsKey(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime) Then
                If (CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Green AndAlso
               CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Red) OrElse
               (CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Red AndAlso
                CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor = Color.Green) Then
                    ret = True
                End If
            End If
            If Not ret And IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Above, False) OrElse
           IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Below, False) Then
                ret = True
            End If
            If printDetails Then
                Try
                    logger.Debug("IsTransitionAchieved-> RunningCandlePayloadSnapshotDateTime:{0}, PreviousCandlePayloadSnapshotDateTime:{1}, PreviousPreviousCandlePayloadSnapshotDateTime:{2}, IsCrossover(above):{3}, IsCrossover(below):{4}, PreviousSupertrendColor:{5}, PreviousSupertrend:{6}, SupertrendColor:{7}, Supertrend:{8}, Is Transition Achieved:{9}, TradingSymbol:{10}",
                    runningCandlePayload.SnapshotDateTime.ToString,
                    runningCandlePayload.PreviousPayload.SnapshotDateTime.ToString,
                    runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime.ToString,
                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Above, True),
                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, CrossDirection.Below, True),
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor.ToString,
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value,
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).SupertrendColor.ToString,
                    CType(supertrendConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), SupertrendConsumer.SupertrendPayload).Supertrend.Value,
                    ret,
                    Me.TradableInstrument.TradingSymbol)
                Catch ex As Exception
                    logger.Error(ex)
                End Try
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