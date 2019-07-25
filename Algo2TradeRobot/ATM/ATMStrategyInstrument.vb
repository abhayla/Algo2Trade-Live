Imports NLog
Imports System.Threading
Imports Utilities.Numbers
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Entities.Indicators
Imports Utilities.Network
Imports System.Net.Http

Public Class ATMStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastTick As ITick = Nothing
    Private _currentDayOpen As Decimal = Decimal.MinValue
    Private _usableATR As Decimal = Decimal.MinValue
    Private _longEntryAllowed As Boolean = True
    Private _shortEntryAllowed As Boolean = True
    Private ReadOnly _signalCandleTime As Date
    Private ReadOnly _levelPercentage As Decimal = 30
    Private ReadOnly _ATRMultiplier As Decimal = 1
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
                {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, ATMUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, ATMUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        _signalCandleTime = New Date(Now.Year, Now.Month, Now.Day, 9, 16, 0)
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As ATMUserInputs = Me.ParentStrategy.UserSettings
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
                'Force Cancel block strat
                If IsAnyTradeTargetReached() Then
                    Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                End If
                'Force Cancel block end
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
                'Cancel Order block start
                Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                End If
                'Cancel Order block end
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
        Dim userSettings As ATMUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim longEntryPrice As Decimal = Decimal.MinValue
        Dim shortEntryPrice As Decimal = Decimal.MinValue
        Dim buyPlaceLevel As Decimal = Decimal.MinValue
        Dim sellPlaceLevel As Decimal = Decimal.MinValue

        If currentTick Is Nothing OrElse currentTick.Timestamp Is Nothing OrElse currentTick.Timestamp.Value = Date.MinValue OrElse currentTick.Timestamp.Value = New Date(1970, 1, 1, 5, 30, 0) Then
            'Return ret
            'Exit Function
            'Do nothing
        Else
            Dim longActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Buy)
            Dim shortActiveTrades As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.Sell)

            'If _currentDayOpen = Decimal.MinValue AndAlso currentTick.LastTradeTime.Value >= userSettings.TradeStartTime AndAlso
            '    Me.TradableInstrument.IsHistoricalCompleted AndAlso runningCandlePayload IsNot Nothing AndAlso
            '    runningCandlePayload.SnapshotDateTime = _signalCandleTime Then
            '    '_currentDayOpen = currentTick.Open
            '    _currentDayOpen = runningCandlePayload.OpenPrice.Value
            '    logger.Debug("Level Price:{0}, Trading Symbol:{1}", _currentDayOpen, Me.TradableInstrument.TradingSymbol)
            '    Debug.WriteLine(String.Format("Level Price:{0}, Trading Symbol:{1}", _currentDayOpen, Me.TradableInstrument.TradingSymbol))
            'End If
            If _currentDayOpen = Decimal.MinValue AndAlso currentTick.LastTradeTime.Value >= userSettings.TradeStartTime AndAlso
                Me.TradableInstrument.IsHistoricalCompleted Then
                _currentDayOpen = currentTick.LastPrice
                logger.Debug("Level Price:{0}, Trading Symbol:{1}", _currentDayOpen, Me.TradableInstrument.TradingSymbol)
                Debug.WriteLine(String.Format("Level Price:{0}, Trading Symbol:{1}", _currentDayOpen, Me.TradableInstrument.TradingSymbol))
            End If

            If _usableATR = Decimal.MinValue AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                Dim lastDayLastCandlePayload As OHLCPayload = Nothing
                If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                    Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                                 If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                                     Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                                 Else
                                                                                                                     Return Nothing
                                                                                                                 End If
                                                                                                             End Function)

                    If XMinutePayloadConsumer IsNot Nothing AndAlso
                    XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                        Dim lastDayLastCandlePayloadTime As Date = XMinutePayloadConsumer.ConsumerPayloads.Max(Function(x)
                                                                                                                   If CType(x.Value, OHLCPayload).SnapshotDateTime.Date = Now.Date Then
                                                                                                                       Return Date.MinValue
                                                                                                                   Else
                                                                                                                       Return CType(x.Value, OHLCPayload).SnapshotDateTime
                                                                                                                   End If
                                                                                                               End Function)
                        lastDayLastCandlePayload = XMinutePayloadConsumer.ConsumerPayloads(lastDayLastCandlePayloadTime)
                    End If
                End If
                If lastDayLastCandlePayload IsNot Nothing Then
                    _usableATR = CType(atrConsumer.ConsumerPayloads(lastDayLastCandlePayload.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value
                End If
            End If

            If _usableATR <> Decimal.MinValue AndAlso _currentDayOpen <> Decimal.MinValue Then
                longEntryPrice = _currentDayOpen + ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                shortEntryPrice = _currentDayOpen - ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                buyPlaceLevel = _currentDayOpen + ConvertFloorCeling((longEntryPrice - _currentDayOpen) * _levelPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                sellPlaceLevel = _currentDayOpen - ConvertFloorCeling((_currentDayOpen - shortEntryPrice) * _levelPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
            End If

            If currentTick.LastPrice > longEntryPrice Then
                Dim lastOrder As IBusinessOrder = GetLastExecutedOrder()
                If lastOrder IsNot Nothing AndAlso lastOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    _longEntryAllowed = False
                Else
                    _longEntryAllowed = True
                End If
            ElseIf currentTick.LastPrice <= _currentDayOpen Then
                _longEntryAllowed = True
            End If
            If currentTick.LastPrice < shortEntryPrice Then
                Dim lastOrder As IBusinessOrder = GetLastExecutedOrder()
                If lastOrder IsNot Nothing AndAlso lastOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    _shortEntryAllowed = False
                Else
                    _shortEntryAllowed = True
                End If
            ElseIf currentTick.LastPrice >= _currentDayOpen Then
                _shortEntryAllowed = True
            End If

            Try
                If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                    If _lastTick Is Nothing OrElse Not currentTick.Timestamp.Value = _lastTick.Timestamp.Value OrElse forcePrint Then
                        _lastTick = currentTick
                        'logger.Debug("Place Order-> Current Tick: {0}", Utilities.Strings.JsonSerialize(currentTick))
                        logger.Debug("Place Order-> Current Tick:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, Previous Day ATR:{5}, Current Day Open:{6}, Buy Price:{7}, Sell Price:{8}, Long Entry Level:{9}, Short Entry Level:{10}, Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, Last Executed Order SL Point:{14}, Long Entry Allowed:{15}, Short Entry Allowed:{16}, Trading Symbol:{17}",
                                     String.Format("Timestamp - {0}, LTP - {1}", currentTick.Timestamp.Value.ToString, currentTick.LastPrice),
                                     userSettings.TradeStartTime.ToString,
                                     userSettings.LastTradeEntryTime.ToString,
                                     Me.TradableInstrument.IsHistoricalCompleted,
                                     IsAnyTradeTargetReached,
                                     If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                                     If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                                     If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                                     If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                                     If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                                     If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                                     GetTotalExecutedOrders(),
                                     If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                                     If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                                     If(GetLastExecutedOrderStoplossPoint() <> Decimal.MinValue, GetLastExecutedOrderStoplossPoint(), "Nothing"),
                                     _longEntryAllowed,
                                     _shortEntryAllowed,
                                     Me.TradableInstrument.TradingSymbol)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex)
            End Try

            Dim parameters1 As PlaceOrderParameters = Nothing
            Dim parameters2 As PlaceOrderParameters = Nothing
            If currentTick.Timestamp.Value >= userSettings.TradeStartTime AndAlso currentTick.Timestamp.Value <= userSettings.LastTradeEntryTime AndAlso
                runningCandlePayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
                Not IsAnyTradeTargetReached() AndAlso _usableATR <> Decimal.MinValue AndAlso _currentDayOpen <> Decimal.MinValue Then

                Dim quantity As Integer = 0
                If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
                    quantity = CalculateQuantityFromInvestment(_currentDayOpen, userSettings.InstrumentsData(Me.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.FutureMinCapital, True)
                Else
                    quantity = CalculateQuantityFromStoploss(longEntryPrice, _currentDayOpen, userSettings.CashMaxSL)
                End If
                If GetTotalExecutedOrders() = 0 Then
                    If _longEntryAllowed AndAlso (longActiveTrades Is Nothing OrElse longActiveTrades.Count = 0) Then
                        Dim triggerPrice = longEntryPrice
                        Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoploss As Decimal = triggerPrice - _currentDayOpen
                        Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        If currentTick.LastPrice < triggerPrice Then
                            parameters1 = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                       .Quantity = quantity,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss}
                        End If
                    End If
                    If _shortEntryAllowed AndAlso (shortActiveTrades Is Nothing OrElse shortActiveTrades.Count = 0) Then
                        Dim triggerPrice = shortEntryPrice
                        Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                        Dim stoploss As Decimal = _currentDayOpen - triggerPrice
                        Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        If currentTick.LastPrice > triggerPrice Then
                            parameters2 = New PlaceOrderParameters(runningCandlePayload) With
                                           {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                           .Quantity = quantity,
                                           .Price = price,
                                           .TriggerPrice = triggerPrice,
                                           .SquareOffValue = target,
                                           .StoplossValue = stoploss}
                        End If
                    End If
                Else
                    If currentTick.LastPrice > buyPlaceLevel Then
                        If _longEntryAllowed AndAlso (longActiveTrades Is Nothing OrElse longActiveTrades.Count = 0) AndAlso
                            _shortEntryAllowed AndAlso (shortActiveTrades Is Nothing OrElse shortActiveTrades.Count = 0) AndAlso
                            GetLastExecutedOrderStoplossPoint() <> Decimal.MinValue Then
                            Dim triggerPrice = longEntryPrice
                            Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                            Dim stoploss As Decimal = triggerPrice - _currentDayOpen
                            Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            If currentTick.LastPrice < triggerPrice Then
                                parameters1 = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                       .Quantity = quantity,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss}
                            End If
                            'ElseIf longActiveTrades IsNot Nothing AndAlso longActiveTrades.Count = 1 Then
                            'Dim triggerPrice = longEntryPrice
                            'Dim price As Decimal = triggerPrice + ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                            'Dim stoploss As Decimal = triggerPrice - _currentDayOpen
                            target = ConvertFloorCeling(GetLastExecutedOrderStoplossPoint() * 1.1, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            If currentTick.LastPrice < triggerPrice Then
                                parameters2 = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                       .Quantity = quantity,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss,
                                       .GenerateDifferentTag = True}
                            End If
                        End If
                    ElseIf currentTick.LastPrice < sellPlaceLevel Then
                        If _shortEntryAllowed AndAlso (shortActiveTrades Is Nothing OrElse shortActiveTrades.Count = 0) AndAlso
                            _longEntryAllowed AndAlso (longActiveTrades Is Nothing OrElse longActiveTrades.Count = 0) AndAlso
                            GetLastExecutedOrderStoplossPoint() <> Decimal.MinValue Then
                            Dim triggerPrice = shortEntryPrice
                            Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                            Dim stoploss As Decimal = _currentDayOpen - triggerPrice
                            Dim target As Decimal = ConvertFloorCeling(stoploss * userSettings.TargetMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            If currentTick.LastPrice > triggerPrice Then
                                parameters1 = New PlaceOrderParameters(runningCandlePayload) With
                                               {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                               .Quantity = quantity,
                                               .Price = price,
                                               .TriggerPrice = triggerPrice,
                                               .SquareOffValue = target,
                                               .StoplossValue = stoploss}
                            End If
                            'ElseIf shortActiveTrades IsNot Nothing AndAlso shortActiveTrades.Count = 1 Then
                            'Dim triggerPrice = shortEntryPrice
                            'Dim price As Decimal = triggerPrice - ConvertFloorCeling(triggerPrice * 0.03 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                            'Dim stoploss As Decimal = _currentDayOpen - triggerPrice
                            target = ConvertFloorCeling(GetLastExecutedOrderStoplossPoint() * 1.1, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                            If currentTick.LastPrice > triggerPrice Then
                                parameters2 = New PlaceOrderParameters(runningCandlePayload) With
                                               {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                               .Quantity = quantity,
                                               .Price = price,
                                               .TriggerPrice = triggerPrice,
                                               .SquareOffValue = target,
                                               .StoplossValue = stoploss,
                                               .GenerateDifferentTag = True}
                            End If
                        End If
                    End If
                End If
            End If

            'Below portion have to be done in every place order trigger
            If parameters1 IsNot Nothing Then
                Try
                    If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters1.ToString, Me.TradableInstrument.TradingSymbol)
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try

                Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters1.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
                If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                    Dim placedActivities As IEnumerable(Of ActivityDashboard) = currentSignalActivities.Where(Function(x)
                                                                                                                  Return x.EntryActivity.RequestRemarks = parameters1.ToString
                                                                                                              End Function)
                    If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                        Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                                   Return x.EntryActivity.RequestTime
                                                                                               End Function).LastOrDefault
                        If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                            lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                            lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters1, parameters1.ToString))
                            'Try
                            '    If forcePrint Then
                            '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                            '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                            '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                            '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                            '                    Buy Price:{7}, Sell Price:{8}, 
                            '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                            '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                            '                    Trading Symbol:{14}, Last Activity:{15}",
                            '                    currentTick.Timestamp.Value.ToString,
                            '                    userSettings.TradeStartTime.ToString,
                            '                    userSettings.LastTradeEntryTime.ToString,
                            '                    Me.TradableInstrument.IsHistoricalCompleted,
                            '                    IsAnyTradeTargetReached,
                            '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                            '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                            '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                            '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                            '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                            '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                            '                    GetTotalExecutedOrders(),
                            '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                            '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                            '                    Me.TradableInstrument.TradingSymbol,
                            '                    lastPlacedActivity.EntryActivity.RequestStatus.ToString)
                            '    End If
                            'Catch ex As Exception
                            '    logger.Error(ex)
                            'End Try
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters1, parameters1.ToString))
                        Else
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
                            'Try
                            '    If forcePrint Then
                            '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                            '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                            '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                            '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                            '                    Buy Price:{7}, Sell Price:{8}, 
                            '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                            '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                            '                    Trading Symbol:{14}, Last Activity:{15}",
                            '                    currentTick.Timestamp.Value.ToString,
                            '                    userSettings.TradeStartTime.ToString,
                            '                    userSettings.LastTradeEntryTime.ToString,
                            '                    Me.TradableInstrument.IsHistoricalCompleted,
                            '                    IsAnyTradeTargetReached,
                            '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                            '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                            '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                            '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                            '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                            '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                            '                    GetTotalExecutedOrders(),
                            '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                            '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                            '                    Me.TradableInstrument.TradingSymbol,
                            '                    lastPlacedActivity.EntryActivity.RequestStatus.ToString)
                            '    End If
                            'Catch ex As Exception
                            '    logger.Error(ex)
                            'End Try
                        End If
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
                        'Try
                        '    If forcePrint Then
                        '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                        '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                        '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                        '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                        '                    Buy Price:{7}, Sell Price:{8}, 
                        '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                        '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                        '                    Trading Symbol:{14}, Last Activity:{15}",
                        '                    currentTick.Timestamp.Value.ToString,
                        '                    userSettings.TradeStartTime.ToString,
                        '                    userSettings.LastTradeEntryTime.ToString,
                        '                    Me.TradableInstrument.IsHistoricalCompleted,
                        '                    IsAnyTradeTargetReached,
                        '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                        '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                        '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                        '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                        '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                        '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                        '                    GetTotalExecutedOrders(),
                        '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                        '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                        '                    Me.TradableInstrument.TradingSymbol,
                        '                    "Nothing")
                        '    End If
                        'Catch ex As Exception
                        '    logger.Error(ex)
                        'End Try
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters1, parameters1.ToString))
                    'Try
                    '    If forcePrint Then
                    '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                    '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                    '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                    '                    Buy Price:{7}, Sell Price:{8}, 
                    '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                    '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                    '                    Trading Symbol:{14}, Last Activity:{15}",
                    '                    currentTick.Timestamp.Value.ToString,
                    '                    userSettings.TradeStartTime.ToString,
                    '                    userSettings.LastTradeEntryTime.ToString,
                    '                    Me.TradableInstrument.IsHistoricalCompleted,
                    '                    IsAnyTradeTargetReached,
                    '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                    '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                    '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                    '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                    '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                    '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                    '                    GetTotalExecutedOrders(),
                    '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                    '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                    '                    Me.TradableInstrument.TradingSymbol,
                    '                    "Nothing")
                    '    End If
                    'Catch ex As Exception
                    '    logger.Error(ex)
                    'End Try
                End If
            End If
            If parameters2 IsNot Nothing Then
                Try
                    If forcePrint Then logger.Debug("***** Place Order Parameter ***** {0}, {1}", parameters2.ToString, Me.TradableInstrument.TradingSymbol)
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try

                Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters2.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
                If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                    Dim placedActivities As IEnumerable(Of ActivityDashboard) = currentSignalActivities.Where(Function(x)
                                                                                                                  Return x.EntryActivity.RequestRemarks = parameters2.ToString
                                                                                                              End Function)
                    If placedActivities IsNot Nothing AndAlso placedActivities.Count > 0 Then
                        Dim lastPlacedActivity As ActivityDashboard = placedActivities.OrderBy(Function(x)
                                                                                                   Return x.EntryActivity.RequestTime
                                                                                               End Function).LastOrDefault
                        If lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                            lastPlacedActivity.EntryActivity.LastException IsNot Nothing AndAlso
                            lastPlacedActivity.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters2, parameters2.ToString))
                            'Try
                            '    If forcePrint Then
                            '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                            '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                            '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                            '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                            '                    Buy Price:{7}, Sell Price:{8}, 
                            '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                            '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                            '                    Trading Symbol:{14}, Last Activity:{15}",
                            '                    currentTick.Timestamp.Value.ToString,
                            '                    userSettings.TradeStartTime.ToString,
                            '                    userSettings.LastTradeEntryTime.ToString,
                            '                    Me.TradableInstrument.IsHistoricalCompleted,
                            '                    IsAnyTradeTargetReached,
                            '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                            '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                            '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                            '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                            '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                            '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                            '                    GetTotalExecutedOrders(),
                            '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                            '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                            '                    Me.TradableInstrument.TradingSymbol,
                            '                    lastPlacedActivity.EntryActivity.RequestStatus.ToString)
                            '    End If
                            'Catch ex As Exception
                            '    logger.Error(ex)
                            'End Try
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                        ElseIf lastPlacedActivity.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, parameters2, parameters2.ToString))
                        Else
                            If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                            ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
                            'Try
                            '    If forcePrint Then
                            '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                            '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                            '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                            '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                            '                    Buy Price:{7}, Sell Price:{8}, 
                            '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                            '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                            '                    Trading Symbol:{14}, Last Activity:{15}",
                            '                    currentTick.Timestamp.Value.ToString,
                            '                    userSettings.TradeStartTime.ToString,
                            '                    userSettings.LastTradeEntryTime.ToString,
                            '                    Me.TradableInstrument.IsHistoricalCompleted,
                            '                    IsAnyTradeTargetReached,
                            '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                            '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                            '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                            '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                            '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                            '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                            '                    GetTotalExecutedOrders(),
                            '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                            '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                            '                    Me.TradableInstrument.TradingSymbol,
                            '                    lastPlacedActivity.EntryActivity.RequestStatus.ToString)
                            '    End If
                            'Catch ex As Exception
                            '    logger.Error(ex)
                            'End Try
                        End If
                    Else
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
                        'Try
                        '    If forcePrint Then
                        '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                        '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                        '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                        '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                        '                    Buy Price:{7}, Sell Price:{8}, 
                        '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                        '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                        '                    Trading Symbol:{14}, Last Activity:{15}",
                        '                    currentTick.Timestamp.Value.ToString,
                        '                    userSettings.TradeStartTime.ToString,
                        '                    userSettings.LastTradeEntryTime.ToString,
                        '                    Me.TradableInstrument.IsHistoricalCompleted,
                        '                    IsAnyTradeTargetReached,
                        '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                        '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                        '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                        '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                        '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                        '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                        '                    GetTotalExecutedOrders(),
                        '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                        '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                        '                    Me.TradableInstrument.TradingSymbol,
                        '                    "Nothing")
                        '    End If
                        'Catch ex As Exception
                        '    logger.Error(ex)
                        'End Try
                    End If
                Else
                    If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
                    ret.Add(New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters2, parameters2.ToString))
                    'Try
                    '    If forcePrint Then
                    '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    '        logger.Debug("Place Order-> Current Tick Timestamp:{0}, Trade Start Time:{1}, Last Trade Entry Time:{2}, 
                    '                    Is Historial Completed:{3}, Is Any Trade Target Reached:{4}, 
                    '                    Previous Day ATR:{5}, Current Day Open:{6}, 
                    '                    Buy Price:{7}, Sell Price:{8}, 
                    '                    Long Entry Level:{9}, Short Entry Level:{10}, 
                    '                    Total Executed Order:{11}, Long Active Trades:{12}, Short Active Trades:{13}, 
                    '                    Trading Symbol:{14}, Last Activity:{15}",
                    '                    currentTick.Timestamp.Value.ToString,
                    '                    userSettings.TradeStartTime.ToString,
                    '                    userSettings.LastTradeEntryTime.ToString,
                    '                    Me.TradableInstrument.IsHistoricalCompleted,
                    '                    IsAnyTradeTargetReached,
                    '                    If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                    '                    If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                    '                    If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                    '                    If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                    '                    If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                    '                    If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                    '                    GetTotalExecutedOrders(),
                    '                    If(longActiveTrades Is Nothing, "Nothing", longActiveTrades.Count),
                    '                    If(shortActiveTrades Is Nothing, "Nothing", shortActiveTrades.Count),
                    '                    Me.TradableInstrument.TradingSymbol,
                    '                    "Nothing")
                    '    End If
                    'Catch ex As Exception
                    '    logger.Error(ex)
                    'End Try
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
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso _currentDayOpen <> Decimal.MinValue Then
            For Each parentOrderId In OrderDetails.Keys
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso
                    parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    Dim triggerPrice As Decimal = _currentDayOpen
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
        Dim currentTick As ITick = Me.TradableInstrument.LastTick
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 AndAlso GetTotalExecutedOrders() > 0 AndAlso
            _currentDayOpen <> Decimal.MinValue AndAlso _usableATR <> Decimal.MinValue Then
            Dim longEntryPrice As Decimal = _currentDayOpen + ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
            Dim shortEntryPrice As Decimal = _currentDayOpen - ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing)
            Dim buyPlaceLevel As Decimal = _currentDayOpen + ConvertFloorCeling((longEntryPrice - _currentDayOpen) * _levelPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
            Dim sellPlaceLevel As Decimal = _currentDayOpen - ConvertFloorCeling((_currentDayOpen - shortEntryPrice) * _levelPercentage / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)

            Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                              Return x.ParentOrderIdentifier Is Nothing AndAlso
                                                                              x.Status = IOrder.TypeOfStatus.TriggerPending
                                                                          End Function)
            If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                For Each parentOrder In parentOrders
                    Dim parentBussinessOrder As IBusinessOrder = OrderDetails(parentOrder.OrderIdentifier)
                    Dim cancelThisOrder As Boolean = False
                    If parentBussinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.TriggerPending Then
                        If currentTick.LastPrice > buyPlaceLevel AndAlso parentBussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            cancelThisOrder = True
                        ElseIf currentTick.LastPrice < sellPlaceLevel AndAlso parentBussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            cancelThisOrder = True
                        End If
                    End If
                    If cancelThisOrder Then
                        'Below portion have to be done in every cancel order trigger
                        Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                        If currentSignalActivities IsNot Nothing Then
                            If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                                currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                                currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                Continue For
                            End If
                        End If
                        If forcePrint Then
                            logger.Debug("****Cancel Order****-> Current Tick Timestamp:{0}, LTP:{1}, Previous Day ATR:{2}, Current Day Open:{3}, Buy Price:{4}, Sell Price:{5}, Long Entry Level:{6}, Short Entry Level:{7}, Total Executed Order:{8}, Parent Order ID:{9}, Parent Order Direction:{10}, Trading Symbol:{11}",
                                         currentTick.Timestamp.Value.ToString,
                                         currentTick.LastPrice,
                                         If(_usableATR <> Decimal.MinValue, _usableATR, "Nothing"),
                                         If(_currentDayOpen <> Decimal.MinValue, _currentDayOpen, "Nothing"),
                                         If(longEntryPrice <> Decimal.MinValue, longEntryPrice, "Nothing"),
                                         If(shortEntryPrice <> Decimal.MinValue, shortEntryPrice, "Nothing"),
                                         If(buyPlaceLevel <> Decimal.MinValue, buyPlaceLevel, "Nothing"),
                                         If(sellPlaceLevel <> Decimal.MinValue, sellPlaceLevel, "Nothing"),
                                         GetTotalExecutedOrders(),
                                         parentBussinessOrder.ParentOrder.OrderIdentifier,
                                         parentBussinessOrder.ParentOrder.RawTransactionType,
                                         Me.TradableInstrument.TradingSymbol)
                        End If
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentBussinessOrder.ParentOrder, "LTP Near to opposite direction"))
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

            Dim exitOrderResponse As IBusinessOrder = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function

    Private Function IsAnyTradeTargetReached() As Boolean
        Dim ret As Boolean = False
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrder In OrderDetails.Keys
                Dim bussinessOrder As IBusinessOrder = OrderDetails(parentOrder)
                If bussinessOrder.AllOrder IsNot Nothing AndAlso bussinessOrder.AllOrder.Count > 0 Then
                    For Each order In bussinessOrder.AllOrder
                        If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
                            Dim target As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = order.AveragePrice - bussinessOrder.ParentOrder.AveragePrice
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - order.AveragePrice
                            End If
                            If _usableATR <> Decimal.MinValue AndAlso
                                target >= ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing) * (Me.ParentStrategy.UserSettings.TargetMultiplier - 1) Then
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

    Private Function GetLastExecutedStoplossOrder() As IBusinessOrder
        Dim ret As IBusinessOrder = Nothing
        If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            For Each parentOrder In OrderDetails.OrderByDescending(Function(x)
                                                                       Return x.Value.ParentOrder.TimeStamp
                                                                   End Function)
                Dim bussinessOrder As IBusinessOrder = OrderDetails(parentOrder.Key)
                If bussinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
                    bussinessOrder.AllOrder IsNot Nothing AndAlso bussinessOrder.AllOrder.Count > 0 Then
                    For Each order In bussinessOrder.AllOrder
                        If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Cancelled Then
                            Dim target As Decimal = 0
                            If bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                target = order.Price - bussinessOrder.ParentOrder.AveragePrice
                            ElseIf bussinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                target = bussinessOrder.ParentOrder.AveragePrice - order.Price
                            End If
                            If _usableATR <> Decimal.MinValue AndAlso
                                target >= ConvertFloorCeling(_usableATR * _ATRMultiplier, Me.TradableInstrument.TickSize, RoundOfType.Celing) * (Me.ParentStrategy.UserSettings.TargetMultiplier - 1) Then
                                ret = bussinessOrder
                                Exit For
                            End If
                        End If
                    Next
                End If
                If ret IsNot Nothing Then Exit For
            Next
        End If
        Return ret
    End Function

    Private Function GetLastExecutedOrderStoplossPoint() As Decimal
        Dim ret As Decimal = Decimal.MinValue
        Dim order As IBusinessOrder = GetLastExecutedStoplossOrder()
        If order IsNot Nothing AndAlso order.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso
            order.AllOrder IsNot Nothing AndAlso order.AllOrder.Count > 0 Then
            For Each runningOrder In order.AllOrder
                If runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss AndAlso
                    runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                    If order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                        ret = order.ParentOrder.AveragePrice - runningOrder.AveragePrice
                        Exit For
                    ElseIf order.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                        ret = runningOrder.AveragePrice - order.ParentOrder.AveragePrice
                        Exit For
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
