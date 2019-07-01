Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators
Imports Utilities.Numbers

Public Class JoyMaaATMStrategyInstrument
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
    Private _lastTick As ITick
    Private _lastDayFractalHighChange As Boolean = False
    Private _lastDayFractalLowChange As Boolean = False
    Private _eligibleToTakeTrade As Boolean = False
    Private ReadOnly _dummyATRConsumer As ATRConsumer
    Private ReadOnly _dummyFractalConsumer As FractalConsumer
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
                {New FractalConsumer(chartConsumer),
                 New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, JoyMaaATMUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyFractalConsumer = New FractalConsumer(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, JoyMaaATMUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As JoyMaaATMUserInputs = Me.ParentStrategy.UserSettings
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
                        _eligibleToTakeTrade = False
                        If placeOrderResponse IsNot Nothing Then
                            Dim potentialTargetPL As Decimal = 0
                            Dim potentialStoplossPL As Decimal = 0
                            If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.Quantity)
                            ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            End If
                            Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Direction:{1}, Signal Candle Time:{2}, Fractal:{3}, ATR:{4}, Entry Price:{5}, Quantity:{6}, Stoploss Price:{7}, Target Price:{8}, Potential Target PL:{9}, Potential Stoploss PL:{10}, LTP:{11}, Timestamp:{12}",
                                                                  Me.TradableInstrument.TradingSymbol,
                                                                  placeOrderResponse.ParentOrder.TransactionType.ToString,
                                                                  placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToShortTimeString,
                                                                  Math.Round(Val(placeOrderTrigger.Item2.Supporting(0)), 2),
                                                                  Math.Round(Val(placeOrderTrigger.Item2.Supporting(1)), 2),
                                                                  placeOrderResponse.ParentOrder.AveragePrice,
                                                                  placeOrderResponse.ParentOrder.Quantity,
                                                                  placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice,
                                                                  placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice,
                                                                  Math.Round(potentialTargetPL, 2),
                                                                  Math.Round(potentialStoplossPL, 2),
                                                                  _lastTick.LastPrice,
                                                                  Now)
                            GenerateTelegramMessageAsync(message)
                        End If
                    End If
                End If
                'Place Order block end

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
        Dim userSettings As JoyMaaATMUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim fractalConsumer As FractalConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyFractalConsumer)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
                    _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, {5}, {6}, Is Active Instrument:{7}, Reverse Trade:{8}, Number Of Trade:{9}, Current Time:{10}, Current LTP:{11}, Last Day Fractal High Changed:{12}, Last Day Fractal Low Changed:{13}, Eligible To Take Trade:{14}, Last Exit Condition:{15}, TradingSymbol:{16}",
                                userSettings.TradeStartTime.ToString,
                                userSettings.LastTradeEntryTime.ToString,
                                runningCandlePayload.SnapshotDateTime.ToString,
                                runningCandlePayload.PayloadGeneratedBy.ToString,
                                Me.TradableInstrument.IsHistoricalCompleted,
                                fractalConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                IsActiveInstrument(),
                                userSettings.ReverseTrade,
                                Me.GetTotalExecutedOrders(),
                                currentTime.ToString,
                                currentTick.LastPrice,
                                _lastDayFractalHighChange,
                                _lastDayFractalLowChange,
                                _eligibleToTakeTrade,
                                If(_lastExitCondition = "", "Nothing", _lastExitCondition),
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
            (Not IsActiveInstrument() OrElse userSettings.ReverseTrade) AndAlso Me.GetTotalExecutedOrders < userSettings.NumberOfTradePerDay AndAlso
            _lastExitCondition <> "TARGET" Then

            If RawPayloadDependentConsumers.FirstOrDefault.ConsumerPayloads IsNot Nothing AndAlso RawPayloadDependentConsumers.FirstOrDefault.ConsumerPayloads.Count > 0 AndAlso
                RawPayloadDependentConsumers.FirstOrDefault.ConsumerPayloads.ContainsKey(Me.TradableInstrument.ExchangeDetails.ExchangeStartTime) AndAlso
                Not _lastDayFractalHighChange AndAlso Not _lastDayFractalLowChange Then
                Dim firstCandleOfTheDay As OHLCPayload = RawPayloadDependentConsumers.FirstOrDefault.ConsumerPayloads(Me.TradableInstrument.ExchangeDetails.ExchangeStartTime)
                If firstCandleOfTheDay.PreviousPayload IsNot Nothing Then
                    Dim previousDayFractalHigh As Decimal = CType(fractalConsumer.ConsumerPayloads(firstCandleOfTheDay.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalHigh.Value
                    Dim previousDayFractalLow As Decimal = CType(fractalConsumer.ConsumerPayloads(firstCandleOfTheDay.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalLow.Value
                    Dim fractalCheckTime As Date = Me.TradableInstrument.ExchangeDetails.ExchangeStartTime
                    While fractalCheckTime <= runningCandlePayload.PreviousPayload.SnapshotDateTime
                        Dim fractalHigh As Decimal = CType(fractalConsumer.ConsumerPayloads(fractalCheckTime), FractalConsumer.FractalPayload).FractalHigh.Value
                        Dim fractalLow As Decimal = CType(fractalConsumer.ConsumerPayloads(fractalCheckTime), FractalConsumer.FractalPayload).FractalLow.Value
                        If fractalHigh <> previousDayFractalHigh OrElse fractalLow <> previousDayFractalLow Then
                            _lastDayFractalHighChange = True
                            _lastDayFractalLowChange = True
                            Exit While
                        End If
                        fractalCheckTime = fractalCheckTime.AddMinutes(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                    End While
                End If
            End If

            Dim atr As Decimal = CType(atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value

            Dim potentialLongEntryPrice As Decimal = CType(fractalConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalHigh.Value
            'Dim longEntryBuffer As Decimal = CalculateBuffer(potentialLongEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
            Dim longEntryBuffer As Decimal = ConvertFloorCeling(atr * 10 / 100, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

            Dim potentialShortEntryPrice As Decimal = CType(fractalConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalLow.Value
            'Dim shortEntryBuffer As Decimal = CalculateBuffer(potentialShortEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
            Dim shortEntryBuffer As Decimal = ConvertFloorCeling(atr * 10 / 100, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

            If Not _eligibleToTakeTrade AndAlso runningCandlePayload.PreviousPayload.SnapshotDateTime.Date = Now.Date AndAlso
                (runningCandlePayload.PreviousPayload.HighPrice.Value >= potentialShortEntryPrice AndAlso
                runningCandlePayload.PreviousPayload.HighPrice.Value <= potentialLongEntryPrice) OrElse
                (runningCandlePayload.PreviousPayload.LowPrice.Value >= potentialShortEntryPrice AndAlso
                runningCandlePayload.PreviousPayload.LowPrice.Value <= potentialLongEntryPrice) Then
                _eligibleToTakeTrade = True
            End If

            If _eligibleToTakeTrade AndAlso _lastDayFractalHighChange AndAlso currentTick.LastPrice >= potentialLongEntryPrice + longEntryBuffer Then
                Dim potentialEntryPrice As Decimal = potentialLongEntryPrice + longEntryBuffer
                Dim stoploss As Decimal = ConvertFloorCeling(atr, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                        {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                         .Price = potentialEntryPrice,
                         .StoplossValue = stoploss,
                         .SquareOffValue = target,
                         .Quantity = CalculateQuantityFromTarget(potentialEntryPrice, potentialEntryPrice + target, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).TargetINR),
                         .Supporting = New List(Of Object)}

                parameters.Supporting.Add(potentialLongEntryPrice)
                parameters.Supporting.Add(CType(atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value)
                _lastTick = currentTick
            ElseIf _eligibleToTakeTrade AndAlso _lastDayFractalLowChange AndAlso currentTick.LastPrice <= potentialShortEntryPrice - shortEntryBuffer Then
                Dim potentialEntryPrice As Decimal = potentialShortEntryPrice - shortEntryBuffer
                Dim stoploss As Decimal = ConvertFloorCeling(atr, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                        {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                         .Price = potentialEntryPrice,
                         .StoplossValue = stoploss,
                         .SquareOffValue = target,
                         .Quantity = CalculateQuantityFromTarget(potentialEntryPrice - target, potentialEntryPrice, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).TargetINR),
                         .Supporting = New List(Of Object)}

                parameters.Supporting.Add(potentialShortEntryPrice)
                parameters.Supporting.Add(CType(atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value)
                _lastTick = currentTick
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
                                    {5}, {6}, 
                                    Is Active Instrument:{7}, Reverse Trade:{8}, Number Of Trade:{9}, 
                                    Current Time:{10}, Current LTP:{11}, 
                                    Last Day Fractal High Changed:{12}, Last Day Fractal Low Changed:{13}, Eligible To Take Trade:{14}, 
                                    Last Exit Condition:{15}, TradingSymbol:{16}",
                                    userSettings.TradeStartTime.ToString,
                                    userSettings.LastTradeEntryTime.ToString,
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    fractalConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                    atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                    IsActiveInstrument(),
                                    userSettings.ReverseTrade,
                                    Me.GetTotalExecutedOrders(),
                                    currentTime.ToString,
                                    currentTick.LastPrice,
                                    _lastDayFractalHighChange,
                                    _lastDayFractalLowChange,
                                    _eligibleToTakeTrade,
                                    If(_lastExitCondition = "", "Nothing", _lastExitCondition),
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

    Protected Overrides Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
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

            Dim exitOrderResponse As IBusinessOrder = Nothing

            If reason.ToUpper = "TARGET REACHED" OrElse reason.ToUpper = "STOPLOSS REACHED" Then
                exitOrderResponse = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
            Else
                exitOrderResponse = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)
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
                Dim message As String = String.Format("{0}. Trading Symbol:{1}, Direction:{2}, Entry Price:{3}, Quantity:{4}, Exit Price:{5}, Order PL:{6}, Total Stock PL:{7}, Overall PL:{8}, LTP:{9}, Timestamp:{10}",
                                                              reason,
                                                              Me.TradableInstrument.TradingSymbol,
                                                              exitOrderResponse.ParentOrder.TransactionType.ToString,
                                                              exitOrderResponse.ParentOrder.AveragePrice,
                                                              exitOrderResponse.ParentOrder.Quantity,
                                                              exitPrice,
                                                              Math.Round(Me.GetTotalPLOfAnOrderAfterBrokerage(exitOrderResponse.ParentOrderIdentifier), 2),
                                                              Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                              Math.Round(Me.ParentStrategy.GetTotalPLAfterBrokerage, 2),
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
        Dim userInputs As JoyMaaATMUserInputs = Me.ParentStrategy.UserSettings
        If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
            userInputs.TelegramChatID IsNot Nothing AndAlso Not userInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
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
