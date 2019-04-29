Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog

Public Class OHLStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _OHLStrategyProtect As Integer
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
                RawPayloadDependentConsumers.Add(New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame))
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        _OHLStrategyProtect = 0
    End Sub
    Public Overrides Async Function MonitorAsync() As Task
        Try
            'Dim slDelayCtr As Integer = 0
            Dim OHLUserSettings As OHLUserInputs = Me.ParentStrategy.UserSettings
            Dim instrumentName As String = Nothing
            If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
                instrumentName = Me.TradableInstrument.TradingSymbol.Remove(Me.TradableInstrument.TradingSymbol.Count - 8)
            Else
                instrumentName = Me.TradableInstrument.TradingSymbol
            End If
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If Me.GetOverallPL() <= Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxLossPerStock) * -1 OrElse
                    Me.GetOverallPL() >= Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxProfitPerStock) Then
                    Debug.WriteLine("Force Cancel for stock pl")
                    Await ForceExitAllTradesAsync("Force Cancel for stock pl").ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderDetails As Object = Nothing
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take AndAlso
                    Interlocked.Read(_OHLStrategyProtect) = 0 Then
                    Interlocked.Increment(_OHLStrategyProtect)
                    placeOrderDetails = Await ExecuteCommandAsync(ExecuteCommands.PlaceBOLimitMISOrder, Nothing).ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                'If slDelayCtr = 10 Then
                '    slDelayCtr = 0
                'Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal)) = Await IsTriggerReceivedForModifyStoplossOrderAsync().ConfigureAwait(False)
                'If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                '    'Interlocked.Increment(_OHLStrategyProtector)
                '    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                'End If
                'End If
                '_cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                'slDelayCtr += 1
            End While
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function
    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Dim currentTime As Date = Now
        Dim OHLUserSettings As OHLUserInputs = Me.ParentStrategy.UserSettings
        Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(Me.TradableInstrument.ExchangeDetails.ExchangeType)

        Dim instrumentName As String = Nothing
        If Me.TradableInstrument.TradingSymbol.Contains("FUT") Then
            instrumentName = Me.TradableInstrument.TradingSymbol.Remove(Me.TradableInstrument.TradingSymbol.Count - 8)
        Else
            instrumentName = Me.TradableInstrument.TradingSymbol
        End If

        Dim parameters As PlaceOrderParameters = Nothing
        If Now < OHLUserSettings.LastTradeEntryTime AndAlso Not IsActiveInstrument() AndAlso
            Me.GetOverallPL() > Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxLossPerStock) * -1 AndAlso
            Me.GetOverallPL() < Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxProfitPerStock) AndAlso
            Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(OHLUserSettings.MaxLossPercentagePerDay) * -1 / 100 AndAlso
            Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(OHLUserSettings.MaxProfitPercentagePerDay) / 100 Then
            If TradableInstrument.LastTick.Timestamp IsNot Nothing AndAlso
                currentTime.Hour = OHLUserSettings.TradeStartTime.Hour AndAlso currentTime.Minute = OHLUserSettings.TradeStartTime.Minute AndAlso
                currentTime.Second >= OHLUserSettings.TradeStartTime.Second Then

                Dim OHLTradePrice As Decimal = TradableInstrument.LastTick.LastPrice
                Dim buffer As Decimal = Math.Round(ConvertFloorCeling(OHLTradePrice * 0.003, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Floor), 2)
                Dim entryPrice As Decimal = Nothing
                Dim target As Decimal = Math.Round(ConvertFloorCeling(OHLTradePrice * OHLUserSettings.TargetPercentage / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                Dim stoploss As Decimal = Math.Round(ConvertFloorCeling(OHLTradePrice * OHLUserSettings.StoplossPercentage / 100, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
                Dim quantity As Integer = Nothing
                If Me.TradableInstrument.RawInstrumentType.ToUpper = "FUT" Then
                    quantity = Me.TradableInstrument.LotSize * OHLUserSettings.InstrumentsData(instrumentName).Quantity
                Else
                    If OHLUserSettings.InstrumentsData(instrumentName).Capital <> Decimal.MinValue AndAlso
                        OHLUserSettings.InstrumentsData(instrumentName).Capital > 0 Then
                        quantity = Math.Floor(OHLUserSettings.InstrumentsData(instrumentName).Capital / (Math.Floor(Me.TradableInstrument.LastTick.LastPrice / 13)))
                    Else
                        quantity = OHLUserSettings.InstrumentsData(instrumentName).Quantity
                    End If
                End If
                Dim dummyPayload As OHLCPayload = Nothing
                If TradableInstrument.LastTick.Open = TradableInstrument.LastTick.High Then
                    entryPrice = OHLTradePrice - buffer
                    dummyPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {
                        .SnapshotDateTime = TradableInstrument.LastTick.Timestamp
                    }
                    parameters = New PlaceOrderParameters(dummyPayload) With
                                {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                .Quantity = quantity,
                                .Price = entryPrice,
                                .TriggerPrice = Nothing,
                                .SquareOffValue = target,
                                .StoplossValue = stoploss}
                ElseIf TradableInstrument.LastTick.Open = TradableInstrument.LastTick.Low Then
                    entryPrice = OHLTradePrice + buffer
                    dummyPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {
                        .SnapshotDateTime = TradableInstrument.LastTick.Timestamp
                    }
                    parameters = New PlaceOrderParameters(dummyPayload) With
                                {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                .Quantity = quantity,
                                .Price = entryPrice,
                                .TriggerPrice = Nothing,
                                .SquareOffValue = target,
                                .StoplossValue = stoploss}
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
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "")
                ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
                    'ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                    '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
                Else
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
                End If
            Else
                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
            End If
        Else
            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        End If
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        'If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
        '    Dim currentTime As Date = Now
        '    For Each parentOrderId In OrderDetails.Keys
        '        Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
        '        If parentBusinessOrder.ParentOrder.Status = "COMPLETE" AndAlso
        '            parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
        '            'If parentBusinessOrder.ParentOrder.Tag.Substring(GenerateTag().Count + 1) = "1" Then
        '            Dim parentOrderPrice As Decimal = parentBusinessOrder.ParentOrder.AveragePrice
        '            Dim triggerPrice As Decimal = TradableInstrument.LastTick.Open
        '            Dim buffer As Decimal = CalculateBuffer(triggerPrice, Me.TradableInstrument.TickSize, RoundOfType.Floor)
        '            If parentBusinessOrder.ParentOrder.TransactionType = "BUY" Then
        '                triggerPrice -= buffer
        '            ElseIf parentBusinessOrder.ParentOrder.TransactionType = "SELL" Then
        '                triggerPrice += buffer
        '            End If

        '            Dim potentialStoplossPrice As Decimal = Nothing
        '            For Each slOrder In parentBusinessOrder.SLOrder
        '                If Not slOrder.Status = "COMPLETE" AndAlso Not slOrder.Status = "CANCELLED" AndAlso Not slOrder.Status = "REJECTED" Then
        '                    If parentBusinessOrder.ParentOrder.TransactionType = "BUY" Then
        '                        potentialStoplossPrice = Math.Round(ConvertFloorCeling(parentOrderPrice - parentOrderPrice * 0.005, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
        '                        If currentTime.Hour = 9 AndAlso currentTime.Minute >= 16 AndAlso triggerPrice < potentialStoplossPrice Then
        '                            triggerPrice = potentialStoplossPrice
        '                        End If
        '                    ElseIf parentBusinessOrder.ParentOrder.TransactionType = "SELL" Then
        '                        potentialStoplossPrice = Math.Round(ConvertFloorCeling(parentOrderPrice + parentOrderPrice * 0.005, Convert.ToDouble(TradableInstrument.TickSize), RoundOfType.Celing), 2)
        '                        If currentTime.Hour = 9 AndAlso currentTime.Minute >= 16 AndAlso triggerPrice > potentialStoplossPrice Then
        '                            triggerPrice = potentialStoplossPrice
        '                        End If
        '                    End If
        '                    If slOrder.TriggerPrice <> triggerPrice Then
        '                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal))
        '                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal)(True, slOrder, triggerPrice))
        '                        'Else
        '                        '    Debug.WriteLine(String.Format("Stoploss modified {0} Quantity:{1}, ID:{2}", Me.GenerateTag(), slOrder.Quantity, slOrder.OrderIdentifier))
        '                    End If
        '                End If
        '            Next
        '            'End If
        '        End If
        '    Next
        'End If
        Throw New NotImplementedException
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException()
    End Function
    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException
        Return ret
    End Function
    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, ByVal reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }
            Await ExecuteCommandAsync(ExecuteCommands.ForceCancelBOOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                If _APIAdapter IsNot Nothing Then
                    RemoveHandler _APIAdapter.Heartbeat, AddressOf OnHeartbeat
                    RemoveHandler _APIAdapter.WaitingFor, AddressOf OnWaitingFor
                    RemoveHandler _APIAdapter.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                    RemoveHandler _APIAdapter.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                End If
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
