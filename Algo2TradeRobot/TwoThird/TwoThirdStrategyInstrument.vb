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
    Private _lastPrevPayloadInnerPlaceOrder As String = ""
    Private _lastPrevPayloadOuterPlaceOrder As String = ""
    Private _lastPlacedOrder As IBusinessOrder = Nothing
    Private _lastExitCondition As String = ""
    Private _lastExitTime As Date = Now.Date
    Private _lastTick As ITick = Nothing
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

                _cts.Token.ThrowIfCancellationRequested()
                Dim activeOrder As IBusinessOrder = Me.GetActiveOrder(IOrder.TypeOfTransaction.None)
                'Check Target block start
                If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 AndAlso
                    activeOrder.TargetOrder IsNot Nothing AndAlso activeOrder.TargetOrder.Count > 0 Then
                    If activeOrder.TargetOrder.Count > 1 Then
                        Throw New ApplicationException("Why target order greater than 1")
                    Else
                        Dim currentTick As ITick = Me.TradableInstrument.LastTick
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice >= target Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                                _lastExitCondition = "TARGET"
                                _lastExitTime = Now
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice <= target Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                                _lastExitCondition = "TARGET"
                                _lastExitTime = Now
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
                        Dim currentTick As ITick = Me.TradableInstrument.LastTick
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            If currentTick.LastPrice <= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                                _lastExitCondition = "STOPLOSS"
                                _lastExitTime = Now
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If currentTick.LastPrice >= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                                _lastExitCondition = "STOPLOSS"
                                _lastExitTime = Now
                            End If
                        End If
                    End If
                End If
                'Check Stoploss block end

                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    Dim reverseExit As Boolean = False
                    If userSettings.ReverseTrade AndAlso _lastPlacedOrder IsNot Nothing AndAlso
                        _lastPlacedOrder.ParentOrder.TransactionType <> placeOrderTrigger.Item2.EntryDirection AndAlso
                        _lastPlacedOrder.SLOrder IsNot Nothing AndAlso _lastPlacedOrder.SLOrder.Count > 0 Then
                        Await ForceExitSpecificTradeAsync(_lastPlacedOrder.SLOrder.FirstOrDefault, "Force exit order for reverse entry").ConfigureAwait(False)
                        reverseExit = True
                    End If

                    If _lastExitCondition = "TARGET" Then
                        If Not _lastPrevPayloadOuterPlaceOrder = placeOrderTrigger.Item2.SignalCandle.ToString Then
                            _lastPrevPayloadOuterPlaceOrder = placeOrderTrigger.Item2.SignalCandle.ToString
                            logger.Debug("****Place Order******Can not take trade as target reached previous trade********")
                        End If
                    ElseIf Not IsActiveInstrument() Then
                        Dim modifiedPlaceOrderTrigger As Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String) = New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(placeOrderTrigger.Item1, Me, placeOrderTrigger.Item2, placeOrderTrigger.Item3)
                        Dim placeOrderResponse As IBusinessOrder = Nothing
                        If reverseExit Then
                            placeOrderResponse = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger).ConfigureAwait(False)
                        Else
                            placeOrderResponse = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger, True, _lastTick).ConfigureAwait(False)
                        End If
                        _lastPlacedOrder = placeOrderResponse
                        If placeOrderResponse IsNot Nothing Then
                            Dim potentialTargetPL As Decimal = 0
                            Dim potentialStoplossPL As Decimal = 0
                            Dim potentialEntry As Decimal = 0
                            Dim slipage As Decimal = 0
                            Dim atr As Decimal = Math.Round(Val(placeOrderTrigger.Item2.Supporting(0)), 2)
                            Dim buffer As Decimal = Math.Round(Val(placeOrderTrigger.Item2.Supporting(1)), 2)
                            If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialEntry = _signalCandle.HighPrice.Value + buffer
                                slipage = potentialEntry - placeOrderResponse.ParentOrder.AveragePrice
                            ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialEntry = _signalCandle.LowPrice.Value - buffer
                                slipage = placeOrderResponse.ParentOrder.AveragePrice - potentialEntry
                            End If
                            Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Signal Candle Time:{1}, Candle Range:{2}, ATR:{3}, {4}Direction:{5}, Quantity:{6}, {7}Potential Entry:{8}, Entry Price:{9}({10}), {11}Stoploss Price:{12}, Potential Stoploss PL:{13}, {14}Target Price:{15}, Potential Target PL:{16}, {17}LTP:{18}, Timestamp:{19}",
                                                                  Me.TradableInstrument.TradingSymbol,
                                                                  _signalCandle.SnapshotDateTime.ToShortTimeString,
                                                                  _signalCandle.CandleRange,
                                                                  atr,
                                                                  vbNewLine,
                                                                  placeOrderResponse.ParentOrder.TransactionType.ToString,
                                                                  placeOrderResponse.ParentOrder.Quantity,
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
                                                                  If(reverseExit, "Entered at next available tick", _lastTick.LastPrice),
                                                                  Now)
                            GenerateTelegramMessageAsync(message)
                        End If
                    End If
                End If
                'Place Order block end

                'Modify Order block start
                Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    Dim modifyOrderResponse As IBusinessOrder = Await ModifySLPaperTradeAsync(modifyStoplossOrderTrigger).ConfigureAwait(False)
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
                        Dim message As String = String.Format("Order Modified. Trading Symbol:{0}, {1}Direction:{2}, Quantity:{3}, {4}Entry Price:{5}, {6}Stoploss Price:{7}, Potential Stoploss PL:{8}, {9}Target Price:{10}, Potential Target PL:{11}, {12}Reason:{13}, Timestamp:{14}",
                                                                Me.TradableInstrument.TradingSymbol,
                                                                vbNewLine,
                                                                modifyOrderResponse.ParentOrder.TransactionType.ToString,
                                                                modifyOrderResponse.ParentOrder.Quantity,
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
                                                                Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                End If
                'Modify Order block end

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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
                    _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Signal Candle Range:{5}, {6}, Is Active Instrument:{7}, Reverse Trade:{8}, Number Of Trade:{9}, Stoploss Movement To Breakeven:{10}, Count Trades With Breakeven Movement:{11}, OverAll PL:{12}, Last Exit Condition:{13}, Current Time:{14}, Current LTP:{15}, TradingSymbol:{16}",
                                userSettings.TradeStartTime.ToString,
                                userSettings.LastTradeEntryTime.ToString,
                                runningCandlePayload.SnapshotDateTime.ToString,
                                runningCandlePayload.PayloadGeneratedBy.ToString,
                                Me.TradableInstrument.IsHistoricalCompleted,
                                If(_signalCandle IsNot Nothing, _signalCandle.CandleRange, "Nothing"),
                                atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                IsActiveInstrument(),
                                userSettings.ReverseTrade,
                                If(userSettings.CountTradesWithBreakevenMovement, Me.GetTotalExecutedOrders(), GetTotalLogicalExecutedOrders()),
                                userSettings.StoplossMovementToBreakeven,
                                userSettings.CountTradesWithBreakevenMovement,
                                Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                If(_lastExitCondition = "", "Nothing", _lastExitCondition),
                                currentTime.ToString,
                                currentTick.LastPrice,
                                Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            (Not IsActiveInstrument() OrElse userSettings.ReverseTrade) AndAlso ((userSettings.CountTradesWithBreakevenMovement AndAlso
            Me.GetTotalExecutedOrders < userSettings.NumberOfTradePerStock) OrElse GetTotalLogicalExecutedOrders() < userSettings.NumberOfTradePerStock) AndAlso
            _lastExitCondition <> "TARGET" AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() > Math.Abs(userSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay Then
            Dim atr As Decimal = Math.Round(CType(atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)

            If _signalCandle Is Nothing Then
                If runningCandlePayload.PreviousPayload.CandleRange <> 0 AndAlso runningCandlePayload.PreviousPayload.CandleRange <= atr Then
                    _signalCandle = runningCandlePayload.PreviousPayload
                End If
            Else
                atr = Math.Round(CType(atrConsumer.ConsumerPayloads(_signalCandle.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)
                Dim potentialLongEntryPrice As Decimal = _signalCandle.HighPrice.Value
                Dim longEntryBuffer As Decimal = CalculateBuffer(potentialLongEntryPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)

                Dim potentialShortEntryPrice As Decimal = _signalCandle.LowPrice.Value
                Dim shortEntryBuffer As Decimal = CalculateBuffer(potentialShortEntryPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)

                Dim quantity As Integer = 0
                If currentTick.LastPrice >= potentialLongEntryPrice + longEntryBuffer Then
                    Dim potentialEntryPrice As Decimal = potentialLongEntryPrice + longEntryBuffer
                    Dim stoploss As Decimal = ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        If userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity <> Integer.MinValue Then
                            quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity
                            _firstTradeQuantity = quantity
                        Else
                            quantity = CalculateQuantityFromInvestment(potentialEntryPrice, 30, userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Capital, userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).AllowCapitalToIncrease)
                            _firstTradeQuantity = quantity
                        End If
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .Price = potentialEntryPrice,
                                     .StoplossValue = stoploss + 2 * longEntryBuffer,
                                     .SquareOffValue = target,
                                     .Quantity = quantity,
                                     .Supporting = New List(Of Object)}

                        parameters.Supporting.Add(atr)
                        parameters.Supporting.Add(longEntryBuffer)
                        _lastTick = currentTick
                    End If
                ElseIf currentTick.LastPrice <= potentialShortEntryPrice - shortEntryBuffer Then
                    Dim potentialEntryPrice As Decimal = potentialShortEntryPrice - shortEntryBuffer
                    Dim stoploss As Decimal = ConvertFloorCeling(_signalCandle.CandleRange, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    If _firstTradeQuantity = Integer.MinValue Then
                        If userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity <> Integer.MinValue Then
                            quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity
                            _firstTradeQuantity = quantity
                        Else
                            quantity = CalculateQuantityFromInvestment(potentialEntryPrice, 30, userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Capital, userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).AllowCapitalToIncrease)
                            _firstTradeQuantity = quantity
                        End If
                    Else
                        quantity = _firstTradeQuantity
                    End If
                    If quantity <> 0 Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .Price = potentialEntryPrice,
                                     .StoplossValue = stoploss + 2 * shortEntryBuffer,
                                     .SquareOffValue = target,
                                     .Quantity = quantity,
                                     .Supporting = New List(Of Object)}

                        parameters.Supporting.Add(atr)
                        parameters.Supporting.Add(shortEntryBuffer)
                        _lastTick = currentTick
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                If forcePrint Then
                    logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    If Me.TradableInstrument.IsHistoricalCompleted Then
                        logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                        logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, 
                                    RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, 
                                    Signal Candle Range:{5}, {6}, 
                                    Is Active Instrument:{7}, Reverse Trade:{8}, Number Of Trade:{9}, 
                                    Stoploss Movement To Breakeven:{10}, Count Trades With Breakeven Movement:{11},
                                    OverAll PL:{12},
                                    Last Exit Condition:{13}, Current Time:{14}, Current LTP:{15}, TradingSymbol:{16}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    If(_signalCandle IsNot Nothing, _signalCandle.CandleRange, "Nothing"),
                                    atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                    IsActiveInstrument(),
                                    userSettings.ReverseTrade,
                                    If(userSettings.CountTradesWithBreakevenMovement, Me.GetTotalExecutedOrders(), GetTotalLogicalExecutedOrders()),
                                    userSettings.StoplossMovementToBreakeven,
                                    userSettings.CountTradesWithBreakevenMovement,
                                    Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                                    If(_lastExitCondition = "", "Nothing", _lastExitCondition),
                                    currentTime.ToString,
                                    currentTick.LastPrice,
                                    Me.TradableInstrument.TradingSymbol)
                    End If
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

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If userSettings.StoplossMovementToBreakeven Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each runningOrderID In OrderDetails.Keys
                    Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                    Dim targetReachedForBreakevenMovement As Boolean = False
                    If bussinessOrder.TargetOrder IsNot Nothing AndAlso bussinessOrder.TargetOrder.Count > 0 Then
                        For Each runningTragetOrder In bussinessOrder.TargetOrder
                            If runningTragetOrder.Status <> IOrder.TypeOfStatus.Rejected Then
                                Dim target As Decimal = 0
                                Dim potentialTargetPrice As Decimal = 0
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    target = runningTragetOrder.AveragePrice - bussinessOrder.ParentOrder.AveragePrice
                                    potentialTargetPrice = bussinessOrder.ParentOrder.AveragePrice + ConvertFloorCeling(target * 2 / 3, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                    If currentTick.LastPrice >= potentialTargetPrice Then targetReachedForBreakevenMovement = True
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    target = bussinessOrder.ParentOrder.AveragePrice - runningTragetOrder.AveragePrice
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
                                triggerPrice = bussinessOrder.ParentOrder.AveragePrice + GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, IOrder.TypeOfTransaction.Buy)
                            Else
                                triggerPrice = _signalCandle.LowPrice.Value - CalculateBuffer(_signalCandle.LowPrice.Value, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)
                            End If
                        ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If targetReachedForBreakevenMovement Then
                                triggerPrice = bussinessOrder.ParentOrder.AveragePrice - GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, IOrder.TypeOfTransaction.Sell)
                            Else
                                triggerPrice = _signalCandle.HighPrice.Value + CalculateBuffer(_signalCandle.HighPrice.Value, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Floor)
                            End If
                        End If
                        For Each slOrder In bussinessOrder.SLOrder
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
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, If(targetReachedForBreakevenMovement, "Move to breakeven", "SL movement to signal candle high/low")))
                                End If
                            End If
                        Next
                    End If
                Next
            End If
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
            Dim exitOrderResponse As IBusinessOrder = Nothing
            If reason.ToUpper = "TARGET REACHED" Then
                exitOrderResponse = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).TargetOrder.LastOrDefault.AveragePrice
            ElseIf reason.ToUpper = "STOPLOSS REACHED" OrElse
                reason.ToUpper = "FORCE EXIT ORDER FOR REVERSE ENTRY" Then
                exitOrderResponse = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.AveragePrice
            Else
                exitOrderResponse = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.AveragePrice
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
                If exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, exitOrderResponse.ParentOrder.AveragePrice, potentialExitPrice, exitOrderResponse.ParentOrder.Quantity)
                ElseIf exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, potentialExitPrice, exitOrderResponse.ParentOrder.AveragePrice, exitOrderResponse.ParentOrder.Quantity)
                End If
                Dim message As String = String.Format("{0}. Trading Symbol:{1}, {2}Direction:{3}, Quantity:{4}, {5}Entry Price:{6}, {7}Potential Exit Price:{8}, Potential Exit PL:{9}, {10}Exit Price:{11}, Order PL:{12}, {13}Total Stock PL:{14}, Overall PL:{15}, {16}LTP:{17}, Timestamp:{18}",
                                                        reason,
                                                        Me.TradableInstrument.TradingSymbol,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.TransactionType.ToString,
                                                        exitOrderResponse.ParentOrder.Quantity,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.AveragePrice,
                                                        vbNewLine,
                                                        potentialExitPrice,
                                                        Math.Round(potentialExitPL, 2),
                                                        vbNewLine,
                                                        exitPrice,
                                                        Math.Round(Me.GetTotalPLOfAnOrderAfterBrokerage(exitOrderResponse.ParentOrderIdentifier), 2),
                                                        vbNewLine,
                                                        Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                        Math.Round(Me.ParentStrategy.GetTotalPLAfterBrokerage, 2),
                                                        vbNewLine,
                                                        _lastTick.LastPrice,
                                                        Now)
                GenerateTelegramMessageAsync(message)
            End If
        End If
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
            userInputs.TelegramChatID IsNot Nothing AndAlso Not userInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
    End Function

    Private Function GetTotalLogicalExecutedOrders() As Integer
        Dim ret As Integer = Me.GetTotalExecutedOrders()
        Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        If ret > 0 Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
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
