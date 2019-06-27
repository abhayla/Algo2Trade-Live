Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Entities.UserSettings
Imports NLog

Namespace Strategies
    Public MustInherit Class Strategy

#Region "Events/Event handlers"
        'This will launch the Ex events so that source is included, but will handle normal events from all the objects that it calls and convert into Ex events
        Public Event DocumentDownloadCompleteEx(ByVal source As List(Of Object))
        Public Event DocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
        Public Event HeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
        Public Event WaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
        Public Event NewItemAdded(ByVal item As ActivityDashboard)
        Public Event EndOfTheDay(ByVal runningStrategy As Strategy)
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadCompleteEx(ByVal source As List(Of Object))
            If source Is Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentDownloadCompleteEx(source)
        End Sub
        Protected Overridable Sub OnDocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
            If source Is Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, source)
        End Sub
        Protected Overridable Sub OnHeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
            If source Is Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            RaiseEvent HeartbeatEx(msg, source)
        End Sub
        Protected Overridable Sub OnWaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
            If source Is Nothing Then source = New List(Of Object)
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
        Protected Overridable Sub OnNewItemAdded(ByVal item As ActivityDashboard)
            If item IsNot Nothing Then
                RaiseEvent NewItemAdded(item)
            End If
        End Sub
        Protected Overridable Sub OnEndOfTheDay(ByVal runningStrategy As Strategy)
            RaiseEvent EndOfTheDay(runningStrategy)
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region
        Public ReadOnly Property StrategyIdentifier As String
        Public Property TradableInstrumentsAsPerStrategy As IEnumerable(Of IInstrument)
        Public Property TradableStrategyInstruments As IEnumerable(Of StrategyInstrument)
        Public Property UserSettings As StrategyUserInputs = Nothing
        Public Property ParentController As APIStrategyController
        Public Property SignalManager As SignalStateManager
        Public Property ExitAllTrades As Boolean
        Public ReadOnly Property MaxNumberOfDaysForHistoricalFetch As Integer
        Public ReadOnly Property IsStrategyCandleStickBased As Boolean
        Public ReadOnly Property IsTickPopulationNeeded As Boolean
        Public Property IsFirstTimeInformationCollected As Boolean
        Public Property MaxDrawUp As Decimal = Decimal.MinValue
        Public Property MaxDrawDown As Decimal = Decimal.MaxValue
        Public Property ExportCSV As Boolean

        Protected _cts As CancellationTokenSource
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal associatedStrategyIdentifier As String,
                       ByVal isStrategyCandleStickBased As Boolean,
                       ByVal userSettings As StrategyUserInputs,
                       ByVal maxNumberOfDaysForHistoricalFetch As Integer,
                       ByVal canceller As CancellationTokenSource,
                       Optional ByVal isTickPopulationNeeded As Boolean = False)
            Me.ParentController = associatedParentController
            Me.StrategyIdentifier = associatedStrategyIdentifier
            Me.IsStrategyCandleStickBased = isStrategyCandleStickBased
            Me.UserSettings = userSettings
            Me.MaxNumberOfDaysForHistoricalFetch = maxNumberOfDaysForHistoricalFetch
            Me.IsFirstTimeInformationCollected = False
            Me.IsTickPopulationNeeded = isTickPopulationNeeded
            Me.ExportCSV = True
            Me.SignalManager = New SignalStateManager(associatedParentController, Me, canceller)
            AddHandler Me.SignalManager.NewItemAdded, AddressOf OnNewItemAdded
            _cts = canceller
        End Sub

#Region "Public Functions"
        Public Function GetNumberOfActiveInstruments() As Integer
            Dim instrumentCount As Integer = 0
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningStrategyInstrument In TradableStrategyInstruments
                    If runningStrategyInstrument.IsActiveInstrument() Then
                        instrumentCount += 1
                    End If
                Next
            End If
            Return instrumentCount
        End Function

        Public Function GetTotalPL() As Decimal
            Dim plOfDay As Decimal = 0
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningStrategyInstrument In TradableStrategyInstruments
                    plOfDay += runningStrategyInstrument.GetOverallPL()
                Next
            End If
            Return plOfDay
        End Function
        Public Function GetTotalPLAfterBrokerage() As Decimal
            Dim plOfDay As Decimal = 0
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningStrategyInstrument In TradableStrategyInstruments
                    plOfDay += runningStrategyInstrument.GetOverallPLAfterBrokerage()
                Next
            End If
            MaxDrawUp = Math.Max(MaxDrawUp, plOfDay)
            MaxDrawDown = Math.Min(MaxDrawDown, plOfDay)
            Return plOfDay
        End Function
