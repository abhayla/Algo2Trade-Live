Imports System.Net.Http
Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.ChartHandler.ChartStyle
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Exceptions
Imports NLog
Imports Utilities
Imports Utilities.ErrorHandlers
Imports Utilities.Numbers.NumberManipulation

Namespace Strategies
    Public MustInherit Class StrategyInstrument

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
            If TradableInstrument IsNot Nothing Then
                RaiseEvent HeartbeatEx(String.Format("{0}:{1}", TradableInstrument.InstrumentIdentifier, msg), source)
            Else
                RaiseEvent HeartbeatEx(String.Format("{0}:{1}", "No instrument", msg), source)
            End If
        End Sub
        Protected Overridable Sub OnWaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
            If source IsNot Nothing Then source = New List(Of Object)
            If source.Find(Function(x)
                               Return x.ToString.Equals(Me.ToString)
                           End Function) Is Nothing Then
                source.Add(Me)
            End If
            If TradableInstrument IsNot Nothing Then
                RaiseEvent WaitingForEx(elapsedSecs, totalSecs, String.Format("{0}-{1}", TradableInstrument.InstrumentIdentifier, msg), source)
            Else
                RaiseEvent WaitingForEx(elapsedSecs, totalSecs, String.Format("{0}-{1}", "No instrument", msg), source)
            End If
        End Sub
        Protected Overridable Sub OnDocumentDownloadComplete()
            RaiseEvent DocumentDownloadCompleteEx(New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, New List(Of Object) From {Me})
        End Sub
        Protected Overridable Sub OnHeartbeat(ByVal msg As String)
            If TradableInstrument IsNot Nothing Then
                RaiseEvent HeartbeatEx(String.Format("{0}:{1}", TradableInstrument.InstrumentIdentifier, msg), New List(Of Object) From {Me})
            Else
                RaiseEvent HeartbeatEx(String.Format("{0}:{1}", "No instrument", msg), New List(Of Object) From {Me})
            End If
        End Sub
        Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
            If TradableInstrument IsNot Nothing Then
                RaiseEvent WaitingForEx(elapsedSecs, totalSecs, String.Format("{0}-{1}", TradableInstrument.InstrumentIdentifier, msg), New List(Of Object) From {Me})
            Else
                RaiseEvent WaitingForEx(elapsedSecs, totalSecs, String.Format("{0}-{1}", "No instrument", msg), New List(Of Object) From {Me})
            End If
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

#Region "Enum"
        Public Enum ExecuteCommands
            PlaceBOLimitMISOrder = 1
            PlaceBOSLMISOrder
            PlaceCOMarketMISOrder
            PlaceRegularMarketMISOrder
            PlaceRegularLimitMISOrder
            PlaceRegularSLMMISOrder
            PlaceRegularMarketCNCOrder
            ModifyStoplossOrder
            ModifyTargetOrder
            CancelBOOrder
            CancelCOOrder
            CancelRegularOrder
            ForceCancelBOOrder
            ForceCancelCOOrder
            ForceCancelRegularOrder
        End Enum
        Protected Enum ExecuteCommandAction
            Take = 1
            DonotTake
            WaitAndTake
            None
        End Enum
#End Region

        Protected _cts As CancellationTokenSource
        Protected _APIAdapter As APIAdapter
        Protected _MaxReTries As Integer = 20
        Protected _WaitDurationOnConnectionFailure As TimeSpan = TimeSpan.FromSeconds(5)
        Protected _WaitDurationOnServiceUnavailbleFailure As TimeSpan = TimeSpan.FromSeconds(30)
        Protected _WaitDurationOnAnyFailure As TimeSpan = TimeSpan.FromSeconds(10)
        Protected _RMSException As AdapterBusinessException = Nothing
        Public Property ParentStrategy As Strategy
        Public Property TradableInstrument As IInstrument
        Public Property OrderDetails As Concurrent.ConcurrentDictionary(Of String, IBusinessOrder)
        Public Property HoldingDetails As IHolding
        Public Property PositionDetails As IPosition
        Public Property RawPayloadDependentConsumers As List(Of IPayloadConsumer)
        Public Property TickPayloadDependentConsumers As List(Of IPayloadConsumer)
        Public Property IsPairInstrument As Boolean
        Public Property StrategyExitAllTriggerd As Boolean
        Public Property DependendStrategyInstruments As IEnumerable(Of StrategyInstrument) 'Only used if it is a pair instrument
        Public Property ParentStrategyInstruments As IEnumerable(Of StrategyInstrument) 'Only used if it is a pair instrument
        Public Property PairConsumerProtection As Boolean 'Only used if it is a pair instrument

        Private _historicalLock As Integer = 0
        Private _TemporaryOrderCollection As Concurrent.ConcurrentBag(Of IOrder)

        Public Sub New(ByVal associatedInstrument As IInstrument,
                       ByVal associatedParentStrategy As Strategy,
                       ByVal isPairInstrument As Boolean,
                       ByVal canceller As CancellationTokenSource)
            TradableInstrument = associatedInstrument
            Me.ParentStrategy = associatedParentStrategy
            _cts = canceller
            OrderDetails = New Concurrent.ConcurrentDictionary(Of String, IBusinessOrder)
            Me.IsPairInstrument = isPairInstrument
            Me.PairConsumerProtection = True
            Me.StrategyExitAllTriggerd = False
        End Sub

