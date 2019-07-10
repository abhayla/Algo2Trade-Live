Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Utilities.Numbers
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class OHLStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private ReadOnly _dummyOISMAConsumer As TickSMAConsumer
    Private ReadOnly _dummyLastPriceSMAConsumer As TickSMAConsumer
    Private _lastDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
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
        If Me.ParentStrategy.IsStrategyCandleStickBased Then
            RawPayloadDependentConsumers = New List(Of IPayloadConsumer)
            If Me.ParentStrategy.UserSettings.SignalTimeFrame > 0 Then
                RawPayloadDependentConsumers.Add(New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame))
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        If Me.ParentStrategy.IsTickPopulationNeeded Then
            TickPayloadDependentConsumers = New List(Of IPayloadConsumer)
            Dim chartConsumer As PayloadToChartConsumer = New PayloadToChartConsumer(Me.ParentStrategy.UserSettings.SignalTimeFrame)
            chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
            {New TickSMAConsumer(chartConsumer, 50, TypeOfField.OI),
            New TickSMAConsumer(chartConsumer, 50, TypeOfField.LastPrice)}
            TickPayloadDependentConsumers.Add(chartConsumer)

            _dummyOISMAConsumer = New TickSMAConsumer(chartConsumer, 50, TypeOfField.OI)
            _dummyLastPriceSMAConsumer = New TickSMAConsumer(chartConsumer, 50, TypeOfField.LastPrice)
        End If
    End Sub
    Public Overrides Function MonitorAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task
        Throw New NotImplementedException()
    End Function
    'Public Overrides Async Function MonitorAsync() As Task
    '    Try
    '        While True
    '            If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
    '                Throw Me.ParentStrategy.ParentController.OrphanException
    '            End If
    '            _cts.Token.ThrowIfCancellationRequested()
    '            Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
    '            If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
    '                If placeOrderTrigger.Item2.EntryDirection <> _lastDirection Then
    '                    GenerateTelegramMessageAsync(String.Format("Trading Symbol:{0}, Direction:{1}, LTP:{2}, TimeStamp:{3}",
    '                                                                 Me.TradableInstrument.TradingSymbol,
    '                                                                 placeOrderTrigger.Item2.EntryDirection.ToString,
    '                                                                 placeOrderTrigger.Item2.Price,
    '                                                                 placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToString))
    '                    _lastDirection = placeOrderTrigger.Item2.EntryDirection
    '                End If
    '            End If
    '            _cts.Token.ThrowIfCancellationRequested()

    '            Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
    '        End While
    '    Catch ex As Exception
    '        'To log exceptions getting created from this function as the bubble up of the exception
    '        'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
    '        logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
    '        Throw ex
    '    End Try
    'End Function
    'Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
    '    Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
    '    Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
    '    Dim OHLUserSettings As OHLUserInputs = Me.ParentStrategy.UserSettings
    '    Dim capitalAtDayStart As Decimal = Me.ParentStrategy.ParentController.GetUserMargin(Me.TradableInstrument.ExchangeDetails.ExchangeType)
    '    Dim instrumentName As String = Me.TradableInstrument.RawInstrumentName

    '    Dim parameters As PlaceOrderParameters = Nothing
    '    If Now < OHLUserSettings.LastTradeEntryTime AndAlso Not IsActiveInstrument() AndAlso
    '        Me.GetOverallPL() > Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxLossPerStock) * -1 AndAlso
    '        Me.GetOverallPL() < Math.Abs(OHLUserSettings.InstrumentsData(instrumentName).MaxProfitPerStock) AndAlso
    '        Me.ParentStrategy.GetTotalPL() > capitalAtDayStart * Math.Abs(OHLUserSettings.MaxLossPercentagePerDay) * -1 / 100 AndAlso
    '        Me.ParentStrategy.GetTotalPL() < capitalAtDayStart * Math.Abs(OHLUserSettings.MaxProfitPercentagePerDay) / 100 Then

    '        Dim OISignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = CheckSignal(_dummyOISMAConsumer)
    '        If OISignal IsNot Nothing AndAlso OISignal.Item1 Then
    '            Dim LTPSignal As Tuple(Of Boolean, IOrder.TypeOfTransaction) = CheckSignal(_dummyLastPriceSMAConsumer)
    '            If LTPSignal IsNot Nothing AndAlso LTPSignal.Item1 Then
    '                Dim dummyPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {
    '                    .SnapshotDateTime = Me.TradableInstrument.LastTick.Timestamp
    '                }
    '                If LTPSignal.Item2 = IOrder.TypeOfTransaction.Buy Then
    '                    parameters = New PlaceOrderParameters(dummyPayload) With
    '                        {
    '                        .EntryDirection = IOrder.TypeOfTransaction.Buy,
    '                        .Price = Me.TradableInstrument.LastTick.LastPrice
    '                        }
    '                ElseIf LTPSignal.Item2 = IOrder.TypeOfTransaction.Sell Then
    '                    parameters = New PlaceOrderParameters(dummyPayload) With
    '                        {
    '                        .EntryDirection = IOrder.TypeOfTransaction.Sell,
    '                        .Price = Me.TradableInstrument.LastTick.LastPrice
    '                        }
    '                End If
    '            End If
    '        End If
    '    End If

    '    'Below portion have to be done in every place order trigger
    '    If parameters IsNot Nothing Then
    '        Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
    '        If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
    '            If currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
    '                currentSignalActivities.FirstOrDefault.EntryActivity.LastException IsNot Nothing AndAlso
    '                currentSignalActivities.FirstOrDefault.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
    '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "")
    '            ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
    '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
    '                'ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
    '                '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
    '            Else
    '                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
    '            End If
    '        Else
    '            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "")
    '        End If
    '    Else
    '        ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
    '    End If
    '    Return ret
    'End Function
    Private _isBuyOrderPlaced As Boolean = False
    Private _isSellOrderPlaced As Boolean = False
    Public Overrides Async Function MonitorAsync() As Task
        Try
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    If placeOrderTrigger.Item2.EntryDirection = IOrder.TypeOfTransaction.Buy AndAlso Not _isBuyOrderPlaced Then
                        Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                        _isBuyOrderPlaced = True
                    ElseIf placeOrderTrigger.Item2.EntryDirection = IOrder.TypeOfTransaction.Sell AndAlso Not _isSellOrderPlaced Then
                        Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                        _isSellOrderPlaced = True
                    End If
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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim startTime As Date = New Date(Now.Year, Now.Month, Now.Day, 9, 15, 0)
        Dim currentTick As ITick = Me.TradableInstrument.LastTick

        If currentTick Is Nothing OrElse currentTick.Timestamp Is Nothing OrElse currentTick.Timestamp.Value = Date.MinValue OrElse currentTick.Timestamp.Value = New Date(1970, 1, 1, 5, 30, 0) Then
            Exit Function
        End If

        Try
            logger.Debug("Place Order-> LTP:{0}, Timestamp:{1}", currentTick.LastPrice, currentTick.Timestamp.Value)
        Catch ex As Exception
            logger.Error("Error in tick log")
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTick.Timestamp.Value >= startTime Then
            Dim dummyPayload As OHLCPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {.SnapshotDateTime = Now}
            If Not _isBuyOrderPlaced Then
                Dim open As Decimal = currentTick.Open
                Dim triggerPrice = open + ConvertFloorCeling(open * 0.5 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                Dim price As Decimal = triggerPrice + ConvertFloorCeling(open * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                Dim stoploss As Decimal = ConvertFloorCeling(open * 0.5 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(stoploss * 3.1, Me.TradableInstrument.TickSize, RoundOfType.Celing)

                If currentTick.LastPrice < triggerPrice Then
                    parameters = New PlaceOrderParameters(dummyPayload) With
                                   {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                   .Quantity = 1,
                                   .Price = price,
                                   .TriggerPrice = triggerPrice,
                                   .SquareOffValue = target,
                                   .StoplossValue = stoploss}
                End If
            ElseIf Not _isSellOrderPlaced Then
                Dim open As Decimal = currentTick.Open
                Dim triggerPrice = open - ConvertFloorCeling(open * 0.5 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                Dim price As Decimal = triggerPrice - ConvertFloorCeling(open * 0.3 / 100, TradableInstrument.TickSize, RoundOfType.Celing)
                Dim stoploss As Decimal = ConvertFloorCeling(open * 0.5 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                Dim target As Decimal = ConvertFloorCeling(stoploss * 3.1, Me.TradableInstrument.TickSize, RoundOfType.Celing)

                If currentTick.LastPrice > triggerPrice Then
                    parameters = New PlaceOrderParameters(dummyPayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                       .Quantity = 1,
                                       .Price = price,
                                       .TriggerPrice = triggerPrice,
                                       .SquareOffValue = target,
                                       .StoplossValue = stoploss}
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                logger.Debug("*******Place Order******* Open:{0}, Order Place Time:{1}, LTP:{2}", currentTick.Open, currentTick.Timestamp.Value, currentTick.LastPrice)
            Catch ex As Exception
                logger.Error("Error in place order log")
            End Try

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
    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function
    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Throw New NotImplementedException()
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

    Private Function CheckSignal(ByVal dummyConsumer As TickSMAConsumer) As Tuple(Of Boolean, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, IOrder.TypeOfTransaction) = Nothing
        If dummyConsumer IsNot Nothing Then
            Dim consumer As TickSMAConsumer = GetConsumer(Me.TickPayloadDependentConsumers, dummyConsumer)
            If consumer IsNot Nothing AndAlso consumer.OutputPayload IsNot Nothing AndAlso consumer.OutputPayload.Count > 0 Then
                Dim currentConsumerPayload As TickSMAConsumer.TickSMAPayload = consumer.OutputPayload.OrderBy(Function(x)
                                                                                                                  Return x.TimeStamp
                                                                                                              End Function).LastOrDefault

                Dim requiredData As IEnumerable(Of TickSMAConsumer.TickSMAPayload) = Nothing
                requiredData = consumer.OutputPayload.Where(Function(y)
                                                                Return y.TimeStamp <= currentConsumerPayload.TimeStamp
                                                            End Function)

                Dim requiredDataSet As IEnumerable(Of TickSMAConsumer.TickSMAPayload) = Nothing
                If requiredData IsNot Nothing AndAlso requiredData.Count > 0 Then
                    requiredDataSet = requiredData.OrderByDescending(Function(x)
                                                                         Return x.TimeStamp
                                                                     End Function).Take(10)
                End If

                If requiredDataSet IsNot Nothing AndAlso requiredDataSet.Count = 10 Then
                    Dim sum As Integer = requiredDataSet.Sum(Function(x)
                                                                 Return x.Momentum
                                                             End Function)

                    Select Case consumer.SMAField
                        Case TypeOfField.OI
                            If sum = 10 Then
                                ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.None)
                            End If
                        Case TypeOfField.LastPrice
                            If sum = 10 Then
                                ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Buy)
                            ElseIf sum = -10 Then
                                ret = New Tuple(Of Boolean, IOrder.TypeOfTransaction)(True, IOrder.TypeOfTransaction.Sell)
                            End If
                    End Select
                End If
            End If
        End If
        Return ret
    End Function

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

        Using tSender As New Utilities.Notification.Telegram("700121864:AAHjes45V0kEPBDLIfnZzsatH5NhRwIjciw", "-335771635", _cts)
            Dim encodedString As String = Utilities.Strings.EncodeString(message)
            logger.Debug("Encoded String:{0}", encodedString)
            Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
        End Using
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
