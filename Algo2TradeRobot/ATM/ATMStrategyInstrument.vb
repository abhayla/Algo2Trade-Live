Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class ATMStrategyInstrument
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
                RawPayloadDependentConsumers.Add(chartConsumer)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As ATMUserInputs = Me.ParentStrategy.UserSettings
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                'Place Order block start
                'Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                'If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                '    'Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularMarketCNCOrder, Nothing).ConfigureAwait(False)
                '    'If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                '    '    placeOrderResponse("data").ContainsKey("order_id") Then

                '    'End If
                'End If
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
        Dim userSettings As ATMUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTime As Date = Now()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
                    _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    'logger.Debug("PlaceOrder-> Rest all parameters: Trade Start Time:{0}, Last Trade Entry Time:{1}, RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, Max Stock Profit:{5}, Max Stock Loss:{6}, Stock PL:{7}, Max Profit Per Day:{8}, Max Loss Per Day:{9}, Overall PL:{10}, Is Active Instrument:{11}, Reverse Trade:{12}, Top Wick %:{13}, Bottom Wick %:{14}, Current Time:{15}, Current LTP:{16}, TradingSymbol:{17}",
                    'userSettings.TradeStartTime.ToString,
                    'userSettings.LastTradeEntryTime.ToString,
                    'runningCandlePayload.SnapshotDateTime.ToString,
                    'runningCandlePayload.PayloadGeneratedBy.ToString,
                    'Me.TradableInstrument.IsHistoricalCompleted,
                    'maxStockProfit,
                    'maxStockLoss,
                    'Me.GetOverallPLAfterBrokerage(),
                    'userSettings.MaxProfitPerDay,
                    'userSettings.MaxLossPerDay,
                    'Me.ParentStrategy.GetTotalPLAfterBrokerage,
                    'IsActiveInstrument(),
                    'reverseTrade,
                    '(runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                    '(runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                    'currentTime.ToString,
                    'currentLTP,
                    'Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        'If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
        '    runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
        '    runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
        '    runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted AndAlso
        '    Me.GetOverallPLAfterBrokerage() < maxStockProfit AndAlso Me.GetOverallPLAfterBrokerage() > Math.Abs(maxStockLoss) * -1 AndAlso
        '    Me.ParentStrategy.GetTotalPLAfterBrokerage < userSettings.MaxProfitPerDay AndAlso Me.ParentStrategy.GetTotalPLAfterBrokerage > Math.Abs(userSettings.MaxLossPerDay) * -1 Then
        '    If reverseTrade OrElse Not IsActiveInstrument() Then
        '        Dim higherTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).HigherTailPercentage
        '        Dim lowerTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowerTailPercentage
        '        If (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
        '            (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then
        '            Dim potentialEntryPrice As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value
        '            potentialEntryPrice += CalculateBuffer(potentialEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
        '            Dim stoploss As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value
        '            stoploss -= CalculateBuffer(stoploss, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
        '            If (potentialEntryPrice - stoploss) >= minStoplossPoint AndAlso (potentialEntryPrice - stoploss) <= maxStoplossPoint Then
        '                If currentLTP >= potentialEntryPrice Then
        '                    parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
        '                            {.EntryDirection = IOrder.TypeOfTransaction.Buy,
        '                             .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
        '                             .TriggerPrice = stoploss,
        '                             .Supporting = New List(Of Object)}

        '                    parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
        '                    parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)
        '                End If
        '            Else
        '                Try
        '                    If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadInnerPlaceOrder Then
        '                        _lastPrevPayloadInnerPlaceOrder = runningCandlePayload.PreviousPayload.ToString
        '                        logger.Debug("Place Order-> Stoploss is greater than maximum stoploss point or less than minimum stoploss point. Direction:{0}, Entry Price:{1}, Stoploss:{2}, SLPoint:{3}, Max Stoploss Point:{4}",
        '                                     "Buy", potentialEntryPrice, stoploss, potentialEntryPrice - stoploss, maxStoplossPoint)
        '                    End If
        '                Catch ex As Exception
        '                    logger.Error(ex)
        '                End Try
        '            End If
        '        ElseIf (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
        '            (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then
        '            Dim potentialEntryPrice As Decimal = runningCandlePayload.PreviousPayload.LowPrice.Value
        '            potentialEntryPrice -= CalculateBuffer(potentialEntryPrice, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
        '            Dim stoploss As Decimal = runningCandlePayload.PreviousPayload.HighPrice.Value
        '            stoploss += CalculateBuffer(stoploss, Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
        '            If (stoploss - potentialEntryPrice) >= minStoplossPoint AndAlso (stoploss - potentialEntryPrice) <= maxStoplossPoint Then
        '                If currentLTP <= potentialEntryPrice Then
        '                    parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
        '                        {.EntryDirection = IOrder.TypeOfTransaction.Sell,
        '                         .Quantity = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).Quantity,
        '                         .TriggerPrice = stoploss,
        '                         .Supporting = New List(Of Object)}

        '                    parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
        '                    parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)
        '                End If
        '            Else
        '                Try
        '                    If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadInnerPlaceOrder Then
        '                        _lastPrevPayloadInnerPlaceOrder = runningCandlePayload.PreviousPayload.ToString
        '                        logger.Debug("Place Order-> Stoploss is greater than maximum stoploss point or less than minimum stoploss point. Direction:{0}, Entry Price:{1}, Stoploss:{2}, SLPoint:{3}, Max Stoploss Point:{4}",
        '                                     "Sell", potentialEntryPrice, stoploss, stoploss - potentialEntryPrice, maxStoplossPoint)
        '                    End If
        '                Catch ex As Exception
        '                    logger.Error(ex)
        '                End Try
        '            End If
        '        End If
        '    End If
        'End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                If forcePrint Then
                    logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    If Me.TradableInstrument.IsHistoricalCompleted Then
                        logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                        'logger.Debug("PlaceOrder-> Rest all parameters: 
                        '            Trade Start Time:{0}, Last Trade Entry Time:{1}, 
                        '            RunningCandlePayloadSnapshotDateTime:{2}, PayloadGeneratedBy:{3}, IsHistoricalCompleted:{4}, 
                        '            Max Stock Profit:{5}, Max Stock Loss:{6}, Stock PL:{7}, 
                        '            Max Profit Per Day:{8}, Max Loss Per Day:{9}, Overall PL:{10}, 
                        '            Is Active Instrument:{11}, Reverse Trade:{12}, 
                        '            Top Wick %:{13}, Bottom Wick %:{14}, 
                        '            Current Time:{15}, Current LTP:{16}, 
                        '            TradingSymbol:{17}",
                        '            userSettings.TradeStartTime.ToString,
                        '            userSettings.LastTradeEntryTime.ToString,
                        '            runningCandlePayload.SnapshotDateTime.ToString,
                        '            runningCandlePayload.PayloadGeneratedBy.ToString,
                        '            Me.TradableInstrument.IsHistoricalCompleted,
                        '            maxStockProfit,
                        '            maxStockLoss,
                        '            Me.GetOverallPLAfterBrokerage(),
                        '            userSettings.MaxProfitPerDay,
                        '            userSettings.MaxLossPerDay,
                        '            Me.ParentStrategy.GetTotalPLAfterBrokerage,
                        '            IsActiveInstrument(),
                        '            reverseTrade,
                        '            (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                        '            (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100,
                        '            currentTime.ToString,
                        '            currentLTP,
                        '            Me.TradableInstrument.TradingSymbol)
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

            Dim exitOrderResponse As IBusinessOrder = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
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