#Region "Required Functions"
        Protected Function CalculateBuffer(ByVal price As Double, ByVal tickSize As Decimal, ByVal floorOrCeiling As RoundOfType) As Double
            'logger.Debug("CalculateBuffer, parameters:{0},{1}", price, floorOrCeiling)
            Dim bufferPrice As Double = Nothing
            'Assuming 1% target, we can afford to have buffer as 2.5% of that 1% target
            bufferPrice = ConvertFloorCeling(price * 0.01 * 0.025, tickSize, floorOrCeiling)
            Return bufferPrice
        End Function
        Public Function GetXMinuteCurrentCandle(ByVal timeFrame As Integer) As OHLCPayload
            Dim ret As OHLCPayload = Nothing
            If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                'Indibar
                'Dim XMinutePayloadConsumers As IEnumerable(Of IPayloadConsumer) = RawPayloadConsumers.Where(Function(x)
                '                                                                                                Return x.TypeOfConsumer = IPayloadConsumer.ConsumerType.Chart AndAlso
                '                                                                                                      CType(x, PayloadToChartConsumer).Timeframe = timeFrame
                '                                                                                            End Function)
                'Dim XMinutePayloadConsumer As PayloadToChartConsumer = Nothing
                'If XMinutePayloadConsumers IsNot Nothing AndAlso XMinutePayloadConsumers.Count > 0 Then
                '    XMinutePayloadConsumer = XMinutePayloadConsumers.FirstOrDefault
                'End If
                Dim XMinutePayloadConsumer As PayloadToChartConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                             If x.GetType Is GetType(PayloadToChartConsumer) Then
                                                                                                                 Return CType(x, PayloadToChartConsumer).Timeframe = timeFrame
                                                                                                             Else
                                                                                                                 Return Nothing
                                                                                                             End If
                                                                                                         End Function)

                If XMinutePayloadConsumer IsNot Nothing AndAlso
                    XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                    'Dim lastExistingPayloads As IEnumerable(Of KeyValuePair(Of Date, IPayload)) =
                    '    XMinutePayloadConsumer.ConsumerPayloads.Where(Function(y)
                    '                                                      Return Utilities.Time.IsDateTimeEqualTillMinutes(y.Key, XMinutePayloadConsumer.ConsumerPayloads.Keys.Max)
                    '                                                  End Function)

                    'If lastExistingPayloads IsNot Nothing AndAlso lastExistingPayloads.Count > 0 Then ret = lastExistingPayloads.LastOrDefault.Value
                    ret = XMinutePayloadConsumer.ConsumerPayloads(XMinutePayloadConsumer.ConsumerPayloads.Keys.Max)
                End If
            End If
            Return ret
        End Function
        Public Function GetXMinuteCurrentCandle() As PairPayload
            Dim ret As PairPayload = Nothing
            If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                Dim XMinutePayloadConsumer As PayloadToPairConsumer = RawPayloadDependentConsumers.Find(Function(x)
                                                                                                            Return x.GetType Is GetType(PayloadToPairConsumer)
                                                                                                        End Function)

                If XMinutePayloadConsumer IsNot Nothing AndAlso
                    XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 Then
                    ret = XMinutePayloadConsumer.ConsumerPayloads(XMinutePayloadConsumer.ConsumerPayloads.Keys.Max)
                End If
            End If
            Return ret
        End Function
        Public Function IsCrossover(ByVal firstDummyConsumer As PayloadToIndicatorConsumer,
                                    ByVal secondDummyConsumer As PayloadToIndicatorConsumer,
                                    ByVal firstDummyConsumerField As TypeOfField,
                                    ByVal secondDummyConsumerField As TypeOfField,
                                    ByVal currentCandle As OHLCPayload,
                                    ByVal crossSide As Enums.CrossDirection,
                                    ByVal printDetails As Boolean) As Boolean
            Dim ret As Boolean = False
            If currentCandle IsNot Nothing AndAlso currentCandle.PreviousPayload IsNot Nothing AndAlso currentCandle.PreviousPayload.PreviousPayload IsNot Nothing Then
                Dim firstConsumer As PayloadToIndicatorConsumer = GetConsumer(RawPayloadDependentConsumers, firstDummyConsumer)
                Dim secondConsumer As PayloadToIndicatorConsumer = GetConsumer(RawPayloadDependentConsumers, secondDummyConsumer)

                If firstConsumer IsNot Nothing AndAlso secondConsumer IsNot Nothing AndAlso
                    firstConsumer.ConsumerPayloads IsNot Nothing AndAlso firstConsumer.ConsumerPayloads.Count > 0 AndAlso
                    secondConsumer.ConsumerPayloads IsNot Nothing AndAlso secondConsumer.ConsumerPayloads.Count > 0 Then
                    Dim firstConsumerPreviousValue As IPayload = Nothing
                    Dim firstConsumerCurrentValue As IPayload = Nothing
                    Dim secondConsumerPreviousValue As IPayload = Nothing
                    Dim secondConsumerCurrentValue As IPayload = Nothing
                    If firstConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.PreviousPayload.SnapshotDateTime) Then
                        firstConsumerPreviousValue = firstConsumer.ConsumerPayloads(currentCandle.PreviousPayload.PreviousPayload.SnapshotDateTime)
                    End If
                    If firstConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) Then
                        firstConsumerCurrentValue = firstConsumer.ConsumerPayloads(currentCandle.PreviousPayload.SnapshotDateTime)
                    End If
                    If secondConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.PreviousPayload.SnapshotDateTime) Then
                        secondConsumerPreviousValue = secondConsumer.ConsumerPayloads(currentCandle.PreviousPayload.PreviousPayload.SnapshotDateTime)
                    End If
                    If secondConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) Then
                        secondConsumerCurrentValue = secondConsumer.ConsumerPayloads(currentCandle.PreviousPayload.SnapshotDateTime)
                    End If

                    If firstConsumerPreviousValue IsNot Nothing AndAlso firstConsumerCurrentValue IsNot Nothing AndAlso
                        secondConsumerPreviousValue IsNot Nothing AndAlso secondConsumerCurrentValue IsNot Nothing Then
                        Dim firstConsumerPreviousValueField As Field = GetFieldFromType(firstConsumerPreviousValue, firstDummyConsumerField)
                        Dim firstConsumerCurrentValueField As Field = GetFieldFromType(firstConsumerCurrentValue, firstDummyConsumerField)
                        Dim secondConsumerPreviousValueField As Field = GetFieldFromType(secondConsumerPreviousValue, secondDummyConsumerField)
                        Dim secondConsumerCurrentValueField As Field = GetFieldFromType(secondConsumerCurrentValue, secondDummyConsumerField)
                        Select Case crossSide
                            Case CrossDirection.Above
                                ret = firstConsumerPreviousValueField.Value < secondConsumerPreviousValueField.Value AndAlso
                                    firstConsumerCurrentValueField.Value > secondConsumerCurrentValueField.Value
                            Case CrossDirection.Below
                                ret = firstConsumerPreviousValueField.Value > secondConsumerPreviousValueField.Value AndAlso
                                    firstConsumerCurrentValueField.Value < secondConsumerCurrentValueField.Value
                        End Select
                        If printDetails Then
                            logger.Debug("IsCrossover-> FirstConsumer:{0}, SecondConsumer:{1}, FirstConsumerField:{2}, SecondConsumerField:{3}, CurrentCandle:{5}, Condition:{6}, [{7},{8}][{9},{10}], CrossSide:{4}, IsCrossover:{11}, TradingSymbol:{12}",
                                          firstDummyConsumer.ToString,
                                          secondDummyConsumer.ToString,
                                          firstDummyConsumerField.ToString,
                                          secondDummyConsumerField.ToString,
                                          crossSide.ToString,
                                          currentCandle.ToString,
                                          If(crossSide = CrossDirection.Above, "[x<y][p>q]", "[x>y][p<q]"),
                                          firstConsumerPreviousValueField.Value,
                                          secondConsumerPreviousValueField.Value,
                                          firstConsumerCurrentValueField.Value,
                                          secondConsumerCurrentValueField.Value,
                                          ret,
                                          Me.TradableInstrument.TradingSymbol)
                        End If
                    End If
                End If
            End If
            Return ret
        End Function
        Public Function IsAboveOrBelow(ByVal firstDummyConsumer As PayloadToIndicatorConsumer,
                                        ByVal secondDummyConsumer As PayloadToIndicatorConsumer,
                                        ByVal firstDummyConsumerField As TypeOfField,
                                        ByVal secondDummyConsumerField As TypeOfField,
                                        ByVal currentCandle As OHLCPayload,
                                        ByVal position As Enums.Positions,
                                        ByVal printDetails As Boolean) As Boolean
            Dim ret As Boolean = False
            If currentCandle IsNot Nothing AndAlso currentCandle.PreviousPayload IsNot Nothing AndAlso currentCandle.PreviousPayload.PreviousPayload IsNot Nothing Then
                Dim firstConsumer As PayloadToIndicatorConsumer = GetConsumer(RawPayloadDependentConsumers, firstDummyConsumer)
                Dim secondConsumer As PayloadToIndicatorConsumer = GetConsumer(RawPayloadDependentConsumers, secondDummyConsumer)

                If firstConsumer IsNot Nothing AndAlso secondConsumer IsNot Nothing AndAlso
                    firstConsumer.ConsumerPayloads IsNot Nothing AndAlso firstConsumer.ConsumerPayloads.Count > 0 AndAlso
                    secondConsumer.ConsumerPayloads IsNot Nothing AndAlso secondConsumer.ConsumerPayloads.Count > 0 Then
                    Dim firstConsumerCurrentValue As IPayload = Nothing
                    Dim secondConsumerCurrentValue As IPayload = Nothing

                    If firstConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) Then
                        firstConsumerCurrentValue = firstConsumer.ConsumerPayloads(currentCandle.PreviousPayload.SnapshotDateTime)
                    End If
                    If secondConsumer.ConsumerPayloads.ContainsKey(currentCandle.PreviousPayload.SnapshotDateTime) Then
                        secondConsumerCurrentValue = secondConsumer.ConsumerPayloads(currentCandle.PreviousPayload.SnapshotDateTime)
                    End If

                    If firstConsumerCurrentValue IsNot Nothing AndAlso secondConsumerCurrentValue IsNot Nothing Then
                        Dim firstConsumerCurrentValueField As Field = GetFieldFromType(firstConsumerCurrentValue, firstDummyConsumerField)
                        Dim secondConsumerCurrentValueField As Field = GetFieldFromType(secondConsumerCurrentValue, secondDummyConsumerField)
                        Select Case position
                            Case Positions.Above
                                ret = firstConsumerCurrentValueField.Value > secondConsumerCurrentValueField.Value
                            Case Positions.Below
                                ret = firstConsumerCurrentValueField.Value < secondConsumerCurrentValueField.Value
                        End Select
                        If printDetails Then
                            logger.Debug("IsAboveOrBelow-> FirstConsumer:{0}, SecondConsumer:{1}, FirstConsumerField:{2}, SecondConsumerField:{3}, CurrentCandle:{4}, Condition:{5}, [{6},{7}], Position:{8}, IsAboveOrBelow:{9}, TradingSymbol:{10}",
                                          firstDummyConsumer.ToString,
                                          secondDummyConsumer.ToString,
                                          firstDummyConsumerField.ToString,
                                          secondDummyConsumerField.ToString,
                                          currentCandle.ToString,
                                          If(position = Positions.Above, "[x>y]", "[x<y]"),
                                          firstConsumerCurrentValueField.Value,
                                          secondConsumerCurrentValueField.Value,
                                          position.ToString,
                                          ret,
                                          Me.TradableInstrument.TradingSymbol)
                        End If
                    End If
                End If
            End If
            Return ret
        End Function
        Public Function GetFieldFromType(ByVal ownerClassObj As Object, ByVal fieldType As TypeOfField) As Field
            Dim ret As Field = Nothing
            Dim propInfos As System.Reflection.PropertyInfo() = ownerClassObj.GetType.GetProperties()
            If propInfos IsNot Nothing AndAlso propInfos.Count > 0 Then
                For Each runningPropInfo In propInfos
                    If runningPropInfo.PropertyType Is GetType(Field) AndAlso CType(runningPropInfo.GetValue(ownerClassObj), Field).FieldType = fieldType Then
                        ret = runningPropInfo.GetValue(ownerClassObj)
                        Exit For
                    End If
                Next
            End If
            Return ret
        End Function
        Public Function GetConsumer(ByVal startLevelConsumers As List(Of IPayloadConsumer), ByVal dummyConsumerToFind As IPayloadConsumer) As IPayloadConsumer
            Dim ret As IPayloadConsumer = Nothing
            If startLevelConsumers IsNot Nothing AndAlso startLevelConsumers.Count > 0 Then
                For Each runningConsumer In startLevelConsumers
                    If runningConsumer.ToString.Equals(dummyConsumerToFind.ToString) Then
                        ret = runningConsumer
                    Else
                        ret = GetConsumer(runningConsumer.OnwardLevelConsumers, dummyConsumerToFind)
                    End If
                    If ret IsNot Nothing Then Exit For
                Next
            End If
            Return ret
        End Function
        Private Async Function WaitAndGenerateFreshTag(ByVal currentActivityTag As String) As Task(Of String)
            If Me.ParentStrategy.SignalManager.ActivityDetails IsNot Nothing AndAlso
                Me.ParentStrategy.SignalManager.ActivityDetails.ContainsKey(currentActivityTag) Then

                Dim currentActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.ActivityDetails(currentActivityTag)

                If currentActivities.EntryActivity.ReceivedTime.Date <> Date.MinValue.Date AndAlso
                    currentActivities.EntryActivity.RequestTime.Date <> Date.MinValue.Date Then
                    Dim delay As Integer = DateDiff(DateInterval.Second, currentActivities.EntryActivity.RequestTime, currentActivities.EntryActivity.ReceivedTime)
                    delay = Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay - delay
                    If delay > 0 Then
                        logger.Debug("Place order is retrying after timeout, putting the required delay first:{0} seconds, {1}", delay, Me.TradableInstrument.TradingSymbol)
                        Await Task.Delay(delay * 1000, _cts.Token).ConfigureAwait(False)
                    End If
                End If
            End If

            Return GenerateTag(Now)
        End Function
        Private Function GenerateFreshTagForNewSignal(ByVal currentActivityTag As String, ByVal signalTime As Date, ByVal signalDirection As IOrder.TypeOfTransaction, ByVal forceGenerateFreshTag As Boolean) As String
            Dim ret As String = currentActivityTag
            If Me.ParentStrategy.SignalManager.ActivityDetails IsNot Nothing AndAlso
            Me.ParentStrategy.SignalManager.ActivityDetails.ContainsKey(currentActivityTag) Then

                Dim currentActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.ActivityDetails(currentActivityTag)

                If forceGenerateFreshTag Then
                    ret = GenerateTag(Now)
                Else
                    If Not Utilities.Time.IsDateTimeEqualTillMinutes(currentActivities.SignalGeneratedTime, signalTime) Then
                        ret = GenerateTag(Now)
                    Else
                        If currentActivities.SignalDirection <> signalDirection Then
                            ret = GenerateTag(Now)
                        End If
                    End If
                End If
            End If
            Return ret
        End Function
        Private Async Function GenerateTagForPlaceOrderTriggers(ByVal placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))) As Task
            If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                Dim lastActivityTag As String = GenerateTag(Now)
                For Each runningPlaceOrderTrigger In placeOrderTriggers
                    Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
                    Dim activityTag As String = GenerateTag(Now)
                    While lastActivityTag.ToUpper = activityTag.ToUpper
                        activityTag = GenerateTag(Now)
                        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
                    End While
                    lastActivityTag = activityTag
                    'lastActivityTag = GenerateFreshTagForNewSignal(activityTag, runningPlaceOrderTrigger.Item2.SignalCandle.SnapshotDateTime, runningPlaceOrderTrigger.Item2.EntryDirection, runningPlaceOrderTrigger.Item2.GenerateDifferentTag)
                    runningPlaceOrderTrigger.Item2.Tag = lastActivityTag
                Next
            End If
        End Function
        Protected Function GetSignalCandleOfAnOrder(ByVal parentOrderID As String, ByVal timeFrame As Integer) As OHLCPayload
            Dim ret As OHLCPayload = Nothing
            If Me.OrderDetails IsNot Nothing AndAlso Me.OrderDetails.Count > 0 AndAlso Me.OrderDetails.ContainsKey(parentOrderID) Then
                Dim currentBussinessOrder As IBusinessOrder = Me.OrderDetails(parentOrderID)
                If currentBussinessOrder.ParentOrder IsNot Nothing Then
                    Dim activityTag As String = currentBussinessOrder.ParentOrder.Tag
                    If Me.ParentStrategy.SignalManager.ActivityDetails IsNot Nothing AndAlso
                        Me.ParentStrategy.SignalManager.ActivityDetails.Count > 0 AndAlso
                        Me.ParentStrategy.SignalManager.ActivityDetails.ContainsKey(activityTag) Then
                        If Me.ParentStrategy.SignalManager.ActivityDetails(activityTag).ParentOrderID = parentOrderID Then
                            Dim signalCandleTime As Date = Me.ParentStrategy.SignalManager.ActivityDetails(activityTag).SignalGeneratedTime
                            If timeFrame = 1 Then
                                If Me.TradableInstrument.RawPayloads IsNot Nothing AndAlso Me.TradableInstrument.RawPayloads.Count > 0 AndAlso
                                    Me.TradableInstrument.RawPayloads.ContainsKey(signalCandleTime) Then
                                    ret = Me.TradableInstrument.RawPayloads(signalCandleTime)
                                End If
                            Else
                                If Me.RawPayloadDependentConsumers IsNot Nothing AndAlso Me.RawPayloadDependentConsumers.Count > 0 Then
                                    Dim XMinutePayloadConsumers As IEnumerable(Of IPayloadConsumer) = RawPayloadDependentConsumers.Where(Function(x)
                                                                                                                                             Return x.TypeOfConsumer = IPayloadConsumer.ConsumerType.Chart AndAlso
                                                                                                                                          CType(x, PayloadToChartConsumer).Timeframe = timeFrame
                                                                                                                                         End Function)
                                    Dim XMinutePayloadConsumer As PayloadToChartConsumer = Nothing
                                    If XMinutePayloadConsumers IsNot Nothing AndAlso XMinutePayloadConsumers.Count > 0 Then
                                        XMinutePayloadConsumer = XMinutePayloadConsumers.FirstOrDefault
                                    End If

                                    If XMinutePayloadConsumer IsNot Nothing AndAlso
                                        XMinutePayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso XMinutePayloadConsumer.ConsumerPayloads.Count > 0 AndAlso
                                        XMinutePayloadConsumer.ConsumerPayloads.ContainsKey(signalCandleTime) Then
                                        ret = XMinutePayloadConsumer.ConsumerPayloads(signalCandleTime)
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
            Return ret
        End Function
        Public Function GetParentFromChildOrder(ByVal childOrder As IOrder) As IBusinessOrder
            Dim ret As IBusinessOrder = Nothing
            If childOrder IsNot Nothing AndAlso childOrder.ParentOrderIdentifier IsNot Nothing AndAlso
                OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso OrderDetails.ContainsKey(childOrder.ParentOrderIdentifier) Then
                ret = OrderDetails(childOrder.ParentOrderIdentifier)
            End If
            Return ret
        End Function
        Public Function GetBlockDateTime(ByVal time As Date, ByVal timeframe As Integer) As Date
            Dim ret As Date = Date.MinValue
            If Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Minute Mod timeframe = 0 Then
                ret = New Date(time.Year, time.Month, time.Day, time.Hour, Math.Floor(time.Minute / timeframe) * timeframe, 0)
            Else
                Dim exchangeStartTime As Date = New Date(time.Year, time.Month, time.Day,
                                                         Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Hour,
                                                         Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.Minute, 0)
                Dim currentTime As Date = New Date(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0)
                Dim timeDifference As Double = currentTime.Subtract(exchangeStartTime).TotalMinutes
                Dim adjustedTimeDifference As Integer = Math.Floor(timeDifference / timeframe) * timeframe
                Dim currentMinute As Date = exchangeStartTime.AddMinutes(adjustedTimeDifference)
                ret = New Date(time.Year, time.Month, time.Day, currentMinute.Hour, currentMinute.Minute, 0)
            End If
            Return ret
        End Function
        Public Function GetOrderExitTime(ByVal order As IBusinessOrder) As Date
            Dim ret As Date = Date.MinValue
            If order IsNot Nothing AndAlso order.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                If order.AllOrder IsNot Nothing AndAlso order.AllOrder.Count > 0 Then
                    For Each runningOrder In order.AllOrder
                        If runningOrder.Status = IOrder.TypeOfStatus.Complete Then
                            If runningOrder.TimeStamp > ret Then
                                ret = runningOrder.TimeStamp
                            End If
                        End If
                    Next
                End If
            End If
            Return ret
        End Function
#End Region

