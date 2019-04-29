Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports KiteConnect
Imports NLog

Namespace Subscriber

    Public Class ZerodhaInstrumentSubscriber
        Inherits APIInstrumentSubscriber
#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Sub OnTickerConnect()
            OnHeartbeat("Ticker, connected")
        End Sub
        Public Sub OnTickerClose()
            OnHeartbeat("Ticker, closed")
        End Sub
        Public Sub OnTickerError(message As String)
            OnHeartbeat(String.Format("Ticker, Error:{0}", message))
        End Sub
        Public Sub OnTickerNoReconnect()
            OnHeartbeat("Ticker, not Reconnecting")
        End Sub
        Public Sub OnTickerReconnect()
            OnHeartbeat("Ticker, reconnecting")
        End Sub
        Public Async Sub OnTickerTickAsync(ByVal tickData As Tick)
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0).ConfigureAwait(False)
            If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 Then
                Dim runningTick As New ZerodhaTick() With {.WrappedTick = tickData}
                For Each runningStrategyInstrument In _subscribedStrategyInstruments(tickData.InstrumentToken)
                    runningStrategyInstrument.ProcessTickAsync(runningTick)
                Next
            End If
        End Sub
        Public Async Sub OnTickerOrderUpdateAsync(orderData As Order)
            Await Task.Delay(0).ConfigureAwait(False)
            'If _todaysInstrumentsForOHLStrategy IsNot Nothing AndAlso _todaysInstrumentsForOHLStrategy.Count > 0 Then
            '    _todaysInstrumentsForOHLStrategy(orderData.InstrumentToken).StrategyWorker.ConsumedOrderUpdateAsync(orderData)
            'End If
            'OnHeartbeat(String.Format("OrderUpdate {0}", Utils.JsonSerialize(orderData)))
        End Sub
        Public Sub New(ByVal apiAdapter As APIAdapter, ByVal canceller As CancellationTokenSource)
            MyBase.New(apiAdapter, canceller)
        End Sub
        Public Overrides Async Function RunAdditionalStrategyTriggersAsync() As Task
            While True
                Await Task.Delay(10000).ConfigureAwait(False)
                If _subscribedStrategyInstruments IsNot Nothing Then
                    For Each runningStrategyInstruments In _subscribedStrategyInstruments.Values
                        For Each runningStrategyInstrument In runningStrategyInstruments
                            runningStrategyInstrument.RunDirectAsync()
                        Next
                    Next
                End If
            End While
        End Function

    End Class
End Namespace