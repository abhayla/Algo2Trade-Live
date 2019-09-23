Imports NLog
Imports System.Threading
Imports Utilities.Numbers
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Entities.Indicators
Imports Utilities.Network
Imports System.Net.Http

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

    Private _lastTick As ITick = Nothing
    Private _currentDayOpen As Decimal = Decimal.MinValue
    Private _usableATR As Decimal = Decimal.MinValue
    Private _longEntryAllowed As Boolean = False
    Private _shortEntryAllowed As Boolean = False
    Private ReadOnly _levelPercentage As Decimal = 30
    Private ReadOnly _ATRMultiplier As Decimal = 1.5
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
                'If Me.EligibleToTakeTrade Then
                '    'Force Cancel block strat
                '    If IsAnyTradeTargetReached() Then
                '        Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                '    End If
                '    'Force Cancel block end
                '    _cts.Token.ThrowIfCancellationRequested()
                '    'Place Order block start
                '    Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                '    If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                '        Await ExecuteCommandAsync(ExecuteCommands.PlaceBOSLMISOrder, Nothing).ConfigureAwait(False)
                '    End If
                '    'Place Order block end
                '    _cts.Token.ThrowIfCancellationRequested()
                '    ''Modify Order block start
                '    'Dim modifyStoplossOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(False).ConfigureAwait(False)
                '    'If modifyStoplossOrderTrigger IsNot Nothing AndAlso modifyStoplossOrderTrigger.Count > 0 Then
                '    '    Await ExecuteCommandAsync(ExecuteCommands.ModifyStoplossOrder, Nothing).ConfigureAwait(False)
                '    'End If
                '    ''Modify Order block end
                '    _cts.Token.ThrowIfCancellationRequested()
                '    'Exit Order block start
                '    Dim exitOrderTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                '    If exitOrderTrigger IsNot Nothing AndAlso exitOrderTrigger.Count > 0 Then
                '        Await ExecuteCommandAsync(ExecuteCommands.CancelBOOrder, Nothing).ConfigureAwait(False)
                '    End If
                '    'Exit Order block end
                '    _cts.Token.ThrowIfCancellationRequested()
                'End If
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
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Throw New NotImplementedException()
    End Function

    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
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