#Region "Public Functions"
        Public Function GetTotalPLOfAnOrder(ByVal parentOrderId As String) As Decimal
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso OrderDetails.ContainsKey(parentOrderId) Then
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                Dim calculateWithLTP As Boolean = False
                If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    calculateWithLTP = True
                End If
                If parentBusinessOrder.TargetOrder IsNot Nothing AndAlso parentBusinessOrder.TargetOrder.Count > 0 Then
                    calculateWithLTP = True
                End If

                If parentBusinessOrder.AllOrder IsNot Nothing AndAlso parentBusinessOrder.AllOrder.Count > 0 Then
                    For Each order In parentBusinessOrder.AllOrder
                        'If order.Status = IOrder.TypeOfStatus.Cancelled OrElse order.Status = IOrder.TypeOfStatus.Complete Then
                        If order.Status = IOrder.TypeOfStatus.Complete Then
                            If order.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                plOfDay += order.AveragePrice * order.Quantity * -1
                            ElseIf order.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                plOfDay += order.AveragePrice * order.Quantity
                            End If
                        ElseIf Not order.Status = IOrder.TypeOfStatus.Rejected Then
                            calculateWithLTP = True
                        End If
                    Next
                Else
                    calculateWithLTP = True
                End If

                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    plOfDay += parentBusinessOrder.ParentOrder.AveragePrice * parentBusinessOrder.ParentOrder.Quantity * -1
                ElseIf parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    plOfDay += parentBusinessOrder.ParentOrder.AveragePrice * parentBusinessOrder.ParentOrder.Quantity
                End If
                If calculateWithLTP AndAlso parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                    Dim quantityToCalculate As Integer = parentBusinessOrder.ParentOrder.Quantity
                    If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                        quantityToCalculate = 0
                        For Each slOrder In parentBusinessOrder.SLOrder
                            If Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Complete Then
                                quantityToCalculate += slOrder.Quantity
                            End If
                        Next
                        If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            plOfDay += Me.TradableInstrument.LastTick.LastPrice * quantityToCalculate
                        ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            plOfDay += Me.TradableInstrument.LastTick.LastPrice * quantityToCalculate * -1
                        End If
                    End If
                End If
                Return plOfDay * Me.TradableInstrument.QuantityMultiplier
            Else
                Return 0
            End If
        End Function
        Public Function GetOverallPL() As Decimal
            'logger.Debug("CalculatePL, parameters:Nothing")
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each parentOrderId In OrderDetails.Keys
                    plOfDay += GetTotalPLOfAnOrder(parentOrderId)
                Next
                Return plOfDay
            Else
                Return 0
            End If
        End Function
        Public Function GetTotalPLPointOfAnOrder(ByVal parentOrderId As String) As Decimal
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso OrderDetails.ContainsKey(parentOrderId) Then
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                Dim calculateWithLTP As Boolean = False
                If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    calculateWithLTP = True
                End If
                If parentBusinessOrder.TargetOrder IsNot Nothing AndAlso parentBusinessOrder.TargetOrder.Count > 0 Then
                    calculateWithLTP = True
                End If

                If parentBusinessOrder.AllOrder IsNot Nothing AndAlso parentBusinessOrder.AllOrder.Count > 0 Then
                    For Each order In parentBusinessOrder.AllOrder
                        'If order.Status = IOrder.TypeOfStatus.Cancelled OrElse order.Status = IOrder.TypeOfStatus.Complete Then
                        If order.Status = IOrder.TypeOfStatus.Complete Then
                            If order.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                plOfDay += order.AveragePrice * -1
                            ElseIf order.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                plOfDay += order.AveragePrice
                            End If
                        ElseIf Not order.Status = IOrder.TypeOfStatus.Rejected Then
                            calculateWithLTP = True
                        End If
                    Next
                Else
                    calculateWithLTP = True
                End If

                If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                    plOfDay += parentBusinessOrder.ParentOrder.AveragePrice * -1
                ElseIf parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                    plOfDay += parentBusinessOrder.ParentOrder.AveragePrice
                End If
                If calculateWithLTP AndAlso parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                    Dim quantityToCalculate As Integer = parentBusinessOrder.ParentOrder.Quantity
                    If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                        quantityToCalculate = 0
                        For Each slOrder In parentBusinessOrder.SLOrder
                            If Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Complete Then
                                quantityToCalculate += slOrder.Quantity
                            End If
                        Next
                        If quantityToCalculate <> 0 Then
                            If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                plOfDay += Me.TradableInstrument.LastTick.LastPrice
                            ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                plOfDay += Me.TradableInstrument.LastTick.LastPrice * -1
                            End If
                        End If
                    End If
                End If
                Return plOfDay
            Else
                Return 0
            End If
        End Function
        Public Function GetOverallPLPoint() As Decimal
            'logger.Debug("CalculatePL, parameters:Nothing")
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each parentOrderId In OrderDetails.Keys
                    plOfDay += GetTotalPLPointOfAnOrder(parentOrderId)
                Next
                Return plOfDay
            Else
                Return 0
            End If
        End Function
        Public Function IsActiveInstrument() As Boolean
            Dim ret As Boolean = False
            Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
            ret = allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0
            Return ret
        End Function
        Public Function GetLastExecutedOrder() As IBusinessOrder
            Dim ret As IBusinessOrder = Nothing
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                Dim allExecutedOrders As IEnumerable(Of IBusinessOrder) = OrderDetails.Values.Where(Function(x)
                                                                                                        Return x.ParentOrder IsNot Nothing AndAlso
                                                                                                        x.ParentOrder.Status = IOrder.TypeOfStatus.Complete
                                                                                                    End Function)
                If allExecutedOrders IsNot Nothing AndAlso allExecutedOrders.Count > 0 Then
                    ret = allExecutedOrders.OrderBy(Function(y)
                                                        Return y.ParentOrder.TimeStamp
                                                    End Function).LastOrDefault
                End If
            End If
            Return ret
        End Function
        Public Function GetTotalExecutedOrders() As Integer
            Dim tradeCount As Integer = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each parentOrderId In OrderDetails.Keys
                    Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                    If parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                        tradeCount += 1
                    End If
                Next
            End If
            Return tradeCount
        End Function
        Public Function GetTotalPLOfAnOrderAfterBrokerage(ByVal parentOrderId As String) As Decimal
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 AndAlso OrderDetails.ContainsKey(parentOrderId) Then
                Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                Dim calculateWithLTP As Boolean = False
                If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                    calculateWithLTP = True
                End If
                If parentBusinessOrder.TargetOrder IsNot Nothing AndAlso parentBusinessOrder.TargetOrder.Count > 0 Then
                    calculateWithLTP = True
                End If

                If parentBusinessOrder.AllOrder IsNot Nothing AndAlso parentBusinessOrder.AllOrder.Count > 0 Then
                    For Each order In parentBusinessOrder.AllOrder
                        _cts.Token.ThrowIfCancellationRequested()
                        'If order.Status = IOrder.TypeOfStatus.Cancelled OrElse order.Status = IOrder.TypeOfStatus.Complete Then
                        If order.Status = IOrder.TypeOfStatus.Complete Then
                            Dim buyPrice As Decimal = Decimal.MinValue
                            Dim sellPrice As Decimal = Decimal.MinValue
                            If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                buyPrice = parentBusinessOrder.ParentOrder.AveragePrice
                                sellPrice = order.AveragePrice
                            ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                sellPrice = parentBusinessOrder.ParentOrder.AveragePrice
                                buyPrice = order.AveragePrice
                            End If
                            plOfDay += _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, order.Quantity)
                        ElseIf Not order.Status = IOrder.TypeOfStatus.Rejected Then
                            calculateWithLTP = True
                        End If
                    Next
                Else
                    calculateWithLTP = True
                End If

                If calculateWithLTP AndAlso parentBusinessOrder.ParentOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                    If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                        For Each slOrder In parentBusinessOrder.SLOrder
                            If Not slOrder.Status = IOrder.TypeOfStatus.Cancelled AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Complete Then
                                Dim buyPrice As Decimal = Decimal.MinValue
                                Dim sellPrice As Decimal = Decimal.MinValue
                                If parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Buy Then
                                    buyPrice = parentBusinessOrder.ParentOrder.AveragePrice
                                    sellPrice = Me.TradableInstrument.LastTick.LastPrice
                                ElseIf parentBusinessOrder.ParentOrder.TransactionType = IOrder.TypeOfTransaction.Sell Then
                                    sellPrice = parentBusinessOrder.ParentOrder.AveragePrice
                                    buyPrice = Me.TradableInstrument.LastTick.LastPrice
                                End If
                                plOfDay += _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, slOrder.Quantity)
                            End If
                        Next
                    End If
                End If
                Return plOfDay
            Else
                Return 0
            End If
        End Function
        Public Function GetOverallPLAfterBrokerage() As Decimal
            Dim plOfDay As Decimal = 0
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each parentOrderId In OrderDetails.Keys
                    plOfDay += GetTotalPLOfAnOrderAfterBrokerage(parentOrderId)
                Next
                Return plOfDay
            Else
                Return 0
            End If
        End Function
        Public Function CalculateQuantityFromTarget(ByVal buyPrice As Double, ByVal sellPrice As Double, ByVal NetProfitLossOfTrade As Double) As Integer
            Dim lotSize As Integer = Me.TradableInstrument.LotSize
            Dim quantityMultiplier As Integer = 1
            Dim previousQuantity As Integer = lotSize
            For quantityMultiplier = 1 To Integer.MaxValue
                Dim plAfterBrokerage As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, lotSize * quantityMultiplier)
                If plAfterBrokerage >= NetProfitLossOfTrade Then
                    previousQuantity = lotSize * quantityMultiplier
                    Exit For
                Else
                    previousQuantity = lotSize * quantityMultiplier
                End If
            Next
            Return previousQuantity
        End Function
        Public Function CalculateQuantityFromStoploss(ByVal buyPrice As Double, ByVal sellPrice As Double, ByVal NetProfitLossOfTrade As Double) As Integer
            Dim lotSize As Integer = Me.TradableInstrument.LotSize
            Dim quantityMultiplier As Integer = 1
            Dim previousQuantity As Integer = lotSize
            For quantityMultiplier = 1 To Integer.MaxValue
                Dim plAfterBrokerage As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, sellPrice, lotSize * quantityMultiplier)
                If plAfterBrokerage <= Math.Abs(NetProfitLossOfTrade) * -1 Then
                    previousQuantity = lotSize * If(quantityMultiplier - 1 = 0, 1, quantityMultiplier - 1)
                    Exit For
                Else
                    previousQuantity = lotSize * quantityMultiplier
                End If
            Next
            Return previousQuantity
        End Function
        Public Function CalculateTargetFromPL(ByVal buyPrice As Decimal, ByVal quantity As Integer, ByVal NetProfitOfTrade As Decimal) As Decimal
            Dim ret As Decimal = buyPrice
            For ret = buyPrice To Decimal.MaxValue Step Me.TradableInstrument.TickSize
                Dim plAfterBrokerage As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, ret, quantity)
                If plAfterBrokerage >= NetProfitOfTrade Then
                    Exit For
                End If
            Next
            Return ret
        End Function
        Public Function CalculateStplossFromPL(ByVal buyPrice As Decimal, ByVal quantity As Integer, ByVal NetLossOfTrade As Decimal) As Decimal
            Dim ret As Decimal = buyPrice
            For ret = buyPrice To Decimal.MinValue Step Me.TradableInstrument.TickSize * -1
                Dim plAfterBrokerage As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, buyPrice, ret, quantity)
                If plAfterBrokerage <= NetLossOfTrade Then
                    Exit For
                End If
            Next
            Return ret
        End Function
        Public Function CalculateQuantityFromInvestment(ByVal stockPrice As Double, ByVal marginMultiplier As Decimal, ByVal totalInvestment As Double, ByVal allowCapitalToIncrease As Boolean) As Integer
            Dim quantity As Integer = Me.TradableInstrument.LotSize * Me.TradableInstrument.QuantityMultiplier
            Dim multiplier As Integer = 1
            If allowCapitalToIncrease Then
                multiplier = Math.Ceiling(totalInvestment / (quantity * stockPrice / marginMultiplier))
            Else
                multiplier = Math.Floor(totalInvestment / (quantity * stockPrice / marginMultiplier))
            End If
            If multiplier = 0 Then multiplier = 1
            Return quantity * multiplier / Me.TradableInstrument.QuantityMultiplier
        End Function
        Public Function GetBreakevenPoint(ByVal entryPrice As Decimal, ByVal quantity As Integer, ByVal direction As IOrder.TypeOfTransaction) As Decimal
            Dim ret As Decimal = Me.TradableInstrument.TickSize
            If direction = IOrder.TypeOfTransaction.Buy Then
                For exitPrice As Decimal = entryPrice To Decimal.MaxValue Step ret
                    Dim pl As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, entryPrice, exitPrice, quantity)
                    If pl >= 0 Then
                        ret = ConvertFloorCeling(exitPrice - entryPrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        Exit For
                    End If
                Next
            ElseIf direction = IOrder.TypeOfTransaction.Sell Then
                For exitPrice As Decimal = entryPrice To Decimal.MinValue Step ret * -1
                    Dim pl As Decimal = _APIAdapter.CalculatePLWithBrokerage(Me.TradableInstrument, exitPrice, entryPrice, quantity)
                    If pl >= 0 Then
                        ret = ConvertFloorCeling(entryPrice - exitPrice, Me.TradableInstrument.TickSize, RoundOfType.Celing)
                        Exit For
                    End If
                Next
            End If
            Return ret
        End Function
#End Region

#Region "Public Overridable Functions"
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}", ParentStrategy.ToString, TradableInstrument.ToString)
        End Function
        Public Overridable Function GenerateTag(ByVal timeOfOrder As Date) As String
            Dim instrumentIdentifier As String = Nothing
            If Me.ParentStrategy.ParentController.InstrumentMappingTable IsNot Nothing AndAlso
                Me.ParentStrategy.ParentController.InstrumentMappingTable.Count > 0 Then
                Dim identifier As String = Me.ParentStrategy.ParentController.InstrumentMappingTable(Me.TradableInstrument.InstrumentIdentifier)
                instrumentIdentifier = identifier.PadLeft(3, "0")
            Else
                Throw New ApplicationException("No instrument map detected, cannot generate tag for order tracking")
            End If
            Return Hex(CLng(String.Format("{0}{1}{2}",
                                 Me.ParentStrategy.StrategyIdentifier,
                                 instrumentIdentifier,
                                 Right(timeOfOrder.Ticks, 5))))
            'Date.Parse(timeOfOrder).Subtract(Date.Parse(Now.Date)).TotalSeconds)))
        End Function
        Public Overridable Async Function HandleTickTriggerToUIETCAsync() As Task
            Await Me.ParentStrategy.SignalManager.UIRefresh(Me, False).ConfigureAwait(False)
        End Function
        Public Overridable Async Function PopulateChartAndIndicatorsAsync(ByVal candleCreator As Chart, ByVal currentCandle As OHLCPayload) As Task
            'logger.Debug("PopulateChartAndIndicatorsAsync, parameters:{0},{1}", candleCreator.ToString, currentCandle.ToString)
            If RawPayloadDependentConsumers IsNot Nothing AndAlso RawPayloadDependentConsumers.Count > 0 Then
                For Each runningRawPayloadConsumer In RawPayloadDependentConsumers
                    If runningRawPayloadConsumer.TypeOfConsumer = IPayloadConsumer.ConsumerType.Chart Then
                        Dim currentXMinute As Date = candleCreator.ConvertTimeframe(CType(runningRawPayloadConsumer, PayloadToChartConsumer).Timeframe,
                                                                    currentCandle,
                                                                    runningRawPayloadConsumer)
                        If candleCreator.IndicatorCreator Is Nothing Then candleCreator.IndicatorCreator = New ChartHandler.Indicator.IndicatorManeger(Me.ParentStrategy.ParentController, candleCreator, _cts)
                        If currentXMinute <> Date.MaxValue Then
                            Dim c As Integer = 1
                            If runningRawPayloadConsumer.OnwardLevelConsumers IsNot Nothing AndAlso runningRawPayloadConsumer.OnwardLevelConsumers.Count > 0 Then
                                ''EMA Supertrend Strategy
                                'For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                '    If c < 3 Then
                                '        candleCreator.IndicatorCreator.CalculateEMA(currentXMinute, consumer)
                                '    Else
                                '        candleCreator.IndicatorCreator.CalculateSupertrend(currentXMinute, consumer)
                                '    End If
                                '    c += 1
                                'Next

                                ''PetDGandhi Strategy
                                'For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                '    If c = 1 Then
                                '        candleCreator.IndicatorCreator.CalculateEMA(currentXMinute, consumer)
                                '    Else
                                '        candleCreator.IndicatorCreator.CalculatePivotHighLow(currentXMinute, consumer)
                                '    End If
                                '    c += 1
                                'Next

                                ''EMA Crossover Strategy
                                'For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                '    candleCreator.IndicatorCreator.CalculateEMA(currentXMinute, consumer)
                                'Next

                                ''Joy Maa Strategy
                                'For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                '    If c = 1 Then
                                '        candleCreator.IndicatorCreator.CalculateFractal(currentXMinute, consumer)
                                '    ElseIf c = 2 Then
                                '        candleCreator.IndicatorCreator.CalculateATR(currentXMinute, consumer)
                                '    End If
                                '    c += 1
                                'Next

                                ''TwoThird Strategy
                                'For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                '    candleCreator.IndicatorCreator.CalculateATR(currentXMinute, consumer)
                                'Next

                                'Pet-D Gandhi
                                For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                    If c = 1 Then
                                        candleCreator.IndicatorCreator.CalculateFractal(currentXMinute, consumer)
                                    ElseIf c = 2 Then
                                        candleCreator.IndicatorCreator.CalculateATR(currentXMinute, consumer)
                                    End If
                                    c += 1
                                Next
                            End If

                            'Below block for pair strategy
                            If Me.DependendStrategyInstruments IsNot Nothing AndAlso Me.DependendStrategyInstruments.Count > 0 Then
                                For Each runningDependendStrategyInstrument In Me.DependendStrategyInstruments
                                    If runningDependendStrategyInstrument.RawPayloadDependentConsumers IsNot Nothing AndAlso
                                        runningDependendStrategyInstrument.RawPayloadDependentConsumers.Count > 0 Then
                                        For Each runningDependendRawPayloadConsumer In runningDependendStrategyInstrument.RawPayloadDependentConsumers
                                            If runningDependendRawPayloadConsumer.TypeOfConsumer = IPayloadConsumer.ConsumerType.Pair Then
                                                If runningRawPayloadConsumer.ConsumerPayloads IsNot Nothing AndAlso runningRawPayloadConsumer.ConsumerPayloads.Count > 0 Then
                                                    Dim requiredDataSet As IEnumerable(Of Date) = runningRawPayloadConsumer.ConsumerPayloads.Keys.Where(Function(x)
                                                                                                                                                            Return x >= currentXMinute
                                                                                                                                                        End Function)

                                                    For Each runningInputDate In requiredDataSet.OrderBy(Function(x)
                                                                                                             Return x
                                                                                                         End Function)
                                                        If runningDependendRawPayloadConsumer.ConsumerPayloads Is Nothing Then runningDependendRawPayloadConsumer.ConsumerPayloads = New Concurrent.ConcurrentDictionary(Of Date, IPayload)
                                                        Dim currentMinutePairData As PairPayload = Nothing
                                                        currentMinutePairData = runningDependendRawPayloadConsumer.ConsumerPayloads.GetOrAdd(runningInputDate, currentMinutePairData)
                                                        If currentMinutePairData Is Nothing Then currentMinutePairData = New PairPayload
                                                        If Me.TradableInstrument.TradingSymbol = runningDependendStrategyInstrument.TradableInstrument.TradingSymbol.Split("_")(0) Then
                                                            currentMinutePairData.Instrument1Payload = runningRawPayloadConsumer.ConsumerPayloads(runningInputDate)
                                                        ElseIf Me.TradableInstrument.TradingSymbol = runningDependendStrategyInstrument.TradableInstrument.TradingSymbol.Split("_")(1) Then
                                                            currentMinutePairData.Instrument2Payload = runningRawPayloadConsumer.ConsumerPayloads(runningInputDate)
                                                        Else
                                                            Throw New NotImplementedException("Pair strategy should not have any other instrument which is not matching with virtual instrument name")
                                                        End If
                                                        runningDependendRawPayloadConsumer.ConsumerPayloads.AddOrUpdate(runningInputDate, currentMinutePairData, Function(key, value) currentMinutePairData)
                                                    Next
                                                End If
                                            End If
                                        Next
                                    End If
                                    Dim chartCreator As Chart = Me.ParentStrategy.ParentController.GetChartCreator(runningDependendStrategyInstrument.TradableInstrument.InstrumentIdentifier)
                                    If chartCreator IsNot Nothing Then
                                        Dim currentPayload As OHLCPayload = runningRawPayloadConsumer.ConsumerPayloads(currentXMinute)
                                        Await runningDependendStrategyInstrument.PopulateChartAndIndicatorsAsync(chartCreator, currentPayload).ConfigureAwait(False)
                                    End If
                                Next
                            End If
                            'End Block

                        End If
                    ElseIf runningRawPayloadConsumer.TypeOfConsumer = IPayloadConsumer.ConsumerType.Pair Then
                        'This Is also for pair
                        If Me.ParentStrategyInstruments IsNot Nothing AndAlso Me.ParentStrategyInstruments.Count > 0 Then
                            Dim isAllHistoricalCompleted As Boolean = True
                            For Each runningParentStrategyInstrument In Me.ParentStrategyInstruments
                                isAllHistoricalCompleted = isAllHistoricalCompleted And runningParentStrategyInstrument.TradableInstrument.IsHistoricalCompleted
                            Next
                            If isAllHistoricalCompleted Then
                                Dim currentXMinute As Date = Date.MinValue
                                If PairConsumerProtection Then
                                    currentXMinute = Now.AddDays(-30)
                                    Me.PairConsumerProtection = False
                                Else
                                    currentXMinute = currentCandle.SnapshotDateTime
                                End If
                                If candleCreator.IndicatorCreator Is Nothing Then candleCreator.IndicatorCreator = New ChartHandler.Indicator.IndicatorManeger(Me.ParentStrategy.ParentController, candleCreator, _cts)
                                If currentXMinute <> Date.MinValue Then
                                    If runningRawPayloadConsumer.OnwardLevelConsumers IsNot Nothing AndAlso runningRawPayloadConsumer.OnwardLevelConsumers.Count > 0 Then
                                        For Each consumer In runningRawPayloadConsumer.OnwardLevelConsumers
                                            candleCreator.IndicatorCreator.CalculateSpreadRatio(currentXMinute, consumer)
                                            If consumer.OnwardLevelConsumers IsNot Nothing AndAlso consumer.OnwardLevelConsumers.Count > 0 Then
                                                For Each dependendConsumer In consumer.OnwardLevelConsumers
                                                    candleCreator.IndicatorCreator.CalculateBollinger(currentXMinute, dependendConsumer)
                                                Next
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
            'If TickPayloadDependentConsumers IsNot Nothing AndAlso TickPayloadDependentConsumers.Count > 0 Then
            '    For Each runningTickPayloadConsumer In TickPayloadDependentConsumers
            '        If runningTickPayloadConsumer.OnwardLevelConsumers IsNot Nothing AndAlso runningTickPayloadConsumer.OnwardLevelConsumers.Count > 0 Then
            '            If candleCreator.IndicatorCreator Is Nothing Then candleCreator.IndicatorCreator = New ChartHandler.Indicator.IndicatorManeger(Me.ParentStrategy.ParentController, candleCreator, _cts)
            '            For Each consumer In runningTickPayloadConsumer.OnwardLevelConsumers
            '                candleCreator.IndicatorCreator.CalculateTickSMA(currentCandle.SnapshotDateTime, consumer)
            '            Next
            '        End If
            '    Next
            'End If
        End Function
        Public Overridable Sub PopulateChartAndIndicatorsFromTick(ByVal candleCreator As Chart, ByVal currentTimestamp As Date)
            'logger.Debug("PopulateChartAndIndicatorsAsync, parameters:{0},{1}", candleCreator.ToString, currentCandle.ToString)
            If TickPayloadDependentConsumers IsNot Nothing AndAlso TickPayloadDependentConsumers.Count > 0 Then
                For Each runningTickPayloadConsumer In TickPayloadDependentConsumers
                    If runningTickPayloadConsumer.OnwardLevelConsumers IsNot Nothing AndAlso runningTickPayloadConsumer.OnwardLevelConsumers.Count > 0 Then
                        If candleCreator.IndicatorCreator Is Nothing Then candleCreator.IndicatorCreator = New ChartHandler.Indicator.IndicatorManeger(Me.ParentStrategy.ParentController, candleCreator, _cts)
                        For Each consumer In runningTickPayloadConsumer.OnwardLevelConsumers
                            candleCreator.IndicatorCreator.CalculateTickSMA(consumer)
                        Next
                    End If
                Next
            End If
        End Sub
        Public Overridable Async Function ProcessOrderUpadteAsync(ByVal orderData As IOrder) As Task
            Try
                While 1 = Interlocked.Exchange(_historicalLock, 1)
                    Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                End While
                Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
                If orderData IsNot Nothing AndAlso orderData.Status <> IOrder.TypeOfStatus.None Then
                    Dim s As Stopwatch = New Stopwatch
                    s.Start()
                    Dim parentOrderID As String = Nothing
                    Dim parentOrder As Boolean = False
                    If orderData.ParentOrderIdentifier IsNot Nothing AndAlso
                        orderData.ParentOrderIdentifier <> "" Then
                        parentOrderID = orderData.ParentOrderIdentifier
                    Else
                        parentOrderID = orderData.OrderIdentifier
                        parentOrder = True
                    End If
                    logger.Debug(String.Format("Order Processing from tick update. ID:{0}, Parent ID:{1}. {2}", orderData.OrderIdentifier, parentOrderID, Me.TradableInstrument.TradingSymbol))
                    If parentOrder Then
                        orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent
                    Else
                        If orderData.OrderType = IOrder.TypeOfOrder.Limit AndAlso orderData.TriggerPrice = 0 Then
                            orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target
                        ElseIf orderData.OrderType = IOrder.TypeOfOrder.Limit AndAlso orderData.TriggerPrice <> 0 Then
                            orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                        ElseIf orderData.OrderType = IOrder.TypeOfOrder.SL Then
                            orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                        End If
                    End If
                    If orderData.LogicalOrderType <> IOrder.LogicalTypeOfOrder.Parent AndAlso
                        Me.OrderDetails IsNot Nothing AndAlso Me.OrderDetails.Count > 0 AndAlso Me.OrderDetails.ContainsKey(parentOrderID) Then
                        If orderData.Status = IOrder.TypeOfStatus.Open OrElse orderData.Status = IOrder.TypeOfStatus.TriggerPending Then
                            Dim availableOrder As IEnumerable(Of IOrder) = Nothing
                            If Me.OrderDetails(parentOrderID).AllOrder IsNot Nothing AndAlso Me.OrderDetails(parentOrderID).AllOrder.Count > 0 Then
                                availableOrder = Me.OrderDetails(parentOrderID).AllOrder.Where(Function(x)
                                                                                                   Return x.OrderIdentifier = orderData.OrderIdentifier
                                                                                               End Function)
                            End If
                            If availableOrder Is Nothing OrElse (availableOrder IsNot Nothing AndAlso availableOrder.Count = 0) Then
                                If orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss Then
                                    Dim slOrderList As List(Of IOrder) = New List(Of IOrder)
                                    slOrderList.Add(orderData)
                                    If OrderDetails(parentOrderID).SLOrder IsNot Nothing AndAlso OrderDetails(parentOrderID).SLOrder.Count > 0 Then
                                        For Each order In OrderDetails(parentOrderID).SLOrder
                                            If orderData.OrderIdentifier <> orderData.OrderIdentifier Then
                                                slOrderList.Add(order)
                                            End If
                                        Next
                                    End If
                                    OrderDetails(parentOrderID).SLOrder = slOrderList
                                ElseIf orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target Then
                                    Dim targetOrderList As List(Of IOrder) = New List(Of IOrder)
                                    targetOrderList.Add(orderData)
                                    If OrderDetails(parentOrderID).TargetOrder IsNot Nothing AndAlso OrderDetails(parentOrderID).TargetOrder.Count > 0 Then
                                        For Each order In OrderDetails(parentOrderID).TargetOrder
                                            If orderData.OrderIdentifier <> orderData.OrderIdentifier Then
                                                targetOrderList.Add(order)
                                            End If
                                        Next
                                    End If
                                    OrderDetails(parentOrderID).TargetOrder = targetOrderList
                                Else
                                    Throw New ApplicationException(String.Format("Logical Order type unknown. Order ID:{0}", orderData.OrderIdentifier))
                                End If
                            Else
                                logger.Debug("Cannot process this order as it is already in final stage. Order ID:{0}. {1}", orderData.OrderIdentifier, Me.TradableInstrument.TradingSymbol)
                            End If
                        Else
                            Dim availableOrder As IEnumerable(Of IOrder) = Nothing
                            If Me.OrderDetails(parentOrderID).AllOrder IsNot Nothing AndAlso Me.OrderDetails(parentOrderID).AllOrder.Count > 0 Then
                                availableOrder = Me.OrderDetails(parentOrderID).AllOrder.Where(Function(x)
                                                                                                   Return x.OrderIdentifier = orderData.OrderIdentifier
                                                                                               End Function)
                            End If
                            If availableOrder Is Nothing OrElse (availableOrder IsNot Nothing AndAlso availableOrder.Count = 0) Then
                                Dim allOrderList As List(Of IOrder) = New List(Of IOrder)
                                allOrderList.Add(orderData)
                                If OrderDetails(parentOrderID).AllOrder IsNot Nothing AndAlso OrderDetails(parentOrderID).AllOrder.Count > 0 Then
                                    logger.Debug("Before modifying all order list. Count:{0}, List Count:{1}. {2}", OrderDetails(parentOrderID).AllOrder.Count, allOrderList.Count, Me.TradableInstrument.TradingSymbol)
                                    For Each order In OrderDetails(parentOrderID).AllOrder
                                        If order.OrderIdentifier <> orderData.OrderIdentifier Then
                                            allOrderList.Add(order)
                                        End If
                                    Next
                                End If
                                OrderDetails(parentOrderID).AllOrder = Nothing
                                OrderDetails(parentOrderID).AllOrder = allOrderList
                                logger.Debug("After modifying all order list. Count:{0}, List Count:{1}. {2}", OrderDetails(parentOrderID).AllOrder.Count, allOrderList.Count, Me.TradableInstrument.TradingSymbol)
                                If orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss Then
                                    If OrderDetails(parentOrderID).SLOrder IsNot Nothing AndAlso OrderDetails(parentOrderID).SLOrder.Count > 0 Then
                                        logger.Debug(String.Format("**** Before remove ***** SL Order count:{0}. {1}", OrderDetails(parentOrderID).SLOrder.Count, Me.TradableInstrument.TradingSymbol))
                                        OrderDetails(parentOrderID).SLOrder = OrderDetails(parentOrderID).SLOrder.Where(Function(x)
                                                                                                                            Return x.OrderIdentifier <> orderData.OrderIdentifier
                                                                                                                        End Function)
                                        logger.Debug(String.Format("**** After remove ***** SL Order count:{0}. {1}", OrderDetails(parentOrderID).SLOrder.Count, Me.TradableInstrument.TradingSymbol))
                                    End If
                                ElseIf orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target Then
                                    If OrderDetails(parentOrderID).TargetOrder IsNot Nothing AndAlso OrderDetails(parentOrderID).TargetOrder.Count > 0 Then
                                        logger.Debug(String.Format("**** Before remove ***** Target Order count:{0}. {1}", OrderDetails(parentOrderID).TargetOrder.Count, Me.TradableInstrument.TradingSymbol))
                                        OrderDetails(parentOrderID).TargetOrder = OrderDetails(parentOrderID).TargetOrder.Where(Function(x)
                                                                                                                                    Return x.OrderIdentifier <> orderData.OrderIdentifier
                                                                                                                                End Function)
                                        logger.Debug(String.Format("**** After remove ***** Target Order count:{0}. {1}", OrderDetails(parentOrderID).TargetOrder.Count, Me.TradableInstrument.TradingSymbol))
                                    End If
                                End If
                            Else
                                logger.Debug("Cannot process this order as it is already in final stage. Order ID:{0}. {1}", orderData.OrderIdentifier, Me.TradableInstrument.TradingSymbol)
                            End If
                        End If
                    ElseIf orderData.LogicalOrderType <> IOrder.LogicalTypeOfOrder.Parent Then
                        If _TemporaryOrderCollection Is Nothing Then _TemporaryOrderCollection = New Concurrent.ConcurrentBag(Of IOrder)
                        _TemporaryOrderCollection.Add(orderData)
                    ElseIf orderData.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent Then
                        If Me.OrderDetails IsNot Nothing AndAlso Me.OrderDetails.Count > 0 AndAlso Me.OrderDetails.ContainsKey(orderData.OrderIdentifier) Then
                            If Me.OrderDetails(orderData.OrderIdentifier).ParentOrder.Status <> IOrder.TypeOfStatus.Complete AndAlso
                                Me.OrderDetails(orderData.OrderIdentifier).ParentOrder.Status <> IOrder.TypeOfStatus.Cancelled Then
                                Me.OrderDetails(orderData.OrderIdentifier).ParentOrder = orderData
                            Else
                                logger.Debug("Cannot process this order as it is already in final stage. Order ID:{0}, {1}", orderData.OrderIdentifier, Me.TradableInstrument.TradingSymbol)
                            End If
                        Else
                            Dim businessOrderData As BusinessOrder = New BusinessOrder
                            businessOrderData.ParentOrder = orderData
                            businessOrderData.ParentOrderIdentifier = orderData.OrderIdentifier
                            If _TemporaryOrderCollection IsNot Nothing AndAlso _TemporaryOrderCollection.Count > 0 Then
                                Dim slOrderList As List(Of IOrder) = Nothing
                                Dim targetOrderList As List(Of IOrder) = Nothing
                                Dim allOrderList As List(Of IOrder) = Nothing
                                For Each order In _TemporaryOrderCollection
                                    If order.Status <> IOrder.TypeOfStatus.Open AndAlso order.Status <> IOrder.TypeOfStatus.TriggerPending Then
                                        If allOrderList Is Nothing Then allOrderList = New List(Of IOrder)
                                        allOrderList.Add(order)
                                    End If
                                Next
                                For Each order In _TemporaryOrderCollection
                                    If order.Status = IOrder.TypeOfStatus.Open OrElse order.Status = IOrder.TypeOfStatus.TriggerPending Then
                                        Dim availableOrder As IEnumerable(Of IOrder) = Nothing
                                        If allOrderList IsNot Nothing AndAlso allOrderList.Count > 0 Then
                                            availableOrder = allOrderList.Where(Function(x)
                                                                                    Return x.OrderIdentifier = orderData.OrderIdentifier
                                                                                End Function)
                                        End If
                                        If availableOrder Is Nothing OrElse (availableOrder IsNot Nothing AndAlso availableOrder.Count = 0) Then
                                            If order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss Then
                                                If slOrderList Is Nothing Then slOrderList = New List(Of IOrder)
                                                slOrderList.Add(order)
                                            ElseIf order.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target Then
                                                If targetOrderList Is Nothing Then targetOrderList = New List(Of IOrder)
                                                targetOrderList.Add(order)
                                            End If
                                        Else
                                            logger.Debug("Cannot process this order as it is already in final stage. Order ID:{0}. {1}", orderData.OrderIdentifier, Me.TradableInstrument.TradingSymbol)
                                        End If
                                    End If
                                Next
                                businessOrderData.AllOrder = allOrderList
                                businessOrderData.SLOrder = slOrderList
                                businessOrderData.TargetOrder = targetOrderList

                                'Delete from temporary collection
                                If slOrderList IsNot Nothing AndAlso slOrderList.Count > 0 Then
                                    For Each order In slOrderList
                                        _TemporaryOrderCollection.TryTake(order)
                                    Next
                                End If
                                If targetOrderList IsNot Nothing AndAlso targetOrderList.Count > 0 Then
                                    For Each order In targetOrderList
                                        _TemporaryOrderCollection.TryTake(order)
                                    Next
                                End If
                                If allOrderList IsNot Nothing AndAlso allOrderList.Count > 0 Then
                                    For Each order In allOrderList
                                        _TemporaryOrderCollection.TryTake(order)
                                    Next
                                End If
                            End If
                            Await ProcessOrderAsync(businessOrderData).ConfigureAwait(False)
                        End If
                    Else
                        Throw New ApplicationException(String.Format("No reason to be here. Need to check. Order ID:{0}", orderData.OrderIdentifier))
                    End If
                    s.Stop()
                    logger.Debug(String.Format("Order Update complete for {0}. Elapsed time:{1}. SL Count:{2}, Target Count:{3}, All Count:{4}, Parent Status:{5}, Order Status:{6}. {7}",
                                                    orderData.OrderIdentifier,
                                                    s.ElapsedMilliseconds,
                                                    If(OrderDetails(parentOrderID).SLOrder IsNot Nothing, OrderDetails(parentOrderID).SLOrder.Count, "Nothing"),
                                                    If(OrderDetails(parentOrderID).TargetOrder IsNot Nothing, OrderDetails(parentOrderID).TargetOrder.Count, "Nothing"),
                                                    If(OrderDetails(parentOrderID).AllOrder IsNot Nothing, OrderDetails(parentOrderID).AllOrder.Count, "Nothing"),
                                                    OrderDetails(parentOrderID).ParentOrder.Status.ToString,
                                                    orderData.Status.ToString,
                                                    Me.TradableInstrument.TradingSymbol))
                End If
            Catch cex As OperationCanceledException
                logger.Warn(cex)
                Me.ParentStrategy.ParentController.OrphanException = cex
            Catch ex As Exception
                'Neglect error as in the next minute, it will be run again,
                'till that time tick based candles will be used
                logger.Warn(ex)
            Finally
                Interlocked.Exchange(_historicalLock, 0)
            End Try
        End Function
        Public Overridable Async Function ProcessOrderAsync(ByVal orderData As IBusinessOrder) As Task
            'logger.Debug("ProcessOrderAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            If orderData.ParentOrder.Tag IsNot Nothing AndAlso orderData.ParentOrder.Tag <> "" Then
                OrderDetails.AddOrUpdate(orderData.ParentOrderIdentifier, orderData, Function(key, value) orderData)
            Else
                Dim activityTag As String = GenerateTag(orderData.ParentOrder.TimeStamp)
                Dim parentOrderWithTag As IOrder = _APIAdapter.CreateSimilarOrderWithTag(activityTag, orderData.ParentOrder)
                orderData.ParentOrder = parentOrderWithTag
                OrderDetails.AddOrUpdate(orderData.ParentOrderIdentifier, orderData, Function(key, value) orderData)
            End If

            'Modify Activity Details
            'Actvity Signal Status flow diagram
            'Entry Activity: Handled->Activated->Running->Complete/Cancelled/Rejected/Discarded
            'Modify/Cancel Activity: Handled->Activated->Complete/Rejected

            '-------Entry Activity-------'
            _cts.Token.ThrowIfCancellationRequested()
            If orderData.ParentOrder.Status = IOrder.TypeOfStatus.Rejected Then
                Await Me.ParentStrategy.SignalManager.RejectEntryActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                If orderData.ParentOrder.StatusMessage.ToUpper.Contains("BO / CO ORDERS ARE BLOCKED".ToUpper) Then
                    _RMSException = New ZerodhaBusinessException(orderData.ParentOrder.StatusMessage, Nothing, AdapterBusinessException.TypeOfException.RMSError)
                End If
            ElseIf orderData.ParentOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                Await Me.ParentStrategy.SignalManager.CancelEntryActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
            ElseIf orderData.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                Dim runningOrder As Boolean = False
                If orderData.SLOrder IsNot Nothing AndAlso orderData.SLOrder.Count > 0 Then
                    For Each slOrder In orderData.SLOrder
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                            runningOrder = True
                            Exit For
                        End If
                    Next
                ElseIf orderData.AllOrder IsNot Nothing AndAlso orderData.AllOrder.Count > 0 Then
                    For Each allOrder In orderData.AllOrder
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not allOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not allOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                            runningOrder = True
                            Exit For
                        End If
                    Next
                End If
                If runningOrder Then
                    Await Me.ParentStrategy.SignalManager.RunningEntryActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                Else
                    Await Me.ParentStrategy.SignalManager.CompleteEntryActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                End If
            Else
                Await Me.ParentStrategy.SignalManager.UpdateEntryActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
            End If

            _cts.Token.ThrowIfCancellationRequested()
            If Me.ParentStrategy.SignalManager.ActivityDetails IsNot Nothing AndAlso
                Me.ParentStrategy.SignalManager.ActivityDetails.Count > 0 AndAlso
                Me.ParentStrategy.SignalManager.ActivityDetails.ContainsKey(orderData.ParentOrder.Tag) Then
                '-------Cancel Activity-------'
                If Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                    Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                    Dim orderCancelled As Boolean = True
                    Dim statusMessage As String = Nothing
                    Dim currentCancelActivity As ActivityDashboard.Activity = Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).CancelActivity
                    If orderData.SLOrder IsNot Nothing AndAlso orderData.SLOrder.Count > 0 Then
                        For Each slOrder In orderData.SLOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                orderCancelled = False
                                statusMessage = slOrder.StatusMessage
                                Exit For
                            Else
                                statusMessage = slOrder.StatusMessage
                            End If
                        Next
                    ElseIf orderData.AllOrder IsNot Nothing AndAlso orderData.AllOrder.Count > 0 Then
                        For Each allOrder In orderData.AllOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not allOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not allOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                orderCancelled = False
                                statusMessage = allOrder.StatusMessage
                                Exit For
                            Else
                                statusMessage = allOrder.StatusMessage
                            End If
                        Next
                    ElseIf orderData.ParentOrder IsNot Nothing Then
                        If Not orderData.ParentOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not orderData.ParentOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                            orderCancelled = False
                            statusMessage = orderData.ParentOrder.StatusMessage
                        Else
                            statusMessage = orderData.ParentOrder.StatusMessage
                        End If
                    End If
                    If orderCancelled Then
                        Await Me.ParentStrategy.SignalManager.CompleteCancelActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                    Else
                        If DateDiff(DateInterval.Second, currentCancelActivity.ReceivedTime, Now) > Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay Then
                            Await Me.ParentStrategy.SignalManager.RejectCancelActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                        End If
                    End If
                End If

                '-------Modify Stoploss Activity-------'
                _cts.Token.ThrowIfCancellationRequested()
                If Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                    Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).StoplossModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                    Dim orderModified As Boolean = True
                    Dim statusMessage As String = Nothing
                    Dim currentModifyActivity As ActivityDashboard.Activity = Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).StoplossModifyActivity
                    If orderData.SLOrder IsNot Nothing AndAlso orderData.SLOrder.Count > 0 Then
                        For Each slOrder In orderData.SLOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                If slOrder.TriggerPrice <> Val(currentModifyActivity.Supporting) Then
                                    orderModified = False
                                End If
                                statusMessage = slOrder.StatusMessage
                            End If
                        Next
                    ElseIf orderData.AllOrder IsNot Nothing AndAlso orderData.AllOrder.Count > 0 Then
                        For Each allOrder In orderData.AllOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not allOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not allOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                If allOrder.TriggerPrice <> 0 AndAlso allOrder.TriggerPrice <> Val(currentModifyActivity.Supporting) Then
                                    orderModified = False
                                End If
                                statusMessage = allOrder.StatusMessage
                            End If
                        Next
                    End If
                    If orderModified Then
                        Await Me.ParentStrategy.SignalManager.CompleteStoplossModifyActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                    Else
                        If DateDiff(DateInterval.Second, currentModifyActivity.ReceivedTime, Now) > Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay Then
                            Await Me.ParentStrategy.SignalManager.RejectStoplossModifyActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                        End If
                    End If
                End If

                '-------Modify target Activity-------'
                If Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                    Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated Then
                    Dim orderModified As Boolean = True
                    Dim statusMessage As String = Nothing
                    Dim currentModifyActivity As ActivityDashboard.Activity = Me.ParentStrategy.SignalManager.ActivityDetails(orderData.ParentOrder.Tag).TargetModifyActivity
                    If orderData.SLOrder IsNot Nothing AndAlso orderData.SLOrder.Count > 0 Then
                        For Each targetOrder In orderData.TargetOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not targetOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not targetOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                If targetOrder.Price <> Val(currentModifyActivity.Supporting) Then
                                    orderModified = False
                                End If
                                statusMessage = targetOrder.StatusMessage
                            End If
                        Next
                    ElseIf orderData.AllOrder IsNot Nothing AndAlso orderData.AllOrder.Count > 0 Then
                        For Each allOrder In orderData.AllOrder
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not allOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not allOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                If allOrder.Price <> 0 AndAlso allOrder.Price <> Val(currentModifyActivity.Supporting) Then
                                    orderModified = False
                                End If
                                statusMessage = allOrder.StatusMessage
                            End If
                        Next
                    End If
                    If orderModified Then
                        Await Me.ParentStrategy.SignalManager.CompleteTargetModifyActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                    Else
                        If DateDiff(DateInterval.Second, currentModifyActivity.ReceivedTime, Now) > Me.ParentStrategy.ParentController.UserInputs.BackToBackOrderCoolOffDelay Then
                            Await Me.ParentStrategy.SignalManager.RejectTargetModifyActivity(orderData.ParentOrder.Tag, Me, orderData.ParentOrderIdentifier).ConfigureAwait(False)
                        End If
                    End If
                End If
            End If
            Try
                Await Me.ParentStrategy.SignalManager.UIRefresh(Me, True).ConfigureAwait(False)
            Catch ex As Exception
                'logger.Error(ex)
                'Debug.WriteLine("Error from UI refresh")
            End Try
        End Function
        Public Overridable Async Function ProcessHoldingAsync(ByVal holdingData As IHolding) As Task
            'logger.Debug("ProcessHoldingAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            Me.HoldingDetails = holdingData
        End Function
        Public Overridable Async Function ProcessPositionAsync(ByVal positionData As IPosition) As Task
            'logger.Debug("ProcessPositionAsync, parameters:{0}", Utilities.Strings.JsonSerialize(orderData))
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            Me.PositionDetails = positionData
        End Function
        Public Overridable Function GetAllActiveOrders(ByVal signalDirection As IOrder.TypeOfTransaction) As List(Of IOrder)
            Dim ret As List(Of IOrder) = Nothing
            'Dim direction As String = Nothing
            'If signalDirection = IOrder.TypeOfTransaction.Buy Then
            '    direction = "BUY"
            'ElseIf signalDirection = IOrder.TypeOfTransaction.Sell Then
            '    direction = "SELL"
            'End If
            If OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
                For Each parentOrderId In OrderDetails.Keys
                    Dim parentBusinessOrder As IBusinessOrder = OrderDetails(parentOrderId)
                    If parentBusinessOrder IsNot Nothing AndAlso parentBusinessOrder.ParentOrder IsNot Nothing Then
                        If signalDirection = IOrder.TypeOfTransaction.None OrElse parentBusinessOrder.ParentOrder.TransactionType = signalDirection Then
                            'If parentBusinessOrder.ParentOrder.Status = "COMPLETE" OrElse parentBusinessOrder.ParentOrder.Status = "OPEN" Then
                            If Not parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Rejected Then
                                If parentBusinessOrder.SLOrder IsNot Nothing AndAlso parentBusinessOrder.SLOrder.Count > 0 Then
                                    Dim parentNeedToInsert As Boolean = False
                                    For Each slOrder In parentBusinessOrder.SLOrder
                                        If Not slOrder.Status = IOrder.TypeOfStatus.Complete AndAlso Not slOrder.Status = IOrder.TypeOfStatus.Cancelled Then
                                            If ret Is Nothing Then ret = New List(Of IOrder)
                                            ret.Add(slOrder)
                                            parentNeedToInsert = True
                                        End If
                                    Next
                                    If ret Is Nothing Then ret = New List(Of IOrder)
                                    If parentNeedToInsert Then ret.Add(parentBusinessOrder.ParentOrder)
                                Else
                                    If parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Complete Then
                                        If (parentBusinessOrder.SLOrder Is Nothing OrElse parentBusinessOrder.SLOrder.Count = 0) AndAlso
                                            (parentBusinessOrder.AllOrder Is Nothing OrElse parentBusinessOrder.AllOrder.Count = 0) Then
                                            If ret Is Nothing Then ret = New List(Of IOrder)
                                            ret.Add(parentBusinessOrder.ParentOrder)
                                        End If
                                    End If
                                End If
                                If ret Is Nothing Then ret = New List(Of IOrder)
                                If parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.Open Then ret.Add(parentBusinessOrder.ParentOrder)
                                If parentBusinessOrder.ParentOrder.Status = IOrder.TypeOfStatus.TriggerPending Then ret.Add(parentBusinessOrder.ParentOrder)
                            End If
                        End If
                    End If
                Next
            End If
            Return ret
        End Function
        Public Overridable Function GetActiveOrder(ByVal signalDirection As IOrder.TypeOfTransaction) As IBusinessOrder
            'logger.Debug("GetActiveOrder, parameters:Nothing")
            Dim ret As IBusinessOrder = Nothing
            Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(signalDirection)
            If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
                Dim parentOrders As List(Of IOrder) = allActiveOrders.FindAll(Function(x)
                                                                                  Return x.ParentOrderIdentifier Is Nothing
                                                                              End Function)
                If parentOrders IsNot Nothing AndAlso parentOrders.Count > 0 Then
                    ret = OrderDetails(parentOrders.FirstOrDefault.OrderIdentifier)
                End If
            End If
            Return ret
        End Function
        Protected Overridable Function GetAllCancelableOrders(ByVal signalDirection As IOrder.TypeOfTransaction) As List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
            Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
            Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(signalDirection)
            If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
                For Each activeOrder In allActiveOrders
                    If Not activeOrder.Status = IOrder.TypeOfStatus.Complete Then
                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, activeOrder, ""))
                    End If
                Next
            End If
            Return ret
        End Function
        Public Overridable Async Function ForceExitAllTradesAsync(ByVal reason As String) As Task(Of Boolean)
            Dim ret As Boolean = False
            Try
                Dim allCancelableOrders As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = GetAllCancelableOrders(IOrder.TypeOfTransaction.None)
                If allCancelableOrders IsNot Nothing AndAlso allCancelableOrders.Count > 0 Then
                    For Each cancelableOrder In allCancelableOrders
                        Await ForceExitSpecificTradeAsync(cancelableOrder.Item2, reason).ConfigureAwait(False)
                    Next
                    ret = True
                End If
            Catch cex As OperationCanceledException
                logger.Error(cex)
                Me.ParentStrategy.ParentController.OrphanException = cex
            Catch ex As Exception
                logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
                Throw ex
            End Try
            Return ret
        End Function
