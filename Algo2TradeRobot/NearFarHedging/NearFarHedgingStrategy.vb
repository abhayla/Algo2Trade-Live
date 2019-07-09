Imports System.Text.RegularExpressions
Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog

Public Class NearFarHedgingStrategy
    Inherits Strategy
#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public Sub New(ByVal associatedParentController As APIStrategyController,
                   ByVal strategyIdentifier As String,
                   ByVal userSettings As NearFarHedgingUserInputs,
                   ByVal maxNumberOfDaysForHistoricalFetch As Integer,
                   ByVal canceller As CancellationTokenSource)
        MyBase.New(associatedParentController, strategyIdentifier, True, userSettings, maxNumberOfDaysForHistoricalFetch, canceller)
        Me.ExitAllTrades = False
        'Though the TradableStrategyInstruments is being populated from inside by newing it,
        'lets also initiatilize here so that after creation of the strategy and before populating strategy instruments,
        'the fron end grid can bind to this created TradableStrategyInstruments which will be empty
        'TradableStrategyInstruments = New List(Of StrategyInstrument)
    End Sub
    Public Overrides Async Function CreateTradableStrategyInstrumentsAsync(allInstruments As IEnumerable(Of IInstrument), ByVal bannedInstruments As List(Of String)) As Task(Of Boolean)
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            logger.Debug("CreateTradableStrategyInstrumentsAsync, allInstruments.Count:{0}", allInstruments.Count)
        Else
            logger.Debug("CreateTradableStrategyInstrumentsAsync, allInstruments.Count:Nothing or 0")
        End If
        _cts.Token.ThrowIfCancellationRequested()
        Dim ret As Boolean = False
        Dim retTradableInstrumentsAsPerStrategy As List(Of IInstrument) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        logger.Debug("Starting to fill strategy specific instruments, strategy:{0}", Me.ToString)

        Dim userInputs As NearFarHedgingUserInputs = CType(Me.UserSettings, NearFarHedgingUserInputs)
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            'Get Strategy Instruments
            If userInputs.InstrumentsData IsNot Nothing AndAlso userInputs.InstrumentsData.Count > 0 Then
                Dim dummyAllInstruments As List(Of IInstrument) = allInstruments.ToList
                For Each instrument In userInputs.InstrumentsData
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim runningTradableInstrument As IInstrument = Nothing
                    Dim dummyRunningTradableInstrument1 As IInstrument = Nothing
                    Dim dummyRunningTradableInstrument2 As IInstrument = Nothing

                    dummyRunningTradableInstrument1 = dummyAllInstruments.Find(Function(x)
                                                                                   Return x.TradingSymbol = instrument.Value.Pair1TradingSymbol
                                                                               End Function)

                    dummyRunningTradableInstrument2 = dummyAllInstruments.Find(Function(x)
                                                                                   Return x.TradingSymbol = instrument.Value.Pair2TradingSymbol
                                                                               End Function)

                    'If dummyRunningTradableInstrument1.RawInstrumentName = dummyRunningTradableInstrument2.RawInstrumentName Then
                    '    If dummyRunningTradableInstrument1.InstrumentType = IInstrument.TypeOfInstrument.Futures AndAlso
                    '        dummyRunningTradableInstrument2.InstrumentType = IInstrument.TypeOfInstrument.Futures Then

                    '    End If
                    'End If

                    If retTradableInstrumentsAsPerStrategy Is Nothing Then retTradableInstrumentsAsPerStrategy = New List(Of IInstrument)

                    runningTradableInstrument = dummyRunningTradableInstrument1
                    If runningTradableInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
                    _cts.Token.ThrowIfCancellationRequested()

                    runningTradableInstrument = dummyRunningTradableInstrument2
                    If runningTradableInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
                    _cts.Token.ThrowIfCancellationRequested()

                    ret = True
                Next

                TradableInstrumentsAsPerStrategy = retTradableInstrumentsAsPerStrategy
            End If
        End If

        If retTradableInstrumentsAsPerStrategy IsNot Nothing AndAlso retTradableInstrumentsAsPerStrategy.Count > 0 Then
            'tradableInstrumentsAsPerStrategy = tradableInstrumentsAsPerStrategy.Take(5).ToList
            'Now create the strategy tradable instruments
            Dim retTradableStrategyInstruments As List(Of NearFarHedgingStrategyInstrument) = Nothing
            logger.Debug("Creating strategy tradable instruments, _tradableInstruments.count:{0}", retTradableInstrumentsAsPerStrategy.Count)
            'Remove the old handlers from the previous strategyinstruments collection
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningTradableStrategyInstruments In TradableStrategyInstruments
                    RemoveHandler runningTradableStrategyInstruments.HeartbeatEx, AddressOf OnHeartbeatEx
                    RemoveHandler runningTradableStrategyInstruments.WaitingForEx, AddressOf OnWaitingForEx
                    RemoveHandler runningTradableStrategyInstruments.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                    RemoveHandler runningTradableStrategyInstruments.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx
                Next
                TradableStrategyInstruments = Nothing
            End If

            'Now create the fresh handlers
            For Each runningTradableInstrument In retTradableInstrumentsAsPerStrategy
                _cts.Token.ThrowIfCancellationRequested()
                If retTradableStrategyInstruments Is Nothing Then retTradableStrategyInstruments = New List(Of NearFarHedgingStrategyInstrument)
                Dim runningTradableStrategyInstrument As New NearFarHedgingStrategyInstrument(runningTradableInstrument, Me, False, _cts)
                AddHandler runningTradableStrategyInstrument.HeartbeatEx, AddressOf OnHeartbeatEx
                AddHandler runningTradableStrategyInstrument.WaitingForEx, AddressOf OnWaitingForEx
                AddHandler runningTradableStrategyInstrument.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                AddHandler runningTradableStrategyInstrument.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx

                retTradableStrategyInstruments.Add(runningTradableStrategyInstrument)
                'If runningTradableInstrument.FirstLevelConsumers Is Nothing Then runningTradableInstrument.FirstLevelConsumers = New List(Of StrategyInstrument)
                'runningTradableInstrument.FirstLevelConsumers.Add(runningTradableStrategyInstrument)
            Next

            'Loop instrument1_instrument_2
            Dim lastUniqueInstrumentToken As UInteger = 0
            For Each instrument In userInputs.InstrumentsData
                Dim uniqueInstrumentToken As UInteger = Utilities.Numbers.GetUniqueNumber()
                If uniqueInstrumentToken = lastUniqueInstrumentToken Then uniqueInstrumentToken += 1
                If retTradableStrategyInstruments IsNot Nothing AndAlso retTradableStrategyInstruments.Count > 0 Then
                    Dim parentStrategyInstrument1 As NearFarHedgingStrategyInstrument =
                        retTradableStrategyInstruments.Find(Function(x)
                                                                Return x.TradableInstrument.TradingSymbol = instrument.Value.Pair1TradingSymbol
                                                            End Function)

                    Dim parentStrategyInstrument2 As NearFarHedgingStrategyInstrument =
                        retTradableStrategyInstruments.Find(Function(x)
                                                                Return x.TradableInstrument.TradingSymbol = instrument.Value.Pair2TradingSymbol
                                                            End Function)

                    Dim virtualStrategyInstrument As New NearFarHedgingStrategyInstrument(Me.ParentController.CreateDummySingleInstrument(instrument.Value.VirtualInstrumentName, uniqueInstrumentToken, parentStrategyInstrument1.TradableInstrument), Me, True, _cts)
                    AddHandler virtualStrategyInstrument.HeartbeatEx, AddressOf OnHeartbeatEx
                    AddHandler virtualStrategyInstrument.WaitingForEx, AddressOf OnWaitingForEx
                    AddHandler virtualStrategyInstrument.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                    AddHandler virtualStrategyInstrument.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx

                    Dim virtualParentStrategyInstruments As List(Of NearFarHedgingStrategyInstrument) = New List(Of NearFarHedgingStrategyInstrument)
                    virtualParentStrategyInstruments.Add(parentStrategyInstrument1)
                    virtualParentStrategyInstruments.Add(parentStrategyInstrument2)
                    virtualStrategyInstrument.ParentStrategyInstruments = virtualParentStrategyInstruments

                    Dim dependentStrategyInstruments As List(Of NearFarHedgingStrategyInstrument) = New List(Of NearFarHedgingStrategyInstrument)
                    dependentStrategyInstruments.Add(virtualStrategyInstrument)
                    parentStrategyInstrument1.DependendStrategyInstruments = dependentStrategyInstruments
                    parentStrategyInstrument2.DependendStrategyInstruments = dependentStrategyInstruments

                    retTradableStrategyInstruments.Add(virtualStrategyInstrument)
                End If
                lastUniqueInstrumentToken = uniqueInstrumentToken
            Next
            TradableStrategyInstruments = retTradableStrategyInstruments

            ''Adding dependend pair instrumnents to a strategy instrument
            'If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
            '    For Each runningStrategyInstrument In TradableStrategyInstruments
            '        If runningStrategyInstrument.IsPairInstrument Then
            '            Dim pairInstruments As IEnumerable(Of StrategyInstrument) = TradableStrategyInstruments.Where(Function(x)
            '                                                                                                              Return x.TradableInstrument.RawInstrumentName = runningStrategyInstrument.TradableInstrument.RawInstrumentName AndAlso
            '                                                                                                                    x.TradableInstrument.TradingSymbol <> runningStrategyInstrument.TradableInstrument.TradingSymbol
            '                                                                                                          End Function)
            '            If pairInstruments IsNot Nothing AndAlso pairInstruments.Count > 0 Then
            '                runningStrategyInstrument.DependendStrategyInstruments = pairInstruments
            '            End If
            '        End If
            '    Next
            'End If
        Else
            Throw New ApplicationException(String.Format("Cannot run this strategy as no strategy instruments could be created from the tradable instruments, stratgey:{0}", Me.ToString))
        End If

        Return ret
    End Function

    Public Overrides Function ToString() As String
        Return Me.GetType().Name
    End Function

    Public Overrides Async Function MonitorAsync() As Task
        Dim lastException As Exception = Nothing

        Try
            _cts.Token.ThrowIfCancellationRequested()
            Dim tasks As New List(Of Task)()
            For Each tradableStrategyInstrument As NearFarHedgingStrategyInstrument In TradableStrategyInstruments
                _cts.Token.ThrowIfCancellationRequested()
                If tradableStrategyInstrument.IsPairInstrument Then
                    tasks.Add(Task.Run(AddressOf tradableStrategyInstrument.MonitorAsync, _cts.Token))
                End If
            Next
            tasks.Add(Task.Run(AddressOf ForceExitAllTradesAsync, _cts.Token))
            tasks.Add(Task.Run(AddressOf GetOverAllPLDrawUpDrawDown, _cts.Token))
            Await Task.WhenAll(tasks).ConfigureAwait(False)
        Catch ex As Exception
            lastException = ex
            logger.Error(ex)
        End Try
        If lastException IsNot Nothing Then
            Await ParentController.CloseTickerIfConnectedAsync().ConfigureAwait(False)
            Await ParentController.CloseFetcherIfConnectedAsync(False).ConfigureAwait(False)
            Await ParentController.CloseCollectorIfConnectedAsync(False).ConfigureAwait(False)
            Throw lastException
        End If
    End Function

    Protected Overrides Function IsTriggerReceivedForExitAllOrders() As Tuple(Of Boolean, String)
        Dim ret As Tuple(Of Boolean, String) = Nothing
        Dim capitalAtDayStart As Decimal = Me.ParentController.GetUserMargin(Me.TradableInstrumentsAsPerStrategy.FirstOrDefault.ExchangeDetails.ExchangeType)
        Dim currentTime As Date = Now
        If currentTime >= Me.UserSettings.EODExitTime Then
            ret = New Tuple(Of Boolean, String)(True, "EOD Exit")
            'ElseIf ExitAllTrades Then
            '    logger.Warn("Exit All Button")
            '    ret = New Tuple(Of Boolean, String)(True, "Button Exit")
            'ElseIf Me.GetTotalPL <= capitalAtDayStart * Math.Abs(Me.UserSettings.MaxLossPercentagePerDay) * -1 / 100 Then
            '    logger.Warn("MTM Reached")
            '    ret = New Tuple(Of Boolean, String)(True, "Max Loss % Per Day Reached")
            'ElseIf Me.GetTotalPL >= capitalAtDayStart * Math.Abs(Me.UserSettings.MaxProfitPercentagePerDay) / 100 Then
            '    logger.Warn("MTM Reached")
            '    ret = New Tuple(Of Boolean, String)(True, "Max Profit % Per Day Reached")
        End If
        Return ret
    End Function

    Private Async Function GetOverAllPLDrawUpDrawDown() As Task
        Try
            While True
                If Me.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()

                If Me.GetTotalPLAfterBrokerage <> 0 OrElse Me.MaxDrawUp <> 0 OrElse Me.MaxDrawDown <> 0 Then
                    Dim message As String = Nothing
                    If Me.ParentController.UserInputs.FormRemarks IsNot Nothing AndAlso Not Me.ParentController.UserInputs.FormRemarks = "" Then
                        message = String.Format("{0}{1}PL:{2}, MaxDrawUP:{3}, MaxDrawDown:{4}",
                                                Me.ParentController.UserInputs.FormRemarks, vbNewLine,
                                                Math.Round(Me.GetTotalPLAfterBrokerage, 2),
                                                Math.Round(Me.MaxDrawUp, 2),
                                                Math.Round(Me.MaxDrawDown, 2))
                    Else
                        message = String.Format("PL:{0}, MaxDrawUP:{1}, MaxDrawDown:{2}",
                                                Math.Round(Me.GetTotalPLAfterBrokerage, 2),
                                                Math.Round(Me.MaxDrawUp, 2),
                                                Math.Round(Me.MaxDrawDown, 2))
                    End If
                    If message.Contains("&") Then
                        message = message.Replace("&", "_")
                    End If

                    Dim hedgingUserInputs As NearFarHedgingUserInputs = Me.UserSettings
                    If hedgingUserInputs.TelegramAPIKey IsNot Nothing AndAlso Not hedgingUserInputs.TelegramAPIKey.Trim = "" AndAlso
                        hedgingUserInputs.TelegramChatID IsNot Nothing AndAlso Not hedgingUserInputs.TelegramPLChatID.Trim = "" Then
                        Using tSender As New Utilities.Notification.Telegram(hedgingUserInputs.TelegramAPIKey.Trim, hedgingUserInputs.TelegramPLChatID, _cts)
                            Dim encodedString As String = Utilities.Strings.EncodeString(message)
                            Await tSender.SendMessageGetAsync(encodedString).ConfigureAwait(False)
                        End Using
                    End If
                End If
                _cts.Token.ThrowIfCancellationRequested()
                Await Task.Delay(60000, _cts.Token).ConfigureAwait(False)
            End While
        Catch ex As Exception
            logger.Error("Get OverAll PL DrawUp DrawDown message generation error: {0}", ex.ToString)
            Throw ex
        End Try
    End Function
End Class
