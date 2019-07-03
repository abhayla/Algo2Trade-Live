Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators
Imports Utilities.Numbers

Public Class TwoThirdStrategyInstrument
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
    Private _lastTick As ITick
    Private _lastDayFractalHighChange As Boolean = False
    Private _lastDayFractalLowChange As Boolean = False
    Private _eligibleToTakeTrade As Boolean = False
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
                {New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, TwoThirdUserInputs).ATRPeriod)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyATRConsumer = New ATRConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, TwoThirdUserInputs).ATRPeriod)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            Dim userSettings As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()

                'Dim activeOrder As IBusinessOrder = Me.GetActiveOrder(IOrder.TypeOfTransaction.None)
                ''Check Target block start
                'If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 AndAlso
                '    activeOrder.TargetOrder IsNot Nothing AndAlso activeOrder.TargetOrder.Count > 0 Then
                '    If activeOrder.TargetOrder.Count > 1 Then
                '        Throw New ApplicationException("Why target order greater than 1")
                '    Else
                '        Dim currentTick As ITick = Me.TradableInstrument.LastTick
                '        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                '            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                '            If currentTick.LastPrice >= target Then
                '                _lastTick = currentTick
                '                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                '                _lastExitCondition = "TARGET"
                '                _lastExitTime = Now
                '            End If
                '        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                '            Dim target As Decimal = activeOrder.TargetOrder.FirstOrDefault.AveragePrice
                '            If currentTick.LastPrice <= target Then
                '                _lastTick = currentTick
                '                Await ForceExitAllTradesAsync("Target reached").ConfigureAwait(False)
                '                _lastExitCondition = "TARGET"
                '                _lastExitTime = Now
                '            End If
                '        End If
                '    End If
                'End If
                ''Check Target block start

                '_cts.Token.ThrowIfCancellationRequested()
                ''Check Stoploss block start -------------- Only for paper trade
                'If activeOrder IsNot Nothing AndAlso activeOrder.SLOrder IsNot Nothing AndAlso activeOrder.SLOrder.Count > 0 Then
                '    If activeOrder.SLOrder.Count > 1 Then
                '        Throw New ApplicationException("Why sl order greater than 1")
                '    Else
                '        Dim currentTick As ITick = Me.TradableInstrument.LastTick
                '        If activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                '            If currentTick.LastPrice <= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                '                _lastTick = currentTick
                '                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                '                _lastExitCondition = "STOPLOSS"
                '                _lastExitTime = Now
                '            End If
                '        ElseIf activeOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                '            If currentTick.LastPrice >= activeOrder.SLOrder.FirstOrDefault.TriggerPrice Then
                '                _lastTick = currentTick
                '                Await ForceExitAllTradesAsync("Stoploss reached").ConfigureAwait(False)
                '                _lastExitCondition = "STOPLOSS"
                '                _lastExitTime = Now
                '            End If
                '        End If
                '    End If
                'End If
                ''Check Stoploss block end

                '_cts.Token.ThrowIfCancellationRequested()
                ''Place Order block start
                'Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                'If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                '    Dim reverseExit As Boolean = False
                '    If userSettings.ReverseTrade AndAlso _lastPlacedOrder IsNot Nothing AndAlso
                '        _lastPlacedOrder.ParentOrder.TransactionType <> placeOrderTrigger.Item2.EntryDirection AndAlso
                '        _lastPlacedOrder.SLOrder IsNot Nothing AndAlso _lastPlacedOrder.SLOrder.Count > 0 Then
                '        Await ForceExitSpecificTradeAsync(_lastPlacedOrder.SLOrder.FirstOrDefault, "Force exit order for reverse entry").ConfigureAwait(False)
                '        reverseExit = True
                '    End If

                '    If _lastExitCondition = "TARGET" Then
                '        If Not _lastPrevPayloadOuterPlaceOrder = placeOrderTrigger.Item2.SignalCandle.ToString Then
                '            _lastPrevPayloadOuterPlaceOrder = placeOrderTrigger.Item2.SignalCandle.ToString
                '            logger.Debug("****Place Order******Can not take trade as target reached previous trade********")
                '        End If
                '    ElseIf Not IsActiveInstrument() Then
                '        Dim modifiedPlaceOrderTrigger As Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String) = New Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)(placeOrderTrigger.Item1, Me, placeOrderTrigger.Item2, placeOrderTrigger.Item3)
                '        Dim placeOrderResponse As IBusinessOrder = Nothing
                '        If reverseExit Then
                '            placeOrderResponse = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger).ConfigureAwait(False)
                '        Else
                '            placeOrderResponse = Await TakeBOPaperTradeAsync(modifiedPlaceOrderTrigger, True, _lastTick).ConfigureAwait(False)
                '        End If
                '        _lastPlacedOrder = placeOrderResponse
                '        _eligibleToTakeTrade = False
                '        If placeOrderResponse IsNot Nothing Then
                '            Dim potentialTargetPL As Decimal = 0
                '            Dim potentialStoplossPL As Decimal = 0
                '            Dim potentialEntry As Decimal = 0
                '            Dim fractal As Decimal = Math.Round(Val(placeOrderTrigger.Item2.Supporting(0)), 2)
                '            Dim atr As Decimal = Math.Round(Val(placeOrderTrigger.Item2.Supporting(1)), 2)
                '            If placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                '                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                '                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.Quantity)
                '                potentialEntry = fractal + ConvertFloorCeling(atr * 10 / 100, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                '            ElseIf placeOrderResponse.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                '                potentialTargetPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                '                potentialStoplossPL = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice, placeOrderResponse.ParentOrder.AveragePrice, placeOrderResponse.ParentOrder.Quantity)
                '                potentialEntry = fractal - ConvertFloorCeling(atr * 10 / 100, Me.TradableInstrument.TickSize, NumberManipulation.RoundOfType.Celing)
                '            End If
                '            Dim message As String = String.Format("Order Placed. Trading Symbol:{0}, Direction:{1}, Signal Candle Time:{2}, Fractal:{3}, ATR:{4}, Entry Price:{5}, Quantity:{6}, Stoploss Price:{7}, Target Price:{8}, Potential Entry:{9}, Potential Target PL:{10}, Potential Stoploss PL:{11}, LTP:{12}, Timestamp:{13}",
                '                                                  Me.TradableInstrument.TradingSymbol,
                '                                                  placeOrderResponse.ParentOrder.TransactionType.ToString,
                '                                                  placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToShortTimeString,
                '                                                  fractal,
                '                                                  atr,
                '                                                  placeOrderResponse.ParentOrder.AveragePrice,
                '                                                  placeOrderResponse.ParentOrder.Quantity,
                '                                                  placeOrderResponse.SLOrder.FirstOrDefault.TriggerPrice,
                '                                                  placeOrderResponse.TargetOrder.FirstOrDefault.AveragePrice,
                '                                                  potentialEntry,
                '                                                  Math.Round(potentialTargetPL, 2),
                '                                                  Math.Round(potentialStoplossPL, 2),
                '                                                  _lastTick.LastPrice,
                '                                                  Now)
                '            GenerateTelegramMessageAsync(message)
                '        End If
                '    End If
                'End If
                ''Place Order block end

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

    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
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

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As TwoThirdUserInputs = Me.ParentStrategy.UserSettings
        If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
            userInputs.TelegramChatID IsNot Nothing AndAlso Not userInputs.TelegramChatID.Trim = "" Then
            Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramChatID, _cts)
                Dim encodedString As String = Utilities.Strings.EncodeString(message)
                Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
            End Using
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
