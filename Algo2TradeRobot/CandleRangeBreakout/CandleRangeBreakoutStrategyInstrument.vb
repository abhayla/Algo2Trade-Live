Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Algo2TradeCore.Entities.Indicators

Public Class CandleRangeBreakoutStrategyInstrument
    Inherits StrategyInstrument
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Private _lastPrevPayloadPlaceOrder As String = ""
    Private _lastSendTime As Date = Date.MinValue
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
                RawPayloadDependentConsumers.Add(chartConsumer)
            Else
                Throw New ApplicationException(String.Format("Signal Timeframe is 0 or Nothing, does not adhere to the strategy:{0}", Me.ParentStrategy.ToString))
            End If
        End If
    End Sub

    Public Overrides Async Function MonitorAsync() As Task
        Try
            While True
                If Me.ParentStrategy.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentStrategy.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Dim placeOrderTrigger As Tuple(Of ExecuteCommandAction, PlaceOrderParameters, String) = Await IsTriggerReceivedForPlaceOrderAsync(False).ConfigureAwait(False)
                If placeOrderTrigger IsNot Nothing AndAlso placeOrderTrigger.Item1 = ExecuteCommandAction.Take Then
                    If _lastSendTime = Date.MinValue OrElse (_lastSendTime <> Date.MinValue AndAlso _lastSendTime <> placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime) Then
                        _lastSendTime = placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime
                        Dim message As String = String.Format("Trading Symbol:{0}, Direction:{1}, Signal Candle Time:{2}, Top Wick %:{3}, Bottom Wick %:{4}, Timestamp:{5}",
                                                              Me.TradableInstrument.TradingSymbol,
                                                              placeOrderTrigger.Item2.EntryDirection.ToString,
                                                              placeOrderTrigger.Item2.SignalCandle.SnapshotDateTime.ToShortTimeString,
                                                              placeOrderTrigger.Item2.Supporting(0),
                                                              placeOrderTrigger.Item2.Supporting(1),
                                                              Now)
                        GenerateTelegramMessageAsync(message)
                    End If
                    'Dim placeOrderResponse As Object = Await ExecuteCommandAsync(ExecuteCommands.PlaceRegularMarketCNCOrder, Nothing).ConfigureAwait(False)
                    'If placeOrderResponse IsNot Nothing AndAlso placeOrderResponse.ContainsKey("data") AndAlso
                    '    placeOrderResponse("data").ContainsKey("order_id") Then

                    'End If
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
        Dim userSettings As CandleRangeBreakoutUserInputs = Me.ParentStrategy.UserSettings
        Dim runningCandlePayload As OHLCPayload = GetXMinuteCurrentCandle(userSettings.SignalTimeFrame)
        Dim currentTime As Date = Now()

        'Try
        '    If runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Me.TradableInstrument.IsHistoricalCompleted Then
        '        If Not runningCandlePayload.PreviousPayload.ToString = _lastPrevPayloadPlaceOrder Then
        '            _lastPrevPayloadPlaceOrder = runningCandlePayload.PreviousPayload.ToString
        '            logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
        '            logger.Debug("PlaceOrder-> Rest all parameters: RunningCandlePayloadSnapshotDateTime:{0}, PayloadGeneratedBy:{1}, IsHistoricalCompleted:{2}, IsFirstTimeInformationCollected:{3}, IsCrossover(above):{4}, IsCrossover(below):{5}, EMA({6}):{7}, EMA({8}):{9}, Force Exit by user:{10}, Quantity:{11}, Exchange Start Time:{12}, Exchange End Time:{13}, Current Time:{14}, Trade entry delay:{15}, TradingSymbol:{16}",
        '            runningCandlePayload.SnapshotDateTime.ToString,
        '            runningCandlePayload.PayloadGeneratedBy.ToString,
        '            Me.TradableInstrument.IsHistoricalCompleted,
        '            Me.ParentStrategy.IsFirstTimeInformationCollected,
        '            IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Above, True),
        '            IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Below, True),
        '            CandleRangeBreakoutUserSettings.FastEMAPeriod,
        '            fastEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
        '            CandleRangeBreakoutUserSettings.SlowEMAPeriod,
        '            slowEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
        '            Me.ForceExitByUser,
        '            GetQuantityToTrade(),
        '           Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
        '           Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
        '           currentTime.ToString,
        '           CandleRangeBreakoutUserSettings.TradeEntryDelay,
        '           Me.TradableInstrument.TradingSymbol)
        '        End If
        '    End If
        'Catch ex As Exception
        '    logger.Error(ex)
        'End Try

        Dim parameters As PlaceOrderParameters = Nothing
        If currentTime >= userSettings.TradeStartTime AndAlso currentTime <= userSettings.LastTradeEntryTime AndAlso
            runningCandlePayload IsNot Nothing AndAlso runningCandlePayload.SnapshotDateTime >= userSettings.TradeStartTime AndAlso
            runningCandlePayload.PayloadGeneratedBy = OHLCPayload.PayloadSource.CalculatedTick AndAlso
            runningCandlePayload.PreviousPayload IsNot Nothing AndAlso Not IsActiveInstrument() AndAlso Me.TradableInstrument.IsHistoricalCompleted Then

            Dim higherTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).HigherTailPercentage
            Dim lowerTailPercentage As Decimal = userSettings.InstrumentsData(Me.TradableInstrument.RawInstrumentName).LowerTailPercentage
            If (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
                (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then

                parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                    {.EntryDirection = IOrder.TypeOfTransaction.Buy,
                     .Supporting = New List(Of Object)}

                parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)

            ElseIf (runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100 >= higherTailPercentage AndAlso
                (runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100 <= lowerTailPercentage Then

                parameters = New PlaceOrderParameters(runningCandlePayload.PreviousPayload) With
                    {.EntryDirection = IOrder.TypeOfTransaction.Sell,
                     .Supporting = New List(Of Object)}

                parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Top / runningCandlePayload.PreviousPayload.CandleRange) * 100)
                parameters.Supporting.Add((runningCandlePayload.PreviousPayload.CandleWicks.Bottom / runningCandlePayload.PreviousPayload.CandleRange) * 100)

            End If

        End If

        'Below portion have to be done in every place order trigger
        If parameters IsNot Nothing Then
            'Try
            '    If forcePrint Then
            '        logger.Debug("PlaceOrder-> ************************************************ {0}", Me.TradableInstrument.TradingSymbol)
            '        If Me.TradableInstrument.IsHistoricalCompleted Then
            '            logger.Debug("PlaceOrder-> Potential Signal Candle is:{0}. Will check rest parameters.", runningCandlePayload.PreviousPayload.ToString)
            '            logger.Debug("PlaceOrder-> Rest all parameters: 
            '                        RunningCandlePayloadSnapshotDateTime:{0}, PayloadGeneratedBy:{1}, 
            '                        IsHistoricalCompleted:{2}, IsFirstTimeInformationCollected:{3}, 
            '                        IsCrossover(above):{4}, IsCrossover(below):{5}, 
            '                        EMA({6}):{7}, EMA({8}):{9}, 
            '                        Force Exit by user:{10}, Quantity:{11},
            '                        Exchange Start Time:{12}, Exchange End Time:{13},
            '                        Current Time:{14}, Trade entry delay:{15},
            '                        TradingSymbol:{16}",
            '                        runningCandlePayload.SnapshotDateTime.ToString,
            '                        runningCandlePayload.PayloadGeneratedBy.ToString,
            '                        Me.TradableInstrument.IsHistoricalCompleted,
            '                        Me.ParentStrategy.IsFirstTimeInformationCollected,
            '                        IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Above, True),
            '                        IsCrossover(_dummyFastEMAConsumer, _dummySlowEMAConsumer, TypeOfField.EMA, TypeOfField.EMA, runningCandlePayload, Positions.Below, True),
            '                        CandleRangeBreakoutUserSettings.FastEMAPeriod,
            '                        fastEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
            '                        CandleRangeBreakoutUserSettings.SlowEMAPeriod,
            '                        slowEMAConsumer.ConsumerPayloads(runningCandlePayload.PreviousPayload.SnapshotDateTime).ToString,
            '                        Me.ForceExitByUser,
            '                        GetQuantityToTrade(),
            '                        Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
            '                        Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
            '                        currentTime.ToString,
            '                        CandleRangeBreakoutUserSettings.TradeEntryDelay,
            '                        Me.TradableInstrument.TradingSymbol)
            '        ElseIf ForceExitByUser Then
            '            logger.Debug("PlaceOrder-> Rest all parameters:
            '                        Force exit done by user before historical completed. 
            '                        IsHistoricalCompleted:{0}, IsFirstTimeInformationCollected:{1}, 
            '                        Force Exit by user:{2}, Quantity:{3},
            '                        Exchange Start Time:{4}, Exchange End Time:{5},
            '                        Current Time:{6}, Trading Symbol:{7}",
            '                        Me.TradableInstrument.IsHistoricalCompleted,
            '                        Me.ParentStrategy.IsFirstTimeInformationCollected,
            '                        ForceExitByUser,
            '                        GetQuantityToTrade(),
            '                        Me.TradableInstrument.ExchangeDetails.ExchangeStartTime.ToString,
            '                        Me.TradableInstrument.ExchangeDetails.ExchangeEndTime.ToString,
            '                        currentTime.ToString,
            '                        Me.TradableInstrument.TradingSymbol)
            '        End If
            '    End If
            'Catch ex As Exception
            '    logger.Error(ex)
            'End Try

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

    Private Async Function GenerateTelegramMessageAsync(ByVal message As String) As Task
        logger.Debug("Telegram Message:{0}", message)
        If message.Contains("&") Then
            message = message.Replace("&", "_")
        End If
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        Dim userInputs As CandleRangeBreakoutUserInputs = Me.ParentStrategy.UserSettings
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
