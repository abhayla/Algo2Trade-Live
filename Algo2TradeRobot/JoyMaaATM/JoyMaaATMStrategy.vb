Imports System.Text.RegularExpressions
Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog

Public Class JoyMaaATMStrategy
    Inherits Strategy

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public Sub New(ByVal associatedParentController As APIStrategyController,
                   ByVal strategyIdentifier As String,
                   ByVal userSettings As JoyMaaATMUserInputs,
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
        Await Task.Delay(1, _cts.Token).ConfigureAwait(False)
        logger.Debug("Starting to fill strategy specific instruments, strategy:{0}", Me.ToString)
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            Dim userInputs As JoyMaaATMUserInputs = Me.UserSettings
            If userInputs.InstrumentsData IsNot Nothing AndAlso userInputs.InstrumentsData.Count > 0 Then
                Dim dummyAllInstruments As List(Of IInstrument) = allInstruments.ToList
                For Each instrument In userInputs.InstrumentsData
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim runningTradableInstrument As IInstrument = Nothing
                    Dim allTradableInstruments As List(Of IInstrument) = dummyAllInstruments.FindAll(Function(x)
                                                                                                         Return Regex.Replace(x.TradingSymbol, "[0-9]+[A-Z]+FUT", "") = instrument.Key AndAlso
                                                                                                            x.RawInstrumentType = "FUT" AndAlso (x.RawExchange = "NFO" OrElse x.RawExchange = "MCX")
                                                                                                     End Function)
                    Dim minExpiry As Date = allTradableInstruments.Min(Function(x)
                                                                           If Not x.Expiry.Value.Date = Now.Date Then
                                                                               Return x.Expiry.Value
                                                                           Else
                                                                               Return Date.MaxValue
                                                                           End If
                                                                       End Function)
                    runningTradableInstrument = allTradableInstruments.Find(Function(x)
                                                                                Return x.Expiry = minExpiry
                                                                            End Function)

                    _cts.Token.ThrowIfCancellationRequested()
                    If retTradableInstrumentsAsPerStrategy Is Nothing Then retTradableInstrumentsAsPerStrategy = New List(Of IInstrument)
                    If runningTradableInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
                    ret = True
                Next
                TradableInstrumentsAsPerStrategy = retTradableInstrumentsAsPerStrategy
            End If
        End If
        If retTradableInstrumentsAsPerStrategy IsNot Nothing AndAlso retTradableInstrumentsAsPerStrategy.Count > 0 Then
            'Now create the strategy tradable instruments
            Dim retTradableStrategyInstruments As List(Of JoyMaaATMStrategyInstrument) = Nothing
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
                If retTradableStrategyInstruments Is Nothing Then retTradableStrategyInstruments = New List(Of JoyMaaATMStrategyInstrument)
                Dim runningTradableStrategyInstrument As New JoyMaaATMStrategyInstrument(runningTradableInstrument, Me, False, _cts)
                AddHandler runningTradableStrategyInstrument.HeartbeatEx, AddressOf OnHeartbeatEx
                AddHandler runningTradableStrategyInstrument.WaitingForEx, AddressOf OnWaitingForEx
                AddHandler runningTradableStrategyInstrument.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                AddHandler runningTradableStrategyInstrument.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx

                retTradableStrategyInstruments.Add(runningTradableStrategyInstrument)
                'If runningTradableInstrument.FirstLevelConsumers Is Nothing Then runningTradableInstrument.FirstLevelConsumers = New List(Of StrategyInstrument)
                'runningTradableInstrument.FirstLevelConsumers.Add(runningTradableStrategyInstrument)
            Next
            TradableStrategyInstruments = retTradableStrategyInstruments
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
            For Each tradableStrategyInstrument As JoyMaaATMStrategyInstrument In TradableStrategyInstruments
                _cts.Token.ThrowIfCancellationRequested()
                tasks.Add(Task.Run(AddressOf tradableStrategyInstrument.MonitorAsync, _cts.Token))
            Next
            tasks.Add(Task.Run(AddressOf ForceExitAllTradesAsync, _cts.Token))
            tasks.Add(Task.Run(AddressOf GetOverAllPLDrawUpDrawDownAsync, _cts.Token))
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
        Dim userSettings As JoyMaaATMUserInputs = Me.UserSettings
        Dim currentTime As Date = Now
        If currentTime >= Me.UserSettings.EODExitTime Then
            ret = New Tuple(Of Boolean, String)(True, "EOD Exit")
        End If
        Return ret
    End Function

    Private Async Function GetOverAllPLDrawUpDrawDownAsync() As Task
        Try
            Dim lastPL As Decimal = Decimal.MinValue
            While True
                If Me.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()

                If Me.GetTotalPLAfterBrokerage <> lastPL AndAlso (Me.GetTotalPLAfterBrokerage <> 0 OrElse Me.MaxDrawUp <> 0 OrElse Me.MaxDrawDown <> 0) Then
                    Dim message As String = Nothing
                    If Me.ParentController.UserInputs.FormRemarks IsNot Nothing AndAlso Not Me.ParentController.UserInputs.FormRemarks = "" Then
                        message = String.Format("{0}{1}PL:{2}, MaxDrawUP:{3}, MaxDrawDown:{4}",
                                                Me.ParentController.UserInputs.FormRemarks, vbNewLine,
                                                Math.Round(Me.GetTotalPLAfterBrokerage, 2),
                                                Math.Round(Me.MaxDrawUp, 2),
                                                Math.Round(Me.MaxDrawDown, 2))
                    Else
                        message = String.Format("Joy Maa ATM. PL:{0}, MaxDrawUP:{1}, MaxDrawDown:{2}",
                                                Math.Round(Me.GetTotalPLAfterBrokerage, 2),
                                                Math.Round(Me.MaxDrawUp, 2),
                                                Math.Round(Me.MaxDrawDown, 2))
                    End If
                    If message.Contains("&") Then
                        message = message.Replace("&", "_")
                    End If

                    Dim userInputs As JoyMaaATMUserInputs = Me.UserSettings
                    If userInputs.TelegramAPIKey IsNot Nothing AndAlso Not userInputs.TelegramAPIKey.Trim = "" AndAlso
                        userInputs.TelegramChatID IsNot Nothing AndAlso Not userInputs.TelegramPLChatID.Trim = "" Then
                        Using tSender As New Utilities.Notification.Telegram(userInputs.TelegramAPIKey.Trim, userInputs.TelegramPLChatID, _cts)
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
