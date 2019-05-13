Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog

Public Class AmiSignalStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public EntrySignalFileName As String = Path.Combine(My.Application.Info.DirectoryPath, String.Format("{0}_{1}.EntrySignal.a2t", Me.ToString, Now.ToString("yy_MM_dd")))
    Public TargetSignalFileName As String = Path.Combine(My.Application.Info.DirectoryPath, String.Format("{0}_{1}.TargetSignal.a2t", Me.ToString, Now.ToString("yy_MM_dd")))
    Public StoplossSignalFileName As String = Path.Combine(My.Application.Info.DirectoryPath, String.Format("{0}_{1}.StoplossSignal.a2t", Me.ToString, Now.ToString("yy_MM_dd")))

    Public EntrySignals As Concurrent.ConcurrentDictionary(Of String, AmiSignal)
    Public TargetSignals As Concurrent.ConcurrentDictionary(Of String, AmiSignal)
    Public StoplossSignals As Concurrent.ConcurrentDictionary(Of String, AmiSignal)

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
    End Sub
    Public Overrides Async Function ProcessOrderAsync(ByVal orderData As IBusinessOrder) As Task
        _cts.Token.ThrowIfCancellationRequested()
        Await MyBase.ProcessOrderAsync(orderData).ConfigureAwait(False)
        _cts.Token.ThrowIfCancellationRequested()
        DeleteProcessedOrderAsync(orderData)
    End Function
    Public Overrides Async Function MonitorAsync() As Task
        Try
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If Not Me.ParentStrategy.IsFirstTimeInformationCollected Then
                    logger.Debug("Information collector is not completed till now")
                    Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                    Continue While
                End If

                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    Dim placeOrderResponse As Object = Nothing
                    If EntrySignals IsNot Nothing AndAlso EntrySignals.Count > 0 AndAlso placeOrderTrigger.Item2.Supporting.Count > 0 AndAlso
                       EntrySignals.ContainsKey(placeOrderTrigger.Item2.Supporting.FirstOrDefault) AndAlso
                       placeOrderTrigger.Item2.OrderType = IOrder.TypeOfOrder.Market Then
                        placeOrderResponse = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularMarketMISOrder, Nothing).ConfigureAwait(False)
                        If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                            placeOrderResponse("data").ContainsKey("order_id") Then
                            EntrySignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderTimestamp = Now()
                            EntrySignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderID = placeOrderResponse("data")("order_id")
                        End If
                    End If
                    If TargetSignals IsNot Nothing AndAlso TargetSignals.Count > 0 AndAlso placeOrderTrigger.Item2.Supporting.Count > 0 AndAlso
                        TargetSignals.ContainsKey(placeOrderTrigger.Item2.Supporting.FirstOrDefault) AndAlso
                        placeOrderTrigger.Item2.OrderType = IOrder.TypeOfOrder.Limit Then
                        placeOrderResponse = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularLimitMISOrder, Nothing).ConfigureAwait(False)
                        If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                            placeOrderResponse("data").ContainsKey("order_id") Then
                            TargetSignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderTimestamp = Now()
                            TargetSignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderID = placeOrderResponse("data")("order_id")
                        End If
                    End If
                    If StoplossSignals IsNot Nothing AndAlso StoplossSignals.Count > 0 AndAlso placeOrderTrigger.Item2.Supporting.Count > 0 AndAlso
                       StoplossSignals.ContainsKey(placeOrderTrigger.Item2.Supporting.FirstOrDefault) AndAlso
                       placeOrderTrigger.Item2.OrderType = IOrder.TypeOfOrder.SL_M Then
                        placeOrderResponse = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularSLMMISOrder, Nothing).ConfigureAwait(False)
                        If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                            placeOrderResponse("data").ContainsKey("order_id") Then
                            StoplossSignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderTimestamp = Now()
                            StoplossSignals(placeOrderTrigger.Item2.Supporting.FirstOrDefault).OrderID = placeOrderResponse("data")("order_id")
                        End If
                    End If
                    SerializeSignalCollections()
                End If

                _cts.Token.ThrowIfCancellationRequested()
                Dim modifyTargetOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Await IsTriggerReceivedForModifyTargetOrderAsync(False).ConfigureAwait(False)
                If modifyTargetOrdersTrigger IsNot Nothing AndAlso modifyTargetOrdersTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.ModifyTargetOrder, Nothing).ConfigureAwait(False)
                End If

                _cts.Token.ThrowIfCancellationRequested()
                Dim exitOrdersTrigger As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Await IsTriggerReceivedForExitOrderAsync(False).ConfigureAwait(False)
                If exitOrdersTrigger IsNot Nothing AndAlso exitOrdersTrigger.Count > 0 Then
                    Await ExecuteCommandAsync(ExecuteCommands.CancelRegularOrder, Nothing).ConfigureAwait(False)
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
            End While
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        Finally
            SerializeSignalCollections()
        End Try
    End Function
    Public Overrides Function MonitorAsync(ByVal command As ExecuteCommands, ByVal data As Object) As Task
        Throw New NotImplementedException()
    End Function
    Protected Overrides Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, PlaceOrderParameters, String)))
        Throw New NotImplementedException()
    End Function
    Protected Overrides Function IsTriggerReceivedForExitOrderAsync(forcePrint As Boolean, data As Object) As Task(Of List(Of Tuple(Of ExecuteCommandAction, StrategyInstrument, IOrder, String)))
        Throw New NotImplementedException()
    End Function
    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(ByVal forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim amiUserSettings As AmiSignalUserInputs = Me.ParentStrategy.UserSettings

        Dim unusedEntrySignals As IEnumerable(Of KeyValuePair(Of String, AmiSignal)) = GetUnUsedSignals(EntrySignals)
        If unusedEntrySignals IsNot Nothing AndAlso unusedEntrySignals.Count > 0 Then
            logger.Debug(Utilities.Strings.JsonSerialize(unusedEntrySignals))
        End If
        Dim unusedTargetSignals As IEnumerable(Of KeyValuePair(Of String, AmiSignal)) = GetUnUsedSignals(TargetSignals)
        Dim unusedStoplossSignals As IEnumerable(Of KeyValuePair(Of String, AmiSignal)) = GetUnUsedSignals(StoplossSignals)


        Dim parameters As PlaceOrderParameters = Nothing
        If Now >= amiUserSettings.TradeStartTime AndAlso Now <= amiUserSettings.LastTradeEntryTime AndAlso Not IsLogicalActiveInstrument() Then
            Dim dummyPayload As OHLCPayload = Nothing
            If unusedEntrySignals IsNot Nothing AndAlso unusedEntrySignals.Count > 0 Then
                Dim currentEntrySignal As AmiSignal = unusedEntrySignals.FirstOrDefault.Value
                If CType(Me.ParentStrategy, AmiSignalStrategy).GetNumberOfLogicalActiveInstruments >= amiUserSettings.MaxNumberOfOpenPositions Then
                    logger.Error(String.Format("{0} - {1} Number of trade is running. So this signal cannot execute. {2}", Me.TradableInstrument.TradingSymbol, Me.ParentStrategy.GetNumberOfActiveInstruments, If(currentEntrySignal.Direction = IOrder.TypeOfTransaction.Buy, "BUY", "SHORT")))
                    EntrySignals(currentEntrySignal.UniqueIdentifier).Used = True
                    SerializeSignalCollections()
                Else
                    If currentEntrySignal.OrderType = IOrder.TypeOfOrder.Market AndAlso currentEntrySignal.OrderTimestamp = Date.MinValue Then
                        dummyPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {.SnapshotDateTime = Now}
                        parameters = New PlaceOrderParameters(dummyPayload) With
                                 {
                                    .EntryDirection = currentEntrySignal.Direction,
                                    .Quantity = currentEntrySignal.Quantity,
                                    .OrderType = IOrder.TypeOfOrder.Market
                                 }
                        parameters.Supporting.Add(currentEntrySignal.UniqueIdentifier)
                    End If
                End If
            ElseIf unusedTargetSignals IsNot Nothing AndAlso unusedTargetSignals.Count > 0 Then
                Dim currentTargetSignal As AmiSignal = unusedTargetSignals.FirstOrDefault.Value
                If CType(Me.ParentStrategy, AmiSignalStrategy).GetNumberOfLogicalActiveInstruments >= amiUserSettings.MaxNumberOfOpenPositions Then
                    logger.Error(String.Format("{0} - {1} Number of trade is running. So this signal cannot execute. {2}", Me.TradableInstrument.TradingSymbol, Me.ParentStrategy.GetNumberOfActiveInstruments, If(currentTargetSignal.Direction = IOrder.TypeOfTransaction.Buy, "BUY", "SHORT")))
                    TargetSignals(currentTargetSignal.UniqueIdentifier).Used = True
                    SerializeSignalCollections()
                Else
                    If currentTargetSignal.OrderType = IOrder.TypeOfOrder.Limit AndAlso currentTargetSignal.OrderTimestamp = Date.MinValue Then
                        dummyPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {.SnapshotDateTime = Now.AddSeconds(3)}
                        parameters = New PlaceOrderParameters(dummyPayload) With
                                 {
                                    .EntryDirection = currentTargetSignal.Direction,
                                    .Quantity = currentTargetSignal.Quantity,
                                    .Price = currentTargetSignal.Price,
                                    .OrderType = IOrder.TypeOfOrder.Limit
                                 }
                        parameters.Supporting.Add(currentTargetSignal.UniqueIdentifier)
                    End If
                End If
            ElseIf unusedStoplossSignals IsNot Nothing AndAlso unusedStoplossSignals.Count = 1 Then
                Dim currentStoplossSignal As AmiSignal = unusedStoplossSignals.FirstOrDefault.Value
                If CType(Me.ParentStrategy, AmiSignalStrategy).GetNumberOfLogicalActiveInstruments >= amiUserSettings.MaxNumberOfOpenPositions Then
                    logger.Error(String.Format("{0} - {1} Number of trade is running. So this signal cannot execute. {2}", Me.TradableInstrument.TradingSymbol, Me.ParentStrategy.GetNumberOfActiveInstruments, If(currentStoplossSignal.Direction = IOrder.TypeOfTransaction.Buy, "BUY", "SHORT")))
                    StoplossSignals(currentStoplossSignal.UniqueIdentifier).Used = True
                    SerializeSignalCollections()
                Else
                    If currentStoplossSignal.OrderType = IOrder.TypeOfOrder.SL_M AndAlso currentStoplossSignal.OrderTimestamp = Date.MinValue Then
                        dummyPayload = New OHLCPayload(OHLCPayload.PayloadSource.None) With {.SnapshotDateTime = Now.AddSeconds(6)}
                        parameters = New PlaceOrderParameters(dummyPayload) With
                                     {
                                        .EntryDirection = currentStoplossSignal.Direction,
                                        .Quantity = currentStoplossSignal.Quantity,
                                        .TriggerPrice = currentStoplossSignal.Price,
                                        .OrderType = IOrder.TypeOfOrder.SL_M
                                     }
                        parameters.Supporting.Add(currentStoplossSignal.UniqueIdentifier)
                    End If
                End If
            End If
        ElseIf IsLogicalActiveInstrument() Then
            Dim currentSignal As AmiSignal = Nothing
            If unusedEntrySignals IsNot Nothing AndAlso unusedEntrySignals.Count > 0 Then
                logger.Error(String.Format("{0} - Trade is running. So another entry signal cannot execute.", Me.TradableInstrument.TradingSymbol))
                EntrySignals(unusedEntrySignals.FirstOrDefault.Key).Used = True
            End If
            If unusedTargetSignals IsNot Nothing AndAlso unusedTargetSignals.Count > 0 Then
                logger.Error(String.Format("{0} - Trade is running. So another target signal cannot execute.", Me.TradableInstrument.TradingSymbol))
                TargetSignals(unusedTargetSignals.FirstOrDefault.Key).Used = True
            End If
            If unusedStoplossSignals IsNot Nothing AndAlso unusedStoplossSignals.Count > 0 Then
                logger.Error(String.Format("{0} - Trade is running. So another stoploss signal cannot execute.", Me.TradableInstrument.TradingSymbol))
                StoplossSignals(unusedStoplossSignals.FirstOrDefault.Key).Used = True
            End If
            SerializeSignalCollections()
        Else
            Dim currentSignal As AmiSignal = Nothing
            If unusedEntrySignals IsNot Nothing AndAlso unusedEntrySignals.Count > 0 Then
                logger.Error(String.Format("{0} - Outside market hours. Trade Start Time:{1}, Last Trade Entry Time:{2}", Me.TradableInstrument.TradingSymbol, amiUserSettings.TradeStartTime.ToString, amiUserSettings.LastTradeEntryTime.ToString))
                EntrySignals(unusedEntrySignals.FirstOrDefault.Key).Used = True
            End If
            If unusedTargetSignals IsNot Nothing AndAlso unusedTargetSignals.Count > 0 Then
                logger.Error(String.Format("{0} - Outside market hours. Trade Start Time:{1}, Last Trade Entry Time:{2}", Me.TradableInstrument.TradingSymbol, amiUserSettings.TradeStartTime.ToString, amiUserSettings.LastTradeEntryTime.ToString))
                TargetSignals(unusedTargetSignals.FirstOrDefault.Key).Used = True
            End If
            If unusedStoplossSignals IsNot Nothing AndAlso unusedStoplossSignals.Count > 0 Then
                logger.Error(String.Format("{0} - Outside market hours. Trade Start Time:{1}, Last Trade Entry Time:{2}", Me.TradableInstrument.TradingSymbol, amiUserSettings.TradeStartTime.ToString, amiUserSettings.LastTradeEntryTime.ToString))
                StoplossSignals(unusedStoplossSignals.FirstOrDefault.Key).Used = True
            End If
            SerializeSignalCollections()
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Dim currentSignalActivities As IEnumerable(Of ActivityDashboard) = Me.ParentStrategy.SignalManager.GetSignalActivities(parameters.SignalCandle.SnapshotDateTime, Me.TradableInstrument.InstrumentIdentifier)
            If currentSignalActivities IsNot Nothing AndAlso currentSignalActivities.Count > 0 Then
                If currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded AndAlso
                    currentSignalActivities.FirstOrDefault.EntryActivity.LastException IsNot Nothing AndAlso
                    currentSignalActivities.FirstOrDefault.EntryActivity.LastException.Message.ToUpper.Contains("TIME") Then
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.WaitAndTake, parameters, "Condition Satisfied")
                ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Discarded Then
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
                    'ElseIf currentSignalActivities.FirstOrDefault.EntryActivity.RequestStatus = ActivityDashboard.SignalStatusType.Rejected Then
                    '    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters)(ExecuteCommandAction.Take, parameters)
                Else
                    ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "Condition Satisfied")
                End If
            Else
                ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.Take, parameters, "Condition Satisfied")
            End If
        Else
            ret = New Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String)(ExecuteCommandAction.DonotTake, Nothing, "")
        End If
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyStoplossOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Throw New NotImplementedException
    End Function
    Protected Overrides Async Function IsTriggerReceivedForModifyTargetOrderAsync(forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim amiUserSettings As AmiSignalUserInputs = Me.ParentStrategy.UserSettings

        If Now >= amiUserSettings.EODExitTime Then
            Dim allCancelableOrders As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = GetAllCancelableOrders(IOrder.TypeOfTransaction.None)
            If allCancelableOrders IsNot Nothing AndAlso allCancelableOrders.Count > 0 Then
                For Each cancelableOrder In allCancelableOrders
                    If cancelableOrder.Item2.OrderType = IOrder.TypeOfOrder.Limit AndAlso Not cancelableOrder.Item2.Status = IOrder.TypeOfStatus.Complete Then
                        Dim price As Decimal = Decimal.MinValue
                        If cancelableOrder.Item2.TransactionType = IOrder.TypeOfTransaction.Buy Then
                            price = Me.TradableInstrument.LastTick.LastPrice + Utilities.Numbers.ConvertFloorCeling((Me.TradableInstrument.LastTick.LastPrice * 0.3 / 100), Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                        ElseIf cancelableOrder.Item2.TransactionType = IOrder.TypeOfTransaction.Sell Then
                            price = Me.TradableInstrument.LastTick.LastPrice - Utilities.Numbers.ConvertFloorCeling((Me.TradableInstrument.LastTick.LastPrice * 0.3 / 100), Me.TradableInstrument.TickSize, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                        End If
                        'Below portion have to be done in every modify stoploss order trigger
                        Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(cancelableOrder.Item2.Tag)
                        If currentSignalActivities IsNot Nothing Then
                            If currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                            currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                            currentSignalActivities.TargetModifyActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                Continue For
                            End If
                        End If

                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, Decimal, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, Decimal, String)(ExecuteCommandAction.Take, cancelableOrder.Item2, price, "EOD Square Off"))
                    End If
                Next
            End If
        End If
        Return ret
    End Function
    Protected Overrides Async Function IsTriggerReceivedForExitOrderAsync(ByVal forcePrint As Boolean) As Task(Of List(Of Tuple(Of ExecuteCommandAction, IOrder, String)))
        Dim ret As List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim amiUserSettings As AmiSignalUserInputs = Me.ParentStrategy.UserSettings

        If Now < amiUserSettings.EODExitTime AndAlso OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
            If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
                For Each parentOrder In allActiveOrders
                    Dim isSignalShouldCancel As Boolean = False
                    Dim uniqueID As String = Nothing
                    If parentOrder.OrderType = IOrder.TypeOfOrder.Limit Then
                        uniqueID = GetUniqueIDFromOrderID(TargetSignals, parentOrder.OrderIdentifier)
                    ElseIf parentOrder.OrderType = IOrder.TypeOfOrder.SL_M Then
                        uniqueID = GetUniqueIDFromOrderID(StoplossSignals, parentOrder.OrderIdentifier)
                    End If

                    Dim associatedOrders As List(Of IOrder) = GetOrdersFromUniqueID(uniqueID)
                    If associatedOrders IsNot Nothing AndAlso associatedOrders.Count > 0 Then
                        associatedOrders = associatedOrders.FindAll(Function(x)
                                                                        Return x.OrderType = IOrder.TypeOfOrder.Market OrElse
                                                                        (x.OrderType <> IOrder.TypeOfOrder.Market AndAlso x.Status <> IOrder.TypeOfStatus.Rejected)
                                                                    End Function)
                        If associatedOrders IsNot Nothing AndAlso associatedOrders.Count = 3 Then
                            Dim openOrderCount As Integer = 0
                            For Each order In associatedOrders
                                If order.Status = IOrder.TypeOfStatus.Open OrElse order.Status = IOrder.TypeOfStatus.TriggerPending Then
                                    openOrderCount += 1
                                End If
                            Next
                            If openOrderCount = 1 Then
                                If parentOrder.OrderType = IOrder.TypeOfOrder.Limit OrElse parentOrder.OrderType = IOrder.TypeOfOrder.SL_M Then
                                    isSignalShouldCancel = True
                                End If
                            End If
                        End If
                    End If
                    If isSignalShouldCancel Then
                        'Below portion have to be done in every cancel order trigger
                        Dim currentSignalActivities As ActivityDashboard = Me.ParentStrategy.SignalManager.GetSignalActivities(parentOrder.Tag)
                        If currentSignalActivities IsNot Nothing Then
                            If currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Handled OrElse
                            currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Activated OrElse
                            currentSignalActivities.CancelActivity.RequestStatus = ActivityDashboard.SignalStatusType.Completed Then
                                Continue For
                            End If
                        End If

                        Dim exitReason As String = "Opposite order executed"
                        If parentOrder.OrderType = IOrder.TypeOfOrder.Limit Then
                            exitReason = "Stoploss Reached"
                        ElseIf parentOrder.OrderType = IOrder.TypeOfOrder.SL_M Then
                            exitReason = "Target Reached"
                        End If

                        If ret Is Nothing Then ret = New List(Of Tuple(Of ExecuteCommandAction, IOrder, String))
                        ret.Add(New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, parentOrder, exitReason))
                    End If
                Next
            End If
        End If
        Return ret
    End Function
    Protected Overrides Async Function ForceExitSpecificTradeAsync(order As IOrder, ByVal reason As String) As Task
        If order IsNot Nothing AndAlso Not order.Status = IOrder.TypeOfStatus.Complete AndAlso Not order.OrderType = IOrder.TypeOfOrder.Limit Then
            Dim cancellableOrder As New List(Of Tuple(Of ExecuteCommandAction, IOrder, String)) From
            {
                New Tuple(Of ExecuteCommandAction, IOrder, String)(ExecuteCommandAction.Take, order, reason)
            }
            Await ExecuteCommandAsync(ExecuteCommands.ForceCancelRegularOrder, cancellableOrder).ConfigureAwait(False)
        End If
    End Function

    Private Async Function DeleteProcessedOrderAsync(ByVal orderData As IBusinessOrder) As Task
        Try
            Await Task.Delay(0).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            If orderData IsNot Nothing Then
                Dim signalIDToBeDeleted As String = Nothing

                signalIDToBeDeleted = GetUniqueIDFromOrderID(EntrySignals, orderData.ParentOrderIdentifier)
                If signalIDToBeDeleted IsNot Nothing Then EntrySignals(signalIDToBeDeleted).Used = True

                signalIDToBeDeleted = GetUniqueIDFromOrderID(TargetSignals, orderData.ParentOrderIdentifier)
                If signalIDToBeDeleted IsNot Nothing Then TargetSignals(signalIDToBeDeleted).Used = True

                signalIDToBeDeleted = GetUniqueIDFromOrderID(StoplossSignals, orderData.ParentOrderIdentifier)
                If signalIDToBeDeleted IsNot Nothing Then StoplossSignals(signalIDToBeDeleted).Used = True
            End If
        Catch ex As Exception
            logger.Error(ex)
        Finally
            SerializeSignalCollections()
        End Try
    End Function

    Public Async Function PopulateExternalSignalAsync(ByVal signal As String) As Task
        logger.Info("PopulateExternalSignalAsync, parameters:{0}", signal)
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Try
            Dim currentSignal As AmiSignal = Nothing
            If EntrySignals Is Nothing Then EntrySignals = New Concurrent.ConcurrentDictionary(Of String, AmiSignal)
            If TargetSignals Is Nothing Then TargetSignals = New Concurrent.ConcurrentDictionary(Of String, AmiSignal)
            If StoplossSignals Is Nothing Then StoplossSignals = New Concurrent.ConcurrentDictionary(Of String, AmiSignal)
            Dim signalarr() As String = signal.Trim.Split(" ")
            Dim returnedSignal As AmiSignal = Nothing

            Select Case signalarr(2).ToUpper()
                Case "BUY"
                    currentSignal = New AmiSignal
                    With currentSignal
                        .UniqueIdentifier = signalarr(0)
                        .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                        .Direction = IOrder.TypeOfTransaction.Buy
                        .OrderType = IOrder.TypeOfOrder.Market
                        .Price = signalarr(4)
                        .Quantity = signalarr(5)
                        .Timestamp = Now
                        .Used = False
                    End With
                    returnedSignal = EntrySignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                    If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                        logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                    End If
                Case "SELL"
                    Select Case signalarr(3).ToUpper()
                        Case "LIMIT"
                            currentSignal = New AmiSignal
                            With currentSignal
                                .UniqueIdentifier = signalarr(0)
                                .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                .Direction = IOrder.TypeOfTransaction.Sell
                                .OrderType = IOrder.TypeOfOrder.Limit
                                .Price = signalarr(4)
                                .Quantity = signalarr(5)
                                .Timestamp = Now
                                .Used = False
                            End With
                            returnedSignal = TargetSignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                            If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                                logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                            End If
                        Case "SL-M", "SLM"
                            currentSignal = New AmiSignal
                            With currentSignal
                                .UniqueIdentifier = signalarr(0)
                                .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                .Direction = IOrder.TypeOfTransaction.Sell
                                .OrderType = IOrder.TypeOfOrder.SL_M
                                .Price = signalarr(4)
                                .Quantity = signalarr(5)
                                .Timestamp = Now
                                .Used = False
                            End With
                            returnedSignal = StoplossSignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                            If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                                logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                            End If
                        Case Else
                            logger.Error(String.Format("{0} Invalid Signal Details. {1}", Me.TradableInstrument.TradingSymbol, signal))
                    End Select
                Case "SHORT"
                    currentSignal = New AmiSignal
                    With currentSignal
                        .UniqueIdentifier = signalarr(0)
                        .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                        .Direction = IOrder.TypeOfTransaction.Sell
                        .OrderType = IOrder.TypeOfOrder.Market
                        .Price = signalarr(4)
                        .Quantity = signalarr(5)
                        .Timestamp = Now
                        .Used = False
                    End With
                    returnedSignal = EntrySignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                    If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                        logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                    End If
                Case "COVER"
                    Select Case signalarr(3).ToUpper()
                        Case "LIMIT"
                            currentSignal = New AmiSignal
                            With currentSignal
                                .UniqueIdentifier = signalarr(0)
                                .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                .Direction = IOrder.TypeOfTransaction.Buy
                                .OrderType = IOrder.TypeOfOrder.Limit
                                .Price = signalarr(4)
                                .Quantity = signalarr(5)
                                .Timestamp = Now
                                .Used = False
                            End With
                            returnedSignal = TargetSignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                            If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                                logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                            End If
                        Case "SL-M", "SLM"
                            currentSignal = New AmiSignal
                            With currentSignal
                                .UniqueIdentifier = signalarr(0)
                                .InstrumentIdentifier = Me.TradableInstrument.InstrumentIdentifier
                                .Direction = IOrder.TypeOfTransaction.Buy
                                .OrderType = IOrder.TypeOfOrder.SL_M
                                .Price = signalarr(4)
                                .Quantity = signalarr(5)
                                .Timestamp = Now
                                .Used = False
                            End With
                            returnedSignal = StoplossSignals.GetOrAdd(currentSignal.UniqueIdentifier, currentSignal)
                            If Not returnedSignal.Timestamp = currentSignal.Timestamp Then
                                logger.Error(String.Format("{0} - Previous signal still exists", Me.TradableInstrument.TradingSymbol))
                            End If
                        Case Else
                            logger.Error(String.Format("{0} Invalid Signal Details. {1}", Me.TradableInstrument.TradingSymbol, signal))
                    End Select
                Case Else
                    logger.Error(String.Format("{0} Invalid Signal Details. {1}", Me.TradableInstrument.TradingSymbol, signal))
            End Select
        Catch ex As Exception
            logger.Error(ex)
        Finally
            SerializeSignalCollections()
        End Try
    End Function

    Public Function GetUnUsedSignals(ByVal signalCollection As Concurrent.ConcurrentDictionary(Of String, AmiSignal)) As IEnumerable(Of KeyValuePair(Of String, AmiSignal))
        Dim ret As IEnumerable(Of KeyValuePair(Of String, AmiSignal)) = Nothing
        If signalCollection IsNot Nothing AndAlso signalCollection.Count > 0 Then
            ret = signalCollection.Where(Function(x)
                                             Return x.Value.Used = False AndAlso x.Value.OrderID Is Nothing
                                         End Function)
        End If
        Return ret
    End Function

    Public Function GetUniqueIDFromOrderID(ByVal signalCollection As Concurrent.ConcurrentDictionary(Of String, AmiSignal), ByVal orderID As String) As String
        Dim ret As String = Nothing
        If signalCollection IsNot Nothing AndAlso signalCollection.Count > 0 Then
            Dim enterdSignals As IEnumerable(Of KeyValuePair(Of String, AmiSignal)) =
                signalCollection.Where(Function(x)
                                           Return x.Value.OrderID IsNot Nothing AndAlso
                                           x.Value.OrderID.ToUpper = orderID.ToUpper
                                       End Function)
            If enterdSignals IsNot Nothing AndAlso enterdSignals.Count > 0 Then
                If enterdSignals.Count = 1 Then
                    ret = enterdSignals.FirstOrDefault.Value.UniqueIdentifier
                Else
                    Throw New ApplicationException("Check why one order id has multiple signals")
                End If
            End If
        End If
        Return ret
    End Function

    Public Function GetOrdersFromUniqueID(ByVal uniqueID) As List(Of IOrder)
        Dim ret As List(Of IOrder) = Nothing
        If uniqueID IsNot Nothing AndAlso OrderDetails IsNot Nothing AndAlso OrderDetails.Count > 0 Then
            If EntrySignals IsNot Nothing AndAlso EntrySignals.Count > 0 AndAlso EntrySignals.ContainsKey(uniqueID) Then
                If EntrySignals(uniqueID).OrderID IsNot Nothing AndAlso OrderDetails.ContainsKey(EntrySignals(uniqueID).OrderID) Then
                    Dim marketOrder As IOrder = OrderDetails(EntrySignals(uniqueID).OrderID).ParentOrder
                    If ret Is Nothing Then ret = New List(Of IOrder)
                    ret.Add(marketOrder)
                End If
            End If
            If TargetSignals IsNot Nothing AndAlso TargetSignals.Count > 0 AndAlso TargetSignals.ContainsKey(uniqueID) Then
                If TargetSignals(uniqueID).OrderID IsNot Nothing AndAlso OrderDetails.ContainsKey(TargetSignals(uniqueID).OrderID) Then
                    Dim targetOrder As IOrder = OrderDetails(TargetSignals(uniqueID).OrderID).ParentOrder
                    If ret Is Nothing Then ret = New List(Of IOrder)
                    ret.Add(targetOrder)
                End If
            End If
            If StoplossSignals IsNot Nothing AndAlso StoplossSignals.Count > 0 AndAlso StoplossSignals.ContainsKey(uniqueID) Then
                If StoplossSignals(uniqueID).OrderID IsNot Nothing AndAlso OrderDetails.ContainsKey(StoplossSignals(uniqueID).OrderID) Then
                    Dim stoplossOrder As IOrder = OrderDetails(StoplossSignals(uniqueID).OrderID).ParentOrder
                    If ret Is Nothing Then ret = New List(Of IOrder)
                    ret.Add(stoplossOrder)
                End If
            End If
        End If
        Return ret
    End Function

    Public Function IsLogicalActiveInstrument() As Boolean
        Dim ret As Boolean = False
        Dim allActiveOrders As List(Of IOrder) = GetAllActiveOrders(IOrder.TypeOfTransaction.None)
        If allActiveOrders IsNot Nothing AndAlso allActiveOrders.Count > 0 Then
            For Each order In allActiveOrders
                Dim uniqueId As String = Nothing
                If order.OrderType = IOrder.TypeOfOrder.Limit Then
                    uniqueId = GetUniqueIDFromOrderID(TargetSignals, order.OrderIdentifier)
                ElseIf order.OrderType = IOrder.TypeOfOrder.SL_M Then
                    uniqueId = GetUniqueIDFromOrderID(StoplossSignals, order.OrderIdentifier)
                End If
                If uniqueId IsNot Nothing Then
                    Dim associatedOrders As List(Of IOrder) = GetOrdersFromUniqueID(uniqueId)
                    If associatedOrders IsNot Nothing AndAlso associatedOrders.Count = 3 Then
                        For Each signalOrder In associatedOrders
                            If signalOrder.Status = IOrder.TypeOfStatus.Open OrElse signalOrder.Status = IOrder.TypeOfStatus.TriggerPending Then
                                ret = True
                                Exit For
                            End If
                        Next
                    End If
                End If
            Next
        End If
        Return ret
    End Function

    Public Sub SerializeSignalCollections()
        Try
            If EntrySignals IsNot Nothing Then Utilities.Strings.SerializeFromCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignal))(EntrySignalFileName, EntrySignals)
            If TargetSignals IsNot Nothing Then Utilities.Strings.SerializeFromCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignal))(TargetSignalFileName, TargetSignals)
            If StoplossSignals IsNot Nothing Then Utilities.Strings.SerializeFromCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignal))(StoplossSignalFileName, StoplossSignals)
        Catch ex As Exception
            logger.Error(ex)
        End Try
    End Sub

#Region "AmiSignal"
    <Serializable>
    Public Class AmiSignal
        Public UniqueIdentifier As String
        Public InstrumentIdentifier As String
        Public Direction As IOrder.TypeOfTransaction
        Public OrderType As IOrder.TypeOfOrder
        Public Price As Decimal
        Public Quantity As Integer
        Public Timestamp As Date
        Public OrderTimestamp As Date = Date.MinValue
        Public OrderID As String
        Public Used As Boolean
    End Class
#End Region

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