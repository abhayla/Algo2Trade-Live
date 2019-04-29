Imports NLog
Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.ChartHandler.Indicator

Namespace ChartHandler.ChartStyle
    Public MustInherit Class Chart

#Region "Events/Event handlers"
        Public Event DocumentDownloadCompleteEx(ByVal source As List(Of Object))
        Public Event DocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
        Public Event HeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
        Public Event WaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadCompleteEx(ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentDownloadCompleteEx(source)
        End Sub
        Protected Overridable Sub OnDocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, source)
        End Sub
        Protected Overridable Sub OnHeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent HeartbeatEx(msg, source)
        End Sub
        Protected Overridable Sub OnWaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent WaitingForEx(elapsedSecs, totalSecs, msg, source)
        End Sub
        Protected Overridable Sub OnDocumentDownloadComplete()
            RaiseEvent DocumentDownloadCompleteEx(New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnHeartbeat(ByVal msg As String)
            RaiseEvent HeartbeatEx(msg, New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
            RaiseEvent WaitingForEx(elapsedSecs, totalSecs, msg, New List(Of Object) From {Me})
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Property ParentController As APIStrategyController
        Protected _parentInstrument As IInstrument
        Protected _subscribedStrategyInstruments As IEnumerable(Of StrategyInstrument)
        Protected _cts As New CancellationTokenSource
        Protected _historicalLock As Integer
        Protected _tickLock As Integer
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                      ByVal assoicatedParentInstrument As IInstrument,
                      ByVal associatedStrategyInstruments As IEnumerable(Of StrategyInstrument),
                      ByVal canceller As CancellationTokenSource)
            Me.ParentController = associatedParentController
            _parentInstrument = assoicatedParentInstrument
            _subscribedStrategyInstruments = associatedStrategyInstruments
            _cts = canceller
        End Sub
        Public Property IndicatorCreator As IndicatorManeger
        Public MustOverride Async Function GetChartFromHistoricalAsync(ByVal historicalCandlesJSONDict As Dictionary(Of String, Object)) As Task
        Public MustOverride Async Function GetChartFromTickAsync(ByVal tickData As ITick) As Task
        Public MustOverride Async Function ConvertTimeframeAsync(ByVal timeframe As Integer, ByVal currentPayload As OHLCPayload, ByVal outputConsumer As PayloadToChartConsumer) As Task(Of Date)
    End Class
End Namespace