Imports NLog
Imports System.Threading
Imports Utilities.Numbers
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Entities.Indicators

Public Class LowSLStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public EligibleToTakeTrade As Boolean
    Public StopStrategyInstrument As Boolean
    Public VolumeChangePercentage As Decimal
    Public Quantity As Integer
    Public SLPoint As Decimal
    Public DayATR As Decimal

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _potentialHighEntryPrice As Decimal = Decimal.MinValue
    Private _potentialLowEntryPrice As Decimal = Decimal.MinValue
    Private _signalCandle As OHLCPayload = Nothing
    Private _entryChanged As Boolean = False
    Private _targetPoint As Decimal = Decimal.MinValue

    Private _mpLock As Integer
    Private _mlLock As Integer
    Private _smpLock As Integer
    Private _smlLock As Integer
    Private _mdfyLock As Integer

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
                    CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).CashInstrument Then
                    chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                    {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).ATRPeriod)}
                End If
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        Me.EligibleToTakeTrade = False
        Me.VolumeChangePercentage = Decimal.MinValue
        Me.StopStrategyInstrument = False
        If CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).InstrumentsData.ContainsKey(Me.TradableInstrument.TradingSymbol) Then
            Me.DayATR = CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).InstrumentsData(Me.TradableInstrument.TradingSymbol).DayATR
            Me.Quantity = CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).InstrumentsData(Me.TradableInstrument.TradingSymbol).Quantity
            Me.SLPoint = CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).InstrumentsData(Me.TradableInstrument.TradingSymbol).SLPoint
        Else
            Me.DayATR = 0
            Me.Quantity = 0
            Me.SLPoint = 0
        End If
        If Not CType(Me.ParentStrategy.UserSettings, LowSLUserInputs).AutoSelectStock Then
            Me.EligibleToTakeTrade = True
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
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

    Public Overrides Function HandleTickTriggerToUIETCAsync() As Task
        TickTrigger()
        Return MyBase.HandleTickTriggerToUIETCAsync()
    End Function

    Public Async Function TickTrigger() As Task
        Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
        If Me.ParentStrategy.GetTotalPLAfterBrokerage < Math.Abs(userSettings.MaxLossPerDay) * -1 Then
            Try
                While 1 = Interlocked.Exchange(_mlLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                    logger.Warn("Unable to take action for lock. Max loss reached.")
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                Await ForceExitAllTradesAsync("Max Loss reached").ConfigureAwait(False)
                Me.StrategyExitAllTriggerd = True
            Catch ex As Exception
                logger.Error("TickTrigger:{0}, error:{1}", Me.ToString, ex.ToString)
            Finally
                Interlocked.Exchange(_mlLock, 0)
            End Try
        End If
        If Me.ParentStrategy.GetTotalPLAfterBrokerage > userSettings.MaxProfitPerDay Then
            Try
                While 1 = Interlocked.Exchange(_mpLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                    logger.Warn("Unable to take action for lock. Max profit reached.")
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                Await ForceExitAllTradesAsync("Max Profit reached").ConfigureAwait(False)
                Me.StrategyExitAllTriggerd = True
            Catch ex As Exception
                logger.Error("TickTrigger:{0}, error:{1}", Me.ToString, ex.ToString)
            Finally
                Interlocked.Exchange(_mpLock, 0)
            End Try
        End If
        If Me.GetOverallPLAfterBrokerage < Math.Abs(userSettings.StockMaxLossPerDay) * -1 Then
            Try
                While 1 = Interlocked.Exchange(_smlLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                    logger.Warn("Unable to take action for lock. Stock max loss reached.")
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                Await ForceExitAllTradesAsync("Stock Max Loss reached").ConfigureAwait(False)
            Catch ex As Exception
                logger.Error("TickTrigger:{0}, error:{1}", Me.ToString, ex.ToString)
            Finally
                Interlocked.Exchange(_smlLock, 0)
            End Try
        End If
        If Me.GetOverallPLAfterBrokerage > userSettings.StockMaxProfitPerDay Then
            Try
                While 1 = Interlocked.Exchange(_smpLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                    logger.Warn("Unable to take action for lock. Stock max profit reached.")
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                Await ForceExitAllTradesAsync("Stock Max Profit reached").ConfigureAwait(False)
            Catch ex As Exception
                logger.Error("TickTrigger:{0}, error:{1}", Me.ToString, ex.ToString)
            Finally
                Interlocked.Exchange(_smpLock, 0)
            End Try
        End If
        'Modify Order block start
        Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
        If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
            Try
                While 1 = Interlocked.Exchange(_mdfyLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                    logger.Warn("Unable to take action for lock. Target protection movement.")
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

                Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
            Catch ex As Exception
                logger.Error("TickTrigger:{0}, error:{1}", Me.ToString, ex.ToString)
            Finally
                Interlocked.Exchange(_mdfyLock, 0)
            End Try
        End If
        'Modify Order block end
    End Function

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim currentTime As Date = Now()

        If Not _entryChanged AndAlso _signalCandle IsNot Nothing Then
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                Dim firstOrder As IBusinessOrder = Nothing
                For Each runningOrder In OrderDetails.OrderBy(Function(x)
                                                                  Return x.Value.ParentOrder.TimeStamp
                                                              End Function)
                    If runningOrder.Value.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                        firstOrder = runningOrder.Value
                        Exit For
                    End If
                Next
                If firstOrder IsNot Nothing AndAlso firstOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                    If firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        _potentialLowEntryPrice = _potentialHighEntryPrice - 2 * Me.SLPoint
                    ElseIf firstOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        _potentialHighEntryPrice = _potentialLowEntryPrice + 2 * Me.SLPoint
                    End If
                    _entryChanged = True
                End If
            End If
        End If

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso
                Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder OrElse forcePrint Then
                _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                If _signalCandle IsNot Nothing Then
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Signal Candle Time:{5}, Signal Candle Range:{6}, Signal Candle Source:{7}, {8}, Is Active Instrument:{9}, Number Of Trade:{10}, OverAll PL:{11}, Stock PL:{12}, Is Target Reached:{13}, Buy Entry:{14}, Sell Entry:{15}, Current Time:{16}, Current LTP:{17}, TradingSymbol:{18}",
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
                            Me.GetOverallPLAfterBrokerage(),
                            IsAnyTradeTargetReached(),
                            _potentialHighEntryPrice,
                            _potentialLowEntryPrice,
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
                Else
                    logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Current Candle Time:{5}, Current Candle Range:{6}, Current Candle Source:{7}, {8}, {9}, Is Active Instrument:{10}, Number Of Trade:{11}, OverAll PL:{12}, Stock PL:{13}, Is Target Reached:{14}, Current Time:{15}, Current LTP:{16}, TradingSymbol:{17}",
                            userSettings.TradeStartTime.ToString,
                            userSettings.LastTradeEntryTime.ToString,
                            runningCandlePayload.SnapshotDateTime.ToString,
                            runningCandlePayload.PayloadGeneratedBy.ToString,
                            Me.TradableInstrument.IsHistoricalCompleted,
                            runningCandlePayload.PreviousPayload.SnapshotDateTime.ToShortTimeString,
                            runningCandlePayload.PreviousPayload.CandleRange,
                            runningCandlePayload.PreviousPayload.PayloadGeneratedBy.ToString,
                            atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.PreviousPayload.SnapshotDateTime).ToString,
                            atrConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                            IsActiveInstrument(),
                            Me.GetTotalExecutedOrders(),
                            Me.ParentStrategy.GetTotalPLAfterBrokerage(),
                            Me.GetOverallPLAfterBrokerage(),
                            IsAnyTradeTargetReached(),
                            currentTime.ToString,
                            currentTick.LastPrice,
                            Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
            Not IsActiveInstrument() AndAlso GetTotalExecutedOrders() < userSettings.NumberOfTradePerStock AndAlso
            Not IsAnyTradeTargetReached() AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage() > Math.Abs(userSettings.MaxLossPerDay) * -1 AndAlso
            Me.ParentStrategy.GetTotalPLAfterBrokerage() < userSettings.MaxProfitPerDay AndAlso Not Me.StrategyExitAllTriggerd AndAlso
            Me.GetOverallPLAfterBrokerage() > Math.Abs(userSettings.StockMaxLossPerDay) * -1 AndAlso Me.GetOverallPLAfterBrokerage < userSettings.StockMaxProfitPerDay Then
            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, currentTick)
            If signal IsNot Nothing AndAlso signal.Item1 Then
                If signal.Item4 = IOrder.TypeOfTransaction.Buy Then
                    Dim triggerPrice As Decimal = signal.Item2
                    Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3
                    Dim stoploss As Decimal = ConvertFloorCeling(triggerPrice - stoplossPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(_targetPoint + GetExtraAdjustableTargetPoint(), Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                    If currentTick.LastPrice < triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = Me.Quantity}
                    End If
                ElseIf signal.Item4 = IOrder.TypeOfTransaction.Sell Then
                    Dim triggerPrice As Decimal = signal.Item2
                    Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                    Dim stoplossPrice As Decimal = signal.Item3
                    Dim stoploss As Decimal = ConvertFloorCeling(stoplossPrice - triggerPrice, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                    Dim target As Decimal = ConvertFloorCeling(_targetPoint + GetExtraAdjustableTargetPoint(), Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)

                    If currentTick.LastPrice > triggerPrice Then
                        parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                     .TriggerPrice = triggerPrice,
                                     .Price = price,
                                     .StoplossValue = stoploss,
                                     .SquareOffValue = target,
                                     .Quantity = Me.Quantity}
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

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each runningOrderID In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(runningOrderID)
                If bussinessOrder.SLOrder IsNot Nothing AndAlso bussinessOrder.SLOrder.Count > 0 Then
                    Dim orderPL As Decimal = GetTotalPLOfAnOrderAfterBrokerage(runningOrderID)
                    Dim stockPrice As Decimal = bussinessOrder.ParentOrder.AveragePrice
                    Dim expectedLossFromOneOrder As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, stockPrice, stockPrice - Me.SLPoint, Me.Quantity)
                    Dim expectedTargetPL As Decimal = Math.Abs(expectedLossFromOneOrder) * (userSettings.TargetMultiplier + 1)
                    Dim requiredPL As Decimal = Math.Abs(expectedLossFromOneOrder) * userSettings.TargetMultiplier
                    Dim target As Decimal = CalculateTargetFromPL(stockPrice, bussinessOrder.ParentOrder.Quantity, requiredPL)
                    Dim targetPoint As Decimal = target - stockPrice

                    For Each slOrder In bussinessOrder.SLOrder
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso
                            Not slOrder.Status = IOrder.TypeOfStatus.Rejected Then
                            Dim triggerPrice As Decimal = Decimal.MinValue
                            If orderPL >= expectedTargetPL Then
                                If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    triggerPrice = bussinessOrder.ParentOrder.AveragePrice + targetPoint
                                ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    triggerPrice = bussinessOrder.ParentOrder.AveragePrice - targetPoint
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
                                ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, slOrder, triggerPrice, "Target Protection"))
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

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(Me.ParentStrategy.UserSettings.SignalTimeFrame)
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
                        If Not orderCancelled AndAlso _signalCandle IsNot Nothing Then
                            Dim signal As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = GetSignalCandle(runningCandlePayload.PreviousPayload, Me.TradableInstrument.LastTick)
                            If signal IsNot Nothing Then
                                If signal.Item2 <> parentOrder.TriggerPrice Then
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
            Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
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
                        Dim lastCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, userSettings.TradeStartTime.Hour, userSettings.TradeStartTime.Minute - 1, 0)
                        If runningPayload.Date = Now.Date Then
                            If runningPayload <= lastCandle Then
                                currentDayVolumeSum += CType(XMinutePayloadConsumer.ConsumerPayloads(runningPayload), OHLCPayload).Volume.Value
                            End If
                        ElseIf runningPayload.Date < Now.Date Then
                            If runningPayload <= lastCandle Then
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

    Private Function GetSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing AndAlso
            Not candle.DeadCandle AndAlso Not candle.PreviousPayload.DeadCandle Then
            Dim userSettings As LowSLUserInputs = Me.ParentStrategy.UserSettings
            If Not _entryChanged Then
                If IsDipInATR(candle) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value + CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    _potentialLowEntryPrice = candle.LowPrice.Value - CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                    _signalCandle = candle

                    Dim atr As Decimal = GetSignalCandleATR()
                    Dim pl As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, _potentialHighEntryPrice, _potentialHighEntryPrice - (Me.SLPoint + Me.TradableInstrument.TickSize), Quantity)
                    Dim target As Decimal = CalculateTargetFromPL(_potentialHighEntryPrice, Me.Quantity, Math.Abs(pl) * userSettings.TargetMultiplier)
                    If ConvertFloorCeling(atr * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing) >= target - _potentialHighEntryPrice Then
                        _targetPoint = ConvertFloorCeling(atr * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        If _targetPoint > Me.DayATR / 2 Then
                            _potentialHighEntryPrice = Decimal.MinValue
                            _potentialLowEntryPrice = Decimal.MinValue
                            _signalCandle = Nothing
                        End If
                    Else
                        _targetPoint = target - _potentialHighEntryPrice
                    End If
                End If
            End If

            If _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
                If _entryChanged Then
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.LastPrice >= middlePoint + range * 40 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialHighEntryPrice - Me.SLPoint, IOrder.TypeOfTransaction.Buy)
                    ElseIf currentTick.LastPrice <= middlePoint - range * 40 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialLowEntryPrice + Me.SLPoint, IOrder.TypeOfTransaction.Sell)
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
                    If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialHighEntryPrice - Me.SLPoint, IOrder.TypeOfTransaction.Buy)
                    ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialLowEntryPrice + Me.SLPoint, IOrder.TypeOfTransaction.Sell)
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsDipInATR(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing Then
            If GetCandleATR(candle) <= GetCandleATR(candle.PreviousPayload) * 0.99 Then
                ret = True
            End If
        End If
        Return ret
    End Function

    Private Function GetSignalCandleATR() As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If _signalCandle IsNot Nothing Then
            ret = GetCandleATR(_signalCandle)
        End If
        Return ret
    End Function

    Private Function GetCandleATR(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If candle IsNot Nothing Then
            Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
            If atrConsumer IsNot Nothing AndAlso atrConsumer.ConsumerPayloads IsNot Nothing AndAlso atrConsumer.ConsumerPayloads.Count > 0 AndAlso
                atrConsumer.ConsumerPayloads.ContainsKey(candle.SnapshotDateTime) Then
                ret = CType(atrConsumer.ConsumerPayloads(candle.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value
            End If
        End If
        Return ret
    End Function

    Private Function GetExtraAdjustableTargetPoint() As Decimal
        Dim ret As Decimal = 0
        Dim stockPrice As Decimal = Me.TradableInstrument.LastTick.LastPrice
        Dim expectedLossFromOneOrder As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, stockPrice, stockPrice - Me.SLPoint, Me.Quantity)
        If GetTotalExecutedOrders() <> 0 Then
            Dim totalExpectedLoss As Decimal = expectedLossFromOneOrder * GetTotalExecutedOrders()
            Dim totalLoss As Decimal = GetOverallPLAfterBrokerage()
            Dim extraLoss As Decimal = Math.Round(totalLoss, 2) - Math.Round(totalExpectedLoss, 2)
            If Math.Round(extraLoss, 2) <> 0 Then
                Dim targetForExtraLossMakeup As Decimal = CalculateTargetFromPL(stockPrice, Me.Quantity, Math.Abs(extraLoss))
                Dim targetPointForExtraLossMakeup As Decimal = targetForExtraLossMakeup - stockPrice
                If _targetPoint + targetPointForExtraLossMakeup < (_targetPoint / 4) * 5 Then
                    ret = targetPointForExtraLossMakeup
                End If
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
