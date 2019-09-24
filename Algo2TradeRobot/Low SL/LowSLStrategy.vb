Imports System.Threading
Imports System.IO
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog
Imports Utilities.DAL

Public Class LowSLStrategy
    Inherits Strategy

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public Sub New(ByVal associatedParentController As APIStrategyController,
                   ByVal strategyIdentifier As String,
                   ByVal userSettings As LowSLUserInputs,
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
            Dim userInputs As LowSLUserInputs = CType(Me.UserSettings, LowSLUserInputs)
            If userInputs.AutoSelectStock Then
                Using fillInstrumentDetails As New LowSLFillInstrumentDetails(_cts, Me)
                    Await fillInstrumentDetails.GetInstrumentData(allInstruments, bannedInstruments).ConfigureAwait(False)
                End Using
                logger.Debug(Utilities.Strings.JsonSerialize(Me.UserSettings))
            End If
            If userInputs.InstrumentsData IsNot Nothing AndAlso userInputs.InstrumentsData.Count > 0 Then
                Dim dummyAllInstruments As List(Of IInstrument) = allInstruments.ToList
                For Each instrument In userInputs.InstrumentsData
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim runningTradableInstrument As IInstrument = Nothing
                    runningTradableInstrument = dummyAllInstruments.Find(Function(x)
                                                                             Return x.TradingSymbol = instrument.Value.TradingSymbol
                                                                         End Function)

                    _cts.Token.ThrowIfCancellationRequested()

                    If retTradableInstrumentsAsPerStrategy Is Nothing Then retTradableInstrumentsAsPerStrategy = New List(Of IInstrument)
                    If runningTradableInstrument IsNot Nothing Then
                        retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
                        If userInputs.AutoSelectStock AndAlso runningTradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures Then
                            Dim cashInstrument As IInstrument = dummyAllInstruments.Find(Function(x)
                                                                                             Return x.TradingSymbol = runningTradableInstrument.RawInstrumentName
                                                                                         End Function)
                            If cashInstrument IsNot Nothing Then retTradableInstrumentsAsPerStrategy.Add(cashInstrument)
                        End If
                    End If
                    ret = True
                Next
                TradableInstrumentsAsPerStrategy = retTradableInstrumentsAsPerStrategy
            End If
        End If
        If retTradableInstrumentsAsPerStrategy IsNot Nothing AndAlso retTradableInstrumentsAsPerStrategy.Count > 0 Then
            'Now create the strategy tradable instruments
            Dim retTradableStrategyInstruments As List(Of LowSLStrategyInstrument) = Nothing
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
                If retTradableStrategyInstruments Is Nothing Then retTradableStrategyInstruments = New List(Of LowSLStrategyInstrument)
                Dim runningTradableStrategyInstrument As New LowSLStrategyInstrument(runningTradableInstrument, Me, False, _cts)
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
            For Each tradableStrategyInstrument As LowSLStrategyInstrument In TradableStrategyInstruments
                _cts.Token.ThrowIfCancellationRequested()
                tasks.Add(Task.Run(AddressOf tradableStrategyInstrument.MonitorAsync, _cts.Token))
            Next
            tasks.Add(Task.Run(AddressOf ForceExitAllTradesAsync, _cts.Token))
            If CType(Me.UserSettings, LowSLUserInputs).AutoSelectStock Then tasks.Add(Task.Run(AddressOf CompleteProcessAsync, _cts.Token))
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
        Dim userSettings As LowSLUserInputs = Me.UserSettings
        Dim currentTime As Date = Now
        If currentTime >= Me.UserSettings.EODExitTime Then
            ret = New Tuple(Of Boolean, String)(True, "EOD Exit")
        ElseIf Me.GetTotalPL <= Math.Abs(userSettings.MaxLossPerDay) * -1 Then
            ret = New Tuple(Of Boolean, String)(True, "Max Loss Per Day Reached")
        ElseIf Me.GetTotalPL >= Math.Abs(userSettings.MaxProfitPerDay) Then
            ret = New Tuple(Of Boolean, String)(True, "Max Profit Per Day Reached")
        End If
        Return ret
    End Function

    Private Async Function CompleteProcessAsync() As Task
        Try
            Dim userSettings As LowSLUserInputs = Me.UserSettings
            Dim delayCtr As Integer = 0
            Dim cashStrategyInstrumentList As IEnumerable(Of StrategyInstrument) = Nothing
            If Me.TradableStrategyInstruments IsNot Nothing AndAlso Me.TradableStrategyInstruments.Count > 0 Then
                cashStrategyInstrumentList = Me.TradableStrategyInstruments.Where(Function(x)
                                                                                      Return x.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash
                                                                                  End Function)
            End If
            While True
                If Me.ParentController.OrphanException IsNot Nothing Then
                    Throw Me.ParentController.OrphanException
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If Now > Me.UserSettings.TradeStartTime.AddSeconds(5) AndAlso cashStrategyInstrumentList.Where(Function(z)
                                                                                                                   Return CType(z, LowSLStrategyInstrument).VolumeChangePercentage = Decimal.MinValue
                                                                                                               End Function).Count = 0 Then
                    If cashStrategyInstrumentList IsNot Nothing AndAlso cashStrategyInstrumentList.Count > 0 Then
                        Dim counter As Integer = 0
                        For Each runningCashInstrument In cashStrategyInstrumentList.OrderByDescending(Function(x)
                                                                                                           Return CType(x, LowSLStrategyInstrument).VolumeChangePercentage
                                                                                                       End Function)
                            If CType(runningCashInstrument, LowSLStrategyInstrument).VolumeChangePercentage > CType(Me.UserSettings, LowSLUserInputs).MinVolumeSpikePercentage Then
                                Dim futureIntruments As IEnumerable(Of StrategyInstrument) =
                                Me.TradableStrategyInstruments.Where(Function(x)
                                                                         Return x.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Futures AndAlso
                                                                        x.TradableInstrument.RawInstrumentName = runningCashInstrument.TradableInstrument.TradingSymbol
                                                                     End Function)
                                If futureIntruments IsNot Nothing AndAlso futureIntruments.Count > 0 Then
                                    Dim stockPrice As Decimal = futureIntruments.FirstOrDefault.TradableInstrument.LastTick.LastPrice
                                    Dim quantity As Integer = futureIntruments.FirstOrDefault.CalculateQuantityFromInvestment(stockPrice, userSettings.InstrumentsData(futureIntruments.FirstOrDefault.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, True)
                                    Dim capital As Decimal = stockPrice * quantity / userSettings.InstrumentsData(futureIntruments.FirstOrDefault.TradableInstrument.TradingSymbol).MarginMultiplier
                                    If capital < userSettings.MaxCapital Then
                                        Dim stoploss As Decimal = futureIntruments.FirstOrDefault.CalculateStplossFromPL(stockPrice, quantity, Math.Abs(userSettings.MaxStoploss) * -1)
                                        Dim slPoint As Decimal = stockPrice - stoploss - futureIntruments.FirstOrDefault.TradableInstrument.TickSize
                                        If slPoint > 0.2 Then
                                            CType(futureIntruments.FirstOrDefault, LowSLStrategyInstrument).Quantity = quantity
                                            CType(futureIntruments.FirstOrDefault, LowSLStrategyInstrument).SLPoint = slPoint
                                            CType(futureIntruments.FirstOrDefault, LowSLStrategyInstrument).VolumeChangePercentage = CType(runningCashInstrument, LowSLStrategyInstrument).VolumeChangePercentage
                                            CType(futureIntruments.FirstOrDefault, LowSLStrategyInstrument).EligibleToTakeTrade = True
                                            futureIntruments.FirstOrDefault.TradableInstrument.FetchHistorical = True
                                            counter += 1
                                            Console.WriteLine(String.Format("{0} : {1}",
                                                                            futureIntruments.FirstOrDefault.TradableInstrument.TradingSymbol,
                                                                            CType(futureIntruments.FirstOrDefault, LowSLStrategyInstrument).VolumeChangePercentage))
                                        End If
                                    End If
                                Else
                                    Dim stockPrice As Decimal = runningCashInstrument.TradableInstrument.LastTick.LastPrice
                                    Dim quantity As Integer = runningCashInstrument.CalculateQuantityFromInvestment(stockPrice, userSettings.InstrumentsData(runningCashInstrument.TradableInstrument.TradingSymbol).MarginMultiplier, userSettings.MinCapital, True)
                                    Dim capital As Decimal = stockPrice * quantity / userSettings.InstrumentsData(runningCashInstrument.TradableInstrument.TradingSymbol).MarginMultiplier
                                    If capital < userSettings.MaxCapital Then
                                        Dim stoploss As Decimal = runningCashInstrument.CalculateStplossFromPL(stockPrice, quantity, Math.Abs(userSettings.MaxStoploss) * -1)
                                        Dim slPoint As Decimal = stockPrice - stoploss - runningCashInstrument.TradableInstrument.TickSize
                                        If slPoint > 0.2 Then
                                            CType(runningCashInstrument, LowSLStrategyInstrument).Quantity = quantity
                                            CType(runningCashInstrument, LowSLStrategyInstrument).SLPoint = slPoint
                                            CType(runningCashInstrument, LowSLStrategyInstrument).EligibleToTakeTrade = True
                                            runningCashInstrument.TradableInstrument.FetchHistorical = True
                                            counter += 1
                                            Console.WriteLine(String.Format("{0} : {1}",
                                                                    runningCashInstrument.TradableInstrument.TradingSymbol,
                                                                    CType(runningCashInstrument, LowSLStrategyInstrument).VolumeChangePercentage))
                                        End If
                                    End If
                                End If
                                If counter = CType(Me.UserSettings, LowSLUserInputs).NumberOfStock Then
                                    WriteCSV()
                                    Exit For
                                End If
                            End If
                        Next
                        For Each runningInstrument In Me.TradableStrategyInstruments
                            If Not CType(runningInstrument, LowSLStrategyInstrument).EligibleToTakeTrade Then
                                runningInstrument.TradableInstrument.FetchHistorical = False
                                Await Me.ParentController.UnSubscribeTicker(runningInstrument.TradableInstrument.InstrumentIdentifier).ConfigureAwait(False)
                                CType(runningInstrument, LowSLStrategyInstrument).StopStrategyInstrument = True
                            End If
                        Next
                        Exit While
                    End If
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

    Private Async Function WriteCSV() As Task
        Await Task.Delay(0).ConfigureAwait(False)
        Try
            If Me.TradableStrategyInstruments IsNot Nothing AndAlso Me.TradableStrategyInstruments.Count > 0 Then
                Dim allStockData As DataTable = Nothing
                If CType(Me.UserSettings, LowSLUserInputs).InstrumentDetailsFilePath IsNot Nothing AndAlso
                    File.Exists(CType(Me.UserSettings, LowSLUserInputs).InstrumentDetailsFilePath) Then
                    File.Delete(CType(Me.UserSettings, LowSLUserInputs).InstrumentDetailsFilePath)
                    Using csv As New CSVHelper(CType(Me.UserSettings, LowSLUserInputs).InstrumentDetailsFilePath, ",", _cts)
                        _cts.Token.ThrowIfCancellationRequested()
                        allStockData = New DataTable
                        allStockData.Columns.Add("Trading Symbol")
                        allStockData.Columns.Add("Margin Multiplier")
                        allStockData.Columns.Add("Day ATR")
                        allStockData.Columns.Add("Quantity")
                        allStockData.Columns.Add("SL Point")
                        For Each runningInstrument In Me.TradableStrategyInstruments
                            If CType(runningInstrument, LowSLStrategyInstrument).EligibleToTakeTrade Then
                                Dim row As DataRow = allStockData.NewRow
                                row("Trading Symbol") = runningInstrument.TradableInstrument.TradingSymbol
                                row("Margin Multiplier") = If(runningInstrument.TradableInstrument.InstrumentType = IInstrument.TypeOfInstrument.Cash, 13, 30)
                                row("Day ATR") = CType(runningInstrument, LowSLStrategyInstrument).DayATR
                                row("Quantity") = CType(runningInstrument, LowSLStrategyInstrument).Quantity
                                row("SL Point") = CType(runningInstrument, LowSLStrategyInstrument).SLPoint
                                allStockData.Rows.Add(row)
                            End If
                        Next
                        csv.GetCSVFromDataTable(allStockData)
                    End Using
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
        End Try
    End Function
End Class
