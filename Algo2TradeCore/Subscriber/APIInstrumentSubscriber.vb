Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Strategies
Imports NLog

Namespace Subscriber
    Public MustInherit Class APIInstrumentSubscriber

#Region "Events/Event handlers"
        Public Event DocumentDownloadComplete()
        Public Event DocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
        Public Event Heartbeat(ByVal msg As String)
        Public Event WaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadComplete()
            RaiseEvent DocumentDownloadComplete()
        End Sub
        Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
            RaiseEvent DocumentRetryStatus(currentTry, totalTries)
        End Sub
        Protected Overridable Sub OnHeartbeat(ByVal msg As String)
            RaiseEvent Heartbeat(msg)
        End Sub
        Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
            RaiseEvent WaitingFor(elapsedSecs, totalSecs, msg)
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Protected _apiAdapter As APIAdapter
        Protected _cts As CancellationTokenSource
        Protected _subscribedStrategyInstruments As Dictionary(Of String, List(Of StrategyInstrument))
        Public SubcribedInstruments As List(Of String)
        Public Sub New(ByVal apiAdapter As APIAdapter, ByVal canceller As CancellationTokenSource)
            _apiAdapter = apiAdapter
            _cts = canceller
        End Sub
        Public Overridable Sub SubscribeStrategy(ByVal strategy As StrategyInstrument)
            AddHandler strategy.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            AddHandler strategy.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            AddHandler strategy.Heartbeat, AddressOf OnHeartbeat
            AddHandler strategy.WaitingFor, AddressOf OnWaitingFor

            If _subscribedStrategyInstruments Is Nothing Then _subscribedStrategyInstruments = New Dictionary(Of String, List(Of StrategyInstrument))
            Dim instrumentKey As String = strategy.TradableInstrument.InstrumentIdentifier
            Dim strategiesToBeSubscribedForThisInstrument As List(Of StrategyInstrument) = Nothing
            If _subscribedStrategyInstruments.ContainsKey(instrumentKey) Then
                strategiesToBeSubscribedForThisInstrument = _subscribedStrategyInstruments(instrumentKey)
            Else
                strategiesToBeSubscribedForThisInstrument = New List(Of StrategyInstrument)
                _subscribedStrategyInstruments.Add(instrumentKey, strategiesToBeSubscribedForThisInstrument)
            End If
            strategiesToBeSubscribedForThisInstrument.Add(strategy)

            If SubcribedInstruments Is Nothing Then SubcribedInstruments = New List(Of String)
            If SubcribedInstruments IsNot Nothing AndAlso SubcribedInstruments.Count > _apiAdapter.MaxInstrumentPerTicker Then
                Throw New ApplicationException(String.Format("Max instruments per ticker exceeded, allowed:{0}, existing:{1}", _apiAdapter.MaxInstrumentPerTicker, SubcribedInstruments.Count))
            End If

            If Not SubcribedInstruments.Contains(strategy.TradableInstrument.InstrumentIdentifier) Then
                SubcribedInstruments.Add(strategy.TradableInstrument.InstrumentIdentifier)
            Else
                logger.Debug("Neglected subscribing to instrument as it exists earlier from another strategy, instrument identifier:{0}, current startegy instrument:{1}", strategy.TradableInstrument.InstrumentIdentifier, strategy.ToString)
            End If
        End Sub
        Public MustOverride Async Function RunAdditionalStrategyTriggersAsync() As Task
    End Class
End Namespace