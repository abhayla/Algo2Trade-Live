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
    Private _lastTick As ITick = Nothing

    Private _stockMaxProfitPL As Decimal = Decimal.MinValue
    Private _stockMaxLossPL As Decimal = Decimal.MinValue
    Private _exitDoneForStockMaxLoss As Boolean = False

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
                {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Function HandleTickTriggerToUIETCAsync() As Task
        If Me.ParentStrategy.GetTotalPLAfterBrokerage <= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxLossPerDay * -1 Then
            CType(Me.ParentStrategy, PetDGandhiStrategy).SendMTMNotification()
        ElseIf Me.ParentStrategy.GetTotalPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxProfitPerDay Then
            CType(Me.ParentStrategy, PetDGandhiStrategy).SendMTMNotification()
        End If
        If _stockMaxProfitPL <> Decimal.MinValue Then
            If Me.GetOverallPLAfterBrokerage >= _stockMaxProfitPL Then
                SendNotification()
            End If
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
                _cts.Token.ThrowIfCancellationRequested()

                'Stock max loss
                If _stockMaxLossPL <> Decimal.MinValue Then
                    If Me.GetOverallPLAfterBrokerage <= _stockMaxLossPL Then
                        Await ForceExitAllTradesAsync("Force Exit. Reason:Stock Max Loss reached").ConfigureAwait(False)
                        _exitDoneForStockMaxLoss = True
                    End If
                End If

                _lastTick = Me.TradableInstrument.LastTick
                Dim currentTick As ITick = _lastTick
                _cts.Token.ThrowIfCancellationRequested()

                'Place Order block start
                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 AndAlso placeOrderTriggers.FirstOrDefault.Item1 = ExecuteCommandAction.Take Then
                    Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = placeOrderTriggers.FirstOrDefault
                    Dim modifiedPlaceOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = New List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String))
                    modifiedPlaceOrderTrigger.Add(New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(placeOrderTrigger.Item1, Me, placeOrderTrigger.Item2, placeOrderTrigger.Item3))
                    Dim placeOrderResponses As List(Of IBusinessOrder) = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger, True, _lastTick).ConfigureAwait(False)
                    Dim placeOrderResponse As IBusinessOrder = placeOrderResponses.FirstOrDefault
                    If placeOrderResponse IsNot Nothing Then
                        Dim potentialTargetPL As Decimal = 0
                        Dim potentialStoplossPL As Decimal = 0
                        Dim potentialEntry As Decimal = 0
                        Dim slipage As Decimal = 0
                        If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialEntry = placeOrderTrigger.Item2.TriggerPrice
                            slipage = potentialEntry - placeOrderResponse.ParentOrder.AveragePrice
                        ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                            potentialEntry = placeOrderTrigger.Item2.TriggerPrice
                            slipage = placeOrderResponse.ParentOrder.AveragePrice - potentialEntry
                        End If
                        Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Signal Candle Time:{1}, Candle Body:{2}, ATR:{3}, Quantity:{4}, {5}Direction:{6}, {7}Potential Entry:{8}, Entry Price:{9}({10}), {11}Stoploss Price:{12}, Potential Stoploss PL:₹{13}, {14}Target Price:{15}, Potential Target PL:₹{16}, {17}Total Stock PL:₹{18}, {19}LTP:{20}, Tick Timestamp:{21}, {22}Timestamp:{23}",
                                                                Me.TradableInstrument.TradingSymbol,
                                                                placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToShortTimeString,
                                                                GetCandleBody(placeOrderTrigger.Item2.SignalCandle, placeOrderResponse.ParentOrder.TransactionType),
                                                                GetCandleATR(placeOrderTrigger.Item2.SignalCandle),
                                                                placeOrderResponse.ParentOrder.Quantity,
                                                                vbNewLine,
                                                                placeOrderResponse.ParentOrder.TransactionType.ToString,
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
                                                                Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                                vbNewLine,
                                                                _lastTick.LastPrice,
                                                                _lastTick.Timestamp,
                                                                vbNewLine,
                                                                Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                End If
                'Place Order block end

                _cts.Token.ThrowIfCancellationRequested()
                'Exit Order block start
                Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                    Await ForceExitAllTradesAsync(exitOrderTrigger.FirstOrDefault.Item3).ConfigureAwait(False)
                End If
                'Exit Order block end

                _cts.Token.ThrowIfCancellationRequested()
                'Modify Order block start
                Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    Dim modifyOrderResponses As List(Of IBusinessOrder) = Await ModifySLPaperTradeAsync(modifyStoplossOrderTrigger).ConfigureAwait(False)
                    Dim modifyOrderResponse As IBusinessOrder = Nothing
                    If modifyOrderResponses IsNot Nothing AndAlso modifyOrderResponses.Count > 0 Then
                        modifyOrderResponse = modifyOrderResponses.FirstOrDefault
                    End If
                    If modifyOrderResponse IsNot Nothing Then
                        Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(modifyOrderResponse.ParentOrderIdentifier, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                        Dim potentialTargetPL As Decimal = 0
                        Dim potentialStoplossPL As Decimal = 0
                        If modifyOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, modifyOrderResponse.ParentOrder.Quantity)
                        ElseIf modifyOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                            potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, modifyOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, modifyOrderResponse.ParentOrder.AveragePrice, modifyOrderResponse.ParentOrder.Quantity)
                        End If
                        Dim message As String = String.Format("Order Modified. Reason:{16}, {15}Trading Symbol:{0}, Signal Candle Time:{1}, Candle Body:{2}, ATR:{3}, Quantity:{4}, {5}Direction:{6}, {7}Entry Price:{8}, {9}Stoploss Price:{10}, Potential Stoploss PL:₹{11}, {12}Target Price:{13}, Potential Target PL:₹{14}, {17}Total Stock PL:₹{18}, Timestamp:{19}",
                                                                Me.TradableInstrument.TradingSymbol,
                                                                signalCandle.SnapshotDateTime.ToShortTimeString,
                                                                GetCandleBody(signalCandle, modifyOrderResponse.ParentOrder.TransactionType),
                                                                GetCandleATR(signalCandle),
                                                                modifyOrderResponse.ParentOrder.Quantity,
                                                                vbNewLine,
                                                                modifyOrderResponse.ParentOrder.TransactionType.ToString,
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
                                                                vbNewLine,
                                                                Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                                Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                End If
                'Modify Order block end

                _cts.Token.ThrowIfCancellationRequested()
                Dim activeOrder As IBusinessOrder = Me.GetActiveOrder(IOrder.TypeOfTransaction.None)
                'Check Target block start
                If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 AndAlso
                    activeOrder.TargetOrder IsNot Nothing AndAlso activeOrder.TargetOrder.Count > 0 Then
                    If activeOrder.TargetOrder.Count > 1 Then
                        Throw New ApplicationException("Why target order greater than 1")
                    Else
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice >= target Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                            If currentTick.LastPrice <= target Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
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
                        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            If currentTick.LastPrice <= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If currentTick.LastPrice >= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                                _lastTick = currentTick
                                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                            End If
                        End If
                    End If
                End If
                'Check Stoploss block end
                _cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
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
        Dim currentTick As ITick = _lastTick
        Dim currentTime As Date = Now()
        Dim lastExecutedOrder As IBusinessOrder = GetLastExecutedOrder()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                (Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint) Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)

                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Potential Signal Candle Time:{5}, Potential Signal Candle Range:{6}, Potential Signal Candle Top:{7}%, Potential Signal Candle Bottom:{8}%, Potential Signal Candle Top Body:{9}, Potential Signal Candle Bottom Body:{10}, Potential Signal Candle Source:{11}, Potential Signal Candle ATR:{12}, Is Active Instrument:{13}, Number Of Trade:{14}, OverAll PL:{15}, Stock PL:{16}, Is Any Trade Target Reached:{17}, Strategy Exit All Triggerd:{18}, Stock Max Loss Triggerd:{19}, Is Last Trade Exited At Current Candle:{20}, Is Last Trade Force Exit For Candle Close:{21}, Running Capital:{22}, Current Time:{23}, Current LTP:{24}, TradingSymbol:{25}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToShortTimeString,
                            runningCandlePayload.PreviousPayload.CandleRange,
                            Math.Round((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100, 2),
                            Math.Round((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100, 2),
                            If(runningCandlePayload.PreviousPayload.CandleColor = Color.Red, runningCandlePayload.PreviousPayload.HighPrice.Value - runningCandlePayload.PreviousPayload.ClosePrice.Value, runningCandlePayload.PreviousPayload.HighPrice.Value - runningCandlePayload.PreviousPayload.OpenPrice.Value),
                            If(runningCandlePayload.PreviousPayload.CandleColor = Color.Red, runningCandlePayload.PreviousPayload.OpenPrice.Value - runningCandlePayload.PreviousPayload.LowPrice.Value, runningCandlePayload.PreviousPayload.ClosePrice.Value - runningCandlePayload.PreviousPayload.LowPrice.Value),
                            runningCandlePayload.PreviousPayload.PayloadGeneratedBy.ToString,
                            GetCandleATR(runningCandlePayload.PreviousPayload),
                            IsActiveInstrument(),
                            Me.GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            Me.GetOverallPLAfterBrokerage(),
                            IsAnyTradeTargetReached(),
                            Me.StrategyExitAllTriggerd,
                            Me._exitDoneForStockMaxLoss,
                            IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime),
                            IsLastTradeForceExitForCandleClose(),
                            Math.Round(Await Me.ParentStrategy.GetRunningCapitalAsync(Me.TradableInstrument.ExchangeDetails.ExchangeType).ConfigureAwait(False), 4),
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)

                SendCandleDetails(runningCandlePayload, currentTime, userSettings)
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not Me.StrategyExitAllTriggerd AndAlso Not _exitDoneForStockMaxLoss Then

            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                Dim takeTrade As Boolean = True
                If lastExecutedOrder IsNot Nothing AndAlso
                    Utilities.Time.IsDateTimeEqualTillMinutes(lastExecutedOrder.ParentOrder.TimeStamp, runningCandlePayload.SnapshotDateTime) Then
                    takeTrade = False
                End If
                If takeTrade AndAlso (IsLastTradeForceExitForCandleClose() OrElse Not IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime)) Then
                    If _firstTradedQuantity = Integer.MinValue Then
                        _firstTradedQuantity = CalculateQuantityFromInvestment(signal.Item2, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapitalPerStock, userSettings.AllowToIncreaseQuantity)
                    End If
                    If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                        Dim triggerPrice As Decimal = signal.Item2
                        Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoplossPrice As Decimal = signal.Item3
                        Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                        Dim target As Decimal = ConvertFloorCeling(GetCandleATR(runningCandlePayload.PreviousPayload) * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                        If currentTick.LastPrice >= triggerPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                        {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                         .TriggerPrice = triggerPrice,
                                         .Price = price,
                                         .StoplossValue = stoploss,
                                         .SquareOffValue = target,
                                         .Quantity = _firstTradedQuantity,
                                         .Supporting = New List(Of Object) From {GetCandleATR(runningCandlePayload.PreviousPayload)}}
                        End If
                    ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                        Dim triggerPrice As Decimal = signal.Item2
                        Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoplossPrice As Decimal = signal.Item3
                        Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                        Dim target As Decimal = ConvertFloorCeling(GetCandleATR(runningCandlePayload.PreviousPayload) * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                        If currentTick.LastPrice <= triggerPrice Then
                            parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                        {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                         .TriggerPrice = triggerPrice,
                                         .Price = price,
                                         .StoplossValue = stoploss,
                                         .SquareOffValue = target,
                                         .Quantity = _firstTradedQuantity,
                                         .Supporting = New List(Of Object) From {GetCandleATR(runningCandlePayload.PreviousPayload)}}
                        End If
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Dim capitalRequired As Decimal = parameters.TriggerPrice * parameters.Quantity / userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier
            Dim runningCapital As Decimal = Await Me.ParentStrategy.GetRunningCapitalAsync(Me.TradableInstrument.ExchangeDetails.ExchangeType).ConfigureAwait(False)
            If userSettings.MaxCapitalToBeUsed - runningCapital >= capitalRequired Then
                Try
                    If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters.ToString, Me.TradableInstrument.TradingSymbol)
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try

                If _stockMaxProfitPL = Decimal.MinValue Then
                    Dim stockMaxProfitPoint As Decimal = ConvertFloorCeling(GetCandleATR(runningCandlePayload.PreviousPayload), Me.TradableInstrument.TickSize, RoundOfType.Celing) * userSettings.MaxProfitPerStockMultiplier
                    _stockMaxProfitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, parameters.TriggerPrice, parameters.TriggerPrice + stockMaxProfitPoint, parameters.Quantity)
                End If
                If _stockMaxLossPL = Decimal.MinValue Then
                    Dim stockMaxLossPoint As Decimal = ConvertFloorCeling(GetCandleATR(runningCandlePayload.PreviousPayload), Me.TradableInstrument.TickSize, RoundOfType.Floor) * userSettings.MaxLossPerStockMultiplier
                    _stockMaxLossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, parameters.TriggerPrice, parameters.TriggerPrice - stockMaxLossPoint, parameters.Quantity)
                End If

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
        Dim currentTick As ITick = _lastTick
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected AndAlso
                            Not slOrder.SupportingFlag Then
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            Dim reason As String = Nothing
                            Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(bussinessOrder.ParentOrderIdentifier, userSettings.SignalTimeFrame)
                            Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.AveragePrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            If signalCandle IsNot Nothing Then
                                If signalCandle.SnapshotDateTime = runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime Then
                                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        Dim potentialSLPrice As Decimal = Decimal.MinValue
                                        If signalCandle.CandleColor = Color.Red Then
                                            potentialSLPrice = signalCandle.ClosePrice.Value - buffer
                                        Else
                                            potentialSLPrice = signalCandle.OpenPrice.Value - buffer
                                        End If
                                        Dim minimusSL As Decimal = bussinessOrder.ParentOrder.AveragePrice * userSettings.MinLossPercentagePerTrade / 100
                                        If potentialSLPrice <= ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice - minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                                            triggerPrice = potentialSLPrice
                                            reason = "Move to candle body"
                                        Else
                                            triggerPrice = ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice - minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                            reason = "Minimum loss % per trade"
                                        End If
                                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        Dim potentialSLPrice As Decimal = Decimal.MinValue
                                        If signalCandle.CandleColor = Color.Red Then
                                            potentialSLPrice = signalCandle.OpenPrice.Value + buffer
                                        Else
                                            potentialSLPrice = signalCandle.ClosePrice.Value + buffer
                                        End If
                                        Dim minimusSL As Decimal = bussinessOrder.ParentOrder.AveragePrice * userSettings.MinLossPercentagePerTrade / 100
                                        If potentialSLPrice >= ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice + minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                                            triggerPrice = potentialSLPrice
                                            reason = "Move to candle body"
                                        Else
                                            triggerPrice = ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice + minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                            reason = "Minimum loss % per trade"
                                        End If
                                    End If
                                End If
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    If runningCandlePayload.PreviousPayload.LowPrice.Value > bussinessOrder.ParentOrder.AveragePrice Then
                                        Dim potentialPrice As Decimal = bussinessOrder.ParentOrder.AveragePrice + GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, bussinessOrder.ParentOrder.TransactionType)
                                        triggerPrice = ConvertFloorCeling(potentialPrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                                        reason = "Breakeven movement"
                                    End If
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    If runningCandlePayload.PreviousPayload.HighPrice.Value < bussinessOrder.ParentOrder.AveragePrice Then
                                        Dim potentialPrice As Decimal = bussinessOrder.ParentOrder.AveragePrice - GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, bussinessOrder.ParentOrder.TransactionType)
                                        triggerPrice = ConvertFloorCeling(potentialPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                                        reason = "Breakeven movement"
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
        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTick As ITick = _lastTick
        If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
            OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected AndAlso
                            Not slOrder.SupportingFlag Then
                            Dim exitOrder As Boolean = False
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(bussinessOrder.ParentOrderIdentifier, userSettings.SignalTimeFrame)
                            Dim buffer As Decimal = CalculateBuffer(bussinessOrder.ParentOrder.AveragePrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                            If signalCandle IsNot Nothing Then
                                If signalCandle.SnapshotDateTime = runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime Then
                                    If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        Dim potentialSLPrice As Decimal = Decimal.MinValue
                                        If signalCandle.CandleColor = Color.Red Then
                                            potentialSLPrice = signalCandle.ClosePrice.Value - buffer
                                        Else
                                            potentialSLPrice = signalCandle.OpenPrice.Value - buffer
                                        End If
                                        Dim minimusSL As Decimal = bussinessOrder.ParentOrder.AveragePrice * userSettings.MinLossPercentagePerTrade / 100
                                        triggerPrice = Math.Min(potentialSLPrice, ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice - minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor))
                                        If runningCandlePayload.PreviousPayload.ClosePrice.Value <= triggerPrice Then
                                            exitOrder = True
                                        End If
                                    ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        Dim potentialSLPrice As Decimal = Decimal.MinValue
                                        If signalCandle.CandleColor = Color.Red Then
                                            potentialSLPrice = signalCandle.OpenPrice.Value + buffer
                                        Else
                                            potentialSLPrice = signalCandle.ClosePrice.Value + buffer
                                        End If
                                        Dim minimusSL As Decimal = bussinessOrder.ParentOrder.AveragePrice * userSettings.MinLossPercentagePerTrade / 100
                                        triggerPrice = Math.Max(potentialSLPrice, ConvertFloorCeling(bussinessOrder.ParentOrder.AveragePrice + minimusSL, Me.TradableInstrument.TickSize, RoundOfType.Floor))
                                        If runningCandlePayload.PreviousPayload.ClosePrice.Value >= triggerPrice Then
                                            exitOrder = True
                                        End If
                                    End If
                                End If
                            End If

                            If exitOrder Then
                                'Below portion have to be done in every cancel order trigger
                                Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(bussinessOrder.ParentOrder.Tag)
                                If currentSignalActivities IsNot Nothing Then
                                    If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                    currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                    currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                        Continue For
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, bussinessOrder.ParentOrder, "Force Exit. Reason:Candle closed beyond body"))
                            End If
                        End If
                    Next
                End If
            Next
        End If
        If forcePrint AndAlso ret IsNot Nothing AndAlso ret.Count > 0 Then
            For Each runningOrder In ret
                logger.Debug("***** Exit Order ***** Order ID:{0}, Reason:{1}, {2}", runningOrder.Item2.OrderIdentifier, runningOrder.Item3, Me.TradableInstrument.TradingSymbol)
            Next
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

            Dim potentialExitPrice As Decimal = 0
            Dim exitOrderResponses As List(Of IBusinessOrder) = Nothing
            If reason.ToUpper = "TARGET REACHED" Then
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).TargetOrder.LastOrDefault.AveragePrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
            ElseIf reason.ToUpper = "STOPLOSS REACHED" Then
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.TriggerPrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
            ElseIf reason.ToUpper = "FORCE EXIT. REASON:CANDLE CLOSED BEYOND BODY" Then
                Dim lastTradeTime As Date = _lastTick.LastTradeTime.Value
                While Utilities.Time.IsTimeEqualTillSeconds(Me.TradableInstrument.LastTick.LastTradeTime.Value, lastTradeTime)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                _lastTick = Me.TradableInstrument.LastTick
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.TriggerPrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder, True, _lastTick).ConfigureAwait(False)
            Else
                potentialExitPrice = GetParentFromChildOrder(cancellableOrder.LastOrDefault.Item2).SLOrder.LastOrDefault.TriggerPrice
                exitOrderResponses = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)
                OnHeartbeat(String.Format("Force Cancel Order Successful. {0}", reason))
            End If

            Dim exitOrderResponse As IBusinessOrder = Nothing
            If exitOrderResponses IsNot Nothing AndAlso exitOrderResponses.Count > 0 Then
                exitOrderResponse = exitOrderResponses.FirstOrDefault
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
                Dim slipage As Decimal = 0
                Dim plSlipage As Decimal = 0
                Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(exitOrderResponse.ParentOrderIdentifier, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                Dim orderPL As Decimal = Me.GetTotalPLOfAnOrderAfterBrokerage(exitOrderResponse.ParentOrderIdentifier)
                If exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, exitOrderResponse.ParentOrder.AveragePrice, potentialExitPrice, exitOrderResponse.ParentOrder.Quantity)
                    slipage = exitPrice - potentialExitPrice
                    plSlipage = orderPL - potentialExitPL
                ElseIf exitOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    potentialExitPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, potentialExitPrice, exitOrderResponse.ParentOrder.AveragePrice, exitOrderResponse.ParentOrder.Quantity)
                    slipage = potentialExitPrice - exitPrice
                    plSlipage = orderPL - potentialExitPL
                End If
                If reason.ToUpper = "STOPLOSS REACHED" AndAlso potentialExitPL >= 0 Then
                    reason = "Breakeven Exit"
                End If
                Dim message As String = String.Format("{0}. {26}Trading Symbol:{1}, Signal Candle Time:{2}, Candle Body:{3}, ATR:{4}, Quantity:{5}, {6}Direction:{7}, {8}Entry Price:{9}, {10}Potential Exit Price:{11}, Exit Price:{12}({13}), {14}Potential Exit PL:₹{15}, Exit PL:₹{16}(₹{17}), {18}Total Stock PL:₹{19}, Number Of Trade:{20}, {21}LTP:{22}, Tick Timestamp:{23}, {24}Timestamp:{25}",
                                                        reason,
                                                        Me.TradableInstrument.TradingSymbol,
                                                        signalCandle.SnapshotDateTime.ToShortTimeString,
                                                        GetCandleBody(signalCandle, exitOrderResponse.ParentOrder.TransactionType),
                                                        GetCandleATR(signalCandle),
                                                        exitOrderResponse.ParentOrder.Quantity,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.TransactionType.ToString,
                                                        vbNewLine,
                                                        exitOrderResponse.ParentOrder.AveragePrice,
                                                        vbNewLine,
                                                        potentialExitPrice,
                                                        exitPrice,
                                                        slipage,
                                                        vbNewLine,
                                                        Math.Round(potentialExitPL, 2),
                                                        Math.Round(orderPL, 2),
                                                        Math.Round(plSlipage, 2),
                                                        vbNewLine,
                                                        Math.Round(Me.GetOverallPLAfterBrokerage(), 2),
                                                        GetTotalExecutedOrders(),
                                                        vbNewLine,
                                                        _lastTick.LastPrice,
                                                        _lastTick.Timestamp,
                                                        vbNewLine,
                                                        Now,
                                                        vbNewLine)
                GenerateTelegramMessageAsync(message)
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
                        'If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
                        If order.Status = IOrder.TypeOfStatus.Complete Then
                            Dim target As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = order.AveragePrice - bussinessOrder.ParentOrder.AveragePrice
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - order.AveragePrice
                            End If
                            If target > GetBreakevenPoint(bussinessOrder.ParentOrder.AveragePrice, bussinessOrder.ParentOrder.Quantity, bussinessOrder.ParentOrder.TransactionType) + Me.TradableInstrument.TickSize Then
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

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso Not candle.DeadCandle Then
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
            Dim slPoint As Decimal = candle.CandleRange
            Dim lowBuffer As Decimal = CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
            Dim highBuffer As Decimal = CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
            If candle.CandleWicks.Top + lowBuffer >= candle.CandleRange * userSettings.PinbarTailPercentage / 100 Then
                slPoint = slPoint + 2 * lowBuffer
                Dim potentialSLPrice As Decimal = Decimal.MinValue
                If candle.CandleColor = Color.Red Then
                    potentialSLPrice = candle.OpenPrice.Value
                Else
                    potentialSLPrice = candle.ClosePrice.Value
                End If
                If Math.Abs(potentialSLPrice - candle.LowPrice.Value) <= (GetCandleATR(candle) + lowBuffer) * userSettings.MaxLossPerTradeMultiplier Then
                    If slPoint < candle.LowPrice.Value * userSettings.MinLossPercentagePerTrade / 100 Then
                        slPoint = ConvertFloorCeling(candle.LowPrice.Value * userSettings.MinLossPercentagePerTrade / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    End If
                    ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, candle.LowPrice.Value - lowBuffer, candle.LowPrice.Value - lowBuffer + slPoint, IOrder.TypeOfTransaction.Sell)
                End If
            ElseIf candle.CandleWicks.Bottom + highBuffer >= candle.CandleRange * userSettings.PinbarTailPercentage / 100 Then
                slPoint = slPoint + 2 * highBuffer
                Dim potentialSLPrice As Decimal = Decimal.MinValue
                If candle.CandleColor = Color.Red Then
                    potentialSLPrice = candle.ClosePrice.Value
                Else
                    potentialSLPrice = candle.OpenPrice.Value
                End If
                If Math.Abs(candle.HighPrice.Value - potentialSLPrice) <= (GetCandleATR(candle) + highBuffer) * userSettings.MaxLossPerTradeMultiplier Then
                    If slPoint < candle.HighPrice.Value * userSettings.MinLossPercentagePerTrade / 100 Then
                        slPoint = ConvertFloorCeling(candle.HighPrice.Value * userSettings.MinLossPercentagePerTrade / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    End If
                    ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, candle.HighPrice.Value + highBuffer, candle.HighPrice.Value + highBuffer - slPoint, IOrder.TypeOfTransaction.Buy)
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetCandleBody(ByVal candle As OHLCPayload, ByVal direction As IOrder.TypeOfTransaction) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If candle IsNot Nothing Then
            If candle.CandleColor = Color.Red Then
                If direction = IOrder.TypeOfTransaction.Buy Then
                    ret = candle.HighPrice.Value - candle.ClosePrice.Value
                ElseIf direction = IOrder.TypeOfTransaction.Sell Then
                    ret = candle.OpenPrice.Value - candle.LowPrice.Value
                End If
            Else
                If direction = IOrder.TypeOfTransaction.Buy Then
                    ret = candle.HighPrice.Value - candle.OpenPrice.Value
                ElseIf direction = IOrder.TypeOfTransaction.Sell Then
                    ret = candle.ClosePrice.Value - candle.LowPrice.Value
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetCandleATR(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        If candle IsNot Nothing Then
            If atrConsumer.ConsumerPayloads IsNot Nothing AndAlso atrConsumer.ConsumerPayloads.Count > 0 AndAlso
                atrConsumer.ConsumerPayloads.ContainsKey(candle.SnapshotDateTime) Then
                ret = Math.Round(CType(atrConsumer.ConsumerPayloads(candle.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)
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
                    If order.Status = IOrder.TypeOfStatus.Complete Then
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

    Private Function IsLastTradeForceExitForCandleClose() As Boolean
        Dim ret As Boolean = False
        Dim lastExecutedOrder As IBusinessOrder = GetLastExecutedOrder()
        If lastExecutedOrder IsNot Nothing Then
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
                Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(lastExecutedOrder.ParentOrderIdentifier, timeframe)
                If blockDateInThisTimeframe = signalCandle.SnapshotDateTime.AddMinutes(2 * timeframe) Then
                    Dim lastTradeExitPrice As Decimal = Decimal.MinValue
                    If lastExecutedOrder.AllOrder IsNot Nothing AndAlso lastExecutedOrder.AllOrder.Count > 0 Then
                        For Each order In lastExecutedOrder.AllOrder
                            If order.Status = IOrder.TypeOfStatus.Complete Then
                                lastTradeExitPrice = order.AveragePrice
                            End If
                        Next
                    End If
                    If lastTradeExitPrice <> Decimal.MinValue Then
                        If lastExecutedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            If lastTradeExitPrice > signalCandle.LowPrice.Value AndAlso
                                lastTradeExitPrice < signalCandle.HighPrice.Value Then
                                ret = True
                            End If
                        ElseIf lastExecutedOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            If lastTradeExitPrice < signalCandle.HighPrice.Value AndAlso
                                lastTradeExitPrice > signalCandle.LowPrice.Value Then
                                ret = True
                            End If
                        End If
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Public Function GetMovementPoint(ByVal entryPrice As Decimal, ByVal quantity As Integer, ByVal direction As IOrder.TypeOfTransaction) As Decimal
        Dim ret As Decimal = Me.TradableInstrument.TickSize
        If direction = IOrder.TypeOfTransaction.Buy Then
            For exitPrice As Decimal = entryPrice To Decimal.MaxValue Step ret
                Dim pl As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, entryPrice, exitPrice, quantity)
                If pl >= -100 Then
                    ret = ConvertFloorCeling(exitPrice - entryPrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    Exit For
                End If
            Next
        ElseIf direction = IOrder.TypeOfTransaction.Sell Then
            For exitPrice As Decimal = entryPrice To Decimal.MinValue Step ret * -1
                Dim pl As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, exitPrice, entryPrice, quantity)
                If pl >= -100 Then
                    ret = ConvertFloorCeling(entryPrice - exitPrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                    Exit For
                End If
            Next
        End If
        Return ret
    End Function

    Private Async Function SendCandleDetails(ByVal runningCandlePayload As OHLCPayload, ByVal currentTime As Date, ByVal userSettings As PetDGandhiUserInputs) As Task
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing Then
                Dim message As String = String.Format("{0} <b>{1}</b>", runningCandlePayload.PreviousPayload.SnapshotDateTime.ToString("HH:mm:ss"), Me.TradableInstrument.TradingSymbol)
                message = String.Format("{0}{1}Open:{2}, Low:{3}, High:{4}, Close:{5}, Source:{6}",
                                        message,
                                        vbNewLine,
                                        runningCandlePayload.PreviousPayload.OpenPrice.Value,
                                        runningCandlePayload.PreviousPayload.LowPrice.Value,
                                        runningCandlePayload.PreviousPayload.HighPrice.Value,
                                        runningCandlePayload.PreviousPayload.ClosePrice.Value,
                                        runningCandlePayload.PreviousPayload.PayloadGeneratedBy.ToString)

                message = String.Format("{0}{1}-----------------------", message, vbNewLine)

                Dim conditionMatched As Boolean = True
                If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
                    runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime Then
                    'message = String.Format("{0}{1}Entry time OK. Trade Start Time:{2}, Last Trade Entry Time:{3}, Running Candle Time:{4}, Current Time:{5}",
                    '                        message, vbNewLine, userSettings.TradeStartTime.ToString, userSettings.LastTradeEntryTime.ToString,
                    '                        runningCandlePayload.SnapshotDateTime.ToString, currentTime.ToString)
                Else
                    message = String.Format("{0}{1}<b>Entry time NOT OK</b> {2}Trd Strt Tm:{3}, Lst Trd Ntry Tm:{4}, Rng Cndl Tm:{5}, Crnt Tm:{6}",
                                            message, vbNewLine, vbNewLine, userSettings.TradeStartTime.ToString, userSettings.LastTradeEntryTime.ToString,
                                            runningCandlePayload.SnapshotDateTime.ToString, currentTime.ToString)
                    conditionMatched = False
                End If

                If Me.TradableInstrument.IsHistoricalCompleted Then
                    'message = String.Format("{0}{1}Historical OK. Is Historical Completed:{2}", message, vbNewLine, Me.TradableInstrument.IsHistoricalCompleted)
                Else
                    message = String.Format("{0}{1}<b>Historical NOT OK</b> {2}Is Hstrcl Cmpltd:{3}", message, vbNewLine, vbNewLine, Me.TradableInstrument.IsHistoricalCompleted)
                    conditionMatched = False
                End If

                If Not IsActiveInstrument() Then
                    'message = String.Format("{0}{1}Active Instrument OK. Is Active Instrument:{2}", message, vbNewLine, IsActiveInstrument())
                Else
                    message = String.Format("{0}{1}<b>InActive Instrument NOT OK</b> {2}Is Actv Instrmnt:{3}", message, vbNewLine, vbNewLine, IsActiveInstrument())
                    conditionMatched = False
                End If

                If GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock Then
                    'message = String.Format("{0}{1}Number Of Trade OK. Total Trade:{2}, Max Number of Trade:{3}",
                    '                        message, vbNewLine, GetTotalExecutedOrders(), userSettings.NumberOfTradePerStock)
                Else
                    message = String.Format("{0}{1}<b>Number Of Trade NOT OK</b> {2}Total Trd:{3}, Max Nmbr of Trd:{4}",
                                            message, vbNewLine, vbNewLine, GetTotalExecutedOrders(), userSettings.NumberOfTradePerStock)
                    conditionMatched = False
                End If

                If Not Me.StrategyExitAllTriggerd AndAlso Not _exitDoneForStockMaxLoss Then
                    'message = String.Format("{0}{1}Force exit OK. Strategy Exit All Triggerd:{2}, Stock Max Loss Triggerd:{3}",
                    '                        message, vbNewLine, Me.StrategyExitAllTriggerd, _exitDoneForStockMaxLoss)
                Else
                    message = String.Format("{0}{1}<b>No Force exit NOT OK</b> {2}Strgy Ext All Trgrd:{3}, Stck Max Loss Trgrd:{4}",
                                            message, vbNewLine, vbNewLine, Me.StrategyExitAllTriggerd, _exitDoneForStockMaxLoss)
                    conditionMatched = False
                End If

                Dim lowBuffer As Decimal = CalculateBuffer(runningCandlePayload.PreviousPayload.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                Dim highBuffer As Decimal = CalculateBuffer(runningCandlePayload.PreviousPayload.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                Dim topTail As Decimal = Math.Round(((runningCandlePayload.PreviousPayload.CandleWicks.Top + lowBuffer) / runningCandlePayload.PreviousPayload.CandleRange) * 100, 2)
                Dim bottomTail As Decimal = Math.Round(((runningCandlePayload.PreviousPayload.CandleWicks.Bottom + highBuffer) / runningCandlePayload.PreviousPayload.CandleRange) * 100, 2)
                If topTail >= userSettings.PinbarTailPercentage OrElse bottomTail >= userSettings.PinbarTailPercentage Then
                    'message = String.Format("{0}{1}Pinbar OK. Top Tail:{2}%, Bottom Tail:{3}%, Minimum Pinbar Tail:{4}%",
                    '                        message, vbNewLine, topTail, bottomTail, userSettings.PinbarTailPercentage)
                Else
                    message = String.Format("{0}{1}<b>Pinbar NOT OK</b> {2}Top Tail:{3}%, Btm Tail:{4}%, Min Pinbar Tail:{5}%",
                                            message, vbNewLine, vbNewLine, topTail, bottomTail, userSettings.PinbarTailPercentage)
                    conditionMatched = False
                End If

                If topTail >= userSettings.PinbarTailPercentage Then
                    Dim maxAllowableBody As Decimal = (GetCandleATR(runningCandlePayload.PreviousPayload) + lowBuffer) * userSettings.MaxLossPerTradeMultiplier
                    Dim body As Decimal = GetCandleBody(runningCandlePayload.PreviousPayload, IOrder.TypeOfTransaction.Sell)
                    If body <= maxAllowableBody Then
                        'message = String.Format("{0}{1}Candle Body OK. Body:{2}, Max allowable body:{3}, ATR:{4}",
                        '                        message, vbNewLine, body, maxAllowableBody, GetCandleATR(runningCandlePayload.PreviousPayload))
                    Else
                        message = String.Format("{0}{1}<b>Candle Body NOT OK</b> {2}Body:{3}, Max alwbl body:{4}, ATR:{5}",
                                                message, vbNewLine, vbNewLine, body, maxAllowableBody, GetCandleATR(runningCandlePayload.PreviousPayload))
                        conditionMatched = False
                    End If
                ElseIf bottomTail >= userSettings.PinbarTailPercentage Then
                    Dim maxAllowableBody As Decimal = (GetCandleATR(runningCandlePayload.PreviousPayload) + highBuffer) * userSettings.MaxLossPerTradeMultiplier
                    Dim body As Decimal = GetCandleBody(runningCandlePayload.PreviousPayload, IOrder.TypeOfTransaction.Buy)
                    If body <= maxAllowableBody Then
                        'message = String.Format("{0}{1}Candle Body OK. Body:{2}, Max allowable body:{3}, ATR:{4}",
                        '                        message, vbNewLine, body, maxAllowableBody, GetCandleATR(runningCandlePayload.PreviousPayload))
                    Else
                        message = String.Format("{0}{1}<b>Candle Body NOT OK</b> {2}Body:{3}, Max alwbl body:{4}, ATR:{5}",
                                                message, vbNewLine, vbNewLine, body, maxAllowableBody, GetCandleATR(runningCandlePayload.PreviousPayload))
                        conditionMatched = False
                    End If
                End If

                If Not IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime) Then
                    'message = String.Format("{0}{1}Last Trade Exit OK. Is Last Trade Exited At Current Candle:{2}, Last Trade Exit Time:{3}",
                    '                        message, vbNewLine, IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime),
                    '                        GetLastOrderExitTime())
                Else
                    message = String.Format("{0}{1}<b>No Last Trade Exit NOT OK</b> {2}Is Lst Trd Extd At Crnt Cndl:{3}, Last Trd Ext Tm:{4}",
                                            message, vbNewLine, vbNewLine, IsLastTradeExitedAtCurrentCandle(runningCandlePayload.SnapshotDateTime),
                                            GetLastOrderExitTime())
                    conditionMatched = False
                End If

                'message = String.Format("{0}{1}Lst Trd Frc Ext fr Cndl Cls:{2}", message, vbNewLine, IsLastTradeForceExitForCandleClose())
                If Not conditionMatched Then
                    If message.Contains("&") Then
                        message = message.Replace("&", "_")
                    End If

                    logger.Debug(message)
                    If userSettings.TelegramAPIKey IsNot Nothing AndAlso Not userSettings.TelegramAPIKey.Trim = "" AndAlso
                    userSettings.TelegramSignalChatID IsNot Nothing AndAlso Not userSettings.TelegramSignalChatID.Trim = "" Then
                        Using tSender As New Utilities.Notification.Telegram(userSettings.TelegramAPIKey.Trim, userSettings.TelegramSignalChatID, _cts)
                            Dim encodedString As String = Utilities.Strings.EncodeURLString(message)
                            Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
                        End Using
                    End If
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
        If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
            userInputs.TelegramTradeChatID IsNot Nothing AndAlso Not userInputs.TelegramTradeChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramTradeChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
    End Function

    Private _triggerUsed As Boolean = False
    Public Async Function SendNotification() As Task
        If Not _triggerUsed Then
            _triggerUsed = True
            Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
            Try
                _cts.Token.ThrowIfCancellationRequested()
                Dim message As String = Nothing

                message = String.Format("{0}{1}{2}, Stock PL:{3}, {4}Strategy PL:{5}, {6}MaxDrawUP:{7}, MaxDrawUpTime:{8}, {9}MaxDrawDown:{10}, MaxDrawDownTime:{11}",
                                        "Pinbar Stock Max Profit reached",
                                        vbNewLine,
                                        Me.TradableInstrument.TradingSymbol,
                                        Me.GetOverallPLAfterBrokerage(),
                                        vbNewLine,
                                        Math.Round(Me.ParentStrategy.GetTotalPLAfterBrokerage, 2),
                                        vbNewLine,
                                        Math.Round(Me.ParentStrategy.MaxDrawUp, 2),
                                        Me.ParentStrategy.MaxDrawUpTime,
                                        vbNewLine,
                                        Math.Round(Me.ParentStrategy.MaxDrawDown, 2),
                                        Me.ParentStrategy.MaxDrawDownTime)

                If message.Contains("&") Then
                    message = message.Replace("&", "_")
                End If

                Dim userInputs As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
                If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
                    userInputs.TelegramMTMChatID IsNot Nothing AndAlso Not userInputs.TelegramMTMChatID.Trim = "" Then
                    Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramMTMChatID, _cts)
                        Dim encodedString As String = Utilities.Strings.EncodeString(message)
                        tSender.SendMessageGetAsync(encodedString)
                    End Using
                End If
            Catch ex As Exception
                logger.Error("Generate Trigger after mtm reached message generation error: {0}", ex.ToString)
            End Try
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
