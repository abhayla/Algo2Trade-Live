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

    Private ReadOnly _apiKey As String = "815345137:AAH_34NObix7jbARfZZII5yu-bhFpGXdUFE"
    Private ReadOnly _chaitId As String = "-322631613"
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
                chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim spreadData As SpreadConsumer = New SpreadConsumer(chartConsumer, Me.DependendStratrgyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault, TypeOfField.Close, TypeOfField.Close)
                spreadData.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim bollingerData As BollingerConsumer = New BollingerConsumer(spreadData, 20, 2, TypeOfField.Spread)
                spreadData.OnwardLevelConsumers.Add(bollingerData)
                chartConsumer.OnwardLevelConsumers.Add(spreadData)
                RawPayloadDependentConsumers.Add(chartConsumer)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            If RawPayloadDependentConsumers IsNot Nothing AndAlso RawPayloadDependentConsumers.Count > 0 Then
                Dim chartConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.FirstOrDefault
                chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim spreadData As SpreadConsumer = New SpreadConsumer(chartConsumer, Me.DependendStratrgyInstruments.FirstOrDefault.RawPayloadDependentConsumers.FirstOrDefault, TypeOfField.Close, TypeOfField.Close)
                spreadData.OnwardLevelConsumers = New List(Of IPayloadConsumer)
                Dim bollingerData As BollingerConsumer = New BollingerConsumer(spreadData, 20, 2, TypeOfField.Spread)
                spreadData.OnwardLevelConsumers.Add(bollingerData)
                chartConsumer.OnwardLevelConsumers.Add(spreadData)
            End If

            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If

                '_cts.Token.ThrowIfCancellationRequested()
                'Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                'If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                '    Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceCOMarketMISOrder, Nothing).ConfigureAwait(False)
                '    'If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                '    '    placeOrderResponse("data").ContainsKey("order_id") Then
                '    '    _sendParentOrderDetailsOfOrderId = placeOrderResponse("data")("order_id")
                '    '    GenerateTelegramMessageAsync(String.Format("{0}, {1} - Order Placed, Direction: {2}, Quantity: {3}, Proposed SL Price: {4}", Now, Me.TradableInstrument.TradingSymbol, placeOrderTrigger.Item2.EntryDirection.ToString, placeOrderTrigger.Item2.Quantity, placeOrderTrigger.Item2.TriggerPrice))
                '    'End If
                'End If
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
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
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
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        'Using tSender As New Utilities.Notification.Telegram(_apiKey, _chaitId, _cts)
        '    Await tSender.SendMessageGetAsync(Utilities.Strings.EncodeString(message)).ConfigureAwait(False)
        'End Using
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