#End Region

#Region "Public Overridable Functions"
        Public Overridable Async Function SubscribeAsync(ByVal usableTicker As APITicker, ByVal usableFetcher As APIHistoricalDataFetcher) As Task
            logger.Debug("SubscribeAsync, usableTicker:{0}, usableFetcher:{1}", usableTicker.ToString, usableFetcher.ToString)
            _cts.Token.ThrowIfCancellationRequested()
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                Dim runningInstrumentIdentifiers As List(Of String) = Nothing
                For Each runningTradableStrategyInstruments In TradableStrategyInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If Not runningTradableStrategyInstruments.IsPairInstrument Then
                        If runningInstrumentIdentifiers Is Nothing Then runningInstrumentIdentifiers = New List(Of String)
                        runningInstrumentIdentifiers.Add(runningTradableStrategyInstruments.TradableInstrument.InstrumentIdentifier)
                    End If
                Next
                _cts.Token.ThrowIfCancellationRequested()
                Await usableTicker.SubscribeAsync(runningInstrumentIdentifiers).ConfigureAwait(False)
                If Me.IsStrategyCandleStickBased Then
                    Await usableFetcher.SubscribeAsync(TradableInstrumentsAsPerStrategy, Me.MaxNumberOfDaysForHistoricalFetch).ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()

                'Activity Dashboard modify
                Me.SignalManager.DeSerializeActivityCollection()
                If Me.SignalManager IsNot Nothing AndAlso
                    Me.SignalManager.ActivityDetails IsNot Nothing AndAlso Me.SignalManager.ActivityDetails.Count > 0 Then
                    For Each instrumentActivity In Me.SignalManager.ActivityDetails
                        Dim activityTag As String = Convert.ToInt64(instrumentActivity.Key, 16)
                        Dim instrumentMappedNumber As String = activityTag.Substring(1, 3)
                        Dim instrumentIdentifiers As IEnumerable(Of KeyValuePair(Of String, String)) =
                            Me.ParentController.InstrumentMappingTable.Where(Function(x)
                                                                                 Return x.Value = Val(instrumentMappedNumber)
                                                                             End Function)
                        If instrumentIdentifiers IsNot Nothing AndAlso instrumentIdentifiers.Count > 0 Then
                            Dim currentStrategyInstruments As IEnumerable(Of StrategyInstrument) =
                                Me.TradableStrategyInstruments.Where(Function(x)
                                                                         Return x.TradableInstrument.InstrumentIdentifier = instrumentIdentifiers.FirstOrDefault.Key
                                                                     End Function)
                            If currentStrategyInstruments IsNot Nothing AndAlso currentStrategyInstruments.Count > 0 Then
                                If instrumentActivity.Value.TradingSymbol = currentStrategyInstruments.FirstOrDefault.TradableInstrument.TradingSymbol Then
                                    instrumentActivity.Value.ParentStrategyInstrument = currentStrategyInstruments.FirstOrDefault
                                Else
                                    Me.SignalManager.ActivityDetails.TryRemove(instrumentActivity.Key, instrumentActivity.Value)
                                End If
                            Else
                                Me.SignalManager.ActivityDetails.TryRemove(instrumentActivity.Key, instrumentActivity.Value)
                            End If
                        Else
                            Me.SignalManager.ActivityDetails.TryRemove(instrumentActivity.Key, instrumentActivity.Value)
                        End If
                    Next

                End If
            End If
        End Function
        Public Overridable Async Function ForceExitAllTradesAsync() As Task
            'logger.Debug("ForceExitAllTrades, parameters:Nothing")
            Try
                Dim delayCtr As Integer = 0
                While True
                    If Me.ParentController.OrphanException IsNot Nothing Then
                        Throw Me.ParentController.OrphanException
                    End If
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim triggerResponse As Tuple(Of Boolean, String) = IsTriggerReceivedForExitAllOrders()
                    If triggerResponse IsNot Nothing AndAlso triggerResponse.Item1 AndAlso
                        TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                        If delayCtr = 5 Then
                            delayCtr = 0
                            Dim exitAllResponse As Boolean = False
                            For Each runningStrategyInstrument In TradableStrategyInstruments
                                exitAllResponse = exitAllResponse Or Await runningStrategyInstrument.ForceExitAllTradesAsync(triggerResponse.Item2).ConfigureAwait(False)
                            Next
                            If Me.ExitAllTrades AndAlso exitAllResponse Then
                                OnHeartbeatEx("All active trades exited", New List(Of Object) From {Me})
                            ElseIf Me.ExitAllTrades Then
                                OnHeartbeatEx(String.Format("No active trades to exit"), New List(Of Object) From {Me})
                            End If
                            Me.ExitAllTrades = False
                            If triggerResponse.Item2.ToUpper.Contains("EOD") Then
                                OnEndOfTheDay(Me)
                            End If
                        End If
                        delayCtr += 1
                    Else
                        delayCtr = 0
                    End If
                    Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                End While
            Catch ex As Exception
                'To log exceptions getting created from this function as the bubble up of the exception
                'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
                logger.Error("Strategy:{0}, error:{1}", Me.ToString, ex.ToString)
                Throw ex
            End Try
        End Function
        Public Overridable Async Function ProcessOrderAsync(ByVal orderData As IBusinessOrder) As Task
            'logger.Debug("ProcessOrderAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningTradableStrategyInstrument In TradableStrategyInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If runningTradableStrategyInstrument.TradableInstrument.InstrumentIdentifier = orderData.ParentOrder.InstrumentIdentifier Then
                        If orderData.ParentOrder.Tag IsNot Nothing Then
                            Dim decodedTag As String = Convert.ToInt64(orderData.ParentOrder.Tag, 16)
                            If decodedTag.Substring(0, 1).Equals(Me.StrategyIdentifier) Then
                                Await runningTradableStrategyInstrument.ProcessOrderAsync(orderData).ConfigureAwait(False)
                            End If
                        End If
                    End If
                Next
            End If
        End Function
        Public Overridable Async Function ProcessHoldingAsync(ByVal holdingData As IHolding) As Task
            'logger.Debug("ProcessHoldingAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningTradableStrategyInstrument In TradableStrategyInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If runningTradableStrategyInstrument.TradableInstrument.InstrumentIdentifier = holdingData.InstrumentIdentifier Then
                        Await runningTradableStrategyInstrument.ProcessHoldingAsync(holdingData).ConfigureAwait(False)
                    End If
                Next
            End If
        End Function
        Public Overridable Async Function ProcessPositionAsync(ByVal positionData As IPosition) As Task
            'logger.Debug("ProcessPositionAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningTradableStrategyInstrument In TradableStrategyInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If runningTradableStrategyInstrument.TradableInstrument.InstrumentIdentifier = positionData.InstrumentIdentifier Then
                        Await runningTradableStrategyInstrument.ProcessPositionAsync(positionData).ConfigureAwait(False)
                    End If
                Next
            End If
        End Function
#End Region

#Region "Public MustOverride Functions"
        Public MustOverride Async Function CreateTradableStrategyInstrumentsAsync(ByVal allInstruments As IEnumerable(Of IInstrument)) As Task(Of Boolean)
        Public MustOverride Overrides Function ToString() As String
        Public MustOverride Async Function MonitorAsync() As Task
        Protected MustOverride Function IsTriggerReceivedForExitAllOrders() As Tuple(Of Boolean, String)
#End Region

#Region "Private Functions"
        Protected Async Function GetHoldingsDataAsync() As Task
            Dim holdingDetails As Concurrent.ConcurrentBag(Of IHolding) = Await Me.ParentController.GetHoldingDetailsAsync().ConfigureAwait(False)
            If holdingDetails IsNot Nothing AndAlso holdingDetails.Count > 0 Then
                For Each holdingData In holdingDetails
                    _cts.Token.ThrowIfCancellationRequested()
                    Await Me.ProcessHoldingAsync(holdingData).ConfigureAwait(False)
                Next
            End If
        End Function
#End Region

    End Class
End Namespace
