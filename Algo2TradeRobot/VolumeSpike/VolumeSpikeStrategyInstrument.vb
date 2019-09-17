Imports NLog
Imports System.Threading
Imports Utilities.Numbers
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Entities.Indicators

Public Class VolumeSpikeStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

    Public EligibleToTakeTrade As Boolean
    Public VolumeChangePercentage As Decimal

    Private _potentialHighEntryPrice As Decimal = Decimal.MinValue
    Private _potentialLowEntryPrice As Decimal = Decimal.MinValue
    Private _signalCandle As OHLCPayload = Nothing
    Private _signalType As TypeOfSignal = TypeOfSignal.None
    Private _entryChanged As Boolean = False
    Private _targetMultiplier As Decimal = Decimal.MinValue

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
                If Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures Then
                    chartConsumer.OnwardLevelConsumers = New List(Of IPayloadConsumer) From
                    {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).ATRPeriod)}
                End If
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, VolumeSpikeUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        Me.EligibleToTakeTrade = False
        Me.VolumeChangePercentage = Decimal.MinValue
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
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
                If Me.VolumeChangePercentage = Decimal.MinValue AndAlso Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                    Await GetVolumeSpike().ConfigureAwait(False)
                End If
                'Calculate volume spike end
                _cts.Token.ThrowIfCancellationRequested()
                If Me.EligibleToTakeTrade Then
                    'Force Cancel block strat
                    If IsAnyTradeTargetReached() Then
                        Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                    End If
                    'Force Cancel block end
                    _cts.Token.ThrowIfCancellationRequested()
                    ''Place Order block start
                    'Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                    'If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                    '    Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                    'End If
                    ''Place Order block end
                    '_cts.Token.ThrowIfCancellationRequested()
                    ''Modify Order block start
                    'Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                    'If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                    '    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                    'End If
                    ''Modify Order block end
                End If
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

    Public Overrides Function MonitorAsync(command As ExecuteCommands, data As Object) As Task
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
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

    Protected Overrides Function ForceExitSpecificTradeAsync(order As IOrder, reason As String) As Task
        Throw New NotImplementedException()
    End Function

    Private Async Function GetVolumeSpike() As Task
        Await Task.Delay(0).ConfigureAwait(False)
        If Me.VolumeChangePercentage = Decimal.MinValue AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            If Me.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash Then
                Me.TradableInstrument.FetchHistorical = False
            End If
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
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
                        Dim firstCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, 9, 15, 0)
                        Dim secondCandle As Date = New Date(runningPayload.Year, runningPayload.Month, runningPayload.Day, 9, 16, 0)
                        If runningPayload.Date = Now.Date Then
                            If runningPayload = firstCandle OrElse runningPayload = secondCandle Then
                                currentDayVolumeSum += CType(XMinutePayloadConsumer.ConsumerPayloads(runningPayload), OHLCPayload).Volume.Value
                            End If
                        ElseIf runningPayload.Date < Now.Date Then
                            If runningPayload = firstCandle OrElse runningPayload = secondCandle Then
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
                        If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target AndAlso order.Status = IOrder.TypeOfStatus.Complete Then
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

    Private Function GetSignalCandleATR() As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If _signalCandle IsNot Nothing Then
            Dim atrConsumer As ATRConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyATRConsumer)
            If atrConsumer IsNot Nothing AndAlso atrConsumer.ConsumerPayloads IsNot Nothing AndAlso atrConsumer.ConsumerPayloads.Count > 0 AndAlso
                atrConsumer.ConsumerPayloads.ContainsKey(_signalCandle.SnapshotDateTime) Then
                ret = Math.Round(CType(atrConsumer.ConsumerPayloads(_signalCandle.SnapshotDateTime), ATRConsumer.ATRPayload).ATR.Value, 2)
            End If
        End If
        Return ret
    End Function

    Private Function IsSignalCandle(ByVal candle As OHLCPayload, ByVal currentTick As ITick) As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)
        Dim ret As Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction) = Nothing
        If candle IsNot Nothing AndAlso candle.PreviousPayload IsNot Nothing AndAlso
            Not candle.DeadCandle AndAlso Not candle.PreviousPayload.DeadCandle Then
            Dim userSettings As VolumeSpikeUserInputs = Me.ParentStrategy.UserSettings
            If _potentialHighEntryPrice = Decimal.MinValue AndAlso _potentialLowEntryPrice = Decimal.MinValue Then
                If IsCandleHalf(candle) AndAlso candle.CandleRange > CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.CandleHalf
                    _targetMultiplier = userSettings.TargetMultiplier
                ElseIf IsPinBar(candle) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.PinBar
                    _targetMultiplier = Math.Floor(userSettings.TargetMultiplier - userSettings.TargetMultiplier * 25 / 100)
                ElseIf IsTweezerPattern(candle) Then
                    _potentialHighEntryPrice = candle.HighPrice.Value
                    _potentialLowEntryPrice = candle.LowPrice.Value
                    _signalCandle = candle
                    _signalType = TypeOfSignal.TweezerPattern
                    _targetMultiplier = Math.Floor(userSettings.TargetMultiplier - userSettings.TargetMultiplier * 25 / 100)
                End If
            End If

            If _potentialHighEntryPrice <> Decimal.MinValue AndAlso _potentialLowEntryPrice <> Decimal.MinValue Then
                If _entryChanged Then
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.High >= middlePoint + range * 60 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, middlePoint, IOrder.TypeOfTransaction.Buy)
                    ElseIf currentTick.Open <= middlePoint - range * 60 / 100 Then
                        ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, middlePoint, IOrder.TypeOfTransaction.Sell)
                    End If
                Else
                    Dim tradeDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
                    Dim middlePoint As Decimal = (_potentialHighEntryPrice + _potentialLowEntryPrice) / 2
                    Dim range As Decimal = _potentialHighEntryPrice - middlePoint
                    If currentTick.Open >= middlePoint + range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Buy
                    ElseIf currentTick.Open <= middlePoint - range * 30 / 100 Then
                        tradeDirection = IOrder.TypeOfTransaction.Sell
                    End If
                    Select Case _signalType
                        Case TypeOfSignal.PinBar, TypeOfSignal.TweezerPattern
                            If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialHighEntryPrice - ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing), IOrder.TypeOfTransaction.Buy)
                            ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialLowEntryPrice + ConvertFloorCeling(GetSignalCandleATR(), Me.TradableInstrument.TickSize, RoundOfType.Celing), IOrder.TypeOfTransaction.Sell)
                            End If
                        Case TypeOfSignal.CandleHalf
                            If tradeDirection = IOrder.TypeOfTransaction.Buy Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialHighEntryPrice, _potentialLowEntryPrice, IOrder.TypeOfTransaction.Buy)
                            ElseIf tradeDirection = IOrder.TypeOfTransaction.Sell Then
                                ret = New Tuple(Of Boolean, Decimal, Decimal, IOrder.TypeOfTransaction)(True, _potentialLowEntryPrice, _potentialHighEntryPrice, IOrder.TypeOfTransaction.Sell)
                            End If
                    End Select
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsTweezerPattern(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        If candle.PreviousPayload.CandleColor = Color.Red Then
            If candle.PreviousPayload.CandleWicks.Top <= candle.PreviousPayload.CandleRange * 50 / 100 AndAlso
                candle.PreviousPayload.CandleWicks.Bottom <= candle.PreviousPayload.CandleRange * 25 / 100 Then
                Dim buffer As Decimal = CalculateBuffer(candle.PreviousPayload.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                If candle.CandleColor = Color.Green AndAlso candle.HighPrice.Value >= candle.PreviousPayload.HighPrice.Value - buffer Then
                    If candle.CandleWicks.Top <= candle.CandleRange * 25 / 100 AndAlso
                        candle.CandleWicks.Bottom <= candle.CandleRange * 50 / 100 Then
                        ret = True
                    End If
                End If
            End If
        ElseIf candle.PreviousPayload.CandleColor = Color.Green Then
            If candle.PreviousPayload.CandleWicks.Top <= candle.PreviousPayload.CandleRange * 25 / 100 AndAlso
                candle.PreviousPayload.CandleWicks.Bottom <= candle.PreviousPayload.CandleRange * 50 / 100 Then
                Dim buffer As Decimal = CalculateBuffer(candle.PreviousPayload.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
                If candle.CandleColor = Color.Red AndAlso candle.LowPrice.Value <= candle.PreviousPayload.LowPrice.Value + buffer Then
                    If candle.CandleWicks.Top <= candle.CandleRange * 50 / 100 AndAlso
                        candle.CandleWicks.Bottom <= candle.CandleRange * 25 / 100 Then
                        ret = True
                    End If
                End If
            End If
        End If
        Return ret
    End Function

    Private Function IsCandleHalf(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        Dim middlePoint As Decimal = (candle.PreviousPayload.HighPrice.Value + candle.PreviousPayload.LowPrice.Value) / 2
        If candle.SnapshotDateTime >= Me.ParentStrategy.UserSettings.TradeStartTime AndAlso
            candle.CandleRange <= candle.PreviousPayload.CandleRange / 2 AndAlso
            (candle.HighPrice.Value <= ConvertFloorCeling(middlePoint, Me.TradableInstrument.TickSize, RoundOfType.Floor) OrElse
            candle.LowPrice.Value >= ConvertFloorCeling(middlePoint, Me.TradableInstrument.TickSize, RoundOfType.Celing)) Then
            ret = True
        End If
        Return ret
    End Function

    Private Function IsPinBar(ByVal candle As OHLCPayload) As Boolean
        Dim ret As Boolean = False
        Dim candleHighBuffer As Decimal = CalculateBuffer(candle.HighPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
        Dim candleLowBuffer As Decimal = CalculateBuffer(candle.LowPrice.Value, Me.TradableInstrument.TickSize, RoundOfType.Floor)
        If candle.CandleWicks.Bottom >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) AndAlso
            candle.Volume.Value >= candle.PreviousPayload.Volume.Value AndAlso
            candle.LowPrice.Value < candle.PreviousPayload.LowPrice.Value Then
            Dim dayLow As Decimal = GetDayLow(candle)
            If dayLow <> Decimal.MinValue AndAlso candle.LowPrice.Value <= dayLow + CalculateBuffer(dayLow, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                ret = True
            End If
        ElseIf candle.CandleWicks.Top >= ConvertFloorCeling(candle.CandleRange * 50 / 100, Me.TradableInstrument.TickSize, RoundOfType.Celing) AndAlso
            candle.Volume.Value >= candle.PreviousPayload.Volume.Value AndAlso
            candle.HighPrice.Value > candle.PreviousPayload.HighPrice.Value Then
            Dim dayHigh As Decimal = GetDayHigh(candle)
            If dayHigh <> Decimal.MinValue AndAlso candle.HighPrice.Value >= dayHigh - CalculateBuffer(dayHigh, Me.TradableInstrument.TickSize, RoundOfType.Floor) Then
                ret = True
            End If
        End If
        Return ret
    End Function

    Private Function GetDayHigh(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
            Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                         If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                             Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                         Else
                                                                                                             Return Nothing
                                                                                                         End If
                                                                                                     End Function)

            If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                ret = XMinutePayloadConsumer.ConsumerPayloads.Values.Max(Function(x)
                                                                             Dim y As OHLCPayload = x
                                                                             If y.SnapshotDateTime.Date = Now.Date AndAlso
                                                                              y.SnapshotDateTime <= candle.SnapshotDateTime Then
                                                                                 Return y.HighPrice.Value
                                                                             Else
                                                                                 Return Decimal.MinValue
                                                                             End If
                                                                         End Function)
            End If
        End If
        Return ret
    End Function

    Private Function GetDayLow(ByVal candle As OHLCPayload) As Decimal
        Dim ret As Decimal = Decimal.MinValue
        If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
            Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                         If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                             Return CType(x, PayloadToChartConsumer).Timeframe = Me.ParentStrategy.UserSettings.SignalTimeFrame
                                                                                                         Else
                                                                                                             Return Nothing
                                                                                                         End If
                                                                                                     End Function)

            If XMinutePayloadConsumer IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                ret = XMinutePayloadConsumer.ConsumerPayloads.Values.Min(Function(x)
                                                                             Dim y As OHLCPayload = x
                                                                             If y.SnapshotDateTime.Date = Now.Date AndAlso
                                                                              y.SnapshotDateTime <= candle.SnapshotDateTime Then
                                                                                 Return y.LowPrice.Value
                                                                             Else
                                                                                 Return Decimal.MaxValue
                                                                             End If
                                                                         End Function)
            End If
        End If
        Return ret
    End Function

    Enum TypeOfSignal
        CandleHalf = 1
        PinBar
        TweezerPattern
        None
    End Enum

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
