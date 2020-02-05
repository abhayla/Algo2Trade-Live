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
    Private _strategyInstrumentExit As Boolean = False
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
        'If Me.ParentStrategy.IsStrategyCandleStickBased Then
        If Me.ParentStrategy.UserSettings.SignalTimeFrame > 0 Then
            Dim chartConsumer As PayloadToChartConsumer = New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame)
            chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                {New FractalConsumer(chartConsumer)}
            RawPayloadDependentConsumers.Add(chartConsumer)
            _dummyFractalConsumer = New FractalConsumer(chartConsumer)
        Else
            Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
        End If
        'End If
    End Sub

    Public Overrides Function HandleTickTriggerToUIETCAsync() As Task
        If Me.ParentStrategy.GetTotalPLAfterBrokerage <= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxLossPerDay Then
            Me.StrategyExitAllTriggerd = True
            ForceExitAllTradesAsync("Max Loss reached")
        ElseIf Me.ParentStrategy.GetTotalPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).MaxProfitPerDay Then
            Me.StrategyExitAllTriggerd = True
            ForceExitAllTradesAsync("Max Profit reached")
        ElseIf Me.GetOverallPLAfterBrokerage <= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).StockMaxLossPerDay Then
            _strategyInstrumentExit = True
            ForceExitAllTradesAsync("Instrument Max Loss reached")
        ElseIf Me.GetOverallPLAfterBrokerage >= CType(Me.ParentStrategy.UserSettings, PetDGandhiUserInputs).StockMaxProfitPerDay Then
            _strategyInstrumentExit = True
            ForceExitAllTradesAsync("Instrument Max Profit reached")
        End If
        Return MyBase.HandleTickTriggerToUIETCAsync()
    End Function

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As PetDGandhiUserInputs = Me.ParentStrategy.UserSettings
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
                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                End If
                'Place Order block end
                _cts.Token.ThrowIfCancellationRequested()
                'Exit Order block start
                Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                End If
                'Exit Order block end
                '_cts.Token.ThrowIfCancellationRequested()
                ''Force Exit Start
                'If GetTotalExecutedOrders() >= userSettings.NumberOfTradePerStock Then
                '    Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
                '    If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
                '        Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                '                                                                          Return x.ParentOrderIdentifier Is Nothing
                '                                                                      End Function)
                '        If parentOrders IsNot Nothing AndAlso parentOrders.Count = 1 Then
                '            Await ForceExitAllTradesAsync("Another two trades exited").ConfigureAwait(False)
                '        End If
                '    End If
                'End If
                ''Force Exit End
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
        Dim fractal As FractalConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyFractalConsumer)

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                (Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint) Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Previous Candle:{5}, Is Active Instrument:{6}, Number Of Trade:{7}, OverAll PL:{8}, Stock PL:{9}, Strategy Exit All Triggerd:{10}, Strategy Instrument Exit:{11}, Is Any Trade Target Reached:{12}, Time:{13},{14}, Time:{15},{16}, Current Time:{17}, Current LTP:{18}, TradingSymbol:{19}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToString,
                            IsActiveInstrument(),
                            GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            Me.GetOverallPLAfterBrokerage(),
                            Me.StrategyExitAllTriggerd,
                            _strategyInstrumentExit,
                            IsAnyTradeTargetReached(),
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToString("dd-MM-yyyy HH:mm:ss"),
                            fractal.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                            runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime.ToString("dd-MM-yyyy HH:mm:ss"),
                            fractal.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime).ToString,
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameter As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Me.GetOverallPLAfterBrokerage() > userSettings.StockMaxLossPerDay AndAlso Me.GetOverallPLAfterBrokerage() < userSettings.StockMaxProfitPerDay AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() > userSettings.MaxLossPerDay AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso
            Not Me.StrategyExitAllTriggerd AndAlso Not _strategyInstrumentExit AndAlso Not IsAnyTradeTargetReached() Then

            Dim buyActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Buy)
            Dim sellActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Sell)

            If buyActiveTrades Is Nothing OrElse buyActiveTrades.Count = 0 Then
                Dim signal As Tuple(Of Boolean, Decimal) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick, IOrder.TypeOfTransaction.Buy, forcePrint)
                If signal IsNot Nothing AndAlso signal.Item1 Then
                    Dim buffer As Decimal = CalculateBuffer(signal.Item2, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim triggerPrice As Decimal = signal.Item2 + buffer
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    'Dim stoplossPrice As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value - buffer
                    Dim stoplossPrice As Decimal = CType(fractal.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalLow.Value - buffer
                    If ((triggerPrice - stoplossPrice) / stoplossPrice) * 100 > 1 Then
                        stoplossPrice = ConvertFloorCeling(triggerPrice - triggerPrice * 1 / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    End If
                    Dim quantity As Integer = CalculateQuantityFromStoploss(triggerPrice, stoplossPrice, userSettings.MaxLossPerTrade)
                    Dim targetPrice As Decimal = CalculateTargetFromPL(triggerPrice, quantity, userSettings.MaxProfitPerTrade)

                    If currentTick.LastPrice < triggerPrice Then
                        parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = triggerPrice - stoplossPrice,
                                     .SquareOffValue = targetPrice - triggerPrice,
                                     .Quantity = quantity}
                    End If
                End If
            End If
            If sellActiveTrades Is Nothing OrElse sellActiveTrades.Count = 0 Then
                Dim signal As Tuple(Of Boolean, Decimal) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick, IOrder.TypeOfTransaction.Sell, forcePrint)
                If signal IsNot Nothing AndAlso signal.Item1 Then
                    Dim buffer As Decimal = CalculateBuffer(signal.Item2, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    Dim triggerPrice As Decimal = signal.Item2 - buffer
                    Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    'Dim stoplossPrice As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value + buffer
                    Dim stoplossPrice As Decimal = CType(fractal.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime), FractalConsumer.FractalPayload).FractalHigh.Value + buffer
                    If ((stoplossPrice - triggerPrice) / triggerPrice) * 100 > 1 Then
                        stoplossPrice = ConvertFloorCeling(triggerPrice + triggerPrice * 1 / 100, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    End If
                    Dim quantity As Integer = CalculateQuantityFromStoploss(stoplossPrice, triggerPrice, userSettings.MaxLossPerTrade)
                    Dim targetPrice As Decimal = CalculateTargetFromPL(triggerPrice, quantity, userSettings.MaxProfitPerTrade)

                    If currentTick.LastPrice > triggerPrice Then
                        parameter = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoplossPrice - triggerPrice,
                                     .SquareOffValue = targetPrice - triggerPrice,
                                     .Quantity = quantity}
                    End If
                End If
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
                        If currentTime >= lastPlacedActivity.EntryActivity.RequestTime.AddSeconds(Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay) Then
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameter, parameter.ToString))
                        Else
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameter, parameter.ToString))
                        End If
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

    Protected Overrides Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException
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
                Dim currentTick As ITick = Me.TradableInstrument.LastTick
                Dim runningCandle As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    If runningCandle IsNot Nothing AndAlso runningCandle.PreviousPayload IsNot Nothing AndAlso runningCandle.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick Then
                        Dim signalCandle As OHLCPayload = GetSignalCandleOfAnOrder(parentOrder.OrderIdentifier, Me.ParentStrategy.UserSettings.SignalTimeFrame)
                        If signalCandle IsNot Nothing Then
                            Dim signal As Tuple(Of Boolean, Decimal) = GetSignalCandle(runningCandle.PreviousPayload, currentTick, parentOrder.TransactionType, forcePrint)
                            If signal IsNot Nothing Then
                                If signalCandle.SnapshotDateTime <> runningCandle.PreviousPayload.SnapshotDateTime Then
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
                                    ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "Invalid Signal"))
                                End If
                            End If
                        End If
                    End If
                    If ret Is Nothing AndAlso IsAnyTradeTargetReached() Then
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
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "One Trade Target Reached"))
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

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick, ByVal direction As IOrder.TypeOfTransaction, ByVal forcePrint As Boolean) As Tuple(Of Boolean, Decimal)
        Dim ret As Tuple(Of Boolean, Decimal) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing Then
            'If candle.HighPrice.Value < candle.PreviousPayload.HighPrice.Value AndAlso
            '    candle.LowPrice.Value > candle.PreviousPayload.LowPrice.Value Then
            '    If currentTick.Open > currentTick.Close AndAlso currentTick.Low > currentTick.Close Then
            '        If direction = IOrder.TypeOfTransaction.Buy Then direction = IOrder.TypeOfTransaction.None
            '    ElseIf currentTick.Open < currentTick.Close AndAlso currentTick.High < currentTick.Close Then
            '        If direction = IOrder.TypeOfTransaction.Sell Then direction = IOrder.TypeOfTransaction.None
            '    End If
            'End If
            'If direction = IOrder.TypeOfTransaction.Buy Then
            '    If candle.HighPrice.Value < candle.PreviousPayload.HighPrice.Value Then
            '        ret = New Tuple(Of Boolean, Decimal)(True, candle.HighPrice.Value)
            '    End If
            'ElseIf direction = IOrder.TypeOfTransaction.Sell Then
            '    If candle.LowPrice.Value > candle.PreviousPayload.LowPrice.Value Then
            '        ret = New Tuple(Of Boolean, Decimal)(True, candle.LowPrice.Value)
            '    End If
            'End If

            Dim fractal As FractalConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyFractalConsumer)
            Dim candleFractalPayload As FractalConsumer.FractalPayload = fractal.ConsumerPayloads(candle.SnapshotDateTime)
            Dim previousCandleFractalPayload As FractalConsumer.FractalPayload = fractal.ConsumerPayloads(candle.PreviousPayload.SnapshotDateTime)
            If direction = IOrder.TypeOfTransaction.Buy Then
                If candleFractalPayload.FractalHigh.Value < previousCandleFractalPayload.FractalHigh.Value Then
                    ret = New Tuple(Of Boolean, Decimal)(True, candleFractalPayload.FractalHigh.Value)
                End If
            ElseIf direction = IOrder.TypeOfTransaction.Sell Then
                If candleFractalPayload.FractalLow.Value > previousCandleFractalPayload.FractalLow.Value Then
                    ret = New Tuple(Of Boolean, Decimal)(True, candleFractalPayload.FractalLow.Value)
                End If
            End If

            If ret IsNot Nothing AndAlso forcePrint Then
                Try
                    logger.Debug("GetSignalCandle-> Direction:{0}, Time:{1},{2}, Time:{3},{4}",
                                 direction.ToString,
                                 candle.SnapshotDateTime,
                                 candleFractalPayload.ToString,
                                 candle.PreviousPayload.SnapshotDateTime,
                                 previousCandleFractalPayload.ToString)
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
            End If
        End If
        Return ret
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