#End Region

#Region "Public MustOverride Functions"
        Public MustOverride Async Function MonitorAsync() As Task
        Public MustOverride Async Function MonitorAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task
        Protected MustOverride Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)))
        Protected MustOverride Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean, ByVal data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Protected MustOverride Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Protected MustOverride Async Function IsTriggerReceivedForModifyTargetOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Protected MustOverride Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Protected MustOverride Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean, ByVal data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Protected MustOverride Async Function ForceExitSpecificTradeAsync(ByVal order As IOrder, ByVal reason As String) As Task
#End Region

#Region "Excecute Command"
        ''' <summary>
        ''' To run in diffrent thread it is not defined in Strategy level
        ''' </summary>
        ''' <param name="command"></param>
        ''' <param name="data"></param>
        ''' <returns></returns>
        Protected Async Function ExecuteCommandAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task(Of Object)
            'logger.Debug("ExecuteCommandAsync, parameters:{0},{1}", command, Utilities.Strings.JsonSerialize(data))
            Dim ret As Object = Nothing
            Dim lastException As Exception = Nothing
            Dim allOKWithoutException As Boolean = False
            Using Waiter As New Waiter(_cts)
                AddHandler Waiter.Heartbeat, AddressOf OnHeartbeat
                AddHandler Waiter.WaitingFor, AddressOf OnWaitingFor
                Dim apiConnectionBeingUsed As IConnection = Me.ParentStrategy.ParentController.APIConnection
                Dim orderResponses As Concurrent.ConcurrentBag(Of Object) = Nothing
                For retryCtr = 1 To _MaxReTries
                    _cts.Token.ThrowIfCancellationRequested()
                    lastException = Nothing
                    While Me.ParentStrategy.ParentController.APIConnection Is Nothing OrElse apiConnectionBeingUsed Is Nothing OrElse
                        (Me.ParentStrategy.ParentController.APIConnection IsNot Nothing AndAlso apiConnectionBeingUsed IsNot Nothing AndAlso
                        Not Me.ParentStrategy.ParentController.APIConnection.Equals(apiConnectionBeingUsed))
                        apiConnectionBeingUsed = Me.ParentStrategy.ParentController.APIConnection
                        _cts.Token.ThrowIfCancellationRequested()
                        logger.Debug("Waiting for fresh token before running command:{0}", command.ToString)
                        Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End While
                    _APIAdapter.SetAPIAccessToken(Me.ParentStrategy.ParentController.APIConnection.AccessToken)

                    logger.Debug("Firing command:{0} for {1}", command.ToString, Me.TradableInstrument.TradingSymbol)
                    OnDocumentRetryStatus(retryCtr, _MaxReTries)
                    Try
                        _cts.Token.ThrowIfCancellationRequested()
                        Select Case command
                            Case ExecuteCommands.ModifyStoplossOrder
                                Dim modifyStoplossOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyStoplossOrderAsync(True).ConfigureAwait(False)
                                If modifyStoplossOrderTriggers IsNot Nothing AndAlso modifyStoplossOrderTriggers.Count > 0 Then
                                    Dim tasks = modifyStoplossOrderTriggers.Select(Async Function(x)
                                                                                       Try
                                                                                           _cts.Token.ThrowIfCancellationRequested()
                                                                                           If x.Item1 = ExecuteCommandAction.Take Then
                                                                                               Await Me.ParentStrategy.SignalManager.HandleStoplossModifyActivity(x.Item2.Tag, Me, Nothing, Now, x.Item3, x.Item4).ConfigureAwait(False)
                                                                                               Dim modifyStoplossOrderResponse As Dictionary(Of String, Object) = Nothing

                                                                                               modifyStoplossOrderResponse = Await _APIAdapter.ModifyStoplossOrderAsync(orderId:=x.Item2.OrderIdentifier, triggerPrice:=x.Item3).ConfigureAwait(False)

                                                                                               If modifyStoplossOrderResponse IsNot Nothing Then
                                                                                                   logger.Debug("Modify stoploss order is completed, modifyStoplossOrderResponse:{0}, {1}", Strings.JsonSerialize(modifyStoplossOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                                   Await Me.ParentStrategy.SignalManager.ActivateStoplossModifyActivity(x.Item2.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                                                                                   lastException = Nothing
                                                                                                   allOKWithoutException = True
                                                                                                   _cts.Token.ThrowIfCancellationRequested()
                                                                                                   If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                                   orderResponses.Add(modifyStoplossOrderResponse)
                                                                                                   _cts.Token.ThrowIfCancellationRequested()
                                                                                               Else
                                                                                                   Throw New ApplicationException(String.Format("Modify stoploss order did not succeed"))
                                                                                               End If
                                                                                           End If
                                                                                       Catch ex As Exception
                                                                                           logger.Error(ex)
                                                                                           Throw ex
                                                                                       End Try
                                                                                       Return True
                                                                                   End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.ModifyTargetOrder
                                Dim modifyTargetOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyTargetOrderAsync(True).ConfigureAwait(False)
                                If modifyTargetOrderTriggers IsNot Nothing AndAlso modifyTargetOrderTriggers.Count > 0 Then
                                    Dim tasks = modifyTargetOrderTriggers.Select(Async Function(x)
                                                                                     Try
                                                                                         _cts.Token.ThrowIfCancellationRequested()
                                                                                         If x.Item1 = ExecuteCommandAction.Take Then
                                                                                             Await Me.ParentStrategy.SignalManager.HandleTargetModifyActivity(x.Item2.Tag, Me, Nothing, Now, x.Item3, x.Item4).ConfigureAwait(False)
                                                                                             Dim modifyTargetOrderResponse As Dictionary(Of String, Object) = Nothing

                                                                                             modifyTargetOrderResponse = Await _APIAdapter.ModifyTargetOrderAsync(orderId:=x.Item2.OrderIdentifier, price:=x.Item3).ConfigureAwait(False)

                                                                                             If modifyTargetOrderResponse IsNot Nothing Then
                                                                                                 logger.Debug("Modify target order is completed, modifyTargetOrderResponse:{0}, {1}", Strings.JsonSerialize(modifyTargetOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                                 Await Me.ParentStrategy.SignalManager.ActivateTargetModifyActivity(x.Item2.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                                                                                 lastException = Nothing
                                                                                                 allOKWithoutException = True
                                                                                                 _cts.Token.ThrowIfCancellationRequested()
                                                                                                 If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                                 orderResponses.Add(modifyTargetOrderResponse)
                                                                                                 _cts.Token.ThrowIfCancellationRequested()
                                                                                             Else
                                                                                                 Throw New ApplicationException(String.Format("Modify target order did not succeed"))
                                                                                             End If
                                                                                         End If
                                                                                     Catch ex As Exception
                                                                                         logger.Error(ex)
                                                                                         Throw ex
                                                                                     End Try
                                                                                     Return True
                                                                                 End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.CancelBOOrder, ExecuteCommands.CancelCOOrder, ExecuteCommands.CancelRegularOrder, ExecuteCommands.ForceCancelBOOrder, ExecuteCommands.ForceCancelCOOrder, ExecuteCommands.ForceCancelRegularOrder
                                Dim cancelOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
                                Select Case command
                                    Case ExecuteCommands.ForceCancelBOOrder, ExecuteCommands.ForceCancelCOOrder, ExecuteCommands.ForceCancelRegularOrder
                                        cancelOrderTriggers = data
                                    Case ExecuteCommands.CancelBOOrder, ExecuteCommands.CancelCOOrder, ExecuteCommands.CancelRegularOrder
                                        cancelOrderTriggers = Await IsTriggerReceivedForExitOrderAsync(True).ConfigureAwait(False)
                                End Select
                                If cancelOrderTriggers IsNot Nothing AndAlso cancelOrderTriggers.Count > 0 Then
                                    Dim tasks = cancelOrderTriggers.Select(Async Function(x)
                                                                               Try
                                                                                   _cts.Token.ThrowIfCancellationRequested()
                                                                                   If x.Item1 = ExecuteCommandAction.Take Then
                                                                                       Await Me.ParentStrategy.SignalManager.HandleCancelActivity(x.Item2.Tag, Me, Nothing, Now, x.Item3).ConfigureAwait(False)
                                                                                       Dim cancelOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                       Select Case command
                                                                                           Case ExecuteCommands.CancelBOOrder, ExecuteCommands.ForceCancelBOOrder
                                                                                               cancelOrderResponse = Await _APIAdapter.CancelBOOrderAsync(orderId:=x.Item2.OrderIdentifier, parentOrderID:=x.Item2.ParentOrderIdentifier).ConfigureAwait(False)
                                                                                           Case ExecuteCommands.CancelCOOrder, ExecuteCommands.ForceCancelCOOrder
                                                                                               cancelOrderResponse = Await _APIAdapter.CancelCOOrderAsync(orderId:=x.Item2.OrderIdentifier, parentOrderID:=x.Item2.ParentOrderIdentifier).ConfigureAwait(False)
                                                                                           Case ExecuteCommands.CancelRegularOrder, ExecuteCommands.ForceCancelRegularOrder
                                                                                               cancelOrderResponse = Await _APIAdapter.CancelRegularOrderAsync(orderId:=x.Item2.OrderIdentifier, parentOrderID:=x.Item2.ParentOrderIdentifier).ConfigureAwait(False)
                                                                                       End Select
                                                                                       If cancelOrderResponse IsNot Nothing Then
                                                                                           logger.Debug("Cancel order is completed, cancelOrderResponse:{0}, {1}", Strings.JsonSerialize(cancelOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                           Await Me.ParentStrategy.SignalManager.ActivateCancelActivity(x.Item2.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                                                                           lastException = Nothing
                                                                                           allOKWithoutException = True
                                                                                           _cts.Token.ThrowIfCancellationRequested()
                                                                                           If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                           orderResponses.Add(cancelOrderResponse)
                                                                                           _cts.Token.ThrowIfCancellationRequested()
                                                                                       Else
                                                                                           Throw New ApplicationException(String.Format("Cancel order did not succeed"))
                                                                                       End If
                                                                                   End If
                                                                               Catch ex As Exception
                                                                                   logger.Error(ex)
                                                                                   Throw ex
                                                                               End Try
                                                                               Return True
                                                                           End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceBOLimitMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceBOLimitMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                    tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                    transaction:=x.Item2.EntryDirection,
                                                                                                                                                    quantity:=x.Item2.Quantity,
                                                                                                                                                    price:=x.Item2.Price,
                                                                                                                                                    squareOffValue:=x.Item2.SquareOffValue,
                                                                                                                                                    stopLossValue:=x.Item2.StoplossValue,
                                                                                                                                                    tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceBOSLMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceBOSLMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                  tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                  transaction:=x.Item2.EntryDirection,
                                                                                                                                                  quantity:=x.Item2.Quantity,
                                                                                                                                                  price:=x.Item2.Price,
                                                                                                                                                  triggerPrice:=x.Item2.TriggerPrice,
                                                                                                                                                  squareOffValue:=x.Item2.SquareOffValue,
                                                                                                                                                  stopLossValue:=x.Item2.StoplossValue,
                                                                                                                                                  tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceCOMarketMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceCOMarketMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                    tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                    transaction:=x.Item2.EntryDirection,
                                                                                                                                                    quantity:=x.Item2.Quantity,
                                                                                                                                                    triggerPrice:=x.Item2.TriggerPrice,
                                                                                                                                                    tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceRegularMarketMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceRegularMarketMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                            tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                            transaction:=x.Item2.EntryDirection,
                                                                                                                                                            quantity:=x.Item2.Quantity,
                                                                                                                                                            tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceRegularLimitMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceRegularLimitMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                            tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                            transaction:=x.Item2.EntryDirection,
                                                                                                                                                            quantity:=x.Item2.Quantity,
                                                                                                                                                            price:=x.Item2.Price,
                                                                                                                                                            tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceRegularSLMMISOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceRegularSLMMISOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                        tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                        transaction:=x.Item2.EntryDirection,
                                                                                                                                                        quantity:=x.Item2.Quantity,
                                                                                                                                                        triggerPrice:=x.Item2.TriggerPrice,
                                                                                                                                                        tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                            Case ExecuteCommands.PlaceRegularMarketCNCOrder
                                Dim placeOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)) = Await IsTriggerReceivedForPlaceOrderAsync(True).ConfigureAwait(False)
                                If placeOrderTriggers IsNot Nothing AndAlso placeOrderTriggers.Count > 0 Then
                                    Await GenerateTagForPlaceOrderTriggers(placeOrderTriggers).ConfigureAwait(False)
                                    Dim tasks = placeOrderTriggers.Select(Async Function(x)
                                                                              Try
                                                                                  _cts.Token.ThrowIfCancellationRequested()
                                                                                  If x.Item1 = ExecuteCommandAction.Take OrElse x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                      If x.Item1 = ExecuteCommandAction.WaitAndTake Then
                                                                                          x.Item2.Tag = Await WaitAndGenerateFreshTag(x.Item2.Tag).ConfigureAwait(False)
                                                                                      End If

                                                                                      Await Me.ParentStrategy.SignalManager.HandleEntryActivity(x.Item2.Tag, Me, Nothing, x.Item2.SignalCandle.SnapshotDateTime, x.Item2.EntryDirection, Now, x.Item3).ConfigureAwait(False)

                                                                                      Dim placeOrderResponse As Dictionary(Of String, Object) = Nothing
                                                                                      placeOrderResponse = Await _APIAdapter.PlaceRegularMarketCNCOrderAsync(tradeExchange:=Me.TradableInstrument.RawExchange,
                                                                                                                                                            tradingSymbol:=Me.TradableInstrument.TradingSymbol,
                                                                                                                                                            transaction:=x.Item2.EntryDirection,
                                                                                                                                                            quantity:=x.Item2.Quantity,
                                                                                                                                                            tag:=x.Item2.Tag).ConfigureAwait(False)
                                                                                      If placeOrderResponse IsNot Nothing Then
                                                                                          logger.Debug("Place order is completed, placeOrderResponse:{0}, {1}", Strings.JsonSerialize(placeOrderResponse), Me.TradableInstrument.TradingSymbol)
                                                                                          Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(x.Item2.Tag, Me, placeOrderResponse("data")("order_id"), Now).ConfigureAwait(False)
                                                                                          lastException = Nothing
                                                                                          allOKWithoutException = True
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                          If orderResponses Is Nothing Then orderResponses = New Concurrent.ConcurrentBag(Of Object)
                                                                                          orderResponses.Add(placeOrderResponse)
                                                                                          _cts.Token.ThrowIfCancellationRequested()
                                                                                      Else
                                                                                          Throw New ApplicationException(String.Format("Place order did not succeed"))
                                                                                      End If
                                                                                  Else
                                                                                      lastException = Nothing
                                                                                      allOKWithoutException = True
                                                                                      _cts.Token.ThrowIfCancellationRequested()
                                                                                  End If
                                                                              Catch ex As Exception
                                                                                  logger.Error("{0}: {1}", Me.TradableInstrument.TradingSymbol, ex.ToString)
                                                                                  Me.ParentStrategy.SignalManager.DiscardEntryActivity(x.Item2.Tag, Me, Nothing, Now, ex)
                                                                                  Throw ex
                                                                              End Try
                                                                              Return True
                                                                          End Function)
                                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                                    ret = orderResponses
                                Else
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                End If
                        End Select
                    Catch aex As AdapterBusinessException
                        logger.Error("{0}, {1}", Me.TradableInstrument.TradingSymbol, aex)
                        lastException = aex
                        Select Case aex.ExceptionType
                            Case AdapterBusinessException.TypeOfException.TokenException
                                Continue For
                            Case AdapterBusinessException.TypeOfException.DataException
                                Continue For
                            Case AdapterBusinessException.TypeOfException.NetworkException
                                Continue For
                            Case AdapterBusinessException.TypeOfException.InputException
                                Continue For
                            Case Else
                                Exit For
                        End Select
                    Catch opx As OperationCanceledException
                        logger.Error("{0}, {1}", Me.TradableInstrument.TradingSymbol, opx)
                        lastException = opx
                        If Not _cts.Token.IsCancellationRequested Then
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                                'Provide required wait in case internet was already up
                                logger.Debug("HTTP->Task was cancelled without internet problem:{0}",
                                             opx.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Non-explicit cancellation")
                                _cts.Token.ThrowIfCancellationRequested()
                            Else
                                logger.Debug("HTTP->Task was cancelled due to internet problem:{0}, waited prescribed seconds, will now retry",
                                             opx.Message)
                                'Since internet was down, no need to consume retries
                                retryCtr -= 1
                            End If
                        End If
                    Catch hex As HttpRequestException
                        logger.Error("{0}, {1}", Me.TradableInstrument.TradingSymbol, hex)
                        lastException = hex
                        If ExceptionExtensions.GetExceptionMessages(hex).Contains("trust relationship") Then
                            Throw New ForbiddenException(hex.Message, hex, ForbiddenException.TypeOfException.PossibleReloginRequired)
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                            If hex.Message.Contains("429") Or hex.Message.Contains("503") Then
                                logger.Debug("HTTP->429/503 error without internet problem:{0}",
                                             hex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnServiceUnavailbleFailure.TotalSeconds, "Service unavailable(429/503)")
                                _cts.Token.ThrowIfCancellationRequested()
                                'Since site service is blocked, no need to consume retries
                                retryCtr -= 1
                            ElseIf hex.Message.Contains("404") Then
                                logger.Debug("HTTP->404 error without internet problem:{0}",
                                             hex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                'No point retrying, exit for
                                Exit For
                            Else
                                If ExceptionExtensions.IsExceptionConnectionRelated(hex) Then
                                    logger.Debug("HTTP->HttpRequestException without internet problem but of type internet related detected:{0}",
                                                 hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Waiter.SleepRequiredDuration(_WaitDurationOnConnectionFailure.TotalSeconds, "Connection HttpRequestException")
                                    _cts.Token.ThrowIfCancellationRequested()
                                    'Since exception was internet related, no need to consume retries
                                    retryCtr -= 1
                                Else
                                    'Provide required wait in case internet was already up
                                    logger.Debug("HTTP->HttpRequestException without internet problem:{0}",
                                                 hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Unknown HttpRequestException:" & hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                End If
                            End If
                        Else
                            logger.Debug("HTTP->HttpRequestException with internet problem:{0}, waited prescribed seconds, will now retry",
                                         hex.Message)
                            'Since internet was down, no need to consume retries
                            retryCtr -= 1
                        End If
                    Catch ex As Exception
                        logger.Error("{0}, {1}", Me.TradableInstrument.TradingSymbol, ex)
                        lastException = ex
                        'Exit if it is a network failure check and stop retry to avoid stack overflow
                        'Need to relogin, no point retrying
                        If ExceptionExtensions.GetExceptionMessages(ex).Contains("disposed") Then
                            Throw New ForbiddenException(ex.Message, ex, ForbiddenException.TypeOfException.ExceptionInBetweenLoginProcess)
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                            'Provide required wait in case internet was already up
                            _cts.Token.ThrowIfCancellationRequested()
                            If ExceptionExtensions.IsExceptionConnectionRelated(ex) Then
                                logger.Debug("HTTP->Exception without internet problem but of type internet related detected:{0}",
                                             ex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnConnectionFailure.TotalSeconds, "Connection Exception")
                                _cts.Token.ThrowIfCancellationRequested()
                                'Since exception was internet related, no need to consume retries
                                retryCtr -= 1
                            Else
                                logger.Debug("HTTP->Exception without internet problem of unknown type detected:{0}",
                                             ex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Unknown Exception")
                                _cts.Token.ThrowIfCancellationRequested()
                            End If
                        Else
                            logger.Debug("HTTP->Exception with internet problem:{0}, waited prescribed seconds, will now retry",
                                         ex.Message)
                            'Since internet was down, no need to consume retries
                            retryCtr -= 1
                        End If
                    Finally
                        OnDocumentDownloadComplete()
                    End Try
                    _cts.Token.ThrowIfCancellationRequested()
                    If ret IsNot Nothing Then
                        Exit For
                    End If
                    GC.Collect()
                Next
                RemoveHandler Waiter.Heartbeat, AddressOf OnHeartbeat
                RemoveHandler Waiter.WaitingFor, AddressOf OnWaitingFor
            End Using
            _cts.Token.ThrowIfCancellationRequested()
            If Not allOKWithoutException Then
                Throw lastException
            End If
            Return ret
        End Function
#End Region

#Region "Paper Trade"
        Private _placeOrderLock As Integer = 0
        Protected Async Function TakeCOPaperTradeAsync(ByVal data As Object, Optional ByVal entryImmediately As Boolean = False, Optional ByVal currentTick As ITick = Nothing) As Task(Of List(Of IBusinessOrder))
            Dim ret As List(Of IBusinessOrder) = Nothing
            'logger.Debug(String.Format("Before Place Lock:{0}, {1}", Interlocked.Read(_placeOrderLock), Me.TradableInstrument.TradingSymbol))
            If 0 = Interlocked.Exchange(_placeOrderLock, 1) Then
                'logger.Debug(String.Format("After Place Lock:{0}, {1}", Interlocked.Read(_placeOrderLock), Me.TradableInstrument.TradingSymbol))
                Try
                    Dim activityTag As String = GenerateTag(Now)
                    Dim parentPlaceOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = data
                    If parentPlaceOrderTriggers IsNot Nothing AndAlso parentPlaceOrderTriggers.Count > 0 Then
                        For Each runningPlaceOrderTrigger In parentPlaceOrderTriggers
                            If runningPlaceOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                                logger.Debug("Place Order Details-> Direction:{0}, Qunatity:{1}, Trigger Price:{2}",
                                     runningPlaceOrderTrigger.Item3.EntryDirection.ToString, runningPlaceOrderTrigger.Item3.Quantity, runningPlaceOrderTrigger.Item3.TriggerPrice)

                                If Not entryImmediately Then
                                    Dim lastTradeTime As Date = Me.TradableInstrument.LastTick.LastTradeTime.Value
                                    While Utilities.Time.IsTimeEqualTillSeconds(Me.TradableInstrument.LastTick.LastTradeTime.Value, lastTradeTime)
                                        Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                                    End While
                                End If

                                activityTag = GenerateFreshTagForNewSignal(activityTag, runningPlaceOrderTrigger.Item3.SignalCandle.SnapshotDateTime, runningPlaceOrderTrigger.Item3.EntryDirection, runningPlaceOrderTrigger.Item3.GenerateDifferentTag)

                                If runningPlaceOrderTrigger.Item1 = ExecuteCommandAction.WaitAndTake Then activityTag = Await WaitAndGenerateFreshTag(activityTag).ConfigureAwait(False)

                                Await Me.ParentStrategy.SignalManager.HandleEntryActivity(activityTag, Me, Nothing, runningPlaceOrderTrigger.Item3.SignalCandle.SnapshotDateTime, runningPlaceOrderTrigger.Item3.EntryDirection, Now, runningPlaceOrderTrigger.Item4).ConfigureAwait(False)

                                Dim entryPrice As Decimal = Decimal.MinValue
                                If currentTick IsNot Nothing Then
                                    entryPrice = currentTick.LastPrice
                                Else
                                    entryPrice = Me.TradableInstrument.LastTick.LastPrice
                                End If

                                Dim parentOrder As PaperOrder = New PaperOrder
                                parentOrder.AveragePrice = entryPrice
                                parentOrder.Quantity = runningPlaceOrderTrigger.Item3.Quantity
                                parentOrder.Status = IOrder.TypeOfStatus.Complete
                                parentOrder.InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                parentOrder.OrderIdentifier = Utilities.Numbers.GetUniqueNumber()
                                parentOrder.ParentOrderIdentifier = Nothing
                                parentOrder.Tradingsymbol = Me.TradableInstrument.TradingSymbol
                                parentOrder.TransactionType = runningPlaceOrderTrigger.Item3.EntryDirection
                                parentOrder.TimeStamp = Now
                                parentOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent
                                parentOrder.Tag = activityTag

                                Dim slOrder As PaperOrder = New PaperOrder
                                slOrder.TriggerPrice = runningPlaceOrderTrigger.Item3.TriggerPrice
                                slOrder.Quantity = runningPlaceOrderTrigger.Item3.Quantity
                                slOrder.Status = IOrder.TypeOfStatus.TriggerPending
                                slOrder.InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                slOrder.OrderIdentifier = Utilities.Numbers.GetUniqueNumber()
                                slOrder.ParentOrderIdentifier = parentOrder.OrderIdentifier
                                slOrder.Tradingsymbol = Me.TradableInstrument.TradingSymbol
                                slOrder.TransactionType = If(runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Buy, IOrder.TypeOfTransaction.Sell, IOrder.TypeOfTransaction.Buy)
                                slOrder.TimeStamp = Now
                                slOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                                slOrder.Tag = activityTag

                                Dim slOrderList As List(Of IOrder) = New List(Of IOrder)
                                slOrderList.Add(slOrder)
                                Dim parentBOrder As BusinessOrder = New BusinessOrder
                                parentBOrder.ParentOrder = parentOrder
                                parentBOrder.SLOrder = slOrderList
                                parentBOrder.ParentOrderIdentifier = parentOrder.OrderIdentifier

                                Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(activityTag, Me, parentBOrder.ParentOrderIdentifier, Now).ConfigureAwait(False)
                                logger.Debug("Order Placed {0}, Time:{1}", Me.TradableInstrument.TradingSymbol, Now.ToString)
                                Await ProcessOrderAsync(parentBOrder).ConfigureAwait(False)
                                'Me.OrderDetails.AddOrUpdate(parentBOrder.ParentOrderIdentifier, parentBOrder, Function(key, value) parentBOrder)
                                If ret Is Nothing Then ret = New List(Of IBusinessOrder)
                                ret.Add(OrderDetails(parentBOrder.ParentOrderIdentifier))
                                OnHeartbeat(String.Format("Place Order Successful. Order ID:{0}", parentBOrder.ParentOrderIdentifier))
                            End If
                        Next
                    End If
                Finally
                    'logger.Debug("Releasing lock")
                    Interlocked.Exchange(_placeOrderLock, 0)
                End Try
            End If
            Return ret
        End Function

        Protected Async Function TakeBOPaperTradeAsync(ByVal data As Object, Optional ByVal entryImmediately As Boolean = False, Optional ByVal currentTick As ITick = Nothing) As Task(Of List(Of IBusinessOrder))
            Dim ret As List(Of IBusinessOrder) = Nothing
            'logger.Debug(String.Format("Before Place Lock:{0}, {1}", Interlocked.Read(_placeOrderLock), Me.TradableInstrument.TradingSymbol))
            If 0 = Interlocked.Exchange(_placeOrderLock, 1) Then
                'logger.Debug(String.Format("After Place Lock:{0}, {1}", Interlocked.Read(_placeOrderLock), Me.TradableInstrument.TradingSymbol))
                Try
                    Dim activityTag As String = GenerateTag(Now)
                    Dim parentPlaceOrderTriggers As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)) = data
                    If parentPlaceOrderTriggers IsNot Nothing AndAlso parentPlaceOrderTriggers.Count Then
                        For Each runningPlaceOrderTrigger In parentPlaceOrderTriggers
                            If runningPlaceOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                                logger.Debug("Place Order Details-> Direction:{0}, Qunatity:{1}, Trigger Price:{2}",
                                     runningPlaceOrderTrigger.Item3.EntryDirection.ToString, runningPlaceOrderTrigger.Item3.Quantity, runningPlaceOrderTrigger.Item3.TriggerPrice)

                                If Not entryImmediately Then
                                    Dim lastTradeTime As Date = Me.TradableInstrument.LastTick.LastTradeTime.Value
                                    While Utilities.Time.IsTimeEqualTillSeconds(Me.TradableInstrument.LastTick.LastTradeTime.Value, lastTradeTime)
                                        Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                                    End While
                                End If

                                activityTag = GenerateFreshTagForNewSignal(activityTag, runningPlaceOrderTrigger.Item3.SignalCandle.SnapshotDateTime, runningPlaceOrderTrigger.Item3.EntryDirection, runningPlaceOrderTrigger.Item3.GenerateDifferentTag)

                                If runningPlaceOrderTrigger.Item1 = ExecuteCommandAction.WaitAndTake Then activityTag = Await WaitAndGenerateFreshTag(activityTag).ConfigureAwait(False)

                                Await Me.ParentStrategy.SignalManager.HandleEntryActivity(activityTag, Me, Nothing, runningPlaceOrderTrigger.Item3.SignalCandle.SnapshotDateTime, runningPlaceOrderTrigger.Item3.EntryDirection, Now, runningPlaceOrderTrigger.Item4).ConfigureAwait(False)

                                Dim entryPrice As Decimal = Decimal.MinValue
                                Dim entryTime As Date = Date.MinValue
                                If currentTick IsNot Nothing Then
                                    entryPrice = currentTick.LastPrice
                                    entryTime = currentTick.Timestamp.Value
                                Else
                                    entryPrice = Me.TradableInstrument.LastTick.LastPrice
                                    entryTime = Me.TradableInstrument.LastTick.Timestamp.Value
                                End If

                                Dim parentOrder As PaperOrder = New PaperOrder
                                parentOrder.AveragePrice = entryPrice
                                parentOrder.Quantity = runningPlaceOrderTrigger.Item3.Quantity
                                parentOrder.Status = IOrder.TypeOfStatus.Complete
                                parentOrder.InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                parentOrder.OrderIdentifier = Utilities.Numbers.GetUniqueNumber()
                                parentOrder.ParentOrderIdentifier = Nothing
                                parentOrder.Tradingsymbol = Me.TradableInstrument.TradingSymbol
                                parentOrder.TransactionType = runningPlaceOrderTrigger.Item3.EntryDirection
                                parentOrder.TimeStamp = entryTime
                                parentOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent
                                parentOrder.Tag = activityTag

                                Dim slOrder As PaperOrder = New PaperOrder
                                slOrder.TransactionType = If(runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Buy, IOrder.TypeOfTransaction.Sell, IOrder.TypeOfTransaction.Buy)
                                If runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Buy Then
                                    slOrder.TriggerPrice = parentOrder.AveragePrice - runningPlaceOrderTrigger.Item3.StoplossValue
                                ElseIf runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Sell Then
                                    slOrder.TriggerPrice = parentOrder.AveragePrice + runningPlaceOrderTrigger.Item3.StoplossValue
                                End If
                                slOrder.Quantity = runningPlaceOrderTrigger.Item3.Quantity
                                slOrder.Status = IOrder.TypeOfStatus.TriggerPending
                                slOrder.InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                slOrder.OrderIdentifier = Utilities.Numbers.GetUniqueNumber()
                                slOrder.ParentOrderIdentifier = parentOrder.OrderIdentifier
                                slOrder.Tradingsymbol = Me.TradableInstrument.TradingSymbol
                                slOrder.TimeStamp = entryTime
                                slOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                                slOrder.Tag = activityTag

                                Dim targetOrder As PaperOrder = New PaperOrder
                                targetOrder.TransactionType = If(runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Buy, IOrder.TypeOfTransaction.Sell, IOrder.TypeOfTransaction.Buy)
                                If runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Buy Then
                                    targetOrder.AveragePrice = parentOrder.AveragePrice + runningPlaceOrderTrigger.Item3.SquareOffValue
                                ElseIf runningPlaceOrderTrigger.Item3.EntryDirection = IOrder.TypeOfTransaction.Sell Then
                                    targetOrder.AveragePrice = parentOrder.AveragePrice - runningPlaceOrderTrigger.Item3.SquareOffValue
                                End If
                                targetOrder.Quantity = runningPlaceOrderTrigger.Item3.Quantity
                                targetOrder.Status = IOrder.TypeOfStatus.Open
                                targetOrder.InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                targetOrder.OrderIdentifier = Utilities.Numbers.GetUniqueNumber()
                                targetOrder.ParentOrderIdentifier = parentOrder.OrderIdentifier
                                targetOrder.Tradingsymbol = Me.TradableInstrument.TradingSymbol
                                targetOrder.TimeStamp = entryTime
                                targetOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target
                                targetOrder.Tag = activityTag

                                Dim slOrderList As List(Of IOrder) = New List(Of IOrder)
                                slOrderList.Add(slOrder)
                                Dim targetOrderList As List(Of IOrder) = New List(Of IOrder)
                                targetOrderList.Add(targetOrder)
                                Dim parentBOrder As BusinessOrder = New BusinessOrder
                                parentBOrder.ParentOrder = parentOrder
                                parentBOrder.SLOrder = slOrderList
                                parentBOrder.TargetOrder = targetOrderList
                                parentBOrder.ParentOrderIdentifier = parentOrder.OrderIdentifier

                                Await Me.ParentStrategy.SignalManager.ActivateEntryActivity(activityTag, Me, parentBOrder.ParentOrderIdentifier, Now).ConfigureAwait(False)
                                logger.Debug("Order Placed {0}, Time:{1}", Me.TradableInstrument.TradingSymbol, Now.ToString)
                                Await ProcessOrderAsync(parentBOrder).ConfigureAwait(False)
                                'Me.OrderDetails.AddOrUpdate(parentBOrder.ParentOrderIdentifier, parentBOrder, Function(key, value) parentBOrder)
                                If ret Is Nothing Then ret = New List(Of IBusinessOrder)
                                ret.Add(OrderDetails(parentBOrder.ParentOrderIdentifier))
                                OnHeartbeat(String.Format("Place Order Successful. Order ID:{0}", parentBOrder.ParentOrderIdentifier))
                            End If
                        Next
                    End If
                Finally
                    'logger.Debug("Releasing lock")
                    Interlocked.Exchange(_placeOrderLock, 0)
                End Try
            End If
            Return ret
        End Function

        Private _cancelOrderLock As Integer = 0
        Public PairStrategyCancellationRequest As Boolean = False
        Protected Async Function CancelPaperTradeAsync(ByVal data As Object) As Task(Of List(Of IBusinessOrder))
            Dim ret As List(Of IBusinessOrder) = Nothing
            If 0 = Interlocked.Exchange(_cancelOrderLock, 1) Then
                Try
                    PairStrategyCancellationRequest = True
                    Dim exitOrdersTriggers As List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)) = data
                    If exitOrdersTriggers IsNot Nothing AndAlso exitOrdersTriggers.Count > 0 Then
                        For Each runningExitOrdersTrigger In exitOrdersTriggers
                            If runningExitOrdersTrigger.Item1 = ExecuteCommandAction.Take Then
                                Dim potentialExitOrders As List(Of IOrder) = Nothing
                                If runningExitOrdersTrigger.Item1 = ExecuteCommandAction.Take Then

                                    Dim lastTradeTime As Date = Me.TradableInstrument.LastTick.LastTradeTime.Value
                                    While Utilities.Time.IsTimeEqualTillSeconds(Me.TradableInstrument.LastTick.LastTradeTime.Value, lastTradeTime)
                                        Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                                    End While

                                    Dim parentBusinessOrder As IBusinessOrder = GetParentFromChildOrder(runningExitOrdersTrigger.Item3)
                                    Await Me.ParentStrategy.SignalManager.HandleCancelActivity(runningExitOrdersTrigger.Item3.Tag, Me, Nothing, Now, runningExitOrdersTrigger.Item4).ConfigureAwait(False)
                                    CType(runningExitOrdersTrigger.Item3, PaperOrder).Status = IOrder.TypeOfStatus.Cancelled
                                    CType(runningExitOrdersTrigger.Item3, PaperOrder).TimeStamp = Now
                                    CType(runningExitOrdersTrigger.Item3, PaperOrder).AveragePrice = Me.TradableInstrument.LastTick.LastPrice
                                    Await Me.ParentStrategy.SignalManager.ActivateCancelActivity(runningExitOrdersTrigger.Item3.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                    Await ProcessOrderAsync(parentBusinessOrder).ConfigureAwait(False)
                                    logger.Debug("Order Exited {0}", Me.TradableInstrument.TradingSymbol)

                                    If parentBusinessOrder.SLOrder.Count = 1 Then
                                        parentBusinessOrder.SLOrder = Nothing
                                    Else
                                        Throw New ApplicationException("Check why there is more than one sl order")
                                    End If

                                    If potentialExitOrders Is Nothing Then potentialExitOrders = New List(Of IOrder)
                                    potentialExitOrders.Add(runningExitOrdersTrigger.Item3)
                                    parentBusinessOrder.AllOrder = potentialExitOrders

                                    If ret Is Nothing Then ret = New List(Of IBusinessOrder)
                                    ret.Add(parentBusinessOrder)
                                    OnHeartbeat(String.Format("Cancel Order Successful. Order ID:{0}", parentBusinessOrder.ParentOrderIdentifier))
                                End If
                            End If
                        Next
                    End If
                Finally
                    Interlocked.Exchange(_cancelOrderLock, 0)
                    PairStrategyCancellationRequest = False
                End Try
            End If
            Return ret
        End Function

        Private _forceCancelOrderLock As Integer = 0
        Protected Async Function ForceCancelPaperTradeAsync(ByVal data As Object, Optional ByVal exitImmediately As Boolean = False, Optional ByVal currentTick As ITick = Nothing) As Task(Of List(Of IBusinessOrder))
            Dim ret As List(Of IBusinessOrder) = Nothing
            If 0 = Interlocked.Exchange(_forceCancelOrderLock, 1) Then
                Try
                    Dim exitOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = data
                    If exitOrdersTrigger IsNot Nothing AndAlso exitOrdersTrigger.Count > 0 Then
                        Dim potentialExitOrders As List(Of IOrder) = Nothing
                        For Each runningExitOrder In exitOrdersTrigger
                            If runningExitOrder.Item1 = ExecuteCommandAction.Take Then
                                Dim parentBusinessOrder As IBusinessOrder = GetParentFromChildOrder(runningExitOrder.Item2)

                                logger.Debug("Cancel Order Details-> Parent Order ID:{0}, Direction:{1}, Reason:{2}",
                                             parentBusinessOrder.ParentOrderIdentifier, parentBusinessOrder.ParentOrder.TransactionType.ToString, runningExitOrder.Item3)

                                If Not exitImmediately Then
                                    Dim lastTradeTime As Date = Me.TradableInstrument.LastTick.LastTradeTime.Value
                                    While Utilities.Time.IsTimeEqualTillSeconds(Me.TradableInstrument.LastTick.LastTradeTime.Value, lastTradeTime)
                                        Await Task.Delay(10, _cts.Token).ConfigureAwait(False)
                                    End While
                                End If

                                Await Me.ParentStrategy.SignalManager.HandleCancelActivity(runningExitOrder.Item2.Tag, Me, Nothing, Now, runningExitOrder.Item3).ConfigureAwait(False)

                                Dim slOrder As IOrder = runningExitOrder.Item2
                                Dim targetOrder As IOrder = Nothing
                                If parentBusinessOrder.TargetOrder IsNot Nothing AndAlso parentBusinessOrder.TargetOrder.Count = 1 Then
                                    targetOrder = parentBusinessOrder.TargetOrder.FirstOrDefault
                                End If

                                Dim exitPrice As Decimal = Decimal.MinValue
                                Dim exitTime As Date = Date.MinValue
                                If currentTick IsNot Nothing Then
                                    exitPrice = currentTick.LastPrice
                                    exitTime = currentTick.Timestamp.Value
                                Else
                                    exitPrice = Me.TradableInstrument.LastTick.LastPrice
                                    exitTime = Me.TradableInstrument.LastTick.Timestamp.Value
                                End If

                                If runningExitOrder.Item3.ToUpper = "STOPLOSS REACHED" Then
                                    CType(slOrder, PaperOrder).Status = IOrder.TypeOfStatus.Complete
                                    CType(slOrder, PaperOrder).TimeStamp = exitTime
                                    CType(slOrder, PaperOrder).AveragePrice = exitPrice
                                    If targetOrder IsNot Nothing Then
                                        CType(targetOrder, PaperOrder).Status = IOrder.TypeOfStatus.Cancelled
                                        CType(targetOrder, PaperOrder).TimeStamp = exitTime
                                        CType(targetOrder, PaperOrder).Quantity = 0
                                    End If
                                ElseIf runningExitOrder.Item3.ToUpper = "TARGET REACHED" Then
                                    If targetOrder IsNot Nothing Then
                                        CType(slOrder, PaperOrder).Status = IOrder.TypeOfStatus.Cancelled
                                        CType(slOrder, PaperOrder).TimeStamp = exitTime
                                        CType(slOrder, PaperOrder).Quantity = 0
                                        CType(targetOrder, PaperOrder).Status = IOrder.TypeOfStatus.Complete
                                        CType(targetOrder, PaperOrder).TimeStamp = exitTime
                                        CType(targetOrder, PaperOrder).AveragePrice = exitPrice
                                    Else
                                        CType(slOrder, PaperOrder).Status = IOrder.TypeOfStatus.Cancelled
                                        CType(slOrder, PaperOrder).TimeStamp = exitTime
                                        CType(slOrder, PaperOrder).AveragePrice = exitPrice
                                    End If
                                Else
                                    CType(slOrder, PaperOrder).Status = IOrder.TypeOfStatus.Complete
                                    CType(slOrder, PaperOrder).TimeStamp = exitTime
                                    CType(slOrder, PaperOrder).AveragePrice = exitPrice
                                    If targetOrder IsNot Nothing Then
                                        CType(targetOrder, PaperOrder).Status = IOrder.TypeOfStatus.Cancelled
                                        CType(targetOrder, PaperOrder).TimeStamp = exitTime
                                        CType(targetOrder, PaperOrder).Quantity = 0
                                    End If
                                End If

                                Await Me.ParentStrategy.SignalManager.ActivateCancelActivity(runningExitOrder.Item2.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                Await ProcessOrderAsync(parentBusinessOrder).ConfigureAwait(False)
                                logger.Debug("Order Exited {0}", Me.TradableInstrument.TradingSymbol)

                                If parentBusinessOrder.SLOrder.Count = 1 Then
                                    parentBusinessOrder.SLOrder = Nothing
                                Else
                                    Throw New ApplicationException("Check why there is more than one sl order")
                                End If
                                If parentBusinessOrder.TargetOrder.Count = 1 Then
                                    parentBusinessOrder.TargetOrder = Nothing
                                Else
                                    Throw New ApplicationException("Check why there is more than one target order")
                                End If

                                If potentialExitOrders Is Nothing Then potentialExitOrders = New List(Of IOrder)
                                potentialExitOrders.Add(runningExitOrder.Item2)
                                If targetOrder IsNot Nothing Then potentialExitOrders.Add(targetOrder)
                                parentBusinessOrder.AllOrder = potentialExitOrders
                                If ret Is Nothing Then ret = New List(Of IBusinessOrder)
                                ret.Add(parentBusinessOrder)
                                'OnHeartbeat(String.Format("Force Cancel Order Successful. Order ID:{0}", parentBusinessOrder.ParentOrderIdentifier))
                            End If
                        Next
                    End If
                Finally
                    Interlocked.Exchange(_forceCancelOrderLock, 0)
                End Try
            End If
            Return ret
        End Function

        Private _modifySLOrderLock As Integer = 0
        Protected Async Function ModifySLPaperTradeAsync(ByVal data As Object) As Task(Of List(Of IBusinessOrder))
            Dim ret As List(Of IBusinessOrder) = Nothing
            If 0 = Interlocked.Exchange(_modifySLOrderLock, 1) Then
                Try
                    Dim modifyOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = data
                    If modifyOrdersTrigger IsNot Nothing AndAlso modifyOrdersTrigger.Count > 0 Then
                        For Each runningOrder In modifyOrdersTrigger
                            If runningOrder.Item1 = ExecuteCommandAction.Take Then
                                Dim parentBusinessOrder As IBusinessOrder = GetParentFromChildOrder(runningOrder.Item2)

                                logger.Debug("Modify Order Details-> Parent Order ID:{0}, Direction:{1}, Trigger Price:{2}, Reason:{3}",
                                             parentBusinessOrder.ParentOrderIdentifier, parentBusinessOrder.ParentOrder.TransactionType.ToString, runningOrder.Item3, runningOrder.Item4)


                                Await Me.ParentStrategy.SignalManager.HandleStoplossModifyActivity(runningOrder.Item2.Tag, Me, Nothing, Now, runningOrder.Item3, runningOrder.Item4).ConfigureAwait(False)

                                Dim slOrder As IOrder = runningOrder.Item2
                                CType(slOrder, PaperOrder).TriggerPrice = runningOrder.Item3

                                Await Me.ParentStrategy.SignalManager.ActivateStoplossModifyActivity(runningOrder.Item2.Tag, Me, Nothing, Now).ConfigureAwait(False)
                                Await ProcessOrderAsync(parentBusinessOrder).ConfigureAwait(False)
                                logger.Debug("Order Modified {0}", Me.TradableInstrument.TradingSymbol)
                                If ret Is Nothing Then ret = New List(Of IBusinessOrder)
                                ret.Add(parentBusinessOrder)
                                OnHeartbeat(String.Format("Modify Order Successful. Order ID:{0}", parentBusinessOrder.ParentOrderIdentifier))
                            End If
                        Next
                    End If
                Finally
                    Interlocked.Exchange(_modifySLOrderLock, 0)
                End Try
            End If
            Return ret
        End Function
#End Region

    End Class
End Namespace