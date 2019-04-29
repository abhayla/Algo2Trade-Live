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
                   ByVal userSettings As NearFarHedgingStrategyUserInputs,
                   ByVal maxNumberOfDaysForHistoricalFetch As Integer,
                   ByVal canceller As CancellationTokenSource)
        MyBase.New(associatedParentController, strategyIdentifier, True, userSettings, maxNumberOfDaysForHistoricalFetch, canceller)
        Me.ExitAllTrades = False
        'Though the TradableStrategyInstruments is being populated from inside by newing it,
        'lets also initiatilize here so that after creation of the strategy and before populating strategy instruments,
        'the fron end grid can bind to this created TradableStrategyInstruments which will be empty
        'TradableStrategyInstruments = New List(Of StrategyInstrument)
    End Sub
    Public Overrides Async Function CreateTradableStrategyInstrumentsAsync(allInstruments As IEnumerable(Of IInstrument)) As Task(Of Boolean)
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
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then

            'Get Strategy Instruments
            Dim userInputs As NearFarHedgingStrategyUserInputs = CType(Me.UserSettings, NearFarHedgingStrategyUserInputs)
            If userInputs.InstrumentsData IsNot Nothing AndAlso userInputs.InstrumentsData.Count > 0 Then
                Dim dummyAllInstruments As List(Of IInstrument) = allInstruments.ToList
                For Each instrument In userInputs.InstrumentsData
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim runningTradableInstrument As IInstrument = Nothing
                    Dim allTradableInstruments As List(Of IInstrument) = dummyAllInstruments.FindAll(Function(x)
                                                                                                         Return Regex.Replace(x.TradingSymbol, "[0-9]+[A-Z]+FUT", "") = instrument.Key AndAlso
                                                                                                             x.RawInstrumentType = "FUT" AndAlso (x.RawExchange = "NFO" OrElse x.RawExchange = "MCX" OrElse x.RawExchange = "CDS")
                                                                                                     End Function)

                    Dim minExpiry As Date = allTradableInstruments.Min(Function(x)
                                                                           If Not x.Expiry.Value.Date = Now.Date Then
                                                                               Return x.Expiry.Value
                                                                           Else
                                                                               Return Date.MaxValue
                                                                           End If
                                                                       End Function)

                    Dim minNextExpiry As Date = allTradableInstruments.Min(Function(x)
                                                                               If x.Expiry.Value.Date > minExpiry.Date Then
                                                                                   Return x.Expiry.Value
                                                                               Else
                                                                                   Return Date.MaxValue
                                                                               End If
                                                                           End Function)

                    runningTradableInstrument = allTradableInstruments.Find(Function(x)
                                                                                Return x.Expiry = minExpiry
                                                                            End Function)
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = True
                    If retTradableInstrumentsAsPerStrategy Is Nothing Then retTradableInstrumentsAsPerStrategy = New List(Of IInstrument)
                    If runningTradableInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)

                    runningTradableInstrument = allTradableInstruments.Find(Function(x)
                                                                                Return x.Expiry = minNextExpiry
                                                                            End Function)
                    If runningTradableInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
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
                Dim isPairInstrument As Boolean = False
                Dim anotherContract As IInstrument = retTradableInstrumentsAsPerStrategy.Find(Function(x)
                                                                                                  Return x.RawInstrumentName = runningTradableInstrument.RawInstrumentName AndAlso
                                                                                                    x.TradingSymbol <> runningTradableInstrument.TradingSymbol
                                                                                              End Function)
                If anotherContract IsNot Nothing Then isPairInstrument = True

                _cts.Token.ThrowIfCancellationRequested()
                If retTradableStrategyInstruments Is Nothing Then retTradableStrategyInstruments = New List(Of NearFarHedgingStrategyInstrument)
                Dim runningTradableStrategyInstrument As New NearFarHedgingStrategyInstrument(runningTradableInstrument, Me, isPairInstrument, _cts)
                AddHandler runningTradableStrategyInstrument.HeartbeatEx, AddressOf OnHeartbeatEx
                AddHandler runningTradableStrategyInstrument.WaitingForEx, AddressOf OnWaitingForEx
                AddHandler runningTradableStrategyInstrument.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                AddHandler runningTradableStrategyInstrument.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx

                retTradableStrategyInstruments.Add(runningTradableStrategyInstrument)
                'If runningTradableInstrument.FirstLevelConsumers Is Nothing Then runningTradableInstrument.FirstLevelConsumers = New List(Of StrategyInstrument)
                'runningTradableInstrument.FirstLevelConsumers.Add(runningTradableStrategyInstrument)
            Next
            TradableStrategyInstruments = retTradableStrategyInstruments

            'Adding dependend pair instrumnents to a strategy instrument
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningStrategyInstrument In TradableStrategyInstruments
                    If runningStrategyInstrument.IsPairInstrument Then
                        Dim pairInstruments As IEnumerable(Of StrategyInstrument) = TradableStrategyInstruments.Where(Function(x)
                                                                                                                          Return x.TradableInstrument.RawInstrumentName = runningStrategyInstrument.TradableInstrument.RawInstrumentName AndAlso
                                                                                                                                x.TradableInstrument.TradingSymbol <> runningStrategyInstrument.TradableInstrument.TradingSymbol
                                                                                                                      End Function)
                        If pairInstruments IsNot Nothing AndAlso pairInstruments.Count > 0 Then
                            runningStrategyInstrument.DependendStratrgyInstruments = pairInstruments
                        End If
                    End If
                Next
            End If
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
                tasks.Add(Task.Run(AddressOf tradableStrategyInstrument.MonitorAsync, _cts.Token))
            Next
            tasks.Add(Task.Run(AddressOf ForceExitAllTradesAsync, _cts.Token))
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
        ElseIf ExitAllTrades Then
            logger.Warn("Exit All Button")
            ret = New Tuple(Of Boolean, String)(True, "Button Exit")
        ElseIf Me.GetTotalPL <= capitalAtDayStart * Math.Abs(Me.UserSettings.MaxLossPercentagePerDay) * -1 / 100 Then
            logger.Warn("MTM Reached")
            ret = New Tuple(Of Boolean, String)(True, "Max Loss % Per Day Reached")
        ElseIf Me.GetTotalPL >= capitalAtDayStart * Math.Abs(Me.UserSettings.MaxProfitPercentagePerDay) / 100 Then
            logger.Warn("MTM Reached")
            ret = New Tuple(Of Boolean, String)(True, "Max Profit % Per Day Reached")
        End If
        Return ret
    End Function
End Class
