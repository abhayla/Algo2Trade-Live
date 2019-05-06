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
    Private ReadOnly _apiKey As String = "700121864:AAHjes45V0kEPBDLIfnZzsatH5NhRwIjciw"
    Private ReadOnly _chaitId As String = "-337360611"
    Private ReadOnly _dummySpreadRatioConsumer As SpreadRatioConsumer
    Private ReadOnly _dummySpreadBollingerConsumer As BollingerConsumer
    Private ReadOnly _dummyRatioBollingerConsumer As BollingerConsumer
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
                _dummySpreadRatioConsumer = New SpreadRatioConsumer(pairConsumer, TypeOfField.Close)
                _dummySpreadBollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Spread)
                _dummyRatioBollingerConsumer = New BollingerConsumer(spreadRatioData, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerPeriod, CType(Me.ParentStrategy.UserSettings, NearFarHedgingStrategyUserInputs).BollingerMultiplier, TypeOfField.Ratio)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            If Me.IsPairInstrument Then
                Dim lastTriggerTime As Date = Date.MinValue
                While True
                    If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                        Throw Me.ParentStrategy.ParentController.OrphanException
                    End If

                    _cts.Token.ThrowIfCancellationRequested()
                    Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                    If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                        If Not Utilities.Time.IsDateTimeEqualTillMinutes(placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime, lastTriggerTime) Then
                            lastTriggerTime = placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime
                            GenerateTelegramMessageAsync(String.Format("Signal:{0}, SignalTime:{6}, {1}:{2}, {3}:{4}, Timestamp:{5}",
                                                                    placeOrderTrigger.Item2.EntryDirection.ToString,
                                                                    Me.ParentStrategyInstruments.FirstOrDefault.TradableInstrument.TradingSymbol,
                                                                    Me.ParentStrategyInstruments.FirstOrDefault.TradableInstrument.LastTick.LastPrice,
                                                                    Me.ParentStrategyInstruments.LastOrDefault.TradableInstrument.TradingSymbol,
                                                                    Me.ParentStrategyInstruments.LastOrDefault.TradableInstrument.LastTick.LastPrice,
                                                                    Now, placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToString))
                        End If
                    End If
                    '_cts.Token.ThrowIfCancellationRequested()
                    'Dim modifyStoplossOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                    'If modifyStoplossOrdersTrigger IsNot Nothing AndAlso modifyStoplossOrdersTrigger.Count > 0 Then
                    '    Dim modifyOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    '    'If modifyOrderResponse IsNot Nothing Then
                    '    '    _sendParentOrderDetailsOfOrderId = Nothing
                    '    '    Dim modifyStoplossOrderTrigger As Tuple(Of ExecuteCommandAction, IOrder, Decimal, String) = modifyStoplossOrdersTrigger.LastOrDefault
                    '    '    Dim parentBusinessOrder As IBusinessOrder = GetParentFromChildOrder(modifyStoplossOrderTrigger.Item2)
                    '    '    If parentBusinessOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder IsNot Nothing Then
                    '    '        If modifyStoplossOrderTrigger.Item4 = "Normal SL movement according to SL%" Then
                    '    '            GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Executed, Direction: {2}, Quantity: {3}, Actual Entry Price:{4}, Actual SL Price: {5}", Now, Me.TradableInstrument.TradingSymbol, parentBusinessOrder.ParentOrder.TransactionType, parentBusinessOrder.ParentOrder.Quantity, parentBusinessOrder.ParentOrder.AveragePrice, modifyStoplossOrderTrigger.Item3))
                    '    '        Else
                    '    '            GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Modified, Direction: {2}, Quantity: {3}, Actual Entry Price:{4}, Actual SL Price: {5}, Remarks:{6}", Now, Me.TradableInstrument.TradingSymbol, parentBusinessOrder.ParentOrder.TransactionType, parentBusinessOrder.ParentOrder.Quantity, parentBusinessOrder.ParentOrder.AveragePrice, modifyStoplossOrderTrigger.Item3, modifyStoplossOrderTrigger.Item4))
                    '    '        End If
                    '    '    End If
                    '    'End If
                    'End If
                    '_cts.Token.ThrowIfCancellationRequested()
                    'Dim exitOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                    'If exitOrdersTrigger IsNot Nothing AndAlso exitOrdersTrigger.Count > 0 Then
                    '    Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.CancelCOOrder, Nothing).ConfigureAwait(False)
                    '    'If exitOrderResponse IsNot Nothing Then
                    '    '    GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, exitOrdersTrigger.LastOrDefault.Item3))
                    '    'End If
                    'End If
                    '_cts.Token.ThrowIfCancellationRequested()
                    Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                End While
            End If
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim hedgingUserInputs As NearFarHedgingStrategyUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As PairPayload = GetXMinuteCurrentCandle()
        Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(TypeOfExchage.NSE)
        Dim spreadRatioConsumer As SpreadRatioConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySpreadRatioConsumer)
        Dim spreadBollingerConsumer As BollingerConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySpreadBollingerConsumer)
        Dim ratioBollingerConsumer As BollingerConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyRatioBollingerConsumer)

        Try
            If runningCandlePayload IsNot Nothing AndAlso
                runningCandlePayload.Instrument1Payload IsNot Nothing AndAlso runningCandlePayload.Instrument2Payload IsNot Nothing AndAlso
                runningCandlePayload.Instrument1Payload.PreviousPayload IsNot Nothing AndAlso runningCandlePayload.Instrument2Payload.PreviousPayload IsNot Nothing Then
                If Not runningCandlePayload.Instrument1Payload.PreviousPayload.ToString = lastPrevPayloadPlaceOrder Then
                    lastPrevPayloadPlaceOrder = runningCandlePayload.Instrument1Payload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:Intrument1:{0}", runningCandlePayload.Instrument1Payload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:Intrument2:{0}", runningCandlePayload.Instrument2Payload.PreviousPayload.ToString)
                    logger.Debug(CType(spreadRatioConsumer.ConsumerPayloads(runningCandlePayload.Instrument1Payload.PreviousPayload.SnapshotDateTime), SpreadRatioConsumer.SpreadRatioPayload).ToString)
                    logger.Debug(CType(spreadBollingerConsumer.ConsumerPayloads(runningCandlePayload.Instrument1Payload.PreviousPayload.SnapshotDateTime), BollingerConsumer.BollingerPayload).ToString)
                    logger.Debug(CType(ratioBollingerConsumer.ConsumerPayloads(runningCandlePayload.Instrument1Payload.PreviousPayload.SnapshotDateTime), BollingerConsumer.BollingerPayload).ToString)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If Now >= hedgingUserInputs.TradeStartTime AndAlso Now <= hedgingUserInputs.LastTradeEntryTime AndAlso runningCandlePayload IsNot Nothing AndAlso
            (runningCandlePayload.Instrument1Payload IsNot Nothing OrElse runningCandlePayload.Instrument2Payload IsNot Nothing) AndAlso
            Not IsActiveInstrument() AndAlso Not Me.PairConsumerProtection AndAlso
            Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(hedgingUserInputs.MaxLossPercentagePerDay) * -1 / 100 AndAlso
            Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(hedgingUserInputs.MaxProfitPercentagePerDay) / 100 AndAlso
            spreadRatioConsumer IsNot Nothing AndAlso spreadBollingerConsumer IsNot Nothing AndAlso ratioBollingerConsumer IsNot Nothing Then
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

            If currentCandle IsNot Nothing AndAlso currentCandle.PreviousPayload IsNot Nothing AndAlso
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
                                                  ratioBollingerConsumer.ConsumerPayloads(signalCandleTime))
                If potentialSignalData IsNot Nothing AndAlso potentialSignalData.Item1 Then
                    If potentialSignalData.Item2 = IOrder.TypeOfTransaction.Buy Then
                        parameters = New PlaceOrderParameters(currentCandle.PreviousPayload) With
                                     {
                                       .EntryDirection = IOrder.TypeOfTransaction.Buy,
                                       .Quantity = 1,
                                       .Price = 0
                                     }
                    ElseIf potentialSignalData.Item2 = IOrder.TypeOfTransaction.Sell Then
                        parameters = New PlaceOrderParameters(currentCandle.PreviousPayload) With
                                     {
                                       .EntryDirection = IOrder.TypeOfTransaction.Sell,
                                       .Quantity = 1,
                                       .Price = 0
                                     }
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
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

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
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
            Dim exitOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.ForceCancelCOOrder, cancellableOrder).ConfigureAwait(False)
            'If exitOrderResponse IsNot Nothing Then
            '    GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Exit Placed, Remarks:{2}", Now, Me.TradableInstrument.TradingSymbol, reason))
            'End If
        End If
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        Console.WriteLine(message)
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Using tSender As New Utilities.Notification.Telegram(_apiKey, _chaitId, _cts)
            Await tSender.SendMessageGetAsync(Utilities.Strings.EncodeString(message)).ConfigureAwait(False)
        End Using
    End Function

    Private Function CheckSignal(ByVal spreadRatioData As SpreadRatioConsumer.SpreadRatioPayload,
                                 ByVal spreadBollingerData As BollingerConsumer.BollingerPayload,
                                 ByVal ratioBollingerData As BollingerConsumer.BollingerPayload) As Tuple(Of Boolean, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
        If spreadRatioData IsNot Nothing AndAlso spreadBollingerData IsNot Nothing AndAlso ratioBollingerData IsNot Nothing Then
            Dim spreadSignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
            Dim ratioSignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
            If spreadRatioData.Spread IsNot Nothing AndAlso
                spreadBollingerData.HighBollinger IsNot Nothing AndAlso
                spreadBollingerData.LowBollinger IsNot Nothing Then
                If spreadRatioData.Spread.Value > spreadBollingerData.HighBollinger.Value Then
                    logger.Debug("Spread Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Spread.Value, spreadBollingerData.HighBollinger.Value, spreadBollingerData.LowBollinger.Value)
                    spreadSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Sell)
                ElseIf spreadRatioData.Spread.Value < spreadBollingerData.LowBollinger.Value Then
                    logger.Debug("Spread Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Spread.Value, spreadBollingerData.HighBollinger.Value, spreadBollingerData.LowBollinger.Value)
                    spreadSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Buy)
                End If
            End If
            If spreadRatioData.Ratio IsNot Nothing AndAlso
                ratioBollingerData.HighBollinger IsNot Nothing AndAlso
                ratioBollingerData.LowBollinger IsNot Nothing Then
                If spreadRatioData.Ratio.Value > ratioBollingerData.HighBollinger.Value Then
                    logger.Debug("Ratio Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Ratio.Value, ratioBollingerData.HighBollinger.Value, ratioBollingerData.LowBollinger.Value)
                    ratioSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Sell)
                ElseIf spreadRatioData.Ratio.Value < ratioBollingerData.LowBollinger.Value Then
                    logger.Debug("Ratio Value:{0}, Bollinger High:{1}, Bollinger Low:{2}", spreadRatioData.Ratio.Value, ratioBollingerData.HighBollinger.Value, ratioBollingerData.LowBollinger.Value)
                    ratioSignal = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Buy)
                End If
            End If
            'If spreadSignal IsNot Nothing AndAlso ratioSignal IsNot Nothing Then
            'If spreadSignal.Item1 OrElse ratioSignal.Item1 Then
            'If spreadSignal.Item2 = ratioSignal.Item2 Then

            If spreadSignal IsNot Nothing AndAlso spreadSignal.Item1 Then ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, spreadSignal.Item2)
            If ratioSignal IsNot Nothing AndAlso ratioSignal.Item1 Then ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, ratioSignal.Item2)
            'End If
            'End If
            'End If
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
