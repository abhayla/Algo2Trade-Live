Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class NearFarHedgingStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

    Private lastPrevPayloadPlaceOrder As String = ""
    'Private ReadOnly _apiKey As String = "700121864:AAHjes45V0kEPBDLIfnZzsatH5NhRwIjciw"
    'Private ReadOnly _chaitId As String = "-337360611"
    Public ReadOnly DummySpreadRatioConsumer As SpreadRatioConsumer
    Public ReadOnly DummySpreadBollingerConsumer As BollingerConsumer
    Public ReadOnly DummyRatioBollingerConsumer As BollingerConsumer
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
            If Me.ParentStrategy.UserSettings.SignalTimeFrame > 0 AndAlso Not Me.IsPairInstrument Then
                Dim chartConsumer As PayloadToChartConsumer = New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                RawPayloadDependentConsumers.Add(chartConsumer)
            ElseIf Me.ParentStrategy.UserSettings.SignalTimeFrame > 0 AndAlso Me.IsPairInstrument Then
                Dim pairConsumer As PayloadToPairConsumer = New PayloadToPairConsumer()
                pairConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim spreadRatioData As SpreadRatioConsumer = New SpreadRatioConsumer(pairConsumer, TypeOfField.Close)
                spreadRatioData.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim spreadBollinger As BollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Spread)
                Dim ratioBollinger As BollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Ratio)
                spreadRatioData.OnwardLevelConsumers.Add(spreadBollinger)
                spreadRatioData.OnwardLevelConsumers.Add(ratioBollinger)
                pairConsumer.OnwardLevelConsumers.Add(spreadRatioData)
                RawPayloadDependentConsumers.Add(pairConsumer)
                DummySpreadRatioConsumer = New SpreadRatioConsumer(pairConsumer, TypeOfField.Close)
                DummySpreadBollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Spread)
                DummyRatioBollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Ratio)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            If Me.IsPairInstrument Then
                Dim lastTriggerTime As Date = Date.MinValue
                Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
                While True
                    If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                        Throw Me.ParentStrategy.ParentController.OrphanException
                    End If

                    _cts.Token.ThrowIfCancellationRequested()
                    Dim placeOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False, Nothing).ConfigureAwait(False)
                    If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Count > 0 Then
                        For Each runningPlaceOrderTrigger In placeOrderTrigger
                            runningPlaceOrderTrigger.Item2.MonitorAsync(StrategyInstrument.ExecuteCommands.PlaceCOMarketMISOrder, runningPlaceOrderTrigger)
                        Next
                        'Dim tasks = placeOrderTrigger.Select(Async Function(x)
                        '                                         Await x.Item2.MonitorAsync(StrategyInstrument.ExecuteCommands.PlaceCOMarketMISOrder, x).ConfigureAwait(False)
                        '                                         Return True
                        '                                     End Function)
                        'Await Task.WhenAll(tasks).ConfigureAwait(False)
                    End If

                    _cts.Token.ThrowIfCancellationRequested()

                    Dim cancelOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False, Nothing).ConfigureAwait(False)
                    If cancelOrderTrigger IsNot Nothing AndAlso cancelOrderTrigger.Count > 0 Then
                        For Each runningCancelOrderTrigger In cancelOrderTrigger
                            runningCancelOrderTrigger.Item2.MonitorAsync(StrategyInstrument.ExecuteCommands.CancelCOOrder, runningCancelOrderTrigger)
                        Next
                        'Dim tasks = cancelOrderTrigger.Select(Async Function(x)
                        '                                          Await x.Item2.MonitorAsync(StrategyInstrument.ExecuteCommands.CancelCOOrder, x).ConfigureAwait(False)
                        '                                          Return True
                        '                                      End Function)
                        'Await Task.WhenAll(tasks).ConfigureAwait(False)
                    End If

                    _cts.Token.ThrowIfCancellationRequested()

                    If Me.GetPairPLAfterBrokerage >= hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).MaxPairGain Then
                        If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count > 0 Then
                            For Each runningParentStrategyInstrument In Me.ParentStrategyInstruments
                                Await runningParentStrategyInstrument.ForceExitAllTradesAsync("MTM Reached").ConfigureAwait(False)
                            Next
                        End If
                    End If

                    _cts.Token.ThrowIfCancellationRequested()
                    Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                End While
                _cts.Token.ThrowIfCancellationRequested()
            End If
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function
    Public Overrides Async Function MonitorAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Try
            If Not Me.IsPairInstrument Then
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If

                _cts.Token.ThrowIfCancellationRequested()

                Select Case command
                    Case ExecuteCommands.PlaceCOMarketMISOrder
                        Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String) = data
                        If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take AndAlso
                            (Not Me.IsActiveInstrument OrElse (Me.IsActiveInstrument AndAlso Me.PairStrategyCancellationRequest)) Then
                            Dim placeOrderData As IBusinessOrder = Await TakePaperTradeAsync(placeOrderTrigger).ConfigureAwait(False)
                            If placeOrderData IsNot Nothing AndAlso placeOrderData.ParentOrder IsNot Nothing Then
                                _cts.Token.ThrowIfCancellationRequested()
                                GenerateTelegramMessageAsync(String.Format("Order placed. {0}{1},{2}Trading Symbol:{3}, Direction:{4}, Entry Price:{5}, Quantity:{6},{7}Timestamp:{8}",
                                                                    placeOrderTrigger.Item4,
                                                                    "",
                                                                    vbNewLine,
                                                                    placeOrderData.ParentOrder.Tradingsymbol,
                                                                    placeOrderData.ParentOrder.TransactionType.ToString,
                                                                    placeOrderData.ParentOrder.AveragePrice,
                                                                    placeOrderData.ParentOrder.Quantity,
                                                                    vbNewLine,
                                                                    Now.ToString))
                            End If
                        End If

                    Case ExecuteCommands.CancelCOOrder
                        Dim cancelOrderTrigger As Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String) = data
                        If cancelOrderTrigger IsNot Nothing AndAlso cancelOrderTrigger.Item1 = ExecuteCommandAction.Take Then

                            Dim cancelOrderData As IBusinessOrder = Await Me.CancelPaperTradeAsync(cancelOrderTrigger).ConfigureAwait(False)

                            If cancelOrderData IsNot Nothing AndAlso cancelOrderData.ParentOrder IsNot Nothing AndAlso
                                    cancelOrderData.AllOrder IsNot Nothing AndAlso cancelOrderData.AllOrder.Count > 0 Then
                                For Each runningSLOrder In cancelOrderData.AllOrder
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Dim buyPrice As Decimal = Decimal.MinValue
                                    Dim sellPrice As Decimal = Decimal.MinValue
                                    If cancelOrderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                        buyPrice = cancelOrderData.ParentOrder.AveragePrice
                                        sellPrice = runningSLOrder.AveragePrice
                                    ElseIf cancelOrderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                        sellPrice = cancelOrderData.ParentOrder.AveragePrice
                                        buyPrice = runningSLOrder.AveragePrice
                                    End If
                                    _cts.Token.ThrowIfCancellationRequested()
                                    GenerateTelegramMessageAsync(String.Format("Order exited. Trading Symbol:{0}, Direction:{1}, Entry Price:{2}, Exit Price:{3}, Quantity:{4},{5}PL: {6},{7}Timestamp:{8}",
                                                                                   runningSLOrder.Tradingsymbol,
                                                                                   cancelOrderData.ParentOrder.TransactionType.ToString,
                                                                                   cancelOrderData.ParentOrder.AveragePrice,
                                                                                   runningSLOrder.AveragePrice,
                                                                                   runningSLOrder.Quantity,
                                                                                   vbNewLine,
                                                                                   _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, runningSLOrder.Quantity),
                                                                                   vbNewLine,
                                                                                   Now.ToString))
                                Next
                            End If
                        End If

                End Select


                _cts.Token.ThrowIfCancellationRequested()
                'Exit Order

                _cts.Token.ThrowIfCancellationRequested()
            End If
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean, ByVal data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = Nothing
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
        Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(TypeOfExchage.NSE)
        Dim potentialSignalData As Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload) = GetCurrentSignal(False)
        Dim runningCandlePayload As PairPayload = Nothing
        Dim currentCandlePayload As OHLCPayload = Nothing
        If potentialSignalData IsNot Nothing Then
            runningCandlePayload = potentialSignalData.Item3
            currentCandlePayload = potentialSignalData.Item4
        End If

        Try
            If Me.IsPairInstrument AndAlso runningCandlePayload IsNot Nothing AndAlso
                runningCandlePayload.Instrument1Payload IsNot Nothing AndAlso runningCandlePayload.Instrument2Payload IsNot Nothing AndAlso
                runningCandlePayload.Instrument1Payload.PreviousPayload IsNot Nothing AndAlso runningCandlePayload.Instrument2Payload.PreviousPayload IsNot Nothing AndAlso
                currentCandlePayload IsNot Nothing AndAlso currentCandlePayload.PreviousPayload IsNot Nothing Then
                If Not currentCandlePayload.PreviousPayload.ToString = lastPrevPayloadPlaceOrder Then
                    lastPrevPayloadPlaceOrder = runningCandlePayload.Instrument1Payload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:Intrument1:{0}", runningCandlePayload.Instrument1Payload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:Intrument2:{0}", runningCandlePayload.Instrument2Payload.PreviousPayload.ToString)
                    Dim potentialSignalDataForLog As Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload) = GetCurrentSignal(True)
                    logger.Debug("PlaceOrder Parameters-> Trade Start Time:{0}, Last Trade Entry Time:{1}, Running Candle Time: {2}, IsActiveInstrument:{3}, PairConsumerProtection:{4}, UseBothSignal:{5}, IsSignalReceived:{6}, MTM:{7}, Pair PL:{8}",
                                 hedgingUserInputs.TradeStartTime.ToString,
                                 hedgingUserInputs.LastTradeEntryTime.ToString,
                                 currentCandlePayload.SnapshotDateTime.ToString,
                                 IsLogicalActiveInstrument,
                                 PairConsumerProtection,
                                 hedgingUserInputs.UseBothSignal,
                                 If(potentialSignalDataForLog Is Nothing, "False", potentialSignalDataForLog.Item1),
                                 hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).MaxPairGain,
                                 GetPairPLAfterBrokerage)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim virtualInstrument As IInstrument = Nothing
        If Me.IsPairInstrument Then
            virtualInstrument = Me.TradableInstrument
        Else
            virtualInstrument = Me.DependendStrategyInstruments.FirstOrDefault.TradableInstrument
        End If

        Dim parameters As PlaceOrderParameters = Nothing
        If Now >= hedgingUserInputs.TradeStartTime AndAlso Now <= hedgingUserInputs.LastTradeEntryTime AndAlso runningCandlePayload IsNot Nothing AndAlso
            (runningCandlePayload.Instrument1Payload IsNot Nothing OrElse runningCandlePayload.Instrument2Payload IsNot Nothing) AndAlso
            Not IsLogicalActiveInstrument() AndAlso Me.IsPairInstrument AndAlso currentCandlePayload IsNot Nothing AndAlso
            Me.GetPairPLAfterBrokerage < hedgingUserInputs.InstrumentsData(virtualInstrument.TradingSymbol).MaxPairGain Then
            'Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(hedgingUserInputs.MaxLossPercentagePerDay) * -1 / 100 Then
            'Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(hedgingUserInputs.MaxProfitPercentagePerDay) / 100 AndAlso

            If currentCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso currentCandlePayload.PreviousPayload IsNot Nothing Then
                If potentialSignalData IsNot Nothing AndAlso potentialSignalData.Item1 Then
                    Dim triggerPrice As Decimal = Decimal.MinValue
                    Dim quantity As Integer = 0

                    If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count = 2 Then
                        For Each runningParentStrategyInstrument In Me.ParentStrategyInstruments
                            If Not runningParentStrategyInstrument.IsActiveInstrument OrElse (runningParentStrategyInstrument.IsActiveInstrument AndAlso runningParentStrategyInstrument.PairStrategyCancellationRequest) Then
                                Dim pair1StrategyInstrument As NearFarHedgingStrategyInstrument = Me.ParentStrategyInstruments.FirstOrDefault
                                Dim pair2StrategyInstrument As NearFarHedgingStrategyInstrument = Me.ParentStrategyInstruments.LastOrDefault
                                Dim higherContract As NearFarHedgingStrategyInstrument = Nothing
                                Dim lowerContract As NearFarHedgingStrategyInstrument = Nothing
                                If pair1StrategyInstrument.TradableInstrument.LastTick.LastPrice > pair2StrategyInstrument.TradableInstrument.LastTick.LastPrice Then
                                    higherContract = pair1StrategyInstrument
                                    lowerContract = pair2StrategyInstrument
                                Else
                                    higherContract = pair2StrategyInstrument
                                    lowerContract = pair1StrategyInstrument
                                End If

                                If runningParentStrategyInstrument.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                                    Dim myPair As NearFarHedgingStrategyInstrument = GetAnotherPairStrategyInstrument()
                                    If myPair IsNot Nothing Then
                                        quantity = Math.Floor(runningParentStrategyInstrument.TradableInstrument.LastTick.LastPrice * myPair.TradableInstrument.LastTick.LastPrice)
                                    Else
                                        quantity = Math.Floor(runningParentStrategyInstrument.TradableInstrument.LastTick.LastPrice)
                                    End If
                                ElseIf runningParentStrategyInstrument.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures Then
                                    quantity = runningParentStrategyInstrument.TradableInstrument.LotSize
                                End If

                                If hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).Pair1TradingSymbol = runningParentStrategyInstrument.TradableInstrument.TradingSymbol Then
                                    quantity = quantity * hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).Pair1Quantity
                                Else
                                    quantity = quantity * hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).Pair2Quantity
                                End If
                                triggerPrice = 0

                                If higherContract.TradableInstrument.TradingSymbol = runningParentStrategyInstrument.TradableInstrument.TradingSymbol Then
                                    parameters = New PlaceOrderParameters(currentCandlePayload.PreviousPayload) With
                                                         {
                                                           .EntryDirection = potentialSignalData.Item2,
                                                           .Quantity = Math.Floor(quantity),
                                                           .TriggerPrice = triggerPrice
                                                         }
                                ElseIf lowerContract.TradableInstrument.TradingSymbol = runningParentStrategyInstrument.TradableInstrument.TradingSymbol Then
                                    parameters = New PlaceOrderParameters(currentCandlePayload.PreviousPayload) With
                                                         {
                                                           .EntryDirection = If(potentialSignalData.Item2 = IOrder.TypeOfTransaction.Buy, IOrder.TypeOfTransaction.Sell, IOrder.TypeOfTransaction.Buy),
                                                           .Quantity = Math.Floor(quantity),
                                                           .TriggerPrice = triggerPrice
                                                         }
                                End If
                                If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String))
                                ret.Add(New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(ExecuteCommandAction.Take, runningParentStrategyInstrument, parameters, String.Format("Signal Time:{0}, Signal Direction:{1}", currentCandlePayload.PreviousPayload.SnapshotDateTime.ToString, potentialSignalData.Item2.ToString)))
                            End If
                        Next
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Throw New NotImplementedException
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)) = Nothing
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
        Dim potentialSignalData As Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload) = GetCurrentSignal(False)
        Dim runningCandlePayload As PairPayload = Nothing
        Dim currentCandlePayload As OHLCPayload = Nothing
        If potentialSignalData IsNot Nothing Then
            runningCandlePayload = potentialSignalData.Item3
            currentCandlePayload = potentialSignalData.Item4
        End If

        If Me.IsPairInstrument AndAlso Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count > 0 AndAlso
            hedgingUserInputs.InstrumentsData(Me.TradableInstrument.TradingSymbol).ReverseSignalExit Then
            For Each runningParentStrategyInstrument In Me.ParentStrategyInstruments
                Dim allActiveOrders As List(Of IOrder) = runningParentStrategyInstrument.GetAllActiveOrders(IOrder.TypeOfTransaction.None)
                If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 AndAlso runningParentStrategyInstrument.TradableInstrument.IsHistoricalCompleted Then
                    Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                                      Return x.ParentOrderIdentifier Is Nothing
                                                                                  End Function)
                    If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                        For Each parentOrder In parentOrders
                            Dim parentBusinessOrder As IBusinessOrder = runningParentStrategyInstrument.OrderDetails(parentOrder.OrderIdentifier)
                            If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                                For Each slOrder In parentBusinessOrder.SLOrder
                                    Dim tradeWillExit As Boolean = False
                                    If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                                    Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                                    Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                                        If potentialSignalData IsNot Nothing AndAlso potentialSignalData.Item1 Then
                                            Dim isHigherContract As Boolean = False
                                            If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count = 2 Then
                                                Dim pair1StrategyInstrument As NearFarHedgingStrategyInstrument = Me.ParentStrategyInstruments.FirstOrDefault
                                                Dim pair2StrategyInstrument As NearFarHedgingStrategyInstrument = Me.ParentStrategyInstruments.LastOrDefault
                                                Dim higherContract As NearFarHedgingStrategyInstrument = Nothing
                                                If pair1StrategyInstrument.TradableInstrument.LastTick.LastPrice > pair2StrategyInstrument.TradableInstrument.LastTick.LastPrice Then
                                                    higherContract = pair1StrategyInstrument
                                                Else
                                                    higherContract = pair2StrategyInstrument
                                                End If
                                                If runningParentStrategyInstrument.TradableInstrument.TradingSymbol = higherContract.TradableInstrument.TradingSymbol Then
                                                    isHigherContract = True
                                                Else
                                                    isHigherContract = False
                                                End If
                                            End If

                                            If isHigherContract Then
                                                If potentialSignalData.Item2 = slOrder.TransactionType Then
                                                    tradeWillExit = True
                                                End If
                                            Else
                                                If potentialSignalData.Item2 <> slOrder.TransactionType Then
                                                    tradeWillExit = True
                                                End If
                                            End If
                                        End If
                                    End If
                                    If tradeWillExit Then
                                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String))
                                        ret.Add(New Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)(ExecuteCommandAction.Take, runningParentStrategyInstrument, slOrder, "Opposite Direction signal"))
                                    End If
                                Next
                            End If
                        Next
                    End If
                End If
            Next
        End If
        Return ret
    End Function

    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete AndAlso
            Not order.Status = IOrder.TypeOfStatus.Cancelled AndAlso
            Not order.Status = IOrder.TypeOfStatus.Rejected Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }

            Dim orderData As IBusinessOrder = Await ForceCancelPaperTradeAsync(cancellableOrder).ConfigureAwait(False)
            If orderData IsNot Nothing AndAlso orderData.ParentOrder IsNot Nothing AndAlso
               orderData.AllOrder IsNot Nothing AndAlso orderData.AllOrder.Count > 0 Then
                For Each runningSLOrder In orderData.AllOrder
                    Dim buyPrice As Decimal = Decimal.MinValue
                    Dim sellPrice As Decimal = Decimal.MinValue
                    If orderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        buyPrice = orderData.ParentOrder.AveragePrice
                        sellPrice = runningSLOrder.AveragePrice
                    ElseIf orderData.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        sellPrice = orderData.ParentOrder.AveragePrice
                        buyPrice = runningSLOrder.AveragePrice
                    End If
                    GenerateTelegramMessageAsync(String.Format("{9} Order exited. Trading Symbol:{0}, Direction:{1}, Entry Price:{2}, Exit Price:{3}, Quantity:{4},{5}PL: {6},{7}Timestamp:{8}",
                                                               runningSLOrder.Tradingsymbol,
                                                               orderData.ParentOrder.TransactionType.ToString,
                                                               orderData.ParentOrder.AveragePrice,
                                                               runningSLOrder.AveragePrice,
                                                               runningSLOrder.Quantity,
                                                               vbNewLine,
                                                               _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, runningSLOrder.Quantity),
                                                               vbNewLine,
                                                               Now.ToString,
                                                               reason))
                Next
            End If

            'Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelCOOrder, cancellableOrder).ConfigureAwait(False)
            'If exitOrderResponse IsNot Nothing Then
            '    GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, reason))
            'End If
        End If
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
        If hedgingUserInputs.TelegramAPIKey IsNot Nothing AndAlso Not hedgingUserInputs.TelegramAPIKey.Trim = "" AndAlso
            hedgingUserInputs.TelegramChatID IsNot Nothing AndAlso Not hedgingUserInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(hedgingUserInputs.TelegramAPIKey.Trim, hedgingUserInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                'logger.Debug("Encoded String:{0}", encodedString)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
        End If
    End Function

    Private Function CheckSignal(ByVal spreadRatioData As SpreadRatioConsumer.SpreadRatioPayload,
                                 ByVal spreadBollingerData As BollingerConsumer.BollingerPayload,
                                 ByVal ratioBollingerData As BollingerConsumer.BollingerPayload,
                                 ByVal forcePrint As Boolean) As Tuple(Of Boolean, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
        If spreadRatioData IsNot Nothing AndAlso spreadBollingerData IsNot Nothing AndAlso ratioBollingerData IsNot Nothing Then
            If forcePrint Then
                logger.Debug("{0}, {1}, {2}", spreadRatioData.ToString, spreadBollingerData.ToString, ratioBollingerData.ToString)
            End If
            Dim spreadSignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
            Dim ratioSignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
            If spreadRatioData.Spread IsNot Nothing AndAlso
                spreadBollingerData.HighBollinger IsNot Nothing AndAlso
                spreadBollingerData.LowBollinger IsNot Nothing Then
                If spreadRatioData.Spread.Value > spreadBollingerData.HighBollinger.Value Then
                    'logger.Debug("Spread Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Spread.Value, spreadBollingerData.HighBollinger.Value, spreadBollingerData.LowBollinger.Value)
                    spreadSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Sell)
                ElseIf spreadRatioData.Spread.Value < spreadBollingerData.LowBollinger.Value Then
                    'logger.Debug("Spread Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Spread.Value, spreadBollingerData.HighBollinger.Value, spreadBollingerData.LowBollinger.Value)
                    spreadSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Buy)
                End If
            End If
            If spreadRatioData.Ratio IsNot Nothing AndAlso
                ratioBollingerData.HighBollinger IsNot Nothing AndAlso
                ratioBollingerData.LowBollinger IsNot Nothing Then
                If spreadRatioData.Ratio.Value > ratioBollingerData.HighBollinger.Value Then
                    'logger.Debug("Ratio Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Ratio.Value, ratioBollingerData.HighBollinger.Value, ratioBollingerData.LowBollinger.Value)
                    ratioSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Sell)
                ElseIf spreadRatioData.Ratio.Value < ratioBollingerData.LowBollinger.Value Then
                    'logger.Debug("Ratio Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Ratio.Value, ratioBollingerData.HighBollinger.Value, ratioBollingerData.LowBollinger.Value)
                    ratioSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Buy)
                End If
            End If
            If hedgingUserInputs.UseBothSignal Then
                If spreadSignal IsNot Nothing AndAlso ratioSignal IsNot Nothing Then
                    If spreadSignal.Item1 AndAlso ratioSignal.Item1 Then
                        If spreadSignal.Item2 = ratioSignal.Item2 Then
                            ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, spreadSignal.Item2)
                        End If
                    End If
                End If
            Else
                If spreadSignal IsNot Nothing AndAlso spreadSignal.Item1 Then ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, spreadSignal.Item2)
                If ratioSignal IsNot Nothing AndAlso ratioSignal.Item1 Then ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, ratioSignal.Item2)
            End If
        End If
        If forcePrint Then
            If ret IsNot Nothing Then
                logger.Debug("Signal Type:{0}, Is Signal Received:{1}, Signal Direction:{2}", If(hedgingUserInputs.UseBothSignal, "Both", "Anyone"), ret.Item1, ret.Item2.ToString)
            Else
                logger.Debug("Signal Type:{0}, Is Signal Received:False, Signal Direction:None", If(hedgingUserInputs.UseBothSignal, "Both", "Anyone"))
            End If
        End If
        Return ret
    End Function

    Private Function GetCurrentSignal(ByVal forcePrint As Boolean) As Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload)
        Dim ret As Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload) = Nothing
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings

        Dim runningCandlePayload As PairPayload = Nothing
        Dim spreadRatioConsumer As SpreadRatioConsumer = Nothing
        Dim spreadBollingerConsumer As BollingerConsumer = Nothing
        Dim ratioBollingerConsumer As BollingerConsumer = Nothing

        If Me.IsPairInstrument Then
            runningCandlePayload = GetXMinuteCurrentCandle()
            spreadRatioConsumer = GetConsumer(Me.RawPayloadDependentConsumers, DummySpreadRatioConsumer)
            spreadBollingerConsumer = GetConsumer(Me.RawPayloadDependentConsumers, DummySpreadBollingerConsumer)
            ratioBollingerConsumer = GetConsumer(Me.RawPayloadDependentConsumers, DummyRatioBollingerConsumer)
        Else
            If Me.DependendStrategyInstruments IsNot Nothing AndAlso Me.DependendStrategyInstruments.Count > 0 Then
                Dim virtualStrategyInstrument As NearFarHedgingStrategyInstrument = Me.DependendStrategyInstruments.FirstOrDefault
                runningCandlePayload = virtualStrategyInstrument.GetXMinuteCurrentCandle()
                spreadRatioConsumer = virtualStrategyInstrument.GetConsumer(virtualStrategyInstrument.RawPayloadDependentConsumers, virtualStrategyInstrument.DummySpreadRatioConsumer)
                spreadBollingerConsumer = virtualStrategyInstrument.GetConsumer(virtualStrategyInstrument.RawPayloadDependentConsumers, virtualStrategyInstrument.DummySpreadBollingerConsumer)
                ratioBollingerConsumer = virtualStrategyInstrument.GetConsumer(virtualStrategyInstrument.RawPayloadDependentConsumers, virtualStrategyInstrument.DummyRatioBollingerConsumer)
            End If
        End If

        If runningCandlePayload IsNot Nothing Then
            Dim currentCandle As OHLCPayload = Nothing
            If runningCandlePayload.Instrument1Payload IsNot Nothing AndAlso
                                    runningCandlePayload.Instrument1Payload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
                                    runningCandlePayload.Instrument1Payload.PreviousPayload IsNot Nothing AndAlso
                                    runningCandlePayload.Instrument1Payload.SnapshotDateTime >= hedgingUserInputs.TradeStartTime Then
                currentCandle = runningCandlePayload.Instrument1Payload
            ElseIf runningCandlePayload.Instrument2Payload IsNot Nothing AndAlso
                                    runningCandlePayload.Instrument2Payload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
                                    runningCandlePayload.Instrument2Payload.PreviousPayload IsNot Nothing AndAlso
                                    runningCandlePayload.Instrument2Payload.SnapshotDateTime >= hedgingUserInputs.TradeStartTime Then
                currentCandle = runningCandlePayload.Instrument2Payload
            End If
            If currentCandle IsNot Nothing AndAlso currentCandle.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso currentCandle.PreviousPayload IsNot Nothing AndAlso
                spreadRatioConsumer.ConsumerPayloads IsNot Nothing AndAlso spreadRatioConsumer.ConsumerPayloads.Count > 0 AndAlso
                spreadBollingerConsumer.ConsumerPayloads IsNot Nothing AndAlso spreadBollingerConsumer.ConsumerPayloads.Count > 0 AndAlso
                ratioBollingerConsumer.ConsumerPayloads IsNot Nothing AndAlso ratioBollingerConsumer.ConsumerPayloads.Count > 0 AndAlso
                spreadRatioConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) AndAlso
                spreadBollingerConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) AndAlso
                ratioBollingerConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) Then
                Dim signalCandleTime As Date = currentCandle.PreviousPayload.SnapshotDateTime
                Dim potentialSignalData As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
                potentialSignalData = CheckSignal(spreadRatioConsumer.ConsumerPayloads(signalCandleTime),
                                              spreadBollingerConsumer.ConsumerPayloads(signalCandleTime),
                                              ratioBollingerConsumer.ConsumerPayloads(signalCandleTime), forcePrint)

                If potentialSignalData IsNot Nothing Then
                    ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload)(potentialSignalData.Item1, potentialSignalData.Item2, runningCandlePayload, currentCandle)
                Else
                    ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction, PairPayload, OHLCPayload)(False, IOrder.TypeOfTransaction.None, runningCandlePayload, currentCandle)
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsLogicalActiveInstrument() As Boolean
        Dim ret As Boolean = False
        If Me.IsPairInstrument Then
            If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count >= 2 Then
                ret = True
                For Each runningParentStrategyInstrumentStrategy In Me.ParentStrategyInstruments
                    ret = ret And runningParentStrategyInstrumentStrategy.IsActiveInstrument
                Next
            End If
        End If
        Return ret
    End Function

    Private Function GetAnotherPairStrategyInstrument() As NearFarHedgingStrategyInstrument
        Dim ret As NearFarHedgingStrategyInstrument = Nothing
        If Not Me.IsPairInstrument Then
            If Me.DependendStrategyInstruments IsNot Nothing AndAlso Me.DependendStrategyInstruments.Count > 0 Then
                Dim virtualStrategyInstrument As NearFarHedgingStrategyInstrument = Me.DependendStrategyInstruments.FirstOrDefault
                If virtualStrategyInstrument.IsPairInstrument Then
                    If virtualStrategyInstrument.ParentStrategyInstruments IsNot Nothing AndAlso
                        virtualStrategyInstrument.ParentStrategyInstruments.Count > 0 Then
                        For Each runningStrategyInstrument In virtualStrategyInstrument.ParentStrategyInstruments
                            If Me.TradableInstrument.TradingSymbol <> runningStrategyInstrument.TradableInstrument.TradingSymbol Then
                                ret = runningStrategyInstrument
                                Exit For
                            End If
                        Next
                    End If
                End If
            End If
        End If
        Return ret
    End Function
    Public Function GetPairPLAfterBrokerage() As Decimal
        Dim ret As Decimal = 0
        If Me.IsPairInstrument Then
            If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count > 0 Then
                For Each runningParentStrategyInstrument In Me.ParentStrategyInstruments
                    ret += runningParentStrategyInstrument.GetOverallPLAfterBrokerage
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
