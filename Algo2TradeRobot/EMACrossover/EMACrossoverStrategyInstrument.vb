Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class EMACrossoverStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public Property ForceExitByUser As Boolean
    Public Property ForceExitForContractRollover As Boolean
    Public Property ForceEntryForContractRollover As Boolean

    Private lastPrevPayloadPlaceOrder As String = ""
    Private ReadOnly _dummyFastEMAConsumer As EMAConsumer
    Private ReadOnly _dummySlowEMAConsumer As EMAConsumer
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
                {New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMACrossoverUserInputs).FastEMAPeriod, TypeOfField.Close),
                 New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMACrossoverUserInputs).SlowEMAPeriod, TypeOfField.Close)}
                RawPayloadDependentConsumers.Add(chartConsumer)
                _dummyFastEMAConsumer = New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMACrossoverUserInputs).FastEMAPeriod, TypeOfField.Close)
                _dummySlowEMAConsumer = New EMAConsumer(chartConsumer, CType(Me.ParentStrategy.UserSettings, EMACrossoverUserInputs).SlowEMAPeriod, TypeOfField.Close)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
        Me.ForceExitByUser = False
        Me.ForceExitForContractRollover = False
    End Sub

    'Public Overrides Function ProcessHoldingAsync(holdingData As IHolding) As Task
    '    Dim todayDate As String = Now.ToString("yy_MM_dd")
    '    For Each runningFile In Directory.GetFiles(My.Application.Info.DirectoryPath, "*.Holdings.a2t")
    '        If Not runningFile.Contains(todayDate) Then File.Delete(runningFile)
    '    Next
    '    Dim holdingFileName As String = Path.Combine(My.Application.Info.DirectoryPath, String.Format("{0}_{1}.Holdings.a2t", Me.ToString, todayDate))
    '    If File.Exists(holdingFileName) Then
    '        Dim dayStartHoldingData As IHolding = Utilities.Strings.DeserializeToCollection(Of IHolding)(holdingFileName)
    '        Return MyBase.ProcessHoldingAsync(dayStartHoldingData)
    '    Else
    '        Return MyBase.ProcessHoldingAsync(holdingData)
    '    End If
    '    If HoldingDetails IsNot Nothing Then
    '        Utilities.Strings.SerializeFromCollection(Of IHolding)(holdingFileName, Me.HoldingDetails)
    '    End If
    'End Function

    Public Overrides Async Function MonitorAsync() As Task
        Try
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    If placeOrderTrigger.Item2.Quantity <> 0 Then
                        Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularMarketCNCOrder, Nothing).ConfigureAwait(False)
                        If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                            placeOrderResponse("data").ContainsKey("order_id") Then
                            If ForceExitByUser Then
                                ForceExitByUser = False
                                OnHeartbeat(String.Format("Force exit successful: {0}", Me.TradableInstrument.TradingSymbol))
                            End If
                            If ForceExitForContractRollover Then
                                ForceExitForContractRollover = False
                                OnHeartbeat(String.Format("Force exit for contract rollover successful: {0}", Me.TradableInstrument.TradingSymbol))
                            End If
                            If ForceEntryForContractRollover Then
                                ForceEntryForContractRollover = False
                                OnHeartbeat(String.Format("Force entry for contract rollover successful: {0}", Me.TradableInstrument.TradingSymbol))
                            End If
                        End If
                    Else
                        If ForceExitByUser Then
                            ForceExitByUser = False
                            OnHeartbeat(String.Format("No position available for force exit: {0}", Me.TradableInstrument.TradingSymbol))
                        End If
                        If ForceExitForContractRollover Then
                            ForceExitForContractRollover = False
                            OnHeartbeat(String.Format("No position available for contract rollover force exit: {0}", Me.TradableInstrument.TradingSymbol))
                        End If
                        If ForceEntryForContractRollover Then
                            ForceEntryForContractRollover = False
                            OnHeartbeat(String.Format("No position available for contract rollover force entry: {0}", Me.TradableInstrument.TradingSymbol))
                        End If
                    End If
                End If

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

    Protected Overrides Async Function IsTriggerReceivedForPlaceOrderAsync(forcePrint As Boolean) As Task(Of Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String))
        Dim ret As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        Dim emaCrossoverUserSettings As EMACrossoverUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(emaCrossoverUserSettings.SignalTimeFrame)
        Dim fastEMAConsumer As EMAConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummyFastEMAConsumer)
        Dim slowEMAConsumer As EMAConsumer = GetConsumer(Me.RawPayloadDependentConsumers, _dummySlowEMAConsumer)
        Dim currentTime As Date = Now()

        Try
            If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
                If Not runningCandlePayload.PreviousPayload.ToString = lastPrevPayloadPlaceOrder Then
                    lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
                    logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                    logger.Debug("PlaceOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, PayloadGeneratedBy:{1}, IsHistoricalCompleted:{2}, IsFirstTimeInformationCollected:{3}, IsCrossover(above):{4}, IsCrossover(below):{5}, EMA({6}):{7}, EMA({8}):{9}, Force Exit by user:{10}, Quantity:{11}, Exchange Start Time:{12}, Exchange End Time:{13}, Current Time:{14}, Trade entry delay:{15}, Is My Another Contract Available:{16}, Contract Rollover Time:{17}, Contract Rollover Force Exit:{18}, Contract Rollover Force Entry:{19}, TradingSymbol:{20}",
                                runningCandlePayload.SnapshotDateTime.ToString,
                                runningCandlePayload.PayloadGeneratedBy.ToString,
                                Me.TradableInstrument.IsHistoricalCompleted,
                                Me.ParentStrategy.IsFirstTimeInformationCollected,
                                IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Above, True),
                                IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Below, True),
                                emaCrossoverUserSettings.FastEMAPeriod,
                                fastEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                emaCrossoverUserSettings.SlowEMAPeriod,
                                slowEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                Me.ForceExitByUser,
                                GetQuantityToTrade(),
                                Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
                                Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
                                currentTime.ToString,
                                emaCrossoverUserSettings.TradeEntryDelay,
                                IsMyAnotherContractAvailable(),
                                Me.TradableInstrument.ExchangeDetails.ContractRolloverTime.ToString,
                                Me.ForceExitForContractRollover,
                                Me.ForceEntryForContractRollover,
                                Me.TradableInstrument.TradingSymbol)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If (ForceExitByUser OrElse ForceExitForContractRollover OrElse ForceEntryForContractRollover) AndAlso
            currentTime >= Me.TradableInstrument.ExchangeDetails.ExchangeStartTime AndAlso currentTime <= Me.TradableInstrument.ExchangeDetails.ExchangeEndTime Then
            Dim quantity As Integer = GetQuantityToTrade() / 2

            If ForceExitForContractRollover Then
                emaCrossoverUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).ModifiedQuantity = quantity
            End If

            If quantity > 0 Then
                parameters = New PlaceOrderParameters(runningCandlePayload) With
                                   {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                    .Quantity = Math.Abs(quantity)}
            Else
                parameters = New PlaceOrderParameters(runningCandlePayload) With
                                  {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                   .Quantity = Math.Abs(quantity)}
            End If

            If Me.ForceEntryForContractRollover Then
                quantity = emaCrossoverUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).ModifiedQuantity
                If quantity > 0 Then
                    parameters = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                        .Quantity = Math.Abs(quantity)}
                Else
                    parameters = New PlaceOrderParameters(runningCandlePayload) With
                                      {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                       .Quantity = Math.Abs(quantity)}
                End If
            End If

        ElseIf currentTime >= Me.TradableInstrument.ExchangeDetails.ExchangeStartTime AndAlso currentTime <= Me.TradableInstrument.ExchangeDetails.ExchangeEndTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            currentTime <= runningCandlePayload.SnapshotDateTime.AddMinutes(emaCrossoverUserSettings.TradeEntryDelay) AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
            If (Me.TradableInstrument.Expiry.Value.Date <> Now.Date AndAlso Not IsMyAnotherContractAvailable.Item1) OrElse
                (Me.TradableInstrument.Expiry.Value.Date <> Now.Date AndAlso IsMyAnotherContractAvailable.Item1 AndAlso currentTime >= Me.TradableInstrument.ExchangeDetails.ContractRolloverTime) OrElse
                (Me.TradableInstrument.Expiry.Value.Date = Now.Date AndAlso IsMyAnotherContractAvailable.Item1 AndAlso currentTime < Me.TradableInstrument.ExchangeDetails.ContractRolloverTime) Then
                If IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Above, False) Then
                    Dim quantity As Integer = GetQuantityToTrade()
                    If quantity < 0 Then
                        parameters = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                                        .Quantity = Math.Abs(quantity)}
                    End If
                ElseIf IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Below, False) Then
                    Dim quantity As Integer = GetQuantityToTrade()
                    If quantity > 0 Then
                        parameters = New PlaceOrderParameters(runningCandlePayload) With
                                       {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                                        .Quantity = Math.Abs(quantity)}
                    End If
                End If
            End If
        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            Try
                If forcePrint Then
                    logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
                    If Me.TradableInstrument.IsHistoricalCompleted Then
                        logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
                        logger.Debug("PlaceOrder-> Rest all parameters: 
                                    RunningCandlePayloadSnapshotDateTime:{0}, PayloadGeneratedBy:{1}, 
                                    IsHistoricalCompleted:{2}, IsFirstTimeInformationCollected:{3}, 
                                    IsCrossover(above):{4}, IsCrossover(below):{5}, 
                                    EMA({6}):{7}, EMA({8}):{9}, 
                                    Force Exit by user:{10}, Quantity:{11},
                                    Exchange Start Time:{12}, Exchange End Time:{13},
                                    Current Time:{14}, Trade entry delay:{15},
                                    Is My Another Contract Available:{16}, Contract Rollover Time:{17},
                                    Contract Rollover Force Exit:{18}, Contract Rollover Force Entry:{19},
                                    TradingSymbol:{20}",
                                    runningCandlePayload.SnapshotDateTime.ToString,
                                    runningCandlePayload.PayloadGeneratedBy.ToString,
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    Me.ParentStrategy.IsFirstTimeInformationCollected,
                                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Above, True),
                                    IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Below, True),
                                    emaCrossoverUserSettings.FastEMAPeriod,
                                    fastEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                    emaCrossoverUserSettings.SlowEMAPeriod,
                                    slowEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
                                    Me.ForceExitByUser,
                                    GetQuantityToTrade(),
                                    Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
                                    Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
                                    currentTime.ToString,
                                    emaCrossoverUserSettings.TradeEntryDelay,
                                    IsMyAnotherContractAvailable(),
                                    Me.TradableInstrument.ExchangeDetails.ContractRolloverTime.ToString,
                                    Me.ForceExitForContractRollover,
                                    Me.ForceEntryForContractRollover,
                                    Me.TradableInstrument.TradingSymbol)
                    ElseIf ForceExitByUser Then
                        logger.Debug("PlaceOrder-> Rest all parameters:
                                    Force exit done by user before historical completed. 
                                    IsHistoricalCompleted:{0}, IsFirstTimeInformationCollected:{1}, 
                                    Force Exit by user:{2}, Quantity:{3},
                                    Exchange Start Time:{4}, Exchange End Time:{5},
                                    Current Time:{6}, Trading Symbol:{7}",
                                    Me.TradableInstrument.IsHistoricalCompleted,
                                    Me.ParentStrategy.IsFirstTimeInformationCollected,
                                    ForceExitByUser,
                                    GetQuantityToTrade(),
                                    Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
                                    Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
                                    currentTime.ToString,
                                    Me.TradableInstrument.TradingSymbol)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex)
            End Try

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

    Private Function GetQuantityToTrade() As Integer
        Dim ret As Integer = 0
        Dim emaCrossoverUserSettings As EMACrossoverUserInputs = Me.ParentStrategy.UserSettings
        If PositionDetails IsNot Nothing Then
            ret = Me.PositionDetails.Quantity * 2
        End If
        If ret = 0 Then
            If Not ForceExitByUser AndAlso Not ForceExitForContractRollover Then
                ret = Me.TradableInstrument.LotSize * emaCrossoverUserSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).InitialQuantity
            End If
        End If
        Return ret
    End Function

    Private Function IsMyAnotherContractAvailable() As Tuple(Of Boolean, EMACrossoverStrategyInstrument)
        Dim ret As Tuple(Of Boolean, EMACrossoverStrategyInstrument) = New Tuple(Of Boolean, EMACrossoverStrategyInstrument)(False, Nothing)
        For Each runningStrategyInstrument As EMACrossoverStrategyInstrument In Me.ParentStrategy.TradableStrategyInstruments
            If runningStrategyInstrument.TradableInstrument.InstrumentIdentifier <> Me.TradableInstrument.InstrumentIdentifier AndAlso
                runningStrategyInstrument.TradableInstrument.RawInstrumentName = Me.TradableInstrument.RawInstrumentName Then
                ret = New Tuple(Of Boolean, EMACrossoverStrategyInstrument)(True, runningStrategyInstrument)
                Exit For
            End If
        Next
        Return ret
    End Function

    Public Async Function ContractRolloverAsync() As Task
        If Me.TradableInstrument.Expiry.Value.Date = Now.Date Then
            Try
                While True
                    If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                        Throw Me.ParentStrategy.ParentController.OrphanException
                    End If
                    _cts.Token.ThrowIfCancellationRequested()

                    If Now >= Me.TradableInstrument.ExchangeDetails.ContractRolloverTime AndAlso
                        IsMyAnotherContractAvailable.Item1 Then
                        Me.ForceExitForContractRollover = True
                        While Me.ForceExitForContractRollover
                            Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                        End While
                        IsMyAnotherContractAvailable.Item2.ForceEntryForContractRollover = True
                        Exit While
                    End If

                    Await Task.Delay(60000, _cts.Token).ConfigureAwait(False)
                End While
            Catch ex As Exception
                logger.Error("Strategy Instrument:{0}, error:{1}", Me.ToString, ex.ToString)
                Throw ex
            End Try
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
