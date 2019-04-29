Imports System.Threading
Imports Algo2TradeCore.Controller
Imports NLog

Namespace Adapter
    Public MustInherit Class APIInformationCollector

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

        Protected _cts As CancellationTokenSource
        Public Property ParentController As APIStrategyController

        Protected _pollingFrequency As Integer
        Protected _isPollRunning As Boolean
        Protected _stopPollRunning As Boolean
        Public Sub New(ByVal associatedParentcontroller As APIStrategyController,
                       ByVal pollingFrequency As Integer,
                       ByVal canceller As CancellationTokenSource)
            Me.ParentController = associatedParentcontroller
            _pollingFrequency = pollingFrequency
            _cts = canceller
        End Sub
        Public MustOverride Async Function ConnectCollectorAsync() As Task
        Public MustOverride Overrides Function ToString() As String
        Public MustOverride Function IsConnected() As Boolean
        Public MustOverride Async Function CloseCollectorIfConnectedAsync(ByVal forceClose As Boolean) As Task
        Public Sub RefreshCancellationToken(ByVal canceller As CancellationTokenSource)
            _cts = canceller
        End Sub
        Protected MustOverride Async Function StartPollingAsync() As Task
        Protected MustOverride Async Function GetOrderUpdatesAsync() As Task
        Public Enum InformationType
            GetOrderDetails = 1
            None
        End Enum
    End Class
End Namespace
